using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Common.Logging;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Infrastructure.Jobs;

// Việc dở dang sau khi tiến trình dừng — docs/quy-uoc/be-architecture.md §1.1 (IBackgroundJobScheduler:
// "hàng đợi nằm trong bộ nhớ tiến trình — KHÔNG bền qua khởi động lại; v1 chạy một instance nên chấp
// nhận được, cùng đánh đổi với ADR-0014").
//
// Đánh đổi đó có một mặt tối: một việc `running` lúc tiến trình chết KHÔNG BAO GIỜ kết thúc, và người
// dùng poll `GET /jobs/{id}` mãi mãi — lỗi im lặng kiểu tệ nhất. Chạy MỘT LẦN lúc khởi động, đánh dấu:
//   - mọi việc `running` (tiến trình cũ đã chết — v1 chạy một instance);
//   - mọi việc `queued` mà KHÔNG còn dòng outbox `pending` nào cho nó (dòng đã `done` nghĩa là lời đẩy
//     vào hàng đợi đã mất theo tiến trình cũ; dòng `dead` nghĩa là nó không bao giờ tới hàng đợi).
// Việc `queued` còn dòng `pending` là việc bình thường đang chờ bộ phát — KHÔNG đụng.
//
// Đánh dấu bằng CHÍNH đường Job.Fail nên có sự kiện JobFinished -> thông báo tới người khởi tạo.
// Khi nhiều instance (điều kiện cần giải trước khi mở instance thứ hai): việc `running` của instance
// KHÁC sẽ bị đánh dấu oan — phép kiểm này chỉ đúng với v1.
internal sealed class JobRecoveryHostedService(
    IServiceScopeFactory scopeFactory,
    IExecutionContextScope executionContextScope,
    TimeProvider timeProvider,
    ILogger<JobRecoveryHostedService> logger)
    : IHostedService
{
    // Khoá của jobId TRONG thân JSON của dòng outbox, suy ra từ CHÍNH bộ tuần tự hoá đã ghi nó
    // (OutboxJson.Options) — không chép tay chuỗi "jobId". Đổi quy ước đặt tên ở một chỗ thì phép dò
    // đi theo; một chuỗi chép tay thì lệch trong im lặng, và lệch ở đây có nghĩa là MỌI việc `queued`
    // bị đánh dấu hỏng oan. JobIdProbeMatchesWrittenPayload (test) khoá đúng cặp này lại.
    private static readonly string JobIdProbePrefix =
        "{\"" + (OutboxJson.Options.PropertyNamingPolicy?.ConvertName(nameof(JobQueuedEvent.JobId))
                 ?? nameof(JobQueuedEvent.JobId)) + "\":\"";

    // Tập ứng viên — internal để test đối chiếu được ĐÚNG tập này, không phải chỉ hệ quả của nó.
    //
    // Bỏ bộ lọc ĐƠN VỊ CÓ NÊU TÊN (luật B6/M5): lúc khởi động chưa có request nên chưa có đơn vị nào
    // để lọc theo — không bỏ thì bộ lọc thành `tenant_id = NULL`, EF gấp cả câu về `WHERE FALSE` và
    // phép khôi phục im lặng không làm gì. Lời gọi ở NGOÀI bỏ luôn bộ lọc của subquery outbox bên
    // trong, đúng điều cần: cả hai bảng đều quét toàn hệ. Bộ lọc XOÁ MỀM thì GIỮ — nó thay cho mệnh
    // đề `is_deleted = false` của bản SQL thô trước đây (ADR-0071 phương án C).
    //
    // Phép dò trên thân JSON dùng toán tử chứa `@>` của jsonb (`payload @> '{"jobId":"…"}'::jsonb`)
    // thay cho `payload ->> 'jobId' = j.id::text`: hai câu cùng nghĩa với thân JSON mà dòng outbox
    // thật sự mang (một object, jobId là chuỗi uuid chữ thường — cùng dạng `j.id::text` sinh ra), và
    // `@>` là hình dạng LINQ dịch được. Cấu trúc câu giữ nguyên, kể cả NOT EXISTS lồng trong OR.
    internal static IQueryable<RecoveryCandidate> Candidates(CoreDbContext db)
        => db.Jobs
            .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
            .Where(j => j.Status == JobStatus.Running
                || (j.Status == JobStatus.Queued
                    && !db.OutboxMessages.Any(o =>
                        o.Status == OutboxStatus.Pending
                        && o.EventType == JobQueuedEvent.TypeKey
                        && EF.Functions.JsonContains(o.Payload, JobIdProbePrefix + j.Id.ToString() + "\"}"))))
            .Select(j => new RecoveryCandidate(j.Id, j.TenantId, j.CreatedByUserId));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Phép kiểm schema ở khởi động (luật E8, DatabaseSchemaStartupCheck) từ chối khởi động khi database không dùng được;
        // việc dọn dẹp này không được tự nó làm tiến trình không lên khi lỗi chỉ ở phép quét, nhưng cũng không được im lặng.
        try
        {
            await RecoverAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Lọc theo TOKEN, không theo kiểu: TaskCanceledException vì hết thời gian chờ khi việc khởi động CHƯA bị huỷ vẫn
            // là "database chưa sẵn" — để nó thoát ra là tiến trình không lên.
            CoreMetrics.BackgroundJobFailed.Add(1);
            logger.LogError(
                "Không đánh dấu được việc dở dang lúc khởi động: {Failure} — việc `running`/`queued` cũ có thể treo cho tới lần khởi động sau.",
                FailureText.DescribeWithStack(ex));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal async Task RecoverAsync(CancellationToken ct)
    {
        List<RecoveryCandidate> candidates;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            candidates = await Candidates(db).ToListAsync(ct);
        }

        var marked = 0;
        foreach (var candidate in candidates)
        {
            // Mỗi việc một phạm vi ngữ cảnh + một giao dịch + một try: một việc hỏng không kéo việc khác. Lọc theo TOKEN
            // như StartAsync — khởi động bị huỷ thì thoát ra, không nuốt.
            try
            {
                using var scope = scopeFactory.CreateScope();
                using var context = executionContextScope.Enter(candidate.TenantId, candidate.CreatedByUserId, null);

                var jobs = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var outcome = await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
                {
                    var job = await jobs.FindByIdAsync(candidate.Id, innerCt);
                    if (job is null)
                        return new TransactionOutcome<RecoveryResult>(RecoveryResult.NotFound, ShouldCommit: false);

                    var failed = job.Fail(timeProvider.GetUtcNow(), JobJson.ErrorOf(JobErrors.Interrupted));
                    return failed.IsSuccess
                        ? new TransactionOutcome<RecoveryResult>(RecoveryResult.Marked, ShouldCommit: true)
                        : new TransactionOutcome<RecoveryResult>(RecoveryResult.Rejected, ShouldCommit: false);
                }, ct);

                switch (outcome)
                {
                    case RecoveryResult.Marked:
                        marked++;
                        logger.LogWarning("Việc {JobId} bị gián đoạn vì tiến trình dừng giữa chừng — đã đánh dấu failed.", candidate.Id);
                        break;
                    case RecoveryResult.NotFound:
                        logger.LogWarning("Việc {JobId} là ứng viên khôi phục nhưng không còn trong kho — không có gì để đánh dấu.", candidate.Id);
                        break;
                    default:
                        logger.LogWarning("Việc {JobId} là ứng viên khôi phục nhưng không ở trạng thái cho phép đánh dấu hỏng — giữ nguyên.", candidate.Id);
                        break;
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                CoreMetrics.BackgroundJobFailed.Add(1);
                logger.LogError(
                    "Không đánh dấu được việc {JobId} lúc khởi động: {Failure} — làm tiếp các việc còn lại.",
                    candidate.Id,
                    FailureText.DescribeWithStack(ex));
            }
        }

        // Luôn ghi, kể cả 0 — việc nền log số bản ghi xử lý (docs/wiki-core/be/07-observability.md §6).
        if (marked > 0)
        {
            logger.LogWarning(
                "Đã đánh dấu {Marked} việc dở dang lúc khởi động, {NotMarked} việc ứng viên còn lại không đánh dấu được.",
                marked,
                candidates.Count - marked);
        }
        else
        {
            logger.LogInformation(
                "Đã đánh dấu {Marked} việc dở dang lúc khởi động, {NotMarked} việc ứng viên còn lại không đánh dấu được.",
                marked,
                candidates.Count - marked);
        }
    }

    private enum RecoveryResult { Marked, NotFound, Rejected }
}

// Ba cột phép khôi phục cần, không phải cả entity: nó KHÔNG sửa Job ở đây mà mở lại phạm vi đơn vị
// rồi nạp lại việc trong phạm vi đó.
internal sealed record RecoveryCandidate(Guid Id, Guid TenantId, Guid CreatedByUserId);
