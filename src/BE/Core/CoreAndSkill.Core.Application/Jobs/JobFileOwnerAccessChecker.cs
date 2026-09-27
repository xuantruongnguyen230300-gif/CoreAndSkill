using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Files;

namespace CoreAndSkill.Core.Application.Jobs;

// Checker của bảng `core.job` — tệp kết quả của việc nền (danh sách dòng nhập lỗi) gắn vào việc đó
// (owner_table = core.job). Quyền của bản ghi chủ = quyền của NGƯỜI KHỞI TẠO việc (contracts/jobs.md §1:
// "việc của chính người đã khởi tạo, không có khoá quyền riêng").
internal sealed class JobFileOwnerAccessChecker(IJobRepository jobs, ICurrentUser currentUser) : IFileOwnerAccessChecker
{
    public string OwnerTable => CoreFilePurposes.JobOwnerTable;

    public Task<bool> CanReadAsync(Guid ownerId, CancellationToken ct) => IsInitiatorAsync(ownerId, ct);

    public Task<bool> CanWriteAsync(Guid ownerId, CancellationToken ct) => IsInitiatorAsync(ownerId, ct);

    private async Task<bool> IsInitiatorAsync(Guid jobId, CancellationToken ct)
    {
        if (currentUser.UserId is not { } callerId)
            return false;

        var job = await jobs.FindForReadAsync(jobId, ct);
        return job is not null && job.CreatedByUserId == callerId;
    }
}
