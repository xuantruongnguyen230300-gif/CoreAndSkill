using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Trên PostgreSQL THẬT, mặt mà TenantCodeUniqueViolationTests và RoleNameUniqueViolationTests (không database) không chứng minh
// được: 23505 thật của Npgsql trên uq_tenant_code / RoleNameIndex ra CODE_DUPLICATE (409) / NAME_DUPLICATE (409), lượt thua không
// để lại dòng nào. Cùng khuôn AppUserUniqueViolationDatabaseTests: "lượt kia" là một kết nối riêng đã chèn bản trùng mà CHƯA
// commit (HeldTransaction); lệnh ghi của lượt thật phải CHỜ trên index duy nhất, test đợi tới khi thấy chờ rồi mới commit.
//
// Cần Docker daemon (PostgresFixture). Máy không có Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class TenantCodeAndRoleNameUniqueViolationDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly B4DockerHost _host = new(db.ConnectionString, configure: services =>
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = InMemoryCoreHost.TestScheme;
                options.DefaultAuthenticateScheme = InMemoryCoreHost.TestScheme;
                options.DefaultChallengeScheme = InMemoryCoreHost.TestScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, InMemoryCoreHost.TestAuthHandler>(InMemoryCoreHost.TestScheme, _ => { });
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
    });

    public Task InitializeAsync() => db.ResetAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    // tenants.md §2 — chạy thẳng service trong transaction (cùng đường TransactionBehavior), không qua HTTP.
    [Fact]
    public async Task CreateTenant_WhileAnotherCreateOfTheSameCodeCommits_IsCodeDuplicate_AndWritesNothing()
    {
        await using var other = await HeldTransaction.OpenAsync(db.ConnectionString);
        await other.ExecuteOneRowAsync(
            "INSERT INTO core.tenant (id, code, name, is_active, is_system) VALUES (@id, 'DV-TRUNG', 'Người kia', true, false)",
            new NpgsqlParameter("id", Guid.NewGuid()));

        using var scope = _host.Services.CreateScope();
        var request = ProvisioningInTransaction.RunAsync(scope.ServiceProvider, (provisioning, ct) => provisioning.CreateTenantAsync(
            new CreateTenantInput(
                "dv-trung", "Đơn vị trùng", IsSystem: false, "quantri", B4DockerHost.Password, AdminHasPermissionBypass: true,
                AdminIsSystemOperator: false, FailIfExists: true, AdminEmail: "quantri@vd.vn", AdminFullName: "Quản trị"),
            ct));

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await other.CommitAsync();
        var result = await request;

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.CodeDuplicate.Code);
        result.Error.Params.ShouldContainKeyAndValue("Code", "dv-trung");
        (await db.CountAsync("SELECT count(*) FROM core.tenant WHERE code = 'DV-TRUNG'")).ShouldBe(1);
        (await db.CountAsync("SELECT count(*) FROM core.app_user WHERE user_name = 'quantri'")).ShouldBe(0);
    }

    // roles.md §2 — POST /roles.
    [Fact]
    public async Task CreateRole_WhileAnotherCreateOfTheSameNameCommits_Is409NameDuplicate_AndWritesNothing()
    {
        var tenant = await _host.CreateTenantAsync("DV-TRUNG");
        using var client = Client(tenant);

        await using var other = await HeldTransaction.OpenAsync(db.ConnectionString);
        await other.ExecuteOneRowAsync(
            "INSERT INTO core.app_role (id, tenant_id, name, normalized_name, is_system, concurrency_stamp) " +
            "VALUES (@id, @t, 'Kế toán', 'KẾ TOÁN', false, @stamp)",
            new NpgsqlParameter("id", Guid.NewGuid()),
            new NpgsqlParameter("t", tenant.Id),
            new NpgsqlParameter("stamp", Guid.NewGuid().ToString()));

        var request = client.PostAsJsonAsync("/api/v1/core/roles", new { name = "Kế toán" });

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await other.CommitAsync();
        var response = await request;

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());
        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
        var error = envelope.Error.ShouldNotBeNull();
        error.Code.ShouldBe(RoleErrors.NameDuplicate.Code);
        (await db.CountAsync(
            "SELECT count(*) FROM core.app_role WHERE tenant_id = @t AND normalized_name = 'KẾ TOÁN'",
            new NpgsqlParameter("t", tenant.Id))).ShouldBe(1);
    }

    // roles.md §3 (+ §2 "Hai lượt ghi cùng name chạy đồng thời … hoặc đổi tên ở §3"): lượt thua nhận CORE.ROLE.NAME_DUPLICATE 409
    // như ca trùng thường, `messageParams.Name` là name như request gửi, không phải 500 — và không ghi gì.
    // Lượt thắng ở đây là một lượt ĐỔI TÊN vai trò B thành đúng tên đó, đã ghi nhưng chưa commit.
    [Fact]
    public async Task RenameRole_WhileAnotherRenameToTheSameNameCommits_Is409NameDuplicate_AndWritesNothing()
    {
        var tenant = await _host.CreateTenantAsync("DV-TRUNG");
        using var client = Client(tenant);
        var roleA = await CreateRoleAsync(client, "Vai trò A");
        var roleB = await CreateRoleAsync(client, "Vai trò B");
        var versionA = await GetRoleVersionAsync(client, roleA);
        var before = await RoleRowAsync(roleA);

        await using var other = await HeldTransaction.OpenAsync(db.ConnectionString);
        await other.ExecuteOneRowAsync(
            "UPDATE core.app_role SET name = 'Kế toán', normalized_name = 'KẾ TOÁN', concurrency_stamp = @stamp " +
            "WHERE id = @id AND tenant_id = @t",
            new NpgsqlParameter("stamp", Guid.NewGuid().ToString()),
            new NpgsqlParameter("id", roleB),
            new NpgsqlParameter("t", tenant.Id));

        var request = client.PutAsJsonAsync($"/api/v1/core/roles/{roleA}", new { name = "Kế toán", version = versionA });

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await other.CommitAsync();
        var response = await request;

        await AssertNameDuplicateAndLoserWroteNothingAsync(response, tenant, roleA, before);
    }

    // Cùng hợp đồng, lượt thắng là một lượt TẠO vai trò mới cùng tên (§2 gọi thẳng "tạo ở đây, hoặc đổi tên ở §3").
    [Fact]
    public async Task RenameRole_WhileAnotherCreateOfTheSameNameCommits_Is409NameDuplicate_AndWritesNothing()
    {
        var tenant = await _host.CreateTenantAsync("DV-TRUNG");
        using var client = Client(tenant);
        var roleA = await CreateRoleAsync(client, "Vai trò A");
        var versionA = await GetRoleVersionAsync(client, roleA);
        var before = await RoleRowAsync(roleA);

        await using var other = await HeldTransaction.OpenAsync(db.ConnectionString);
        await other.ExecuteOneRowAsync(
            "INSERT INTO core.app_role (id, tenant_id, name, normalized_name, is_system, concurrency_stamp) " +
            "VALUES (@id, @t, 'Kế toán', 'KẾ TOÁN', false, @stamp)",
            new NpgsqlParameter("id", Guid.NewGuid()),
            new NpgsqlParameter("t", tenant.Id),
            new NpgsqlParameter("stamp", Guid.NewGuid().ToString()));

        var request = client.PutAsJsonAsync($"/api/v1/core/roles/{roleA}", new { name = "Kế toán", version = versionA });

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await other.CommitAsync();
        var response = await request;

        await AssertNameDuplicateAndLoserWroteNothingAsync(response, tenant, roleA, before);
    }

    private async Task AssertNameDuplicateAndLoserWroteNothingAsync(
        HttpResponseMessage response, TestTenant tenant, Guid loserRoleId, (string Name, string Stamp) before)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, body);
        var error = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(body, Json)!.Error.ShouldNotBeNull();
        error.Code.ShouldBe(RoleErrors.NameDuplicate.Code);
        error.MessageParams.ShouldNotBeNull().ShouldContainKeyAndValue("Name", "Kế toán");

        // Bên thắng giữ đúng một dòng mang tên đó; bên thua còn nguyên tên và token cũ.
        (await db.CountAsync(
            "SELECT count(*) FROM core.app_role WHERE tenant_id = @t AND normalized_name = 'KẾ TOÁN'",
            new NpgsqlParameter("t", tenant.Id))).ShouldBe(1);
        (await RoleRowAsync(loserRoleId)).ShouldBe(before, "409 thì transaction của lượt thua quay lại — tên và concurrency_stamp không đổi");
        (await db.CountAsync(
            "SELECT count(*) FROM core.audit_log WHERE tenant_id = @t AND action_code = 'core.role.rename'",
            new NpgsqlParameter("t", tenant.Id))).ShouldBe(0, "lượt thua không để lại dòng audit đổi tên");
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/core/roles", new { name });
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!
            .Data.GetProperty("id").GetGuid();
    }

    // `version` lấy từ GET chi tiết — đúng đường FE lấy token (roles.md §3, §5).
    private static async Task<string> GetRoleVersionAsync(HttpClient client, Guid roleId)
    {
        var response = await client.GetAsync($"/api/v1/core/roles/{roleId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!
            .Data.GetProperty("version").GetString()!;
    }

    // Đọc THẲNG qua Npgsql — độc lập với bộ lọc và interceptor của chính code đang test.
    private async Task<(string Name, string Stamp)> RoleRowAsync(Guid roleId)
        => (await db.QueryAsync("SELECT name, concurrency_stamp FROM core.app_role WHERE id = @id",
                r => (r.GetString(0), r.GetString(1)), new NpgsqlParameter("id", roleId)))
            .ShouldHaveSingleItem();

    private HttpClient Client(TestTenant tenant)
    {
        var client = _host.Factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-User", tenant.AdminUserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-UserName", tenant.AdminUserName);
        client.DefaultRequestHeaders.Add("X-Test-Tenant", tenant.Id.ToString());

        var token = client.GetFromJsonAsync<JsonElement>("/api/v1/core/antiforgery/token", Json).GetAwaiter().GetResult()
            .GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        return client;
    }
}
