using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Profile;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Hiện thực IUserProfileService — docs/contracts/profile.md. Version trên dây = concurrency_stamp
// của Identity (docs/wiki-core/be/06-concurrency-control.md §6.3).
internal sealed class UserProfileService(UserManager<AppUser> userManager, CoreDbContext db) : IUserProfileService
{
    public async Task<ProfileDto?> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToDto(user);
    }

    public async Task<Result<ProfileDto>> UpdateAsync(Guid userId, UpdateProfileInput input, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("UpdateAsync gọi với userId không tồn tại trong đơn vị hiện tại.");

        // Lệch hoặc thiếu (null không bao giờ khớp) ⇒ 409, không ghi gì — khuôn ở IdentityConcurrency.VersionMatches.
        if (!IdentityConcurrency.VersionMatches(user, input.Version))
            return Result.Failure<ProfileDto>(CommonErrors.ConcurrencyConflict);

        user.UpdateProfile(input.FullName, input.PreferredLanguage);
        user.PhoneNumber = input.PhoneNumber;

        var identityResult = await userManager.UpdateAsync(user);
        if (!identityResult.Succeeded)
        {
            var concurrencyConflict = IdentityConcurrency.DetectConflict(identityResult);
            if (concurrencyConflict is not null)
                return Result.Failure<ProfileDto>(concurrencyConflict);

            throw new InvalidOperationException(
                "UpdateAsync thất bại ngoài dự kiến — dữ liệu đã qua validator trước khi tới đây: " +
                string.Join(", ", identityResult.Errors.Select(e => e.Code)));
        }

        return Result.Success(ToDto(user));
    }

    public async Task<Result> RenouncePermissionBypassAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("RenouncePermissionBypassAsync gọi với userId không tồn tại.");

        if (!user.HasPermissionBypass)
            return Result.Failure(ProfileErrors.PermissionBypassNotHeld);

        if (!await HasOtherPermissionAdminAsync(user.Id, ct))
            return Result.Failure(ProfileErrors.NoOtherPermissionAdmin);

        user.RenouncePermissionBypass();

        var identityResult = await userManager.UpdateAsync(user);
        if (!identityResult.Succeeded)
        {
            var concurrencyConflict = IdentityConcurrency.DetectConflict(identityResult);
            if (concurrencyConflict is not null)
                return Result.Failure(concurrencyConflict);

            throw new InvalidOperationException(
                "RenouncePermissionBypassAsync thất bại ngoài dự kiến: " +
                string.Join(", ", identityResult.Errors.Select(e => e.Code)));
        }

        return Result.Success();
    }

    // Đơn vị có tài khoản KHÁC, KHÔNG bị khoá, giữ core.permission.write qua VAI TRÒ —
    // docs/contracts/profile.md §3 "Ghi chú". Ở B1, core.role_permission/core.permission chưa có
    // dữ liệu (B2 mới seed) nên truy vấn này hợp lệ trả về false cho tới khi B2 seed xong — đúng
    // hành vi an toàn mặc định (chặn từ bỏ khi chưa ai khác giữ quyền phân quyền).
    private async Task<bool> HasOtherPermissionAdminAsync(Guid excludingUserId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var query =
            from ur in db.UserRoles
            join rp in db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in db.Permissions on rp.PermissionId equals p.Id
            join u in db.Users on ur.UserId equals u.Id
            where p.Code == "core.permission.write"
               && u.Id != excludingUserId
               && (u.LockoutEnd == null || u.LockoutEnd <= now)
            select u.Id;

        return await query.AnyAsync(ct);
    }

    private static ProfileDto ToDto(AppUser user) => new(
        user.UserName!,
        user.Email,
        user.FullName,
        user.PhoneNumber,
        user.PreferredLanguage,
        user.HasPermissionBypass,
        user.ConcurrencyStamp ?? string.Empty);
}
