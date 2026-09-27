using CoreAndSkill.Core.Application.Tenants;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Test double của ITenantSeedSource — thế chỗ một module thật. Core tự khai KHÔNG vai trò nào (luật
// S1) nên nếu thiếu nguồn này, vòng lặp vai trò/ánh xạ quyền của ApplySeedAsync không có gì để chạy
// và test seed xanh vì tập rỗng. Nguồn này được GỘP với CoreTenantSeedSource (menu của Core).
//
// Khoá quyền dùng ở đây PHẢI có sẵn trong core.permission — 0002__core__seed-permission-catalog.sql.
internal sealed class FakeTenantSeedSource : ITenantSeedSource
{
    public const string RoleName = "TestRole";
    public const string PermissionCode = "core.user.read";
    public const string MenuItemCode = "test-menu";

    public IReadOnlyCollection<SeedRole> GetRoles() => [new SeedRole(RoleName, IsSystem: false)];

    public IReadOnlyCollection<SeedRolePermission> GetRolePermissions()
        => [new SeedRolePermission(RoleName, PermissionCode)];

    public IReadOnlyCollection<SeedMenuItem> GetMenuItems()
        => [new SeedMenuItem(MenuItemCode, "menu.test", Icon: null, Route: "/test", ParentCode: null,
            DisplayOrder: 5, RequiredPermissionCode: PermissionCode, ModuleKey: null)];
}
