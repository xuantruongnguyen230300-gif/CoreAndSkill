using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Permissions;

// Ô của ma trận: có dòng = được cấp; không có dòng = bị từ chối (deny-by-default).
// docs/database/schema-core.md §5.3. Mang TenantId — vai trò và ánh xạ quyền thuộc về một đơn vị.
// B1 chỉ dựng bảng; ghi ánh xạ đầu tiên qua ITenantSeedSource thuộc B2 khi module/dự án khai vai trò.
public sealed class RolePermission : BaseEntity, ITenantScoped
{
    // init, không private set — cùng khuôn Id (§1.1) và năm kiểu join của Identity (§5.1):
    // TenantInterceptor gán qua entry.Property(...).CurrentValue, init vẫn set được bằng đường đó.
    public Guid TenantId { get; init; }
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission()
    {
        // EF Core cần ctor không tham số.
    }

    public static Result<RolePermission> Create(Guid roleId, Guid permissionId)
        => Result.Success(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
        });
}
