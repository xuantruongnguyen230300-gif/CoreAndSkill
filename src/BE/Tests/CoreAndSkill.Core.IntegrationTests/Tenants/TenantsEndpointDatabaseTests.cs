using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Lệnh ghi của /system/tenants đi QUA HTTP tới PostgreSQL thật: controller → MediatR → TransactionBehavior →
// handler → TenantProvisioningService → UnitOfWork thật trên Npgsql. Đây là đường mà gọi thẳng service (như
// TenantProvisioningDatabaseTests và core bootstrap) KHÔNG đi qua — và chính vì thế một transaction lồng trong
// service từng không bị test nào bắt (Npgsql từ chối transaction thứ hai trên cùng kết nối ⇒ 500).
//
// Danh tính: scheme thử của InMemoryCoreHost (header → claim), vì đăng nhập thật của tài khoản vận hành vừa tạo
// còn vướng must_change_password. Khoá chống CSRF trong bộ nhớ — test không kiểm key ring.
//
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class TenantsEndpointDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string Password = "Passw0rd-Test1";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CoreWebApplicationFactory _factory = new(
        new Dictionary<string, string?> { ["ConnectionStrings:Core"] = db.ConnectionString },
        services =>
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

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ---- Transaction: mọi lệnh ghi qua HTTP thành công, dữ liệu thật sự commit --------------------

    [Fact]
    public async Task WriteEndpoints_ThroughHttp_Commit_NotA500()
    {
        // Arrange
        var system = await CreateSystemTenantAsync();
        using var client = OperatorClient(system);

        // Act 1 — tạo đơn vị
        var created = await client.PostAsJsonAsync("/api/v1/core/system/tenants", new
        {
            code = "DV-HTTP",
            name = "Đơn vị qua HTTP",
            adminUserName = "quantri",
            adminEmail = "quantri@dv-http.example.com",
            adminFullName = "Quản trị",
            adminTempPassword = Password,
        });

        created.StatusCode.ShouldBe(HttpStatusCode.OK, await created.Content.ReadAsStringAsync());
        var tenantId = (await ReadEnvelope(created)).Data.GetProperty("id").GetGuid();

        // Act 2 — tạo quản trị bổ sung
        var admin = await client.PostAsJsonAsync($"/api/v1/core/system/tenants/{tenantId}/admins", new
        {
            userName = "quantri2",
            email = "quantri2@dv-http.example.com",
            fullName = "Quản trị 2",
            tempPassword = Password,
        });
        admin.StatusCode.ShouldBe(HttpStatusCode.OK, await admin.Content.ReadAsStringAsync());

        // Act 3 — khôi phục quản trị
        var recovery = await client.PostAsJsonAsync($"/api/v1/core/system/tenants/{tenantId}/recovery-reset-password",
            new { userName = "quantri", tempPassword = "New-Passw0rd-2" });
        recovery.StatusCode.ShouldBe(HttpStatusCode.OK, await recovery.Content.ReadAsStringAsync());

        // Act 4 — ngưng hoạt động
        var deactivate = await client.PutAsJsonAsync($"/api/v1/core/system/tenants/{tenantId}/active", new { isActive = false });
        deactivate.StatusCode.ShouldBe(HttpStatusCode.OK, await deactivate.Content.ReadAsStringAsync());

        // Assert — đã commit, không chỉ "trả 200"
        (await db.CountAsync("SELECT count(*) FROM core.tenant WHERE id = @t AND is_active = false",
            new NpgsqlParameter("t", tenantId))).ShouldBe(1);
        (await db.CountAsync("SELECT count(*) FROM core.app_user WHERE tenant_id = @t",
            new NpgsqlParameter("t", tenantId))).ShouldBe(2);
    }

    // ---- Đơn vị hệ thống không phải đích của khu quản trị (docs/contracts/tenants.md §4, §6) ------

    [Fact]
    public async Task CreateAdmin_OnSystemTenant_Returns404_TenantNotFound_AndCreatesNoAccount()
    {
        var system = await CreateSystemTenantAsync();
        using var client = OperatorClient(system);
        var usersBefore = await db.CountAsync("SELECT count(*) FROM core.app_user");

        var response = await client.PostAsJsonAsync($"/api/v1/core/system/tenants/{system.TenantId}/admins", new
        {
            userName = "chiem-quyen",
            email = "chiem-quyen@system.example.com",
            fullName = "Chiếm quyền",
            tempPassword = Password,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe(TenantProvisioningErrors.NotFound.Code);
        (await db.CountAsync("SELECT count(*) FROM core.app_user")).ShouldBe(usersBefore);
    }

    [Fact]
    public async Task RecoveryReset_OnSystemTenant_Returns404_TenantNotFound()
    {
        var system = await CreateSystemTenantAsync();
        using var client = OperatorClient(system);

        var response = await client.PostAsJsonAsync($"/api/v1/core/system/tenants/{system.TenantId}/recovery-reset-password",
            new { userName = "superadmin", tempPassword = "New-Passw0rd-2" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe(TenantProvisioningErrors.NotFound.Code);
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private async Task<TenantProvisioningResult> CreateSystemTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var result = await ProvisioningInTransaction.RunAsync(scope.ServiceProvider, (s, ct) => s.CreateTenantAsync(
            new CreateTenantInput(
                Code: "SYSTEM",
                Name: "Hệ thống",
                IsSystem: true,
                AdminUserName: "superadmin",
                AdminPassword: Password,
                AdminHasPermissionBypass: false,
                AdminIsSystemOperator: true,
                AdminEmail: "superadmin@system.example.com",
                AdminFullName: "Vận hành hệ thống"),
            ct));

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private HttpClient OperatorClient(TenantProvisioningResult system)
    {
        var client = _factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-User", system.AdminUserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-UserName", "superadmin");
        client.DefaultRequestHeaders.Add("X-Test-Tenant", system.TenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Operator", bool.TrueString);

        var token = client.GetFromJsonAsync<JsonElement>("/api/v1/core/antiforgery/token", Json).GetAwaiter().GetResult()
            .GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        return client;
    }

    private static async Task<ApiEnvelope<JsonElement>> ReadEnvelope(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
}
