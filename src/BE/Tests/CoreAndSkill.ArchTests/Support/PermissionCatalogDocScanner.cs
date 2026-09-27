namespace CoreAndSkill.ArchTests.Support;

// Bóc bảng danh mục khoá phân quyền của Core ra khỏi CHỦ SỞ HỮU của nó —
// docs/database/schema-core.md §5.2, dòng đăng ký ở docs/OWNERSHIP.md.
//
// VÌ SAO PHẢI ĐỌC TÀI LIỆU. Luật B7 (docs/RULES.md §6) đòi ba bản cài đặt khớp *danh mục* ở §5.2,
// nhưng cả ba nửa hiện có đều chỉ đối chiếu ba bản cài đặt VỚI NHAU: C# ↔ migration ↔ script. File
// được khai là nguồn duy nhất thì không nửa nào đọc. Hệ quả có hình dạng rất khó thấy: người thêm
// một hàng vào §5.2 coi như đã khai xong khoá — đó là việc sổ chủ quyền bảo họ làm — mà ba nửa kia
// vẫn khớp nhau ở tập khoá cũ nên mọi cổng xanh; người sau đọc §5.2 rồi gắn
// [RequirePermission("<khoá mới>")] và nhận 403 im lặng cho mọi người, kể cả tài khoản bypass.
// Chiều ngược lại — thêm khoá vào C# mà không ghi vào §5.2 — cũng không ai canh, và nó làm chính
// nguồn duy nhất thành nguồn thiếu.
//
// check-docs.sh §15 KHÔNG thay được cổng này: nó đếm xem chuỗi mốc xuất hiện ở mấy file, không so
// nội dung bảng với bất cứ thứ gì.
//
// RANH GIỚI: §5.2 không có bảng TÀI NGUYÊN riêng; tập tài nguyên ở đây suy ra từ cột `resource_key`.
// Một tài nguyên hợp lệ mà chưa có khoá nào vì vậy nằm ngoài tầm — hôm nay không có ca nào như thế,
// và nếu có thì nó đỏ ở chiều "C# có khai, tài liệu KHÔNG có", tức đỏ ồn ào chứ không im lặng.
internal static class PermissionCatalogDocScanner
{
    // Mốc của sổ chủ quyền. Đổi mốc này mà không sửa ở đây thì cổng không tìm thấy bảng và ĐỎ —
    // đúng hướng: một cổng mất đầu vào phải kêu, không được xanh vì rỗng.
    public const string Marker = "Danh mục khoá phân quyền Core — định nghĩa gốc";

    public sealed record Row(string Code, string ResourceKey, string Action);

    // Khuôn `code` = `<resource_key>.<action>` — câu luật viết ngay trên bảng §5.2, và là thứ duy
    // nhất giữ ba cột của bảng đó khỏi trôi khỏi nhau.
    //
    // Ở ĐÂY chứ không nội tuyến trong [Fact]: một vị từ chỉ sống trong thân test thì `Detector_`
    // của nó không có gì để gọi, nên nó buộc phải tự khẳng định lại luật trên dữ liệu fixture — và
    // một khẳng định lặp lại chính nó là tautology, xanh vĩnh viễn kể cả khi vị từ thật đã hỏng
    // (luật T1). Tách ra thành hàm công khai để CẢ luật thật LẪN Detector_ cùng gọi đúng một đoạn
    // mã: làm hỏng nó thì Detector_ đỏ ngay, không cần chờ dữ liệu thật đi sai.
    public static bool FollowsCodeShape(Row row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return string.Equals(
            row.Code, $"{row.ResourceKey}.{row.Action}", StringComparison.Ordinal);
    }

    public static IReadOnlyList<Row> ReadCatalog(string markdownPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markdownPath);

        if (!File.Exists(markdownPath))
            throw new InvalidOperationException(
                $"Không thấy tài liệu chủ '{markdownPath}' — cổng B7 đang không đối chiếu được với "
              + "danh mục gốc. Sửa đường dẫn, đừng để test xanh vì không tìm thấy gì.");

        return ParseCatalog(File.ReadAllText(markdownPath), markdownPath);
    }

    public static IReadOnlyList<Row> ParseCatalog(string markdown, string origin)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        var markerIndex = Array.FindIndex(lines, line => line.Contains(Marker, StringComparison.Ordinal));

        if (markerIndex < 0)
            throw new InvalidOperationException(
                $"'{origin}' không còn mốc '{Marker}' — danh mục gốc đã đổi tên hoặc dời chỗ, và cổng "
              + "B7 đang so với một tập rỗng. Sửa mốc ở cả hai phía, KHÔNG bỏ qua.");

        var tableStart = Array.FindIndex(lines, markerIndex + 1, line => line.TrimStart().StartsWith('|'));

        if (tableStart < 0)
            throw new InvalidOperationException(
                $"'{origin}' có mốc '{Marker}' nhưng không có bảng nào sau nó.");

        var header = SplitRow(lines[tableStart]);

        int Column(string name)
        {
            var index = header.FindIndex(cell => string.Equals(cell, name, StringComparison.Ordinal));

            return index >= 0
                ? index
                : throw new InvalidOperationException(
                    $"Bảng danh mục ở '{origin}' không có cột '{name}' — cột đã bị đổi tên, và cổng B7 "
                  + $"đang đọc một thứ không còn tồn tại. Cột đang có: {string.Join(", ", header)}.");
        }

        var codeColumn = Column("code");
        var resourceColumn = Column("resource_key");
        var actionColumn = Column("action");

        var rows = new List<Row>();

        // Bỏ dòng tiêu đề và dòng gạch ngang, đọc tới dòng đầu tiên không còn là dòng bảng.
        for (var i = tableStart + 2; i < lines.Length && lines[i].TrimStart().StartsWith('|'); i++)
        {
            var cells = SplitRow(lines[i]);

            if (cells.Count <= Math.Max(codeColumn, Math.Max(resourceColumn, actionColumn)))
                throw new InvalidOperationException(
                    $"'{origin}' dòng {i + 1}: bảng danh mục có {cells.Count} ô, ít hơn số cột đã khai "
                  + "— không đối chiếu được. Sửa tài liệu, đừng nới bộ đọc.");

            rows.Add(new Row(cells[codeColumn], cells[resourceColumn], cells[actionColumn]));
        }

        if (rows.Count == 0)
            throw new InvalidOperationException(
                $"'{origin}' có bảng danh mục nhưng KHÔNG đọc được dòng nào — cổng B7 đang so một tập "
              + "rỗng với một tập rỗng.");

        return rows;
    }

    // Tách một dòng bảng markdown thành từng ô, đã gỡ backtick và khoảng trắng. Ô rỗng giữ nguyên
    // chỗ: bỏ ô rỗng đi sẽ làm mọi cột sau nó lệch chỉ số.
    private static List<string> SplitRow(string line)
    {
        var trimmed = line.Trim();

        if (trimmed.StartsWith('|'))
            trimmed = trimmed[1..];

        if (trimmed.EndsWith('|'))
            trimmed = trimmed[..^1];

        return [.. trimmed.Split('|').Select(cell => cell.Trim().Trim('`').Trim())];
    }
}
