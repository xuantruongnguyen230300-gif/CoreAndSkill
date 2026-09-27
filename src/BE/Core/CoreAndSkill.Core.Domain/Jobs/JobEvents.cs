using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Jobs;

// Việc vừa được xếp hàng. Bộ phát outbox nhận sự kiện này và đẩy việc vào hàng đợi chạy nền SAU khi
// giao dịch của request đã commit — handler KHÔNG gọi thẳng IBackgroundJobScheduler
// (docs/quy-uoc/be-cqrs-handler.md §10.3, ràng buộc 3).
//
// Input là tham chiếu KHÔNG trong suốt tới đầu vào của việc (ví dụ khoá tệp tạm): core.job không có
// cột đầu vào, nên nó đi theo dòng outbox tới người chạy. Không chứa dữ liệu người dùng.
public sealed record JobQueuedEvent(Guid JobId, string JobType, string? Input) : IDomainEvent
{
    public const string TypeKey = "core.job.queued.v1";

    string IDomainEvent.EventType => TypeKey;
}

// Việc vừa kết thúc (succeeded hoặc failed) — nguồn của thông báo tới người khởi tạo
// (docs/contracts/jobs.md §1, docs/wiki-core/be/12-notifications.md §1.2).
public sealed record JobFinishedEvent(Guid JobId, string JobType, string Status, Guid CreatedByUserId) : IDomainEvent
{
    public const string TypeKey = "core.job.finished.v1";

    string IDomainEvent.EventType => TypeKey;
}
