using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Jobs;

// Ngữ cảnh một lần chạy. Input là tham chiếu tới đầu vào của việc (ví dụ khoá tệp tạm) đi theo dòng
// outbox — core.job không có cột đầu vào. Executor báo tiến độ qua ReportProgressAsync (0-100).
public sealed record JobExecutionContext(
    Guid JobId,
    string JobType,
    string? Input,
    Func<int, Task> ReportProgressAsync);

// Kết quả một lần chạy. Success mang result (jsonb, hình dạng theo loại việc) và tệp kết quả nếu có;
// Failure mang Error — việc chuyển `failed` và Error đi vào cột `error` (contracts/jobs.md §1).
// "Nhập một phần" vẫn là Success: dòng hỏng nằm ở result.failed, việc chỉ `failed` khi không đọc được tệp.
public sealed record JobOutcome(string? ResultJson, Guid? ResultFileId, Error? Error)
{
    public bool IsSuccess => Error is null;

    public static JobOutcome Success(string? resultJson, Guid? resultFileId = null) => new(resultJson, resultFileId, null);

    public static JobOutcome Failure(Error error) => new(null, null, error);
}

// Bộ chạy một loại việc. Một executor có thể phụ trách NHIỀU loại (bộ chạy nhập tệp phụ trách mọi
// loại mà một IImportDefinition đã khai) nên chọn bằng Handles, không bằng một hằng số.
//
// Executor chạy trong một DI scope MỚI với ngữ cảnh đơn vị/người kích hoạt đã mở lại từ dòng outbox;
// không có HttpContext. Ngoại lệ không lường trước KHÔNG được nuốt: JobRunner log Error, tăng chỉ số
// core.job.failed và đánh dấu việc `failed`.
public interface IJobExecutor
{
    bool Handles(string jobType);

    Task<JobOutcome> ExecuteAsync(JobExecutionContext context, CancellationToken ct);
}
