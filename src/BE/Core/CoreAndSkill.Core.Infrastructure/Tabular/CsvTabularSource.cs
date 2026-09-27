using System.Runtime.CompilerServices;
using System.Text;
using CoreAndSkill.Core.Application.Tabular;

namespace CoreAndSkill.Core.Infrastructure.Tabular;

// Đọc CSV theo RFC 4180, THEO DÒNG, không nạp cả tệp — docs/wiki-core/be/15-import-export.md §2.1, §3.1.
//
// KHÔNG BAO GIỜ tự tách CSV bằng cách cắt chuỗi theo dấu phẩy (§2.1: chạy đúng trên tệp mẫu, sai trên tệp
// thật ở đúng những dòng có ghi chú dài): đây là bộ phân tích có trạng thái nhận biết nháy kép, nháy nhân
// đôi và xuống dòng BÊN TRONG ô.
//
// Bẫy đã xử lý:
//   - Bảng mã: dấu hiệu đầu tệp (UTF-8 / UTF-16) được nhận diện, mặc định UTF-8.
//   - Dấu phân tách theo vùng: tự dò từ dòng tiêu đề trong `,` `;` tab.
//   - Số dòng: theo tệp GỐC như người dùng nhìn thấy (kể cả dòng tiêu đề); ô có xuống dòng bên trong thì
//     dòng của bản ghi là dòng nó BẮT ĐẦU.
//   - Ô trống và ô khoảng trắng: cắt khoảng trắng, chuỗi rỗng là null.
//
// Tệp không đọc được (nháy mở không đóng, bản ghi quá lớn, tiêu đề trùng hoặc rỗng, bản ghi nhiều ô hơn số
// cột) ném TabularFormatException — lỗi CỦA TỆP, khác với lỗi CỦA DÒNG do tầng trên xử lý. Thông điệp
// KHÔNG chứa nội dung tệp (dữ liệu người dùng).
internal sealed class CsvTabularSource : ITabularSource
{
    // Trần chống tệp cố ý có nháy mở không đóng làm cả tệp thành MỘT ô nuốt hết bộ nhớ.
    private const int MaxRecordChars = 1_000_000;
    private const int MaxColumns = 1024;

    private static readonly char[] CandidateDelimiters = [',', ';', '\t'];

    private readonly StreamReader _reader;
    private readonly char[] _buffer = new char[8192];
    private int _bufferLength;
    private int _bufferPosition;
    private int _line = 1;
    private char _delimiter = ',';

    private CsvTabularSource(Stream content)
        => _reader = new StreamReader(content, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: true);

    public IReadOnlyList<string> Headers { get; private set; } = [];

    public static async Task<CsvTabularSource> OpenAsync(Stream content, CancellationToken ct)
    {
        var source = new CsvTabularSource(content);
        await source.ReadHeadersAsync(ct);
        return source;
    }

    public async IAsyncEnumerable<TabularRow> ReadRowsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var names = Headers;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var record = await ReadRecordAsync(ct);
            if (record is null)
                yield break;

            var (startLine, fields) = record.Value;
            if (IsBlank(fields))
                continue;

