using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/roles.md.
[ApiController]
[Route(CoreRoutes.Roles)]
public sealed class RolesController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(CorePermissions.RoleRead)]
    public async Task<IActionResult> GetList([FromQuery] GetRolesListQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission(CorePermissions.RoleRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new GetRoleByIdQuery(id), ct));

    [HttpPost]
    [RequirePermission(CorePermissions.RoleWrite)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateRoleCommand(body.Name), ct);
        var location = result.IsSuccess ? $"/{CoreRoutes.Roles}/{result.Value}" : string.Empty;
        return HandleCreated(result, location);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(CorePermissions.RoleWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(new UpdateRoleCommand(id, body.Name, body.Version), ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission(CorePermissions.RoleWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new DeleteRoleCommand(id), ct));
}

public sealed record CreateRoleRequest(string Name);

public sealed record UpdateRoleRequest(string Name, string? Version);
