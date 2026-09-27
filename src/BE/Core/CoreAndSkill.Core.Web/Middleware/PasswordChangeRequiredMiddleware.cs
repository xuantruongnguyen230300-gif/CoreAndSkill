using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace CoreAndSkill.Core.Web.Middleware;

// Chặn tài khoản đang ở trạng thái bắt buộc đổi mật khẩu — docs/contracts/auth.md §1.2,
// docs/quy-uoc/be-architecture.md §3.1. Đọc cờ từ CLAIM (không truy DB); SAU UseAuthorization.
// Endpoint ẩn danh không bị kiểm; trong số endpoint có danh tính, bốn đường được phép đi qua — allowlist ĐÓNG, không mở
// rộng khi chưa có ADR.
internal sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    // Đường dẫn ghép từ CÙNG hằng số mà [Route]/[Http*] của controller dùng (Http/ApiRoutes.cs) — đổi route là đổi
    // allowlist theo. PasswordChangeRequiredMiddlewareTests gõ tay bốn đường của contract để bắt khi cả hai cùng lệch.
    private static readonly HashSet<(string Method, string Path)> Allowlist = new()
    {
        ("GET", $"/{CoreRoutes.Antiforgery}/{CoreRoutes.AntiforgeryToken}"), // change-password-required là POST — cần token CSRF trước
        ("GET", $"/{CoreRoutes.Auth}/{CoreRoutes.AuthMe}"),
        ("POST", $"/{CoreRoutes.Auth}/{CoreRoutes.AuthChangePasswordRequired}"),
        ("POST", $"/{CoreRoutes.Auth}/{CoreRoutes.AuthLogout}"),
    };

    public async Task InvokeAsync(HttpContext context)
    {
        // Endpoint ẩn danh không bị kiểm (docs/contracts/auth.md §1.2) — đọc [AllowAnonymous] trên METADATA của endpoint,
        // cùng khuôn AntiforgeryValidationMiddleware, không đọc đường dẫn. Thiếu nhánh này thì người mang cờ không gửi được
        // báo lỗi của chính màn đổi mật khẩu (client-errors) và không đăng nhập lại được từ trình duyệt còn giữ cookie cũ.
        var allowsAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

        if (!allowsAnonymous && context.User.Identity?.IsAuthenticated == true)
        {
            var mustChangePassword = context.User.FindFirst(CoreClaimTypes.MustChangePassword)?.Value;

            if (string.Equals(mustChangePassword, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                var method = context.Request.Method.ToUpperInvariant();
                var path = context.Request.Path.Value ?? string.Empty;

                var allowed = Allowlist.Any(entry =>
                    entry.Method == method && string.Equals(entry.Path, path, StringComparison.OrdinalIgnoreCase));

                if (!allowed)
                {
                    await SecurityEnvelopeWriter.WriteEnvelopeAsync(context, SecurityErrors.PasswordChangeRequired);
                    return;
                }
            }
        }

        await next(context);
    }
}
