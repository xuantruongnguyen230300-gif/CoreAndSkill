using CoreAndSkill.Core.Application.Tabular;

namespace CoreAndSkill.Core.Infrastructure.Tabular;

// Chọn bộ đọc theo NỘI DUNG, không theo đuôi tệp: chữ ký ZIP (`PK\x03\x04`) là bảng tính, còn lại là CSV.
// Đuôi tệp do người dùng đặt — đọc một tệp .xlsx đổi tên thành .csv (hoặc ngược lại) vẫn ra đúng bộ đọc.
internal sealed class TabularReader : ITabularReader
{
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    public async Task<ITabularSource> OpenAsync(Stream content, CancellationToken ct)
    {
        if (!content.CanSeek)
            throw new ArgumentException("Bộ đọc bảng cần stream seekable.", nameof(content));

        var start = content.Position;
        var signature = new byte[ZipSignature.Length];
        var read = await content.ReadAtLeastAsync(signature, signature.Length, throwOnEndOfStream: false, ct);
        content.Position = start;

        try
        {
            return read == signature.Length && signature.AsSpan().SequenceEqual(ZipSignature)
                ? await XlsxTabularSource.OpenAsync(content, ct)
                : await CsvTabularSource.OpenAsync(content, ct);
        }
        catch (TabularFormatException)
        {
            content.Position = start;
            throw;
        }
    }
}
