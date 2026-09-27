using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// POST /auth/change-password-required (docs/contracts/auth.md §7): đổi mật khẩu xong, IdentityService gỡ cờ phải đổi mật khẩu
// bằng một lệnh UpdateAsync thứ hai. Lệnh đó hỏng — xung đột đồng thời, hay bộ kiểm người dùng từ chối — thì kết quả phải là
// THẤT BẠI: trả thành công thì TransactionBehavior lưu tiếp và commit (UnitOfWork lưu mọi context trước khi commit), mang theo
// thay đổi của lượt lưu hỏng và các dòng interceptor đã thêm cho nó — điều kiện kích hoạt (a) của ADR-0092 — và phiên mới
// báo mustChangePassword = false trong khi database còn true.
//
// Mã lỗi: cùng cách ánh xạ với lệnh ChangePasswordAsync ngay trước nó — xung đột ⇒ CORE.CONCURRENCY.CONFLICT, còn lại ⇒
// CORE.AUTH.CHANGE_PASSWORD_FAILED (card §6, §7 khai cả hai).
public sealed class ChangePasswordClearFlagFailureTests
{
    [Fact]
    public async Task ClearingTheFlagHitsAConcurrencyFailure_ReturnsConflict()
    {
        var users = new ScriptedWriteUserManager(AccountThatMustChangePassword(), failAt: nameof(UserManager<AppUser>.UpdateAsync));

        var result = await Service(users).ChangePasswordAsync(Guid.NewGuid(), "Cu-Passw0rd", "Moi-Passw0rd", clearMustChangePassword: true, CancellationToken.None);

        result.Error.ShouldNotBeNull().Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        users.Writes.ShouldBe([nameof(UserManager<AppUser>.ChangePasswordAsync), nameof(UserManager<AppUser>.UpdateAsync)]);
    }

    [Fact]
    public async Task ClearingTheFlagIsRejectedByIdentity_ReturnsChangePasswordFailed()
    {
        var users = new ScriptedWriteUserManager(
            AccountThatMustChangePassword(), failAt: nameof(UserManager<AppUser>.UpdateAsync),
            failure: new IdentityErrorDescriber().InvalidEmail("khong-hop-le"));

        var result = await Service(users).ChangePasswordAsync(Guid.NewGuid(), "Cu-Passw0rd", "Moi-Passw0rd", clearMustChangePassword: true, CancellationToken.None);

        result.Error.ShouldNotBeNull().Code.ShouldBe(AuthErrors.ChangePasswordFailed.Code);
    }

    // Đối chứng chống test rỗng: cùng bộ dựng, lệnh gỡ cờ thành công ⇒ thành công, cờ đã gỡ.
    [Fact]
    public async Task ClearingTheFlagSucceeds_ReturnsSuccess_WithTheFlagCleared()
    {
        var users = new ScriptedWriteUserManager(AccountThatMustChangePassword());

        var result = await Service(users).ChangePasswordAsync(Guid.NewGuid(), "Cu-Passw0rd", "Moi-Passw0rd", clearMustChangePassword: true, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        result.Value.MustChangePassword.ShouldBeFalse();
        users.Writes.ShouldBe([nameof(UserManager<AppUser>.ChangePasswordAsync), nameof(UserManager<AppUser>.UpdateAsync)]);
    }

    private static IdentityService Service(ScriptedWriteUserManager users) => new(users, new PasswordRehashScope());

    private static AppUser AccountThatMustChangePassword()
    {
        var user = AppUser.NewAccount("an", "an@dv.vn", "Nguyễn Văn An");
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        return user; // NewAccount bật MustChangePassword
    }
}
