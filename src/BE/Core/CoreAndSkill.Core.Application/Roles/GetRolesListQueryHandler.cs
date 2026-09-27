using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Roles;

internal sealed class GetRolesListQueryHandler(IRoleQueryService roleQuery)
    : IRequestHandler<GetRolesListQuery, Result<PagedList<RoleSummaryDto>>>
{
    public async Task<Result<PagedList<RoleSummaryDto>>> Handle(GetRolesListQuery query, CancellationToken ct)
    {
        var criteria = new RoleSearchCriteria(
            query.Page, query.PageSize, query.SortBy ?? "name", query.SortDescending, query.SearchText);

        return Result.Success(await roleQuery.SearchAsync(criteria, ct));
    }
}
