using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Tenants;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Tenants;

public class GetTenantsListQueryHandlerTests
{
    private readonly ITenantAdminQueryService _tenantQuery = Substitute.For<ITenantAdminQueryService>();

    [Fact]
    public async Task Handle_ReturnsPagedList()
    {
        var page = new PagedList<TenantListItemDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0 };
        _tenantQuery.SearchAsync(Arg.Any<TenantSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(page);
        var handler = new GetTenantsListQueryHandler(_tenantQuery);

        var result = await handler.Handle(new GetTenantsListQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(page);
    }

    [Fact]
    public async Task Handle_SortByNull_DefaultsToName()
    {
        _tenantQuery.SearchAsync(Arg.Any<TenantSearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<TenantListItemDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0 });
        var handler = new GetTenantsListQueryHandler(_tenantQuery);

        await handler.Handle(new GetTenantsListQuery(), CancellationToken.None);

        await _tenantQuery.Received(1).SearchAsync(
            Arg.Is<TenantSearchCriteria>(c => c.SortBy == "name"), Arg.Any<CancellationToken>());
    }
}
