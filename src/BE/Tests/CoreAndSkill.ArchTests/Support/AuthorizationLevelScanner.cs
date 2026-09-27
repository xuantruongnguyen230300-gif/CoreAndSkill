using System.Reflection;
using CoreAndSkill.Core.Web.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CoreAndSkill.ArchTests.Support;

// Luật S11 — docs/RULES.md S11, docs/quy-uoc/be-api-controller.md §4.2. Đếm THEO MỨC, không theo
// số attribute: nhiều [RequirePermission] trên cùng action/level vẫn là MỘT mức; action khai mức
// KHÁC mức cấp controller là vi phạm. [AllowAnonymous] thắng — nhưng CHỈ sau khi xác nhận action đó
// có tên trong allowlist ẩn danh (S4); không có tên trong allowlist thì vẫn bị S11 kiểm bình
// thường (rơi vào ca 0 mức — đúng tinh thần "quên khai quyền thì bị từ chối, không mở mặc định").
internal static class AuthorizationLevelScanner
{
    public static IReadOnlyList<string> FindActionsWithoutExactlyOneLevel(Assembly assembly, IReadOnlySet<string> anonymousAllowlist)
    {
        var offenders = new List<string>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(ControllerBase).IsAssignableFrom(type))
                continue;

            var controllerAllowAnonymous = type.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
            var controllerTemplate = type.GetCustomAttribute<RouteAttribute>()?.Template;
            var controllerHasPermission = type.GetCustomAttributes<RequirePermissionAttribute>().Any();
            var controllerHasSystemOperator = type.GetCustomAttribute<RequireSystemOperatorAttribute>() is not null;
            var controllerHasAuthenticatedOnly = type.GetCustomAttribute<AuthenticatedOnlyAttribute>() is not null;

            const BindingFlags actionFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (var method in type.GetMethods(actionFlags))
            {
                if (method.IsSpecialName || method.GetCustomAttribute<NonActionAttribute>() is not null)
                    continue;

                var httpMethodAttributes = method.GetCustomAttributes().OfType<IActionHttpMethodProvider>().ToList();
                if (httpMethodAttributes.Count == 0)
                    continue; // không phải action HTTP.

                var methodAllowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() is not null;

                if (controllerAllowAnonymous || methodAllowAnonymous)
                {
                    var routes = BuildRoutes(controllerTemplate, httpMethodAttributes);
                    if (routes.Any(anonymousAllowlist.Contains))
                        continue; // thắng hợp lệ — bỏ qua.
                    // Không có tên trong allowlist — KHÔNG bỏ qua, rơi xuống kiểm "đúng một mức" bên dưới.
                }

                var methodHasPermission = method.GetCustomAttributes<RequirePermissionAttribute>().Any();
                var methodHasSystemOperator = method.GetCustomAttribute<RequireSystemOperatorAttribute>() is not null;
                var methodHasAuthenticatedOnly = method.GetCustomAttribute<AuthenticatedOnlyAttribute>() is not null;

                var levelCount = new[]
                {
                    controllerHasPermission || methodHasPermission,
                    controllerHasSystemOperator || methodHasSystemOperator,
                    controllerHasAuthenticatedOnly || methodHasAuthenticatedOnly,
                }.Count(present => present);

                if (levelCount != 1)
                    offenders.Add($"{type.FullName}.{method.Name}");
            }
        }

        return offenders;
    }

    private static List<string> BuildRoutes(string? controllerTemplate, List<IActionHttpMethodProvider> httpMethodAttributes)
    {
        var routes = new List<string>();

        foreach (var httpAttribute in httpMethodAttributes)
        {
            var actionTemplate = (httpAttribute as IRouteTemplateProvider)?.Template;
            var parts = new[] { controllerTemplate, actionTemplate }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim('/'));
            var path = "/" + string.Join('/', parts);

            foreach (var httpMethod in httpAttribute.HttpMethods)
                routes.Add($"{httpMethod} {path}");
        }

        return routes;
    }
}
