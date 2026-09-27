using CoreAndSkill.Core.Application.Permissions;
using RuntimeAssembly = System.Reflection.Assembly;

namespace CoreAndSkill.ArchTests.Support;

// Đọc danh mục khoá mà Core khai bằng C# — phần dùng chung của mọi nửa luật B7 (docs/RULES.md §6).
//
// Ở ĐÂY chứ không nằm trong một file test: hai cổng B7 khác nhau cùng cần tập khoá này, và một bộ
// đọc thứ hai sẽ lệch khỏi bộ đọc thứ nhất mà không ai thấy — cùng lý do PermissionScriptParityTests
// đọc phía migration qua đúng PermissionSeedScanner mà PermissionSeedParityTests dùng.
internal static class CorePermissionCatalog
{
    public static bool IsCatalogSourceImplementation(Type type)
        => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
        && typeof(IPermissionCatalogSource).IsAssignableFrom(type);

    public static IReadOnlyList<IPermissionCatalogSource> Sources(RuntimeAssembly assembly)
        => assembly.GetTypes()
            .Where(IsCatalogSourceImplementation)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(Instantiate)
            .ToList();

    public static IReadOnlyCollection<string> Permissions(RuntimeAssembly assembly)
        => Sources(assembly)
            .SelectMany(source => source.GetPermissions())
            .Select(permission => $"{permission.Code}|{permission.ResourceKey}")
            .ToList();

    public static IReadOnlyCollection<string> Resources(RuntimeAssembly assembly)
        => Sources(assembly)
            .SelectMany(source => source.GetResources())
            .Select(resource => resource.Key)
            .ToList();

    // Không lọc bỏ kiểu "khó dựng" — lọc im lặng là cách một nguồn khoá mới biến mất khỏi tầm quét
    // mà cổng vẫn xanh. Dựng không được thì đỏ, và nói rõ kiểu nào.
    private static IPermissionCatalogSource Instantiate(Type type)
    {
        try
        {
            return (IPermissionCatalogSource)Activator.CreateInstance(type, nonPublic: true)!;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Không dựng được '{type.FullName}' để đối chiếu B7. Kiểu này hiện thực "
              + $"{nameof(IPermissionCatalogSource)} nhưng không có constructor không tham số — cổng "
              + "B7 cần được sửa cho đọc được nó, KHÔNG được lặng lẽ bỏ qua.", exception);
        }
    }
}
