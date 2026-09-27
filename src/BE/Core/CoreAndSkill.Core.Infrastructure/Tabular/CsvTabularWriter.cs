using System.Text;
using CoreAndSkill.Core.Application.Tabular;

namespace CoreAndSkill.Core.Infrastructure.Tabular;

// Ghi CSV theo RFC 4180 — docs/wiki-core/be/15-import-export.md §2.1, §5.2, §5.4.
//
//   - UTF-8 KÈM DẤU HIỆU ĐẦU TỆP (BOM): thiếu nó thì phần mềm bảng tính phổ biến mở sai bảng mã và
//     tiếng Việt thành ký tự lạ.
//   - Ô chứa dấu phân tách, nháy kép, hoặc xuống dòng được đặt trong nháy kép (nháy bên trong nhân đôi):
//     không bao giờ tự nối chuỗi bằng dấu phẩy.
//   - Xuống dòng CRLF theo chuẩn.
//   - VÔ HIỆU HOÁ CÔNG THỨC (§5.4): ô bắt đầu bằng `=`, `+`, `-`, `@`, tab hoặc CR được thêm MỘT dấu nháy
//     đơn phía trước. Đây là trách nhiệm của phía XUẤT — dữ liệu độc hại do người dùng KHÁC nhập vào hệ
//     thống, hệ thống chỉ chuyển tiếp, và người bị hại là người MỞ TỆP. Ô bắt đầu bằng `-` (số âm) cũng bị
//     ép thành văn bản: chọn an toàn hơn là tiện, vì phần mềm bảng tính diễn giải `-2+3` như công thức.
//
// Ghi ra luồng, từng dòng một — không dựng cả tệp trong bộ nhớ. Không đóng luồng đầu ra (do chỗ gọi đóng).
internal sealed class CsvTabularWriter : ITabularWriter
{
    private const char Delimiter = ',';

    private readonly StreamWriter _writer;

    private CsvTabularWriter(Stream output)
        => _writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 8192, leaveOpen: true)
        {
            NewLine = "\r\n",
        };

    public static async Task<CsvTabularWriter> CreateAsync(Stream output, IReadOnlyList<string> headers, CancellationToken ct)
    {
        var writer = new CsvTabularWriter(output);
        await writer.WriteRowAsync(headers, ct);
        return writer;
    }

    public async Task WriteRowAsync(IReadOnlyList<string?> cells, CancellationToken ct)
    {
        var line = new StringBuilder();
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
                line.Append(Delimiter);

            line.Append(Escape(cells[i]));
        }

        await _writer.WriteLineAsync(line.ToString().AsMemory(), ct);
    }

    public Task CompleteAsync(CancellationToken ct) => _writer.FlushAsync(ct);

    public ValueTask DisposeAsync() => _writer.DisposeAsync();

    // internal để test đọc được chính hàm vô hiệu hoá công thức.
    internal static string NeutralizeFormula(string value)
        => value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? "'" + value
            : value;

    private static string Escape(string? cell)
    {
        var value = NeutralizeFormula(cell ?? string.Empty);

        return value.IndexOfAny([Delimiter, '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
    }
}
