using System.Security.Cryptography;
using System.Text;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Permissions;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Roles;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Permissions;

// Hiện thực IPermissionMatrixService — docs/contracts/permissions.md. Mọi phép kiểm ở §6 chạy
// TRƯỚC bất kỳ thao tác ghi nào (§6 "Ghi chú": mã lỗi trả về mà bảng vẫn bị xoá là vô nghĩa).
internal sealed class PermissionMatrixService(CoreDbContext db) : IPermissionMatrixService
{
    public async Task<IReadOnlyList<PermissionListItemDto>> GetCatalogAsync(CancellationToken ct)
    {
        var query =
            from p in db.Permissions
            join res in db.PermissionResources on p.ResourceKey equals res.Key
            orderby res.DisplayOrder, p.DisplayOrder
            select new PermissionListItemDto(p.Id, p.Code, p.ResourceKey, p.Action, p.NameKey, p.IsSystem, res.ModuleKey);

        return await query.ToListAsync(ct);
    }

    public async Task<PermissionMatrixDto> GetMatrixAsync(CancellationToken ct)
    {
        var roles = await GetRolesAsync(ct);
        var permissionRows = await GetPermissionRowsAsync(ct);
        var grants = await GetGrantsAsync(ct);
        var grantsByPermission = GroupGrantsByPermission(grants);

        var rows = permissionRows
            .Select(p => new PermissionMatrixRowDto(
                p.Id, p.Code, p.ResourceKey, p.ResourceNameKey, p.NameKey,
                grantsByPermission.TryGetValue(p.Id, out var granted) ? granted : []))
            .ToList();

        return new PermissionMatrixDto(roles, rows, ComputeVersion(grants));
    }

    public async Task<PermissionMatrixByResourceDto> GetMatrixByResourceAsync(CancellationToken ct)
    {
        var roles = await GetRolesAsync(ct);
        var permissionRows = await GetPermissionRowsAsync(ct);
        var grants = await GetGrantsAsync(ct);
        var grantsByPermission = GroupGrantsByPermission(grants);

        var resources = permissionRows
            .GroupBy(p => (p.ResourceKey, p.ResourceNameKey, p.ModuleKey, p.ResourceDisplayOrder))
            .OrderBy(g => g.Key.ResourceDisplayOrder)
            .Select(g => new PermissionMatrixResourceDto(
                g.Key.ResourceKey, g.Key.ResourceNameKey, g.Key.ModuleKey,
                g.Select(p => new PermissionMatrixResourceActionDto(
                    p.Id, p.Action, grantsByPermission.TryGetValue(p.Id, out var granted) ? granted : []))
                 .ToList()))
            .ToList();

        return new PermissionMatrixByResourceDto(roles, resources, ComputeVersion(grants));
    }

