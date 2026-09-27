using System.Text.RegularExpressions;

namespace CoreAndSkill.Core.Domain.Common;

public static partial class MessageTemplateRenderer
{
    [GeneratedRegex(@"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex Placeholder();

    public static string Render(string template, IReadOnlyDictionary<string, string>? args)
    {
        if (args is null || args.Count == 0)
            return template;

        return Placeholder().Replace(template, match =>
            args.TryGetValue(match.Groups["name"].Value, out var value)
                ? value
                : match.Value);   // chỗ giữ không có tham số: GIỮ NGUYÊN, không xoá
    }
}
