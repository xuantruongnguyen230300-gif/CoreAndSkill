using CoreAndSkill.Core.Application.Common.Paging;

namespace CoreAndSkill.Core.Application.Roles;

// Seam CHỈ ĐỌC cho vai trò — docs/contracts/roles.md. Vai trò (AppRole) sống ở Core.Infrastructure
// (luật S5); Application chỉ thấy DTO. Hiện thực ở Core.Infrastructure.
// Version = concurrency_stamp của app_role, opaque với client — docs/wiki-core/be/06-concurrency-control.md §6.3.
public sealed record RoleSummaryDto(
    Guid Id, string Name, bool IsSystem, int UserCount, DateTimeOffset? CreatedAt, string Version);

public sealed record RoleSearchCriteria(int Page, int PageSize, string SortBy, bool SortDescending, string? SearchText);

public interface IRoleQueryService
{
    Task<PagedList<RoleSummaryDto>> SearchAsync(RoleSearchCriteria criteria, CancellationToken ct);

    Task<RoleSummaryDto?> FindByIdAsync(Guid id, CancellationToken ct);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    // Id nào trong danh sách là vai trò có thật (trong đơn vị hiện hành) — MỘT truy vấn theo tập, không một câu cho mỗi id.
    Task<IReadOnlySet<Guid>> FindExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken ct);

    // Người dùng có đang mang BẤT KỲ vai trò nào is_system = true không — docs/contracts/users.md
    // §2 Luật 4.
    Task<bool> UserHoldsAnySystemRoleAsync(Guid userId, CancellationToken ct);
}
