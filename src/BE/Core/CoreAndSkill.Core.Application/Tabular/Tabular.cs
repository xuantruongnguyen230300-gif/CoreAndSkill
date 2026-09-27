namespace CoreAndSkill.Core.Application.Tabular;

public enum TabularFormat
{
    Csv,
    Xlsx,
}

// Một dòng dữ liệu đã đọc. Number là SỐ DÒNG TRONG TỆP GỐC như người dùng nhìn thấy, KỂ CẢ dòng tiêu
// đề (dòng dữ liệu đầu tiên thường là 2) — không phải chỉ số nội bộ. Lệch một dòng do dòng tiêu đề là
// chi tiết nhỏ nhưng làm hỏng toàn bộ giá trị của báo cáo (docs/wiki-core/be/15-import-export.md §4.3,
// docs/contracts/exports.md §2). Với ô có xuống dòng bên trong, Number là dòng nơi bản ghi BẮT ĐẦU.
//
// Values: tên cột (đúng như tiêu đề trong tệp, đã cắt khoảng trắng) -> giá trị chuỗi đã cắt khoảng
// trắng; chuỗi rỗng là null ("ô trống và ô chứa khoảng trắng là hai thứ khác nhau" — §2.2: cắt khoảng
// trắng, coi chuỗi rỗng là trống). Việc chuyển kiểu nằm ở tầng trên, để lỗi chuyển đổi là lỗi CỦA DÒNG.
public sealed record TabularRow(int Number, IReadOnlyDictionary<string, string?> Values);

// Tệp không đọc được (sai định dạng, nén hỏng, vượt giới hạn kích thước một bản ghi…). Hiện thực bên
// Core.Infrastructure ném kiểu này; chỗ gọi ở Application bắt và đổi thành lỗi kiểm hợp lệ — không
// phải lỗi nghiệp vụ nên là exception, không phải Result.
public sealed class TabularFormatException(string message) : Exception(message);

// Nguồn đọc theo dòng — docs/wiki-core/be/15-import-export.md §3.1: KHÔNG nạp cả tệp vào bộ nhớ.
// Headers có ngay từ khi mở (đọc dòng đầu); Rows đọc lười, mỗi phần tử một dòng.
public interface ITabularSource : IAsyncDisposable
{
    IReadOnlyList<string> Headers { get; }

    IAsyncEnumerable<TabularRow> ReadRowsAsync(CancellationToken ct);
}

// Bộ đọc CSV / Excel. Định dạng suy từ NỘI DUNG (chữ ký tệp nén = Excel, còn lại = CSV), không từ đuôi.
public interface ITabularReader
{
    // Ném TabularFormatException khi tệp không mở được. Stream do chỗ gọi đóng.
    Task<ITabularSource> OpenAsync(Stream content, CancellationToken ct);
}

// Bộ ghi bảng dùng chung cho CSV và bảng tính — §8. Ghi theo LUỒNG: một dòng ra ngay, không dựng cả
// tệp trong bộ nhớ (§5.2).
//
// Ô bắt đầu bằng ký tự công thức PHẢI bị vô hiệu hoá khi ghi (§5.4) — đó là trách nhiệm của phía XUẤT,
// người bị hại là người mở tệp. Hiện thực làm việc này; chỗ gọi không phải nhớ.
public interface ITabularWriter : IAsyncDisposable
{
    Task WriteRowAsync(IReadOnlyList<string?> cells, CancellationToken ct);

    // Ghi phần kết của tệp (đóng bảng tính…). Gọi đúng MỘT lần, sau dòng cuối; không gọi thì tệp
    // Excel không hợp lệ.
    Task CompleteAsync(CancellationToken ct);
}

public interface ITabularWriterFactory
{
    // Ghi dòng tiêu đề ngay khi tạo. Stream do chỗ gọi đóng.
    Task<ITabularWriter> CreateAsync(TabularFormat format, Stream output, IReadOnlyList<string> headers, CancellationToken ct);
}
