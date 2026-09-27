using CoreAndSkill.Core.Application.Export;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace CoreAndSkill.Core.Web.Http;

// Nhánh thành công của MỌI phản hồi tệp — docs/contracts/exports.md §1: thân là CHÍNH TỆP kèm
// `Content-Disposition: attachment`, KHÔNG phải envelope. Nhánh lỗi vẫn là envelope như mọi endpoint
// (controller trả HandleResult cho Result thất bại, tới đây mới là thành công).
//
// Ghi thẳng ra luồng phản hồi (docs/wiki-core/be/15-import-export.md §5.2): tệp xuất KHÔNG lưu lại; mất
// kết nối giữa chừng thì xuất lại. Tệp chứa dữ liệu riêng tư nên `no-store` — file người này không được
// còn trong bộ đệm khi người khác dùng chung máy (09-security-beyond-auth.md §6, 14-file-storage.md §4).
//
// Lớp này phục vụ CẢ HAI nhánh tệp (docs/adr/0068-nhanh-tai-tep-di-qua-handlefile.md quyết định 3): nhánh
// xuất qua HandleExport, nhánh tải tệp qua HandleFile + FileDownloadResponse. Tên chỉ nói một nhánh, và
// đó là chọn có chủ đích — nó là hằng số cú pháp của cổng S20 (ADR-0064 quyết định 4), đổi tên là làm
// cổng đang chạy mù.
//
// contentLength: chỉ nhánh BIẾT TRƯỚC độ dài mới truyền. Nhánh xuất ghi dần theo lô nên không biết; nhánh
// tải tệp đọc một luồng đã mở, seekable, nên biết — và `Content-Length` là thứ FileStreamResult của
// framework vẫn đặt cho nhánh đó trước ADR-0068. Bỏ nó đi là đổi hình dạng phản hồi trong im lặng.
internal sealed class ExportFileResult(ExportFile file, long? contentLength = null) : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;

        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = file.ContentType;
        var disposition = new ContentDispositionHeaderValue("attachment");
        disposition.SetHttpFileName(file.FileName);
        response.Headers.ContentDisposition = disposition.ToString();
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.CacheControl = "no-store";

        if (contentLength is { } length)
            response.ContentLength = length;

        await file.WriteToAsync(response.Body, context.HttpContext.RequestAborted);
    }
}
