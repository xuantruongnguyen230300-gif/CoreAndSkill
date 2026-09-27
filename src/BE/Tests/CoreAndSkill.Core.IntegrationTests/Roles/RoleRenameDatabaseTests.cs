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

namespace CoreAndSkill.Core.IntegrationTests.Roles;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// PUT /api/v1/core/roles/{id} qua HTTP tới PostgreSQL thật — docs/contracts/roles.md §3; token
// docs/wiki-core/be/06-concurrency-control.md §6.3 (nguồn: core.app_role.concurrency_stamp). Phép so nằm trong câu
// UPDATE của RoleStore — ở đây là hành vi thật của câu đó: lệch hoặc thiếu ⇒ 409 và dòng trong DB không đổi.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class RoleRenameDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string Url = "/api/v1/core/roles";
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
    public async Task Rename_WithCurrentVersion_Succeeds_ThenTheOldVersionIsRejected_AndNothingIsWritten()
    {
        var tenant = await _host.CreateTenantAsync("DV-VT");
        using var client = Client(tenant);
        var (id, v0) = await SeedRoleAsync(client);

        var first = await client.PutAsJsonAsync($"{Url}/{id}", new { name = "Vai trò mới", version = v0 });
        first.StatusCode.ShouldBe(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        var v1 = (await ReadEnvelope(first)).Data.GetProperty("version").GetString();
        v1.ShouldNotBeNullOrEmpty();
        v1.ShouldNotBe(v0);

        var stale = await client.PutAsJsonAsync($"{Url}/{id}", new { name = "Ghi đè", version = v0 });
        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadEnvelope(stale)).Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);

        (await db.CountAsync(
            "SELECT count(*) FROM core.app_role WHERE id = @id AND name = 'Vai trò mới' AND concurrency_stamp = @v1",
            new NpgsqlParameter("id", id), new NpgsqlParameter("v1", v1))).ShouldBe(1);
    }

    [Fact]
    public async Task Rename_WithoutVersion_Returns409_AndNothingIsWritten()
    {
        var tenant = await _host.CreateTenantAsync("DV-VT");
        using var client = Client(tenant);
        var (id, v0) = await SeedRoleAsync(client);

        var response = await client.PutAsJsonAsync($"{Url}/{id}", new { name = "Ghi đè" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        (await db.CountAsync(
            "SELECT count(*) FROM core.app_role WHERE id = @id AND name = @name AND concurrency_stamp = @v0",
            new NpgsqlParameter("id", id), new NpgsqlParameter("name", FakeTenantSeedSource.RoleName),
            new NpgsqlParameter("v0", v0))).ShouldBe(1);
    }

    // Vai trò seed (không phải hệ thống) của FakeTenantSeedSource, đọc qua GET danh sách — đúng đường FE lấy version.
    private static async Task<(Guid Id, string Version)> SeedRoleAsync(HttpClient client)
    {
        var response = await client.GetAsync(Url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var item = (await ReadEnvelope(response)).Data.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == FakeTenantSeedSource.RoleName);
        return (item.GetProperty("id").GetGuid(), item.GetProperty("version").GetString()!);
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
