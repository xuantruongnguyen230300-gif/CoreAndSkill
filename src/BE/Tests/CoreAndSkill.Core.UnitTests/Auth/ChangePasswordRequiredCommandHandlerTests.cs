using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

public class ChangePasswordRequiredCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly ITenantLookup _tenantLookup = Substitute.For<ITenantLookup>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();

    private ChangePasswordRequiredCommandHandler CreateHandler()
    {
        var authOptions = Options.Create(new CoreAuthOptions
        {
            CookieName = "c",
            AntiforgeryCookieName = "a",
            SessionMinutes = 30,
            AllowedOrigins = ["https://x"],
        });
        var factory = new SessionDtoFactory(_userLookup, _permissionChecker, authOptions);
        return new ChangePasswordRequiredCommandHandler(_currentUser, _tenantContext, _identityService, _userLookup, _tenantLookup, factory);
    }

    [Fact]
    public async Task Handle_NotInMustChangePasswordState_ReturnsPasswordChangeNotRequired()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserSummaryDto(userId, "an.nv", "An", null, false, false, MustChangePassword: false, null));

        var result = await CreateHandler().Handle(new ChangePasswordRequiredCommand("old", "new"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(AuthErrors.PasswordChangeNotRequired.Code);
        await _identityService.DidNotReceiveWithAnyArgs()
            .ChangePasswordAsync(default, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_RequiredState_PassesClearMustChangePasswordTrue()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserSummaryDto(userId, "an.nv", "An", null, false, false, MustChangePassword: true, null));
        _identityService
            .ChangePasswordAsync(userId, "old", "new", true, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CredentialCheck(userId, "stamp", false)));
        _tenantLookup.FindByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(tenantId, "SO-GD", "Sở GD", true));

        var result = await CreateHandler().Handle(new ChangePasswordRequiredCommand("old", "new"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Session.MustChangePassword.ShouldBeFalse();
        await _identityService.Received(1).ChangePasswordAsync(userId, "old", "new", true, Arg.Any<CancellationToken>());
    }
}
