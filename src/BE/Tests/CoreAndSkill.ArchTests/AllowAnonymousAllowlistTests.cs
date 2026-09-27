using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// S4 — docs/RULES.md S4. Allowlist định nghĩa gốc: docs/quy-uoc/be-api-controller.md §5.
public class AllowAnonymousAllowlistTests
{
    [Fact]
    public void EveryAllowAnonymous_IsOn_TheAllowlist()
    {
        var offenders = AnonymousEndpointScanner.FindAnonymousEndpoints(ArchitectureFixture.WebAssembly)
            .Where(endpoint => !AnonymousEndpointAllowlist.Endpoints.Contains(endpoint))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 (phía BE) — docs/wiki-core/be/04-testing-strategy.md §3.4: detector duyệt một tập rỗng
    // luôn PASS. Không có test này, xoá toàn bộ nội dung FindAnonymousEndpoints (trả về [] vô
    // điều kiện) vẫn để test phía trên xanh mãi mãi.
    [Fact]
    public void EveryAllowAnonymous_IsOn_TheAllowlist_ScansAtLeastOneRealEndpoint()
    {
        var endpoints = AnonymousEndpointScanner.FindAnonymousEndpoints(ArchitectureFixture.WebAssembly);

        endpoints.ShouldNotBeEmpty();
    }

    // Detector_S4_Catches_RealViolation — một action [AllowAnonymous] không có tên trong allowlist
    // phải bị tính là offender.
    [Fact]
    public void Detector_S4_Catches_RealViolation()
    {
        var allowlist = new HashSet<string>(StringComparer.Ordinal) { "GET /api/v1/core/diagnostics/probe" };

        var offenders = AnonymousEndpointScanner
            .FindAnonymousEndpoints(typeof(NotOnAllowlistController).Assembly)
            .Where(endpoint => !allowlist.Contains(endpoint))
            .ToList();

        offenders.ShouldContain("GET /api/v1/core/fake/not-on-allowlist");
    }

    // Detector_S4_Ignores_EndpointOnAllowlist — action [AllowAnonymous] có tên trong allowlist
    // không được tính là offender (tái hiện đúng ca thật của DiagnosticsController.Probe).
    [Fact]
    public void Detector_S4_Ignores_EndpointOnAllowlist()
    {
        var allowlist = new HashSet<string>(StringComparer.Ordinal) { "GET /api/v1/core/diagnostics/probe" };

        var offenders = AnonymousEndpointScanner
            .FindAnonymousEndpoints(typeof(OnAllowlistController).Assembly)
            .Where(endpoint => !allowlist.Contains(endpoint))
            .ToList();

        offenders.ShouldNotContain("GET /api/v1/core/diagnostics/probe");
    }

    // Detector_S4_Ignores_ActionWithoutAllowAnonymous — action đòi phân quyền bình thường (không
    // [AllowAnonymous]) không bao giờ bị scanner thu thập, dù route trùng tên với một endpoint
    // không nằm trong allowlist nào. Thiếu test này, detector có thể dương tính giả cho MỌI action.
    [Fact]
    public void Detector_S4_Ignores_ActionWithoutAllowAnonymous()
    {
        var endpoints = AnonymousEndpointScanner.FindAnonymousEndpoints(typeof(AuthorizedController).Assembly);

        endpoints.ShouldNotContain("GET /api/v1/core/fake/authorized");
    }

    [ApiController]
    [Route("api/v1/core/fake")]
    private sealed class NotOnAllowlistController : ControllerBase
    {
        [HttpGet("not-on-allowlist")]
        [AllowAnonymous]
        public IActionResult Probe() => Ok();
    }

    [ApiController]
    [Route("api/v1/core/diagnostics")]
    private sealed class OnAllowlistController : ControllerBase
    {
        [HttpGet("probe")]
        [AllowAnonymous]
        public IActionResult Probe() => Ok();
    }

    [ApiController]
    [Route("api/v1/core/fake")]
    private sealed class AuthorizedController : ControllerBase
    {
        [HttpGet("authorized")]
        public IActionResult Probe() => Ok();
    }
}
