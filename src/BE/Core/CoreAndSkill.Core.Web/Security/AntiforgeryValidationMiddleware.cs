using CoreAndSkill.Core.Application.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Web.Security;

// Chống CSRF hai lớp cho mọi method GHI — docs/quy-uoc/be-api-controller.md §7.2 — và cho action method AN TOÀN mang dấu
// [RequireAntiforgery]: ngoại lệ có tên cho GET có tác dụng phụ, chốt ở
// docs/adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md
// Vị trí trong pipeline: SAU UseAuthentication, TRƯỚC UseAuthorization (docs/quy-uoc/be-architecture.md §3.1).
internal sealed class AntiforgeryValidationMiddleware(
    RequestDelegate next, IAntiforgery antiforgery, IOptions<CoreAuthOptions> authOptions)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        // 1 — chỉ kiểm method GHI, trừ khi action tự khai nó có tác dụng phụ ([RequireAntiforgery] — đọc METADATA của
        // endpoint, không đọc đường dẫn).
        if (SafeMethods.Contains(context.Request.Method) && endpoint?.Metadata.GetMetadata<RequireAntiforgeryAttribute>() is null)
        {
            await next(context);
            return;
        }

        // 2 — endpoint đòi đăng nhập mà phiên chưa xác thực: để UseAuthorization trả 401, KHÔNG
        // chặn 403 CSRF trước (docs/quy-uoc/be-api-controller.md §7.2 "Phiên hết hạn ra 401").
        var requiresAuth = endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null;
        var allowsAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

        if (requiresAuth && !allowsAnonymous && context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        // 3 — lớp 1: Origin ngoài allowlist.
        var origin = context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin) && !IsAllowedOrigin(origin))
        {
            await SecurityEnvelopeWriter.WriteEnvelopeAsync(context, SecurityErrors.OriginRejected);
            return;
        }

        // 4 — lớp 2: token antiforgery.
        if (!await HasValidTokenAsync(context))
        {
            await SecurityEnvelopeWriter.WriteEnvelopeAsync(context, SecurityErrors.CsrfRejected);
            return;
        }

        await next(context);
    }

    // ValidateRequestAsync, KHÔNG phải IsRequestValidAsync: hàm sau trả true cho mọi GET/HEAD/OPTIONS/TRACE theo hợp đồng
    // của chính nó — gọi nó cho một GET mang dấu là một cổng luôn xanh (ADR-0062 "Hệ quả"). ValidateRequestAsync kiểm đủ ba
    // bước (cookie, header, cặp token khớp) bất kể method và báo mọi ca hỏng bằng AntiforgeryValidationException.
    private async Task<bool> HasValidTokenAsync(HttpContext context)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private bool IsAllowedOrigin(string origin)
    {
        foreach (var allowed in authOptions.Value.AllowedOrigins)
        {
            if (string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
