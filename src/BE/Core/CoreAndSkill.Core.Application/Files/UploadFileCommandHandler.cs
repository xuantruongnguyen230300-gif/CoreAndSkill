using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Files;
using MediatR;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Application.Files;

// docs/contracts/files.md §1, docs/wiki-core/be/14-file-storage.md §5.1.
//
// THỨ TỰ THAO TÁC là chỗ quyết định: ghi tệp TRƯỚC, ghi bản ghi SAU. Giao dịch hỏng thì tệp thành
// rác vô hại (job đối soát dọn); ngược thứ tự thì bản ghi trỏ vào hư không và người dùng nhìn thấy.
// "Thao tác trên kho tệp luôn nghiêng về phía để lại rác, không về phía để lại tham chiếu gãy."
internal sealed class UploadFileCommandHandler(
    FilePurposeCatalog purposes,
    IOptions<CoreFileOptions> fileOptions,
    IFileStorage storage,
    IFileRepository files)
    : IRequestHandler<UploadFileCommand, Result<FileDto>>
{
    public async Task<Result<FileDto>> Handle(UploadFileCommand command, CancellationToken ct)
    {
        // Validator đã bảo đảm hai giá trị này khác null.
        var content = command.File!;
        var purposeKey = command.Purpose!;

        var purpose = purposes.Find(purposeKey);
        if (purpose is null)
            return Invalid("Purpose", CommonErrors.Format);

        if (!content.CanSeek)
            return Invalid("File", CommonErrors.Format);

        var length = content.Length;
        if (length == 0)
            return Invalid("File", CommonErrors.Required);

        // Trần chung + trần riêng của purpose: purpose chỉ được SIẾT.
        var globalMax = (long)fileOptions.Value.MaxUploadMb * 1024 * 1024;
        var maxBytes = purpose.MaxBytes > 0 ? Math.Min(purpose.MaxBytes, globalMax) : globalMax;
        if (length > maxBytes)
            return Result.Failure<FileDto>(FileErrors.TooLarge.WithParams(("MaxBytes", maxBytes)));

        // Kiểu do HỆ THỐNG xác định từ nội dung. Không nhận ra, hoặc ngoài danh sách cho phép của
        // purpose ⇒ từ chối. Đuôi tệp client đặt không bao giờ là bằng chứng.
        var contentType = FileContentDetector.Detect(content, command.FileName);
        if (contentType is null || !purpose.AllowedContentTypes.Contains(contentType, StringComparer.Ordinal))
            return Result.Failure<FileDto>(FileErrors.TypeNotAllowed);

        var storageKey = await storage.SaveAsync(
            content, purpose.Key, FileContentDetector.ExtensionFor(contentType), ct);

        var originalName = FileNameSanitizer.Sanitize(command.FileName);
        var file = StoredFile.Create(originalName, contentType, length, storageKey, purpose.Key);
        if (file.IsFailure)
            return Result.Failure<FileDto>(file.Error!);

        await files.AddAsync(file.Value, ct);

        return new FileDto(file.Value.Id, originalName, contentType, length);
    }

    private static Result<FileDto> Invalid(string field, Error fieldError)
        => Result.Failure<FileDto>(CommonErrors.ValidationFailed.WithFieldErrors(
            new Dictionary<string, IReadOnlyList<FieldError>>
            {
                [field] = [new FieldError(fieldError.Code, new Dictionary<string, string>())],
            }));
}
