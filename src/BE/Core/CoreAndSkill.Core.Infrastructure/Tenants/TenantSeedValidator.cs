using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Menu;
using Microsoft.AspNetCore.Identity;

namespace CoreAndSkill.Core.Infrastructure.Tenants;

// Phép kiểm nguồn seed ĐÃ GỘP — docs/quy-uoc/be-architecture.md §1.1 "Nguồn seed cho đơn vị mới". MỘT hiện thực, HAI
// chỗ gọi, vì có HAI đường tới được bước dựng đơn vị:
//   - TenantSeedValidationHostedService — lúc khởi động tiến trình phục vụ: nguồn sai ⇒ không mở cổng.
//   - TenantProvisioningService.CreateTenantAsync — trước dòng ghi ĐẦU TIÊN của một đơn vị sắp dựng. Lệnh `core bootstrap`
//     (CoreCommandRunner) chạy trong một scope DI trước app.Run(), nên KHÔNG hosted service nào khởi động trên đường đó;
//     thiếu lời gọi này, seed sai được commit, lệnh thoát 0, và chỉ tới lần khởi động kế tiếp tiến trình mới từ chối lên
//     — lúc dữ liệu sai đã nằm trong database.
//
// Kiểm mọi thứ ApplySeedAsync của TenantProvisioningService sẽ vấp mà KHÔNG cần database, theo đúng thứ tự nó dựng:
// vai trò không trùng (so theo cách Identity chuẩn hoá tên), ánh xạ trỏ tới vai trò đã khai và khoá có trong danh mục,
// cặp ánh xạ không lặp, mã menu không trùng, mục con trỏ tới một mục CẤP MỘT không mang route, khoá của menu có trong
// danh mục.
// Phần phụ thuộc database (danh mục C# có khoá mà core.permission chưa có dòng) thuộc cổng B7 và ApplySeedAsync.
//
// Ném TenantSeedException — đúng loại lỗi "dữ liệu mặc định không dựng được thành dòng" (TenantSeedException.cs). Mỗi
// chỗ gọi tự quyết hệ quả: hosted service đổi thành InvalidOperationException để host dừng; service tạo đơn vị đổi thành
// CORE.TENANT.SEED_FAILED. Mọi exception KHÁC (một nguồn seed viết sai ném khi được đọc) đi qua nguyên vẹn.
internal static class TenantSeedValidator
{
    private static readonly UpperInvariantLookupNormalizer RoleNameNormalizer = new();

    public static void Validate(IEnumerable<ITenantSeedSource> seedSources, IEnumerable<IPermissionCatalogSource> catalogSources)
    {
        var sources = seedSources.ToList();
        var catalog = catalogSources
            .SelectMany(source => source.GetPermissions())
            .Select(permission => permission.Code)
            .ToHashSet(StringComparer.Ordinal);

        var roleNames = ValidateRoles(sources.SelectMany(s => s.GetRoles()));
        ValidateRolePermissions(sources.SelectMany(s => s.GetRolePermissions()), roleNames, catalog);
        ValidateMenuItems(sources.SelectMany(s => s.GetMenuItems()).ToList(), catalog);
    }

