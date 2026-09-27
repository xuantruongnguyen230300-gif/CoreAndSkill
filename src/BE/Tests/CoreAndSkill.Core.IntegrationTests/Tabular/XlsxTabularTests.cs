using System.IO.Compression;
using System.Text;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Infrastructure.Tabular;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tabular;

// docs/wiki-core/be/15-import-export.md §2.2 (bẫy của Excel), §5.2, §5.4. Bộ đọc/ghi .xlsx là code BCL thuần.
public class XlsxTabularTests
{
    private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static async Task<byte[]> WriteAsync(string[] headers, params string?[][] rows)
    {
        using var output = new MemoryStream();
        await using (var writer = await XlsxTabularWriter.CreateAsync(output, headers, default))
        {
            foreach (var row in rows)
                await writer.WriteRowAsync(row, default);

            await writer.CompleteAsync(default);
        }

        return output.ToArray();
    }

    private static async Task<(IReadOnlyList<string> Headers, List<TabularRow> Rows)> ReadAsync(byte[] bytes)
    {
        await using var source = await XlsxTabularSource.OpenAsync(new MemoryStream(bytes), default);
        var rows = new List<TabularRow>();
        await foreach (var row in source.ReadRowsAsync(default))
            rows.Add(row);

        return (source.Headers, rows);
    }

