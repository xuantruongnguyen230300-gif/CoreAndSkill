using CoreAndSkill.Core.Application.Users;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class GetUserByIdQueryHandlerTests
{
    private readonly IUserQueryService _userQuery = Substitute.For<IUserQueryService>();

    private GetUserByIdQueryHandler CreateHandler() => new(_userQuery);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _userQuery.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns((UserListItemDto?)null);

        var result = await CreateHandler().Handle(new GetUserByIdQuery(id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task Handle_Found_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        var dto = new UserListItemDto(id, "an.nv", "an@vd.vn", "An", [], false, null, false, false, null, "v1");
        _userQuery.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await CreateHandler().Handle(new GetUserByIdQuery(id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(dto);
    }
}
