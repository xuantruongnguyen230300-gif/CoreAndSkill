using CoreAndSkill.Core.Application.Files;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Files;

public class FileNameSanitizerTests
{
    [Theory]
    [InlineData("quyet-dinh.pdf", "quyet-dinh.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("..\\..\\windows\\system32\\cmd.exe", "cmd.exe")]
    [InlineData("C:\\Users\\an\\bao-cao.xlsx", "bao-cao.xlsx")]
    [InlineData("/var/tmp/a b.txt", "a b.txt")]
    [InlineData("tệp có dấu.docx", "tệp có dấu.docx")]
    public void Sanitize_KeepsOnlyTheLastPathSegment(string clientName, string expected)
        => FileNameSanitizer.Sanitize(clientName).ShouldBe(expected);

    [Fact]
    public void Sanitize_RemovesControlCharacters()
        => FileNameSanitizer.Sanitize("a\u0000b\r\nc.txt").ShouldBe("abc.txt");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("...")]
    [InlineData("folder/")]
    [InlineData("\u0001\u0002")]
    public void Sanitize_FallsBack_WhenNothingUsableIsLeft(string? clientName)
        => FileNameSanitizer.Sanitize(clientName).ShouldBe(FileNameSanitizer.Fallback);

    [Fact]
    public void Sanitize_TruncatesToTheColumnWidth()
        => FileNameSanitizer.Sanitize(new string('a', 400) + ".pdf").Length.ShouldBe(FileNameSanitizer.MaxLength);

    // Tên tải về: đuôi theo KIỂU ĐÃ XÁC ĐỊNH, không theo đuôi client đặt — ADR-0050, RULES.md S16.
    [Theory]
    [InlineData("x.bat", FileContentDetector.PlainText, "x.txt")]
    [InlineData("hop-dong.docm", FileContentDetector.Docx, "hop-dong.docx")]
    [InlineData("quyet-dinh.pdf", FileContentDetector.Pdf, "quyet-dinh.pdf")]
    [InlineData("anh.PNG", FileContentDetector.Png, "anh.png")]
    [InlineData("ghichu", FileContentDetector.PlainText, "ghichu.txt")]
    [InlineData("a.b.cmd", FileContentDetector.PlainText, "a.b.txt")]
    [InlineData("Quyết định.bat", FileContentDetector.PlainText, "Quyết định.txt")]
    [InlineData("tep", "application/x-khong-biet", "tep")]
    [InlineData("tep.exe", "application/x-khong-biet", "tep")]
    public void ForDownload_ReplacesTheClientExtension_WithTheOneOfTheDetectedType(string name, string contentType, string expected)
        => FileNameSanitizer.ForDownload(name, contentType).ShouldBe(expected);

    [Fact]
    public void ForDownload_StaysWithinTheColumnWidth_EvenAfterAppendingTheExtension()
    {
        var name = FileNameSanitizer.ForDownload(new string('a', FileNameSanitizer.MaxLength), FileContentDetector.Docx);

        name.Length.ShouldBe(FileNameSanitizer.MaxLength);
        name.ShouldEndWith(".docx");
    }
}

public class FilePurposeCatalogTests
{
    private sealed class Source(params FilePurposeDefinition[] purposes) : IFilePurposeSource
    {
        public IReadOnlyCollection<FilePurposeDefinition> GetPurposes() => purposes;
    }

    private static FilePurposeDefinition Purpose(string key, params string[] types)
        => new(key, types.Length == 0 ? [FileContentDetector.Pdf] : types);

    [Fact]
    public void Merges_EverySource_AndFindsByKey()
    {
        var catalog = new FilePurposeCatalog([new Source(Purpose("ho-so")), new Source(Purpose("hoa-don"))]);

        catalog.All.Count.ShouldBe(2);
        catalog.Find("ho-so")!.Key.ShouldBe("ho-so");
        catalog.Find("khong-co").ShouldBeNull();
    }

    [Fact]
    public void NoSources_IsAValidEmptyCatalog()
        => new FilePurposeCatalog([]).All.ShouldBeEmpty();

    [Fact]
    public void DuplicateKeyAcrossSources_FailsAtBuildTime()
        => Should.Throw<InvalidOperationException>(() => new FilePurposeCatalog(
            [new Source(Purpose("ho-so")), new Source(Purpose("ho-so"))]))
            .Message.ShouldContain("ho-so");

    [Theory]
    [InlineData("Ho-So")]
    [InlineData("ho_so")]
    [InlineData("_tmp")]
    [InlineData("1ho-so")]
    [InlineData("ho so")]
    [InlineData("../x")]
    [InlineData("")]
    public void MalformedKey_FailsAtBuildTime_BecauseTheKeyBecomesADirectoryName(string key)
        => Should.Throw<InvalidOperationException>(() => new FilePurposeCatalog([new Source(Purpose(key))]));

    // ADR-0050 (docs/adr/0050-v1-chua-quet-ma-doc-siet-zip-va-duoi-tai-xuong.md): chưa quét mã độc thì không
    // purpose nào được nhận tệp nén — đường chở mã thực thi rẻ nhất.
    [Theory]
    [InlineData(FileContentDetector.Zip)]
    [InlineData("APPLICATION/ZIP")]
    public void Catalog_Rejects_Purpose_AllowingZip(string zip)
    {
        var message = Should.Throw<InvalidOperationException>(() => new FilePurposeCatalog(
            [new Source(Purpose("ho-so")), new Source(Purpose("dinh-kem", FileContentDetector.Pdf, zip))])).Message;

        message.ShouldContain("dinh-kem");
        message.ShouldContain(FileContentDetector.Zip);
        message.ShouldContain("quét mã độc");
    }

    [Fact]
    public void OfficeFormats_AreNotZip_AndStayAllowed()
        => new FilePurposeCatalog([new Source(Purpose("van-ban", FileContentDetector.Docx, FileContentDetector.Xlsx, FileContentDetector.Pptx))])
            .All.Count.ShouldBe(1);

    [Fact]
    public void EmptyAllowedTypes_FailsAtBuildTime_BecauseNoFileCouldEverBeUploaded()
        => Should.Throw<InvalidOperationException>(() => new FilePurposeCatalog(
            [new Source(new FilePurposeDefinition("ho-so", []))]))
            .Message.ShouldContain("ho-so");
}
