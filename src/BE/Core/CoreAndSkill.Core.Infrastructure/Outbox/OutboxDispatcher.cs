using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Common.Logging;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreAndSkill.Core.Infrastructure.Outbox;

// Bộ phát Outbox — docs/wiki-core/be/12-notifications.md §2.3-§2.5. Một lượt DispatchBatchAsync nhặt tối
// đa BatchSize dòng `pending` đã tới hạn và phát TỪNG DÒNG.
//
// MỖI DÒNG là một giao dịch riêng, trong một DI scope riêng, với phạm vi ngữ cảnh mở theo CHÍNH dòng đó
// (đơn vị + người kích hoạt). Trong giao dịch đó:
//   1. khoá dòng bằng `FOR UPDATE SKIP LOCKED` — hai instance không cùng phát một sự kiện;
//   2. chạy MỌI bên nhận của loại sự kiện; phần ghi DB của họ nằm chung giao dịch;
//   3. đánh dấu `done` — cùng giao dịch, nên thông báo đã tạo và dòng `done` cùng có hoặc cùng không.
//
// Lần phát hỏng ở BẤT KỲ pha nào thì giao dịch đó quay lại (không để lại thông báo dở), rồi một giao dịch KHÁC, trong
// một DI scope MỚI, ghi lần thử hỏng (attempt_count, next_attempt_at, last_error). "Bất kỳ pha nào" gồm cả pha SAU bên
// nhận: bên nhận trả thành công nhưng lượt lưu/commit bị từ chối (vi phạm khoá ngoại 23503, trùng khoá 23505, chuỗi quá
// dài 22001, ngoại lệ của interceptor trong SavingChanges, commit không rõ kết quả). Scope mới vì scope của lần phát có
// thể còn giữ chính thực thể vừa bị từ chối, hoặc một kết nối vừa đứt.
//
// Một dòng hỏng KHÔNG chặn dòng xếp sau: dòng nào đã xử lý cũng rời tập tới hạn (`done`, `dead`, hoặc next_attempt_at lùi
// ra sau). Ca duy nhất một dòng nằm nguyên là khi chính việc GHI lần thử hỏng cũng hỏng — thực tế là database không với
// tới được, vì giao dịch đó chỉ chạm đúng dòng outbox. Khi đó không còn gì trung thực để ghi: dòng giữ nguyên, lượt dừng
// và ném lỗi để nhịp quét (OutboxDispatcherHostedService) ghi log Error + chỉ số. Dừng thay vì đi tiếp các dòng sau vì
// database đang không với tới được thì mỗi dòng sau lại tốn trọn hai vòng thử lại của execution strategy mà không ghi
// được gì. Dòng đó vẫn hiện ra qua log của nhịp và tuổi của dòng `pending` cũ nhất (readiness Degraded, §2.5). Nên không
// dòng nào kẹt IM LẶNG.
//
// "Đang tắt" chỉ là token của chính lượt đã huỷ — lọc theo TOKEN, không theo kiểu ngoại lệ. TaskCanceledException vì
// HttpClient.Timeout (token CHƯA huỷ) là lỗi tạm thời của bên nhận, không phải lệnh dừng.
//
// ĐẢM BẢO "ÍT NHẤT MỘT LẦN": bên nhận có tác dụng NGOÀI DB (gửi email, đẩy vào hàng đợi trong bộ nhớ) có
// thể chạy hai lần nếu commit hỏng sau khi họ đã làm. Họ phải idempotent — JobRunner làm vậy nhờ câu
// UPDATE có điều kiện.
//
// Job toàn hệ: bỏ bộ lọc ĐƠN VỊ có tên (luật M5/B6) ở phép đọc danh sách dòng; ngay sau đó mở phạm vi
// của đúng đơn vị dòng đó, nên mọi truy vấn bên trong đều lọc theo đơn vị bình thường.
internal sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IExecutionContextScope executionContextScope,
    IOptions<CoreOutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger)
{
    // internal để test cố định được độ ngẫu nhiên của khoảng lùi.
    internal Func<double> JitterSource { get; set; } = () => Random.Shared.NextDouble();

    // Trả số dòng đã XỬ LÝ trong lượt này (đúng, hỏng đều tính) — 0 nghĩa là không còn gì tới hạn.
    public async Task<int> DispatchBatchAsync(CancellationToken ct)
    {
        List<PendingRow> due;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var now = timeProvider.GetUtcNow();

            due = await db.OutboxMessages
                .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
                .Where(o => o.Status == OutboxStatus.Pending && o.NextAttemptAt <= now)
                .OrderBy(o => o.NextAttemptAt)
                .Take(options.Value.BatchSize)
                .Select(o => new PendingRow(o.Id, o.TenantId, o.TriggeredByUserId, o.TriggeredByUserName))
                .ToListAsync(ct);
        }

        // Mỗi dòng tự bắt lỗi phát của nó và ghi lần thử hỏng (DispatchOneAsync) — một dòng hỏng không dừng lô. Ngoại lệ
        // duy nhất đi ra khỏi vòng này là không GHI được lần thử hỏng: dừng lượt (xem chú thích đầu lớp).
        foreach (var row in due)
            await DispatchOneAsync(row, ct);

        return due.Count;
    }

    private async Task DispatchOneAsync(PendingRow row, CancellationToken ct)
    {
        using var context = executionContextScope.Enter(row.TenantId, row.TriggeredByUserId, row.TriggeredByUserName);

        Attempt attempt;
        try
        {
            attempt = await AttemptAsync(row, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Pha khoá / lưu / commit — ngoài khối bắt quanh bên nhận. Không ghi lần thử hỏng ở đây thì attempt_count giữ
            // nguyên, dòng mãi đứng đầu lô và không bao giờ thành `dead`.
            attempt = StoreFailure(ex);
        }

        switch (attempt.Kind)
        {
            case AttemptKind.Skipped:
                return;

            case AttemptKind.Done:
                CoreMetrics.OutboxDispatched.Add(1);
                return;

            default:
                await RecordFailureAsync(row.Id, attempt, ct);
                return;
        }
    }

    private async Task<Attempt> AttemptAsync(PendingRow row, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var services = scope.ServiceProvider;
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var db = services.GetRequiredService<CoreDbContext>();
        var handlers = services.GetServices<IOutboxEventHandler>().ToList();

        return await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            // Khoá dòng. Không lấy được (instance khác đang giữ, hoặc đã xử lý xong) thì bỏ qua — không
            // phải lỗi.
            //
            // Mệnh đề `tenant_id` ràng PHẠM VI CỦA LỆNH KHOÁ, không bù cho một bộ lọc thiếu: EF BỌC câu thô này
            // thành subquery rồi phủ vị từ đơn vị lên NGOÀI, nên tập dòng TRẢ VỀ đã đúng đơn vị dù không có nó.
            // Nhưng `FOR UPDATE` nằm trong câu TRONG — khoá lấy TRƯỚC khi vị từ ngoài kịp tỉa, nên một dòng của
            // đơn vị khác lọt vào câu trong sẽ bị GIỮ KHOÁ hết giao dịch rồi mới bị lọc khỏi kết quả. Bộ lọc
            // toàn cục phủ tập dòng ĐI RA, nó không phủ THÂN CÂU (ADR-0074). Mệnh đề này đưa bất biến đó từ NƠI
            // GỌI — row.Id và row.TenantId cùng đến từ một dòng — vào chính câu SQL, chỗ đọc được mà không phải
            // truy ngược lên người gọi.
            var message = await db.OutboxMessages
                .FromSql(
                    $"""
                    SELECT * FROM core.outbox_message
                    WHERE id = {row.Id} AND tenant_id = {row.TenantId} AND status = 'pending'
                    FOR UPDATE SKIP LOCKED
                    """)
                .SingleOrDefaultAsync(innerCt);

            if (message is null)
                return new TransactionOutcome<Attempt>(Attempt.Skipped, ShouldCommit: false);

            var envelope = new OutboxEnvelope(
                message.Id, message.TenantId, message.EventType, message.Payload, message.OccurredAt,
                message.TraceId, message.TriggeredByUserId, message.TriggeredByUserName);

            var forEvent = handlers.Where(h => string.Equals(h.EventType, message.EventType, StringComparison.Ordinal)).ToList();
            if (forEvent.Count == 0)
            {
                // Không bên nhận nào (kể cả phiên bản bên nhận không hiểu): từ chối RÕ RÀNG, không thử
                // lại — thử bao nhiêu lần cũng vậy (§2.4, §2.6).
                var error = OutboxHandlingErrors.NoHandler.WithParams(("EventType", message.EventType));
                return new TransactionOutcome<Attempt>(Attempt.Permanent(Describe(error)), ShouldCommit: false);
            }

            try
            {
                foreach (var handler in forEvent)
                {
                    var handled = await handler.HandleAsync(envelope, innerCt);
                    if (handled.IsFailure)
                        return new TransactionOutcome<Attempt>(Attempt.Permanent(Describe(handled.Error!)), ShouldCommit: false);
                }
            }
            catch (Exception ex) when (!innerCt.IsCancellationRequested)
            {
                // Lỗi tạm thời: giao dịch quay lại (rất có thể nó đã hỏng — vd. PostgreSQL đánh dấu
                // giao dịch bị huỷ sau một lỗi DB), ghi lần thử hỏng ở giao dịch KHÁC.
                return new TransactionOutcome<Attempt>(Attempt.Transient(FailureText.Describe(ex)), ShouldCommit: false);
            }

            message.MarkDone(timeProvider.GetUtcNow());
            return new TransactionOutcome<Attempt>(Attempt.Done, ShouldCommit: true);
        }, ct);
    }

    // Lỗi ở pha khoá / lưu / commit — ngoài bên nhận. VĨNH VIỄN khi máy chủ đã trả lời và từ chối bằng một mã mà phân loại
    // tạm thời của Npgsql — chính phân loại CoreExecutionStrategy dựa vào — coi là không tạm thời (vi phạm ràng buộc 23xxx,
    // dữ liệu sai 22xxx): chạy lại y như cũ sẽ bị từ chối y như cũ (§2.4). Còn lại TẠM THỜI, cùng cách bộ phát xếp ngoại lệ
    // của bên nhận: lỗi tạm thời mà chiến lược thử lại đã dùng hết lượt, hết thời hạn chờ, commit không rõ kết quả (nếu thật
    // ra đã commit thì lần sau dòng không còn `pending` và không bị phát lại), lỗi không đến từ máy chủ.
    //
    // last_error mang câu trả lời của máy chủ khi có, không mang câu chung "see the inner exception" của lớp bọc
    // DbUpdateException — người đọc dòng `dead` cần biết ràng buộc nào đã từ chối.
    private static Attempt StoreFailure(Exception exception)
    {
        PostgresException? rejection = null;
        for (var current = exception; current is not null && rejection is null; current = current.InnerException)
            rejection = current as PostgresException;

        var error = FailureText.Describe(rejection ?? exception);
        return rejection is { IsTransient: false } ? Attempt.Permanent(error) : Attempt.Transient(error);
    }

    // Scope MỚI, không dùng lại scope của lần phát: DbContext ở đó có thể còn giữ thực thể vừa bị từ chối, và kết nối có
    // thể vừa đứt. Phạm vi ngữ cảnh (đơn vị của dòng) vẫn là của DispatchOneAsync.
    private async Task RecordFailureAsync(Guid id, Attempt attempt, CancellationToken ct)
    {
        var retryable = attempt.Kind == AttemptKind.Transient;

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var becameDead = await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            var message = await db.OutboxMessages.FirstOrDefaultAsync(o => o.Id == id && o.Status == OutboxStatus.Pending, innerCt);
            if (message is null)
                return new TransactionOutcome<bool?>(null, ShouldCommit: false);

            var now = timeProvider.GetUtcNow();

            // Khoảng lùi tính theo SỐ LẦN THỬ SAU KHI TĂNG — RecordFailure tự tăng, nên truyền giá trị
            // dự kiến (hiện tại + 1).
            var delay = OutboxRetryPolicy.Delay(message.AttemptCount + 1, JitterSource());
            message.RecordFailure(now, attempt.Error!, retryable, options.Value.MaxAttempts, delay);

            return new TransactionOutcome<bool?>(message.Status == OutboxStatus.Dead, ShouldCommit: true);
        }, ct);

        if (becameDead is null)
            return;

        CoreMetrics.OutboxAttemptFailed.Add(1);

        if (becameDead == true)
        {
            // §2.5 ba yêu cầu: KHÔNG IM LẶNG (log Error + chỉ số + readiness Degraded), xem lại được
            // (last_error), phát lại được (`core outbox-replay`).
            CoreMetrics.OutboxDead.Add(1);
            CoreMetrics.BackgroundJobFailed.Add(1);
            logger.LogError(
                "Sự kiện outbox {OutboxId} chuyển sang dead: {Failure} — cần người can thiệp (core outbox-replay --id).",
                id, attempt.Error);
        }
        else
        {
            logger.LogWarning("Phát sự kiện outbox {OutboxId} hỏng, sẽ thử lại: {Failure}", id, attempt.Error);
        }
    }

    // Cập nhật chỉ số cảnh báo sớm — gọi ở mỗi nhịp quét (OutboxDispatcherHostedService).
    public async Task<OutboxSnapshot> MeasureAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var snapshot = await OutboxSnapshot.MeasureAsync(db, timeProvider.GetUtcNow(), ct);

        CoreMetrics.SetOutboxSnapshot(snapshot.PendingCount, snapshot.OldestPendingAge.TotalSeconds, snapshot.DeadCount);
        return snapshot;
    }

    // Lỗi của bên nhận trả về dạng Result: mã + câu dự phòng, đã ráp tham số.
    private static string Describe(Error error)
        => $"{error.Code}: {MessageTemplateRenderer.Render(error.MessageTemplate, error.Params)}";

    private sealed record PendingRow(Guid Id, Guid TenantId, Guid? TriggeredByUserId, string? TriggeredByUserName);

    private enum AttemptKind
    {
        Skipped,
        Done,
        Permanent,
        Transient,
    }

    private sealed record Attempt(AttemptKind Kind, string? Error)
    {
        public static Attempt Skipped { get; } = new(AttemptKind.Skipped, null);

        public static Attempt Done { get; } = new(AttemptKind.Done, null);

        public static Attempt Permanent(string error) => new(AttemptKind.Permanent, error);

        public static Attempt Transient(string error) => new(AttemptKind.Transient, error);
    }
}

// Ảnh chụp trạng thái outbox — dùng cho chỉ số VÀ health check. Đọc TOÀN HỆ (bỏ filter đơn vị có tên).
internal sealed record OutboxSnapshot(long PendingCount, TimeSpan OldestPendingAge, long DeadCount)
{
    public static async Task<OutboxSnapshot> MeasureAsync(CoreDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var pending = db.OutboxMessages
            .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
            .Where(o => o.Status == OutboxStatus.Pending);

        var pendingCount = await pending.LongCountAsync(ct);
        var oldest = await pending.MinAsync(o => (DateTimeOffset?)o.OccurredAt, ct);

        var deadCount = await db.OutboxMessages
            .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
            .LongCountAsync(o => o.Status == OutboxStatus.Dead, ct);

        return new OutboxSnapshot(pendingCount, oldest is null ? TimeSpan.Zero : now - oldest.Value, deadCount);
    }
}
