using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// TẬP mục menu mà Core seed cho mỗi đơn vị mới — docs/contracts/meta-menu.md §1 (mã, labelKey, icon,
// route, displayOrder) và docs/quy-uoc/fe-routing-guard.md §1 (tuyến nào gác bằng gì).
//
// Vì sao cần pin cả tập chứ không chỉ kiểm nguồn seed có được đăng ký hay không: một mục THIẾU không
// làm cổng nào đỏ. Menu là dữ liệu, nên mất một mục chỉ hiện ra dưới dạng "sidebar không có lối vào
// màn X" — không lỗi biên dịch, không test hỏng, không dòng log nào. Thiếu mục "trang-chu" từng sống
// đúng như vậy: FE có khoá dịch "menu.trang-chu", tuyến /trang-chu có thật, hợp đồng §1 khai mục đó,
// và không gì bắt được việc nguồn seed không gửi nó.
//
// Không chạm database — chỉ đọc nguồn seed trong bộ nhớ. Không [Trait("Category", "RequiresDocker")]
// (luật T9). Kiểu internal của Core.Infrastructure nhìn thấy được nhờ InternalsVisibleTo
// (docs/quy-uoc/be-architecture.md §9.1).
public sealed class CoreTenantSeedMenuTests
{
    private static IReadOnlyCollection<SeedMenuItem> CoreMenu() => new CoreTenantSeedSource().GetMenuItems();

    [Fact]
    public void CoreSeedMenu_MatchesTheContract()
    {
        var actual = CoreMenu()
            .Select(m => (m.Code, m.LabelKey, m.Icon, m.Route, m.ParentCode, m.DisplayOrder, m.RequiredPermissionCode))
            .OrderBy(m => m.ParentCode is null ? 0 : 1)
            .ThenBy(m => m.DisplayOrder)
            .ThenBy(m => m.Code, StringComparer.Ordinal)
            .ToList();

        // Thứ tự dưới đây là thứ tự TOÀN PHẦN của docs/contracts/meta-menu.md §1 — mục gốc trước,
        // rồi displayOrder, rồi code — cùng ba tiêu chí mà MenuQueryService áp lúc trả response.
        actual.ShouldBe(
        [
            ("trang-chu", "menu.trang-chu", "pi-home", "/trang-chu", null, 10, null),
            ("quan-tri", "menu.quan-tri", "pi-cog", null, null, 90, null),
            ("quan-tri-nguoi-dung", "menu.quan-tri-nguoi-dung", "pi-users", "/quan-tri/nguoi-dung",
                "quan-tri", 10, CorePermissions.UserRead),
            ("quan-tri-vai-tro", "menu.quan-tri-vai-tro", "pi-shield", "/quan-tri/vai-tro",
                "quan-tri", 20, CorePermissions.RoleRead),
            ("quan-tri-phan-quyen", "menu.quan-tri-phan-quyen", "pi-key", "/quan-tri/phan-quyen",
                "quan-tri", 30, CorePermissions.PermissionRead),
        ],
        "Tập mục menu Core seed đã lệch khỏi docs/contracts/meta-menu.md §1. Thêm/bớt một mục là đổi "
      + "lối điều hướng của MỌI đơn vị — sửa hợp đồng và spec màn trước, test này sau.");
    }

    // docs/database/schema-core.md §6.3: mục KHÔNG gắn quyền hiện cho mọi người dùng đã đăng nhập.
    // Mọi tuyến dưới /quan-tri gác bằng ma trận quyền (docs/quy-uoc/fe-routing-guard.md §1), nên một
    // mục ở đó mà bỏ trống RequiredPermissionCode là hiện lối vào màn quản trị cho cả những người bị
    // 403 ngay khi bấm — rò thông tin cấu trúc hệ thống, và một liên kết chết.
    [Fact]
    public void CoreSeedMenu_EveryAdminRouteCarriesItsPermission()
    {
        var ungated = CoreMenu()
            .Where(m => m.Route is not null && m.Route.StartsWith("/quan-tri/", StringComparison.Ordinal))
            .Where(m => m.RequiredPermissionCode is null)
            .Select(m => m.Code)
            .ToList();

        ungated.ShouldBeEmpty(
            "Mục menu trỏ vào một màn quản trị mà không khai RequiredPermissionCode — mục đó hiện cho "
          + "MỌI người dùng đã đăng nhập (schema-core.md §6.3 bước 3).");
    }

    // Khu /he-thong gác bằng CỜ is_system_operator, không bằng ma trận quyền
    // (docs/adr/0017-khu-quan-tri-he-thong.md, docs/quy-uoc/fe-routing-guard.md §3.5). Ba bước lọc
    // menu ở docs/database/schema-core.md §6.3 chỉ biết quyền, vai trò, hoặc "mọi người" — KHÔNG có
    // bước nào đọc cờ đó. Nên nguồn seed DÙNG CHUNG của Core (mọi đơn vị nhận) không được có mục nào trỏ vào
    // khu hệ thống: không gắn quyền ⇒ hiện cho mọi người dùng của MỌI đơn vị; gắn một quyền ⇒ dựng đúng đường
    // phân quyền thứ hai mà ADR-0017 cơ chế 2 và luật M9 tồn tại để chặn.
    //
    // Luật M17 (ADR-0122 điểm 4) — HAI vế. Vế 1 (test dưới): nguồn dùng chung không có mục /he-thong. Vế 2 (hai test
    // sau, và SystemTenantMenuDatabaseTests trên PostgreSQL thật): danh sách RIÊNG của đơn vị hệ thống có đúng một mục
    // mang đúng giá trị ADR chốt. Gỡ hay nới một vế để một mục lọt qua là vi phạm — ADR-0079 quyết định 3.
    [Fact]
    public void CoreSeedMenu_HasNoItemPointingIntoTheSystemOperatorArea()
    {
        var leaking = CoreMenu()
            .Where(m => m.Route is not null && m.Route.StartsWith("/he-thong", StringComparison.Ordinal))
            .Select(m => m.Code)
            .ToList();

        leaking.ShouldBeEmpty(
            "Mục menu trỏ vào khu quản trị hệ thống nằm trong nguồn seed DÙNG CHUNG — nó vào MỌI đơn vị. Mô hình lọc "
          + "menu (schema-core.md §6.3) không đọc được cờ is_system_operator. Mục của khu hệ thống chỉ được đi trong "
          + "CoreTenantSeedSource.SystemTenantMenuItems (ADR-0122).");
    }

    [Fact]
    public void SystemTenantMenu_IsExactlyOneItem_WithTheValuesTheAdrFixed()
    {
        var item = CoreTenantSeedSource.SystemTenantMenuItems.ShouldHaveSingleItem();

        (item.Code, item.LabelKey, item.Icon, item.Route, item.ParentCode, item.RequiredPermissionCode, item.ModuleKey)
            .ShouldBe(("he-thong-don-vi", "menu.he-thong-don-vi", "pi-building", "/he-thong/don-vi", null, null, null));
    }

    [Fact]
    public void SystemTenantMenu_StandsAfterEveryRootItemOfTheSharedSource()
    {
        // Thiết kế chốt: đứng CUỐI cây (docs/Design/Screens/00-khung-ung-dung.md, bảng Quyết định bố cục).
        var item = CoreTenantSeedSource.SystemTenantMenuItems.ShouldHaveSingleItem();
        var highestShared = CoreMenu().Where(m => m.ParentCode is null).Max(m => m.DisplayOrder);

        item.DisplayOrder.ShouldBeGreaterThan(highestShared);
    }
}
