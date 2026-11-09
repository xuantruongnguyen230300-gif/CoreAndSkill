using System.Collections;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// docs/quy-uoc/be-api-controller.md §6.1, §6.2. Policy khai trong AddRateLimiter mà không endpoint nào gắn là
// một hàng rào CHỈ CÓ TRÊN GIẤY — cùng khuôn lỗi "khai mà không nối" của middleware
// (docs/wiki-core/be/ly-do/be-architecture.md §3.1). Luật A9 cho middleware CHƯA có cổng — trạng thái ở
// docs/RULES.md, dòng A9. Test 429 của giới hạn nền (SecurityPipelineTests) đi qua
// diagnostics/probe nên không bao giờ thấy được policy "login" có gắn hay không.
public sealed class RateLimitPolicyWiringTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Hàng rào 1 — policy "login" gắn vào action đăng nhập. Ngưỡng 5 lần/phút theo IP (§6.2): năm lần đầu
    // qua được tới validator (400 — thân rỗng, KHÔNG chạm DB), lần thứ sáu bị chặn ở UseRateLimiter.
    // Giới hạn nền là 200/phút nên 429 ở lần thứ sáu CHỈ có thể đến từ policy "login".
    // Host RIÊNG: bộ đếm của rate limiter sống theo host, dùng chung host với lớp khác là đếm lẫn.
    [Fact]
    public async Task Login_SixthAttemptFromSameIp_WithinWindow_Returns429_FromLoginPolicy()
    {
        await using var host = new InMemoryCoreHost();
        using var client = AnonymousClientWithCsrf(host);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var allowed = await PostEmptyLoginAsync(client);
            allowed.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"lần thử {attempt} phải tới được validator");
        }

        var rejected = await PostEmptyLoginAsync(client);
        var body = await rejected.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(body, Json)!;

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var retryAfter = rejected.Headers.GetValues("Retry-After").ShouldHaveSingleItem();
        envelope.Error!.Code.ShouldBe("CORE.RATE_LIMIT.EXCEEDED");
        // Cùng số giây với header — docs/contracts/auth.md §10, be-api-controller.md §6.3 ràng buộc 6.
        envelope.Error.MessageParams.ShouldNotBeNull()["RetryAfterSeconds"].ShouldBe(retryAfter);
    }

    // Giá trị cấu hình được tôn trọng: Core:RateLimit:LoginPermitLimit=2 thì lần thứ ba bị chặn (không phải lần thứ sáu
    // của mặc định 5) — chống ngưỡng bị đóng cứng trong code mà khoá cấu hình chỉ nằm trên giấy.
    [Fact]
    public async Task Login_ConfiguredPermitLimit_IsHonoured_ThirdAttemptReturns429()
    {
        await using var host = new InMemoryCoreHost(new Dictionary<string, string?> { ["Core:RateLimit:LoginPermitLimit"] = "2" });
        using var client = AnonymousClientWithCsrf(host);

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var allowed = await PostEmptyLoginAsync(client);
            allowed.StatusCode.ShouldBe(HttpStatusCode.BadRequest, $"lần thử {attempt} phải tới được validator");
        }

        var rejected = await PostEmptyLoginAsync(client);
        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await rejected.Content.ReadAsStringAsync(), Json)!;

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var retryAfter = rejected.Headers.GetValues("Retry-After").ShouldHaveSingleItem();
        envelope.Error!.Code.ShouldBe("CORE.RATE_LIMIT.EXCEEDED");
        envelope.Error.MessageParams.ShouldNotBeNull()["RetryAfterSeconds"].ShouldBe(retryAfter);
    }

    // Chiều ngược: MỌI policy khai trong AddRateLimiter đều có ít nhất một endpoint dùng nó. Chiều xuôi
    // (endpoint trỏ tới policy không khai) framework tự ném lúc chạy — chiều này thì không ai báo.
    [Fact]
    public void EveryRateLimitPolicy_DeclaredInAddRateLimiter_IsUsedByAtLeastOneEndpoint()
    {
        using var factory = new CoreWebApplicationFactory();
        _ = factory.Server; // dựng host để bảng định tuyến có mặt

        var declared = RateLimiterPolicyNames.Read(
            factory.Services.GetRequiredService<IOptions<RateLimiterOptions>>().Value);

        // Chống test rỗng: bộ đọc thấy được policy nào thì mới có gì để so.
        declared.ShouldContain("client-errors");

        var used = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(e => e.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        declared.Where(name => !used.Contains(name)).ShouldBeEmpty(
            "policy rate limit khai mà không endpoint nào gắn [EnableRateLimiting]/RequireRateLimiting");
    }

    // Đối chứng cho bộ đọc (docs/quy-uoc/be-architecture.md §9.3): cả hai overload AddPolicy đều được thấy.
    // Bộ đọc đi qua thành phần KHÔNG công khai của framework — đổi bản framework mà tên đổi thì test này đỏ
    // thay vì bộ kiểm ở trên xanh rỗng.
    [Fact]
    public void RateLimiterPolicyNames_Read_SeesBothAddPolicyOverloads()
    {
        var options = new RateLimiterOptions();
        options.AddPolicy("theo-ham", _ => RateLimitPartition.GetNoLimiter("x"));
        options.AddPolicy<string, ProbePolicy>("theo-kieu");

        RateLimiterPolicyNames.Read(options).ShouldBe(["theo-ham", "theo-kieu"], ignoreOrder: true);
    }

    // ---- Nợ B26 — docs/DEBT.md, docs/quy-uoc/be-architecture.md §3.1 ràng buộc 5, be-api-controller.md
    // §6.1 hàng 2, docs/contracts/auth.md §10. UseRateLimiter phải đứng NGAY SAU UseCors, TRƯỚC
    // UseAuthentication — nên giới hạn nền đếm cả request bị 401/403 ở ba lớp chặn phía sau nó. Bốn test
    // dưới đây, mỗi test một host RIÊNG (bộ đếm sống theo host — cùng lý do host riêng ở test đăng nhập
    // phía trên). Ngưỡng 200/phút (GlobalPermitLimit, CoreWebServiceCollectionExtensions.cs) nên vòng lặp
    // chạy tối đa 201 lần, dừng ngay khi gặp 429 — cùng khuôn SecurityPipelineTests.GlobalRateLimit_Exceeds…

    // UseRateLimiter đứng ngay sau UseCors, trước UseAuthentication: 401 phát ra ở xác thực/UseAuthorization, SAU
    // khi request đã qua UseRateLimiter — bộ đếm thấy mọi lượt, vòng lặp gặp 429 trong tối đa 201 lần; nếu
    // UseRateLimiter đứng sau lớp chặn, bộ đếm không thấy lượt 401, không có 429 và test đỏ. Đó là canary của B26.
    [Fact]
    public async Task GlobalRateLimit_CountsUnauthenticated401_ThenReturns429()
    {
        using var factory = new CoreWebApplicationFactory();
        using var client = factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage? last = null;
        for (var attempt = 1; attempt <= 201; attempt++)
        {
            last = await client.GetAsync("/api/v1/core/users");
            if (last.StatusCode == HttpStatusCode.TooManyRequests)
                break;

            last.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"lượt {attempt} chưa có phiên nào, phải là 401");
        }

        last!.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests,
            "giới hạn nền phải đếm cả request bị 401 ở xác thực — be-architecture.md §3.1 ràng buộc 5");
        var body = await last.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(Json);
        body!.Error!.Code.ShouldBe("CORE.RATE_LIMIT.EXCEEDED");
    }

    // 403 CSRF_REJECTED phát ra ở AntiforgeryValidationMiddleware, SAU UseRateLimiter nên bộ đếm thấy lượt này;
    // UseRateLimiter đứng sau middleware đó thì không đếm được và test đỏ.
    [Fact]
    public async Task GlobalRateLimit_CountsCsrfRejected403_ThenReturns429()
    {
        await using var host = new InMemoryCoreHost();
        using var client = host.CreateClient(userId: Guid.NewGuid(), withCsrf: false);

        HttpResponseMessage? last = null;
        for (var attempt = 1; attempt <= 201; attempt++)
        {
            last = await client.PostAsJsonAsync("/api/v1/core/users",
                new { userName = "x", email = "x@vd.vn", fullName = "X", tempPassword = "x", roleIds = Array.Empty<Guid>() });
            if (last.StatusCode == HttpStatusCode.TooManyRequests)
                break;

            var envelope = await last.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(Json);
            last.StatusCode.ShouldBe(HttpStatusCode.Forbidden, $"lượt {attempt} thiếu token CSRF, phải bị chặn trước khi chạm quyền");
            envelope!.Error!.Code.ShouldBe("CORE.AUTH.CSRF_REJECTED");
        }

        last!.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests,
            "giới hạn nền phải đếm cả request bị 403 CSRF_REJECTED — be-architecture.md §3.1 ràng buộc 5");
        var body = await last.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(Json);
        body!.Error!.Code.ShouldBe("CORE.RATE_LIMIT.EXCEEDED");
    }

    // 403 PASSWORD_CHANGE_REQUIRED phát ra ở PasswordChangeRequiredMiddleware, SAU UseRateLimiter nên bộ đếm thấy
    // lượt này; UseRateLimiter đứng sau middleware đó thì test đỏ. Endpoint ngoài bốn đường allowlist của contracts/auth.md §1.2.
    [Fact]
    public async Task GlobalRateLimit_CountsPasswordChangeRequired403_ThenReturns429()
    {
        await using var host = new InMemoryCoreHost();
        using var client = host.CreateClient(userId: Guid.NewGuid(), withCsrf: false);
        client.DefaultRequestHeaders.Add("X-Test-MustChangePassword", "true");

        HttpResponseMessage? last = null;
        for (var attempt = 1; attempt <= 201; attempt++)
        {
            last = await client.GetAsync("/api/v1/core/users");
            if (last.StatusCode == HttpStatusCode.TooManyRequests)
                break;

            var envelope = await last.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(Json);
            last.StatusCode.ShouldBe(HttpStatusCode.Forbidden, $"lượt {attempt} mang cờ buộc đổi mật khẩu, endpoint ngoài allowlist §1.2");
            envelope!.Error!.Code.ShouldBe("CORE.AUTH.PASSWORD_CHANGE_REQUIRED");
        }

        last!.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests,
            "giới hạn nền phải đếm cả request bị 403 PASSWORD_CHANGE_REQUIRED — be-architecture.md §3.1 ràng buộc 5");
        var body = await last.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(Json);
        body!.Error!.Code.ShouldBe("CORE.RATE_LIMIT.EXCEEDED");
    }

    // Hệ quả (a) phải giữ dù đổi vị trí: 429 vẫn đứng SAU UseCors nên vẫn mang header CORS cho nguồn trong
    // allowlist — contracts/auth.md §10, be-architecture.md §3.1 ràng buộc 5(a). KHÔNG phải canary — có
    // thể đã xanh trên code hôm nay, vì UseCors đứng trước UseRateLimiter ở CẢ hai vị trí.
    [Fact]
    public async Task GlobalRateLimit_Rejection_CarriesCorsHeaders_ForAnAllowedOrigin()
    {
        using var factory = new CoreWebApplicationFactory();
        using var client = factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage? last = null;
        for (var attempt = 1; attempt <= 201; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/core/diagnostics/probe");
            request.Headers.Add("Origin", CoreWebApplicationFactory.TestAllowedOrigin);
            last = await client.SendAsync(request);
            if (last.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        last!.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        last.Headers.GetValues("Access-Control-Allow-Origin").ShouldContain(CoreWebApplicationFactory.TestAllowedOrigin);
    }

    private static Task<HttpResponseMessage> PostEmptyLoginAsync(HttpClient client)
        => client.PostAsync("/api/v1/core/auth/login", new StringContent(
            """{"tenantCode":"","userName":"","password":""}""", Encoding.UTF8, "application/json"));

    private static HttpClient AnonymousClientWithCsrf(InMemoryCoreHost host)
    {
        var client = host.CreateClient(userId: null);
        var response = client.GetAsync("/api/v1/core/antiforgery/token").GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var token = response.Content.ReadFromJsonAsync<JsonElement>(Json).GetAwaiter().GetResult()
            .GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        return client;
    }

    private sealed class ProbePolicy : IRateLimiterPolicy<string>
    {
        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

        public RateLimitPartition<string> GetPartition(HttpContext httpContext) => RateLimitPartition.GetNoLimiter("x");
    }

    // RateLimiterOptions không công khai danh sách policy đã khai. Gom khoá của MỌI từ điển khoá chuỗi không
    // công khai — không dựa vào tên thành viên cụ thể nào, để một lần đổi tên trong framework không làm bộ
    // đọc mù im lặng (test đối chứng ở trên bắt ca bộ đọc không còn thấy gì).
    private static class RateLimiterPolicyNames
    {
        public static IReadOnlySet<string> Read(RateLimiterOptions options)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var values = typeof(RateLimiterOptions).GetFields(Flags).Select(f => f.GetValue(options))
                .Concat(typeof(RateLimiterOptions).GetProperties(Flags)
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .Select(p => p.GetValue(options)));

            foreach (var value in values)
            {
                if (value is IDictionary dictionary)
                {
                    foreach (var key in dictionary.Keys)
                    {
                        if (key is string name)
                            names.Add(name);
                    }
                }
            }

            return names;
        }
    }
}
