using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CoreAndSkill.Core.Application.Tabular;

namespace CoreAndSkill.Core.Infrastructure.Tabular;

// Đọc .xlsx (Office Open XML) THEO DÒNG bằng BCL thuần — docs/wiki-core/be/15-import-export.md §2.2, §3.1.
// Phạm vi có chủ đích: SHEET ĐẦU TIÊN, dòng đầu là tiêu đề; ô văn bản (dùng chung, nội tuyến, công thức trả
// chuỗi), số, boolean, và NGÀY. Không đọc: nhiều sheet, ô gộp, biểu đồ, ảnh, macro.
//
// Bẫy của Excel (§2.2) đã xử lý:
//   - Ô có công thức: đọc GIÁ TRỊ ĐÃ TÍNH (`<v>`); tệp chưa từng mở bằng Excel thì có thể chưa có giá trị
//     lưu sẵn — ô đó rỗng, không phải công thức.
//   - Ngày là số: nhận diện theo ĐỊNH DẠNG SỐ CỦA Ô (styles.xml), không theo chuỗi hiển thị, rồi đổi về
//     ISO 8601 (yyyy-MM-dd hoặc yyyy-MM-ddTHH:mm:ss). Hệ ngày 1904 được tính. Không nhận diện được định
//     dạng thì đọc số thô — lỗi lộ ra ở bước kiểm hợp lệ của dòng, không lặng lẽ sai ngày.
//   - Số 0 đầu / số dài mất chữ số: KHÔNG sửa được ở phía đọc — nó hỏng từ trước khi hệ thống nhìn thấy
//     tệp; phòng bằng tệp mẫu định dạng cột mã là văn bản (§7).
//   - Ô trống và ô khoảng trắng: cắt khoảng trắng, chuỗi rỗng là null.
//
// Số dòng là SỐ DÒNG TRONG SHEET (thuộc tính `r` của <row>) — đúng thứ người dùng thấy ở lề trái Excel.
//
// TỆP NÉN LÀ ĐẦU VÀO KHÔNG ĐÁNG TIN — chống zip-bomb: số mục tối đa, kích thước GIẢI NÉN của mỗi phần
// được đo bằng số byte THỰC ĐỌC (không tin kích thước khai trong thư mục trung tâm, vốn giả được), và XML
// bị cấm DTD (chống thực thể ngoài và bom thực thể lồng nhau).
internal sealed class XlsxTabularSource : ITabularSource
{
    private const int MaxEntries = 2_000;
    private const long MaxSheetBytes = 256L * 1024 * 1024;
    private const long MaxAuxiliaryBytes = 64L * 1024 * 1024;
    private const int MaxColumns = 1024;

