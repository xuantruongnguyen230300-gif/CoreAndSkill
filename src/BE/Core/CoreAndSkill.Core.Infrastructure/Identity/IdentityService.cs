using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Hiện thực IIdentityService — dùng UserManager<AppUser> trực tiếp, KHÔNG SignInManager (luật
// docs/adr/0026-ranh-gioi-identity-va-cookie.md). Tự đếm lần sai / khoá tài khoản đúng thứ tự ở
// docs/wiki-core/be/02-identity-auth.md §4.2 — SignInManager làm việc này trong MỘT lời gọi, ở đây
// viết tay theo bốn ca của bảng đó.
internal sealed class IdentityService(UserManager<AppUser> userManager, PasswordRehashScope rehashScope)
    : IIdentityService
{
    public async Task<Result<CredentialCheck>> CheckCredentialsAsync(string userName, string password, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            // Không có tài khoản vẫn trả giá một phép băm — thiếu nó, "không có tài khoản" nhanh hẳn "sai mật khẩu" và thời
            // gian phản hồi lộ ra tên đăng nhập nào có thật (docs/wiki-core/be/09-security-beyond-auth.md bảng "Đường rò").
            VerifyAgainstDummyHash(password);
            return Result.Failure<CredentialCheck>(AuthErrors.InvalidCredentials);
        }

        var passwordValid = await CheckPasswordAsync(user, password);
        var isLockedOut = await userManager.IsLockedOutAsync(user);

        if (!passwordValid)
        {
            // Ca 2 (đang khoá): KHÔNG tính lần sai — đủ ngưỡng thì AccessFailedAsync ghi đè mốc
            // khoá, rút ngắn lệnh khoá tay của quản trị xuống bằng thời gian khoá tự động.
            if (!isLockedOut)
                await userManager.AccessFailedAsync(user); // Ca 1: tính lần sai; đủ ngưỡng thì Identity tự đặt mốc khoá.

            return Result.Failure<CredentialCheck>(AuthErrors.InvalidCredentials);
        }

        if (isLockedOut)
            return Result.Failure<CredentialCheck>(AuthErrors.LockedOut); // Ca 3.

        await userManager.ResetAccessFailedCountAsync(user); // Ca 4 — PHẢI xoá bộ đếm.

        var stamp = await userManager.GetSecurityStampAsync(user);
        return Result.Success(new CredentialCheck(user.Id, stamp, user.MustChangePassword));
    }

    public Task SimulateCredentialCheckAsync(string password, CancellationToken ct)
    {
        VerifyAgainstDummyHash(password);
        return Task.CompletedTask;
    }

    // Hash giả dựng MỘT lần bằng chính hasher đang dùng — cùng thuật toán, cùng số vòng lặp với hash thật, nên phép kiểm
    // trên nó tốn đúng giá một phép kiểm thật. Hasher của Identity đăng ký Scoped, nên bộ đệm là tĩnh (một tiến trình,
    // một cấu hình hasher). Hai luồng cùng dựng lần đầu thì một bản thắng — cả hai bản đều hợp lệ.
    private static string? _dummyHash;

    private void VerifyAgainstDummyHash(string password)
    {
        var hasher = userManager.PasswordHasher;
        var dummyUser = new AppUser();
        var dummyHash = Volatile.Read(ref _dummyHash);
        if (dummyHash is null)
        {
            dummyHash = hasher.HashPassword(dummyUser, Guid.NewGuid().ToString("N"));
            dummyHash = Interlocked.CompareExchange(ref _dummyHash, dummyHash, null) ?? dummyHash;
        }

        _ = hasher.VerifyHashedPassword(dummyUser, dummyHash, password);
    }

    public async Task<Result<CredentialCheck>> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, bool clearMustChangePassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("ChangePasswordAsync gọi với userId không tồn tại trong đơn vị hiện tại.");

        var identityResult = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!identityResult.Succeeded)
            return Result.Failure<CredentialCheck>(MapChangePasswordFailure(identityResult, user));

        if (clearMustChangePassword && user.MustChangePassword)
        {
            user.ClearPasswordChangeRequirement();

            // Lệnh gỡ cờ hỏng thì trả thất bại: trả thành công thì TransactionBehavior lưu tiếp và commit, mang theo thay đổi
            // của lượt lưu hỏng cùng dòng interceptor đã thêm cho nó (ADR-0092 điều kiện a), và phiên mới báo cờ đã gỡ trong
            // khi database còn giữ.
            var clearResult = await userManager.UpdateAsync(user);
            if (!clearResult.Succeeded)
                return Result.Failure<CredentialCheck>(MapChangePasswordFailure(clearResult, user));
        }

        var freshStamp = await userManager.GetSecurityStampAsync(user);
        return Result.Success(new CredentialCheck(user.Id, freshStamp, user.MustChangePassword));
    }

    // Xung đột đồng thời trước (IdentityConcurrency — thứ tự bắt buộc), còn lại theo card auth.md §6/§7.
    private Error MapChangePasswordFailure(IdentityResult identityResult, AppUser user)
        => IdentityConcurrency.DetectConflict(identityResult) ?? MapChangePasswordErrors(identityResult.Errors, user);

    // Kiểm mật khẩu đăng nhập. CheckPasswordAsync của Identity tự băm lại và LƯU khi dạng băm đã cũ — lượt lưu đó
    // chạy trong PasswordRehashScope để AuditLogInterceptor không ghi nó thành một lần đổi mật khẩu. internal — cho
    // test gọi thẳng mà không cần database cho FindByNameAsync (InternalsVisibleTo IntegrationTests).
    internal async Task<bool> CheckPasswordAsync(AppUser user, string password)
    {
        using (rehashScope.Begin())
            return await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> IsSessionValidAsync(Guid userId, string securityStamp, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;

        var currentStamp = await userManager.GetSecurityStampAsync(user);
        return string.Equals(currentStamp, securityStamp, StringComparison.Ordinal);
    }

    // docs/contracts/auth.md §6 "Ghi chú": PasswordMismatch nói về mật khẩu HIỆN TẠI → CurrentPassword; lỗi chính sách
    // → NewPassword; mã không quy được cho ô nào → khoá dự phòng "$record". Ánh xạ mã: IdentityErrorMapper.
    private Error MapChangePasswordErrors(IEnumerable<IdentityError> errors, AppUser user)
        => AuthErrors.ChangePasswordFailed.WithFieldErrors(IdentityErrorMapper.ToFieldErrors(
            errors, user, userManager.Options.Password, subject => subject switch
            {
                IdentityErrorMapper.Subject.CurrentPassword => "CurrentPassword",
                IdentityErrorMapper.Subject.NewPassword => "NewPassword",
                _ => "$record",
            }));
}
