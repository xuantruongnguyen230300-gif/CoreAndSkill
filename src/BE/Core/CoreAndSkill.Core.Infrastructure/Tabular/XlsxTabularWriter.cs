using System.IO.Compression;
using System.Text;
using System.Xml;
using CoreAndSkill.Core.Application.Tabular;

namespace CoreAndSkill.Core.Infrastructure.Tabular;

// Ghi .xlsx (Office Open XML) bằng BCL thuần — System.IO.Compression + System.Xml, KHÔNG thư viện bảng
// tính (docs/wiki-core/be/15-import-export.md §2, §5.2 "dùng chế độ ghi theo luồng nếu có"). Tài liệu
// không chỉ định thư viện, và chọn một gói NuGet là quyết định kiến trúc — nên ở đây chỉ dựng đúng tập
// con cần cho một bảng phẳng: một sheet, mọi ô là văn bản.
//
// GHI THEO LUỒNG: các phần tĩnh (khai báo kiểu, quan hệ, workbook) ghi trước, sheet1.xml ghi SAU CÙNG và
// từng dòng ra ngay. Hai điều kiện để ZipArchive chạy trên luồng KHÔNG seekable (thân phản hồi HTTP):
// một mục mở tại một thời điểm, và Dispose lưu thư mục trung tâm — nên CompleteAsync đóng sheet còn
// DisposeAsync đóng tệp nén.
//
// ZipArchive KHÔNG ghi thẳng vào luồng đích mà vào DrainableBufferStream, rồi ta xả sang đích bằng đường
// bất đồng bộ sau mỗi dòng / mỗi lần đóng mục. Lý do: thân phản hồi HTTP (Kestrel mặc định) KHÔNG cho ghi
// đồng bộ, mà việc đóng một mục nén ghi phần nén còn lại bằng Write() đồng bộ — kể cả khi đóng bằng
// DisposeAsync và dù dùng ZipArchive.CreateAsync/OpenAsync — nên ghi thẳng ném InvalidOperationException ở
// ngay lần xuất xlsx đầu tiên. Test trên MemoryStream không bắt được lỗi này; hai test bắt nó là
// XlsxTabularTests.Writer_ToAStreamThatForbidsSynchronousWrites_* (luồng cấm ghi đồng bộ) và
// B4ExportEndpointTests.Export_Xlsx_* (đường HTTP thật).
//
// VÔ HIỆU HOÁ CÔNG THỨC (§5.4): mọi ô là `inlineStr` — kiểu văn bản tường minh, không bao giờ là công
// thức, dù nội dung bắt đầu bằng `=`. Đó là cách "ép kiểu ô là văn bản" mà tài liệu nêu, nên KHÔNG thêm
// dấu nháy phía trước (khác CSV, nơi không có khái niệm kiểu ô): thêm vào sẽ làm bẩn dữ liệu hợp lệ.
//
// Giới hạn của định dạng: 16.384 cột, 1.048.576 dòng — vượt thì ném thay vì sinh tệp Excel hỏng.
internal sealed class XlsxTabularWriter : ITabularWriter
{
    private const int MaxColumns = 16_384;
    private const int MaxRows = 1_048_576;

    private const string SheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private readonly Stream _output;
    private readonly DrainableBufferStream _buffer;
    private readonly ZipArchive _archive;
    private readonly Stream _sheetStream;
    private readonly XmlWriter _sheet;
    private int _rowNumber;
    private bool _completed;

    private XlsxTabularWriter(Stream output, DrainableBufferStream buffer, ZipArchive archive, Stream sheetStream, XmlWriter sheet)
    {
        _output = output;
        _buffer = buffer;
        _archive = archive;
        _sheetStream = sheetStream;
        _sheet = sheet;
    }

    public static async Task<XlsxTabularWriter> CreateAsync(Stream output, IReadOnlyList<string> headers, CancellationToken ct)
    {
        // ZipArchive ghi vào bộ đệm, không ghi thẳng vào `output` — xem DrainableBufferStream.
        var buffer = new DrainableBufferStream();
        var archive = await ZipArchive.CreateAsync(buffer, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null, ct);

        await WriteStaticPartAsync(archive, buffer, output, "[Content_Types].xml", ContentTypesXml, ct);
        await WriteStaticPartAsync(archive, buffer, output, "_rels/.rels", RootRelsXml, ct);
        await WriteStaticPartAsync(archive, buffer, output, "xl/workbook.xml", WorkbookXml, ct);
        await WriteStaticPartAsync(archive, buffer, output, "xl/_rels/workbook.xml.rels", WorkbookRelsXml, ct);

        var sheetStream = await archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest).OpenAsync(ct);
        var sheet = XmlWriter.Create(sheetStream, new XmlWriterSettings
        {
            Async = true,
            Encoding = new UTF8Encoding(false),
            CloseOutput = false,
        });