    private static readonly XmlReaderSettings XmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        Async = true,
    };

    private readonly ZipArchive _archive;
    private readonly ZipArchiveEntry _sheetEntry;
    private readonly IReadOnlyList<string> _sharedStrings;
    private readonly IReadOnlyList<bool> _dateStyles;
    private readonly bool _date1904;

    private XlsxTabularSource(
        ZipArchive archive, ZipArchiveEntry sheetEntry, IReadOnlyList<string> sharedStrings,
        IReadOnlyList<bool> dateStyles, bool date1904)
    {
        _archive = archive;
        _sheetEntry = sheetEntry;
        _sharedStrings = sharedStrings;
        _dateStyles = dateStyles;
        _date1904 = date1904;
    }

    public IReadOnlyList<string> Headers { get; private set; } = [];

    public static async Task<XlsxTabularSource> OpenAsync(Stream content, CancellationToken ct)
    {
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            throw new TabularFormatException("Tệp không phải bảng tính hợp lệ.");
        }

        try
        {
            if (archive.Entries.Count > MaxEntries)
                throw new TabularFormatException("Tệp nén có quá nhiều mục.");

            var (sheetPath, date1904) = await FindFirstSheetAsync(archive, ct);
            var sheetEntry = archive.GetEntry(sheetPath)
                ?? throw new TabularFormatException("Không tìm thấy sheet đầu tiên.");

            var shared = await ReadSharedStringsAsync(archive, ct);
            var dateStyles = await ReadDateStylesAsync(archive, ct);

            var source = new XlsxTabularSource(archive, sheetEntry, shared, dateStyles, date1904);
            await source.ReadHeadersAsync(ct);
            return source;
        }
        catch (Exception ex) when (ex is XmlException or InvalidDataException)
        {
            archive.Dispose();
            throw new TabularFormatException("Tệp bảng tính hỏng hoặc không đọc được.");
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    public IAsyncEnumerable<TabularRow> ReadRowsAsync(CancellationToken ct)
        => Guard(ReadRowsCoreAsync(ct), ct);

    private async IAsyncEnumerable<TabularRow> ReadRowsCoreAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var row in ReadSheetRowsAsync(ct))
            yield return ToRow(row);
    }

    // XML hỏng hoặc luồng giải nén hỏng giữa chừng là lỗi CỦA TỆP — đổi về TabularFormatException để
    // tầng trên xử lý MỘT kiểu lỗi. `yield` không được nằm trong try có catch, nên bọc quanh MoveNext.
    private static async IAsyncEnumerable<TabularRow> Guard(
        IAsyncEnumerable<TabularRow> inner, [EnumeratorCancellation] CancellationToken ct)
    {
        await using var enumerator = inner.GetAsyncEnumerator(ct);
        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await enumerator.MoveNextAsync();
            }
            catch (Exception ex) when (ex is XmlException or InvalidDataException)
            {
                throw new TabularFormatException("Tệp bảng tính hỏng hoặc không đọc được.");
            }

            if (!hasNext)
                yield break;

            yield return enumerator.Current;
        }
    }

    public ValueTask DisposeAsync()
    {
        _archive.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task ReadHeadersAsync(CancellationToken ct)
    {
        await foreach (var row in ReadSheetRowsAsync(ct))
        {
            // Dòng đầu CÓ giá trị là dòng tiêu đề. Dòng trống phía trên bị bỏ qua.
            var names = new List<string>();
            var maxIndex = row.Cells.Keys.DefaultIfEmpty(-1).Max();
            for (var i = 0; i <= maxIndex; i++)
                names.Add(row.Cells.TryGetValue(i, out var text) ? text.Trim() : string.Empty);

            while (names.Count > 0 && names[^1].Length == 0)
                names.RemoveAt(names.Count - 1);

            if (names.Count == 0 || names.Any(n => n.Length == 0))
                throw new TabularFormatException("Dòng tiêu đề có cột không tên.");

            if (names.Count > MaxColumns)
                throw new TabularFormatException("Tệp có quá nhiều cột.");

            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
                throw new TabularFormatException("Dòng tiêu đề có tên cột trùng nhau.");

            Headers = names;
            return;
        }

        throw new TabularFormatException("Tệp không có dòng tiêu đề.");
    }

    // Dòng dữ liệu đã qua dòng tiêu đề — ReadSheetRowsAsync mở lại sheet từ đầu và bỏ qua tới dòng tiêu đề.
    private async IAsyncEnumerable<SheetRow> ReadSheetRowsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await using var limited = new LimitedReadStream(_sheetEntry.Open(), MaxSheetBytes);
        using var xml = XmlReader.Create(limited, XmlSettings);

        var headerSeen = Headers.Count > 0;
        var headerRowNumber = -1;

        while (await xml.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            if (xml.NodeType != XmlNodeType.Element || xml.LocalName != "row")
                continue;

            var rowNumber = int.TryParse(xml.GetAttribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out var r) ? r : 0;
            var cells = await ReadRowCellsAsync(xml, ct);

            if (cells.Count == 0 || cells.Values.All(v => v.Trim().Length == 0))
                continue;

            if (!headerSeen)
            {
                // Đang đọc CHÍNH dòng tiêu đề (lần gọi từ ReadHeadersAsync).
                yield return new SheetRow(rowNumber, cells);
                yield break;
            }

            if (headerRowNumber < 0)
            {
                // Lần gọi từ ReadRowsAsync: dòng CÓ GIÁ TRỊ đầu tiên chính là dòng tiêu đề — bỏ qua.
                headerRowNumber = rowNumber;
                continue;
            }

            yield return new SheetRow(rowNumber, cells);
        }
    }

    private async Task<Dictionary<int, string>> ReadRowCellsAsync(XmlReader xml, CancellationToken ct)
    {
        var cells = new Dictionary<int, string>();
        if (xml.IsEmptyElement)
            return cells;

        var depth = xml.Depth;
        var nextColumn = 0;

        // Quy ước duyệt của mọi hàm đọc XML trong tệp này: đứng trên phần tử mở; xong thì đứng trên
        // THẺ ĐÓNG của nó (hoặc chính nó nếu rỗng) — người gọi tự Read tiếp. Hàm nào nuốt trọn một phần tử
        // (ReadElementContentAsString, Skip) đã đứng SAU phần tử đó nên KHÔNG được Read thêm, nếu không sẽ
        // bỏ mất nút kế tiếp.
        await xml.ReadAsync();
        while (xml.Depth > depth)
        {
            ct.ThrowIfCancellationRequested();

            if (xml.NodeType != XmlNodeType.Element)
            {
                await xml.ReadAsync();
                continue;
            }

            if (xml.LocalName != "c")
            {
                await xml.SkipAsync();
                continue;
            }

            var reference = xml.GetAttribute("r");
            var column = reference is null ? nextColumn : ColumnIndex(reference);
            nextColumn = column + 1;

            if (column >= MaxColumns)
                throw new TabularFormatException("Tệp có quá nhiều cột.");

            var type = xml.GetAttribute("t");
            var style = int.TryParse(xml.GetAttribute("s"), NumberStyles.None, CultureInfo.InvariantCulture, out var s) ? s : 0;

            var value = await ReadCellValueAsync(xml, type, style, ct);
            if (value is not null)
                cells[column] = value;

            await xml.ReadAsync();
        }

        return cells;
    }

    private async Task<string?> ReadCellValueAsync(XmlReader xml, string? type, int style, CancellationToken ct)
    {
        if (xml.IsEmptyElement)
            return null;

        var depth = xml.Depth;
        string? raw = null;
        StringBuilder? inline = null;

        await xml.ReadAsync();
        while (xml.Depth > depth)
        {
            ct.ThrowIfCancellationRequested();

            if (xml.NodeType != XmlNodeType.Element)
            {
                await xml.ReadAsync();
                continue;
            }

            switch (xml.LocalName)
            {
                case "v":
                    raw = await xml.ReadElementContentAsStringAsync();
                    break;

                case "is":
                    inline = new StringBuilder();
                    await AppendTextRunsAsync(xml, inline, ct);
                    await xml.ReadAsync(); // AppendTextRunsAsync đứng trên </is>
                    break;

                default:
                    await xml.SkipAsync();
                    break;
            }
        }

        return type switch
        {
            "s" => raw is not null && int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index < _sharedStrings.Count
                ? _sharedStrings[index]
                : null,
            "inlineStr" => inline?.ToString(),
            "b" => raw switch { "1" => "TRUE", "0" => "FALSE", _ => null },
            "str" or "e" => raw,
            _ => ConvertNumber(raw, style),
        };
    }

    private string? ConvertNumber(string? raw, int style)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (style >= 0 && style < _dateStyles.Count && _dateStyles[style]
            && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)
            && serial is >= 0 and < 2_958_466)
        {
            var date = DateTime.FromOADate(_date1904 ? serial + 1462 : serial);
            return date.TimeOfDay == TimeSpan.Zero
                ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : date.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return raw;
    }

    private static async Task AppendTextRunsAsync(XmlReader xml, StringBuilder builder, CancellationToken ct)
    {
        if (xml.IsEmptyElement)
            return;

        var depth = xml.Depth;
        await xml.ReadAsync();
        while (xml.Depth > depth)
        {
            ct.ThrowIfCancellationRequested();

            if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "t")
            {
                builder.Append(await xml.ReadElementContentAsStringAsync());
            }
            else if (xml.NodeType == XmlNodeType.Element && xml.LocalName is "rPh" or "phoneticPr")
            {
                // Chú âm (tiếng Nhật) không phải văn bản của ô — bỏ cả nhánh để không nối lẫn vào.
                await xml.SkipAsync();
            }
            else
            {
                // <r> (đoạn có định dạng) và mọi nút khác: đi vào / qua.
                await xml.ReadAsync();
            }
        }
    }

    private TabularRow ToRow(SheetRow row)
    {
        var values = new Dictionary<string, string?>(Headers.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Headers.Count; i++)
        {
            var text = row.Cells.TryGetValue(i, out var cell) ? cell.Trim() : string.Empty;
            values[Headers[i]] = text.Length == 0 ? null : text;
        }

        // Cùng luật với CSV: ô dư có giá trị là cột lệch — lỗi của tệp, không lặng lẽ bỏ dữ liệu.
        foreach (var (column, text) in row.Cells)
        {
            if (column >= Headers.Count && text.Trim().Length > 0)
                throw new TabularFormatException("Có dòng nhiều ô hơn số cột của dòng tiêu đề.");
        }

        return new TabularRow(row.Number, values);
    }

    private static int ColumnIndex(string reference)
    {
        var index = 0;
        foreach (var c in reference)
        {
            if (!char.IsAsciiLetterUpper(c))
                break;

            index = index * 26 + (c - 'A' + 1);
            if (index > 16_384)
                throw new TabularFormatException("Tham chiếu ô không hợp lệ.");
        }

        return Math.Max(index - 1, 0);
    }

    // ---- Phần phụ trợ ---------------------------------------------------------------------

    private static async Task<(string SheetPath, bool Date1904)> FindFirstSheetAsync(ZipArchive archive, CancellationToken ct)
    {
        var workbook = archive.GetEntry("xl/workbook.xml")
            ?? throw new TabularFormatException("Tệp không có xl/workbook.xml.");

        string? firstRelationshipId = null;
        var date1904 = false;

        await using (var limited = new LimitedReadStream(workbook.Open(), MaxAuxiliaryBytes))
        using (var xml = XmlReader.Create(limited, XmlSettings))
        {
            while (await xml.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();

                if (xml.NodeType != XmlNodeType.Element)
                    continue;

                if (xml.LocalName == "workbookPr")
                    date1904 = xml.GetAttribute("date1904") is "1" or "true";
                else if (xml.LocalName == "sheet" && firstRelationshipId is null)
                    firstRelationshipId = xml.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            }
        }

        var fallback = "xl/worksheets/sheet1.xml";
        var rels = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (firstRelationshipId is null || rels is null)
            return (fallback, date1904);

        await using var relsLimited = new LimitedReadStream(rels.Open(), MaxAuxiliaryBytes);
        using var relsXml = XmlReader.Create(relsLimited, XmlSettings);
        while (await relsXml.ReadAsync())
        {
            if (relsXml.NodeType == XmlNodeType.Element
                && relsXml.LocalName == "Relationship"
                && relsXml.GetAttribute("Id") == firstRelationshipId
                && relsXml.GetAttribute("Target") is { } target)
            {
                var path = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
                return (path.Replace("../", string.Empty, StringComparison.Ordinal), date1904);
            }
        }

        return (fallback, date1904);
    }

    private static async Task<IReadOnlyList<string>> ReadSharedStringsAsync(ZipArchive archive, CancellationToken ct)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return [];

        var strings = new List<string>();
        await using var limited = new LimitedReadStream(entry.Open(), MaxAuxiliaryBytes);
        using var xml = XmlReader.Create(limited, XmlSettings);

        while (await xml.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "si")
            {
                var builder = new StringBuilder();
                await AppendTextRunsAsync(xml, builder, ct);
                strings.Add(builder.ToString());
            }
        }

        return strings;
    }

    // Với mỗi chỉ số kiểu (cellXfs), ô đó có phải định dạng NGÀY/GIỜ không.
    private static async Task<IReadOnlyList<bool>> ReadDateStylesAsync(ZipArchive archive, CancellationToken ct)
    {
        var entry = archive.GetEntry("xl/styles.xml");
        if (entry is null)
            return [];

        var customFormats = new Dictionary<int, bool>();
        var styles = new List<bool>();

        await using var limited = new LimitedReadStream(entry.Open(), MaxAuxiliaryBytes);
        using var xml = XmlReader.Create(limited, XmlSettings);

        var inCellXfs = false;
        while (await xml.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            if (xml.NodeType == XmlNodeType.EndElement && xml.LocalName == "cellXfs")
            {
                inCellXfs = false;
            }
            else if (xml.NodeType == XmlNodeType.Element)
            {
                switch (xml.LocalName)
                {
                    case "numFmt" when int.TryParse(xml.GetAttribute("numFmtId"), NumberStyles.None, CultureInfo.InvariantCulture, out var id):
                        customFormats[id] = LooksLikeDateFormat(xml.GetAttribute("formatCode") ?? string.Empty);
                        break;

                    case "cellXfs":
                        inCellXfs = !xml.IsEmptyElement;
                        break;

                    case "xf" when inCellXfs:
                        var formatId = int.TryParse(xml.GetAttribute("numFmtId"), NumberStyles.None, CultureInfo.InvariantCulture, out var f) ? f : 0;
                        styles.Add(customFormats.TryGetValue(formatId, out var custom) ? custom : IsBuiltInDateFormat(formatId));
                        break;
                }
            }
        }

        return styles;
    }

    // Định dạng dựng sẵn của Excel là NGÀY/GIỜ: 14-22, 27-36, 45-47, 50-58 (đủ cho các bản địa hoá phổ biến).
    private static bool IsBuiltInDateFormat(int id)
        => id is (>= 14 and <= 22) or (>= 27 and <= 36) or (>= 45 and <= 47) or (>= 50 and <= 58);

    private static bool LooksLikeDateFormat(string formatCode)
    {
        // Bỏ phần trong nháy, trong ngoặc vuông ([Red], [$-409]) và ký tự thoát trước khi tìm ký tự ngày giờ.
        var stripped = Regex.Replace(formatCode, "\"[^\"]*\"|\\[[^\\]]*\\]|\\\\.", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(200));
        return stripped.IndexOfAny(['y', 'Y', 'd', 'D', 'h', 'H', 's', 'S', 'm', 'M']) >= 0;
    }

    private sealed record SheetRow(int Number, Dictionary<int, string> Cells);

    // Đếm byte THỰC ĐỌC từ luồng giải nén: kích thước khai trong thư mục trung tâm của tệp nén giả được.
    private sealed class LimitedReadStream(Stream inner, long limit) : Stream
    {
        private long _total;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Track(inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer) => Track(inner.Read(buffer));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => Track(await inner.ReadAsync(buffer, cancellationToken));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override ValueTask DisposeAsync() => inner.DisposeAsync();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                inner.Dispose();

            base.Dispose(disposing);
        }

        private int Track(int read)
        {
            _total += read;
            if (_total > limit)
                throw new TabularFormatException("Nội dung giải nén vượt giới hạn cho phép.");

            return read;
        }
    }
}
