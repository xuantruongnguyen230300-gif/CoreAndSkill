using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Security;
using Microsoft.AspNetCore.Http;

namespace CoreAndSkill.Core.Web.Middleware;

// Bọc envelope cho response do HẠ TẦNG ĐỊNH TUYẾN sinh (404 không khớp route, 405 sai verb) — thân
// rỗng, không đi qua handler nào. docs/quy-uoc/be-api-controller.md §2.4.
// Ba ràng buộc: chỉ bọc tiền tố /api (allowlist); không ghi đè response đã có thân; không MapFallback.
internal sealed class EnvelopeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (!context.Request.Path.StartsWithSegments("/" + ApiRoutes.Root))
            return;

        if (context.Response.HasStarted
            || context.Response.ContentLength.HasValue
            || !string.IsNullOrEmpty(context.Response.ContentType))
            return;

        // 405: status đã do routing đặt — KHÔNG suy lại từ ErrorType (be-api-controller.md §2.4).
        var error = context.Response.StatusCode switch
        {
            StatusCodes.Status404NotFound => SecurityErrors.RouteNotFound,
            StatusCodes.Status405MethodNotAllowed => SecurityErrors.RouteMethodNotAllowed,
            _ => null,
        };

        if (error is null)
            return;

        await context.Response.WriteAsJsonAsync(Envelope.Failure(error, context.TraceIdentifier));
    }
}
