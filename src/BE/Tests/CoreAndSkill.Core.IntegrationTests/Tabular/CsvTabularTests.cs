using System.Text;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Infrastructure.Tabular;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tabular;

// docs/wiki-core/be/15-import-export.md §2.1 (bẫy của CSV), §3.1, §4.3, §5.2, §5.4. Không cần Postgres: bộ đọc/ghi
// CSV là code thuần trên luồng. Đặt ở IntegrationTests vì kiểu là `internal` của Core.Infrastructure.
public class CsvTabularWriterTests
{
    private static async Task<byte[]> WriteAsync(string[] headers, params string?[][] rows)
    {
        using var output = new MemoryStream();
        await using (var writer = await CsvTabularWriter.CreateAsync(output, headers, default))
        {
            foreach (var row in rows)
                await writer.WriteRowAsync(row, default);

            await writer.CompleteAsync(default);
        }

        return output.ToArray();
    }

    private static async Task<string> WriteTextAsync(string[] headers, params string?[][] rows)
        => new UTF8Encoding(false).GetString((await WriteAsync(headers, rows)).AsSpan(3));

    [Fact]
    public async Task Write_StartsWithAUtf8Bom_SoSpreadsheetSoftwareDoesNotMisreadVietnamese()
    {
        var bytes = await WriteAsync(["Họ tên"], ["Nguyễn Văn Ân"]);

        bytes.Take(3).ShouldBe(new byte[] { 0xEF, 0xBB, 0xBF });
        new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3).ShouldBe("Họ tên\r\nNguyễn Văn Ân\r\n");
    }

    [Fact]
    public async Task Write_UsesCrlfLineEndings()
        => (await WriteTextAsync(["a", "b"], ["1", "2"], ["3", "4"])).ShouldBe("a,b\r\n1,2\r\n3,4\r\n");

    [Theory]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("line1\nline2", "\"line1\nline2\"")]
    [InlineData("line1\r\nline2", "\"line1\r\nline2\"")]
    [InlineData("plain", "plain")]
    [InlineData("có dấu", "có dấu")]
    public async Task Write_QuotesCellsThatContainTheDelimiterQuotesOrLineBreaks(string cell, string expected)
        => (await WriteTextAsync(["h"], [cell])).ShouldBe($"h\r\n{expected}\r\n");

    [Fact]
    public async Task Write_NullCell_IsEmpty()
        => (await WriteTextAsync(["a", "b", "c"], ["1", null, "3"])).ShouldBe("a,b,c\r\n1,,3\r\n");

    // §5.4: ô bắt đầu bằng ký tự công thức bị vô hiệu hoá — trách nhiệm của phía XUẤT.
    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+cmd|' /C calc'!A0", "'+cmd|' /C calc'!A0")]
    [InlineData("-2+3", "'-2+3")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\t=1", "'\t=1")]
    [InlineData("=HYPERLINK(\"http://evil\",\"x\")", "\"'=HYPERLINK(\"\"http://evil\"\",\"\"x\"\")\"")]
    public async Task Write_NeutralisesCellsThatWouldBeReadAsFormulas(string cell, string expectedCell)
        => (await WriteTextAsync(["h"], [cell])).ShouldBe($"h\r\n{expectedCell}\r\n");

    [Theory]
    [InlineData("1=1")]
    [InlineData("a+b")]
    [InlineData("email@vd.vn")]
    [InlineData("50%")]
    [InlineData("")]
    public async Task Write_LeavesOrdinaryCellsAlone_EvenWithFormulaCharactersInside(string cell)
        => (await WriteTextAsync(["h"], [cell])).ShouldBe($"h\r\n{cell}\r\n");

    [Fact]
    public async Task Write_TheHeaderRowIsAlsoNeutralised()
        => (await WriteTextAsync(["=evil"], ["x"])).ShouldStartWith("'=evil\r\n");

    [Fact]
    public async Task Write_DoesNotCloseTheOutputStream()
    {
        using var output = new MemoryStream();
        await using (var writer = await CsvTabularWriter.CreateAsync(output, ["a"], default))
            await writer.CompleteAsync(default);

        output.CanWrite.ShouldBeTrue("luồng do chỗ gọi đóng, không phải bộ ghi");
    }

    [Fact]
    public void NeutralizeFormula_ExposedForTests_Matches()
    {
        CsvTabularWriter.NeutralizeFormula("=x").ShouldBe("'=x");
        CsvTabularWriter.NeutralizeFormula("x").ShouldBe("x");
        CsvTabularWriter.NeutralizeFormula(string.Empty).ShouldBe(string.Empty);
    }
}

public class CsvTabularSourceTests
{
    private static Task<CsvTabularSource> OpenAsync(string text, Encoding? encoding = null, bool bom = false)
    {
        var enc = encoding ?? new UTF8Encoding(false);
        var bytes = bom ? [.. enc.GetPreamble(), .. enc.GetBytes(text)] : enc.GetBytes(text);
        return CsvTabularSource.OpenAsync(new MemoryStream(bytes), default);
    }

