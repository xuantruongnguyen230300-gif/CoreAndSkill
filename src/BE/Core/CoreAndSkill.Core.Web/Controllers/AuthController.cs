using System.Globalization;
using System.Security.Claims;
using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/auth.md. Ba action ngoài "chỉ Send rồi HandleResult" — login, change-password,
// change-password-required — vì chúng phát/cấp lại cookie phiên (docs/quy-uoc/be-api-controller.md
// §3.1, §7.4). Đăng nhập chịu hai hàng rào riêng (be-api-controller.md §6.1): policy "login" theo IP
// gắn ở action dưới, và bộ đếm theo LoginPartitionKey trong handler (ILoginAttemptLimiter, §6.5).
[ApiController]
[Route(CoreRoutes.Auth)]
public sealed class AuthController(ISender mediator, ISessionPrincipalFactory principalFactory, TimeProvider timeProvider)
    : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsFailure)
            return HandleResult(result);

        await SignInAsync(result.Value, timeProvider.GetUtcNow());
        return HandleResult(Result.Success(result.Value.Session));
    }

    [HttpPost(CoreRoutes.AuthLogout)]
    [AuthenticatedOnly("Đăng xuất — ai đã đăng nhập cũng phải thoát được")]
    public async Task<IActionResult> Logout()
    {
        // Chỉ xoá cookie phiên đang gọi — chưa có kho phiếu phía server (docs/contracts/auth.md §4).
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return HandleResult(Result.Success());
    }

    [HttpGet(CoreRoutes.AuthMe)]
    [AuthenticatedOnly("Danh tính của chính phiên đang gọi")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetCurrentSessionQuery(), ct));

    [HttpPost("change-password")]
    [AuthenticatedOnly("Đổi mật khẩu của chính mình")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsFailure)
            return HandleResult(result);

        await SignInAsync(result.Value, ReadIssuedAt(User));
        return HandleResult(Result.Success());
    }

    [HttpPost(CoreRoutes.AuthChangePasswordRequired)]
    [AuthenticatedOnly("Đường thoát duy nhất khỏi trạng thái bắt buộc đổi mật khẩu")]
    public async Task<IActionResult> ChangePasswordRequired([FromBody] ChangePasswordRequiredCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsFailure)
            return HandleResult(result);

        await SignInAsync(result.Value, ReadIssuedAt(User));
        return HandleResult(Result.Success());
    }

    // issuedAt: đăng nhập truyền `now`; cấp lại cookie sau đổi mật khẩu truyền giá trị đọc từ
    // principal CŨ — trần tuyệt đối của phiên KHÔNG reset (docs/adr/0033-luong-dang-nhap-outcome-va-claim.md).
    private Task SignInAsync(LoginOutcome outcome, DateTimeOffset issuedAt) => HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principalFactory.Create(outcome, issuedAt),
        new AuthenticationProperties { IsPersistent = false });

    private static DateTimeOffset ReadIssuedAt(ClaimsPrincipal user)
        => DateTimeOffset.Parse(user.FindFirstValue(CoreClaimTypes.IssuedAt)!, CultureInfo.InvariantCulture);
}
