using System.Collections.ObjectModel;

namespace CoreAndSkill.Core.Application.Common;

// Lọc placeholder của FluentValidation trước khi ra dây — docs/quy-uoc/be-cqrs-handler.md §5.2.
// FormattedMessagePlaceholderValues LUÔN chứa "PropertyValue" (giá trị người dùng vừa gõ); chỉ
// khoá trong allowlist mới được đi tiếp. Thêm rule dùng placeholder mới thì thêm khoá vào đây —
// ba khoá hiện có là ba tham số của nhóm CORE.VALIDATION.* ở be-cqrs-handler.md §7.1.
internal static class MessageParamPolicy
{
    private static readonly HashSet<string> Allowlist = new(StringComparer.Ordinal)
    {
        "MaxLength",
        "MinLength",
        "MaxItems",
    };

    public static IReadOnlyDictionary<string, string> Filter(IDictionary<string, object>? placeholderValues)
    {
        // FluentValidation gán dictionary này khi build message thật; một rule tự viết tay không đi
        // qua đường build message có thể để lại null — coi như rỗng, không ném.
        if (placeholderValues is null || placeholderValues.Count == 0)
            return ReadOnlyDictionary<string, string>.Empty;

        Dictionary<string, string>? filtered = null;

        foreach (var (key, value) in placeholderValues)
        {
            if (!Allowlist.Contains(key))
                continue;

            filtered ??= new Dictionary<string, string>(StringComparer.Ordinal);
            filtered[key] = value?.ToString() ?? string.Empty;
        }

        return filtered is null
            ? ReadOnlyDictionary<string, string>.Empty
            : filtered;
    }
}
