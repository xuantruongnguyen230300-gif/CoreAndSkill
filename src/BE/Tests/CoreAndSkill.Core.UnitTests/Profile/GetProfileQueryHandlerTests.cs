using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Profile;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Profile;

public class GetProfileQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserProfileService _profileService = Substitute.For<IUserProfileService>();

    [Fact]
    public async Task Handle_NoUserId_Throws()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var handler = new GetProfileQueryHandler(_currentUser, _profileService);

        await Should.ThrowAsync<InvalidOperationException>(() => handler.Handle(new GetProfileQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ProfileNotFound_Throws()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _profileService.GetAsync(userId, Arg.Any<CancellationToken>()).Returns((ProfileDto?)null);
        var handler = new GetProfileQueryHandler(_currentUser, _profileService);

        await Should.ThrowAsync<InvalidOperationException>(() => handler.Handle(new GetProfileQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ProfileFound_ReturnsIt()
    {
        var userId = Guid.NewGuid();
        var profile = new ProfileDto("an.nv", "an@vd.vn", "An", null, "vi", false, "v1");
        _currentUser.UserId.Returns(userId);
        _profileService.GetAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var handler = new GetProfileQueryHandler(_currentUser, _profileService);

        var result = await handler.Handle(new GetProfileQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(profile);
    }
}
