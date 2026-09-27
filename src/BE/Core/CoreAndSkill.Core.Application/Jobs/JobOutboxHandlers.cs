using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Jobs;

namespace CoreAndSkill.Core.Application.Jobs;

// Bên nhận JobQueuedEvent: đẩy việc vào hàng đợi chạy nền SAU khi giao dịch của request đã commit —
// handler khởi tạo việc KHÔNG gọi IBackgroundJobScheduler (be-cqrs-handler.md §10.3 ràng buộc 3). Hàng
// đợi nằm trong bộ nhớ nên lời đẩy này KHÔNG bền qua khởi động lại; JobRecoveryHostedService lo phần
// việc dở dang.
internal sealed class JobQueuedOutboxHandler(IBackgroundJobScheduler scheduler) : IOutboxEventHandler
{
    public string EventType => JobQueuedEvent.TypeKey;

    public async Task<Result> HandleAsync(OutboxEnvelope envelope, CancellationToken ct)
    {
        var payload = envelope.ReadPayload<JobQueuedEvent>();
        if (payload is null || payload.JobId == Guid.Empty)
            return Result.Failure(OutboxHandlingErrors.PayloadInvalid.WithParams(("EventType", envelope.EventType)));

        var jobId = payload.JobId;
        var input = payload.Input;
        await scheduler.EnqueueAsync<JobRunner>((runner, token) => runner.RunAsync(jobId, input, token), ct);

        return Result.Success();
    }
}

// Mã thông báo do việc nền kết thúc sinh ra — khoá dịch, không phải câu chữ.
public static class JobNotificationCodes
{
    public const string Succeeded = "core.job.succeeded";
    public const string Failed = "core.job.failed";
}

// Bên nhận JobFinishedEvent: thông báo tới NGƯỜI KHỞI TẠO việc, để người dùng rời màn vẫn biết việc đã
// xong (contracts/jobs.md §1 Ghi chú). Đây là một trong những thứ đăng ký nghe integration event —
// nghiệp vụ không biết kênh gửi (12-notifications.md §1.1).
internal sealed class JobFinishedOutboxHandler(INotificationPublisher publisher) : IOutboxEventHandler
{
    public string EventType => JobFinishedEvent.TypeKey;

    public async Task<Result> HandleAsync(OutboxEnvelope envelope, CancellationToken ct)
    {
        var payload = envelope.ReadPayload<JobFinishedEvent>();
        if (payload is null || payload.JobId == Guid.Empty || payload.CreatedByUserId == Guid.Empty)
            return Result.Failure(OutboxHandlingErrors.PayloadInvalid.WithParams(("EventType", envelope.EventType)));

        var code = payload.Status == JobStatus.Succeeded ? JobNotificationCodes.Succeeded : JobNotificationCodes.Failed;

        var draft = new NotificationDraft(
            code,
            new Dictionary<string, string>
            {
                ["JobId"] = payload.JobId.ToString(),
                ["JobType"] = payload.JobType,
            },
            [payload.CreatedByUserId]);

        return await publisher.PublishAsync(draft, ct);
    }
}
