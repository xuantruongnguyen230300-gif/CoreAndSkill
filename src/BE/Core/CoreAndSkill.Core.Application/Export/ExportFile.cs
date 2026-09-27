namespace CoreAndSkill.Core.Application.Export;

// docs/contracts/exports.md §1. Tệp xuất KHÔNG lưu lại — ghi thẳng ra luồng phản hồi (§5.2 của
// 15-import-export.md); mất kết nối giữa chừng thì xuất lại.
//
// Handler chỉ TRẢ MÔ TẢ (tên, kiểu, và hàm ghi), không ghi gì: Application không biết HTTP, còn việc
// ghi phải chạy SAU khi giao dịch bao quanh handler đã commit (dòng nhật ký kiểm toán thuộc giao dịch
// đó). WriteToAsync đọc dữ liệu theo lô và ghi từng dòng — không dựng cả tệp trong bộ nhớ.
public sealed record ExportFile(
    string FileName,
    string ContentType,
    Func<Stream, CancellationToken, Task> WriteToAsync);

// Thông tin định dạng xuất: `format` là csv hoặc xlsx (contracts/exports.md §1).
public static class ExportFormats
{
    public const string Csv = "csv";
    public const string Xlsx = "xlsx";

    public static bool IsSupported(string? format)
        => format is Csv or Xlsx;

    public static string ContentType(string format)
        => format == Xlsx
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv; charset=utf-8";
}
