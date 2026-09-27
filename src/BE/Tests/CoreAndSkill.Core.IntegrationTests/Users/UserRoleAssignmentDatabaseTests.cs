using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Users;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// PUT /api/v1/core/users/{id}/roles qua HTTP tới PostgreSQL thật — docs/contracts/users.md §7 mục "Ghi chú — đồng thời",
// docs/adr/0082-gan-vai-tro-dung-token-cua-tai-khoan.md. `version` là concurrency_stamp của TÀI KHOẢN đích.
//
// Đồng thời: hai PUT cùng giữ một `version` — đúng MỘT lượt thắng, lượt kia nhận 409 CORE.CONCURRENCY.CONFLICT và không
// ghi gì. Không có câu UPDATE so-và-đổi token thì lượt sau hoặc gỡ im lặng vai trò lượt trước vừa cấp (hai vai trò KHÁC
// nhau), hoặc vỡ khoá chính (user_id, role_id) thành 500 (CÙNG một vai trò). Thứ tự "so-và-đổi token trước mọi thứ" được
// chứng minh không cần Docker ở UserRoleAssignmentVersionTests; ở đây là hành vi thật của khoá dòng trên PostgreSQL.
//
// Hai request chạy song song thật, nhưng không có gì ép chúng CHỒNG nhau về thời gian: nếu chúng tình cờ nối đuôi, test vẫn
// xanh (lượt sau vẫn lệch version). Tức là ca đồng thời có thể xanh trên code chỉ so token mà KHÔNG giữ khoá — nó không đứng
// một mình.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class UserRoleAssignmentDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string UsersUrl = "/api/v1/core/users";
    private const string RolesUrl = "/api/v1/core/roles";
    private const string RoleAssignAction = "core.user.role_assign";
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
    [InlineData(false)] // hai người cấp hai vai trò KHÁC nhau — không token thì lượt sau gỡ im lặng vai trò lượt trước cấp
    [InlineData(true)]  // hai người cấp CÙNG một vai trò — không khoá thì lượt sau vỡ khoá chính app_user_role thành 500
    public async Task TwoAssignmentsHoldingTheSameVersion_ExactlyOneWins_TheOtherGets409(bool sameRole)
    {
        var tenant = await _host.CreateTenantAsync("DV-GVT");
        using var client = Client(tenant);
        var userId = await CreateUserAsync(client);
        var roleA = await SeedRoleIdAsync(client);
        var roleB = sameRole ? roleA : await CreateRoleAsync(client, "Vai trò B");
        var v0 = await GetVersionAsync(client, userId);

        var responses = await Task.WhenAll(
            client.PutAsJsonAsync($"{UsersUrl}/{userId}/roles", new { roleIds = new[] { roleA }, version = v0 }),
            client.PutAsJsonAsync($"{UsersUrl}/{userId}/roles", new { roleIds = new[] { roleB }, version = v0 }));

        var statuses = responses.Select(r => r.StatusCode).ToList();
        statuses.ShouldBe([HttpStatusCode.OK, HttpStatusCode.Conflict], ignoreOrder: true,
            string.Join(" | ", await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()))));

        var loser = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        (await ReadEnvelope(loser)).Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);

        // Trạng thái cuối = đúng tập của bên thắng, không phải hợp của hai bên.
        var winnerRole = responses[0].StatusCode == HttpStatusCode.OK ? roleA : roleB;
        (await RoleIdsInDatabaseAsync(userId)).ShouldBe([winnerRole]);

        // Lượt thua không ghi gì — kể cả dòng nhật ký.
        (await RoleAssignAuditRowsAsync(userId)).ShouldBe(1);
        (await StampInDatabaseAsync(userId)).ShouldNotBe(v0);
    }

    [Fact]
    public async Task StaleVersion_Returns409_AndChangesNothing()
    {
        var tenant = await _host.CreateTenantAsync("DV-GVT");
        using var client = Client(tenant);
        var userId = await CreateUserAsync(client);
        var roleA = await SeedRoleIdAsync(client);
        var roleB = await CreateRoleAsync(client, "Vai trò B");
        var v0 = await GetVersionAsync(client, userId);

        var first = await client.PutAsJsonAsync($"{UsersUrl}/{userId}/roles", new { roleIds = new[] { roleA }, version = v0 });
        first.StatusCode.ShouldBe(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        var v1 = await StampInDatabaseAsync(userId);
        v1.ShouldNotBe(v0, "đổi tập vai trò phải đổi token của tài khoản (card §7 ràng buộc 1)");

        var stale = await client.PutAsJsonAsync($"{UsersUrl}/{userId}/roles", new { roleIds = new[] { roleB }, version = v0 });

        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadEnvelope(stale)).Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        (await RoleIdsInDatabaseAsync(userId)).ShouldBe([roleA]);
        (await StampInDatabaseAsync(userId)).ShouldBe(v1);
        (await RoleAssignAuditRowsAsync(userId)).ShouldBe(1);
    }

    // Card §7 ràng buộc 3: PUT không đổi vai trò nào VẪN so version.
    [Fact]
    public async Task NoRoleChange_StillComparesTheVersion()
    {
        var tenant = await _host.CreateTenantAsync("DV-GVT");
        using var client = Client(tenant);
        var userId = await CreateUserAsync(client);
        var current = await GetVersionAsync(client, userId);

        var stale = await client.PutAsJsonAsync($"{UsersUrl}/{userId}/roles", new { roleIds = Array.Empty<Guid>(), version = "khong-khop" });
        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadEnvelope(stale)).Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        (await StampInDatabaseAsync(userId)).ShouldBe(current);

        var matching = await client.PutAsJsonAsync($"{UsersUrl}/{userId}/roles", new { roleIds = Array.Empty<Guid>(), version = current });
        matching.StatusCode.ShouldBe(HttpStatusCode.OK, await matching.Content.ReadAsStringAsync());
        (await RoleAssignAuditRowsAsync(userId)).ShouldBe(0, "không dòng app_user_role nào đổi thì không có dòng nhật ký");
    }

    [Fact]
    public async Task MissingVersion_Returns409_AndChangesNothing()
    {
        var tenant = await _host.CreateTenantAsync("DV-GVT");
        using var client = Client(tenant);
        var userId = await CreateUserAsync(client);
        var roleA = await SeedRoleIdAsync(client);
        var current = await GetVersionAsync(client, userId);

        var response = await client.PutAsJsonAsync($"{UsersUrl}/{userId}/roles", new { roleIds = new[] { roleA } });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        (await RoleIdsInDatabaseAsync(userId)).ShouldBeEmpty();
        (await StampInDatabaseAsync(userId)).ShouldBe(current);
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private static async Task<Guid> CreateUserAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(UsersUrl, new
        {
            userName = "an.nv",
            email = "an@vd.vn",
            fullName = "Nguyễn Văn An",
            tempPassword = B4DockerHost.Password,
            roleIds = Array.Empty<Guid>(),
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetGuid();
    }

    // `version` lấy từ GET chi tiết — đúng đường FE lấy token (card §7: "token nhận từ GET gần nhất").
    private static async Task<string> GetVersionAsync(HttpClient client, Guid userId)
    {
        var response = await client.GetAsync($"{UsersUrl}/{userId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetProperty("version").GetString()!;
    }

    // Vai trò seed (không phải hệ thống) của FakeTenantSeedSource.
    private static async Task<Guid> SeedRoleIdAsync(HttpClient client)
    {
        var response = await client.GetAsync(RolesUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == FakeTenantSeedSource.RoleName)
            .GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(RolesUrl, new { name });
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetGuid();
    }

    // Đọc THẲNG qua Npgsql — độc lập với bộ lọc và interceptor của chính code đang test.
    private Task<IReadOnlyList<Guid>> RoleIdsInDatabaseAsync(Guid userId)
        => db.QueryAsync("SELECT role_id FROM core.app_user_role WHERE user_id = @id", r => r.GetGuid(0), new NpgsqlParameter("id", userId));

    private async Task<string> StampInDatabaseAsync(Guid userId)
        => (await db.QueryAsync("SELECT concurrency_stamp FROM core.app_user WHERE id = @id", r => r.GetString(0), new NpgsqlParameter("id", userId)))
            .ShouldHaveSingleItem();

    private Task<long> RoleAssignAuditRowsAsync(Guid userId)
        => db.CountAsync(
            "SELECT count(*) FROM core.audit_log WHERE action_code = @code AND target_id = @id",
            new NpgsqlParameter("code", RoleAssignAction), new NpgsqlParameter("id", userId.ToString()));

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
