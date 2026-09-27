using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Web.Middleware;

// Mã lần gọi theo W3C Trace Context — nhận traceparent nếu có, sinh mới nếu không (Activity.Current
// đã làm việc này). Gán vào HttpContext.TraceIdentifier và vào phạm vi log — docs/wiki-core/be/07-observability.md §3.
internal sealed class TraceIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<TraceIdMiddleware> logger)
    {
        var traceId = Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
        context.TraceIdentifier = traceId;

        using (logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId }))
        {
            await next(context);
        }
    }
}