    public async Task<Result<string>> ReplaceMatrixAsync(
        string? expectedVersion, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> entries, CancellationToken ct)
    {
        await LockMatrixOfCurrentTenantAsync(ct);

        // Khoá dòng MỌI vai trò của đơn vị (FOR KEY SHARE, theo thứ tự id) — nợ E17, cặp khoá với lượt xoá vai trò; vì sao
        // mọi vai trò chứ không chỉ vai trò được cấp thêm: RoleRowLocks.LockAllForMatrixWriteAsync. Đứng TRƯỚC mọi câu đọc bên
        // dưới, nên tập vai trò đứng yên từ lần kiểm tồn tại tới câu INSERT/UPDATE của lượt lưu: vai trò bị xoá trước khoá thì
        // không có trong tập (ROLE_NOT_FOUND nếu payload nhắc tới nó, lệch version nếu nó đang có quyền); lệnh xoá tới sau khoá
        // thì chờ lượt này commit.
        //
        // Thứ tự: khoá tư vấn của ma trận TRƯỚC, khoá dòng vai trò SAU — không vòng chờ nào đi qua cặp này:
        //   • lệnh xoá vai trò không xin khoá tư vấn của ma trận, nên không có lượt nào giữ khoá dòng vai trò rồi mới chờ khoá tư
        //     vấn — vòng "tư vấn ⇄ dòng" không dựng được;
        //   • khoá tư vấn đứng trước nên mỗi đơn vị chỉ MỘT lượt ghi ma trận giữ khoá dòng vai trò; các lượt đang xếp hàng ở khoá
        //     tư vấn không giữ gì, và lệnh xoá vai trò chỉ phải chờ lượt đang ghi — không chờ cả hàng;
        //   • lượt ghi ma trận giữ khoá dòng vai trò TRƯỚC mọi câu ghi role_permission. Không vậy thì có vòng: lượt ghi thu hồi
        //     một dòng role_permission của vai trò R (giữ khoá dòng đó) rồi cấp thêm cho R (câu INSERT chờ khoá dòng R của lượt
        //     xoá), còn lượt xoá đang cascade xoá đúng dòng role_permission kia (chờ lượt ghi). Khoá R từ đầu thì lượt xoá không
        //     thể đang giữ R khi lượt ghi chạm role_permission.
        var existingRoleIdSet = await RoleRowLocks.LockAllForMatrixWriteAsync(db, ct);

        var catalogIds = await db.Permissions.Select(p => p.Id).ToListAsync(ct);
        var catalogIdSet = catalogIds.ToHashSet();

        foreach (var permissionId in entries.Keys)
        {
            if (!catalogIdSet.Contains(permissionId))
                return Result.Failure<string>(PermissionErrors.NotFound.WithParams(("PermissionId", permissionId)));
        }

        if (catalogIdSet.Except(entries.Keys).Any())
            return Result.Failure<string>(PermissionErrors.EntriesIncomplete);

        // Tồn tại = có trong tập vừa KHOÁ, không đọc lại: câu đọc thứ hai ngoài khoá sẽ mở lại đúng khe hở E17.
        foreach (var roleId in entries.Values.SelectMany(v => v).Distinct())
        {
            if (!existingRoleIdSet.Contains(roleId))
                return Result.Failure<string>(PermissionErrors.RoleNotFound.WithParams(("RoleId", roleId)));
        }

        var currentGrants = await GetGrantsAsync(ct);
        var currentVersion = ComputeVersion(currentGrants);
        if (expectedVersion is null || !string.Equals(expectedVersion, currentVersion, StringComparison.Ordinal))
            return Result.Failure<string>(PermissionErrors.VersionMismatch);

        var systemRoleIds = await db.Roles.Where(r => r.IsSystem).Select(r => r.Id).ToHashSetAsync(ct);
        if (systemRoleIds.Count > 0)
        {
            var permissionWriteId = await db.Permissions
                .Where(p => p.Code == CorePermissions.PermissionWrite)
                .Select(p => p.Id)
                .SingleOrDefaultAsync(ct);

            if (permissionWriteId != Guid.Empty && entries.TryGetValue(permissionWriteId, out var newRolesForWrite))
            {
                var currentRolesForWrite = currentGrants
                    .Where(g => g.PermissionId == permissionWriteId)
                    .Select(g => g.RoleId);

                var losingSystemRoles = systemRoleIds.Intersect(currentRolesForWrite).Except(newRolesForWrite);
                if (losingSystemRoles.Any())
                    return Result.Failure<string>(PermissionErrors.SystemRoleCannotLoseWrite);
            }
        }

        // Mọi phép kiểm đã qua — TỪ ĐÂY mới chạm dữ liệu.
        var desiredPairs = entries
            .SelectMany(e => e.Value.Select(roleId => (RoleId: roleId, PermissionId: e.Key)))
            .ToHashSet();
        var currentPairSet = currentGrants.Select(g => (g.RoleId, g.PermissionId)).ToHashSet();

        var toRevoke = currentPairSet.Except(desiredPairs).ToList();
        var toGrant = desiredPairs.Except(currentPairSet).ToList();

        if (toRevoke.Count > 0)
        {
            var revokeSet = toRevoke.ToHashSet();
            var revokeRoleIds = toRevoke.Select(p => p.RoleId).Distinct().ToList();
            var rowsToRevoke = await db.RolePermissions
                .Where(rp => revokeRoleIds.Contains(rp.RoleId))
                .ToListAsync(ct);

            foreach (var row in rowsToRevoke.Where(row => revokeSet.Contains((row.RoleId, row.PermissionId))))
                row.IsDeleted = true;
        }

        foreach (var (roleId, permissionId) in toGrant)
        {
            var createResult = RolePermission.Create(roleId, permissionId);
            db.RolePermissions.Add(createResult.Value);
        }

        return Result.Success(ComputeVersion(desiredPairs.Select(p => (p.RoleId, p.PermissionId))));
    }

