namespace CoreAndSkill.Core.Application.Common.Interfaces;

// Danh tính và đơn vị khi KHÔNG có request — định nghĩa gốc, docs/quy-uoc/be-architecture.md §1.1.
// Chỉ nơi gọi thuộc allowlist được mở phạm vi: bảng Allowlist A12 ở docs/quy-uoc/be-architecture.md.
// Hiện thực ở Core.Infrastructure, giữ giá trị trong AsyncLocal.
public sealed record ExecutionContextSnapshot(Guid? UserId, string? UserName, Guid? TenantId);

public interface IExecutionContextScope
{
    // Bên trong khối using, ICurrentUser / ITenantContext trả giá trị này; Dispose khôi phục phạm
    // vi trước đó — KHÔNG xoá về rỗng.
    IDisposable Enter(Guid tenantId, Guid? userId, string? userName);

    // Phạm vi đang mở, null khi không có.
    ExecutionContextSnapshot? Current { get; }
}
