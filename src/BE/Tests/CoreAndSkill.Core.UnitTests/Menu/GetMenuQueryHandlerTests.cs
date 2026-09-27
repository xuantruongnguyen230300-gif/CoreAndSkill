using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Menu;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Menu;

public class GetMenuQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMenuQueryService _menuQuery = Substitute.For<IMenuQueryService>();

    private GetMenuQueryHandler CreateHandler() => new(_currentUser, _menuQuery);

    [Fact]
    public async Task Handle_Authenticated_ReturnsVisibleMenu()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        IReadOnlyList<MenuItemDto> menu = [new MenuItemDto(Guid.NewGuid(), null, "quan-tri", "menu.quan-tri", "pi-cog", null, 90)];
        _menuQuery.GetVisibleMenuAsync(userId, Arg.Any<CancellationToken>()).Returns(menu);

        var result = await CreateHandler().Handle(new GetMenuQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(menu);
    }

    [Fact]
    public async Task Handle_Unauthenticated_Throws()
    {
        _currentUser.UserId.Returns((Guid?)null);

        await Should.ThrowAsync<InvalidOperationException>(
            () => CreateHandler().Handle(new GetMenuQuery(), CancellationToken.None));
    }
}
