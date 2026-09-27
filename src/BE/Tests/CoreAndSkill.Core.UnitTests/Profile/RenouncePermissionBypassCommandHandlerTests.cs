using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Profile;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Profile;

public class RenouncePermissionBypassCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserProfileService _profileService = Substitute.For<IUserProfileService>();

    [Fact]
    public async Task Handle_NoUserId_Throws()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var handler = new RenouncePermissionBypassCommandHandler(_currentUser, _profileService);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new RenouncePermissionBypassCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotHoldingBypass_ReturnsError()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _profileService.RenouncePermissionBypassAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ProfileErrors.PermissionBypassNotHeld));

        var handler = new RenouncePermissionBypassCommandHandler(_currentUser, _profileService);
        var result = await handler.Handle(new RenouncePermissionBypassCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(ProfileErrors.PermissionBypassNotHeld.Code);
    }

    [Fact]
    public async Task Handle_Success_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _profileService.RenouncePermissionBypassAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new RenouncePermissionBypassCommandHandler(_currentUser, _profileService);
        var result = await handler.Handle(new RenouncePermissionBypassCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }
}
