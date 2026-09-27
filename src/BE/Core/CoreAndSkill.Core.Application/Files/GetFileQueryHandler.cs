using CoreAndSkill.Core.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Application.Files;

internal sealed class GetFileQueryHandler(
    IFileRepository files,
    FileAccessPolicy access,
    IFileStorage storage,
    ILogger<GetFileQueryHandler> logger)
    : IRequestHandler<GetFileQuery, Result<FileDownload>>
{
    public async Task<Result<FileDownload>> Handle(GetFileQuery query, CancellationToken ct)
    {
        var file = await files.FindByIdAsync(query.Id, ct);

        // "Không có" và "không có quyền đọc bản ghi chủ" gộp làm MỘT (luật M7): trả 403 là xác nhận
        // định danh đó có tồn tại.
        if (file is null || !await access.CanReadAsync(file, ct))
            return Result.Failure<FileDownload>(FileErrors.NotFound);

        var opened = await storage.OpenAsync(file.StorageKey, ct);
        if (opened.IsFailure)
        {
            // Chiều "bản ghi DB, không có file" của 14-file-storage.md §5: không được im lặng —
            // người dùng nhận lỗi, người vận hành nhận dòng log Error kèm định danh (không kèm khoá
            // kho lưu hay tên tệp).
            logger.LogError("Tệp {FileId} có bản ghi nhưng không còn nội dung trong kho lưu — cần người xem.", file.Id);
            return Result.Failure<FileDownload>(FileErrors.ContentMissing);
        }

        return new FileDownload(opened.Value, file.ContentType, FileNameSanitizer.ForDownload(file.OriginalName, file.ContentType));
    }
}