    private static async Task<List<TabularRow>> ReadAllAsync(CsvTabularSource source)
    {
        var rows = new List<TabularRow>();
        await foreach (var row in source.ReadRowsAsync(default))
            rows.Add(row);

        return rows;
    }

    [Fact]
    public async Task Read_HeadersAndRows_WithLineNumbersOfTheOriginalFile()
    {
        await using var source = await OpenAsync("Email,Tên\nan@vd.vn,An\nbinh@vd.vn,Bình\n");

        source.Headers.ShouldBe(["Email", "Tên"]);
        var rows = await ReadAllAsync(source);

        rows.Select(r => r.Number).ShouldBe([2, 3], "dòng tiêu đề là dòng 1 — số dòng là số dòng trong tệp gốc");
        rows[0].Values["Email"].ShouldBe("an@vd.vn");
        rows[1].Values["Tên"].ShouldBe("Bình");
    }

    [Fact]
    public async Task Read_HeaderLookupIgnoresCase()
    {
        await using var source = await OpenAsync("Email\nx@vd.vn\n");

        (await ReadAllAsync(source))[0].Values["EMAIL"].ShouldBe("x@vd.vn");
    }

    [Theory]
    [InlineData("a;b\n1;2\n")]
    [InlineData("a\tb\n1\t2\n")]
    [InlineData("a,b\n1,2\n")]
    public async Task Read_DetectsTheDelimiterFromTheHeaderLine(string text)
    {
        await using var source = await OpenAsync(text);

        source.Headers.ShouldBe(["a", "b"]);
        (await ReadAllAsync(source)).Single().Values["b"].ShouldBe("2");
    }

    [Fact]
    public async Task Read_QuotedFields_KeepDelimitersEscapedQuotesAndLineBreaksInside()
    {
        // Ghi chú dài có dấu phẩy, nháy và xuống dòng — đúng ca mà cắt chuỗi theo dấu phẩy làm sai.
        await using var source = await OpenAsync("Email,Ghi chú\nan@vd.vn,\"Xin chào, \"\"An\"\"\nDòng hai\"\nb@vd.vn,ok\n");

        var rows = await ReadAllAsync(source);

        rows.Count.ShouldBe(2);
        rows[0].Values["Ghi chú"].ShouldBe("Xin chào, \"An\"\nDòng hai");
        rows[1].Values["Email"].ShouldBe("b@vd.vn");
    }

    [Fact]
    public async Task Read_ARecordSpanningLines_ReportsTheLineItStartedOn_AndTheNextRecordCountsTheExtraLines()
    {
        await using var source = await OpenAsync("a,b\n1,\"x\ny\nz\"\n2,w\n");

        var rows = await ReadAllAsync(source);

        rows.Select(r => r.Number).ShouldBe([2, 5], "bản ghi 1 bắt đầu ở dòng 2 và chiếm dòng 2-4; bản ghi 2 ở dòng 5");
    }

    [Fact]
    public async Task Read_BlankLines_AreSkipped_ButStillCounted()
    {
        await using var source = await OpenAsync("a\n\n1\n\n\n2\n");

        (await ReadAllAsync(source)).Select(r => r.Number).ShouldBe([3, 6]);
    }

    [Theory]
    [InlineData("a\r\n1\r\n2\r\n")]
    [InlineData("a\n1\n2\n")]
    [InlineData("a\r1\r2\r")]
    [InlineData("a\n1\n2")]
    public async Task Read_AnyLineEnding_AndAFinalLineWithoutOne(string text)
    {
        await using var source = await OpenAsync(text);

        var rows = await ReadAllAsync(source);

        rows.Select(r => r.Values["a"]).ShouldBe(["1", "2"]);
        rows.Select(r => r.Number).ShouldBe([2, 3]);
    }

    [Fact]
    public async Task Read_TrimsWhitespace_AndTreatsEmptyAndBlankCellsAsNull()
    {
        await using var source = await OpenAsync("a,b,c\n  x  ,,   \n");

        var values = (await ReadAllAsync(source)).Single().Values;

        values["a"].ShouldBe("x");
        values["b"].ShouldBeNull();
        values["c"].ShouldBeNull();
    }

    [Fact]
    public async Task Read_FewerCellsThanHeaders_MissingOnesAreNull()
    {
        await using var source = await OpenAsync("a,b,c\n1\n");

        var values = (await ReadAllAsync(source)).Single().Values;

        values["a"].ShouldBe("1");
        values["b"].ShouldBeNull();
        values["c"].ShouldBeNull();
    }

    [Fact]
    public async Task Read_TrailingEmptyDelimiters_AreTolerated_ButANonEmptyExtraCellIsAFileError()
    {
        await using var tolerated = await OpenAsync("a,b\n1,2,,\n");
        (await ReadAllAsync(tolerated)).Single().Values["b"].ShouldBe("2");

        await using var shifted = await OpenAsync("a,b\n1,2,3\n");
        await Should.ThrowAsync<TabularFormatException>(() => ReadAllAsync(shifted));
    }

