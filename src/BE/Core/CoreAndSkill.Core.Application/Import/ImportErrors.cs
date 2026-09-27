using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Import;

// docs/contracts/exports.md §2. Mã theo tài nguyên nhưng khuôn CHUNG cho mọi luồng nhập — module không
// tự nghĩ khuôn khác.
public static class ImportErrors
{
    // Thiếu cột bắt buộc, hoặc tiêu đề không khớp mẫu — kiểm CÙNG LƯỢT đếm dòng, trước khi tạo việc.
    public static readonly Error ColumnsMismatch = new(
        "CORE.IMPORT.COLUMNS_MISMATCH",
        "Tệp thiếu cột bắt buộc: {MissingColumns}.",
        ErrorType.Validation);

    // Vượt trần Core:Import:MaxRows — đếm TRƯỚC khi tạo việc; thông điệp nêu số dòng thực tế và giới hạn.
    public static readonly Error TooManyRows = new(
        "CORE.IMPORT.TOO_MANY_ROWS",
        "Tệp có {RowCount} dòng, vượt giới hạn {MaxRows} dòng.",
        ErrorType.BusinessRule);

    // KHÔNG phải lỗi của request — mã của một phần tử `failed` trong kết quả việc: dòng trùng khoá tự
    // nhiên với bản ghi đã có hoặc với dòng trước trong cùng tệp, bị bỏ qua (không ghi đè).
    public static readonly Error DuplicateRow = new(
        "CORE.IMPORT.DUPLICATE_ROW",
        "Dòng trùng khoá với bản ghi đã có hoặc với một dòng trước trong tệp.",
        ErrorType.BusinessRule);

    // Loại việc không có IImportDefinition nào — lỗi lập trình của chỗ gọi, không phải của người dùng.
    public static readonly Error UnknownType = new(
        "CORE.IMPORT.UNKNOWN_TYPE",
        "Không có định nghĩa nhập nào cho loại '{ImportType}'.",
        ErrorType.BusinessRule);

    // Việc chạy nền không đọc được tệp gốc (tệp tạm hết hạn hoặc hỏng giữa chừng) — việc `failed`.
    public static readonly Error SourceUnreadable = new(
        "CORE.IMPORT.SOURCE_UNREADABLE",
        "Không đọc được tệp nhập.",
        ErrorType.BusinessRule);

    // Cơ sở dữ liệu từ chối ghi một dòng (ràng buộc mà tầng nghiệp vụ không thấy trước, thường là đua
    // với một lần ghi khác) — dòng bị báo lỗi, các dòng khác không bị ảnh hưởng.
    public static readonly Error RowNotSaved = new(
        "CORE.IMPORT.ROW_NOT_SAVED",
        "Không ghi được dòng này.",
        ErrorType.BusinessRule);
}