    // Tuần tự hoá kiểm-rồi-ghi của MỘT đơn vị — docs/wiki-core/be/06-concurrency-control.md §6.3 luật 3, §7.
    // Transaction chạy READ COMMITTED: không khoá thì hai lượt lưu cùng giữ một version đều đọc thấy trạng thái cũ, cùng
    // qua phép so, cùng commit — thay đổi của hai người trộn vào nhau, hoặc lượt sau vỡ unique index (23505 ⇒ 500) khi
    // cả hai cùng cấp một ô. Khoá PHẢI đứng trước mọi lệnh đọc dùng để so: lượt chờ khoá xong mới đọc, và mỗi câu lệnh
    // READ COMMITTED lấy ảnh chụp mới, nên nó thấy đúng ma trận lượt trước vừa commit ⇒ lệch version ⇒ 409.
    //
    // Khoá tư vấn cấp TRANSACTION (§7 bảng công cụ, hàng 2): khoá theo một khoá logic — "ma trận của đơn vị X" — không
    // gắn với dòng nào, nên không dính vào thứ tự khoá dòng của bất kỳ luồng nào khác (§7 quy tắc 1); tự nhả khi
    // transaction kết thúc, không có đường quên nhả. Khoá băm từ chuỗi bằng chính PostgreSQL (hashtextextended) — ổn
    // định giữa các tiến trình; hai đơn vị trùng băm chỉ phải chờ nhau, không sai dữ liệu.
    private async Task LockMatrixOfCurrentTenantAsync(CancellationToken ct)
    {
        // Ngoài transaction, khoá cấp transaction nhả ngay sau câu lệnh — bảo vệ biến mất trong im lặng. Lỗi lập trình:
        // command luôn chạy trong TransactionBehavior.
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "PermissionMatrixService.ReplaceMatrixAsync phải chạy trong transaction của người gọi (TransactionBehavior).");

        var tenantId = db.CurrentTenantId
            ?? throw new InvalidOperationException("Ghi ma trận phân quyền khi chưa có đơn vị hiện hành — lỗi lập trình.");

        var lockKey = $"core.permission_matrix:{tenantId}";

        // §7 quy tắc 3: luôn có thời hạn chờ — giá trị và lý do ở LockTimeout.
        await LockTimeout.SetForCurrentTransactionAsync(db, ct);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);
    }

    private Task<List<PermissionMatrixRoleDto>> GetRolesAsync(CancellationToken ct)
        => db.Roles
            .OrderBy(r => r.NormalizedName)
            .Select(r => new PermissionMatrixRoleDto(r.Id, r.Name!, r.IsSystem))
            .ToListAsync(ct);

    private sealed record PermissionRow(
        Guid Id, string Code, string ResourceKey, string ResourceNameKey, string NameKey, string Action,
        string? ModuleKey, int ResourceDisplayOrder);

    private Task<List<PermissionRow>> GetPermissionRowsAsync(CancellationToken ct) =>
        (from p in db.Permissions
         join res in db.PermissionResources on p.ResourceKey equals res.Key
         orderby res.DisplayOrder, p.DisplayOrder
         select new PermissionRow(p.Id, p.Code, p.ResourceKey, res.NameKey, p.NameKey, p.Action, res.ModuleKey, res.DisplayOrder))
        .ToListAsync(ct);

    private async Task<List<(Guid RoleId, Guid PermissionId)>> GetGrantsAsync(CancellationToken ct)
    {
        var rows = await db.RolePermissions.Select(rp => new { rp.RoleId, rp.PermissionId }).ToListAsync(ct);
        return rows.Select(x => (x.RoleId, x.PermissionId)).ToList();
    }

    private static Dictionary<Guid, IReadOnlyList<Guid>> GroupGrantsByPermission(IEnumerable<(Guid RoleId, Guid PermissionId)> grants)
        => grants
            .GroupBy(g => g.PermissionId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(x => x.RoleId).ToList());

    // Băm trên tập (vai trò, quyền) đã SẮP THỨ TỰ ỔN ĐỊNH — docs/contracts/permissions.md §3.3.
    // KHÔNG IgnoreQueryFilters: chỉ tính dòng chưa xoá mềm (mọi lời gọi ở trên đều qua db.RolePermissions
    // thường, đã có bộ lọc soft-delete + tenant mặc định).
    private static string ComputeVersion(IEnumerable<(Guid RoleId, Guid PermissionId)> pairs)
    {
        var ordered = pairs.OrderBy(p => p.RoleId).ThenBy(p => p.PermissionId);

        var sb = new StringBuilder();
        foreach (var (roleId, permissionId) in ordered)
            sb.Append(roleId).Append(':').Append(permissionId).Append(';');

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return "sha256:" + Convert.ToHexStringLower(hash)[..8];
    }
}
