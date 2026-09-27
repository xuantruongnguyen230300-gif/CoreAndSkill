using System.IO.Compression;
using System.Text;

namespace CoreAndSkill.Core.Application.Files;

// Xác định kiểu tệp bằng NỘI DUNG, không tin phần mở rộng — docs/wiki-core/be/09-security-beyond-auth.md
// §9: "một file thực thi đổi đuôi vẫn là file thực thi". Trả null khi không nhận ra: null KHÔNG BAO GIỜ
// được cho qua danh sách cho phép.
//
// Phạm vi có chủ đích hẹp: PDF, ba loại ảnh, họ ZIP (kèm ba định dạng Office suy từ TÊN MỤC trong
// thư mục trung tâm của tệp nén — không giải nén gì), và văn bản thuần. Muốn kiểu khác thì thêm ở
// ĐÂY kèm test — không có đường "tin đuôi tệp" nào để thêm kiểu.
//
// Phần mở rộng chỉ được dùng ĐỂ PHÂN LOẠI TRONG họ văn bản (text/csv so với text/plain): nội dung đã
// được kiểm là văn bản thuần, và tệp luôn được phục vụ dạng tải xuống với kiểu do hệ thống đặt.
public static class FileContentDetector
{
    public const string Pdf = "application/pdf";
    public const string Png = "image/png";
    public const string Jpeg = "image/jpeg";
    public const string Gif = "image/gif";
    public const string Zip = "application/zip";
    public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string Pptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    public const string Csv = "text/csv";
    public const string PlainText = "text/plain";

    private const int SampleSize = 4096;

    // Mục tối đa được liệt kê khi soi tệp nén — chống tệp nén cố ý có hàng triệu mục.
    private const int MaxZipEntriesInspected = 5000;

    // Stream PHẢI seekable (IFormFile.OpenReadStream() là seekable). Vị trí được trả về 0.
    public static string? Detect(Stream stream, string? fileName)
    {
        if (!stream.CanSeek)
            throw new ArgumentException("Nhận diện kiểu tệp cần stream seekable.", nameof(stream));

        stream.Position = 0;
        var buffer = new byte[SampleSize];
        var read = ReadFully(stream, buffer);
        stream.Position = 0;

        var sample = buffer.AsSpan(0, read);
        if (sample.IsEmpty)
            return null;

        if (sample.StartsWith("%PDF-"u8))
            return Pdf;

        if (sample.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return Png;

        if (sample.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            return Jpeg;

        if (sample.StartsWith("GIF87a"u8) || sample.StartsWith("GIF89a"u8))
            return Gif;

        if (sample.StartsWith(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
        {
            var detected = DetectZipFamily(stream);
            stream.Position = 0;
            return detected;
        }

        return IsPlainText(sample) ? ClassifyText(fileName) : null;
    }

    private static string? DetectZipFamily(Stream stream)
    {
        try
        {
            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            var hasContentTypes = false;
            var isWord = false;
            var isExcel = false;
            var isPowerPoint = false;
            var inspected = 0;

            foreach (var entry in archive.Entries)
            {
                if (++inspected > MaxZipEntriesInspected)
                    break;

                var name = entry.FullName;
                if (name == "[Content_Types].xml")
                    hasContentTypes = true;
                else if (name.StartsWith("word/", StringComparison.Ordinal))
                    isWord = true;
                else if (name.StartsWith("xl/", StringComparison.Ordinal))
                    isExcel = true;
                else if (name.StartsWith("ppt/", StringComparison.Ordinal))
                    isPowerPoint = true;
            }

            if (hasContentTypes && isWord && !isExcel && !isPowerPoint)
                return Docx;
            if (hasContentTypes && isExcel && !isWord && !isPowerPoint)
                return Xlsx;
            if (hasContentTypes && isPowerPoint && !isWord && !isExcel)
                return Pptx;

            return Zip;
        }
        catch (InvalidDataException)
        {
            // Bắt đầu bằng chữ ký ZIP nhưng thư mục trung tâm hỏng: không phải tệp nén hợp lệ.
            return null;
        }
    }

    private static string ClassifyText(string? fileName)
        => fileName is not null && fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? Csv : PlainText;

    // Văn bản thuần = giải mã được nghiêm ngặt theo UTF-8 (hoặc UTF-16 có BOM) và không có ký tự điều
    // khiển ngoài tab / xuống dòng. Mẫu bị cắt giữa một ký tự nhiều byte vẫn hợp lệ — chỉ ký tự
    // THẬT SỰ sai mới loại.
    private static bool IsPlainText(ReadOnlySpan<byte> sample)
    {
        Encoding encoding;
        var bomLength = 0;

        if (sample.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
        {
            encoding = new UTF8Encoding(false, throwOnInvalidBytes: true);
            bomLength = 3;
        }
        else if (sample.StartsWith(new byte[] { 0xFF, 0xFE }))
        {
            encoding = new UnicodeEncoding(false, false, throwOnInvalidBytes: true);
            bomLength = 2;
        }
        else if (sample.StartsWith(new byte[] { 0xFE, 0xFF }))
        {
            encoding = new UnicodeEncoding(true, false, throwOnInvalidBytes: true);
            bomLength = 2;
        }
        else
        {
            encoding = new UTF8Encoding(false, throwOnInvalidBytes: true);
        }

        try
        {
            var decoder = encoding.GetDecoder();
            var chars = new char[sample.Length + 1];
            var bytes = sample[bomLength..].ToArray();
            // flush:false — cho phép ký tự nhiều byte bị cắt ở cuối mẫu.
            var count = decoder.GetChars(bytes, 0, bytes.Length, chars, 0, flush: false);

            for (var i = 0; i < count; i++)
            {
                var c = chars[i];
                if (char.IsControl(c) && c is not ('\t' or '\r' or '\n'))
                    return false;
            }

            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static int ReadFully(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var n = stream.Read(buffer, total, buffer.Length - total);
            if (n == 0)
                break;
            total += n;
        }

        return total;
    }

    // Phần mở rộng trên đĩa suy từ KIỂU ĐÃ XÁC ĐỊNH — không bao giờ từ tên client.
    public static string ExtensionFor(string contentType) => contentType switch
    {
        Pdf => ".pdf",
        Png => ".png",
        Jpeg => ".jpg",
        Gif => ".gif",
        Zip => ".zip",
        Docx => ".docx",
        Xlsx => ".xlsx",
        Pptx => ".pptx",
        Csv => ".csv",
        PlainText => ".txt",
        _ => string.Empty,
    };
}
