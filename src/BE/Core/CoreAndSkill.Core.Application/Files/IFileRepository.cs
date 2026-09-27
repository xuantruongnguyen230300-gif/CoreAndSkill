using CoreAndSkill.Core.Domain.Files;

namespace CoreAndSkill.Core.Application.Files;

// Repository của StoredFile — docs/quy-uoc/be-architecture.md §7 bước 5. KHÔNG SaveChanges: giao dịch
// do TransactionBehavior (hoặc IUnitOfWork ở chỗ ngoài request) quyết.
public interface IFileRepository
{
    Task AddAsync(StoredFile file, CancellationToken ct);

    // Đã áp bộ lọc đơn vị và xoá mềm: tệp của đơn vị khác hay đã gỡ đều là "không có".
    Task<StoredFile?> FindByIdAsync(Guid id, CancellationToken ct);
}
