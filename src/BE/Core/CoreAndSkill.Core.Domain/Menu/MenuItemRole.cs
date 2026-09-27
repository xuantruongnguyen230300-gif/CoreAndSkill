using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Menu;

// Ghim một mục menu cho danh sách vai trò cụ thể — docs/database/schema-core.md §6.2.
public sealed class MenuItemRole : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; init; }

    public Guid MenuItemId { get; private set; }
    public Guid RoleId { get; private set; }

    private MenuItemRole()
    {
        // EF Core cần ctor không tham số.
    }

    public static Result<MenuItemRole> Create(Guid menuItemId, Guid roleId)
        => Result.Success(new MenuItemRole
        {
            MenuItemId = menuItemId,
            RoleId = roleId,
        });
}
