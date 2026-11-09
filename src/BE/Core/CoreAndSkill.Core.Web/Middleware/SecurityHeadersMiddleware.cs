using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace CoreAndSkill.Core.Web.Middleware;

// ADR-0120 quyết định 2 và QC3 (docs/adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md):
// nhóm header gắn với NỘI DUNG phản hồi API — nosniff và no-store trên mọi phản hồi, kể cả 401/403/429, lỗi do
// UseExceptionHandler dựng và preflight CORS. HSTS và CSP thuộc proxy (Caddy), không đặt ở đây.
// Đặt lúc phản hồi BẮT ĐẦU (OnStarting) và KHÔNG ghi đè giá trị endpoint đã đặt (ExportFileResult, nhánh tải tệp).
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpContext)state).Response.Headers;

            if (!headers.ContainsKey(HeaderNames.XContentTypeOptions))
                headers[HeaderNames.XContentTypeOptions] = "nosniff";

            if (!headers.ContainsKey(HeaderNames.CacheControl))
                headers[HeaderNames.CacheControl] = "no-store";

            return Task.CompletedTask;
        }, context);

        return next(context);
    }
}
