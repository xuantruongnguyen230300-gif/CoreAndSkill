using CoreAndSkill.Core.Application.Common.Cqrs;
using CoreAndSkill.Core.Application.Common.Paging;

namespace CoreAndSkill.Core.Application.Tenants;

// GET /api/v1/core/system/tenants — docs/contracts/tenants.md §1. SortBy null ⇒ handler áp mặc định "name".
public sealed record GetTenantsListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDescending = false,
    string? SearchText = null) : IQuery<PagedList<TenantListItemDto>>;
