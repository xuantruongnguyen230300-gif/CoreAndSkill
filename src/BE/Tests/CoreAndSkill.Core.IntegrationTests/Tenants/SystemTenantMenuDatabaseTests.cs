using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// ADR-0122 (docs/adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md)
// trên PostgreSQL THẬT:
//   - vế 2 của luật M17 (điểm 4): sau khi tạo một đơn vị hệ thống và một đơn vị nghiệp vụ, đơn vị hệ thống có đúng một mục
//     /he-thong/…, đơn vị nghiệp vụ không có mục nào;
//   - script bù dữ liệu (điểm 5): chạy CHÍNH TỆP database/bu-du-lieu/0001__core__he-thong-don-vi-menu.sql bằng psql, hai lần,
//     trên đơn vị hệ thống thiếu mục, rồi so dòng với CoreTenantSeedSource.SystemTenantMenuItems — để giá trị trong SQL không
//     lệch giá trị trong C#; và dừng khi chưa có đơn vị hệ thống.
// Cần Docker daemon (PostgresFixture). Máy không có Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class SystemTenantMenuDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string Password = "Passw0rd-Test1";
    private const string ScriptName = "0001__core__he-thong-don-vi-menu.sql";
    private const string ItemCode = "he-thong-don-vi";

    private readonly CoreWebApplicationFactory _factory = new(
        new Dictionary<string, string?> { ["ConnectionStrings:Core"] = db.ConnectionString },
        services => services.AddSingleton<ITenantSeedSource, FakeTenantSeedSource>());

    public Task InitializeAsync() => db.ResetAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task SystemTenant_HasExactlyOneSystemAreaItem_BusinessTenantHasNone()
    {
        var system = await CreateAsync(SystemInput());
        var business = await CreateAsync(BusinessInput());

        (await CountSystemAreaItemsAsync(system)).ShouldBe(1);
        (await CountSystemAreaItemsAsync(business)).ShouldBe(0);

        var item = (await ReadItemsAsync(system)).ShouldHaveSingleItem();
        AssertMatchesSeedList(item);
    }

    [Fact]
    public async Task BackfillScript_RunTwiceOnSystemTenantMissingTheItem_InsertsExactlyOneRowMatchingTheSeedList()
    {
        var system = await CreateAsync(SystemInput());
        // Dựng bản cài "đã bootstrap trước ADR-0122": đơn vị hệ thống không có mục. Mục không có dòng core.menu_item_role.
        await DeleteItemAsync(system);
        (await ReadItemsAsync(system)).ShouldBeEmpty();
        var historyBefore = await db.CountAsync("SELECT count(*) FROM core.schema_script_history");

        var first = await RunScriptAsync();
        var afterFirst = (await ReadItemsAsync(system)).ShouldHaveSingleItem();
        var second = await RunScriptAsync();
        var afterSecond = (await ReadItemsAsync(system)).ShouldHaveSingleItem();

        first.ExitCode.ShouldBe(0L, first.Output);
        second.ExitCode.ShouldBe(0L, second.Output);
        second.Output.ShouldContain("khong ghi gi");
        AssertMatchesSeedList(afterFirst);
        afterSecond.Id.ShouldBe(afterFirst.Id); // chạy lại không đổi gì
        afterFirst.Id.ToString()[14].ShouldBe('7', "id sinh bằng uuidv7()");
        afterFirst.CreatedBy.ShouldBe("system");
        afterFirst.UpdatedBy.ShouldBe("system");
        afterFirst.CreatedAtIsSet.ShouldBeTrue();
        // Script bù dữ liệu không ghi bảng lịch sử script schema.
        (await db.CountAsync("SELECT count(*) FROM core.schema_script_history")).ShouldBe(historyBefore);
    }

    [Fact]
    public async Task BackfillScript_BusinessTenantsAreNeverTouched()
    {
        var system = await CreateAsync(SystemInput());
        var business = await CreateAsync(BusinessInput());
        await DeleteItemAsync(system);

        (await RunScriptAsync()).ExitCode.ShouldBe(0L);

        (await CountSystemAreaItemsAsync(system)).ShouldBe(1);
        (await CountSystemAreaItemsAsync(business)).ShouldBe(0);
    }

    [Fact]
    public async Task BackfillScript_WhenNoSystemTenantExists_StopsWithAClearMessageAndWritesNothing()
    {
        var result = await RunScriptAsync();

        result.ExitCode.ShouldNotBe(0L);
        result.Output.ShouldContain("chua bootstrap");
        (await db.CountAsync("SELECT count(*) FROM core.menu_item")).ShouldBe(0);
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private static void AssertMatchesSeedList(MenuRow row)
    {
        var seed = CoreTenantSeedSource.SystemTenantMenuItems.ShouldHaveSingleItem();

        row.Code.ShouldBe(seed.Code);
        row.LabelKey.ShouldBe(seed.LabelKey);
        row.Icon.ShouldBe(seed.Icon);
        row.Route.ShouldBe(seed.Route);
        row.ParentId.ShouldBeNull();
        row.DisplayOrder.ShouldBe(seed.DisplayOrder);
        row.RequiredPermissionId.ShouldBeNull();
        row.ModuleKey.ShouldBe(seed.ModuleKey);
        row.IsDeleted.ShouldBeFalse();
    }

    private Task<(long ExitCode, string Output)> RunScriptAsync()
        => db.RunScriptFileAsync(Path.Combine(PostgresFixture.LocateBackfillDirectory(), ScriptName));

    private async Task<Guid> CreateAsync(CreateTenantInput input)
    {
        using var scope = _factory.Services.CreateScope();
        var result = await ProvisioningInTransaction.RunAsync(
            scope.ServiceProvider, (s, ct) => s.CreateTenantAsync(input, ct));
        result.IsSuccess.ShouldBeTrue();
        return result.Value.TenantId;
    }

    private Task<long> CountSystemAreaItemsAsync(Guid tenantId)
        => db.CountAsync(
            "SELECT count(*) FROM core.menu_item WHERE tenant_id = @t AND route LIKE '/he-thong%' AND is_deleted = false",
            new NpgsqlParameter("t", tenantId));

    private async Task DeleteItemAsync(Guid tenantId)
    {
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DELETE FROM core.menu_item WHERE tenant_id = @t AND code = @c", connection);
        command.Parameters.AddWithValue("t", tenantId);
        command.Parameters.AddWithValue("c", ItemCode);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record MenuRow(
        Guid Id, string Code, string LabelKey, string? Icon, string? Route, Guid? ParentId, int DisplayOrder,
        Guid? RequiredPermissionId, string? ModuleKey, bool IsDeleted, string? CreatedBy, string? UpdatedBy, bool CreatedAtIsSet);

    private Task<IReadOnlyList<MenuRow>> ReadItemsAsync(Guid tenantId)
        => db.QueryAsync(
            """
            SELECT id, code, label_key, icon, route, parent_id, display_order, required_permission_id, module_key, is_deleted,
                   created_by, updated_by, (created_at IS NOT NULL AND updated_at IS NOT NULL)
            FROM core.menu_item
            WHERE tenant_id = @t AND code = 'he-thong-don-vi' AND is_deleted = false
            """,
            r => new MenuRow(
                r.GetGuid(0), r.GetString(1), r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4),
                r.IsDBNull(5) ? null : r.GetGuid(5), r.GetInt32(6),
                r.IsDBNull(7) ? null : r.GetGuid(7), r.IsDBNull(8) ? null : r.GetString(8), r.GetBoolean(9),
                r.IsDBNull(10) ? null : r.GetString(10), r.IsDBNull(11) ? null : r.GetString(11), r.GetBoolean(12)),
            new NpgsqlParameter("t", tenantId));

    private static CreateTenantInput BusinessInput()
        => new(
            Code: "DV-MENU", Name: "Đơn vị DV-MENU", IsSystem: false, AdminUserName: "admin", AdminPassword: Password,
            AdminHasPermissionBypass: true, AdminIsSystemOperator: false,
            AdminEmail: "admin@dv-menu.example.com", AdminFullName: "Quản trị đơn vị");

    private static CreateTenantInput SystemInput()
        => new(
            Code: "SYSTEM", Name: "Hệ thống", IsSystem: true, AdminUserName: "superadmin", AdminPassword: Password,
            AdminHasPermissionBypass: false, AdminIsSystemOperator: true,
            AdminEmail: "superadmin@system.example.com", AdminFullName: "Vận hành hệ thống");
}
