using System.Xml.Linq;

namespace CoreAndSkill.ArchTests.Support;

// S23 vế (e) (docs/RULES-BE.md hàng S23; docs/adr/0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md
// quyết định 3, mục 5): project host phải loại appsettings.Development.json khỏi gói publish. Lý do hiện hành (thiết
// kế DB dev chung của ADR-0098 đang HOÃN, hôm nay không khoá hay bản mã hoá nào tồn tại):
// docs/adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md quyết định 6, gạch vế (e) — tệp
// mang hai mật khẩu bootstrap công khai; máy chủ lỡ chạy ở môi trường Development sẽ nạp chúng.
//
// Đây là dữ liệu của .csproj, không phải metadata assembly đã build — ArchUnitNET không đọc được tệp dự án. Detector
// tự đọc .csproj bằng XML.
//
// CHỈ NHẬN item <Content Update="…">, KHÔNG nhận <None> và KHÔNG nhận <Content Include="…">. Đối chiếu thật bằng
// `dotnet publish` (SDK 10.0.400) trên `dotnet new web`: Web SDK tự đưa `**\*.json` vào item Content
// (CopyToPublishDirectory=PreserveNewest) và GỠ khỏi None — khai lại CopyToPublishDirectory trên một <None> không có
// tác dụng gì, tệp vẫn publish theo item Content ngầm. Khai `<Content Include="appsettings.Development.json">` đụng
// độ với item ngầm đó và làm build lỗi NETSDK1022 (duplicate items). Chỉ `<Content Update="…">` — sửa đúng item đã
// tồn tại — chạy được và có tác dụng thật.
//
// ĐIỂM MÙ: đây là bộ đọc XML TĨNH, không evaluate MSBuild. Không thấy: Directory.Build.props/targets, <Import>, giá
// trị `$(…)` hay `@(…)` bên trong Update/Include/Remove hoặc CopyToPublishDirectory (một spec chứa `$(…)` được đọc
// nguyên văn, không mở rộng — spec đó không bao giờ khớp tên tệp), tác động của <Target> chạy lúc publish (item thêm
// bằng task, Exclude=…, Update giữa chừng trong Target không Condition). Chỉ nhận diện item viết tường minh trong
// chính .csproj. Đọc được và xử lý theo chiều AN TOÀN (không đạt): Condition ở bất kỳ tổ tiên hay chính item, tổ tiên
// Choose/When/Otherwise, Condition trên phần tử con CopyToPublishDirectory, nhiều phần tử con cùng tên (lấy phần tử
// cuối), mọi item khác — mọi loại, trước hay sau — có Include/Update/Remove trùng tên tệp hay glob khớp được tên tệp.
// Glob so khớp KHÔNG phân biệt hoa thường (MSBuild trên Windows không phân biệt; chiều an toàn cho Linux).
internal static class PublishExclusionScanner
{
    private const string TargetFile = "appsettings.Development.json";
    private const string ContentItemType = "Content";
    private const string PublishMetadata = "CopyToPublishDirectory";
    private static readonly string[] SpecAttributes = ["Include", "Update", "Remove"];
    private static readonly string[] ConditionalContainers = ["Choose", "When", "Otherwise"];

