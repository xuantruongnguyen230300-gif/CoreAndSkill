using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

public class GetCurrentSessionQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ITenantLookup _tenantLookup = Substitute.For<ITenantLookup>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();

    private GetCurrentSessionQueryHandler CreateHandler()
    {
        var authOptions = Options.Create(new CoreAuthOptions
        {
            CookieName = "c",
            AntiforgeryCookieName = "a",
            SessionMinutes = 30,
            AllowedOrigins = ["https://x"],
        });
        var factory = new SessionDtoFactory(_userLookup, _permissionChecker, authOptions);
        return new GetCurrentSessionQueryHandler(_currentUser, _tenantContext, _tenantLookup, _userLookup, factory);
    }

    [Fact]
    public async Task Handle_NoUserId_Throws()
    {
        _currentUser.UserId.Returns((Guid?)null);

        await Should.ThrowAsync<InvalidOperationException>(
            () => CreateHandler().Handle(new GetCurrentSessionQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTenantId_Throws()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _tenantContext.TenantId.Returns((Guid?)null);

        await Should.ThrowAsync<InvalidOperationException>(
            () => CreateHandler().Handle(new GetCurrentSessionQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserAndTenantExist_ReturnsSession()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserSummaryDto(userId, "an.nv", "An", null, false, true, true, null));
        _tenantLookup.FindByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(tenantId, "SO-GD", "Sở GD", true));
        _userLookup.GetRoleNamesAsync(userId, Arg.Any<CancellationToken>()).Returns([]);
        _permissionChecker.GetEffectivePermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        var result = await CreateHandler().Handle(new GetCurrentSessionQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(userId);
        result.Value.MustChangePassword.ShouldBeTrue();
        result.Value.IsSystemOperator.ShouldBeTrue();
        result.Value.TenantCode.ShouldBe("SO-GD");
    }
}