    [Fact]
    public async Task Read_Utf8Bom_IsDetectedAndNotPartOfTheFirstHeader()
    {
        await using var source = await OpenAsync("Email\nx@vd.vn\n", bom: true);

        source.Headers.ShouldBe(["Email"]);
    }

    [Fact]
    public async Task Read_Utf16WithBom_IsDecodedCorrectly()
    {
        await using var source = await OpenAsync("Tên;Địa chỉ\nÂn;Hà Nội\n", new UnicodeEncoding(false, true), bom: true);

        source.Headers.ShouldBe(["Tên", "Địa chỉ"]);
        (await ReadAllAsync(source)).Single().Values["Địa chỉ"].ShouldBe("Hà Nội");
    }

    [Fact]
    public async Task Read_LeadingBlankLines_BeforeTheHeader_AreSkipped()
    {
        await using var source = await OpenAsync("\n\nEmail\nx@vd.vn\n");

        source.Headers.ShouldBe(["Email"]);
        (await ReadAllAsync(source)).Single().Number.ShouldBe(4);
    }

    [Fact]
    public async Task Read_HeaderOnly_HasNoRows()
    {
        await using var source = await OpenAsync("a,b\n");

        (await ReadAllAsync(source)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n\n")]
    public async Task Open_NoHeaderAtAll_IsAFileError(string text)
        => await Should.ThrowAsync<TabularFormatException>(() => OpenAsync(text));

    [Theory]
    [InlineData("a,A\n1,2\n", "trùng tên cột, không phân biệt hoa thường")]
    [InlineData("a,,c\n1,2,3\n", "cột không tên ở giữa")]
    public async Task Open_AmbiguousHeaders_AreAFileError(string text, string because)
        => await Should.ThrowAsync<TabularFormatException>(() => OpenAsync(text), because);

    [Fact]
    public async Task Open_ATrailingEmptyHeaderCell_IsDropped()
    {
        await using var source = await OpenAsync("a,b,\n1,2,\n");

        source.Headers.ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task Read_AnUnterminatedQuote_IsAFileError_NotAHang()
    {
        await using var source = await OpenAsync("a,b\n1,\"never closed\n2,3\n");

        await Should.ThrowAsync<TabularFormatException>(() => ReadAllAsync(source));
    }

    [Fact]
    public async Task Read_ARunawayRecord_IsCutOffInsteadOfSwallowingMemory()
    {
        await using var source = await OpenAsync("a\n\"" + new string('x', 1_100_000) + "\"\n");

        var ex = await Should.ThrowAsync<TabularFormatException>(() => ReadAllAsync(source));
        ex.Message.ShouldNotContain("xxxx", Case.Sensitive, "thông điệp không chứa nội dung tệp (dữ liệu người dùng)");
    }

    [Fact]
    public async Task Read_TooManyColumns_IsAFileError()
    {
        var header = string.Join(',', Enumerable.Range(0, 1100).Select(i => $"c{i}"));

        await Should.ThrowAsync<TabularFormatException>(() => OpenAsync(header + "\n1\n"));
    }

    [Fact]
    public async Task Read_QuoteInTheMiddleOfAnUnquotedField_IsKeptLiterally()
    {
        await using var source = await OpenAsync("a\n5\" pipe\n");

        (await ReadAllAsync(source)).Single().Values["a"].ShouldBe("5\" pipe");
    }

    // Vòng đi vòng về: thứ bộ ghi sinh ra thì bộ đọc đọc lại đúng — kể cả giá trị khó.
    [Fact]
    public async Task RoundTrip_WriterOutput_IsReadBackIdentically()
    {
        string?[][] rows =
        [
            ["an@vd.vn", "Nguyễn Văn Ân", "ghi chú, có dấu phẩy"],
            ["b@vd.vn", "Trần \"Bình\"", "xuống\r\ndòng"],
            ["c@vd.vn", "", "cuối"],
        ];
        using var buffer = new MemoryStream();
        await using (var writer = await CsvTabularWriter.CreateAsync(buffer, ["Email", "Tên", "Ghi chú"], default))
        {
            foreach (var row in rows)
                await writer.WriteRowAsync(row, default);

            await writer.CompleteAsync(default);
        }

        buffer.Position = 0;
        await using var source = await CsvTabularSource.OpenAsync(buffer, default);
        var read = await ReadAllAsync(source);

        read.Count.ShouldBe(3);
        read[0].Values["Ghi chú"].ShouldBe("ghi chú, có dấu phẩy");
        read[1].Values["Tên"].ShouldBe("Trần \"Bình\"");
        read[1].Values["Ghi chú"].ShouldBe("xuống\r\ndòng");
        read[2].Values["Tên"].ShouldBeNull();
        read.Select(r => r.Number).ShouldBe([2, 3, 5], "bản ghi 2 chiếm hai dòng vì có xuống dòng bên trong");
    }
}
