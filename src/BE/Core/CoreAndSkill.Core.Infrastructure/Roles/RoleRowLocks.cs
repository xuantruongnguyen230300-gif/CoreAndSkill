using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Roles;

// Khoá dòng core.app_role cho các đường tranh chấp với lệnh xoá vai trò — nợ E17 ở docs/DEBT.md; công cụ và ba quy tắc khoá
// ở docs/wiki-core/be/06-concurrency-control.md §7.
//   • Xoá vai trò: RoleAdminService.DeleteAsync.
//   • Gán vai trò cho người dùng: UserAdminService.AssignRolesAsync, GrantInitialRolesAsync.
//   • Ghi ma trận quyền: PermissionMatrixService.ReplaceMatrixAsync.
//
// Vì sao phải khoá: app_user_role.role_id và role_permission.role_id đều là ON DELETE CASCADE. Không khoá thì:
//   1. xoá commit giữa lần kiểm "vai trò tồn tại" và câu INSERT của lượt gán / lượt ghi ma trận ⇒ khoá ngoại ném 23503 ⇒ 500
//      thay vì 422;
//   2. gán commit giữa lần đếm người mang và câu DELETE của lượt xoá ⇒ cascade lặng lẽ gỡ vai trò vừa gán, luật IN_USE bị
//      vượt, người gán vẫn nhận 200;
//   3. xoá commit giữa lần đọc ma trận và câu UPDATE thu hồi của lượt ghi ma trận ⇒ câu UPDATE chạm 0 dòng (cascade đã xoá
//      dòng đó) ⇒ lỗi đồng thời chung thay vì mã của hợp đồng ma trận.
//
// Các chế độ khoá khai CÙNG một chỗ vì chúng chỉ đúng khi đi thành cặp — chúng phải xung đột nhau:
//   • Xoá: FOR UPDATE — chế độ duy nhất xung đột với FOR KEY SHARE. FOR NO KEY UPDATE thì không, và cặp này mất tác dụng.
//   • Gán, ghi ma trận: FOR KEY SHARE — đúng chế độ khoá mà PostgreSQL tự lấy khi kiểm khoá ngoại lúc INSERT, nhưng lấy SỚM
//     hơn: trước INSERT, để lượt ghi đọc được "vai trò đã mất" thành 422 thay vì gặp 23503. Đây là chế độ yếu nhất còn chặn
//     được lệnh xoá: không chặn lượt đổi tên vai trò (FOR NO KEY UPDATE), không chặn các lượt FOR KEY SHARE khác.
//
// Hai lượt xếp hàng, lượt sau nhận đúng mã nghiệp vụ (READ COMMITTED — mỗi câu lấy ảnh chụp mới):
//   • Bên FOR KEY SHARE giữ khoá trước ⇒ lượt xoá chờ ở FOR UPDATE; bên kia commit xong thì lượt xoá lấy được khoá, câu ĐẾM
//     chạy SAU khoá thấy dòng vừa gán ⇒ 422 CORE.ROLE.IN_USE.
//   • Xoá giữ khoá trước ⇒ bên kia chờ ở FOR KEY SHARE; lượt xoá commit xong thì dòng không còn, PostgreSQL bỏ nó khỏi kết
//     quả ⇒ bên kia trả mã "vai trò không tồn tại" của hợp đồng nó (hoặc, với ma trận, lệch version — mọi câu đọc chạy SAU khoá).
//
// Thứ tự khoá: lượt xoá khoá đúng MỘT dòng vai trò. Lượt nào khoá nhiều dòng thì khoá theo thứ tự id (ORDER BY id) — quy tắc 1
// của §7. Các khoá FOR KEY SHARE không xung đột nhau, nên lượt gán và lượt ghi ma trận không chờ nhau ở đây.
//
// Luật M6 (docs/RULES.md §9, docs/wiki-core/be/17-multi-tenant.md §8): Database.SqlQuery đi thẳng xuống database, không
// bộ lọc toàn cục nào phủ — mệnh đề tenant_id viết trong chính câu, tham số hoá. Nó cũng ràng PHẠM VI CỦA KHOÁ: không dòng
// của đơn vị khác nào bị giữ khoá dù id trùng.
internal static class RoleRowLocks
{
    internal sealed record LockedRole(bool IsSystem);

