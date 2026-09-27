using CoreAndSkill.Core.Application.Export;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// [Authorize] fail-closed — endpoint công khai phải khai [AllowAnonymous] tường minh.
// docs/quy-uoc/be-api-controller.md §3.
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(Envelope.Success(result.Value, HttpContext.TraceIdentifier)) : Failure(result.Error!);

    protected IActionResult HandleResult(Result result)
        => result.IsSuccess ? Ok(Envelope.Success<object>(null, HttpContext.TraceIdentifier)) : Failure(result.Error!);

    // 201 + header Location — docs/contracts/users.md §5, docs/contracts/roles.md §2. Vẫn là HÌNH
    // DẠNG HTTP của một Result đã có, không phải logic nghiệp vụ — cùng ranh giới với hai overload
    // trên, chỉ khác status code và header.
    protected IActionResult HandleCreated<T>(Result<T> result, string location)
        => result.IsSuccess
            ? Created(location, Envelope.Success(result.Value, HttpContext.TraceIdentifier))
            : Failure(result.Error!);

    // Nhánh trả TỆP — docs/adr/0064-nhanh-tra-tep-di-qua-apicontrollerbase.md, docs/contracts/exports.md §1.
    // Thành công: thân là chính tệp, KHÔNG phải envelope. Thất bại: envelope như mọi endpoint, cùng Failure với hai
    // overload trên. ExportFileResult giữ internal có chủ đích — ba header bắt buộc (Content-Disposition, nosniff,
    // no-store) và cách ghi luồng là việc của Core.Web, không phải bề mặt module phải biết và có thể quên.
    protected IActionResult HandleExport(Result<ExportFile> result)
        => result.IsSuccess ? new ExportFileResult(result.Value) : Failure(result.Error!);

    // Nhánh TẢI TỆP — docs/adr/0068-nhanh-tai-tep-di-qua-handlefile.md, docs/contracts/files.md §2. Cùng hình dạng
    // HTTP với HandleExport và dùng chung đúng một chỗ đặt ba header, nhưng tách khỏi nó bằng KIỂU THAM SỐ:
    // ExportFile mang một delegate ghi, FileDownload mang một luồng đã mở. Gọi nhầm lối là lỗi biên dịch, nên dấu
    // hiệu cú pháp `HandleExport` mà cổng S20 dò vẫn chỉ trỏ vào endpoint XUẤT — tải tệp là GET không tác dụng phụ.
    protected IActionResult HandleFile(Result<FileDownload> result)
        => result.IsSuccess ? FileDownloadResponse.For(result.Value) : Failure(result.Error!);

    private IActionResult Failure(Error error)
        => StatusCode(ResultToHttpMapper.ToStatusCode(error.Type), Envelope.Failure(error, HttpContext.TraceIdentifier));
}
