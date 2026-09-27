using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Web.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreAndSkill.Core.Web.Filters;

// Filter TOÀN CỤC — cùng khuôn RequirePermissionFilter, đọc CLAIM (KHÔNG truy DB) —
// docs/quy-uoc/be-api-controller.md §4.2.
internal sealed class RequireSystemOperatorFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var endpointMetadata = context.ActionDescriptor.EndpointMetadata;

        if (endpointMetadata.OfType<IAllowAnonymous>().Any())
            return;

        if (!endpointMetadata.OfType<RequireSystemOperatorAttribute>().Any())
            return;

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var isSystemOperator = context.HttpContext.User.FindFirst(CoreClaimTypes.IsSystemOperator)?.Value;
        if (!string.Equals(isSystemOperator, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            context.Result = new ForbidResult();
    }
}
