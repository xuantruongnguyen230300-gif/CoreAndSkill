using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/files.md. Quyền của tệp là quyền của BẢN GHI CHỦ — không có khoá quyền riêng cho tệp
// (§4), nên ba action đều khai AuthenticatedOnly và handler kiểm quyền theo bản ghi (FileAccessPolicy).
// Tệp KHÔNG BAO GIỜ được phục vụ bằng đường tĩnh: mọi lần tải đi qua đây.
[ApiController]
[Route(CoreRoutes.Files)]
public sealed class FilesController(ISender mediator) : ApiControllerBase
{
    [HttpPost]
    [AuthenticatedOnly("Tệp chưa gắn bản ghi nào — quyền kiểm lúc gắn vào bản ghi chủ")]
    [UploadSizeLimit]
    public async Task<IActionResult> Upload([FromForm] UploadFileRequest request, CancellationToken ct)
    {
        // IFormFile.OpenReadStream() là stream seekable (handler cần đọc chữ ký đầu tệp rồi tua lại).
        await using var content = request.File?.OpenReadStream();

        return HandleResult(await mediator.Send(
            new UploadFileCommand(content, request.File?.FileName, request.Purpose), ct));
    }

    // Kiểu nội dung, tên tải về và vòng đời luồng đều do chỗ khác quyết: handler đặt kiểu theo nội dung
    // ĐÃ XÁC ĐỊNH lúc tải lên (không theo tên client gửi), còn HandleFile đặt ba header của một phản hồi
    // tệp và đóng luồng — docs/adr/0068-nhanh-tai-tep-di-qua-handlefile.md. Action không đặt header nào.
    [HttpGet("{id:guid}")]
    [AuthenticatedOnly("Quyền theo bản ghi chủ của tệp — handler kiểm, không có khoá quyền riêng cho tệp")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
        => HandleFile(await mediator.Send(new GetFileQuery(id), ct));

    [HttpDelete("{id:guid}")]
    [AuthenticatedOnly("Quyền theo bản ghi chủ của tệp — handler kiểm, không có khoá quyền riêng cho tệp")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new DeleteFileCommand(id), ct));
}

// Hai phần của multipart/form-data — docs/contracts/files.md §1. Cả hai nullable có chủ đích: thiếu phần nào
// là lỗi CORE.VALIDATION.FAILED do validator trả (mã trong catalog), không phải lỗi model binding chung.
public sealed class UploadFileRequest
{
    public IFormFile? File { get; init; }

    [FromForm(Name = "purpose")]
    public string? Purpose { get; init; }
}
