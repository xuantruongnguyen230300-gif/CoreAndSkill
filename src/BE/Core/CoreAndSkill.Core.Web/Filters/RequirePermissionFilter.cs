using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Web.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreAndSkill.Core.Web.Filters;

// Filter TOÀN CỤC (đăng ký trong AddCoreWeb) — tự soi EndpointMetadata, chỉ can thiệp khi action
// mang [RequirePermission]. docs/quy-uoc/be-api-controller.md §4.2.
internal sealed class RequirePermissionFilter(IPermissionChecker permissionChecker, ICurrentUser currentUser)
    : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpointMetadata = context.ActionDescriptor.EndpointMetadata;

        // 1 — [AllowAnonymous] thắng mức khai ở cấp controller (bảng §4.2).
        if (endpointMetadata.OfType<IAllowAnonymous>().Any())
            return;

        // 2 — action mang mức khác (không có RequirePermission nào) thì không kiểm gì ở đây.
        var keys = endpointMetadata.OfType<RequirePermissionAttribute>().Select(a => a.Key).Distinct().ToList();
        if (keys.Count == 0)
            return;

        // 3 — chưa xác thực.
        var userId = currentUser.UserId;
        if (userId is null)
        {
            context.Result = new ChallengeResult();
            return;
        }

        // 4 — đòi TẤT CẢ khoá.
        foreach (var key in keys)
        {
            if (!await permissionChecker.HasPermissionAsync(userId.Value, key, context.HttpContext.RequestAborted))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
