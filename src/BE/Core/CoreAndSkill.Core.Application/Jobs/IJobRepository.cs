using CoreAndSkill.Core.Domain.Jobs;

namespace CoreAndSkill.Core.Application.Jobs;

// Repository của Job. AddAsync/FindByIdAsync KHÔNG SaveChanges (giao dịch do chỗ gọi quyết). Hai thao
// tác cuối là câu UPDATE có điều kiện chạy NGAY, không qua ChangeTracker: một việc dài (nhập tệp) xoá
// bộ theo dõi sau mỗi dòng, nên trạng thái việc không được sống trong đó.
public interface IJobRepository
{
    Task AddAsync(Job job, CancellationToken ct);

    // Có theo dõi thay đổi — chỗ gọi mutate rồi lưu.
    Task<Job?> FindByIdAsync(Guid id, CancellationToken ct);

    // Không theo dõi — để đọc.
    Task<Job?> FindForReadAsync(Guid id, CancellationToken ct);

    // queued -> running bằng MỘT câu UPDATE có điều kiện `status = 'queued'`: hai bộ chạy cùng nhặt một
    // việc thì chỉ một bên trả true. Đọc-rồi-ghi bằng hai câu thì cả hai cùng thắng.
    Task<bool> TryClaimAsync(Guid id, DateTimeOffset now, CancellationToken ct);

    Task UpdateProgressAsync(Guid id, int progress, CancellationToken ct);
}
