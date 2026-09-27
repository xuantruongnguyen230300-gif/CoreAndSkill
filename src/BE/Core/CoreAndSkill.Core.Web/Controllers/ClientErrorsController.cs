using CoreAndSkill.Core.Application.ClientErrors;
using CoreAndSkill.Core.Web.Http;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/client-errors.md. [AllowAnonymous] — lỗi ở màn đăng nhập cũng phải báo được;
// khai trong AnonymousEndpointAllowlist (Security/). CSRF vẫn áp như mọi POST — không có ngoại lệ
// theo endpoint (README.md §9); antiforgery middleware không đọc [AllowAnonymous].
[ApiController]
[Route(CoreRoutes.ClientErrors)]
[AllowAnonymous]
[EnableRateLimiting("client-errors")]
public sealed class ClientErrorsController(ISender mediator) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Report([FromBody] ReportClientErrorRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(
            new ReportClientErrorCommand(body.Kind, body.Message, body.Stack, body.DuongDan, body.TraceId, body.PhienBanApp),
            ct));
}

public sealed record ReportClientErrorRequest(
    string Kind, string Message, string? Stack, string DuongDan, string? TraceId, string PhienBanApp);
