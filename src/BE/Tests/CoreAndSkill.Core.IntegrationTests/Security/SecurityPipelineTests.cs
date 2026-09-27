using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// Lớp biên B2 — docs/wiki-core/be/trien-khai/03-b2-phan-quyen-va-bien.md, nghiệm thu #4, #5, #6,
// #8, #9. CoreWebApplicationFactory dùng chuỗi kết nối KHÔNG TỚI ĐƯỢC (Support/CoreWebApplicationFactory.cs)
// — mọi test ở đây phải PASS/FAIL ở lớp middleware, TRƯỚC khi request chạm DB.
public sealed class SecurityPipelineTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CoreWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public SecurityPipelineTests()
        => _client = _factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // #4 — request ghi thiếu header chống CSRF → 403, đúng mã.
    [Fact]
    public async Task Login_MissingCsrfHeader_Returns403_CsrfRejected()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/core/auth/login",
            new { tenantCode = "X", userName = "x", password = "x" });
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.CSRF_REJECTED");
    }

    // #4 (biến thể) — Origin trong allowlist NHƯNG vẫn thiếu token: lớp 2 vẫn chặn độc lập với lớp 1.
    [Fact]
    public async Task Login_AllowedOrigin_MissingCsrfHeader_StillReturns403_CsrfRejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/core/auth/login")
        {
            Content = JsonContent.Create(new { tenantCode = "X", userName = "x", password = "x" }),
        };
        request.Headers.Add("Origin", CoreWebApplicationFactory.TestAllowedOrigin); // nằm trong allowlist của host thử

        var response = await _client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.CSRF_REJECTED");
    }

    // #5 — Origin ngoài allowlist, KỂ CẢ khi không có token (lớp 1 chặn trước, không tới lớp 2) →
    // đúng mã ORIGIN_REJECTED, không lẫn với CSRF_REJECTED.
    [Fact]
    public async Task Login_OriginNotInAllowlist_Returns403_OriginRejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/core/auth/login")
        {
            Content = JsonContent.Create(new { tenantCode = "X", userName = "x", password = "x" }),
        };
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await _client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.ORIGIN_REJECTED");
    }

    // #2 — chưa đăng nhập gọi endpoint đòi quyền → 401 JSON sạch, KHÔNG 302 redirect.
    [Fact]
    public async Task ProtectedEndpoint_Unauthenticated_Returns401_CleanJson_NoRedirect()
    {
        var response = await _client.GetAsync("/api/v1/core/users");
        var body = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(body, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.Location.ShouldBeNull();
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.NOT_AUTHENTICATED");
        body.ShouldNotContain("<html", Case.Insensitive);
    }

    // #6 — phiên hết hạn / không có phiên gọi endpoint đòi quyền → 401, KHÔNG 403 CSRF, kể cả khi
    // request thiếu token chống CSRF (xác thực chạy TRƯỚC antiforgery — auth.md §1,
    // be-architecture.md §3.1).
    [Fact]
    public async Task WriteEndpoint_NoSession_Returns401_NotCsrfRejected()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/core/users",
            new { userName = "x", email = "x@vd.vn", fullName = "X", tempPassword = "x", roleIds = Array.Empty<Guid>() });
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        envelope!.Error!.Code.ShouldBe("CORE.AUTH.NOT_AUTHENTICATED");
    }

    // #8 — CORS: nguồn ngoài allowlist không nhận Access-Control-Allow-Origin (trình duyệt sẽ tự
    // chặn đọc phản hồi); nguồn trong allowlist nhận đúng header, kèm Allow-Credentials cho cookie.
    [Fact]
    public async Task Cors_DisallowedOrigin_ResponseHasNoAccessControlAllowOriginHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/core/antiforgery/token");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await _client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Cors_AllowedOrigin_ResponseHasMatchingAccessControlHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/core/antiforgery/token");
        request.Headers.Add("Origin", CoreWebApplicationFactory.TestAllowedOrigin);

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldContain(CoreWebApplicationFactory.TestAllowedOrigin);
        response.Headers.GetValues("Access-Control-Allow-Credentials").ShouldContain("true");
    }

    // #9 — vượt ngưỡng rate limit → 429 kèm Retry-After, ngưỡng không lộ trong thông báo. Dùng
    // hàng rào 2 (giới hạn NỀN, 200/phút theo IP, be-api-controller.md §6.1) qua một endpoint GET
    // ẩn danh KHÔNG cần antiforgery/DataProtection — antiforgery.GetAndStoreTokens cần một key ring
    // DataProtection hoạt động (persist qua DB), nên hàng rào 1 ("login", cần token hợp lệ trước)
    // không kiểm được trong môi trường này (docs/wiki-core/be/04-testing-strategy.md §4 — Testcontainers
    // Postgres chưa dựng ở B0/B1). Diagnostics probe không rate-limit-exempt và không chạm DB. Host này chạy
    // Production nên probe không có tuyến (docs/contracts/diagnostics.md) và mỗi lần gọi là một 404 — giới hạn nền
    // vẫn đếm nó: GlobalLimiter áp cho mọi request đi qua UseRateLimiter, không phụ thuộc tuyến có tồn tại hay không.
    [Fact]
    public async Task GlobalRateLimit_ExceedsThreshold_Returns429_WithRetryAfter_AndDoesNotLeakThreshold()
    {
        HttpResponseMessage? last = null;
        for (var i = 0; i < 201; i++)
        {
            last = await _client.GetAsync("/api/v1/core/diagnostics/probe");
            if (last.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        last!.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var retryAfter = last.Headers.GetValues("Retry-After").ShouldHaveSingleItem();

        var body = await last.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(body, JsonOptions);
        envelope!.Error!.Code.ShouldBe("CORE.RATE_LIMIT.EXCEEDED");
        // messageParams mang ĐÚNG MỘT khoá — RetryAfterSeconds, cùng số giây với header (docs/contracts/auth.md §10; màn tắt
        // toast chỉ có envelope trong tay). Không khoá nào khác: ngưỡng KHÔNG lộ (be-api-controller.md §6.3 ràng buộc 5, 6).
        var messageParams = envelope.Error.MessageParams.ShouldNotBeNull();
        messageParams.Keys.ShouldBe(["RetryAfterSeconds"]);
        messageParams["RetryAfterSeconds"].ShouldBe(retryAfter);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
