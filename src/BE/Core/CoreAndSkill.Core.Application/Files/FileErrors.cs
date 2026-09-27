using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Files;

// docs/contracts/files.md. Hai mã dung lượng và kiểu tệp cố ý dùng `Validation` -> 400, không dùng mã
// HTTP riêng cho tệp: ánh xạ loại lỗi sang HTTP nằm ở đúng một chỗ (contracts/README.md §5).
public static class FileErrors
{
    public static readonly Error NotFound = new(
        "CORE.FILE.NOT_FOUND", "Không tìm thấy tệp.", ErrorType.NotFound);

    public static readonly Error TooLarge = new(
        "CORE.FILE.TOO_LARGE", "Tệp vượt giới hạn {MaxBytes} byte.", ErrorType.Validation);

    public static readonly Error TypeNotAllowed = new(
        "CORE.FILE.TYPE_NOT_ALLOWED", "Kiểu tệp không nằm trong danh sách cho phép.", ErrorType.Validation);

    // Có bản ghi, không có nội dung (§5: chiều "bản ghi DB, không có file"). Chỉ trả SAU khi quyền đọc
    // đã qua, nên không lộ việc một định danh có tồn tại hay không.
    public static readonly Error ContentMissing = new(
        "CORE.FILE.CONTENT_MISSING", "Nội dung tệp không còn trong kho lưu.", ErrorType.NotFound);
}
