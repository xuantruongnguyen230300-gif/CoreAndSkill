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
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// PUT /api/v1/core/permissions/matrix qua HTTP tới PostgreSQL thật — docs/contracts/permissions.md §6.
//
// Đồng thời: docs/wiki-core/be/06-concurrency-control.md §6.3 luật 3. Hai lượt lưu cùng giữ một `version` — đúng MỘT
// lượt thắng, lượt kia nhận 409 CORE.PERMISSION.VERSION_MISMATCH và không ghi gì. Không khoá thì dưới READ COMMITTED
// cả hai cùng qua phép so và cùng commit (thay đổi hai người trộn vào nhau), hoặc — khi cả hai cùng cấp một ô — lượt
// sau vỡ ux_role_permission_tenant_role_perm_active (23505) thành 500. Thứ tự "khoá trước khi đọc" được chứng minh
// không cần Docker ở PermissionMatrixLockOrderTests; ở đây là hành vi thật trên PostgreSQL.
//
// Hai request chạy song song thật, nhưng không có gì ép chúng CHỒNG nhau về thời gian: nếu chúng tình cờ nối đuôi,
// test vẫn xanh (lượt sau vẫn lệch version). Tức là test này có thể xanh trên code KHÔNG khoá — nó không đứng một mình.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class PermissionMatrixDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string Url = "/api/v1/core/permissions/matrix";
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

    [Theory]
    [InlineData(false)] // hai người sửa hai ô KHÁC nhau — không khoá thì thay đổi trộn vào nhau
    [InlineData(true)]  // hai người cấp CÙNG một ô — không khoá thì lượt sau vỡ unique index thành 500
    public async Task TwoSavesHoldingTheSameVersion_ExactlyOneWins_TheOtherGets409(bool sameCell)
    {
        var tenant = await _host.CreateTenantAsync("DV-MT");
        using var client = Client(tenant);
        var matrix = await GetMatrixAsync(client);
        var role = matrix.RoleIdByName[FakeTenantSeedSource.RoleName];

        var ungranted = matrix.Grants.Where(kv => !kv.Value.Contains(role)).Select(kv => kv.Key).Take(2).ToList();
        ungranted.Count.ShouldBe(2, "chống test rỗng: cần hai quyền chưa cấp cho vai trò thử");
        var cellA = ungranted[0];
        var cellB = sameCell ? ungranted[0] : ungranted[1];

        var responses = await Task.WhenAll(
            client.PutAsJsonAsync(Url, Payload(matrix, grant: (cellA, role))),
            client.PutAsJsonAsync(Url, Payload(matrix, grant: (cellB, role))));

        var statuses = responses.Select(r => r.StatusCode).ToList();
        statuses.ShouldBe([HttpStatusCode.OK, HttpStatusCode.Conflict], ignoreOrder: true,
            string.Join(" | ", await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()))));

        var loser = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        (await ReadEnvelope(loser)).Error!.Code.ShouldBe(PermissionErrors.VersionMismatch.Code);

        // Trạng thái cuối = đúng thay đổi của bên thắng, không phải hợp của hai bên.
        var winnerCell = responses[0].StatusCode == HttpStatusCode.OK ? cellA : cellB;
        var after = await GetMatrixAsync(client);
        var newlyGranted = after.Grants.Where(kv => kv.Value.Contains(role) && !matrix.Grants[kv.Key].Contains(role))
            .Select(kv => kv.Key).ToList();
        newlyGranted.ShouldBe([winnerCell]);
    }

    [Fact]
    public async Task EntriesIncomplete_Returns400_WithFieldErrorOnEntries_AndChangesNothing()
    {
        var tenant = await _host.CreateTenantAsync("DV-MT");
        using var client = Client(tenant);
        var matrix = await GetMatrixAsync(client);
        var rowsBefore = await db.CountAsync("SELECT count(*) FROM core.role_permission");

        var partial = matrix.Grants.Take(1).Select(kv => new { permissionId = kv.Key, roleIds = Array.Empty<Guid>() }).ToArray();
        var response = await client.PutAsJsonAsync(Url, new { version = matrix.Version, entries = partial });
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Code.ShouldBe(PermissionErrors.EntriesIncomplete.Code);
        error.FieldErrors!.Keys.ShouldBe(["Entries"]);
        error.FieldErrors["Entries"].Single().Code.ShouldBe(PermissionErrors.EntriesIncomplete.Code);
        (await db.CountAsync("SELECT count(*) FROM core.role_permission")).ShouldBe(rowsBefore);
        (await db.CountAsync("SELECT count(*) FROM core.role_permission WHERE is_deleted")).ShouldBe(0);
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private sealed record Matrix(string Version, IReadOnlyDictionary<string, Guid> RoleIdByName, IReadOnlyDictionary<Guid, HashSet<Guid>> Grants);

    private static object Payload(Matrix matrix, (Guid PermissionId, Guid RoleId) grant) => new
    {
        version = matrix.Version,
        entries = matrix.Grants.Select(kv => new
        {
            permissionId = kv.Key,
            roleIds = kv.Key == grant.PermissionId ? kv.Value.Append(grant.RoleId).ToArray() : kv.Value.ToArray(),
        }).ToArray(),
    };

    private static async Task<Matrix> GetMatrixAsync(HttpClient client)
    {
        var response = await client.GetAsync(Url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var data = (await ReadEnvelope(response)).Data;

        var roles = data.GetProperty("roles").EnumerateArray()
            .ToDictionary(r => r.GetProperty("name").GetString()!, r => r.GetProperty("id").GetGuid());
        var grants = data.GetProperty("rows").EnumerateArray().ToDictionary(
            r => r.GetProperty("permissionId").GetGuid(),
            r => r.GetProperty("grantedRoleIds").EnumerateArray().Select(x => x.GetGuid()).ToHashSet());

        return new Matrix(data.GetProperty("version").GetString()!, roles, grants);
    }

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
