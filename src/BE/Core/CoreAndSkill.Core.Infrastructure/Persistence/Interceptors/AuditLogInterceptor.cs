using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Audit;
using CoreAndSkill.Core.Domain.Permissions;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;

// Đường GHI nhật ký kiểm toán cho các thay đổi phát hiện được TỪ CHÍNH ChangeTracker — gắn vào tầng
// dữ liệu, không phụ thuộc handler tự nhớ gọi (docs/wiki-core/be/trien-khai/04-b3-van-hanh.md §3).
// Phủ hai nhóm đầu của docs/wiki-core/be/10-data-retention.md §5.4: "thay đổi phân quyền và vai
// trò" (kể cả gán / gỡ vai trò của người dùng — luật S21), "tạo, khoá, mở khoá tài khoản; đổi mật
// khẩu". Các thao tác XUYÊN ĐƠN VỊ của khu quản trị hệ
// thống (tạo đơn vị, ngưng/bật lại, khôi phục quản trị — docs/contracts/tenants.md §5) ghi audit
// TƯỜNG MINH trong TenantProvisioningService — hai dòng khác tenant không suy được từ một lượt
// ChangeTracker duy nhất (xem comment trong AuditLog.cs).
//
// PHẢI đăng ký SAU TenantAssignmentInterceptor trong AddInterceptors(...) — đọc TenantId từ CHÍNH
// entity (đã được gán đúng bởi interceptor kia trong CÙNG lượt SavingChanges), không đọc lại
// ITenantContext (tránh lệch nếu ambient đổi giữa chừng).
//
// Luật M14 (docs/database/schema-core.md §9.4 điểm 4): dòng ghi ở đơn vị đích do một người thuộc đơn vị KHÁC thực hiện
// mang actor_tenant_id — đọc từ CrossTenantActorScope (nơi mở phạm vi đơn vị đích đánh dấu, giá trị là claim của phiếu),
// không đọc ITenantContext: trong phạm vi đó nó đã là đơn vị đích.
public sealed class AuditLogInterceptor(
    TimeProvider timeProvider,
    ICurrentUser currentUser,
    IClientAddressAccessor clientAddress,
    PasswordRehashScope rehashScope,
    CrossTenantActorScope crossTenantActor)
    : SaveChangesInterceptor
{
    // Nhánh đồng bộ chạy StageAsync với async: false — mọi lệnh bên trong khi đó hoàn tất đồng bộ (ToList, không
    // ToListAsync), nên GetResult() không chặn luồng nào. Cùng khuôn EF dùng cho cặp Execute/ExecuteAsync.
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StageAsync(eventData.Context, async: false, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await StageAsync(eventData.Context, async: true, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task StageAsync(DbContext? context, bool async, CancellationToken ct)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();
        var actorUserId = currentUser.UserId;
        var actorDisplay = currentUser.UserName ?? SystemActor.UserName;
        var traceId = System.Diagnostics.Activity.Current?.TraceId.ToHexString();
        var ip = clientAddress.RemoteIpAddress;
        var actorTenantId = crossTenantActor.ActorTenantId;

        // Vật liệu hoá TRƯỚC khi Add — không sửa tracker trong lúc đang enumerate nó.
        var userEntries = context.ChangeTracker.Entries<AppUser>().ToList();
        var roleEntries = context.ChangeTracker.Entries<AppRole>().ToList();
        var rolePermissionEntries = context.ChangeTracker.Entries<RolePermission>().ToList();
        var userRoleEntries = context.ChangeTracker.Entries<AppUserRole>().ToList();

        // Phân loại ô ĐẦU TIÊN, trước mọi lần Add: ClassifyCell ném khi gặp chuyển trạng thái bị cấm (luật S18), và một lượt
        // lưu hỏng không được để lại dòng nhật ký nào đã dàn dựng trong bộ theo dõi.
        var rolesDeletedInThisSave = roleEntries
            .Where(e => e.State == EntityState.Deleted)
            .Select(e => e.Entity.Id)
            .ToHashSet();
        var changedCells = rolePermissionEntries
            .Select(e => (Cell: e.Entity, Change: ClassifyCell(e, rolesDeletedInThisSave)))
            .Where(x => x.Change != CellChange.None)
            .ToList();

        foreach (var entry in userEntries)
        {
            var user = entry.Entity;

            if (entry.State == EntityState.Added)
            {
                Add(context, user.TenantId, AuditActionCodes.UserCreate, "core.user", user.Id.ToString(), user.FullName,
                    now, actorUserId, actorTenantId, actorDisplay, traceId, ip);
                continue;
            }

            if (entry.State != EntityState.Modified)
                continue;

            if (ValueChanged(entry, nameof(AppUser.LockedByAdmin)))
            {
                var action = user.LockedByAdmin ? AuditActionCodes.UserLock : AuditActionCodes.UserUnlock;
                Add(context, user.TenantId, action, "core.user", user.Id.ToString(), user.FullName,
                    now, actorUserId, actorTenantId, actorDisplay, traceId, ip);
            }

            // Identity băm lại mật khẩu lúc đăng nhập (SuccessRehashNeeded) — cùng mật khẩu, chỉ đổi dạng băm — không
            // phải một lần đổi mật khẩu. ChangeTracker không phân biệt được (SecurityStamp cũng xoay), nên nơi gọi
            // đánh dấu bằng PasswordRehashScope.
            if (ValueChanged(entry, nameof(AppUser.PasswordHash)) && !rehashScope.IsActive)
            {
                Add(context, user.TenantId, AuditActionCodes.UserPasswordChange, "core.user", user.Id.ToString(), user.FullName,
                    now, actorUserId, actorTenantId, actorDisplay, traceId, ip);
            }
        }

        foreach (var entry in roleEntries)
        {
            var role = entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    Add(context, role.TenantId, AuditActionCodes.RoleCreate, "core.role", role.Id.ToString(), role.Name,
                        now, actorUserId, actorTenantId, actorDisplay, traceId, ip);
                    break;

                case EntityState.Modified when ValueChanged(entry, nameof(AppRole.Name)):
                    Add(context, role.TenantId, AuditActionCodes.RoleRename, "core.role", role.Id.ToString(), role.Name,
                        now, actorUserId, actorTenantId, actorDisplay, traceId, ip);
                    break;

                case EntityState.Deleted:
                    Add(context, role.TenantId, AuditActionCodes.RoleDelete, "core.role", role.Id.ToString(), role.Name,
                        now, actorUserId, actorTenantId, actorDisplay, traceId, ip);
                    break;
            }
        }

        await StageMatrixUpdatesAsync(
            context, changedCells, roleEntries, now, actorUserId, actorTenantId, actorDisplay, traceId, ip, async, ct);

        // Dòng core.user.create (vòng lặp người dùng ở trên) và dòng này là HAI dòng khi tạo người dùng kèm vai trò — không
        // gộp (docs/contracts/users.md §7 mục "Nhật ký kiểm toán" điểm 1).
        var userRoleChanges = await CollectUserRoleChangesAsync(
            context, userRoleEntries, roleEntries, userEntries, rolesDeletedInThisSave, async, ct);

        foreach (var change in userRoleChanges)
        {
            Add(context, change.TenantId, AuditActionCodes.UserRoleAssign, "core.user", change.UserId.ToString(), change.UserDisplay,
                now, actorUserId, actorTenantId, actorDisplay, traceId, ip, afterValue: change.AfterValue);
        }
    }

    // Luật S17 — docs/contracts/permissions.md §6 mục "Nhật ký kiểm toán" (định nghĩa gốc của hình dạng dòng),
    // docs/adr/0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md. Một dòng cho MỖI vai trò có ô đổi trong lượt
    // này — không phải một dòng cho mỗi ô (bảng phình vô ích, docs/wiki-core/be/10-data-retention.md §5.4), không phải
    // một dòng cho cả request (đường tạo đơn vị không có "một request" nào để gom). Vai trò không có ô nào đổi thì không
    // có dòng nào.
    private static async Task StageMatrixUpdatesAsync(
        DbContext context,
        List<(RolePermission Cell, CellChange Change)> changedCells,
        List<EntityEntry<AppRole>> roleEntries,
        DateTimeOffset now,
        Guid? actorUserId,
        Guid? actorTenantId,
        string actorDisplay,
        string? traceId,
        System.Net.IPAddress? ip,
        bool async,
        CancellationToken ct)
    {
        if (changedCells.Count == 0)
            return;

        var permissionCodes = await ResolvePermissionCodesAsync(
            context, changedCells.Select(x => x.Cell.PermissionId).ToHashSet(), async, ct);
        var roleNames = await ResolveRoleNamesAsync(
            context, roleEntries, changedCells.Select(x => x.Cell.RoleId).ToHashSet(), async, ct);

        foreach (var role in changedCells.GroupBy(x => (x.Cell.TenantId, x.Cell.RoleId)))
        {
            var diff = new MatrixDiff(
                Granted: CodesOf(role, CellChange.Granted, permissionCodes),
                Revoked: CodesOf(role, CellChange.Revoked, permissionCodes));

            Add(context, role.Key.TenantId, AuditActionCodes.PermissionMatrixUpdate, "core.role", role.Key.RoleId.ToString(),
                roleNames.GetValueOrDefault(role.Key.RoleId), now, actorUserId, actorTenantId, actorDisplay, traceId, ip,
                afterValue: JsonSerializer.Serialize(diff, MatrixDiffJson));
        }
    }

    // "Ô đổi" theo GIÁ TRỊ (docs/contracts/permissions.md §6 luật 2): dòng mới thêm là cấp; is_deleted đổi từ false sang
    // true là thu. Cờ IsModified một mình không đủ — DbContext.Update(entity) bật cờ cho mọi cột dù giá trị giữ nguyên.
    //
    // Luật S18 — docs/adr/0054-o-ma-tran-chi-doi-bang-them-dong-va-xoa-mem.md: hai chuyển trạng thái còn lại bị CẤM và ném
    // ngay tại đây, lượt SaveChanges hỏng. Khôi phục (is_deleted true → false) là một lần cấp không để lại dấu; xoá cứng
    // qua ChangeTracker là một lần thu — hoặc một lần xoá lịch sử — không để lại dấu. Ngoại lệ có tên duy nhất: cascade của
    // một lần xoá vai trò mà EF đang theo dõi, tức vai trò chủ cũng Deleted trong CÙNG lượt.
    private static CellChange ClassifyCell(EntityEntry<RolePermission> entry, HashSet<Guid> rolesDeletedInThisSave)
    {
        switch (entry.State)
        {
            case EntityState.Added when !entry.Entity.IsDeleted:
                return CellChange.Granted;

            case EntityState.Modified when ValueChanged(entry, nameof(RolePermission.IsDeleted)):
                return entry.Entity.IsDeleted
                    ? CellChange.Revoked
                    : throw ForbiddenCellTransition(entry.Entity, "khôi phục một ô đã xoá mềm (is_deleted true → false)");

            case EntityState.Deleted when rolesDeletedInThisSave.Contains(entry.Entity.RoleId):
                return CellChange.None; // cascade của lần xoá vai trò — dòng core.role.delete đóng chuỗi nhật ký của vai trò

            case EntityState.Deleted:
                throw ForbiddenCellTransition(entry.Entity, "xoá cứng một ô qua ChangeTracker");

            default:
                return CellChange.None;
        }
    }

    private static InvalidOperationException ForbiddenCellTransition(RolePermission cell, string transition)
        => new($"Luật S18 (docs/adr/0054-o-ma-tran-chi-doi-bang-them-dong-va-xoa-mem.md): ô ma trận chỉ đổi bằng thêm dòng " +
               $"(cấp) và xoá mềm (thu) — lượt lưu bị từ chối vì {transition}. Vai trò {cell.RoleId}, quyền {cell.PermissionId}, " +
               $"dòng {cell.Id}.");

    // Mã khoá, sắp theo thứ tự chữ (ordinal — mã quyền là ASCII, docs/database/schema-core.md §5.2), không lặp.
    private static List<string> CodesOf(
        IEnumerable<(RolePermission Cell, CellChange Change)> cells, CellChange change, Dictionary<Guid, string> codes)
        => cells
            .Where(x => x.Change == change)
            .Select(x => codes.TryGetValue(x.Cell.PermissionId, out var code) ? code : x.Cell.PermissionId.ToString())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    // Tra mã khoá trong CÙNG lượt SaveChanges: lấy từ bộ theo dõi trước, chỉ hỏi database phần còn thiếu. Bỏ bộ lọc xoá
    // mềm — thu một ô trỏ tới quyền đã xoá mềm (docs/contracts/permissions.md §8.2) vẫn phải ra mã khoá. Permission không
    // mang tenant nên không có bộ lọc đơn vị nào để bỏ.
    private static async Task<Dictionary<Guid, string>> ResolvePermissionCodesAsync(
        DbContext context, HashSet<Guid> permissionIds, bool async, CancellationToken ct)
    {
        var codes = context.ChangeTracker.Entries<Permission>()
            .Where(e => permissionIds.Contains(e.Entity.Id))
            .ToDictionary(e => e.Entity.Id, e => e.Entity.Code);

        var missing = permissionIds.Where(id => !codes.ContainsKey(id)).ToList();
        if (missing.Count == 0)
            return codes;

        var query = context.Set<Permission>()
            .IgnoreQueryFilters([CoreQueryFilters.SoftDeleteKey])
            .AsNoTracking()
            .Where(p => missing.Contains(p.Id))
            .Select(p => new { p.Id, p.Code });

        foreach (var row in async ? await query.ToListAsync(ct) : query.ToList())
            codes[row.Id] = row.Code;

        return codes;
    }

    // Tên vai trò TẠI THỜI ĐIỂM GHI (docs/wiki-core/be/10-data-retention.md §5.2). Vai trò vừa tạo trong cùng lượt (seed
    // khi tạo đơn vị) chỉ có trong bộ theo dõi. Phần còn lại hỏi database qua bộ lọc thường: ô ma trận mang TenantId do
    // TenantAssignmentInterceptor gán từ chính ngữ cảnh đơn vị đang mở, nên vai trò của nó nằm trong đơn vị đó.
    private static async Task<Dictionary<Guid, string?>> ResolveRoleNamesAsync(
        DbContext context, List<EntityEntry<AppRole>> roleEntries, HashSet<Guid> roleIds, bool async, CancellationToken ct)
    {
        var names = roleEntries
            .Where(e => roleIds.Contains(e.Entity.Id))
            .ToDictionary(e => e.Entity.Id, e => e.Entity.Name);

        var missing = roleIds.Where(id => !names.ContainsKey(id)).ToList();
        if (missing.Count == 0)
            return names;

        var query = context.Set<AppRole>()
            .AsNoTracking()
            .Where(r => missing.Contains(r.Id))
            .Select(r => new { r.Id, r.Name });

        foreach (var row in async ? await query.ToListAsync(ct) : query.ToList())
            names[row.Id] = row.Name;

        return names;
    }

    // Luật S21 — docs/contracts/users.md §7 mục "Nhật ký kiểm toán" (định nghĩa gốc của hình dạng dòng),
    // docs/adr/0083-doi-vai-tro-nguoi-dung-luon-vao-nhat-ky-kiem-toan.md. Bảng nối core.app_user_role xoá CỨNG
    // (docs/wiki-core/be/10-data-retention.md §2.6), nên lịch sử gán vai trò chỉ còn ở nhật ký kiểm toán.
    //
    // MỘT dòng cho mỗi người dùng có dòng app_user_role thêm hoặc xoá trong lượt này (không một dòng mỗi cặp — cùng lý do
    // StageMatrixUpdatesAsync). Áp cho MỌI đường ghi qua ChangeTracker, không riêng PUT /users/{id}/roles.
    //
    // Cascade của một lần xoá vai trò mà EF đang theo dõi (vai trò chủ cũng Deleted trong CÙNG lượt) KHÔNG sinh dòng —
    // dòng core.role.delete đóng chuỗi, cùng ngoại lệ với ô ma trận (ClassifyCell). Không loại thì nhật ký phụ thuộc vào
    // việc các dòng app_user_role có tình cờ được nạp vào bộ theo dõi hay không: cascade ở database không đi qua đây.
    private static async Task<IReadOnlyList<UserRoleChange>> CollectUserRoleChangesAsync(
        DbContext context,
        List<EntityEntry<AppUserRole>> userRoleEntries,
        List<EntityEntry<AppRole>> roleEntries,
        List<EntityEntry<AppUser>> userEntries,
        HashSet<Guid> rolesDeletedInThisSave,
        bool async,
        CancellationToken ct)
    {
        var changed = userRoleEntries
            .Select(e => (Row: e.Entity, Change: ClassifyUserRole(e, rolesDeletedInThisSave)))
            .Where(x => x.Change != CellChange.None)
            .ToList();

        if (changed.Count == 0)
            return [];

        var roleNames = await ResolveRoleNamesAsync(
            context, roleEntries, changed.Select(x => x.Row.RoleId).ToHashSet(), async, ct);
        var userDisplays = await ResolveUserDisplaysAsync(
            context, userEntries, changed.Select(x => x.Row.UserId).ToHashSet(), async, ct);

        return changed
            .GroupBy(x => (x.Row.TenantId, x.Row.UserId))
            .Select(user => new UserRoleChange(
                user.Key.TenantId,
                user.Key.UserId,
                userDisplays.GetValueOrDefault(user.Key.UserId),
                JsonSerializer.Serialize(
                    new RoleAssignmentDiff(
                        Granted: RolesOf(user, CellChange.Granted, roleNames),
                        Revoked: RolesOf(user, CellChange.Revoked, roleNames)),
                    MatrixDiffJson)))
            .ToList();
    }

    // Bảng nối không có cột nào sửa được (khoá chính là cả hai cột), nên chỉ Added và Deleted mang nghĩa.
    private static CellChange ClassifyUserRole(EntityEntry<AppUserRole> entry, HashSet<Guid> rolesDeletedInThisSave)
        => entry.State switch
        {
            EntityState.Added => CellChange.Granted,
            EntityState.Deleted when rolesDeletedInThisSave.Contains(entry.Entity.RoleId) => CellChange.None,
            EntityState.Deleted => CellChange.Revoked,
            _ => CellChange.None,
        };

    // Phần tử là cặp { id, name } — name là tên TẠI THỜI ĐIỂM GHI, null khi không tra được. KHÔNG thay tên bằng id: một danh
    // sách trộn hai kiểu thì người đọc không phân biệt được "vai trò tên là chuỗi này" với "không biết tên" (ADR-0083).
    // Sắp theo name (ordinal — StringComparer.Ordinal đặt null trước mọi chuỗi) rồi theo id ở dạng chuỗi (ordinal): hai
    // vai trò cùng không tra được tên vẫn ra một thứ tự xác định.
    private static List<RoleRef> RolesOf(
        IEnumerable<(AppUserRole Row, CellChange Change)> rows, CellChange change, Dictionary<Guid, string?> names)
        => rows
            .Where(x => x.Change == change)
            .Select(x => x.Row.RoleId)
            .Distinct()
            .Select(id => new RoleRef(id, names.GetValueOrDefault(id)))
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Id.ToString(), StringComparer.Ordinal)
            .ToList();

    // Họ tên người dùng TẠI THỜI ĐIỂM GHI (docs/wiki-core/be/10-data-retention.md §5.2) — cùng giá trị target_display của
    // các dòng core.user.* khác. Bộ theo dõi trước, thiếu mới hỏi database qua bộ lọc thường (người dùng của đơn vị đang
    // mở — dòng app_user_role mang TenantId của chính ngữ cảnh đó).
    private static async Task<Dictionary<Guid, string?>> ResolveUserDisplaysAsync(
        DbContext context, List<EntityEntry<AppUser>> userEntries, HashSet<Guid> userIds, bool async, CancellationToken ct)
    {
        var displays = userEntries
            .Where(e => userIds.Contains(e.Entity.Id))
            .ToDictionary(e => e.Entity.Id, e => (string?)e.Entity.FullName);

        var missing = userIds.Where(id => !displays.ContainsKey(id)).ToList();
        if (missing.Count == 0)
            return displays;

        var query = context.Set<AppUser>()
            .AsNoTracking()
            .Where(u => missing.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName });

        foreach (var row in async ? await query.ToListAsync(ct) : query.ToList())
            displays[row.Id] = row.FullName;

        return displays;
    }

    private sealed record UserRoleChange(Guid TenantId, Guid UserId, string? UserDisplay, string AfterValue);

    // Hình dạng after_value của dòng core.user.role_assign — docs/contracts/users.md §7. Danh sách rỗng vẫn khai.
    private sealed record RoleAssignmentDiff(IReadOnlyList<RoleRef> Granted, IReadOnlyList<RoleRef> Revoked);

    private sealed record RoleRef(Guid Id, string? Name);

    private enum CellChange
    {
        None,
        Granted,
        Revoked,
    }

    // Hình dạng after_value — docs/contracts/permissions.md §6. Danh sách rỗng vẫn khai.
    private sealed record MatrixDiff(IReadOnlyList<string> Granted, IReadOnlyList<string> Revoked);

    private static readonly JsonSerializerOptions MatrixDiffJson = new(JsonSerializerDefaults.Web);

    // "Đã sửa" theo GIÁ TRỊ, không theo cờ: UserStore/RoleStore.UpdateAsync của Identity gọi DbContext.Update(entity),
    // đánh dấu MỌI cột là đã sửa — kể cả cột không đổi. Đọc cờ IsModified thì mỗi lần Identity lưu một tài khoản (đăng
    // nhập sai, đổi stamp…) đều sinh dòng "mở khoá" và "đổi mật khẩu" giả.
    private static bool ValueChanged(EntityEntry entry, string propertyName)
    {
        var property = entry.Property(propertyName);
        return property.IsModified && !Equals(property.OriginalValue, property.CurrentValue);
    }

    private static void Add(
        DbContext context,
        Guid tenantId,
        string actionCode,
        string targetType,
        string targetId,
        string? targetDisplay,
        DateTimeOffset occurredAt,
        Guid? actorUserId,
        Guid? actorTenantId,
        string actorDisplay,
        string? traceId,
        System.Net.IPAddress? ip,
        string? afterValue = null)
    {
        // Chưa có đơn vị hợp lệ (Guid.Empty) — không ghi. Cùng nguyên tắc M8: không tự bịa tenant.
        if (tenantId == Guid.Empty)
            return;

        // M14: dấu xuyên đơn vị chỉ khi người thực hiện thuộc đơn vị KHÁC đơn vị của dòng (schema-core.md §9.4 cột actor_tenant_id).
        var record = AuditLog.Record(
            tenantId, occurredAt, actorUserId, actorDisplay, actionCode, targetType, targetId, targetDisplay,
            actorTenantId: actorTenantId is { } actor && actor != tenantId ? actor : null,
            afterValue: afterValue, ipAddress: ip, traceId: traceId);

        context.Set<AuditLog>().Add(record.Value);
    }
}