            yield return new TabularRow(startLine, ToValues(names, fields));
        }
    }

    public ValueTask DisposeAsync()
    {
        _reader.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task ReadHeadersAsync(CancellationToken ct)
    {
        // Dò dấu phân tách trên đúng DÒNG ĐẦU: đọc thô tới hết dòng đầu, dò, rồi phân tích lại.
        var firstLine = await PeekFirstLineAsync(ct);
        _delimiter = DetectDelimiter(firstLine);

        while (true)
        {
            var record = await ReadRecordAsync(ct);
            if (record is null)
                throw new TabularFormatException("Tệp không có dòng tiêu đề.");

            if (IsBlank(record.Value.Fields))
                continue;

            var headers = record.Value.Fields.Select(h => h.Trim()).ToList();

            // Cột không tên ở CUỐI dòng tiêu đề (dấu phân tách thừa) là thường gặp — bỏ. Tên rỗng ở GIỮA
            // thì các ô bên dưới không có tên để tra, và là dấu hiệu tệp sai khuôn.
            while (headers.Count > 0 && headers[^1].Length == 0)
                headers.RemoveAt(headers.Count - 1);

            if (headers.Count == 0 || headers.Any(h => h.Length == 0))
                throw new TabularFormatException("Dòng tiêu đề có cột không tên.");

            if (headers.Count > MaxColumns)
                throw new TabularFormatException("Tệp có quá nhiều cột.");

            if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count)
                throw new TabularFormatException("Dòng tiêu đề có tên cột trùng nhau.");

            Headers = headers;
            return;
        }
    }

    // Đọc dòng đầu tiên MÀ KHÔNG tiêu thụ: nạp vào bộ đệm rồi nhìn. Bộ đệm giữ nguyên vị trí nên
    // ReadRecordAsync sau đó đọc lại từ đầu.
    private async Task<string> PeekFirstLineAsync(CancellationToken ct)
    {
        await FillAsync(ct);

        var span = _buffer.AsSpan(_bufferPosition, _bufferLength - _bufferPosition);
        var end = span.IndexOfAny('\r', '\n');
        return new string(end < 0 ? span : span[..end]);
    }

    private static char DetectDelimiter(string firstLine)
    {
        var best = ',';
        var bestCount = 0;

        foreach (var candidate in CandidateDelimiters)
        {
            var count = 0;
            var inQuotes = false;
            foreach (var c in firstLine)
            {
                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == candidate && !inQuotes)
                    count++;
            }

            if (count > bestCount)
            {
                best = candidate;
                bestCount = count;
            }
        }

        return best;
    }

    private static bool IsBlank(List<string> fields)
        => fields.Count == 1 && fields[0].Length == 0;

    private static Dictionary<string, string?> ToValues(IReadOnlyList<string> names, List<string> fields)
    {
        var values = new Dictionary<string, string?>(names.Count, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < names.Count; i++)
        {
            var text = i < fields.Count ? fields[i].Trim() : string.Empty;
            values[names[i]] = text.Length == 0 ? null : text;
        }

        // Bản ghi có NHIỀU ô hơn số cột: chỉ chấp nhận khi phần dư trống (dấu phân tách thừa cuối dòng);
        // ô dư CÓ giá trị là cột lệch — báo lỗi của tệp thay vì lặng lẽ bỏ dữ liệu.
        for (var i = names.Count; i < fields.Count; i++)
        {
            if (fields[i].Trim().Length > 0)
                throw new TabularFormatException("Có dòng nhiều ô hơn số cột của dòng tiêu đề.");
        }

        return values;
    }

    // Trả null ở hết tệp. startLine là dòng bản ghi BẮT ĐẦU.
    private async Task<(int StartLine, List<string> Fields)?> ReadRecordAsync(CancellationToken ct)
    {
        var startLine = _line;
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldWasQuoted = false;
        var recordChars = 0;
        var sawAnything = false;

        while (true)
        {
            var next = await ReadCharAsync(ct);
            if (next < 0)
            {
                if (inQuotes)
                    throw new TabularFormatException("Tệp có nháy kép mở nhưng không đóng.");

                if (!sawAnything)
                    return null;

                fields.Add(field.ToString());
                return (startLine, fields);
            }

            sawAnything = true;
            var c = (char)next;

            if (++recordChars > MaxRecordChars)
                throw new TabularFormatException("Một bản ghi vượt kích thước cho phép.");

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (await PeekCharAsync(ct) == '"')
                    {
                        await ReadCharAsync(ct);
                        field.Append('"');
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                    if (c == '\n')
                        _line++;
                    else if (c == '\r' && await PeekCharAsync(ct) != '\n')
                        _line++;
                }

                continue;
            }

            if (c == '"' && field.Length == 0 && !fieldWasQuoted)
            {
                inQuotes = true;
                fieldWasQuoted = true;
            }
            else if (c == _delimiter)
            {
                fields.Add(field.ToString());
                field.Clear();
                fieldWasQuoted = false;
            }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && await PeekCharAsync(ct) == '\n')
                    await ReadCharAsync(ct);

                _line++;
                fields.Add(field.ToString());
                return (startLine, fields);
            }
            else
            {
                field.Append(c);
            }
        }
    }

    private async ValueTask<int> ReadCharAsync(CancellationToken ct)
    {
        if (_bufferPosition >= _bufferLength && !await FillAsync(ct))
            return -1;

        return _buffer[_bufferPosition++];
    }

    private async ValueTask<int> PeekCharAsync(CancellationToken ct)
    {
        if (_bufferPosition >= _bufferLength && !await FillAsync(ct))
            return -1;

        return _buffer[_bufferPosition];
    }

    // Chỉ nạp khi bộ đệm ĐÃ CẠN — PeekFirstLineAsync gọi lúc bộ đệm còn trống nên không mất dữ liệu.
    private async ValueTask<bool> FillAsync(CancellationToken ct)
    {
        if (_bufferPosition < _bufferLength)
            return true;

        _bufferLength = await _reader.ReadAsync(_buffer.AsMemory(), ct);
        _bufferPosition = 0;
        return _bufferLength > 0;
    }
}
