using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/auth.md §2. KHÔNG rate-limit — siết endpoint này là chặn đúng cái FE cần để gửi
// được request hợp lệ (docs/quy-uoc/be-api-controller.md §6.3).
[ApiController]
[Route(CoreRoutes.Antiforgery)]
public sealed class AntiforgeryController(IAntiforgery antiforgery) : ApiControllerBase
{
    [HttpGet(CoreRoutes.AntiforgeryToken)]
    [AllowAnonymous]
    [DisableRateLimiting]
    public IActionResult GetToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return HandleResult(Result.Success(new AntiforgeryTokenDto(tokens.RequestToken!)));
    }
}

public sealed record AntiforgeryTokenDto(string Token);
