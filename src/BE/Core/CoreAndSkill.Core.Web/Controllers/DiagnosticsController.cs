using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// Endpoint thử của B0 — docs/wiki-core/be/trien-khai/01-b0-nen-mong.md §2 bước 4,
// docs/contracts/diagnostics.md. Không phải nghiệp vụ: chứng minh cầu nối Result → HTTP
// và IExceptionHandler hoạt động trước khi CQRS/Identity (B1) tồn tại.
[ApiController]
[Route(CoreRoutes.Diagnostics)]
public sealed class DiagnosticsController(TimeProvider timeProvider) : ApiControllerBase
{
    [HttpGet("probe")]
    [AllowAnonymous]
    public IActionResult Probe([FromQuery] string? outcome = null)
        => outcome switch
        {
            "failure" => HandleResult(DiagnosticsProbe.Fail()),
            "exception" => throw new InvalidOperationException(
                "Ngoại lệ mô phỏng cho endpoint thử của B0 (outcome=exception)."),
            _ => HandleResult(DiagnosticsProbe.Succeed(timeProvider)),
        };
}
