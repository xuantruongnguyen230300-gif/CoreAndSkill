using System.Reflection;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CoreAndSkill.ArchTests.Support;

// docs/wiki-core/be/14-file-storage.md §7: "mọi action nhận IFormFile phải khai giới hạn dung lượng tường minh, không
// để framework tự quyết bằng mặc định ẩn". Thiếu nó thì action vẫn build và vẫn chạy — cho tới ngày có người gửi một
// tệp rất lớn: nội dung đã bị parser buffer hết trước khi bất kỳ validator nào kiểm được gì.
//
// Một action "nhận tệp" khi có tham số thuộc một trong các dạng: IFormFile, IFormFileCollection, IFormCollection,
// một tập hợp của IFormFile, hoặc một KIỂU MÔ HÌNH có thuộc tính như vậy (dò tới hai tầng lồng nhau).
//
// ĐIỂM MÙ (đã biết, không giả vờ bắt được):
//   - Action đọc tệp qua `Request.Form` / `Request.Form.Files` mà KHÔNG khai tham số nào — phép dò này chỉ thấy
//     chữ ký. Quy ước: không viết action như vậy; nếu cần, dùng tham số IFormFile để cổng này thấy nó.
//   - Mô hình lồng sâu hơn hai tầng, hoặc tệp nằm trong kiểu generic tuỳ biến.
//   - Endpoint không phải MVC controller (minimal API) — solution hiện không có; xuất hiện thì cổng này không thấy.
//   - Cổng chỉ kiểm SỰ CÓ MẶT của attribute, không kiểm giá trị giới hạn thực tế của nó: giá trị nằm ở
//     Core:File:MaxUploadMb và đã được B4EndpointTests.Upload_* kiểm qua đường HTTP.
internal static class UploadSizeLimitScanner
{
    private const int MaxModelDepth = 2;

    public static IReadOnlyList<string> FindFileActionsWithoutSizeLimit(Assembly assembly)
    {
        var offenders = new List<string>();

        foreach (var action in EnumerateActions(assembly))
        {
            if (!AcceptsFiles(action) || action.GetCustomAttribute<UploadSizeLimitAttribute>() is not null)
                continue;

            offenders.Add($"{action.DeclaringType!.FullName}.{action.Name}");
        }

        return offenders;
    }

    // Số action mà bộ dò coi là "nhận tệp" — để test T6 chứng minh bộ dò không quét rỗng.
    public static IReadOnlyList<string> FindFileActions(Assembly assembly)
        => [.. EnumerateActions(assembly).Where(AcceptsFiles).Select(a => $"{a.DeclaringType!.FullName}.{a.Name}")];

    private static IEnumerable<MethodInfo> EnumerateActions(Assembly assembly)
    {
        const BindingFlags actionFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(ControllerBase).IsAssignableFrom(type))
                continue;

            foreach (var method in type.GetMethods(actionFlags))
            {
                if (method.IsSpecialName || method.GetCustomAttribute<NonActionAttribute>() is not null)
                    continue;

                if (method.GetCustomAttributes().OfType<IActionHttpMethodProvider>().Any())
                    yield return method;
            }
        }
    }

    private static bool AcceptsFiles(MethodInfo action)
        => action.GetParameters().Any(p => IsFileBearing(p.ParameterType, MaxModelDepth, []));

    private static bool IsFileBearing(Type type, int depth, HashSet<Type> visiting)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(IFormFile) || type == typeof(IFormFileCollection) || type == typeof(IFormCollection))
            return true;

        // IEnumerable<IFormFile>, List<IFormFile>, IFormFile[] ...
        if (type.IsArray && type.GetElementType() is { } element)
            return IsFileBearing(element, depth, visiting);

        if (type.IsGenericType && type.GetGenericArguments().Any(a => a == typeof(IFormFile)))
            return true;

        if (depth <= 0 || type.IsPrimitive || type.IsEnum || type == typeof(string) || type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
            return false;

        if (!visiting.Add(type))
            return false;

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => IsFileBearing(p.PropertyType, depth - 1, visiting));
    }
}
