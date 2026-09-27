using System.Linq.Expressions;
using System.Threading.Channels;
using CoreAndSkill.Core.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAndSkill.Core.Infrastructure.Jobs;

// Hiện thực IBackgroundJobScheduler bằng BackgroundService của .NET — KHÔNG thư viện (Hangfire,
// Quartz…), chốt ở docs/wiki-core/be/18-trien-khai-va-van-hanh.md §7 (2026-09-15). Hàng đợi trong
// bộ nhớ tiến trình (System.Threading.Channels) — KHÔNG bền qua khởi động lại; v1 chạy một instance
// nên chấp nhận được, cùng đánh đổi với ADR-0014. BackgroundJobQueueHostedService là phía tiêu thụ.
//
// Lời gọi thật: bên nhận outbox của JobQueuedEvent (JobQueuedOutboxHandler) gọi EnqueueAsync để đẩy
// JobRunner vào hàng đợi sau commit (docs/quy-uoc/be-cqrs-handler.md §10.3). Việc định kỳ (dọn dữ liệu
// quá hạn, bảo trì tệp, bộ phát outbox) là BackgroundService RIÊNG, không đi qua seam này — seam chỉ
// cho việc CHẠY MỘT LẦN theo yêu cầu (docs/adr/0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md).
internal sealed class BackgroundJobScheduler(Channel<QueuedJob> channel, IExecutionContextScope executionContextScope)
    : IBackgroundJobScheduler
{
    public Task<string> EnqueueAsync<TJob>(Expression<Func<TJob, CancellationToken, Task>> methodCall, CancellationToken ct = default)
        => ScheduleInternalAsync(methodCall, TimeSpan.Zero, ct);

    public Task<string> ScheduleAsync<TJob>(
        Expression<Func<TJob, CancellationToken, Task>> methodCall, TimeSpan delay, CancellationToken ct = default)
        => ScheduleInternalAsync(methodCall, delay, ct);

    private async Task<string> ScheduleInternalAsync<TJob>(
        Expression<Func<TJob, CancellationToken, Task>> methodCall, TimeSpan delay, CancellationToken ct)
    {
        // Đơn vị và người kích hoạt đi theo việc nền bằng DỮ LIỆU, không bằng trí nhớ của chỗ gọi —
        // chụp NGAY tại lúc enqueue, khôi phục lúc job THẬT SỰ chạy (docs/quy-uoc/be-architecture.md
        // §1.1, mục "Danh tính và đơn vị khi không có request"). Không chụp vai trò/quyền.
        //
        // Không có phạm vi ngữ cảnh đang mở ⇒ TỪ CHỐI, không suy đơn vị từ ITenantContext/ICurrentUser. Lời gọi hợp lệ
        // duy nhất là bên nhận outbox (JobQueuedOutboxHandler), và bộ phát outbox LUÔN mở phạm vi của dòng outbox trước
        // khi gọi nó — dòng outbox luôn có tenant_id, còn người kích hoạt được phép null (việc hệ thống), nên từ chối ở đây
        // không chặn việc hệ thống nào. Thiếu phạm vi nghĩa là scheduler bị gọi thẳng từ một request, tức vi phạm
        // docs/quy-uoc/be-cqrs-handler.md §10.3 ràng buộc 3: trong request đơn vị đến từ claim nên Current là null, và
        // việc chụp null chạy KHÔNG đơn vị — đọc trả rỗng "thành công", ghi ném. Lấy đơn vị từ claim để cứu lời gọi đó
        // chỉ che vi phạm: việc vẫn được đẩy vào hàng đợi TRƯỚC commit, không bền, và mất khi request rollback.
        // Lỗi LẬP TRÌNH, không lỗi người dùng — ném, cùng cách EnsureInsideTransaction của TenantProvisioningService.
        var snapshot = executionContextScope.Current;
        if (snapshot is not { TenantId: not null })
            throw new InvalidOperationException(
                $"IBackgroundJobScheduler bị gọi ngoài phạm vi ngữ cảnh thực thi (việc {typeof(TJob).Name}): không có đơn vị để " +
                "chạy việc nền. Handler không gọi scheduler — nó ghi dòng outbox, bộ phát outbox gọi scheduler sau commit trong " +
                "phạm vi của dòng đó (docs/quy-uoc/be-cqrs-handler.md §10.3 ràng buộc 3).");

        var compiled = methodCall.Compile();
        var jobId = Guid.NewGuid().ToString("N");

        var queued = new QueuedJob(jobId, async (sp, runCt) =>
        {
            // GetRequiredService<TJob>() đòi TJob : notnull — interface KHÔNG khai ràng buộc đó
            // (chữ ký gốc ở be-cqrs-handler.md §10.2), nên dùng overload không-generic rồi ép kiểu.
            var instance = (TJob)sp.GetRequiredService(typeof(TJob));
            await compiled(instance, runCt);
        }, snapshot);

        if (delay <= TimeSpan.Zero)
        {
            await channel.Writer.WriteAsync(queued, ct);
        }
        else
        {
            // Best-effort — hẹn giờ trong bộ nhớ tiến trình, không sống sót qua restart (cùng đánh
            // đổi với toàn bộ hàng đợi này). CancellationToken.None: không huỷ hẹn giờ theo request
            // đã kết thúc từ lâu trước khi delay trôi qua.
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, ct);
                    await channel.Writer.WriteAsync(queued, CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    // Request bị huỷ trước khi delay trôi qua — không enqueue.
                }
            }, CancellationToken.None);
        }

        return jobId;
    }
}