    // true khi .csproj khai đúng loại trừ publish cho appsettings.Development.json: item Content với Update trỏ
    // ĐÚNG tên tệp — so khớp phân biệt hoa thường, cùng quy ước §12 docs/quy-uoc/repo-artifact.md.
    //
    // Luật bổ sung, chiều AN TOÀN (một Condition hay một khai báo khác có thể vô hiệu hoá loại trừ mà mắt thường đọc
    // lướt không thấy):
    //   - Condition trên chính item HOẶC trên BẤT KỲ tổ tiên, hoặc item nằm dưới Choose/When/Otherwise → không đạt.
    //     Publish có thể chạy với cấu hình khác cấu hình mà Condition đó đúng; không có cách nào từ một .csproj
    //     tĩnh biết Condition sẽ đúng hay sai lúc publish thật.
    //   - Nhiều khai báo Content cùng trỏ tệp này: MSBuild áp theo THỨ TỰ TÀI LIỆU, khai báo SAU thắng. Chỉ khai báo
    //     CUỐI quyết định kết quả.
    //   - Phần tử con CopyToPublishDirectory mang Condition → không đạt; nhiều phần tử con cùng tên → lấy phần tử
    //     cuối. Có cả thuộc tính lẫn phần tử con → cả hai phải là Never (không chắc thứ tự áp của MSBuild).
    //   - Mọi phần tử KHÁC có Include/Update/Remove trùng tên tệp hay glob khớp được tên tệp (None, Content Include,
    //     Content Remove, Content Update glob…) → không đạt: mỗi cái đều có thể đưa tệp trở lại gói publish.
    public static bool ExcludesAppsettingsDevelopmentFromPublish(string csprojContent)
    {
        var doc = XDocument.Parse(csprojContent);

        var matches = doc.Descendants()
            .Where(item => item.Name.LocalName == ContentItemType)
            .Where(TargetsAppsettingsDevelopment)
            .ToList();

        if (matches.Count == 0)
            return false;

        if (matches.Any(HasConditionOrConditionalAncestor))
            return false;

        if (doc.Descendants().Any(item => !matches.Contains(item) && SpecsMatchTargetFile(item)))
            return false;

        // XDocument.Descendants() trả về theo đúng thứ tự tài liệu — phần tử cuối là khai báo thắng.
        return HasCopyToPublishDirectoryNever(matches[^1]);
    }

    private static bool TargetsAppsettingsDevelopment(XElement item)
        => item.Attribute("Update")?.Value == TargetFile;

    private static bool HasConditionOrConditionalAncestor(XElement item)
        => item.AncestorsAndSelf().Any(element =>
            element.Attribute("Condition") is not null
            || ConditionalContainers.Contains(element.Name.LocalName));

    private static bool SpecsMatchTargetFile(XElement element)
        => SpecAttributes
            .Select(name => element.Attribute(name)?.Value)
            .Where(value => value is not null)
            .SelectMany(value => value!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(SpecMatchesTargetFile);

    private static bool SpecMatchesTargetFile(string spec)
    {
        var normalized = spec.Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
            normalized = normalized[2..];

        if (normalized.Equals(TargetFile, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!normalized.Contains('*') && !normalized.Contains('?'))
            return false;

        var pattern = new System.Text.StringBuilder("^");
        for (var i = 0; i < normalized.Length; i++)
        {
            if (normalized.AsSpan(i).StartsWith("**/"))
            {
                pattern.Append("(?:.*/)?");
                i += 2;
            }
            else if (normalized.AsSpan(i).StartsWith("**"))
            {
                pattern.Append(".*");
                i += 1;
            }
            else if (normalized[i] == '*')
                pattern.Append("[^/]*");
            else if (normalized[i] == '?')
                pattern.Append("[^/]");
            else
                pattern.Append(System.Text.RegularExpressions.Regex.Escape(normalized[i].ToString()));
        }

        pattern.Append('$');

        return System.Text.RegularExpressions.Regex.IsMatch(
            TargetFile,
            pattern.ToString(),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool HasCopyToPublishDirectoryNever(XElement item)
    {
        var elements = item.Elements().Where(child => child.Name.LocalName == PublishMetadata).ToList();

        if (elements.Any(child => child.Attribute("Condition") is not null))
            return false;

        var attribute = item.Attribute(PublishMetadata)?.Value;

        if (attribute is null && elements.Count == 0)
            return false;

        return (attribute is null || IsNever(attribute))
            && (elements.Count == 0 || IsNever(elements[^1].Value));
    }

    private static bool IsNever(string? value)
        => value is not null && value.Trim().Equals("Never", StringComparison.OrdinalIgnoreCase);
}
