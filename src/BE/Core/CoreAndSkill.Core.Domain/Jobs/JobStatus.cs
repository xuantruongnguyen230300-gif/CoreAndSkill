namespace CoreAndSkill.Core.Domain.Jobs;

// Đúng tập giá trị của cột core.job.status — docs/database/schema-core.md §9.8.
public static class JobStatus
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    // Giá trị dành sẵn của cột; v1 không có đường nào đặt nó (docs/contracts/jobs.md §1).
    public const string Cancelled = "cancelled";
}
