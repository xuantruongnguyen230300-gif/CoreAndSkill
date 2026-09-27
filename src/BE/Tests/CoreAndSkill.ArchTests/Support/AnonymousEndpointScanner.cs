using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CoreAndSkill.ArchTests.Support;

// Tính chuỗi "METHOD /path" cho mọi action mang [AllowAnonymous] (trực tiếp trên action, hoặc kế
// thừa từ [AllowAnonymous] khai ở cấp controller) trong một assembly — dùng bởi luật S4
// (docs/RULES.md S4, docs/quy-uoc/be-api-controller.md §5). Đồ thị assembly (System.Reflection),
// không phải AST/quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.
//
// Route ở repo này viết TƯỜNG MINH, không dùng token [controller]/[action]
// (docs/quy-uoc/be-api-controller.md §3), nên phép ghép dưới đây chỉ nối chuỗi controller +
// action, không suy diễn token nào.
internal static class AnonymousEndpointScanner
{
    public static IReadOnlyList<string> FindAnonymousEndpoints(Assembly assembly)
    {
        var endpoints = new List<string>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(ControllerBase).IsAssignableFrom(type))
                continue;

            var controllerAnonymous = type.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
            var controllerTemplate = type.GetCustomAttribute<RouteAttribute>()?.Template;

            const BindingFlags actionFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (var method in type.GetMethods(actionFlags))
            {
                // IsSpecialName loại bỏ get_/set_ property accessor — không phải action.
                if (method.IsSpecialName || method.GetCustomAttribute<NonActionAttribute>() is not null)
                    continue;

                var methodAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
                if (!controllerAnonymous && !methodAnonymous)
                    continue;

                var httpMethodAttributes = method.GetCustomAttributes()
                    .OfType<IActionHttpMethodProvider>()
                    .ToList();

                if (httpMethodAttributes.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Action ẩn danh '{type.FullName}.{method.Name}' không mang attribute Http* nào — " +
                        "không tính được HTTP method để đối chiếu với allowlist (S4).");
                }

                foreach (var httpAttribute in httpMethodAttributes)
                {
                    var actionTemplate = (httpAttribute as IRouteTemplateProvider)?.Template;
                    var path = CombineRoute(controllerTemplate, actionTemplate);

                    foreach (var httpMethod in httpAttribute.HttpMethods)
                        endpoints.Add($"{httpMethod} {path}");
                }
            }
        }

        return endpoints;
    }

    private static string CombineRoute(string? controllerTemplate, string? actionTemplate)
    {
        var parts = new[] { controllerTemplate, actionTemplate }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim('/'));

        return "/" + string.Join('/', parts);
    }
}
