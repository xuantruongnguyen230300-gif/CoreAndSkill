using System.Net;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Luật S29, ADR-0118 quyết định 2: tài liệu OpenAPI và trang Scalar chỉ ở Development; mọi môi trường khác không
// đăng ký, không map — hai đường ra 404 như đường không tồn tại. Cặp test: vế Development chống xanh rỗng (T6) —
// thiếu nó thì vế Production xanh cả khi OpenAPI chưa từng được nối.
public sealed class OpenApiDocumentTests
{
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    [InlineData("/scalar/v1")]
    public async Task OpenApiDocument_IsNotServedAnonymously_InProduction(string path)
    {
        using var factory = new CoreWebApplicationFactory(); // mặc định Production
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OpenApiDocument_IsServed_InDevelopment()
    {
        using var factory = new CoreWebApplicationFactory(environment: "Development");
        using var client = factory.CreateHttpsClient();

        var document = await client.GetAsync("/openapi/v1.json");
        var page = await client.GetAsync("/scalar");

        document.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await document.Content.ReadAsStringAsync()).ShouldContain("/api/v1/core/auth/login");
        page.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
