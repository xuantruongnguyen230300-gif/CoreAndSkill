using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Web.Filters;
using CoreAndSkill.Core.Web.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// Test THUẦN logic của filter — không HTTP thật, không DB (docs/quy-uoc/be-api-controller.md §4.2).
// Đặt ở IntegrationTests vì đây là project test DUY NHẤT có đường tới Core.Web (cùng lý do
// IdentityConcurrencyTests.cs).
public class RequirePermissionFilterTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();

    private RequirePermissionFilter CreateFilter() => new(_permissionChecker, _currentUser);

    private static AuthorizationFilterContext CreateContext(IList<object> endpointMetadata)
    {
        var actionDescriptor = new ActionDescriptor { EndpointMetadata = endpointMetadata };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task OnAuthorizationAsync_AllowAnonymous_DoesNothing()
    {
        var context = CreateContext([new AllowAnonymousAttribute(), new RequirePermissionAttribute("core.user.read")]);

        await CreateFilter().OnAuthorizationAsync(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_NoRequirePermissionAttribute_DoesNothing()
    {
        var context = CreateContext([]);

        await CreateFilter().OnAuthorizationAsync(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_NotAuthenticated_SetsChallengeResult()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var context = CreateContext([new RequirePermissionAttribute("core.user.read")]);

        await CreateFilter().OnAuthorizationAsync(context);

        context.Result.ShouldBeOfType<ChallengeResult>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_MissingPermission_SetsForbidResult()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _permissionChecker.HasPermissionAsync(userId, "core.user.read", Arg.Any<CancellationToken>()).Returns(false);
        var context = CreateContext([new RequirePermissionAttribute("core.user.read")]);

        await CreateFilter().OnAuthorizationAsync(context);

        context.Result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_HasAllRequiredKeys_DoesNothing()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _permissionChecker.HasPermissionAsync(userId, "core.user.read", Arg.Any<CancellationToken>()).Returns(true);
        _permissionChecker.HasPermissionAsync(userId, "core.user.write", Arg.Any<CancellationToken>()).Returns(true);
        var context = CreateContext(
        [
            new RequirePermissionAttribute("core.user.read"),
            new RequirePermissionAttribute("core.user.write"),
        ]);

        await CreateFilter().OnAuthorizationAsync(context);

        context.Result.ShouldBeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_HasFirstKeyButNotSecond_SetsForbidResult()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _permissionChecker.HasPermissionAsync(userId, "core.user.read", Arg.Any<CancellationToken>()).Returns(true);
        _permissionChecker.HasPermissionAsync(userId, "core.user.write", Arg.Any<CancellationToken>()).Returns(false);
        var context = CreateContext(
        [
            new RequirePermissionAttribute("core.user.read"),
            new RequirePermissionAttribute("core.user.write"),
        ]);

        await CreateFilter().OnAuthorizationAsync(context);

        context.Result.ShouldBeOfType<ForbidResult>();
    }
}
