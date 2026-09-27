using System.IO.Compression;
using System.Text;
using CoreAndSkill.Core.Application.Files;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Files;

// docs/wiki-core/be/09-security-beyond-auth.md §9: kiểm kiểu tệp bằng NỘI DUNG, không tin phần mở rộng.
public class FileContentDetectorTests
{
    private static MemoryStream Bytes(params byte[] bytes) => new(bytes);

    private static MemoryStream Text(string text, Encoding? encoding = null)
        => new((encoding ?? new UTF8Encoding(false)).GetBytes(text));

    private static MemoryStream Zip(params string[] entryNames)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in entryNames)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write("x");
            }
        }

        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Detect_Pdf_ByMagicBytes()
        => FileContentDetector.Detect(Text("%PDF-1.7 ..."), "a.bin").ShouldBe(FileContentDetector.Pdf);

    [Fact]
    public void Detect_Png_ByMagicBytes()
        => FileContentDetector.Detect(Bytes(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0), null).ShouldBe(FileContentDetector.Png);

    [Fact]
    public void Detect_Jpeg_ByMagicBytes()
        => FileContentDetector.Detect(Bytes(0xFF, 0xD8, 0xFF, 0xE0, 0), null).ShouldBe(FileContentDetector.Jpeg);

    [Theory]
    [InlineData("GIF87a....")]
    [InlineData("GIF89a....")]
    public void Detect_Gif_ByMagicBytes(string header)
        => FileContentDetector.Detect(Text(header), null).ShouldBe(FileContentDetector.Gif);

    [Fact]
    public void Detect_AnExecutableRenamedToPdf_IsNotAPdf()
    {
        // "Một file thực thi đổi đuôi vẫn là file thực thi": MZ + byte không phải UTF-8 hợp lệ.
        var exe = Bytes(0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0xFF, 0xFE, 0x00);

        FileContentDetector.Detect(exe, "hop-dong.pdf").ShouldBeNull();
    }

    [Fact]
    public void Detect_AWebPageRenamedToPng_IsPlainText_NotAnImage()
        => FileContentDetector.Detect(Text("<html><script>alert(1)</script></html>"), "anh.png").ShouldBe(FileContentDetector.PlainText);

    [Fact]
    public void Detect_PlainZip()
        => FileContentDetector.Detect(Zip("a.txt", "b.txt"), null).ShouldBe(FileContentDetector.Zip);

    [Fact]
    public void Detect_Xlsx_FromEntryNames_WithoutDecompressingAnything()
        => FileContentDetector.Detect(Zip("[Content_Types].xml", "xl/workbook.xml"), null).ShouldBe(FileContentDetector.Xlsx);

    [Fact]
    public void Detect_Docx_FromEntryNames()
        => FileContentDetector.Detect(Zip("[Content_Types].xml", "word/document.xml"), null).ShouldBe(FileContentDetector.Docx);

    [Fact]
    public void Detect_Pptx_FromEntryNames()
        => FileContentDetector.Detect(Zip("[Content_Types].xml", "ppt/presentation.xml"), null).ShouldBe(FileContentDetector.Pptx);

    [Fact]
    public void Detect_AnArchiveMixingOfficeFolders_IsJustAZip()
        => FileContentDetector.Detect(Zip("[Content_Types].xml", "word/a.xml", "xl/b.xml"), null).ShouldBe(FileContentDetector.Zip);

    [Fact]
    public void Detect_ACorruptZipWithAZipSignature_IsUnknown()
        => FileContentDetector.Detect(Bytes(0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4, 5, 6, 7, 8), null).ShouldBeNull();

    [Fact]
    public void Detect_PlainText_UsesTheExtensionOnlyToTellCsvFromText()
    {
        FileContentDetector.Detect(Text("a,b\n1,2\n"), "du-lieu.CSV").ShouldBe(FileContentDetector.Csv);
        FileContentDetector.Detect(Text("a,b\n1,2\n"), "du-lieu.txt").ShouldBe(FileContentDetector.PlainText);
        FileContentDetector.Detect(Text("a,b\n1,2\n"), null).ShouldBe(FileContentDetector.PlainText);
    }

    [Fact]
    public void Detect_Utf8WithBom_AndVietnameseText_IsText()
    {
        var withBom = new MemoryStream([.. new UTF8Encoding(true).GetPreamble(), .. Encoding.UTF8.GetBytes("Họ và tên,Địa chỉ\n")]);

        FileContentDetector.Detect(withBom, "x.csv").ShouldBe(FileContentDetector.Csv);
    }

    [Fact]
    public void Detect_Utf16WithBom_IsText()
    {
        // Excel "Unicode text" xuất UTF-16 LE kèm BOM; Encoding.GetBytes không tự thêm BOM nên ghép tay.
        var encoding = new UnicodeEncoding(false, true);
        var utf16 = new MemoryStream([.. encoding.GetPreamble(), .. encoding.GetBytes("a;b\r\n1;2\r\n")]);

        FileContentDetector.Detect(utf16, "x.csv").ShouldBe(FileContentDetector.Csv);
    }

    [Fact]
    public void Detect_Utf16WithoutBom_IsNotText_BecauseItIsFullOfNulBytes()
        => FileContentDetector.Detect(Text("a;b\r\n1;2\r\n", new UnicodeEncoding(false, false)), "x.csv").ShouldBeNull();

    [Fact]
    public void Detect_AMultiByteCharacterCutAtTheEndOfTheSample_IsStillText()
    {
        // Mẫu 4096 byte có thể cắt giữa một ký tự nhiều byte — không được coi là nhị phân.
        var text = new string('a', 4095) + "ệ";

        FileContentDetector.Detect(Text(text), null).ShouldBe(FileContentDetector.PlainText);
    }

    [Fact]
    public void Detect_ControlCharacters_MeanBinary()
        => FileContentDetector.Detect(Bytes(0x41, 0x42, 0x00, 0x43), null).ShouldBeNull();

    [Fact]
    public void Detect_Empty_IsUnknown()
        => FileContentDetector.Detect(Bytes(), null).ShouldBeNull();

    [Fact]
    public void Detect_ResetsThePositionToZero()
    {
        var stream = Text("hello");
        stream.Position = 3;

        FileContentDetector.Detect(stream, null);

        stream.Position.ShouldBe(0);
    }

    [Fact]
    public void Detect_ANonSeekableStream_IsAProgrammingError()
    {
        using var stream = new NonSeekableStream();

        Should.Throw<ArgumentException>(() => FileContentDetector.Detect(stream, null));
    }

    [Theory]
    [InlineData(FileContentDetector.Pdf, ".pdf")]
    [InlineData(FileContentDetector.Png, ".png")]
    [InlineData(FileContentDetector.Jpeg, ".jpg")]
    [InlineData(FileContentDetector.Gif, ".gif")]
    [InlineData(FileContentDetector.Zip, ".zip")]
    [InlineData(FileContentDetector.Docx, ".docx")]
    [InlineData(FileContentDetector.Xlsx, ".xlsx")]
    [InlineData(FileContentDetector.Pptx, ".pptx")]
    [InlineData(FileContentDetector.Csv, ".csv")]
    [InlineData(FileContentDetector.PlainText, ".txt")]
    [InlineData("application/x-whatever", "")]
    public void ExtensionFor_IsDerivedFromTheDetectedType_NeverFromTheClientName(string contentType, string expected)
        => FileContentDetector.ExtensionFor(contentType).ShouldBe(expected);

    private sealed class NonSeekableStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => 0;

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

