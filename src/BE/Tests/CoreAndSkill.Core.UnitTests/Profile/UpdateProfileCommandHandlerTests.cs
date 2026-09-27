using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Profile;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Profile;

public class UpdateProfileCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserProfileService _profileService = Substitute.For<IUserProfileService>();

    [Fact]
    public async Task Handle_NoUserId_Throws()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var handler = new UpdateProfileCommandHandler(_currentUser, _profileService);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new UpdateProfileCommand("An", null, null, "v1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DelegatesToProfileServiceWithMappedInput()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        var expected = new ProfileDto("an.nv", null, "An Updated", "0912345678", "vi", false, "v2");
        _profileService
            .UpdateAsync(userId, Arg.Is<UpdateProfileInput>(i =>
                i.FullName == "An Updated" && i.PhoneNumber == "0912345678" && i.PreferredLanguage == "vi" && i.Version == "v1"),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success(expected));

        var handler = new UpdateProfileCommandHandler(_currentUser, _profileService);
        var result = await handler.Handle(new UpdateProfileCommand("An Updated", "0912345678", "vi", "v1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflict_Propagates()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _profileService.UpdateAsync(userId, Arg.Any<UpdateProfileInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ProfileDto>(CoreAndSkill.Core.Application.Common.CommonErrors.ConcurrencyConflict));

        var handler = new UpdateProfileCommandHandler(_currentUser, _profileService);
        var result = await handler.Handle(new UpdateProfileCommand("An", null, null, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CoreAndSkill.Core.Application.Common.CommonErrors.ConcurrencyConflict.Code);
    }
}
