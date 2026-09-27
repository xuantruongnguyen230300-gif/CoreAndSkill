namespace CoreAndSkill.Core.Application.Tenants;

// Nguồn seed cho đơn vị mới — định nghĩa gốc, docs/quy-uoc/be-architecture.md §1.1.
// Các mục tham chiếu nhau bằng MÃ (RoleName, ParentCode, RequiredPermissionCode), không bằng id.
// Nhiều đăng ký được gộp; Core tự đăng ký một nguồn cho menu của Core nhưng KHÔNG khai vai trò nào
// (luật S1) — nguồn của Core (CoreTenantSeedSource) trả rỗng cho vai trò/ánh xạ quyền, và menu của
// ba màn quản trị người dùng/vai trò/phân quyền (B2).
public sealed record SeedRole(string Name, bool IsSystem);

public sealed record SeedRolePermission(string RoleName, string PermissionCode);

public sealed record SeedMenuItem(
    string Code,
    string LabelKey,
    string? Icon,
    string? Route,
    string? ParentCode,
    int DisplayOrder,
    string? RequiredPermissionCode,
    string? ModuleKey);

public interface ITenantSeedSource
{
    IReadOnlyCollection<SeedRole> GetRoles();
    IReadOnlyCollection<SeedRolePermission> GetRolePermissions();
    IReadOnlyCollection<SeedMenuItem> GetMenuItems();
}
