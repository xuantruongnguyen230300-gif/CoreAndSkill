using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// Luật S30 + ADR-0129 — qua host: CORS và lớp 1 chống giả mạo cho CÙNG một phán quyết với cùng một origin. Mỗi ca một host
// (bộ đếm rate limit sống theo host). Cổng 55697 không ai khai; host Development bỏ kho user-secrets của máy để danh sách
// origin không phụ thuộc máy chạy test.
public sealed class OriginAllowlistHostTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("Development", "https://localhost:55697", null, true)]
    [InlineData("Development", "https://evil.example", null, false)]
    [InlineData("Development", "https://localhost.evil.example", null, false)]
    [InlineData("Development", "http://localhost:55697", null, false)]
    [InlineData("Production", "https://localhost:55697", null, false)]
    [InlineData("Development", "https://app.example.com", "https://app.example.com", true)]
    public async Task CorsAndLayerOne_GiveTheSameVerdict_ForTheSameOrigin(
        string environment, string origin, string? listedOrigin, bool expectedAccepted)
    {
        var overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString,
        };
        if (listedOrigin is not null)
            overrides[CoreWebApplicationFactory.AllowedOriginsKey + ":0"] = listedOrigin;

        using var factory = new CoreWebApplicationFactory(overrides, environment: environment, ignoreUserSecrets: true);
        using var client = factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Phía CORS — quyết trang nào ĐỌC được thân của GET token.
        using var read = new HttpRequestMessage(HttpMethod.Get, "/api/v1/core/antiforgery/token");
        read.Headers.Add("Origin", origin);
        using var readResponse = await client.SendAsync(read);

        if (expectedAccepted)
            readResponse.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe([origin]);
        else
            readResponse.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();

        // Phía lớp 1 — quyết trang nào GHI được. Không token: lớp 1 nhận thì lớp 2 vẫn chặn (CSRF_REJECTED).
        using var write = new HttpRequestMessage(HttpMethod.Post, "/api/v1/core/auth/login")
        {
            Content = JsonContent.Create(new { tenantCode = "X", userName = "x", password = "x" }),
        };
        write.Headers.Add("Origin", origin);
        using var writeResponse = await client.SendAsync(write);
        var envelope = await writeResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        writeResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        envelope!.Error!.Code.ShouldBe(expectedAccepted ? "CORE.AUTH.CSRF_REJECTED" : "CORE.AUTH.ORIGIN_REJECTED");
    }

    // Đối chứng cho phép cô lập: host Development của test không còn nguồn user-secrets nào.
    [Fact]
    public void DevelopmentHost_OfThisTest_HasNoUserSecretsSource()
    {
        using var factory = new CoreWebApplicationFactory(
            new Dictionary<string, string?> { ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString },
            environment: "Development",
            ignoreUserSecrets: true);

        var root = (IConfigurationRoot)factory.Services.GetRequiredService<IConfiguration>();

        root.Providers.OfType<JsonConfigurationProvider>()
            .ShouldNotContain(provider => string.Equals(provider.Source.Path, "secrets.json", StringComparison.OrdinalIgnoreCase));
    }
}
