using CoreAndSkill.Core.Application.Menu;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Menu;

// Hiện thực IMenuQueryService — docs/database/schema-core.md §6.3, docs/contracts/meta-menu.md §1.
internal sealed class MenuQueryService(
    CoreDbContext db, IPermissionChecker permissionChecker, IEnumerable<IPermissionCatalogSource> catalogSources)
    : IMenuQueryService
{
    public async Task<IReadOnlyList<MenuItemDto>> GetVisibleMenuAsync(Guid userId, CancellationToken ct)
    {
        // §1.6 — "lọc lúc chạy": module đang lắp = tập ModuleKey của các nguồn đã đăng ký.
        var installedModuleKeys = catalogSources
            .SelectMany(s => s.GetResources())
            .Select(r => r.ModuleKey)
            .Where(key => key is not null)
            .ToHashSet(StringComparer.Ordinal);

        var allItems = await db.MenuItems
            .Where(m => m.ModuleKey == null || installedModuleKeys.Contains(m.ModuleKey))
            .Select(m => new
            {
                m.Id,
                m.ParentId,
                m.Code,
                m.LabelKey,
                m.Icon,
                m.Route,
                m.DisplayOrder,
                m.RequiredPermissionId,
            })
            .ToListAsync(ct);

        if (allItems.Count == 0)
            return [];

        var requiredPermissionIds = allItems
            .Where(m => m.RequiredPermissionId is not null)
            .Select(m => m.RequiredPermissionId!.Value)
            .Distinct()
            .ToList();

        var permissionCodeById = requiredPermissionIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Permissions
                .Where(p => requiredPermissionIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Code, ct);

        var effectivePermissions = await permissionChecker.GetEffectivePermissionsAsync(userId, ct);

        var userRoleIds = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);
        var userRoleIdSet = userRoleIds.ToHashSet();

        var menuItemIds = allItems.Select(m => m.Id).ToList();
        var roleGateRows = await db.MenuItemRoles
            .Where(mir => menuItemIds.Contains(mir.MenuItemId))
            .Select(mir => new { mir.MenuItemId, mir.RoleId })
            .ToListAsync(ct);
        var roleGatesByMenuItem = roleGateRows
            .GroupBy(r => r.MenuItemId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.RoleId).ToHashSet());

        var shownIds = new HashSet<Guid>();

        foreach (var item in allItems)
        {
            bool shown;

            if (item.RequiredPermissionId is { } requiredPermissionId)
            {
                // Bước 1 — thắng tuyệt đối, bỏ qua hoàn toàn menu_item_role — docs/database/schema-core.md §6.3.
                shown = permissionCodeById.TryGetValue(requiredPermissionId, out var code)
                    && effectivePermissions.Contains(code);
            }
            else if (roleGatesByMenuItem.TryGetValue(item.Id, out var gatedRoles) && gatedRoles.Count > 0)
            {
                shown = gatedRoles.Overlaps(userRoleIdSet);
            }
            else
            {
                shown = true; // mọi người dùng đã đăng nhập
            }

            if (shown)
                shownIds.Add(item.Id);
        }

        // §1.5 — mục cha luôn được kéo vào khi có bất kỳ con nào hiện, kể cả khi cha bị §1.4 loại.
        //
        // MỘT lượt duyệt là đủ, và chỉ đủ vì cây đúng MỘT cấp: cha của một mục con không bao giờ có cha nữa, nên không
        // có gì để lan tiếp. Bất biến đó do TenantSeedValidator.ValidateMenuItems ép (schema-core.md §6.1
        // luật 1) chứ không do vòng lặp này — mất nó thì mục ông bà rơi khỏi response, FE dựng cây từ danh sách phẳng
        // bỏ rơi cả nhánh, và người có quyền không thấy menu mà không lỗi nào bắn ra.
        var byId = allItems.ToDictionary(m => m.Id);
        foreach (var item in allItems)
        {
            if (shownIds.Contains(item.Id) && item.ParentId is { } parentId && byId.ContainsKey(parentId))
                shownIds.Add(parentId);
        }

        // Thứ tự TOÀN PHẦN, không phụ thuộc thứ tự database trả về (câu nạp trên không có ORDER BY, và không cần có:
        // ba tiêu chí dưới đây đã phân xử mọi cặp). Code là tiêu chí phụ ổn định vì nó duy nhất trong một đơn vị —
        // ux_menu_item_tenant_code_active. Thiếu nó, hai mục cùng display_order đổi chỗ giữa hai lần tải.
        return allItems
            .Where(m => shownIds.Contains(m.Id))
            .OrderBy(m => m.ParentId is null ? 0 : 1)
            .ThenBy(m => m.DisplayOrder)
            .ThenBy(m => m.Code, StringComparer.Ordinal)
            .Select(m => new MenuItemDto(m.Id, m.ParentId, m.Code, m.LabelKey, m.Icon, m.Route, m.DisplayOrder))
            .ToList();
    }
}
