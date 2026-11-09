using System.Diagnostics;
using CoreAndSkill.Core.Web.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Web.Middleware;

// MỘT dòng Information mỗi request: phương thức, đường dẫn, mã trạng thái, thời gian xử lý, mã lần gọi — thay hai dòng
// request của framework (nhóm Microsoft.AspNetCore để Warning, docs/wiki-core/be/07-observability.md §4.1).
// Đường dẫn lấy từ Request.Path — KHÔNG ghi query string và thân request, vì có thể chứa dữ liệu nhạy cảm (§5).
// Đứng NGAY SAU TraceIdMiddleware và TRƯỚC UseExceptionHandler: dòng log thấy mã trạng thái cuối cùng, kể cả 500
// do IExceptionHandler dựng, và nằm trong phạm vi log mang TraceId.
internal sealed class RequestLogMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<RequestLogMiddleware> logger)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            // Health check bị thăm dò liên tục (compose, load balancer) — không ghi dòng, tránh ngập log.
            if (!context.Request.Path.StartsWithSegments(HealthPaths.Root))
            {
                var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

                logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (trace {TraceId})",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    Math.Round(elapsedMs, 1),
                    context.TraceIdentifier);
            }
        }
    }
}
