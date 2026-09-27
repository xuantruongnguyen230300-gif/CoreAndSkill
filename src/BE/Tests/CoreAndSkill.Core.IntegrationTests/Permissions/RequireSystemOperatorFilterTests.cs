using System.Security.Claims;
using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Web.Filters;
using CoreAndSkill.Core.Web.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// Cùng khuôn RequirePermissionFilterTests — đọc CLAIM, không đọc DB (docs/quy-uoc/be-api-controller.md §4.2).
public class RequireSystemOperatorFilterTests
{
    private static AuthorizationFilterContext CreateContext(IList<object> endpointMetadata, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext();
        if (user is not null)
            httpContext.User = user;

        var actionDescriptor = new ActionDescriptor { EndpointMetadata = endpointMetadata };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static ClaimsPrincipal AuthenticatedUser(bool isSystemOperator)
    {
        var claims = new List<Claim> { new(CoreClaimTypes.IsSystemOperator, isSystemOperator.ToString()) };
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void OnAuthorization_AllowAnonymous_DoesNothing()
    {
        var context = CreateContext([new AllowAnonymousAttribute(), new RequireSystemOperatorAttribute()]);

        new RequireSystemOperatorFilter().OnAuthorization(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public void OnAuthorization_NoRequireSystemOperatorAttribute_DoesNothing()
    {
        var context = CreateContext([]);

        new RequireSystemOperatorFilter().OnAuthorization(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public void OnAuthorization_NotAuthenticated_SetsChallengeResult()
    {
        var context = CreateContext([new RequireSystemOperatorAttribute()]);

        new RequireSystemOperatorFilter().OnAuthorization(context);

        context.Result.ShouldBeOfType<ChallengeResult>();
    }

    [Fact]
    public void OnAuthorization_AuthenticatedButNotSystemOperator_SetsForbidResult()
    {
        var context = CreateContext([new RequireSystemOperatorAttribute()], AuthenticatedUser(isSystemOperator: false));

        new RequireSystemOperatorFilter().OnAuthorization(context);

        context.Result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public void OnAuthorization_SystemOperator_DoesNothing()
    {
        var context = CreateContext([new RequireSystemOperatorAttribute()], AuthenticatedUser(isSystemOperator: true));

        new RequireSystemOperatorFilter().OnAuthorization(context);

        context.Result.ShouldBeNull();
    }
}