    // Khoá dòng vai trò để xoá. null ⇒ không có vai trò đó trong đơn vị hiện hành (hoặc một lượt xoá khác vừa commit).
    // Mọi phép kiểm dùng để quyết định xoá — hệ thống hay không, còn ai mang — phải chạy SAU câu này.
    public static async Task<LockedRole?> LockForDeleteAsync(CoreDbContext db, Guid roleId, CancellationToken ct)
    {
        var tenantId = RequireTransactionAndTenant(db);

        await LockTimeout.SetForCurrentTransactionAsync(db, ct);

        var rows = await db.Database
            .SqlQuery<bool>(
                $"""
                SELECT is_system AS "Value" FROM core.app_role
                WHERE id = {roleId} AND tenant_id = {tenantId}
                FOR UPDATE
                """)
            .ToListAsync(ct);

        return rows.Count == 0 ? null : new LockedRole(rows[0]);
    }

    // Khoá các dòng vai trò sắp được GÁN, theo thứ tự id. Trả tập id còn tồn tại trong đơn vị hiện hành — id vắng mặt là
    // vai trò không có, hoặc vừa bị một lượt xoá commit trong lúc câu này chờ khoá.
    public static async Task<IReadOnlySet<Guid>> LockForAssignAsync(
        CoreDbContext db, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        var wanted = roleIds.Distinct().ToArray();
        if (wanted.Length == 0)
            return new HashSet<Guid>();

        var tenantId = RequireTransactionAndTenant(db);

        await LockTimeout.SetForCurrentTransactionAsync(db, ct);

        var locked = await db.Database
            .SqlQuery<Guid>(
                $"""
                SELECT id AS "Value" FROM core.app_role
                WHERE tenant_id = {tenantId} AND id = ANY({wanted})
                ORDER BY id
                FOR KEY SHARE
                """)
            .ToListAsync(ct);

        return locked.ToHashSet();
    }

    // Khoá MỌI dòng vai trò của đơn vị hiện hành cho một lượt ghi ma trận quyền, theo thứ tự id. Trả tập id đang tồn tại.
    //
    // Mọi vai trò, không chỉ vai trò được cấp thêm: PUT ma trận là THAY THẾ TOÀN BỘ (docs/contracts/permissions.md §6), nên
    // lượt đó quyết tập quyền của mọi vai trò trong đơn vị — vai trò vắng mặt trong payload mất hết quyền. Khoá chỉ vai trò
    // được cấp thêm thì còn hở chiều 3 ở đầu file: vai trò chỉ bị THU HỒI bị xoá chen giữa lần đọc ma trận và câu UPDATE.
    // Khoá một lần trước mọi câu đọc thì cả tập vai trò đứng yên suốt lượt ghi: không phải đoán trước vai trò nào sẽ đổi.
    public static async Task<IReadOnlySet<Guid>> LockAllForMatrixWriteAsync(CoreDbContext db, CancellationToken ct)
    {
        var tenantId = RequireTransactionAndTenant(db);

        await LockTimeout.SetForCurrentTransactionAsync(db, ct);

        var locked = await db.Database
            .SqlQuery<Guid>(
                $"""
                SELECT id AS "Value" FROM core.app_role
                WHERE tenant_id = {tenantId}
                ORDER BY id
                FOR KEY SHARE
                """)
            .ToListAsync(ct);

        return locked.ToHashSet();
    }

    // Ngoài transaction, khoá dòng nhả ngay sau câu lệnh — bảo vệ biến mất trong im lặng. Lỗi lập trình: command luôn chạy
    // trong TransactionBehavior. Kiểm TRƯỚC mọi câu SQL.
    private static Guid RequireTransactionAndTenant(CoreDbContext db)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "Khoá dòng core.app_role phải chạy trong transaction của người gọi (TransactionBehavior).");

        return db.CurrentTenantId
            ?? throw new InvalidOperationException("Khoá dòng core.app_role khi chưa có đơn vị hiện hành — lỗi lập trình.");
    }
}
