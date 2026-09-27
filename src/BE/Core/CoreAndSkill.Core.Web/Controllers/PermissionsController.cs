using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/permissions.md.
[ApiController]
[Route(CoreRoutes.Permissions)]
public sealed class PermissionsController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(CorePermissions.PermissionRead)]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetPermissionsQuery(), ct));

    [HttpGet("matrix")]
    [RequirePermission(CorePermissions.PermissionRead)]
    public async Task<IActionResult> GetMatrix(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetPermissionMatrixQuery(), ct));

    [HttpGet("matrix/by-resource")]
    [RequirePermission(CorePermissions.PermissionRead)]
    public async Task<IActionResult> GetMatrixByResource(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetPermissionMatrixByResourceQuery(), ct));

    [HttpPut("matrix")]
    [RequirePermission(CorePermissions.PermissionWrite)]
    public async Task<IActionResult> UpdateMatrix([FromBody] UpdatePermissionMatrixRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(new UpdatePermissionMatrixCommand(body.Version, body.Entries), ct));
}

public sealed record UpdatePermissionMatrixRequest(string? Version, IReadOnlyList<UpdatePermissionMatrixEntry>? Entries);
