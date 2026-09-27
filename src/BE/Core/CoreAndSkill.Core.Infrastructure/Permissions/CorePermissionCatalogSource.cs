using CoreAndSkill.Core.Application.Permissions;

namespace CoreAndSkill.Core.Infrastructure.Permissions;

// Danh mục khoá quyền của CHÍNH Core — nguồn C# đối chiếu hai chiều với dòng seed trong migration
// (luật B7, cổng: src/BE/Tests/CoreAndSkill.ArchTests/PermissionSeedParityTests.cs). Danh mục đầy
// đủ, nguồn duy nhất: docs/database/schema-core.md §5.2. File này KHÔNG phải bản sao — nó là hiện
// thực C# của đúng danh mục đó.
internal sealed class CorePermissionCatalogSource : IPermissionCatalogSource
{
    public IReadOnlyCollection<PermissionResourceDefinition> GetResources() =>
    [
        new(CoreResourceKeys.User, "resource.core.user", ModuleKey: null, DisplayOrder: 10),
        new(CoreResourceKeys.UserRole, "resource.core.user.role", ModuleKey: null, DisplayOrder: 20),
        new(CoreResourceKeys.Role, "resource.core.role", ModuleKey: null, DisplayOrder: 30),
        new(CoreResourceKeys.Permission, "resource.core.permission", ModuleKey: null, DisplayOrder: 40),
        new(CoreResourceKeys.Menu, "resource.core.menu", ModuleKey: null, DisplayOrder: 50),
    ];

    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(CorePermissions.UserRead, CoreResourceKeys.User, "read", "permission.core.user.read", 10),
        new(CorePermissions.UserWrite, CoreResourceKeys.User, "write", "permission.core.user.write", 20),
        new(CorePermissions.UserLock, CoreResourceKeys.User, "lock", "permission.core.user.lock", 30),
        new(CorePermissions.UserResetPassword, CoreResourceKeys.User, "reset-password", "permission.core.user.reset-password", 40),
        new(CorePermissions.UserExport, CoreResourceKeys.User, "export", "permission.core.user.export", 50),
        new(CorePermissions.UserRoleAssign, CoreResourceKeys.UserRole, "assign", "permission.core.user.role.assign", 10),
        new(CorePermissions.RoleRead, CoreResourceKeys.Role, "read", "permission.core.role.read", 10),
        new(CorePermissions.RoleWrite, CoreResourceKeys.Role, "write", "permission.core.role.write", 20),
        new(CorePermissions.PermissionRead, CoreResourceKeys.Permission, "read", "permission.core.permission.read", 10),
        new(CorePermissions.PermissionWrite, CoreResourceKeys.Permission, "write", "permission.core.permission.write", 20),
        new(CorePermissions.MenuRead, CoreResourceKeys.Menu, "read", "permission.core.menu.read", 10),
        new(CorePermissions.MenuWrite, CoreResourceKeys.Menu, "write", "permission.core.menu.write", 20),
    ];
}
