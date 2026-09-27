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

// Hàng rào 3 (theo LoginPartitionKey) ra 429 qua IExceptionHandler — docs/quy-uoc/be-api-controller.md §6.5, §6.3 ràng buộc
// 6; docs/contracts/auth.md §10 "Response 429". Header Retry-After và messageParams.RetryAfterSeconds phải mang CÙNG số
// giây, làm tròn lên từ RetryAfter của chính bộ đếm đã chặn. 47,2 giây: không tròn, và khác 60 — nên phép làm tròn và việc
// "không giả định 60" cùng bị kiểm. Pipeline HTTP thật; chỉ seam ILoginAttemptLimiter được thay, và nó ném trước khi
// handler chạm tới seam nào cần DB.
public sealed class LoginAttemptLimitResponseTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task LoginPartitionLimitExceeded_Returns429_WithRetryAfterSecondsEqualToTheHeader()
    {
        await using var host = new InMemoryCoreHost(configure: services =>
        {
            services.RemoveAll<ILoginAttemptLimiter>();
            services.AddSingleton<ILoginAttemptLimiter>(new ExhaustedLimiter(TimeSpan.FromSeconds(47.2)));
        });
        using var client = AnonymousClientWithCsrf(host);

        using var response = await client.PostAsync("/api/v1/core/auth/login", new StringContent(
            """{"tenantCode":"DEMO","userName":"an.nv","password":"mat-khau-bat-ky"}""", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var retryAfter = response.Headers.GetValues("Retry-After").ShouldHaveSingleItem();
        retryAfter.ShouldBe("48");

        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
        envelope.Error!.Code.ShouldBe("CORE.RATE_LIMIT.EXCEEDED");
        var messageParams = envelope.Error.MessageParams.ShouldNotBeNull();
        messageParams.Keys.ShouldBe(["RetryAfterSeconds"]);
        messageParams["RetryAfterSeconds"].ShouldBe(retryAfter);
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

    // Bộ đếm đã cạn: mọi lượt thử đều bị chặn với cùng RetryAfter.
    private sealed class ExhaustedLimiter(TimeSpan retryAfter) : ILoginAttemptLimiter
    {
        public ValueTask EnsureAttemptAllowedAsync(string tenantCode, string userName, CancellationToken ct)
            => throw new LoginAttemptLimitExceededException(retryAfter);
    }
}
