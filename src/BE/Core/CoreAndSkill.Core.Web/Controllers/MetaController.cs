using CoreAndSkill.Core.Application.Menu;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/meta-menu.md §1.
[ApiController]
[Route(CoreRoutes.Meta)]
public sealed class MetaController(ISender mediator) : ApiControllerBase
{
    [HttpGet("menu")]
    [AuthenticatedOnly("Menu đã lọc theo tập quyền của chính người gọi — lọc là phân quyền của endpoint này")]
    public async Task<IActionResult> GetMenu(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetMenuQuery(), ct));
}
