using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Users;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class GetUsersListQueryHandlerTests
{
    private readonly IUserQueryService _userQuery = Substitute.For<IUserQueryService>();
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();

    private GetUsersListQueryHandler CreateHandler() => new(_userQuery, _roleQuery);

    [Fact]
    public async Task Handle_RoleIdProvidedButDoesNotExist_ReturnsRoleNotFound()
    {
        var roleId = Guid.NewGuid();
        _roleQuery.ExistsAsync(roleId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(new GetUsersListQuery(RoleId: roleId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.RoleNotFound.Code);
        await _userQuery.DidNotReceiveWithAnyArgs().SearchAsync(default!, default);
    }

    [Fact]
    public async Task Handle_NoRoleFilter_ReturnsPagedList()
    {
        var page = new PagedList<UserListItemDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0 };
        _userQuery.SearchAsync(Arg.Any<UserSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(page);

        var result = await CreateHandler().Handle(new GetUsersListQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(page);
    }

    [Fact]
    public async Task Handle_RoleIdExists_SearchesWithCriteria()
    {
        var roleId = Guid.NewGuid();
        _roleQuery.ExistsAsync(roleId, Arg.Any<CancellationToken>()).Returns(true);
        var page = new PagedList<UserListItemDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0 };
        _userQuery.SearchAsync(Arg.Any<UserSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(page);

        var result = await CreateHandler().Handle(new GetUsersListQuery(RoleId: roleId, SortBy: "fullName"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _userQuery.Received(1).SearchAsync(
            Arg.Is<UserSearchCriteria>(c => c.RoleId == roleId && c.SortBy == "fullName"), Arg.Any<CancellationToken>());
    }
}
