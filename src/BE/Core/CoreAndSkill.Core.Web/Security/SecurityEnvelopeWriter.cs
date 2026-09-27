using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Http;

namespace CoreAndSkill.Core.Web.Security;

// Helper DUY NHẤT ghi envelope lỗi TỪ HẠ TẦNG (không qua Result/HandleResult) — docs/quy-uoc/be-api-controller.md §7.4.
internal static class SecurityEnvelopeWriter
{
    public static Task WriteEnvelopeAsync(HttpContext http, Error error)
    {
        http.Response.StatusCode = ResultToHttpMapper.ToStatusCode(error.Type);
        return http.Response.WriteAsJsonAsync(Envelope.Failure(error, http.TraceIdentifier));
    }
}
