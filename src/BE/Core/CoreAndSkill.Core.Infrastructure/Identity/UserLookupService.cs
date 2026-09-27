using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Hiện thực IUserLookupService — chỉ đọc, chiếu thẳng vào DTO (docs/quy-uoc/be-cqrs-handler.md §11.2).
internal sealed class UserLookupService(CoreDbContext db) : IUserLookupService
{
    public Task<UserSummaryDto?> FindByIdAsync(Guid userId, CancellationToken ct)
        => db.Users
            .Where(u => u.Id == userId)
            .Select(ToSummary)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserSummaryDto>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        => await db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(ToSummary)
            .ToListAsync(ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct);
    }

    public Task<bool> UserNameExistsAsync(string userName, CancellationToken ct)
    {
        var normalized = userName.Trim().ToUpperInvariant();
        return db.Users.AnyAsync(u => u.NormalizedUserName == normalized, ct);
    }

    public Task<bool> HasPermissionBypassAsync(Guid userId, CancellationToken ct)
        => db.Users.Where(u => u.Id == userId).Select(u => u.HasPermissionBypass).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct)
    {
        var roleIds = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        if (roleIds.Count == 0)
            return [];

        return await db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync(ct);
    }

    private static readonly System.Linq.Expressions.Expression<Func<AppUser, UserSummaryDto>> ToSummary = u =>
        new UserSummaryDto(
            u.Id,
            u.UserName!,
            u.FullName,
            u.Email,
            u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
            u.IsSystemOperator,
            u.MustChangePassword,
            u.PreferredLanguage);
}
