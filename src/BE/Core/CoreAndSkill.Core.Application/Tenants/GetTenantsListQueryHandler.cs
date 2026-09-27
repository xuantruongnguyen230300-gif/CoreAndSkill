using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Tenants;

internal sealed class GetTenantsListQueryHandler(ITenantAdminQueryService tenantQuery)
    : IRequestHandler<GetTenantsListQuery, Result<PagedList<TenantListItemDto>>>
{
    public async Task<Result<PagedList<TenantListItemDto>>> Handle(GetTenantsListQuery query, CancellationToken ct)
    {
        var criteria = new TenantSearchCriteria(
            query.Page, query.PageSize, query.SortBy ?? "name", query.SortDescending, query.SearchText);

        return Result.Success(await tenantQuery.SearchAsync(criteria, ct));
    }
}
