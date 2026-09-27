namespace CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;

// Mã hành động của nhật ký kiểm toán — khuôn "<tài nguyên>.<hành động>", docs/database/schema-core.md
// §9.4, docs/wiki-core/be/10-data-retention.md §5.2. KHÔNG phải catalog Error (không đi ra HTTP,
// không cần ErrorType) — khai một chỗ để AuditLogInterceptor và ITenantProvisioningService dùng
// chung, tránh chuỗi literal rải rác.
internal static class AuditActionCodes
{
    public const string UserCreate = "core.user.create";
    public const string UserLock = "core.user.lock";
    public const string UserUnlock = "core.user.unlock";
    public const string UserPasswordChange = "core.user.password_change";

    // Gán / gỡ vai trò của người dùng — docs/adr/0083-doi-vai-tro-nguoi-dung-luon-vao-nhat-ky-kiem-toan.md. Ba đoạn như mọi
    // mã audit; KHÔNG trùng khoá quyền core.user.role.assign (bốn đoạn).
    public const string UserRoleAssign = "core.user.role_assign";

    public const string RoleCreate = "core.role.create";
    public const string RoleRename = "core.role.rename";
    public const string RoleDelete = "core.role.delete";

    public const string PermissionMatrixUpdate = "core.permission.matrix_update";

    public const string TenantCreate = "core.tenant.create";
    public const string TenantActivate = "core.tenant.activate";
    public const string TenantDeactivate = "core.tenant.deactivate";
    public const string TenantAdminRecoveryReset = "core.tenant.admin_recovery_reset";
    public const string TenantAdminCreate = "core.tenant.admin_create";
}
