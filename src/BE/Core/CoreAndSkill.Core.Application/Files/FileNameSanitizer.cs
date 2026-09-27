namespace CoreAndSkill.Core.Application.Files;

// Tên tệp người dùng đặt — docs/wiki-core/be/14-file-storage.md §2: chỉ làm SIÊU DỮ LIỆU hiển thị và
// đặt tên lúc tải về, không bao giờ dựng đường dẫn. Tên client gửi có thể mang đường dẫn tương đối để
// thoát ra thư mục khác, ký tự điều khiển, hoặc dài bất thường.
internal static class FileNameSanitizer
{
    public const int MaxLength = 255;
    public const string Fallback = "tep";

    public static string Sanitize(string? clientName)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            return Fallback;

        // Chỉ giữ phần sau dấu phân tách CUỐI CÙNG, theo cả hai kiểu — client có thể chạy hệ điều hành
        // khác máy chủ, nên Path.GetFileName của nền tảng hiện tại là chưa đủ.
        var name = clientName[(clientName.LastIndexOfAny(['/', '\\']) + 1)..];

        var chars = name.Where(c => !char.IsControl(c)).ToArray();
        name = new string(chars).Trim().Trim('.');

        if (name.Length == 0)
            return Fallback;

        return name.Length <= MaxLength ? name : name[..MaxLength];
    }

    // Tên lúc TẢI VỀ: phần tên gốc bỏ đuôi + đuôi của KIỂU ĐÃ XÁC ĐỊNH từ nội dung (ADR-0050).
    // Đuôi client đặt không bao giờ tới máy người tải về: văn bản thuần tên "x.bat" là văn bản thật, nhưng về máy
    // dưới tên .bat thì một cú nhấp đúp là chạy. Kiểu không có đuôi đã biết thì bỏ đuôi, không giữ đuôi cũ.
    public static string ForDownload(string storedName, string contentType)
    {
        var dot = storedName.LastIndexOf('.');
        var stem = dot > 0 ? storedName[..dot] : storedName;
        var extension = FileContentDetector.ExtensionFor(contentType);

        if (stem.Length + extension.Length > MaxLength)
            stem = stem[..(MaxLength - extension.Length)];

        return stem + extension;
    }
}
