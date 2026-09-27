using CoreAndSkill.Core.Application.Profile;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/profile.md.
[ApiController]
[Route(CoreRoutes.Profile)]
public sealed class ProfileController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    [AuthenticatedOnly("Hồ sơ của chính người gọi — định danh lấy từ phiên")]
    public async Task<IActionResult> Get(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetProfileQuery(), ct));

    [HttpPut]
    [AuthenticatedOnly("Sửa hồ sơ của chính người gọi — định danh lấy từ phiên")]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest body, CancellationToken ct)
    {
        // Chuẩn hoá "" → null TRƯỚC KHI vào pipeline validate — docs/contracts/profile.md §2.
        var command = new UpdateProfileCommand(
            body.FullName,
            string.IsNullOrEmpty(body.PhoneNumber) ? null : body.PhoneNumber,
            string.IsNullOrEmpty(body.PreferredLanguage) ? null : body.PreferredLanguage,
            body.Version);

        return HandleResult(await mediator.Send(command, ct));
    }

    [HttpPost("renounce-permission-bypass")]
    [AuthenticatedOnly("Tự bỏ cờ của chính tài khoản gọi — không ai bỏ hộ người khác")]
    public async Task<IActionResult> RenouncePermissionBypass(CancellationToken ct)
        => HandleResult(await mediator.Send(new RenouncePermissionBypassCommand(), ct));
}

// Hình dạng dây của PUT — không phải UpdateProfileCommand trực tiếp, vì cần chuẩn hoá "" → null
// trước khi validate (docs/contracts/profile.md §2).
public sealed record UpdateProfileRequest(string FullName, string? PhoneNumber, string? PreferredLanguage, string? Version);
