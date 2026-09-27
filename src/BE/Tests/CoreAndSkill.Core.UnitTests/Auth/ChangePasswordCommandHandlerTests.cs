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

public class ChangePasswordCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly ITenantLookup _tenantLookup = Substitute.For<ITenantLookup>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();

    private ChangePasswordCommandHandler CreateHandler()
    {
        var authOptions = Options.Create(new CoreAuthOptions
        {
            CookieName = "c",
            AntiforgeryCookieName = "a",
            SessionMinutes = 30,
            AllowedOrigins = ["https://x"],
        });
        var factory = new SessionDtoFactory(_userLookup, _permissionChecker, authOptions);
        return new ChangePasswordCommandHandler(_currentUser, _tenantContext, _identityService, _userLookup, _tenantLookup, factory);
    }

    [Fact]
    public async Task Handle_PassesClearMustChangePasswordFalse()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(tenantId);
        _identityService
            .ChangePasswordAsync(userId, "old", "new", false, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CredentialCheck(userId, "stamp", false)));
        _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserSummaryDto(userId, "an.nv", "An", null, false, false, false, null));
        _tenantLookup.FindByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(tenantId, "SO-GD", "Sở GD", true));

        var result = await CreateHandler().Handle(new ChangePasswordCommand("old", "new"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _identityService.Received(1).ChangePasswordAsync(userId, "old", "new", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdentityFailure_PropagatesError()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _identityService
            .ChangePasswordAsync(userId, Arg.Any<string>(), Arg.Any<string>(), false, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CredentialCheck>(AuthErrors.ChangePasswordFailed));

        var result = await CreateHandler().Handle(new ChangePasswordCommand("old", "new"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(AuthErrors.ChangePasswordFailed.Code);
    }
}
