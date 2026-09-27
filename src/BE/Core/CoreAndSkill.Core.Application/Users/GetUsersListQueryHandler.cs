using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class GetUsersListQueryHandler(IUserQueryService userQuery, IRoleQueryService roleQuery)
    : IRequestHandler<GetUsersListQuery, Result<PagedList<UserListItemDto>>>
{
    public async Task<Result<PagedList<UserListItemDto>>> Handle(GetUsersListQuery query, CancellationToken ct)
    {
        // roleId không tồn tại là LỖI, không phải "trả danh sách rỗng" — docs/contracts/users.md §3.
        if (query.RoleId is { } roleId && !await roleQuery.ExistsAsync(roleId, ct))
            return Result.Failure<PagedList<UserListItemDto>>(
                UserErrors.RoleNotFound.WithParams(("RoleId", roleId)));

        var criteria = new UserSearchCriteria(
            query.Page, query.PageSize, query.SortBy ?? "userName", query.SortDescending,
            query.SearchText, query.RoleId, query.Status);

        return Result.Success(await userQuery.SearchAsync(criteria, ct));
    }
}
