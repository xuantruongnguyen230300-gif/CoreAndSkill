using CoreAndSkill.Core.Application.Common.Cqrs;
using CoreAndSkill.Core.Application.Common.Paging;

namespace CoreAndSkill.Core.Application.Roles;

// GET /api/v1/core/roles — docs/contracts/roles.md §1. SortBy null ⇒ handler áp mặc định "name".
public sealed record GetRolesListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDescending = false,
    string? SearchText = null) : IQuery<PagedList<RoleSummaryDto>>;