        await sheet.WriteStartDocumentAsync(standalone: true);
        await sheet.WriteStartElementAsync(null, "worksheet", SheetNamespace);
        await sheet.WriteStartElementAsync(null, "sheetData", null);

        var writer = new XlsxTabularWriter(output, buffer, archive, sheetStream, sheet);
        await writer.WriteRowAsync(headers, ct);
        return writer;
    }

    public async Task WriteRowAsync(IReadOnlyList<string?> cells, CancellationToken ct)
    {
        if (cells.Count > MaxColumns)
            throw new InvalidOperationException($"Định dạng xlsx tối đa {MaxColumns} cột.");

        if (++_rowNumber > MaxRows)
            throw new InvalidOperationException($"Định dạng xlsx tối đa {MaxRows} dòng.");

        await _sheet.WriteStartElementAsync(null, "row", null);
        await _sheet.WriteAttributeStringAsync(null, "r", null, _rowNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));

        for (var i = 0; i < cells.Count; i++)
        {
            var text = Sanitize(cells[i]);
            if (text.Length == 0)
                continue; // ô trống = không ghi ô.

            await _sheet.WriteStartElementAsync(null, "c", null);
            await _sheet.WriteAttributeStringAsync(null, "r", null, ColumnName(i) + _rowNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await _sheet.WriteAttributeStringAsync(null, "t", null, "inlineStr");
            await _sheet.WriteStartElementAsync(null, "is", null);
            await _sheet.WriteStartElementAsync(null, "t", null);
            await _sheet.WriteAttributeStringAsync("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
            await _sheet.WriteStringAsync(text);
            await _sheet.WriteEndElementAsync(); // t
            await _sheet.WriteEndElementAsync(); // is
            await _sheet.WriteEndElementAsync(); // c
        }

        await _sheet.WriteEndElementAsync(); // row
        await _buffer.DrainToAsync(_output, ct);
    }

    public async Task CompleteAsync(CancellationToken ct)
    {
        if (_completed)
            return;

        await _sheet.WriteEndElementAsync(); // sheetData
        await _sheet.WriteEndElementAsync(); // worksheet
        await _sheet.WriteEndDocumentAsync();
        await _sheet.FlushAsync();
        await _sheetStream.DisposeAsync();
        await _buffer.DrainToAsync(_output, ct);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            // Không gọi CompleteAsync: tệp sẽ KHÔNG hợp lệ — vẫn đóng để không rò tài nguyên.
            await _sheet.DisposeAsync();
            await _sheetStream.DisposeAsync();
        }

        await _archive.DisposeAsync(); // ghi thư mục trung tâm (vào bộ đệm)
        await _buffer.DrainToAsync(_output, CancellationToken.None);
        await _buffer.DisposeAsync();
    }

    // Loại ký tự không hợp lệ trong XML 1.0 (điều khiển trừ tab/xuống dòng): nếu để lại, XmlWriter ném và
    // tệp hỏng giữa chừng — dữ liệu người dùng là thứ ta không kiểm soát được.
    private static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                builder.Append(c).Append(value[++i]);
            }
            else if (XmlConvert.IsXmlChar(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    // 0 -> A, 25 -> Z, 26 -> AA ...
    internal static string ColumnName(int zeroBasedIndex)
    {
        var name = new StringBuilder();
        for (var n = zeroBasedIndex + 1; n > 0; n = (n - 1) / 26)
            name.Insert(0, (char)('A' + (n - 1) % 26));

        return name.ToString();
    }

    private static async Task WriteStaticPartAsync(
        ZipArchive archive, DrainableBufferStream buffer, Stream output, string name, string content, CancellationToken ct)
    {
        await using (var stream = await archive.CreateEntry(name, CompressionLevel.Fastest).OpenAsync(ct))
        {
            await stream.WriteAsync(new UTF8Encoding(false).GetBytes(content), ct);
        }

        await buffer.DrainToAsync(output, ct);
    }

    private const string ContentTypesXml =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""";

    private const string RootRelsXml =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""";

    private const string WorkbookXml =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Sheet1" sheetId="1" r:id="rId1"/></sheets></workbook>""";

    private const string WorkbookRelsXml =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""";
}
