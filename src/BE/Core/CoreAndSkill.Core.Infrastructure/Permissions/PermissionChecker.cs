using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Permissions;

// docs/quy-uoc/be-api-controller.md §4.2, §4.3. Cờ has_permission_bypass là nhánh DUY NHẤT bỏ qua
// ma trận — đọc ở ĐÚNG MỘT CHỖ trong toàn hệ (docs/adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md).
// Tập quyền hiệu lực RỖNG là dữ liệu trung thực, không phải lỗi: tài khoản không giữ vai trò nào, hoặc tài
// khoản vận hành hệ thống — cờ vận hành không phải một quyền trong ma trận.
internal sealed class PermissionChecker(CoreDbContext db) : IPermissionChecker
{
    public async Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken ct)
    {
        var effective = await GetEffectivePermissionsAsync(userId, ct);
        return effective.Contains(permissionKey);
    }

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct)
    {
        var hasBypass = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.HasPermissionBypass)
            .SingleOrDefaultAsync(ct);

        if (hasBypass)
        {
            var all = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
            return all.ToHashSet(StringComparer.Ordinal);
        }

        var roleIds = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        if (roleIds.Count == 0)
            return new HashSet<string>(StringComparer.Ordinal);

        var codes = await (
            from rp in db.RolePermissions
            where roleIds.Contains(rp.RoleId)
            join p in db.Permissions on rp.PermissionId equals p.Id
            select p.Code)
            .Distinct()
            .ToListAsync(ct);

        return codes.ToHashSet(StringComparer.Ordinal);
    }

    // MỘT câu cho cả danh sách — số câu SQL không tăng theo số vai trò (PermissionSetQueryCountTests).
    public async Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetPermissionsForRolesAsync(
        IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        var ids = roleIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, IReadOnlySet<string>>();

        var rows = await (
            from rp in db.RolePermissions
            where ids.Contains(rp.RoleId)
            join p in db.Permissions on rp.PermissionId equals p.Id
            select new { rp.RoleId, p.Code })
            .Distinct()
            .ToListAsync(ct);

        var byRole = rows.ToLookup(r => r.RoleId, r => r.Code);
        return ids.ToDictionary(id => id, IReadOnlySet<string> (id) => byRole[id].ToHashSet(StringComparer.Ordinal));
    }
}