    private static HashSet<string> ValidateRoles(IEnumerable<SeedRole> roles)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in roles)
        {
            if (!normalized.Add(RoleNameNormalizer.NormalizeName(role.Name)))
                throw Invalid($"vai trò '{role.Name}' bị khai trùng (so theo tên đã chuẩn hoá của Identity).");

            declared.Add(role.Name);
        }

        return declared;
    }

    private static void ValidateRolePermissions(
        IEnumerable<SeedRolePermission> rolePermissions, HashSet<string> roleNames, HashSet<string> catalog)
    {
        var pairs = new HashSet<(string, string)>();

        foreach (var mapping in rolePermissions)
        {
            // Tra theo tên vai trò SEED, cùng hình dạng tra của ApplySeedAsync — đây là kiểm dữ liệu mặc định, không phải
            // một quyết định phân quyền theo tên vai trò (luật S2 cấm cái sau).
            if (!roleNames.TryGetValue(mapping.RoleName, out _))
                throw Invalid($"ánh xạ quyền trỏ tới vai trò '{mapping.RoleName}' không có nguồn nào khai.");

            if (!catalog.Contains(mapping.PermissionCode))
                throw Invalid($"ánh xạ quyền của vai trò '{mapping.RoleName}' trỏ tới khoá '{mapping.PermissionCode}' " +
                              "không có trong danh mục quyền đã gộp.");

            if (!pairs.Add((mapping.RoleName, mapping.PermissionCode)))
                throw Invalid($"cặp ánh xạ ('{mapping.RoleName}', '{mapping.PermissionCode}') bị khai trùng.");
        }
    }

    // Hai luật của docs/database/schema-core.md §6.1 mà không constraint nào diễn đạt được — kiểm ở ĐÂY, và lớp này đứng
    // trước đường ghi duy nhất hiện có (TenantProvisioningService.ApplySeedAsync) trên CẢ HAI đường tới nó:
    //   1. Cây ĐÚNG MỘT CẤP — ParentCode phải trỏ tới một mục CẤP MỘT (ParentCode rỗng), không tới một mục con.
    //   2. Mục cha có route RỖNG — cha chỉ đóng/mở, không điều hướng.
    //
    // 🪤 Luật 1 không viết được thành "cha đã được dựng trước nó": một mục con vừa qua phép kiểm cũng là "đã dựng", nên
    // chuỗi A → B → C lọt qua và seed ghi thẳng cây ba cấp. Triệu chứng không phải một lỗi: GetVisibleMenuAsync kéo mục
    // cha bằng MỘT lượt duyệt nên mục ông bà không vào response, FE dựng cây từ danh sách phẳng bỏ rơi nhánh đó, và
    // người có quyền không thấy mục menu — không lỗi, không log.
    //
    // Mã menu trùng cũng phải bị chặn ở đây, không để tới ux_menu_item_tenant_code_active: tới đó nó là một
    // DbUpdateException thô giữa lượt ghi, không phải một lỗi seed đọc được. Unique đó so mã ĐÃ LƯU, nên phép so trùng và
    // phép tra ParentCode chạy trên MenuItem.NormalizeCode — cùng hàm entity dùng khi lưu, cùng hàm ApplySeedAsync dùng
    // khi nối con vào cha. So trên chuỗi thô thì "bao-cao" và "bao-cao " là hai mã ở đây nhưng là một mã trong database.
    private static void ValidateMenuItems(IReadOnlyList<SeedMenuItem> menuItems, HashSet<string> catalog)
    {
        var byCode = new Dictionary<string, SeedMenuItem>(StringComparer.Ordinal);
        foreach (var item in menuItems)
        {
            var code = MenuItem.NormalizeCode(item.Code);
            if (!byCode.TryAdd(code, item))
                throw Invalid($"mã menu '{item.Code}' bị khai trùng với '{byCode[code].Code}' (so theo mã đã chuẩn hoá '{code}').");

            if (item.RequiredPermissionCode is { } permission && !catalog.Contains(permission))
                throw Invalid($"mục menu '{item.Code}' gắn khoá '{permission}' không có trong danh mục quyền đã gộp.");
        }

        foreach (var child in menuItems.Where(m => m.ParentCode is not null))
        {
            if (!byCode.TryGetValue(MenuItem.NormalizeCode(child.ParentCode!), out var parent))
                throw Invalid($"mục menu '{child.Code}' trỏ ParentCode '{child.ParentCode}' không có mục nào được dựng trước nó.");

            if (parent.ParentCode is not null)
                throw Invalid($"mục menu '{child.Code}' trỏ ParentCode '{child.ParentCode}', mà mục đó lại có cha " +
                              $"'{parent.ParentCode}' — cây menu đúng MỘT cấp (schema-core.md §6.1 luật 1).");

            if (parent.Route is not null)
                throw Invalid($"mục menu '{parent.Code}' có con ('{child.Code}') nên là mục cha, mà mục cha chỉ đóng/mở " +
                              $"và phải có route rỗng (schema-core.md §6.1 luật 2) — nó đang trỏ '{parent.Route}'.");
        }
    }

    private static TenantSeedException Invalid(string detail) => new($"Nguồn seed đơn vị không hợp lệ: {detail}");
}
