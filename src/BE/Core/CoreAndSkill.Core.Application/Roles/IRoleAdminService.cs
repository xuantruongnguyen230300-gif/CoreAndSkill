using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Roles;

// Hành động quản trị vai trò — docs/contracts/roles.md §2, §3, §4. Giữ AppRole/RoleManager không
// rời Core.Infrastructure. Từng thao tác trả Result đã mang đúng ErrorType/NotFound/SystemImmutable
// /InUse — luật nghiệp vụ của roles.md không cần dữ liệu chéo (khác users.md), nên gói gọn ở đây.
public interface IRoleAdminService
{
    Task<Result<Guid>> CreateAsync(string name, CancellationToken ct);

    // version: token nhận từ GET gần nhất (concurrency_stamp) — 06-concurrency-control.md §6.3. Thiếu hoặc lệch ⇒
    // CORE.CONCURRENCY.CONFLICT, không ghi gì.
    Task<Result> RenameAsync(Guid roleId, string name, string? version, CancellationToken ct);

    Task<Result> DeleteAsync(Guid roleId, CancellationToken ct);
}
