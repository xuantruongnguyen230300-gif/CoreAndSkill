using CoreAndSkill.Core.Application.Common.Cqrs;
using CoreAndSkill.Core.Application.Common.Paging;

namespace CoreAndSkill.Core.Application.Users;

// GET /api/v1/core/users — docs/contracts/users.md §3. SortBy null ⇒ handler áp mặc định "userName".
public sealed record GetUsersListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDescending = false,
    string? SearchText = null,
    Guid? RoleId = null,
    string? Status = null) : IQuery<PagedList<UserListItemDto>>;
