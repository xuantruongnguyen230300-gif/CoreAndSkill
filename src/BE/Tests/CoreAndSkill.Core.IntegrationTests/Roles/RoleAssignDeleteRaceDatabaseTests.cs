using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Roles;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Nợ E17 (docs/DEBT.md) trên PostgreSQL thật, mỗi chiều hỏng một ca. app_user_role.role_id là ON DELETE CASCADE; cặp khoá dòng
// core.app_role ở RoleRowLocks làm lượt xoá và lượt gán xếp hàng, và lượt sau nhận đúng mã nghiệp vụ:
//   • Chiều 1 — lượt xoá commit trong lúc lượt gán đang chạy: gán ⇒ 422 CORE.USER.ROLE_NOT_FOUND (docs/contracts/users.md §5,
//     §7), không phải 23503 ⇒ 500.
//   • Chiều 2 — lượt gán commit trong lúc lượt xoá đang chạy: xoá ⇒ 422 CORE.ROLE.IN_USE (docs/contracts/roles.md §4), và dòng
//     vừa gán còn nguyên — không bị cascade gỡ lặng lẽ sau một câu 200.
//
// Ép hai lượt CHỒNG nhau thay vì mong chúng tình cờ chồng: bên "người khác" là một kết nối Npgsql riêng giữ một transaction
// dở dang — đã xoá dòng vai trò (chiều 1) hoặc đã chèn dòng gán (chiều 2), chưa commit. Request thật đi qua HTTP tới lượt nó
// phải CHỜ khoá của kết nối kia (test đợi tới khi pg_stat_activity thấy một phiên chờ khoá — chống test rỗng), rồi kết nối kia
// mới commit. Không có cặp khoá thì cả hai ca đổi kết quả: chiều 1 ra 500 (câu INSERT gặp khoá ngoại), chiều 2 ra 200 và dòng
// gán biến mất.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class RoleAssignDeleteRaceDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string UsersUrl = "/api/v1/core/users";
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

    // Chiều 1, cả hai đường gán: PUT /users/{id}/roles (§7) và POST /users kèm roleIds (§5).
    [Theory]
    [InlineData(true)]  // PUT /users/{id}/roles — tài khoản đã có
    [InlineData(false)] // POST /users — tài khoản mới
    public async Task RoleDeleteCommitsWhileTheAssignmentWaits_AssignmentGets422RoleNotFound_AndWritesNothing(bool existingAccount)
    {
        var tenant = await _host.CreateTenantAsync("DV-E17");
        using var client = Client(tenant);
        var roleId = await CreateRoleAsync(client, "Vai trò sắp xoá");
        var userId = existingAccount ? await CreateUserAsync(client, "an.nv", "an@vd.vn") : Guid.Empty;
        var v0 = existingAccount ? await GetVersionAsync(client, userId) : null;

        // Lượt xoá của người khác: dòng vai trò đã xoá, CHƯA commit — giữ khoá dòng mà câu DELETE lấy.
        await using var deleter = await HeldTransaction.OpenAsync(db.ConnectionString);
        await deleter.ExecuteOneRowAsync("DELETE FROM core.app_role WHERE id = @role AND tenant_id = @tenant",
            new NpgsqlParameter("role", roleId), new NpgsqlParameter("tenant", tenant.Id));

        var request = existingAccount
            ? client.PutAsJsonAsync($"{UsersUrl}/{userId}/roles", new { roleIds = new[] { roleId }, version = v0 })
            : client.PostAsJsonAsync(UsersUrl, NewUserBody("binh.tv", "binh@vd.vn", [roleId]));

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await deleter.CommitAsync();

        var response = await request;
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity, await response.Content.ReadAsStringAsync());
        var error = (await ReadEnvelope(response)).Error!;
        error.Code.ShouldBe(UserErrors.RoleNotFound.Code);
        error.MessageParams!["RoleId"].ShouldBe(roleId.ToString());

        (await db.CountAsync("SELECT count(*) FROM core.app_role WHERE id = @id", new NpgsqlParameter("id", roleId))).ShouldBe(0);
        if (existingAccount)
        {
            (await RoleIdsInDatabaseAsync(userId)).ShouldBeEmpty();
            (await StampInDatabaseAsync(userId)).ShouldBe(v0, "422 thì transaction quay lại — kể cả phép đổi token");
        }
        else
        {
            (await db.CountAsync(
                "SELECT count(*) FROM core.app_user WHERE tenant_id = @tenant AND normalized_user_name = 'BINH.TV'",
                new NpgsqlParameter("tenant", tenant.Id))).ShouldBe(0, "422 thì tài khoản vừa tạo quay lại cùng transaction");
        }
    }

    // Chiều 2.
    [Fact]
    public async Task AssignmentCommitsWhileTheDeleteWaits_DeleteGets422InUse_AndTheAssignmentSurvives()
    {
        var tenant = await _host.CreateTenantAsync("DV-E17");
        using var client = Client(tenant);
        var roleId = await CreateRoleAsync(client, "Vai trò đang được gán");
        var userId = await CreateUserAsync(client, "an.nv", "an@vd.vn");

        // Lượt gán của người khác: dòng gán đã chèn, CHƯA commit — phép kiểm khoá ngoại của nó giữ FOR KEY SHARE trên dòng
        // vai trò, đúng chế độ RoleRowLocks lấy ở đường gán.
        await using var assigner = await HeldTransaction.OpenAsync(db.ConnectionString);
        await assigner.ExecuteOneRowAsync("INSERT INTO core.app_user_role (user_id, role_id, tenant_id) VALUES (@user, @role, @tenant)",
            new NpgsqlParameter("user", userId), new NpgsqlParameter("role", roleId), new NpgsqlParameter("tenant", tenant.Id));

        var request = client.DeleteAsync($"{RolesUrl}/{roleId}");

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await assigner.CommitAsync();

        var response = await request;
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity, await response.Content.ReadAsStringAsync());
        var error = (await ReadEnvelope(response)).Error!;
        error.Code.ShouldBe(RoleErrors.InUse.Code);
        error.MessageParams!["Count"].ShouldBe("1", "câu đếm chạy SAU khoá — thấy dòng vừa commit");

        (await db.CountAsync("SELECT count(*) FROM core.app_role WHERE id = @id", new NpgsqlParameter("id", roleId))).ShouldBe(1);
        (await RoleIdsInDatabaseAsync(userId)).ShouldBe([roleId], "dòng vừa gán không bị cascade gỡ");
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private static object NewUserBody(string userName, string email, Guid[] roleIds) => new
    {
        userName,
        email,
        fullName = "Nguyễn Văn An",
        tempPassword = B4DockerHost.Password,
        roleIds,
    };

    private static async Task<Guid> CreateUserAsync(HttpClient client, string userName, string email)
    {
        var response = await client.PostAsJsonAsync(UsersUrl, NewUserBody(userName, email, []));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetGuid();
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(RolesUrl, new { name });
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetGuid();
    }

    // `version` lấy từ GET chi tiết — đúng đường FE lấy token.
    private static async Task<string> GetVersionAsync(HttpClient client, Guid userId)
    {
        var response = await client.GetAsync($"{UsersUrl}/{userId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetProperty("version").GetString()!;
    }

    // Đọc THẲNG qua Npgsql — độc lập với bộ lọc và interceptor của chính code đang test.
    private Task<IReadOnlyList<Guid>> RoleIdsInDatabaseAsync(Guid userId)
        => db.QueryAsync("SELECT role_id FROM core.app_user_role WHERE user_id = @id", r => r.GetGuid(0), new NpgsqlParameter("id", userId));

    private async Task<string> StampInDatabaseAsync(Guid userId)
        => (await db.QueryAsync("SELECT concurrency_stamp FROM core.app_user WHERE id = @id", r => r.GetString(0), new NpgsqlParameter("id", userId)))
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

    private static async Task<ApiEnvelope<JsonElement>> ReadEnvelope(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
}
