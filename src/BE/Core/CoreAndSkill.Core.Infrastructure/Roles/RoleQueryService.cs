using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Roles;

// Hiện thực IRoleQueryService — chỉ đọc, chiếu thẳng vào DTO (docs/quy-uoc/be-cqrs-handler.md §11.2).
// AppRole (IdentityRole<Guid>) KHÔNG có navigation "Users" mặc định — userCount là subquery tương
// quan trên app_user_role, viết trực tiếp trong câu truy vấn.
internal sealed class RoleQueryService(CoreDbContext db) : IRoleQueryService
{
    public async Task<PagedList<RoleSummaryDto>> SearchAsync(RoleSearchCriteria criteria, CancellationToken ct)
    {
        var roles = db.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var pattern = LikePattern.Contains(criteria.SearchText.Trim());
            roles = roles.Where(r => EF.Functions.ILike(r.Name!, pattern, LikePattern.EscapeCharacter));
        }

        roles = (criteria.SortBy, criteria.SortDescending) switch
        {
            ("createdAt", false) => roles.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
            ("createdAt", true) => roles.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id),
            (_, false) => roles.OrderBy(r => r.NormalizedName).ThenBy(r => r.Id),
            (_, true) => roles.OrderByDescending(r => r.NormalizedName).ThenBy(r => r.Id),
        };

        var totalCount = await roles.CountAsync(ct);

        var page = await roles
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(r => new RoleSummaryDto(
                r.Id, r.Name!, r.IsSystem, db.UserRoles.Count(ur => ur.RoleId == r.Id), r.CreatedAt,
                r.ConcurrencyStamp ?? string.Empty))
            .ToListAsync(ct);

        return new PagedList<RoleSummaryDto>
        {
            Items = page,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalCount = totalCount,
        };
    }

    public Task<RoleSummaryDto?> FindByIdAsync(Guid id, CancellationToken ct)
        => db.Roles
            .Where(r => r.Id == id)
            .Select(r => new RoleSummaryDto(
                r.Id, r.Name!, r.IsSystem, db.UserRoles.Count(ur => ur.RoleId == r.Id), r.CreatedAt,
                r.ConcurrencyStamp ?? string.Empty))
            .SingleOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => db.Roles.AnyAsync(r => r.Id == id, ct);

    public async Task<IReadOnlySet<Guid>> FindExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var wanted = ids.Distinct().ToList();
        if (wanted.Count == 0)
            return new HashSet<Guid>();

        var found = await db.Roles.Where(r => wanted.Contains(r.Id)).Select(r => r.Id).ToListAsync(ct);
        return found.ToHashSet();
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken ct)
    {
        var normalized = name.Trim().ToUpperInvariant();
        return db.Roles.AnyAsync(r => r.NormalizedName == normalized && (excludingId == null || r.Id != excludingId), ct);
    }

    public async Task<bool> UserHoldsAnySystemRoleAsync(Guid userId, CancellationToken ct)
    {
        var query =
            from ur in db.UserRoles
            where ur.UserId == userId
            join r in db.Roles on ur.RoleId equals r.Id
            where r.IsSystem
            select r.Id;

        return await query.AnyAsync(ct);
    }
}
