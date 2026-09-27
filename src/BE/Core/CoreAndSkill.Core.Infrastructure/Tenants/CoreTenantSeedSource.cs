using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;

namespace CoreAndSkill.Core.Infrastructure.Tenants;

// Nguồn seed CỦA CHÍNH CORE — docs/quy-uoc/be-architecture.md §1.1 "Nguồn seed cho đơn vị mới".
// Core KHÔNG khai vai trò nào (luật S1) — bộ vai trò mặc định là dữ liệu CỦA DỰ ÁN. Core chỉ khai
// MENU của chính mình (docs/wiki-core/be/trien-khai/03-b2-phan-quyen-va-bien.md §2 bước 4): trang
// chào, và ba màn quản trị người dùng/vai trò/phân quyền gộp dưới một mục cha "quan-tri".
//
// "trang-chu" KHÔNG gắn quyền vì tuyến /trang-chu không gác bằng quyền nào
// (docs/quy-uoc/fe-routing-guard.md §1) — mọi người dùng đã đăng nhập vào được, nên hiện cho mọi
// người dùng đã đăng nhập (docs/database/schema-core.md §6.3 bước 3) là đúng mức, không rò gì.
// Mã, labelKey, icon, route và displayOrder lấy nguyên từ docs/contracts/meta-menu.md §1; FE có
// khoá dịch "menu.trang-chu" và loại chính mục này khỏi khối lối tắt của trang chào
// (docs/Design/Screens/04-trang-chu.md).
//
// Mục cha "quan-tri" KHÔNG gắn quyền — nó vẫn được kéo vào khi có con hiện
// (docs/database/schema-core.md §6.3, docs/contracts/meta-menu.md §1.5), nên để ngỏ không đổi kết
// quả với người có ít nhất một trong ba quyền con; nó chỉ trống với người không có quyền nào —
// chấp nhận được ở v1.
internal sealed class CoreTenantSeedSource : ITenantSeedSource
{
    public IReadOnlyCollection<SeedRole> GetRoles() => [];

    public IReadOnlyCollection<SeedRolePermission> GetRolePermissions() => [];

    public IReadOnlyCollection<SeedMenuItem> GetMenuItems() =>
    [
        new("trang-chu", "menu.trang-chu", "pi-home", "/trang-chu", null, 10, null, null),
        new("quan-tri", "menu.quan-tri", "pi-cog", null, null, 90, null, null),
        new("quan-tri-nguoi-dung", "menu.quan-tri-nguoi-dung", "pi-users", "/quan-tri/nguoi-dung",
            "quan-tri", 10, CorePermissions.UserRead, null),
        new("quan-tri-vai-tro", "menu.quan-tri-vai-tro", "pi-shield", "/quan-tri/vai-tro",
            "quan-tri", 20, CorePermissions.RoleRead, null),
        new("quan-tri-phan-quyen", "menu.quan-tri-phan-quyen", "pi-key", "/quan-tri/phan-quyen",
            "quan-tri", 30, CorePermissions.PermissionRead, null),
    ];
}
