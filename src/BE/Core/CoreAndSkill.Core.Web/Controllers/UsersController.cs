using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using CoreAndSkill.Core.Web.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/users.md.
[ApiController]
[Route(CoreRoutes.Users)]
public sealed class UsersController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(CorePermissions.UserRead)]
    public async Task<IActionResult> GetList([FromQuery] GetUsersListQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    // docs/contracts/exports.md §1 — quyền xuất là quyền RIÊNG, không dùng lại quyền đọc. Nhánh lỗi là
    // envelope; nhánh thành công là chính tệp, ghi thẳng ra luồng phản hồi. Cả hai do HandleExport dựng
    // (ADR-0064) — controller không tự dựng IActionResult nào, kể cả của Core.
    // [RequireAntiforgery]: GET này ghi nhật ký kiểm toán nên kiểm X-XSRF-TOKEN như lệnh ghi (ADR-0062, luật S20).
    [HttpGet("export")]
    [RequireAntiforgery]
    [RequirePermission(CorePermissions.UserExport)]
    public async Task<IActionResult> Export([FromQuery] ExportUsersCommand command, CancellationToken ct)
        => HandleExport(await mediator.Send(command, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission(CorePermissions.UserRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new GetUserByIdQuery(id), ct));

    [HttpPost]
    [RequirePermission(CorePermissions.UserWrite)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest body, CancellationToken ct)
    {
        var command = new CreateUserCommand(body.UserName, body.Email, body.FullName, body.TempPassword, body.RoleIds ?? []);
        var result = await mediator.Send(command, ct);
        var location = result.IsSuccess ? $"/{CoreRoutes.Users}/{result.Value}" : string.Empty;
        return HandleCreated(result, location);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(CorePermissions.UserWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(new UpdateUserCommand(id, body.Email, body.FullName, body.Version), ct));

    [HttpPut("{id:guid}/roles")]
    [RequirePermission(CorePermissions.UserRoleAssign)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignUserRolesRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(new AssignUserRolesCommand(id, body.RoleIds, body.Version), ct));

    [HttpPost("{id:guid}/lock")]
    [RequirePermission(CorePermissions.UserLock)]
    public async Task<IActionResult> Lock(Guid id, [FromBody] VersionedRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(new LockUserCommand(id, body.Version), ct));

    [HttpPost("{id:guid}/unlock")]
    [RequirePermission(CorePermissions.UserLock)]
    public async Task<IActionResult> Unlock(Guid id, [FromBody] VersionedRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(new UnlockUserCommand(id, body.Version), ct));

    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(CorePermissions.UserResetPassword)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(new ResetPasswordCommand(id, body.TempPassword, body.Version), ct));
}

public sealed record CreateUserRequest(string UserName, string Email, string FullName, string TempPassword, IReadOnlyList<Guid>? RoleIds);

public sealed record UpdateUserRequest(string Email, string FullName, string? Version);

public sealed record AssignUserRolesRequest(IReadOnlyList<Guid> RoleIds, string? Version);

public sealed record VersionedRequest(string? Version);

public sealed record ResetPasswordRequest(string TempPassword, string? Version);
