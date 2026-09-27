using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Roles;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Roles;

public class GetRolesListQueryHandlerTests
{
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();

    [Fact]
    public async Task Handle_ReturnsPagedList()
    {
        var page = new PagedList<RoleSummaryDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0 };
        _roleQuery.SearchAsync(Arg.Any<RoleSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(page);
        var handler = new GetRolesListQueryHandler(_roleQuery);

        var result = await handler.Handle(new GetRolesListQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(page);
    }
}
