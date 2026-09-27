using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Web.Http;

// Giới hạn dung lượng KHAI TƯỜNG MINH trên action nhận tệp — docs/wiki-core/be/14-file-storage.md §7:
// "mọi action nhận IFormFile phải khai giới hạn này tường minh, không để framework tự quyết bằng mặc
// định ẩn". Thiếu nó thì action vẫn build, vẫn chạy — cho tới ngày có người gửi một tệp rất lớn: nội
// dung đã bị multipart parser buffer hết TRƯỚC khi bất kỳ validator nào kiểm được gì (kể cả trần số dòng
// Core:Import:MaxRows, thứ đếm dòng SAU khi nhận đủ tệp).
//
// Giá trị lấy từ khoá Core:File:MaxUploadMb lúc CHẠY, nên không dùng được [RequestSizeLimit] (đòi hằng
// số lúc biên dịch). Attribute này đặt ba lớp:
//   1. Content-Length khai báo vượt trần  -> từ chối NGAY bằng envelope CORE.FILE.TOO_LARGE, không đọc thân.
//   2. Kestrel (IHttpMaxRequestBodySizeFeature) -> ngắt luồng nhận khi thân thật vượt trần, kể cả khi
//      client nói dối hoặc gửi kiểu chunked.
//   3. Multipart parser (FormOptions.MultipartBodyLengthLimit) -> giới hạn từng phần tệp.
//
// ArchTest `EveryFormFileAction_DeclaresUploadSizeLimit` ép: action có tham số kiểu IFormFile (hay kiểu
// có thuộc tính IFormFile) mà thiếu attribute này thì cổng đỏ.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UploadSizeLimitAttribute : Attribute, IResourceFilter
{
    // Phần multipart ngoài nội dung tệp: trường `purpose`, ranh giới, header của từng phần.
    private const long MultipartOverheadBytes = 64 * 1024;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var http = context.HttpContext;
        var limit = (long)http.RequestServices.GetRequiredService<IOptions<CoreFileOptions>>().Value.MaxUploadMb * 1024 * 1024;
        var bodyLimit = limit + MultipartOverheadBytes;

        if (http.Request.ContentLength is { } declared && declared > bodyLimit)
        {
            var error = FileErrors.TooLarge.WithParams(("MaxBytes", limit));
            context.Result = new ObjectResult(Envelope.Failure(error, http.TraceIdentifier))
            {
                StatusCode = ResultToHttpMapper.ToStatusCode(error.Type),
            };
            return;
        }

        var bodySize = http.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySize is { IsReadOnly: false })
            bodySize.MaxRequestBodySize = bodyLimit;

        // Cùng cách RequestFormLimitsAttribute của framework: thay tay cầm đọc form TRƯỚC khi form được đọc.
        var formFeature = http.Features.Get<IFormFeature>();
        if (formFeature is null || formFeature.Form is null)
        {
            // Trần multipart bằng trần THÂN, không phải trần tệp: tệp vượt trần nhưng còn trong phần dư multipart
            // phải tới được handler để nhận CORE.FILE.TOO_LARGE. Đặt bằng `limit` thì parser ném
            // InvalidDataException ngay trong lúc đọc form và người gọi nhận lỗi model binding thay vì mã của tệp.
            var formOptions = new FormOptions { MultipartBodyLengthLimit = bodyLimit };
            http.Features.Set<IFormFeature>(new FormFeature(http.Request, formOptions));
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
