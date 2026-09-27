using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// B21 (docs/DEBT.md) — biên của phép làm tròn trên đường 429 qua IExceptionHandler (hàng rào 3). Luật:
// docs/quy-uoc/be-api-controller.md §6.5, hàng "Retry-After là số nguyên giây, làm tròn lên từ RetryAfter"; header và
// messageParams.RetryAfterSeconds mang cùng số (§6.3 ràng buộc 6, docs/contracts/auth.md §10).
//
// LoginAttemptLimitResponseTests kiểm 47,2 giây → 48. Một ca không nguyên đứng một mình không phân biệt được "làm tròn
// lên" với "cắt phần lẻ rồi cộng một": cả hai cho 48. Hai ca ở đây tách chúng — một ca đúng số nguyên (không được cộng
// thêm), một ca dưới một giây (không được về 0, vì Retry-After: 0 là bảo client thử lại ngay khi bộ đếm vẫn đang chặn).
//
// Pipeline HTTP thật; chỉ seam ILoginAttemptLimiter được thay (luật T8), và nó ném trước khi handler chạm seam nào cần DB.
public sealed class LoginAttemptLimitRetryAfterRoundingTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(47_000, "47")]
    [InlineData(200, "1")]
    public async Task LoginPartitionLimitExceeded_RoundsRetryAfterUp_InBothHeaderAndMessageParams(
        int retryAfterMilliseconds, string expectedSeconds)
    {
        await using var host = new InMemoryCoreHost(configure: services =>
        {
            services.RemoveAll<ILoginAttemptLimiter>();
            services.AddSingleton<ILoginAttemptLimiter>(
                new ExhaustedLimiter(TimeSpan.FromMilliseconds(retryAfterMilliseconds)));
        });
        using var client = AnonymousClientWithCsrf(host);

        using var response = await client.PostAsync("/api/v1/core/auth/login", new StringContent(
            """{"tenantCode":"DEMO","userName":"an.nv","password":"mat-khau-bat-ky"}""", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Headers.GetValues("Retry-After").ShouldHaveSingleItem().ShouldBe(expectedSeconds);

        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
        envelope.Error!.MessageParams.ShouldNotBeNull()["RetryAfterSeconds"].ShouldBe(expectedSeconds);
    }

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

    private sealed class ExhaustedLimiter(TimeSpan retryAfter) : ILoginAttemptLimiter
    {
        public ValueTask EnsureAttemptAllowedAsync(string tenantCode, string userName, CancellationToken ct)
            => throw new LoginAttemptLimitExceededException(retryAfter);
    }
}
