using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/tenants.md — khu quản trị hệ thống. MỌI endpoint mang [RequireSystemOperator]
// (luật S11, docs/adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md) — cờ vận hành hệ thống trên tài
// khoản, KHÔNG phải một quyền trong ma trận. Không có [RequirePermission] nào ở controller này.
[ApiController]
[Route(CoreRoutes.SystemTenants)]
[RequireSystemOperator]
public sealed class TenantsController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetTenantsListQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(
            new CreateTenantCommand(body.Code, body.Name, body.AdminUserName, body.AdminEmail, body.AdminFullName, body.AdminTempPassword),
            ct));

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetTenantActiveRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(new SetTenantActiveCommand(id, body.IsActive), ct));

    [HttpPost("{id:guid}/recovery-reset-password")]
    public async Task<IActionResult> RecoveryResetPassword(Guid id, [FromBody] RecoveryResetPasswordRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(
            new RecoveryResetTenantAdminPasswordCommand(id, body.UserName, body.TempPassword), ct));

    [HttpPost("{id:guid}/admins")]
    public async Task<IActionResult> CreateAdmin(Guid id, [FromBody] CreateTenantAdminRequest body, CancellationToken ct)
        => HandleResult(await mediator.Send(
            new CreateTenantAdminCommand(id, body.UserName, body.Email, body.FullName, body.TempPassword), ct));
}

public sealed record CreateTenantRequest(
    string Code, string Name, string AdminUserName, string AdminEmail, string AdminFullName, string AdminTempPassword);

// IsActive: bool? — docs/contracts/tenants.md §3, xem SetTenantActiveCommand.cs.
public sealed record SetTenantActiveRequest(bool? IsActive);

public sealed record RecoveryResetPasswordRequest(string UserName, string TempPassword);

public sealed record CreateTenantAdminRequest(string UserName, string Email, string FullName, string TempPassword);
