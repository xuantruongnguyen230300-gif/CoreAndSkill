using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Nhập từ tệp — docs/wiki-core/be/15-import-export.md §6. Chung cho mọi đơn vị.
public sealed class CoreImportOptions
{
    public const string SectionName = "Core:Import";

    // Trần số dòng dữ liệu của một tệp nhập. Đếm TRƯỚC khi tạo việc; vượt thì CORE.IMPORT.TOO_MANY_ROWS
    // và không có bản ghi core.job nào được tạo. Trần này đếm dòng SAU khi tệp đã nhận đủ — nó không
    // thay được giới hạn dung lượng ở Core:File:MaxUploadMb (14-file-storage.md §7).
    [Range(1, 1_000_000)]
    public int MaxRows { get; init; } = 50_000;

    // Số phần tử tối đa của `result.failed` nhúng trong bản ghi việc. Danh sách ĐẦY ĐỦ nằm ở tệp kết
    // quả (result_file_id): một tệp 50.000 dòng sai cả không được biến cột jsonb thành vài chục MB.
    [Range(1, 10_000)]
    public int MaxFailedRowsInResult { get; init; } = 100;
}
