using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// UserManager có kịch bản cho các lệnh GHI: ghi lại thứ tự lệnh ghi được gọi, và lệnh mang tên `failAt` trả IdentityResult
// thất bại với `failure` (mặc định ConcurrencyFailure — đúng thứ UserStore trả khi bắt DbUpdateConcurrencyException). Lệnh
// ghi khác thành công mà không chạm kho. Hai lệnh tra (theo tên, theo id) trả đúng tài khoản cho sẵn.
//
// Dùng để khẳng định "lệnh ghi hỏng thì không lệnh ghi nào chạy sau nó": thứ tự trong Writes là thứ tự service gọi.
internal sealed class ScriptedWriteUserManager(AppUser user, string? failAt = null, IdentityError? failure = null)
    : UserManager<AppUser>(
        Substitute.For<IUserStore<AppUser>>(), Microsoft.Extensions.Options.Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(),
        [], [new PasswordValidator<AppUser>()], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
        null!, NullLogger<UserManager<AppUser>>.Instance)
{
    private readonly IdentityError _failure = failure ?? new IdentityErrorDescriber().ConcurrencyFailure();

    public List<string> Writes { get; } = [];

    public override Task<AppUser?> FindByNameAsync(string userName) => Task.FromResult<AppUser?>(user);

    public override Task<AppUser?> FindByIdAsync(string userId) => Task.FromResult<AppUser?>(user);

    public override Task<string> GetSecurityStampAsync(AppUser target) => Task.FromResult(target.SecurityStamp ?? string.Empty);

    public override Task<IdentityResult> RemovePasswordAsync(AppUser target) => Write(nameof(RemovePasswordAsync));

    public override Task<IdentityResult> AddPasswordAsync(AppUser target, string password) => Write(nameof(AddPasswordAsync));

    public override Task<IdentityResult> ChangePasswordAsync(AppUser target, string currentPassword, string newPassword)
        => Write(nameof(ChangePasswordAsync));

    public override Task<IdentityResult> SetLockoutEndDateAsync(AppUser target, DateTimeOffset? lockoutEnd)
        => Write(nameof(SetLockoutEndDateAsync));

    public override Task<IdentityResult> UpdateAsync(AppUser target) => Write(nameof(UpdateAsync));

    public override Task<IdentityResult> UpdateSecurityStampAsync(AppUser target) => Write(nameof(UpdateSecurityStampAsync));

    private Task<IdentityResult> Write(string call)
    {
        Writes.Add(call);
        return Task.FromResult(call == failAt ? IdentityResult.Failed(_failure) : IdentityResult.Success);
    }
}
