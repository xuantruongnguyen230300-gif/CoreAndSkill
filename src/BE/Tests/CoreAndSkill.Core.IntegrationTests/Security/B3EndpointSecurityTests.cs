using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// Endpoint mới của B3 (docs/contracts/tenants.md, docs/contracts/client-errors.md) — cùng khuôn
// SecurityPipelineTests: CoreWebApplicationFactory dùng chuỗi kết nối KHÔNG TỚI ĐƯỢC, mọi test ở
// đây PHẢI pass/fail ở lớp middleware, TRƯỚC khi request chạm DB.
public sealed class B3EndpointSecurityTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CoreWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public B3EndpointSecurityTests()
        => _client = _factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // docs/contracts/tenants.md — [RequireSystemOperator] đứng SAU [Authorize] của ApiControllerBase;
    // chưa đăng nhập thì trượt ở [Authorize] trước, cùng hành vi 401 sạch của mọi endpoint có bảo vệ.
    [Fact]
    public async Task TenantsList_Unauthenticated_Returns401_CleanJson_NoRedirect()
    {
        var response = await _client.GetAsync("/api/v1/core/system/tenants");
        var body = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(body, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.Location.ShouldBeNull();
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.NOT_AUTHENTICATED");
        body.ShouldNotContain("<html", Case.Insensitive);
    }

    [Fact]
    public async Task TenantsCreate_Unauthenticated_Returns401_NotCsrfRejected()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/core/system/tenants",
            new { code = "X", name = "X", adminUserName = "x", adminEmail = "x@vd.vn", adminFullName = "X", adminTempPassword = "x" });
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.NOT_AUTHENTICATED");
    }

    // docs/contracts/client-errors.md "Ghi chú" — CSRF vẫn áp như mọi POST, KHÔNG có ngoại lệ theo
    // endpoint dù [AllowAnonymous]. Antiforgery chạy TRƯỚC UseAuthorization trong pipeline nên đây
    // là request DUY NHẤT đi qua được [AllowAnonymous] rồi mới trượt ở lớp CSRF.
    [Fact]
    public async Task ClientErrors_AllowAnonymous_ButMissingCsrfHeader_Returns403_CsrfRejected()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/core/client-errors",
            new { kind = "TypeError", message = "lỗi", duongDan = "/trang", phienBanApp = "2026.09.10-a1" });
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.CSRF_REJECTED");
    }

    [Fact]
    public async Task ClientErrors_OriginNotInAllowlist_Returns403_OriginRejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/core/client-errors")
        {
            Content = JsonContent.Create(new { kind = "TypeError", message = "lỗi", duongDan = "/trang", phienBanApp = "2026.09.10-a1" }),
        };
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await _client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.ORIGIN_REJECTED");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
