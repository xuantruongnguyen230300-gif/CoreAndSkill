using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Export;

// Catalog lỗi của luồng xuất — docs/quy-uoc/be-architecture.md §7 bước 3 (`<Feature>Errors.cs`),
// docs/contracts/exports.md §1.
public static class ExportErrors
{
    // Vượt giới hạn số dòng của đường đồng bộ. Xuất chạy nền chưa có ở v1, nên trần này BẮT BUỘC:
    // nó giữ cho một lần xuất không treo cả tiến trình. Thông điệp nêu số dòng thực tế và giới hạn để
    // người dùng biết cần lọc hẹp thêm bao nhiêu.
    public static readonly Error TooManyRows = new(
        "CORE.EXPORT.TOO_MANY_ROWS",
        "Kết quả có {RowCount} dòng, vượt giới hạn xuất {MaxRows} dòng. Hãy siết bộ lọc.",
        ErrorType.BusinessRule);
}
