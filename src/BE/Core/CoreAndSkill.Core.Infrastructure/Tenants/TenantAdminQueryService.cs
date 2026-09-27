using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Tenants;

// Hiện thực ITenantAdminQueryService — chỉ đọc, chiếu thẳng vào DTO (docs/quy-uoc/be-cqrs-handler.md
// §11.2). core.tenant KHÔNG mang bộ lọc đơn vị (nó là GỐC — docs/wiki-core/be/17-multi-tenant.md §2),
// nên không có gì để IgnoreQueryFilters ở đây.
internal sealed class TenantAdminQueryService(CoreDbContext db) : ITenantAdminQueryService
{
    public async Task<PagedList<TenantListItemDto>> SearchAsync(TenantSearchCriteria criteria, CancellationToken ct)
    {
        var tenants = db.Tenants.Where(t => !t.IsSystem); // §1 "Ghi chú" — đơn vị hệ thống không liệt kê

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var pattern = LikePattern.Contains(criteria.SearchText.Trim());
            tenants = tenants.Where(t =>
                EF.Functions.ILike(t.Code, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(t.Name, pattern, LikePattern.EscapeCharacter));
        }

        tenants = (criteria.SortBy, criteria.SortDescending) switch
        {
            ("code", false) => tenants.OrderBy(t => t.Code).ThenBy(t => t.Id),
            ("code", true) => tenants.OrderByDescending(t => t.Code).ThenBy(t => t.Id),
            ("createdAt", false) => tenants.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),
            ("createdAt", true) => tenants.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id),
            (_, false) => tenants.OrderBy(t => t.Name).ThenBy(t => t.Id),
            (_, true) => tenants.OrderByDescending(t => t.Name).ThenBy(t => t.Id),
        };

        var totalCount = await tenants.CountAsync(ct);

        var page = await tenants
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(t => new TenantListItemDto(t.Id, t.Code, t.Name, t.IsActive, t.CreatedAt))
            .ToListAsync(ct);

        return new PagedList<TenantListItemDto>
        {
            Items = page,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalCount = totalCount,
        };
    }

    public Task<TenantListItemDto?> FindByIdAsync(Guid id, CancellationToken ct)
        => db.Tenants
            .Where(t => t.Id == id)
            .Select(t => new TenantListItemDto(t.Id, t.Code, t.Name, t.IsActive, t.CreatedAt))
            .SingleOrDefaultAsync(ct);
}
