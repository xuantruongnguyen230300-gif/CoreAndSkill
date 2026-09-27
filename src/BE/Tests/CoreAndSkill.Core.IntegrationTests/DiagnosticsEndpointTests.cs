using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests;

// Endpoint thử của B0 — docs/wiki-core/be/trien-khai/01-b0-nen-mong.md §4, docs/contracts/diagnostics.md. Endpoint chỉ
// có NGOÀI Production, nên host ở đây chạy Development; hành vi ở Production nằm ở DiagnosticsProbeProductionTests.
public sealed class DiagnosticsEndpointTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CoreWebApplicationFactory _factory = new(environment: "Development");
    private readonly HttpClient _client;

    public DiagnosticsEndpointTests()
        => _client = _factory.CreateHttpsClient();

    [Fact]
    public async Task Probe_DefaultOutcome_Returns200_WithSuccessEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/core/diagnostics/probe");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        envelope.ShouldNotBeNull();
        envelope.Success.ShouldBeTrue();
        envelope.Error.ShouldBeNull();
        envelope.TraceId.ShouldNotBeNullOrWhiteSpace();
        envelope.Data.GetProperty("message").GetString().ShouldBe("pong");
    }

    [Fact]
    public async Task Probe_OutcomeFailure_Returns422_WithBusinessRuleEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/core/diagnostics/probe?outcome=failure");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        // 422 — ErrorType.BusinessRule, be-api-controller.md §1.1. KHÔNG khẳng định "khác 200" (T5).
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        envelope.ShouldNotBeNull();
        envelope.Success.ShouldBeFalse();
        envelope.Error.ShouldNotBeNull();
        envelope.Error.Code.ShouldBe("CORE.DIAGNOSTICS.PROBE_FAILURE_REQUESTED");
        envelope.Error.Type.ShouldBe("BusinessRule");
        envelope.TraceId.ShouldNotBeNullOrWhiteSpace();
    }

    // Luật A15 (docs/RULES.md §3): IExceptionHandler của Core đã đăng ký VÀ UseExceptionHandler() đã nối — exception ngoài
    // dự kiến ra đúng 500, mang envelope có code, KHÔNG lộ stack trace.
    [Fact]
    public async Task Probe_OutcomeException_Returns500_WithUnexpectedEnvelope_NoStackTrace()
    {
        var response = await _client.GetAsync("/api/v1/core/diagnostics/probe?outcome=exception");
        var body = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(body, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        envelope.ShouldNotBeNull();
        envelope.Success.ShouldBeFalse();
        envelope.Error.ShouldNotBeNull();
        envelope.Error.Code.ShouldBe("CORE.SYSTEM.UNEXPECTED");
        envelope.Error.Type.ShouldBe("Unexpected");
        body.ShouldNotContain("InvalidOperationException");
        body.ShouldNotContain("   at CoreAndSkill");
    }

    [Fact]
    public async Task Probe_TraceParentHeader_IsEchoedBack_AsTraceId()
    {
        const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/core/diagnostics/probe");
        request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

        var response = await _client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        envelope.ShouldNotBeNull();
        envelope.TraceId.ShouldBe(traceId);
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404_WithEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/core/does-not-exist");
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        envelope.ShouldNotBeNull();
        envelope.Error.ShouldNotBeNull();
        envelope.Error.Code.ShouldBe("CORE.ROUTE.NOT_FOUND");
    }

    // GET là method AN TOÀN — AntiforgeryValidationMiddleware bỏ qua hoàn toàn (§7.2 bước 1), nên
    // ca này cô lập đúng hành vi định tuyến "wrong verb" mà không chạm CSRF/DataProtection. Ngược
    // lại (POST vào route chỉ nhận GET) sẽ CHẠM CSRF trước khi tới routing — B2, xem
    // Security/SecurityPipelineTests cho ca đó.
    [Fact]
    public async Task WrongVerbOnKnownApiRoute_Returns405_WithEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/core/auth/login"); // route chỉ khai [HttpPost]

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        envelope.ShouldNotBeNull();
        envelope.Error.ShouldNotBeNull();
        envelope.Error.Code.ShouldBe("CORE.ROUTE.METHOD_NOT_ALLOWED");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
