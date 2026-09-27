using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// AuditLogInterceptor trên những lượt ghi AppUser đi qua UserManager thật (UserStore thật, ChangeTracker thật) — không cần
// database: lượt SaveChanges bị chặn sau khi interceptor đã dàn dựng dòng nhật ký (OfflineCoreDbContext).
//
// UserStore.UpdateAsync gọi DbContext.Update(user) — mọi cột bị đánh dấu "đã sửa", kể cả cột không đổi giá trị. Nhật ký
// chỉ được ghi cho thay đổi CÓ THẬT (docs/wiki-core/be/10-data-retention.md §5.4): khoá/mở khoá, đổi mật khẩu.
public sealed class UserAuditOnIdentityUpdateTests
{
    private const string Password = "Passw0rd-Test1";

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly SaveCapture _capture = new();

    private readonly PasswordRehashScope _rehashScope = new();

    private (CoreDbContext Db, UserManager<AppUser> Users) Build()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        var clientAddress = Substitute.For<IClientAddressAccessor>();
        var auditLog = new AuditLogInterceptor(TimeProvider.System, currentUser, clientAddress, _rehashScope, new CrossTenantActorScope());

        var db = OfflineCoreDbContext.Create(_tenantId, auditLog, _capture);
        var store = new UserStore<AppUser, AppRole, CoreDbContext, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(db);

        // Không UserValidator: bộ kiểm trùng tên đăng nhập tra database. Mật khẩu vẫn qua PasswordValidator mặc định.
        var users = new UserManager<AppUser>(
            store, Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(),
            [], [new PasswordValidator<AppUser>()], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null!, NullLogger<UserManager<AppUser>>.Instance);

        return (db, users);
    }

    // Người dùng như vừa đọc lên từ database: đang được theo dõi, trạng thái Unchanged.
    private AppUser TrackedUser(CoreDbContext db, string passwordHash)
    {
        var user = new AppUser
        {
            TenantId = _tenantId,
            UserName = "quantri",
            NormalizedUserName = "QUANTRI",
            PasswordHash = passwordHash,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
        };
        user.ChangeFullName("Quản trị");
        db.Attach(user);
        return user;
    }

    private static string HashWithIterations(string password, int iterations)
        => new PasswordHasher<AppUser>(Options.Create(new PasswordHasherOptions { IterationCount = iterations }))
            .HashPassword(new AppUser(), password);

    private IEnumerable<string> ActionCodes => _capture.AuditRows.Select(r => r.ActionCode);

    // V-03: Identity băm lại mật khẩu (SuccessRehashNeeded) lúc kiểm mật khẩu đăng nhập — mật khẩu KHÔNG đổi.
    [Fact]
    public async Task RehashOnLogin_IsNotAPasswordChange()
    {
        var (db, users) = Build();
        await using var _ = db;
        var user = TrackedUser(db, HashWithIterations(Password, 1_000));
        var hashBefore = user.PasswordHash;

        // Đúng đường đăng nhập dùng (IdentityService.CheckCredentialsAsync gọi hàm này sau FindByNameAsync).
        var identity = new IdentityService(users, _rehashScope);
        (await identity.CheckPasswordAsync(user, Password)).ShouldBeTrue();

        user.PasswordHash.ShouldNotBe(hashBefore, "chống test rỗng: Identity phải thật sự băm lại và lưu");
        ActionCodes.ShouldNotContain(AuditActionCodes.UserPasswordChange);
    }

    // Đăng nhập sai: AccessFailedAsync lưu qua UserStore.UpdateAsync — không khoá, không mở khoá, không đổi mật khẩu.
    [Fact]
    public async Task FailedLoginCounter_WritesNoLockOrPasswordAudit()
    {
        var (db, users) = Build();
        await using var _ = db;
        var user = TrackedUser(db, new PasswordHasher<AppUser>().HashPassword(new AppUser(), Password));

        await users.AccessFailedAsync(user);

        user.AccessFailedCount.ShouldBe(1, "chống test rỗng: lượt lưu phải thật sự chạy");
        ActionCodes.ShouldNotContain(AuditActionCodes.UserUnlock);
        ActionCodes.ShouldNotContain(AuditActionCodes.UserLock);
        ActionCodes.ShouldNotContain(AuditActionCodes.UserPasswordChange);
    }

    // Đối chứng dương — nếu hai test trên xanh vì interceptor không ghi gì cả thì test này đỏ.
    [Fact]
    public async Task RealPasswordChange_IsAuditedOnce()
    {
        var (db, users) = Build();
        await using var _ = db;
        var user = TrackedUser(db, new PasswordHasher<AppUser>().HashPassword(new AppUser(), Password));

        (await users.ChangePasswordAsync(user, Password, "New-Passw0rd-2")).Succeeded.ShouldBeTrue();

        ActionCodes.Count(c => c == AuditActionCodes.UserPasswordChange).ShouldBe(1);
    }

    [Fact]
    public async Task AdminLock_IsAuditedAsLock()
    {
        var (db, users) = Build();
        await using var _ = db;
        var user = TrackedUser(db, new PasswordHasher<AppUser>().HashPassword(new AppUser(), Password));

        user.MarkLockedByAdmin();
        (await users.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        ActionCodes.ShouldBe([AuditActionCodes.UserLock]);
    }
}
