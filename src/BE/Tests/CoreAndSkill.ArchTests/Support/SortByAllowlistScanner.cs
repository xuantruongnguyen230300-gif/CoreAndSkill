using System.Reflection;
using FluentValidation;
using MediatR;

namespace CoreAndSkill.ArchTests.Support;

// Luật B3 — docs/RULES.md §10: `SortBy` chỉ nhận giá trị thuộc allowlist của từng endpoint. Allowlist là DỮ LIỆU nằm trong
// validator của từng request, không có hình dạng chung để quét; thiếu nó thì tốt nhất là lỗi lúc chạy (tên cột không tồn
// tại), tệ nhất là một chuỗi do client gửi đi thẳng vào câu sắp xếp.
//
// Phép dò: mọi record request của MediatR có property `SortBy` phải có MỘT validator khai rule trên CHÍNH property đó, và
// rule ấy phải thuộc loại "kiểm miền giá trị" — Must (PredicateValidator / AsyncPredicateValidator) hoặc IsInEnum
// (EnumValidator). Đọc mô tả rule lúc CHẠY (IValidatorDescriptor), không đọc chữ trong source: `RuleFor(x => x.SortBy)` có
// thể viết bằng nhiều cách, còn descriptor là thứ FluentValidation thật sự thi hành.
//
// NGOÀI TẦM (biết trước): cổng kiểm HÌNH DẠNG, không kiểm allowlist có đúng không — `Must(_ => true)` vẫn qua. Nội dung
// allowlist được canh bằng test hành vi của từng validator (vd. GetUsersListQueryValidatorTests).
internal static class SortByAllowlistScanner
{
    private const string SortByProperty = "SortBy";

    private static readonly string[] DomainRuleNames = ["PredicateValidator", "AsyncPredicateValidator", "EnumValidator"];

    // Mọi kiểu request mang property SortBy — để test T6 chứng minh tập đầu vào khác rỗng.
    public static IReadOnlyList<Type> RequestsWithSortBy(params Assembly[] assemblies)
        => [.. assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IBaseRequest).IsAssignableFrom(t)
                        && t.GetProperty(SortByProperty, BindingFlags.Public | BindingFlags.Instance) is not null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)];

    // offender = "<Request>: <lý do>".
    public static IReadOnlyList<string> FindRequestsWithoutSortByAllowlist(params Assembly[] assemblies)
    {
        var validators = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .Select(t => (Type: t, Validated: ValidatedType(t)))
            .Where(x => x.Validated is not null)
            .ToLookup(x => x.Validated!, x => x.Type);

        var offenders = new List<string>();

        foreach (var request in RequestsWithSortBy(assemblies))
        {
            var rules = validators[request]
                .Select(Instantiate)
                .SelectMany(v => v.CreateDescriptor().GetValidatorsForMember(SortByProperty))
                .Select(pair => pair.Validator.Name)
                .ToList();

            if (rules.Count == 0)
            {
                offenders.Add($"{request.FullName}: không validator nào khai rule trên {SortByProperty}");
                continue;
            }

            if (!rules.Any(DomainRuleNames.Contains))
                offenders.Add($"{request.FullName}: rule trên {SortByProperty} không kiểm miền giá trị ({string.Join(", ", rules)})");
        }

        return offenders;
    }

    private static Type? ValidatedType(Type candidate)
    {
        for (var current = candidate.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                return current.GetGenericArguments()[0];
        }

        return null;
    }

    // Validator của Core là `internal sealed` với constructor rỗng — nonPublic: true là đường duy nhất dựng nó từ đây.
    private static IValidator Instantiate(Type validatorType)
        => (IValidator)Activator.CreateInstance(validatorType, nonPublic: true)!;
}
