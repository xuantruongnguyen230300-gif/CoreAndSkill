using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Jobs;

// Việc chạy nền — docs/database/schema-core.md §9.8, docs/contracts/jobs.md. Id chính là jobId trả
// cho FE.
//
// Máy trạng thái: queued -> running -> succeeded | failed. Bước queued -> running KHÔNG đi qua entity
// mà qua một câu UPDATE có điều kiện (IJobRepository.TryClaimAsync) — hai bộ chạy cùng nhặt một việc
// thì chỉ MỘT bên thắng, đọc-rồi-ghi bằng hai câu thì cả hai cùng thắng. Entity chỉ ghi nhận hai bước
// còn lại, và mỗi bước phát một sự kiện để outbox chuyển thành thông báo.
public sealed class Job : BaseEntity, ITenantScoped, IHasDomainEvents
{
    private readonly List<IDomainEvent> _events = [];

    public Guid TenantId { get; init; }

    public string Type { get; private set; } = string.Empty;
    public string Status { get; private set; } = JobStatus.Queued;
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public short Progress { get; private set; }

    // jsonb — hình dạng theo Type; chỉ có khi Succeeded.
    public string? ResultJson { get; private set; }

    // jsonb — { code, message, messageParams }; chỉ có khi Failed.
    public string? ErrorJson { get; private set; }

    public Guid? ResultFileId { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events;

    private Job()
    {
        // EF Core cần ctor không tham số.
    }

    public static Result<Job> Create(string type, Guid createdByUserId, string? input)
    {
        var job = new Job
        {
            Type = type.Trim(),
            CreatedByUserId = createdByUserId,
        };

        job._events.Add(new JobQueuedEvent(job.Id, job.Type, input));
        return Result.Success(job);
    }

    // Việc phải đang chạy — nó vừa được TryClaimAsync nhặt.
    public Result Complete(DateTimeOffset now, string? resultJson, Guid? resultFileId)
    {
        if (Status != JobStatus.Running)
            return Result.Failure(JobStateErrors.InvalidTransition.WithParams(("From", Status), ("To", JobStatus.Succeeded)));

        Status = JobStatus.Succeeded;
        FinishedAt = now;
        Progress = 100;
        ResultJson = resultJson;
        ResultFileId = resultFileId;
        _events.Add(new JobFinishedEvent(Id, Type, Status, CreatedByUserId));
        return Result.Success();
    }

    // Chấp nhận cả queued: khởi động lại tiến trình làm mất hàng đợi trong bộ nhớ, và một việc còn
    // queued khi đó không bao giờ được nhặt nữa (JobRecoveryHostedService).
    public Result Fail(DateTimeOffset now, string errorJson)
    {
        if (Status is not (JobStatus.Running or JobStatus.Queued))
            return Result.Failure(JobStateErrors.InvalidTransition.WithParams(("From", Status), ("To", JobStatus.Failed)));

        Status = JobStatus.Failed;
        FinishedAt = now;
        ErrorJson = errorJson;
        _events.Add(new JobFinishedEvent(Id, Type, Status, CreatedByUserId));
        return Result.Success();
    }

    public void ClearDomainEvents() => _events.Clear();
}
