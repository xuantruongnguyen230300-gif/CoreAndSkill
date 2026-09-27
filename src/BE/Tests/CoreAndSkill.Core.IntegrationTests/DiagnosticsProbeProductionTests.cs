using System.Net;
using System.Text.Json;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests;

// docs/contracts/diagnostics.md: GET /api/v1/core/diagnostics/probe chỉ hoạt động NGOÀI Production. Ở Production nó trả 404
// như một tuyến không tồn tại — cùng mã, cùng hình dạng với tuyến bịa ra — chứ không phải một 404 riêng để lộ rằng
// endpoint có ở đó. Nhánh outcome=exception là lý do chính: một endpoint ẩn danh ai cũng gọi được để sinh 500 và một
// dòng log lỗi mỗi lần.
public sealed class DiagnosticsProbeProductionTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("")]
    [InlineData("?outcome=failure")]
    [InlineData("?outcome=exception")]
    public async Task InProduction_Probe_Is404_ExactlyLikeARouteThatDoesNotExist(string query)
    {
        using var factory = new CoreWebApplicationFactory(); // mặc định Production
        using var client = factory.CreateHttpsClient();

        var probe = await client.GetAsync($"/api/v1/core/diagnostics/probe{query}");
        var unknown = await client.GetAsync("/api/v1/core/diagnostics/khong-ton-tai");

        probe.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CodeOf(probe)).ShouldBe(await CodeOf(unknown));
        (await CodeOf(probe)).ShouldBe("CORE.ROUTE.NOT_FOUND");
    }

    // Ngoài Production — không chỉ Development: mọi môi trường không phải Production vẫn có endpoint.
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public async Task OutsideProduction_Probe_StillAnswers(string environment)
    {
        using var factory = new CoreWebApplicationFactory(environment: environment);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/core/diagnostics/probe");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), JsonOptions)?.Error?.Code;
}
