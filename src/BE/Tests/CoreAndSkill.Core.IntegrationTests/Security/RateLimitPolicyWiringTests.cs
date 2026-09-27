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
