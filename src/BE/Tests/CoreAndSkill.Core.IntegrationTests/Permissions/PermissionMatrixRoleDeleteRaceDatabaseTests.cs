using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Nợ E17 áp cho ma trận quyền (docs/DEBT.md), trên PostgreSQL thật. role_permission.role_id là ON DELETE CASCADE; PUT ma trận
// khoá dòng mọi vai trò của đơn vị (RoleRowLocks.LockAllForMatrixWriteAsync) trước mọi câu đọc, nên lượt xoá vai trò commit
// trong lúc lượt ghi ma trận chờ khoá thì lượt ghi thấy trạng thái SAU lần xoá và trả đúng mã của docs/contracts/permissions.md
// §6:
//   • payload CẤP quyền cho vai trò vừa bị xoá ⇒ 422 CORE.PERMISSION.ROLE_NOT_FOUND — không có khoá thì câu INSERT gặp khoá
//     ngoại (23503) ⇒ 500;
//   • payload chỉ THU HỒI quyền của vai trò vừa bị xoá ⇒ 409 CORE.PERMISSION.VERSION_MISMATCH (ma trận đã đổi dưới tay người
//     gửi) — không có khoá thì câu UPDATE thu hồi chạm 0 dòng (cascade đã xoá dòng đó) ⇒ 409 mã chung CORE.CONCURRENCY.CONFLICT,
//     không phải mã của hợp đồng ma trận.
//
// Bên "người xoá" là một kết nối Npgsql riêng giữ một câu DELETE chưa commit (Support/LockContention.cs); request PUT thật phải
// chờ khoá của nó — test đợi tới khi thấy chờ (chống test rỗng) rồi mới commit.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class PermissionMatrixRoleDeleteRaceDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string MatrixUrl = "/api/v1/core/permissions/matrix";
    private const string RolesUrl = "/api/v1/core/roles";
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

    [Fact]
    public async Task RoleDeleteCommitsWhileTheMatrixSaveWaits_GrantingThatRole_Gets422RoleNotFound_AndChangesNothing()
    {
        var tenant = await _host.CreateTenantAsync("DV-E17M");
        using var client = Client(tenant);
        var roleId = await CreateRoleAsync(client, "Vai trò sắp xoá");
        var matrix = await GetMatrixAsync(client);
        var permissionId = matrix.Grants.Keys.First();
        var rowsBefore = await RolePermissionRowsAsync(tenant.Id);

        await using var deleter = await HeldTransaction.OpenAsync(db.ConnectionString);
        await deleter.ExecuteOneRowAsync("DELETE FROM core.app_role WHERE id = @role AND tenant_id = @tenant",
            new NpgsqlParameter("role", roleId), new NpgsqlParameter("tenant", tenant.Id));

        var request = client.PutAsJsonAsync(MatrixUrl, Payload(matrix, grants => grants.Key == permissionId
            ? [.. grants.Value, roleId]
            : grants.Value));

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await deleter.CommitAsync();

        var response = await request;
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity, await response.Content.ReadAsStringAsync());
        var error = (await ReadEnvelope(response)).Error!;
        error.Code.ShouldBe(PermissionErrors.RoleNotFound.Code);
        error.MessageParams!["RoleId"].ShouldBe(roleId.ToString());
        (await RolePermissionRowsAsync(tenant.Id)).ShouldBe(rowsBefore, "422 thì không ghi dòng nào");
    }

    [Fact]
    public async Task RoleDeleteCommitsWhileTheMatrixSaveWaits_RevokingFromThatRole_Gets409VersionMismatch()
    {
        var tenant = await _host.CreateTenantAsync("DV-E17M");
        using var client = Client(tenant);
        var roleId = await CreateRoleAsync(client, "Vai trò sắp xoá");
        var before = await GetMatrixAsync(client);
        var permissionId = before.Grants.Keys.First();

        // Cấp cho vai trò đó một quyền (lượt ghi tuần tự, không tranh chấp) rồi đọc lại ma trận — đúng thứ người gửi đang cầm.
        var grant = await client.PutAsJsonAsync(MatrixUrl, Payload(before, grants => grants.Key == permissionId
            ? [.. grants.Value, roleId]
            : grants.Value));
        grant.StatusCode.ShouldBe(HttpStatusCode.OK, await grant.Content.ReadAsStringAsync());
        var matrix = await GetMatrixAsync(client);
        matrix.Grants[permissionId].ShouldContain(roleId, "chống test rỗng: vai trò phải đang có quyền để lượt sau thu hồi");

        await using var deleter = await HeldTransaction.OpenAsync(db.ConnectionString);
        await deleter.ExecuteOneRowAsync("DELETE FROM core.app_role WHERE id = @role AND tenant_id = @tenant",
            new NpgsqlParameter("role", roleId), new NpgsqlParameter("tenant", tenant.Id));

        // Payload thu hồi mọi quyền của vai trò đó và không nhắc tới nó — không có nhánh ROLE_NOT_FOUND nào để đi.
        var request = client.PutAsJsonAsync(MatrixUrl, Payload(matrix, grants => [.. grants.Value.Where(r => r != roleId)]));

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await deleter.CommitAsync();

        var response = await request;
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, await response.Content.ReadAsStringAsync());
        (await ReadEnvelope(response)).Error!.Code.ShouldBe(
            PermissionErrors.VersionMismatch.Code, "mã của hợp đồng ma trận, không phải mã đồng thời chung");
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private sealed record Matrix(string Version, IReadOnlyDictionary<Guid, HashSet<Guid>> Grants);

    private static object Payload(Matrix matrix, Func<KeyValuePair<Guid, HashSet<Guid>>, IEnumerable<Guid>> roleIds) => new
    {
        version = matrix.Version,
        entries = matrix.Grants.Select(kv => new { permissionId = kv.Key, roleIds = roleIds(kv).ToArray() }).ToArray(),
    };

    private static async Task<Matrix> GetMatrixAsync(HttpClient client)
    {
        var response = await client.GetAsync(MatrixUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var data = (await ReadEnvelope(response)).Data;

        var grants = data.GetProperty("rows").EnumerateArray().ToDictionary(
            r => r.GetProperty("permissionId").GetGuid(),
            r => r.GetProperty("grantedRoleIds").EnumerateArray().Select(x => x.GetGuid()).ToHashSet());

        return new Matrix(data.GetProperty("version").GetString()!, grants);
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(RolesUrl, new { name });
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetGuid();
    }

    // Đọc THẲNG qua Npgsql, kể cả dòng đã xoá mềm — độc lập với bộ lọc của chính code đang test.
    private Task<long> RolePermissionRowsAsync(Guid tenantId)
        => db.CountAsync("SELECT count(*) FROM core.role_permission WHERE tenant_id = @tenant", new NpgsqlParameter("tenant", tenantId));

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

    private static async Task<ApiEnvelope<JsonElement>> ReadEnvelope(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
}
