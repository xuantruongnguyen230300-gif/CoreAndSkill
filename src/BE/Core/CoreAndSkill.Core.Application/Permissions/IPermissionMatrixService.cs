using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Permissions;

// Seam của màn hình ma trận phân quyền — docs/contracts/permissions.md. Vượt ranh giới một entity
// đơn (đọc/ghi xuyên Permission + PermissionResource + RolePermission + AppRole), nên gói thành một
// service thay vì để handler tự dàn xếp nhiều repository — cùng khuôn ITenantProvisioningService /
// IUserProfileService.
public sealed record PermissionListItemDto(
    Guid Id, string Code, string ResourceKey, string Action, string NameKey, bool IsSystem, string? ModuleKey);

public sealed record PermissionMatrixRoleDto(Guid Id, string Name, bool IsSystem);

public sealed record PermissionMatrixRowDto(
    Guid PermissionId, string Code, string ResourceKey, string ResourceNameKey, string NameKey,
    IReadOnlyList<Guid> GrantedRoleIds);

public sealed record PermissionMatrixDto(
    IReadOnlyList<PermissionMatrixRoleDto> Roles, IReadOnlyList<PermissionMatrixRowDto> Rows, string Version);

public sealed record PermissionMatrixResourceActionDto(Guid PermissionId, string Action, IReadOnlyList<Guid> GrantedRoleIds);

public sealed record PermissionMatrixResourceDto(
    string ResourceKey, string NameKey, string? ModuleKey, IReadOnlyList<PermissionMatrixResourceActionDto> Permissions);

public sealed record PermissionMatrixByResourceDto(
    IReadOnlyList<PermissionMatrixRoleDto> Roles, IReadOnlyList<PermissionMatrixResourceDto> Resources, string Version);

public interface IPermissionMatrixService
{
    Task<IReadOnlyList<PermissionListItemDto>> GetCatalogAsync(CancellationToken ct);

    Task<PermissionMatrixDto> GetMatrixAsync(CancellationToken ct);

    Task<PermissionMatrixByResourceDto> GetMatrixByResourceAsync(CancellationToken ct);

    // entries: permissionId → tập roleId được cấp. Đã qua kiểm "phủ đủ danh mục" và "không trùng"
    // ở HANDLER trước khi gọi — docs/contracts/permissions.md §6 "Ghi chú". Trả version MỚI khi
    // thành công.
    Task<Result<string>> ReplaceMatrixAsync(
        string? expectedVersion, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> entries, CancellationToken ct);
}
