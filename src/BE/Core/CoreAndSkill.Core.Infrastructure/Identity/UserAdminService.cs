using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Hiện thực IUserAdminService — docs/contracts/users.md. Luật nghiệp vụ phụ thuộc dữ liệu chéo
// (leo thang đặc quyền, tự khoá mình…) đã được UserPrivilegeGuard duyệt Ở HANDLER trước khi tới
// đây (docs/contracts/users.md §2) — service này chỉ còn: trùng unique cấp DỮ LIỆU (email khi sửa),
// vai trò được gán đã bị xoá đồng thời (kiểm lại dưới khoá dòng — RoleRowLocks), thao tác ghi qua UserManager, và dịch
// lỗi Identity.
internal sealed class UserAdminService(
    UserManager<AppUser> userManager,
    CoreDbContext db,
    TimeProvider timeProvider,
    ICurrentUser currentUser)
    : IUserAdminService
{
    // Id các tài khoản CreateAsync của chính instance này đã tạo thành công — căn cứ duy nhất của GrantInitialRolesAsync.
    // Service là Scoped (một instance cho một request), nên tập này sống đúng bằng đơn vị công việc. Không dựa vào
    // ChangeTracker: sau SaveChanges của UserStore.CreateAsync, tài khoản vừa tạo nằm ở trạng thái Unchanged — y hệt một
    // tài khoản cũ vừa được đọc có theo dõi (FindByIdAsync), nên "có trong bộ theo dõi" không phân biệt được hai ca.
    private readonly HashSet<Guid> _createdByThisService = [];

    public async Task<Result<Guid>> CreateAsync(CreateUserInput input, CancellationToken ct)
    {
        var user = AppUser.NewAccount(input.UserName, input.Email, input.FullName);

        var identityResult = await userManager.CreateAsync(user, input.TempPassword);
        if (!identityResult.Succeeded)
            return Result.Failure<Guid>(MapFailure(identityResult.Errors, user, UserErrors.CreateFailed));

        _createdByThisService.Add(user.Id);
        return Result.Success(user.Id);
    }

    public async Task<Result> UpdateAsync(Guid userId, UpdateUserInput input, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var normalizedEmail = input.Email.Trim().ToUpperInvariant();
        var emailTakenByAnother = await db.Users
            .AnyAsync(u => u.Id != userId && u.NormalizedEmail == normalizedEmail, ct);
        if (emailTakenByAnother)
            return Result.Failure(UserErrors.EmailDuplicated.WithParams(("Email", input.Email)));

        if (!IdentityConcurrency.VersionMatches(user, input.Version))
            return Result.Failure(CommonErrors.ConcurrencyConflict);

        user.Email = input.Email;
        user.ChangeFullName(input.FullName);

        var identityResult = await userManager.UpdateAsync(user);
        if (!identityResult.Succeeded)
        {
            var conflict = IdentityConcurrency.DetectConflict(identityResult);
            return conflict is not null
                ? Result.Failure(conflict)
                : Result.Failure(MapFailure(identityResult.Errors, user, UserErrors.UpdateFailed));
        }

        return Result.Success();
    }

    public Task<Result> LockAsync(Guid userId, string? version, CancellationToken ct)
        => SetLockoutAsync(userId, version, locked: true, ct);

    public Task<Result> UnlockAsync(Guid userId, string? version, CancellationToken ct)
        => SetLockoutAsync(userId, version, locked: false, ct);

    public async Task<Result> ResetPasswordAsync(Guid userId, string tempPassword, string? version, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (!IdentityConcurrency.VersionMatches(user, version))
            return Result.Failure(CommonErrors.ConcurrencyConflict);

        user.RequirePasswordChange();

        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            var conflict = IdentityConcurrency.DetectConflict(removeResult);
            return Result.Failure(conflict ?? UserErrors.ResetPasswordFailed);
        }

        var addResult = await userManager.AddPasswordAsync(user, tempPassword);
        if (!addResult.Succeeded)
            return Result.Failure(MapFailure(addResult.Errors, user, UserErrors.ResetPasswordFailed));

        // Mọi phiên đang mở của tài khoản đích bị chấm dứt ở request kế tiếp — docs/contracts/users.md §9. Hỏng thì trả thất
        // bại: trả thành công thì TransactionBehavior lưu tiếp sau một lượt lưu hỏng (ADR-0092 điều kiện a).
        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
            return Result.Failure(IdentityConcurrency.DetectConflict(stampResult) ?? UserErrors.ResetPasswordFailed);

        return Result.Success();
    }

    // docs/contracts/users.md §7 mục "Ghi chú — đồng thời", docs/adr/0082-gan-vai-tro-dung-token-cua-tai-khoan.md.
    //
    // Phép so-và-đổi token là câu ĐẦU TIÊN chạm dữ liệu — chỉ câu đặt thời hạn chờ khoá (LockTimeout) đứng trước nó — trước cả
    // lần đọc tập vai trò hiện có, nên nó chạy trước mọi thay đổi app_user_role bất kể tập đích có khác tập hiện có hay không
    // (card §7 ràng buộc 3: PUT không đổi vai trò nào vẫn so version; ở đây nó còn đổi token — card cho phép). Câu UPDATE giữ
    // khoá dòng tài khoản tới hết transaction: PUT thứ hai chờ ở chính câu đó, tối đa bằng thời hạn LockTimeout, rồi
    // PostgreSQL đánh giá lại WHERE trên bản dòng vừa commit — token đã đổi ⇒ 0 dòng ⇒ 409. Lượt sau
    // không bao giờ tới được lệnh INSERT vào khoá chính (user_id, role_id), nên không còn 500. Đọc tập hiện có SAU khoá thì
    // thấy đúng tập lượt trước vừa commit.
    //
    // Vai trò được THÊM bị khoá dòng (FOR KEY SHARE) SAU phép so-và-đổi token và TRƯỚC câu INSERT — nợ E17, cặp khoá với
    // đường xoá vai trò ở RoleRowLocks. Phép kiểm "vai trò tồn tại" của handler chạy ngoài khoá; phép kiểm dưới khoá này là
    // phép chốt: vai trò vừa bị xoá ⇒ 422 ROLE_NOT_FOUND, không phải 23503 ⇒ 500. Vai trò bị GỠ không cần khoá: lượt xoá
    // đếm người mang dưới khoá của nó, thấy dòng còn đó ⇒ IN_USE, không cascade.
    public async Task<Result> AssignRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, string? version, CancellationToken ct)
    {
        if (!await TryClaimAccountVersionAsync(userId, version, ct))
            return Result.Failure(CommonErrors.ConcurrencyConflict);

        var currentRoleIds = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var desired = roleIds.ToHashSet();
        var current = currentRoleIds.ToHashSet();

        var toRemove = current.Except(desired).ToList();
        // Theo thứ tự payload — vai trò vắng mặt ĐẦU TIÊN quyết định messageParams, như phép kiểm của handler.
        var toAdd = roleIds.Distinct().Where(id => !current.Contains(id)).ToList();

        var missing = await FirstMissingRoleAsync(toAdd, ct);
        if (missing is { } missingRoleId)
            return Result.Failure(UserErrors.RoleNotFound.WithParams(("RoleId", missingRoleId)));

        if (toRemove.Count > 0)
        {
            var rowsToRemove = await db.UserRoles
                .Where(ur => ur.UserId == userId && toRemove.Contains(ur.RoleId))
                .ToListAsync(ct);
            db.UserRoles.RemoveRange(rowsToRemove);
        }

        // TenantId gán bằng interceptor lúc Added (docs/quy-uoc/be-entity-domain.md §5.1) — không
        // gán tay ở đây.
        foreach (var roleId in toAdd)
            db.UserRoles.Add(new AppUserRole { UserId = userId, RoleId = roleId });

        return Result.Success();
    }

    public async Task<Result> GrantInitialRolesAsync(Guid newUserId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        // Chỉ tài khoản do CreateAsync của chính instance này tạo. Mọi tài khoản khác — kể cả một tài khoản cũ đang nằm trong
        // bộ theo dõi — là tài khoản đã tồn tại: đường đó PHẢI đi qua AssignRolesAsync và phép so token (ADR-0082, "Dấu hiệu
        // quyết định này bắt đầu sai").
        if (!_createdByThisService.Contains(newUserId))
            throw new InvalidOperationException(
                $"GrantInitialRolesAsync chỉ dành cho tài khoản vừa tạo bằng CreateAsync trong cùng đơn vị công việc; {newUserId} " +
                "không phải — đổi vai trò của tài khoản đã có đi qua AssignRolesAsync.");

        var toAdd = roleIds.Distinct().ToList();

        // Cùng khoá với AssignRolesAsync, cùng lý do (nợ E17): mọi vai trò của tài khoản mới đều là vai trò được THÊM.
        var missing = await FirstMissingRoleAsync(toAdd, ct);
        if (missing is { } missingRoleId)
            return Result.Failure(UserErrors.RoleNotFound.WithParams(("RoleId", missingRoleId)));

        foreach (var roleId in toAdd)
            db.UserRoles.Add(new AppUserRole { UserId = newUserId, RoleId = roleId });

        return Result.Success();
    }

    // Khoá dòng các vai trò sắp được thêm (RoleRowLocks.LockForAssignAsync — theo thứ tự id) rồi trả vai trò ĐẦU TIÊN theo
    // thứ tự của danh sách truyền vào mà không còn tồn tại; null nếu đủ cả. Danh sách rỗng thì không khoá, không SQL.
    private async Task<Guid?> FirstMissingRoleAsync(IReadOnlyList<Guid> toAdd, CancellationToken ct)
    {
        if (toAdd.Count == 0)
            return null;

        var existing = await RoleRowLocks.LockForAssignAsync(db, toAdd, ct);
        foreach (var roleId in toAdd)
        {
            if (!existing.Contains(roleId))
                return roleId;
        }

        return null;
    }

    // MỘT câu UPDATE có điều kiện trên core.app_user: so token client gửi lên VÀ đổi sang token mới trong cùng câu. Không
    // tách thành đọc-rồi-so: hai PUT cùng đọc một stamp đều qua phép so (ADR-0082 phương án B, "đọc-rồi-so").
    //
    // Qua db.Users nên câu mang bộ lọc đơn vị (luật M6 không cần SQL thô). ExecuteUpdate không đi qua ChangeTracker nên
    // AuditInterceptor không chạy — tự điền hai cột vết (docs/quy-uoc/be-performance.md §7.1). Đổi token không phải một
    // thay đổi nhật ký kiểm toán cần ghi: dòng core.user.role_assign do AuditLogInterceptor sinh từ chính thay đổi vai trò.
    private async Task<bool> TryClaimAccountVersionAsync(Guid userId, string? version, CancellationToken ct)
    {
        // null không bao giờ khớp — và EF dịch "== null" thành IS NULL, nên phải chặn trước khi dựng câu.
        if (version is null)
            return false;

        // Ngoài transaction, khoá dòng nhả ngay sau câu lệnh và token đã đổi dù phần ghi vai trò sau đó hỏng. Lỗi lập trình:
        // command luôn chạy trong TransactionBehavior.
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "UserAdminService.AssignRolesAsync phải chạy trong transaction của người gọi (TransactionBehavior).");

        // Câu UPDATE dưới đây là lượt giữ khoá ĐẦU TIÊN của thao tác: PUT thứ hai chờ khoá dòng tài khoản ở chính nó. Thời hạn
        // chờ phải có TRƯỚC câu đó (docs/wiki-core/be/06-concurrency-control.md §7 quy tắc 3) — đặt muộn hơn, ở khoá dòng vai trò,
        // thì lượt chờ ở đây chỉ bị chặn bởi CommandTimeout.
        await LockTimeout.SetForCurrentTransactionAsync(db, ct);

        var newStamp = Guid.NewGuid().ToString();
        var now = timeProvider.GetUtcNow();
        var actor = currentUser.UserName ?? SystemActor.UserName;

        var claimed = await db.Users
            .Where(u => u.Id == userId && u.ConcurrencyStamp == version)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.ConcurrencyStamp, newStamp)
                .SetProperty(u => u.UpdatedAt, (DateTimeOffset?)now)
                .SetProperty(u => u.UpdatedBy, actor), ct);

        return claimed == 1;
    }

    private async Task<Result> SetLockoutAsync(Guid userId, string? version, bool locked, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (!IdentityConcurrency.VersionMatches(user, version))
            return Result.Failure(CommonErrors.ConcurrencyConflict);

        if (locked)
            user.MarkLockedByAdmin();
        else
            user.ClearAdminLock();

        var lockoutEnd = locked ? DateTimeOffset.MaxValue : (DateTimeOffset?)null;
        var identityResult = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        if (!identityResult.Succeeded)
        {
            var conflict = IdentityConcurrency.DetectConflict(identityResult);
            return Result.Failure(conflict ?? UserErrors.LockFailed);
        }

        // Đổi security stamp tường minh CHỈ khi khoá — mở khoá KHÔNG đổi stamp
        // (docs/contracts/users.md §8 "Ghi chú"). Hỏng thì trả thất bại, như ResetPasswordAsync.
        if (locked)
        {
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                return Result.Failure(IdentityConcurrency.DetectConflict(stampResult) ?? UserErrors.LockFailed);
        }

        return Result.Success();
    }

    // Ánh xạ mã Identity thô → catalog qua IdentityErrorMapper (docs/contracts/auth.md §6 "Ghi chú"). Khoá dự phòng
    // "$record" cho mã không quy được cho một ô nhập của card users.md. rejectedUser: tài khoản vừa đưa vào UserManager —
    // nguồn tham số UserName/Email của nhánh trùng lặp (hai người tạo/sửa cùng lúc lọt qua phép kiểm trước của handler).
    private Error MapFailure(IEnumerable<IdentityError> errors, AppUser rejectedUser, Error rootError)
        => rootError.WithFieldErrors(IdentityErrorMapper.ToFieldErrors(
            errors, rejectedUser, userManager.Options.Password, subject => subject switch
            {
                IdentityErrorMapper.Subject.UserName => "UserName",
                IdentityErrorMapper.Subject.Email => "Email",
                IdentityErrorMapper.Subject.NewPassword => "TempPassword",
                _ => "$record",
            }));
}
