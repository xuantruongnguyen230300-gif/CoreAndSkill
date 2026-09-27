using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Users;

// Năm luật bảo vệ tài khoản quản trị — docs/contracts/users.md §2. Ở ĐÚNG MỘT CHỖ trong
// Core.Application, trước khi chạm tầng ghi — rải ra từng handler là cách chắc chắn để một handler
// mới quên một luật. Composition nội bộ (như SessionDtoFactory), không phải seam: đăng ký Scoped,
// không interface, không hiện thực ở tầng khác.
internal sealed class UserPrivilegeGuard(
    IPermissionChecker permissionChecker,
    IRoleQueryService roleQueryService,
    IUserLookupService userLookup)
{
    // Luật 1 — người gọi chỉ được gán/gỡ một vai trò R nếu có ĐỦ mọi quyền mà R cấp. Áp cho MỌI roleId bị chạm (cả thêm
    // và gỡ). Tập quyền của người gọi tính MỘT lần, tập quyền của mọi vai trò tra bằng MỘT câu theo tập; phép so từng vai
    // trò chạy trên bộ nhớ (RoleEscalationCheck.Check) — số truy vấn không tăng theo số vai trò trong payload.
    public async Task<RoleEscalationCheck> LoadRoleEscalationCheckAsync(
        Guid callerId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
        => await LoadRoleEscalationCheckAsync(await permissionChecker.GetEffectivePermissionsAsync(callerId, ct), roleIds, ct);

    // Biến thể cho chỗ gọi đã có tập quyền của người gọi (handler tạo người dùng đọc nó để kiểm core.user.role.assign) —
    // không đọc lại lần hai.
    public async Task<RoleEscalationCheck> LoadRoleEscalationCheckAsync(
        IReadOnlySet<string> callerPermissions, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        var rolePermissions = roleIds.Count == 0
            ? new Dictionary<Guid, IReadOnlySet<string>>()
            : await permissionChecker.GetPermissionsForRolesAsync(roleIds, ct);

        return new RoleEscalationCheck(callerPermissions, rolePermissions);
    }

    // Luật 2 — không ai được gỡ khỏi CHÍNH MÌNH một vai trò is_system, kể cả khi luật 1 cho qua.
    public static Result EnsureNotRemovingOwnSystemRole(Guid callerId, Guid targetUserId, bool roleIsSystem)
        => targetUserId == callerId && roleIsSystem
            ? Result.Failure(UserErrors.SelfSystemRoleRemovalForbidden)
            : Result.Success();

    // Luật 3 — không tự khoá tài khoản của chính mình. Áp cho MỌI người dùng.
    public static Result EnsureNotLockingSelf(Guid callerId, Guid targetUserId)
        => targetUserId == callerId
            ? Result.Failure(UserErrors.CannotLockSelf)
            : Result.Success();

    // Luật 4 — chỉ người mang vai trò hệ thống mới khoá được tài khoản mang vai trò hệ thống.
    public async Task<Result> EnsureCanLockAsync(Guid callerId, Guid targetUserId, CancellationToken ct)
    {
        // Đích mang cờ bypass HOẶC là tài khoản vận hành hệ thống: luôn chặn, bất kể người gọi — kể cả người gọi cũng mang
        // cờ. Cả hai loại tài khoản không giữ vai trò is_system nào (quản trị đơn vị mặc định chỉ mang cờ), nên thiếu vế này
        // thì vế vai trò bên dưới cho qua mọi người có core.user.lock.
        if (await userLookup.HasPermissionBypassAsync(targetUserId, ct) || await IsSystemOperatorAsync(targetUserId, ct))
            return Result.Failure(UserErrors.SystemRoleLockForbidden);

        var targetHoldsSystemRole = await roleQueryService.UserHoldsAnySystemRoleAsync(targetUserId, ct);
        if (!targetHoldsSystemRole)
            return Result.Success();

        // Người gọi mang cờ bypass được tính như mang vai trò hệ thống — không có vế này, quản trị đơn vị (không giữ vai trò
        // nào) không khoá được tài khoản giữ vai trò is_system.
        var callerCountsAsSystemRoleHolder = await roleQueryService.UserHoldsAnySystemRoleAsync(callerId, ct)
            || await userLookup.HasPermissionBypassAsync(callerId, ct);
        return callerCountsAsSystemRoleHolder
            ? Result.Success()
            : Result.Failure(UserErrors.SystemRoleLockForbidden);
    }

    // Luật 5 — không nhắm vào tài khoản "cao hơn" người gọi, không nhắm vào chính mình. Ba vế về
    // đích dùng CHUNG một mã (§2 "Ba vế dùng chung một mã, có chủ đích").
    public async Task<Result> EnsureCanResetPasswordAsync(Guid callerId, Guid targetUserId, CancellationToken ct)
    {
        if (targetUserId == callerId)
            return Result.Failure(UserErrors.CannotResetOwnPassword);

        // Đích mang cờ bypass HOẶC là tài khoản vận hành hệ thống: chung một mã với vế tập quyền. Tập quyền của
        // tài khoản vận hành RỖNG nên vế tập quyền luôn cho qua; khôi phục nó chỉ đi đường lệnh chạy tay
        // (docs/contracts/tenants.md §4 "Ghi chú").
        if (await userLookup.HasPermissionBypassAsync(targetUserId, ct) || await IsSystemOperatorAsync(targetUserId, ct))
            return Result.Failure(UserErrors.ResetPasswordTargetForbidden);

        var callerPermissions = await permissionChecker.GetEffectivePermissionsAsync(callerId, ct);
        var targetPermissions = await permissionChecker.GetEffectivePermissionsAsync(targetUserId, ct);

        return targetPermissions.All(callerPermissions.Contains)
            ? Result.Success()
            : Result.Failure(UserErrors.ResetPasswordTargetForbidden);
    }

    private async Task<bool> IsSystemOperatorAsync(Guid userId, CancellationToken ct)
        => (await userLookup.FindByIdAsync(userId, ct))?.IsSystemOperator == true;
}

// Luật 1 đã nạp sẵn dữ liệu — xem UserPrivilegeGuard.LoadRoleEscalationCheckAsync. Vai trò không có trong dữ liệu đã nạp
// bị từ chối (đóng, không mở): seam hứa trả mọi id được hỏi, nên thiếu một id là dấu hiệu chỗ gọi quên đưa nó vào.
internal sealed class RoleEscalationCheck(
    IReadOnlySet<string> callerPermissions, IReadOnlyDictionary<Guid, IReadOnlySet<string>> rolePermissions)
{
    public Result Check(Guid roleId)
        => rolePermissions.TryGetValue(roleId, out var granted) && granted.All(callerPermissions.Contains)
            ? Result.Success()
            : Result.Failure(UserErrors.RoleEscalationForbidden);
}
