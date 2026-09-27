using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// Đặt lại mật khẩu hộ (docs/contracts/users.md §9) và khoá tài khoản (§8) kết thúc bằng một lệnh đổi security stamp để mọi
// phiên đang mở trượt. Lệnh đó hỏng thì thao tác phải THẤT BẠI: trả thành công thì TransactionBehavior lưu tiếp và commit sau
// một lượt lưu hỏng (ADR-0092 điều kiện a) — và phiên của tài khoản đích vẫn sống dù người quản trị thấy "đã xong".
//
// Mã lỗi: cùng cách ánh xạ với các lệnh Identity khác của chính đường đó — xung đột ⇒ CORE.CONCURRENCY.CONFLICT, còn lại ⇒
// mã riêng của đường (card §8, §9 khai cả hai).
public sealed class UserAdminStampFailureTests
{
    private const string Stamp = nameof(UserManager<AppUser>.UpdateSecurityStampAsync);

    [Fact]
    public async Task ResetPassword_StampUpdateHitsAConcurrencyFailure_ReturnsConflict()
    {
        var (users, account) = Scripted(failAt: Stamp);

        var result = await Service(users).ResetPasswordAsync(account.Id, "MatKhauTam!2026", account.ConcurrencyStamp, CancellationToken.None);

        result.Error.ShouldNotBeNull().Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        users.Writes.Last().ShouldBe(Stamp);
    }

    [Fact]
    public async Task ResetPassword_StampUpdateRejectedByIdentity_ReturnsResetPasswordFailed()
    {
        var (users, account) = Scripted(failAt: Stamp, failure: new IdentityErrorDescriber().DefaultError());

        var result = await Service(users).ResetPasswordAsync(account.Id, "MatKhauTam!2026", account.ConcurrencyStamp, CancellationToken.None);

        result.Error.ShouldNotBeNull().Code.ShouldBe(UserErrors.ResetPasswordFailed.Code);
    }

    [Fact]
    public async Task Lock_StampUpdateHitsAConcurrencyFailure_ReturnsConflict()
    {
        var (users, account) = Scripted(failAt: Stamp);

        var result = await Service(users).LockAsync(account.Id, account.ConcurrencyStamp, CancellationToken.None);

        result.Error.ShouldNotBeNull().Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        users.Writes.ShouldBe([nameof(UserManager<AppUser>.SetLockoutEndDateAsync), Stamp]);
    }

    [Fact]
    public async Task Lock_StampUpdateRejectedByIdentity_ReturnsLockFailed()
    {
        var (users, account) = Scripted(failAt: Stamp, failure: new IdentityErrorDescriber().DefaultError());

        var result = await Service(users).LockAsync(account.Id, account.ConcurrencyStamp, CancellationToken.None);

        result.Error.ShouldNotBeNull().Code.ShouldBe(UserErrors.LockFailed.Code);
    }

    // Đối chứng chống test rỗng: cùng bộ dựng, không lệnh nào hỏng ⇒ cả hai thao tác thành công và đều đổi stamp.
    [Fact]
    public async Task ResetPasswordAndLock_AllWritesSucceed_ReturnSuccess_AfterTheStampUpdate()
    {
        var (resetUsers, resetAccount) = Scripted();
        var reset = await Service(resetUsers).ResetPasswordAsync(resetAccount.Id, "MatKhauTam!2026", resetAccount.ConcurrencyStamp, CancellationToken.None);
        reset.IsSuccess.ShouldBeTrue(reset.Error?.Code);
        resetUsers.Writes.Last().ShouldBe(Stamp);

        var (lockUsers, lockAccount) = Scripted();
        var locked = await Service(lockUsers).LockAsync(lockAccount.Id, lockAccount.ConcurrencyStamp, CancellationToken.None);
        locked.IsSuccess.ShouldBeTrue(locked.Error?.Code);
        lockUsers.Writes.Last().ShouldBe(Stamp);
    }

    private static (ScriptedWriteUserManager Users, AppUser Account) Scripted(string? failAt = null, IdentityError? failure = null)
    {
        var account = AppUser.NewAccount("binh", "binh@dv.vn", "Trần Văn Bình");
        account.ConcurrencyStamp = Guid.NewGuid().ToString();
        return (new ScriptedWriteUserManager(account, failAt, failure), account);
    }

    private static UserAdminService Service(ScriptedWriteUserManager users)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserName.Returns("quantri");
        return new UserAdminService(users, OfflineCoreDbContext.Create(Guid.NewGuid()), TimeProvider.System, currentUser);
    }
}