    // Dựng tệp .xlsx bằng tay để kiểm bộ đọc với những cấu trúc mà bộ ghi của ta không sinh ra (chuỗi dùng chung,
    // ngày theo kiểu ô, hệ ngày 1904…) — đúng những thứ Excel thật ghi.
    private static byte[] BuildWorkbook(
        string sheetXml, string? sharedStrings = null, string? styles = null, bool date1904 = false, string? extraEntry = null)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string name, string content)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }

            Add("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"/>");
            Add("xl/workbook.xml",
                $"<workbook xmlns=\"{MainNs}\" xmlns:r=\"{RelNs}\"><workbookPr{(date1904 ? " date1904=\"1\"" : string.Empty)}/>" +
                "<sheets><sheet name=\"Dữ liệu\" sheetId=\"1\" r:id=\"rId7\"/><sheet name=\"Khác\" sheetId=\"2\" r:id=\"rId8\"/></sheets></workbook>");
            Add("xl/_rels/workbook.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId7\" Type=\"worksheet\" Target=\"worksheets/data.xml\"/>" +
                "<Relationship Id=\"rId8\" Type=\"worksheet\" Target=\"worksheets/other.xml\"/></Relationships>");
            Add("xl/worksheets/data.xml", $"<worksheet xmlns=\"{MainNs}\"><sheetData>{sheetXml}</sheetData></worksheet>");
            Add("xl/worksheets/other.xml", $"<worksheet xmlns=\"{MainNs}\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>SHEET KHÁC</t></is></c></row></sheetData></worksheet>");

            if (sharedStrings is not null)
                Add("xl/sharedStrings.xml", $"<sst xmlns=\"{MainNs}\">{sharedStrings}</sst>");

            if (styles is not null)
                Add("xl/styles.xml", $"<styleSheet xmlns=\"{MainNs}\">{styles}</styleSheet>");

            if (extraEntry is not null)
                Add(extraEntry, "x");
        }

        return stream.ToArray();
    }

    // ---- Ghi ------------------------------------------------------------------------------------

    [Fact]
    public async Task Write_ProducesAValidZipWithTheExpectedParts()
    {
        var bytes = await WriteAsync(["Email"], ["a@vd.vn"]);

        using var zip = new ZipArchive(new MemoryStream(bytes));
        zip.Entries.Select(e => e.FullName).ShouldBe(
            ["[Content_Types].xml", "_rels/.rels", "xl/workbook.xml", "xl/_rels/workbook.xml.rels", "xl/worksheets/sheet1.xml"]);
    }

    [Fact]
    public async Task RoundTrip_ValuesAndRowNumbers_ComeBackIdentical()
    {
        var bytes = await WriteAsync(["Email", "Tên", "Ghi chú"], ["an@vd.vn", "Nguyễn Văn Ân", "xuống\ndòng"], ["b@vd.vn", null, "  khoảng trắng  "]);

        var (headers, rows) = await ReadAsync(bytes);

        headers.ShouldBe(["Email", "Tên", "Ghi chú"]);
        rows.Select(r => r.Number).ShouldBe([2, 3]);
        rows[0].Values["Tên"].ShouldBe("Nguyễn Văn Ân");
        rows[0].Values["Ghi chú"].ShouldBe("xuống\ndòng");
        rows[1].Values["Tên"].ShouldBeNull("ô trống không được ghi ra");
        rows[1].Values["Ghi chú"].ShouldBe("khoảng trắng", "bộ đọc cắt khoảng trắng");
    }

    [Fact]
    public async Task Write_EveryCellIsAnInlineString_SoAFormulaLookingValueCanNeverBeEvaluated()
    {
        var bytes = await WriteAsync(["h"], ["=1+1"], ["@SUM(A1)"], ["-2+3"]);

        using var zip = new ZipArchive(new MemoryStream(bytes));
        using var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheet = await reader.ReadToEndAsync();

        sheet.ShouldNotContain("<f>", Case.Sensitive, "không có phần tử công thức nào");
        sheet.ShouldContain("t=\"inlineStr\"");
        sheet.ShouldContain(">=1+1<");
        // Đọc lại: chính xác là chuỗi gốc, KHÔNG có dấu nháy thêm (khác CSV, xlsx ép kiểu ô là văn bản).
        var (_, rows) = await ReadAsync(bytes);
        rows.Select(r => r.Values["h"]).ShouldBe(["=1+1", "@SUM(A1)", "-2+3"]);
    }

    [Fact]
    public async Task Write_StripsCharactersThatAreIllegalInXml_InsteadOfProducingACorruptFile()
    {
        var bytes = await WriteAsync(["h"], ["a\u0001b\u0000c\u001fd"], ["ok 😀 emoji"]);

        var (_, rows) = await ReadAsync(bytes);

        rows[0].Values["h"].ShouldBe("abcd");
        rows[1].Values["h"].ShouldBe("ok 😀 emoji");
    }

    [Fact]
    public async Task Write_NoRows_StillProducesAReadableFile()
    {
        var (headers, rows) = await ReadAsync(await WriteAsync(["a", "b"]));

        headers.ShouldBe(["a", "b"]);
        rows.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    [InlineData(16383, "XFD")]
    public void ColumnName_FollowsTheSpreadsheetConvention(int index, string expected)
        => XlsxTabularWriter.ColumnName(index).ShouldBe(expected);

    [Fact]
    public async Task RoundTrip_MoreThan26Columns_KeepsEveryValueInItsOwnColumn()
    {
        var headers = Enumerable.Range(0, 30).Select(i => $"c{i}").ToArray();
        var row = Enumerable.Range(0, 30).Select(i => (string?)$"v{i}").ToArray();

        var (readHeaders, rows) = await ReadAsync(await WriteAsync(headers, row));

        readHeaders.ShouldBe(headers);
        rows.Single().Values["c29"].ShouldBe("v29");
        rows.Single().Values["c26"].ShouldBe("v26");
    }

    [Fact]
    public async Task Write_TooManyColumns_IsRejected_NotSilentlyTruncated()
    {
        using var output = new MemoryStream();
        await using var writer = await XlsxTabularWriter.CreateAsync(output, ["a"], default);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            writer.WriteRowAsync(new string?[16_385], default));
    }

    [Fact]
    public async Task Write_CompleteTwice_IsHarmless_AndDisposingWithoutCompleteDoesNotThrow()
    {
        using var output = new MemoryStream();
        var writer = await XlsxTabularWriter.CreateAsync(output, ["a"], default);
        await writer.CompleteAsync(default);
        await writer.CompleteAsync(default);
        await writer.DisposeAsync();

        using var abandoned = new MemoryStream();
        var incomplete = await XlsxTabularWriter.CreateAsync(abandoned, ["a"], default);
        await Should.NotThrowAsync(async () => await incomplete.DisposeAsync());
    }

    [Fact]
    public async Task Write_ToANonSeekableStream_Works_BecauseTheHttpResponseBodyIsOne()
    {
        var target = new MemoryStream();
        await using var forwardOnly = new ForwardOnlyWriteStream(target);
        await using (var writer = await XlsxTabularWriter.CreateAsync(forwardOnly, ["a"], default))
        {
            await writer.WriteRowAsync(["x"], default);
            await writer.CompleteAsync(default);
        }

        var (_, rows) = await ReadAsync(target.ToArray());
        rows.Single().Values["a"].ShouldBe("x");
    }

    // ---- Đọc: cấu trúc do Excel thật sinh ---------------------------------------------------------

    [Fact]
    public async Task Read_SharedStrings_Numbers_Booleans_AndFormulaResults()
    {
        var sheet =
            "<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"B1\" t=\"s\"><v>1</v></c><c r=\"C1\" t=\"s\"><v>2</v></c><c r=\"D1\" t=\"s\"><v>3</v></c></row>" +
            "<row r=\"2\"><c r=\"A2\" t=\"s\"><v>4</v></c><c r=\"B2\"><v>1.5</v></c><c r=\"C2\" t=\"b\"><v>1</v></c><c r=\"D2\" t=\"str\"><f>A2&amp;\"!\"</f><v>Ân!</v></c></row>" +
            "<row r=\"3\"><c r=\"A3\" t=\"s\"><v>5</v></c><c r=\"B3\"><f>1+1</f></c><c r=\"C3\" t=\"b\"><v>0</v></c><c r=\"D3\" t=\"e\"><v>#DIV/0!</v></c></row>";
        var shared = "<si><t>Tên</t></si><si><t>Số</t></si><si><t>Đúng</t></si><si><t>Chuỗi</t></si><si><t>Ân</t></si><si><t>Bình</t></si>";

        var (headers, rows) = await ReadAsync(BuildWorkbook(sheet, shared));

        headers.ShouldBe(["Tên", "Số", "Đúng", "Chuỗi"]);
        rows[0].Values["Tên"].ShouldBe("Ân");
        rows[0].Values["Số"].ShouldBe("1.5");
        rows[0].Values["Đúng"].ShouldBe("TRUE");
        rows[0].Values["Chuỗi"].ShouldBe("Ân!");
        rows[1].Values["Số"].ShouldBeNull("công thức chưa có giá trị lưu sẵn: ô rỗng, không phải công thức");
        rows[1].Values["Đúng"].ShouldBe("FALSE");
        rows[1].Values["Chuỗi"].ShouldBe("#DIV/0!");
    }

    [Fact]
    public async Task Read_RichTextRuns_AreConcatenated_AndPhoneticGuidesAreIgnored()
    {
        var sheet =
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>h</t></is></c><c r=\"B1\" t=\"inlineStr\"><is><t>k</t></is></c></row>" +
            "<row r=\"2\"><c r=\"A2\" t=\"s\"><v>0</v></c><c r=\"B2\" t=\"inlineStr\"><is><r><rPr><b/></rPr><t>Xin </t></r><r><t>chào</t></r></is></c></row>";
        var shared = "<si><r><t>Nguyễn </t></r><r><t>An</t></r><rPh sb=\"0\" eb=\"1\"><t>PHIÊN ÂM</t></rPh></si>";

        var (_, rows) = await ReadAsync(BuildWorkbook(sheet, shared));

        rows.Single().Values["h"].ShouldBe("Nguyễn An");
        rows.Single().Values["k"].ShouldBe("Xin chào");
    }

    [Fact]
    public async Task Read_Dates_AreRecognisedByTheCellFormat_NotByTheDisplayedString()
    {
        // xf 0 = chung, xf 1 = định dạng dựng sẵn 14 (ngày), xf 2 = tuỳ chỉnh 164 "dd/mm/yyyy hh:mm", xf 3 = tuỳ chỉnh 165 '"Mã" 0'.
        var styles =
            "<numFmts><numFmt numFmtId=\"164\" formatCode=\"dd/mm/yyyy\\ hh:mm\"/><numFmt numFmtId=\"165\" formatCode=\"&quot;Mã&quot;\\ 0\"/></numFmts>" +
            "<cellXfs><xf numFmtId=\"0\"/><xf numFmtId=\"14\"/><xf numFmtId=\"164\"/><xf numFmtId=\"165\"/></cellXfs>";
        var sheet =
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>ngay</t></is></c><c r=\"B1\" t=\"inlineStr\"><is><t>gio</t></is></c><c r=\"C1\" t=\"inlineStr\"><is><t>so</t></is></c><c r=\"D1\" t=\"inlineStr\"><is><t>chung</t></is></c></row>" +
            "<row r=\"2\"><c r=\"A2\" s=\"1\"><v>46286</v></c><c r=\"B2\" s=\"2\"><v>46286.5</v></c><c r=\"C2\" s=\"3\"><v>46286</v></c><c r=\"D2\" s=\"0\"><v>46286</v></c></row>";

        var (_, rows) = await ReadAsync(BuildWorkbook(sheet, styles: styles));

        var values = rows.Single().Values;
        values["ngay"].ShouldBe("2026-09-21");
        values["gio"].ShouldBe("2026-09-21T12:00:00");
        values["so"].ShouldBe("46286", "định dạng chữ + số không phải ngày dù chứa 'M'");
        values["chung"].ShouldBe("46286");
    }

    [Fact]
    public async Task Read_TheThousandNineHundredFourDateSystem_IsHonoured()
    {
        var styles = "<cellXfs><xf numFmtId=\"14\"/></cellXfs>";
        var sheet =
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>ngay</t></is></c></row>" +
            "<row r=\"2\"><c r=\"A2\" s=\"0\"><v>44824</v></c></row>";

        var (_, rows) = await ReadAsync(BuildWorkbook(sheet, styles: styles, date1904: true));

        rows.Single().Values["ngay"].ShouldBe("2026-09-21");
    }

    [Fact]
    public async Task Read_OnlyTheFirstSheet_IsRead_EvenWhenRelationshipTargetsAreCustom()
    {
        var sheet = "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>a</t></is></c></row><row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>1</t></is></c></row>";

        var (headers, rows) = await ReadAsync(BuildWorkbook(sheet));

        headers.ShouldBe(["a"]);
        rows.Single().Values["a"].ShouldBe("1");
    }

    [Fact]
    public async Task Read_RowNumbersAreTheSheetRowNumbers_AndGapsAndColumnGapsAreHandled()
    {
        // Tiêu đề ở dòng 3 (hai dòng trống phía trên), dữ liệu ở dòng 4 và 9; cột B để trống ở dòng 9.
        var sheet =
            "<row r=\"1\"/><row r=\"2\"><c r=\"A2\"/></row>" +
            "<row r=\"3\"><c r=\"A3\" t=\"inlineStr\"><is><t>a</t></is></c><c r=\"B3\" t=\"inlineStr\"><is><t>b</t></is></c><c r=\"C3\" t=\"inlineStr\"><is><t>c</t></is></c></row>" +
            "<row r=\"4\"><c r=\"A4\" t=\"inlineStr\"><is><t>x</t></is></c><c r=\"C4\" t=\"inlineStr\"><is><t>z</t></is></c></row>" +
            "<row r=\"9\"><c r=\"A9\" t=\"inlineStr\"><is><t>y</t></is></c></row>";

        var (headers, rows) = await ReadAsync(BuildWorkbook(sheet));

        headers.ShouldBe(["a", "b", "c"]);
        rows.Select(r => r.Number).ShouldBe([4, 9]);
        rows[0].Values["b"].ShouldBeNull();
        rows[0].Values["c"].ShouldBe("z");
        rows[1].Values["a"].ShouldBe("y");
    }

    [Fact]
    public async Task Read_ACellWithNoReference_IsPlacedInSequence()
    {
        var sheet =
            "<row r=\"1\"><c t=\"inlineStr\"><is><t>a</t></is></c><c t=\"inlineStr\"><is><t>b</t></is></c></row>" +
            "<row r=\"2\"><c t=\"inlineStr\"><is><t>1</t></is></c><c t=\"inlineStr\"><is><t>2</t></is></c></row>";

        var (_, rows) = await ReadAsync(BuildWorkbook(sheet));

        rows.Single().Values["b"].ShouldBe("2");
    }

    [Fact]
    public async Task Read_ANonEmptyCellBeyondTheHeaderColumns_IsAFileError()
    {
        var sheet =
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>a</t></is></c></row>" +
            "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>1</t></is></c><c r=\"B2\" t=\"inlineStr\"><is><t>lệch cột</t></is></c></row>";

        await Should.ThrowAsync<TabularFormatException>(() => ReadAsync(BuildWorkbook(sheet)));
    }

    [Fact]
    public async Task Read_UnknownElementsInsideRowsAndCells_AreSkipped()
    {
        var sheet =
            "<row r=\"1\"><extLst><ext/></extLst><c r=\"A1\" t=\"inlineStr\"><is><t>a</t></is><extLst/></c></row>" +
            "<row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>1</t></is></c></row>";

        var (headers, rows) = await ReadAsync(BuildWorkbook(sheet));

        headers.ShouldBe(["a"]);
        rows.Single().Values["a"].ShouldBe("1");
    }

    // ---- Đầu vào KHÔNG đáng tin -------------------------------------------------------------------

    [Fact]
    public async Task Read_ADoctypeInsideTheSheet_IsRejected_ThatBlocksExternalEntitiesAndEntityBombs()
    {
        var bomb = "<!DOCTYPE lol [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;\">]>";
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string name, string content)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }

            Add("xl/workbook.xml", $"<workbook xmlns=\"{MainNs}\"><sheets><sheet name=\"x\" sheetId=\"1\"/></sheets></workbook>");
            Add("xl/worksheets/sheet1.xml", $"{bomb}<worksheet xmlns=\"{MainNs}\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>&b;</t></is></c></row></sheetData></worksheet>");
        }

        await Should.ThrowAsync<TabularFormatException>(() => ReadAsync(stream.ToArray()));
    }

    [Fact]
    public async Task Open_NotAZipAtAll_IsAFileError()
        => await Should.ThrowAsync<TabularFormatException>(() => ReadAsync(Encoding.UTF8.GetBytes("PK\u0003\u0004 không phải zip")));

    [Fact]
    public async Task Open_AZipWithoutAWorkbook_IsAFileError()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            zip.CreateEntry("readme.txt");

        await Should.ThrowAsync<TabularFormatException>(() => ReadAsync(stream.ToArray()));
    }

    [Fact]
    public async Task Open_AnArchiveWithAbsurdlyManyEntries_IsRefusedBeforeAnythingIsRead()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < 2_100; i++)
                zip.CreateEntry($"junk/{i}.txt");
        }

        var ex = await Should.ThrowAsync<TabularFormatException>(() => ReadAsync(stream.ToArray()));
        ex.Message.ShouldContain("quá nhiều mục");
    }

    [Fact]
    public async Task Open_ASheetThatIsNotXml_IsAFileError_NotAnUnhandledXmlException()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string name, string content)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }

            Add("xl/workbook.xml", "not xml at all <<<");
            Add("xl/worksheets/sheet1.xml", "<worksheet/>");
        }

        await Should.ThrowAsync<TabularFormatException>(() => ReadAsync(stream.ToArray()));
    }

    // ---- Bộ chọn bộ đọc theo NỘI DUNG ---------------------------------------------------------------

    [Fact]
    public async Task Reader_PicksXlsxByTheZipSignature_AndCsvOtherwise_RegardlessOfAnyFileName()
    {
        var reader = new TabularReader();
        var xlsx = await WriteAsync(["a"], ["1"]);
        var csv = Encoding.UTF8.GetBytes("a\n1\n");

        await using var fromXlsx = await reader.OpenAsync(new MemoryStream(xlsx), default);
        await using var fromCsv = await reader.OpenAsync(new MemoryStream(csv), default);

        fromXlsx.ShouldBeOfType<XlsxTabularSource>();
        fromCsv.ShouldBeOfType<CsvTabularSource>();
        fromXlsx.Headers.ShouldBe(["a"]);
        fromCsv.Headers.ShouldBe(["a"]);
    }

    [Fact]
    public async Task Reader_RestoresThePosition_WhenTheFileCannotBeOpened()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("\n\n"));

        await Should.ThrowAsync<TabularFormatException>(() => new TabularReader().OpenAsync(stream, default));

        stream.Position.ShouldBe(0);
    }

    [Fact]
    public async Task Reader_ANonSeekableStream_IsAProgrammingError()
        => await Should.ThrowAsync<ArgumentException>(() =>
            new TabularReader().OpenAsync(new ForwardOnlyWriteStream(new MemoryStream()), default));

    [Fact]
    public async Task WriterFactory_CreatesTheRightWriterForEachFormat()
    {
        var factory = new TabularWriterFactory();

        await using var csv = await factory.CreateAsync(TabularFormat.Csv, new MemoryStream(), ["a"], default);
        await using var xlsx = await factory.CreateAsync(TabularFormat.Xlsx, new MemoryStream(), ["a"], default);

        csv.ShouldBeOfType<CsvTabularWriter>();
        xlsx.ShouldBeOfType<XlsxTabularWriter>();
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => factory.CreateAsync((TabularFormat)99, new MemoryStream(), ["a"], default));
    }

    // Thân phản hồi HTTP của Kestrel mặc định: KHÔNG cho ghi đồng bộ. ForwardOnlyWriteStream bên dưới cho phép Write()
    // đồng bộ nên KHÔNG bắt được lỗi này — ZipArchive đóng mục nén bằng Write() đồng bộ kể cả khi đóng bằng
    // DisposeAsync, và bản ghi thẳng vào luồng đích đã nổ ở lần xuất xlsx đầu tiên qua HTTP.
    [Fact]
    public async Task Writer_ToAStreamThatForbidsSynchronousWrites_ProducesAValidWorkbook()
    {
        var output = new AsyncOnlyWriteStream();

        await using (var writer = await XlsxTabularWriter.CreateAsync(output, ["Mã", "Tên"], default))
        {
            await writer.WriteRowAsync(["A1", "Nguyễn An"], default);
            await writer.WriteRowAsync(["A2", "Trần Bình"], default);
            await writer.CompleteAsync(default);
        }

        output.SynchronousWriteAttempts.ShouldBe(0);
        var (headers, rows) = await ReadAsync(output.ToArray());
        headers.ShouldBe(["Mã", "Tên"]);
        rows.Select(r => r.Values["Tên"]).ShouldBe(["Nguyễn An", "Trần Bình"]);
    }

    [Fact]
    public async Task Writer_StreamsTheBytesOutAsRowsAreWritten_NotOnlyAtTheEnd()
    {
        var output = new AsyncOnlyWriteStream();

        await using var writer = await XlsxTabularWriter.CreateAsync(output, ["Mã", "Nội dung"], default);
        for (var i = 0; i < 3000; i++)
            await writer.WriteRowAsync([$"M{i}", new string((char)('a' + (i % 26)), 200) + i], default);

        // Chưa CompleteAsync / DisposeAsync: phần lớn nội dung đã nằm ở đích, bộ đệm trung gian không giữ cả tệp.
        output.Length.ShouldBeGreaterThan(20_000, "bộ nén đã đẩy dữ liệu ra dần theo dòng");
        output.LargestSingleWrite.ShouldBeLessThan(200_000, "không có lần ghi nào là 'cả tệp'");
        await writer.CompleteAsync(default);
    }

    [Fact]
    public async Task CsvWriter_ToAStreamThatForbidsSynchronousWrites_Works()
    {
        var output = new AsyncOnlyWriteStream();

        await using (var writer = await new TabularWriterFactory().CreateAsync(TabularFormat.Csv, output, ["a", "b"], default))
        {
            await writer.WriteRowAsync(["1", "2"], default);
            await writer.CompleteAsync(default);
        }

        output.SynchronousWriteAttempts.ShouldBe(0);
        Encoding.UTF8.GetString(output.ToArray()).ShouldContain("1");
    }

    private sealed class AsyncOnlyWriteStream : Stream
    {
        private readonly MemoryStream _inner = new();

        public int SynchronousWriteAttempts { get; private set; }

        public int LargestSingleWrite { get; private set; }

        public byte[] ToArray() => _inner.ToArray();

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _inner.Length;

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            SynchronousWriteAttempts++;
            throw new InvalidOperationException("Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true.");
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            LargestSingleWrite = Math.Max(LargestSingleWrite, buffer.Length);
            return _inner.WriteAsync(buffer, cancellationToken);
        }
    }

    // Luồng ghi KHÔNG seekable — giống thân phản hồi HTTP.
    private sealed class ForwardOnlyWriteStream(Stream inner) : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => inner.WriteAsync(buffer, cancellationToken);
    }
}
