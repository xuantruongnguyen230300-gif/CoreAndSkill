namespace CoreAndSkill.Core.Application.Permissions;

// Hằng số CHUỖI của khoá quyền do chính Core khai — docs/quy-uoc/be-api-controller.md §4.3.
// Mỗi hằng số ở đây PHẢI khớp đúng chuỗi một dòng core.permission.code — danh mục đầy đủ, nguồn
// duy nhất: docs/database/schema-core.md §5.2. File này không phải bản sao — nó là hiện thực C#
// của đúng danh mục đó. Cổng đối chiếu hai chiều với dòng seed trong migration (luật B7):
// src/BE/Tests/CoreAndSkill.ArchTests/PermissionSeedParityTests.cs.
public static class CoreResourceKeys
{
    public const string User = "core.user";
    public const string UserRole = "core.user.role";
    public const string Role = "core.role";
    public const string Permission = "core.permission";
    public const string Menu = "core.menu";
}

public static class CorePermissions
{
    public const string UserRead = "core.user.read";
    public const string UserWrite = "core.user.write";
    public const string UserLock = "core.user.lock";
    public const string UserResetPassword = "core.user.reset-password";
    public const string UserExport = "core.user.export";
    public const string UserRoleAssign = "core.user.role.assign";
    public const string RoleRead = "core.role.read";
    public const string RoleWrite = "core.role.write";
    public const string PermissionRead = "core.permission.read";
    public const string PermissionWrite = "core.permission.write";
    public const string MenuRead = "core.menu.read";
    public const string MenuWrite = "core.menu.write";
}
