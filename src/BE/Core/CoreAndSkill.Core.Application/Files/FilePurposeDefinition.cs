using System.Text.RegularExpressions;

namespace CoreAndSkill.Core.Application.Files;

// Một `purpose` của tệp — docs/contracts/files.md §1: do MODULE khai; quyết định thư mục đích và giới
// hạn áp dụng. Core không khai purpose nào cho người dùng tải lên: nội dung nghiệp vụ thuộc dự án.
//
// AllowedContentTypes là danh sách CHO PHÉP (không danh sách chặn — 09-security-beyond-auth.md §9),
// so với kiểu do HỆ THỐNG xác định từ nội dung (FileContentDetector), không với kiểu client gửi.
// MaxBytes <= 0 nghĩa là dùng trần chung Core:File:MaxUploadMb; giá trị dương chỉ được SIẾT, không nới.
public sealed record FilePurposeDefinition(
    string Key,
    IReadOnlyCollection<string> AllowedContentTypes,
    long MaxBytes = 0);

public interface IFilePurposeSource
{
    IReadOnlyCollection<FilePurposeDefinition> GetPurposes();
}

// Gộp mọi nguồn. Khoá trùng, sai khuôn, danh sách kiểu rỗng hoặc nhận tệp nén ⇒ ném NGAY lúc dựng — và catalog được
// dựng lúc khởi động (FilePurposeCatalogValidationHostedService), nên tiến trình không lên.
public sealed partial class FilePurposeCatalog
{
    // Khoá trở thành tên thư mục và cột varchar(50): chữ thường, số, gạch ngang. KHÔNG có gạch dưới
    // để không bao giờ trùng thư mục tạm `_tmp`.
    [GeneratedRegex("^[a-z][a-z0-9-]{0,49}$")]
    private static partial Regex KeyPattern();

    private readonly Dictionary<string, FilePurposeDefinition> _byKey = new(StringComparer.Ordinal);

    public FilePurposeCatalog(IEnumerable<IFilePurposeSource> sources)
    {
        foreach (var definition in sources.SelectMany(s => s.GetPurposes()))
        {
            if (!KeyPattern().IsMatch(definition.Key))
                throw new InvalidOperationException(
                    $"Khoá purpose '{definition.Key}' sai khuôn — chỉ chữ thường, số và gạch ngang, tối đa 50 ký tự.");

            if (definition.AllowedContentTypes.Count == 0)
                throw new InvalidOperationException(
                    $"Purpose '{definition.Key}' không khai kiểu tệp nào được phép — danh sách rỗng nghĩa là không tệp nào tải lên được.");

            // docs/adr/0050-v1-chua-quet-ma-doc-siet-zip-va-duoi-tai-xuong.md: v1 chưa quét mã độc, nên tệp nén —
            // đường chở mã thực thi rẻ nhất, qua mọi phép nhận diện kiểu — bị cấm ở mọi purpose. Office (Docx/Xlsx/
            // Pptx) là kiểu riêng, không phải Zip, nên không bị chặn ở đây.
            if (definition.AllowedContentTypes.Any(t => string.Equals(t, FileContentDetector.Zip, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    $"Purpose '{definition.Key}' khai nhận '{FileContentDetector.Zip}' — tệp nén bị cấm ở mọi purpose cho tới khi " +
                    "hệ thống có quét mã độc (ADR-0050).");

            if (!_byKey.TryAdd(definition.Key, definition))
                throw new InvalidOperationException($"Khoá purpose '{definition.Key}' bị khai trùng giữa các nguồn.");
        }
    }

    public IReadOnlyCollection<FilePurposeDefinition> All => _byKey.Values;

    public FilePurposeDefinition? Find(string key)
        => _byKey.TryGetValue(key, out var definition) ? definition : null;
}
