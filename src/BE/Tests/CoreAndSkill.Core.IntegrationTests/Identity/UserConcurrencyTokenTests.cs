using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Profile;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// Token đồng thời của bản ghi tài khoản trên mọi thao tác ghi nhận `version` — UserAdminService.UpdateAsync,
// ResetPasswordAsync, LockAsync, UnlockAsync và UserProfileService.UpdateAsync — chạy trên UserManager + UserStore
// THẬT, ChangeTracker THẬT, không database (OfflineCoreDbContext). docs/wiki-core/be/06-concurrency-control.md §6.3;
// hợp đồng: docs/contracts/users.md §6, §8, §9 và docs/contracts/profile.md §2.
//
// UserRowEmulator đứng thay PostgreSQL ở phép so của câu UPDATE — WHERE concurrency_stamp = <OriginalValue lúc
// SaveChanges>, 0 dòng ⇒ DbUpdateConcurrencyException — và giữ "dòng trong DB". Nó đọc OriginalValue SAU khi
// UserStore.UpdateAsync đã Attach bản ghi, nên một hiện thực chỉ đặt OriginalValue = version (bị Attach ghi đè bằng
// stamp vừa đọc) sẽ đỏ ở ca version cũ và ca thiếu version. Thứ nó KHÔNG chứng minh: câu SQL thật trên PostgreSQL.
//
// Cùng khuôn RoleRenameConcurrencyTests. Tra cứu theo id của UserManager được trỏ về bản ghi đang theo dõi; truy vấn
// kiểm trùng email của UpdateAsync nhận "không trùng" từ ExistsFalseReader.
public sealed class UserConcurrencyTokenTests
{
    private const string OriginalFullName = "Nguyễn Văn An";
    private const string TempPassword = "Temp-Passw0rd-9";

    private readonly Guid _tenantId = Guid.NewGuid();

    public static TheoryData<string> Operations =>
        ["users.update", "users.reset-password", "users.lock", "users.unlock", "profile.update"];

    private sealed record Fixture(
        UserAdminService Admin, UserProfileService Profile, UserRowEmulator Row, AppUser User, CoreDbContext Db);

    private Fixture Build(bool lockedByAdmin)
    {
        var user = new AppUser
        {
            TenantId = _tenantId,
            UserName = "an.nguyen",
            NormalizedUserName = "AN.NGUYEN",
            Email = "an@vd.vn",
            NormalizedEmail = "AN@VD.VN",
            PasswordHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), "Old-Passw0rd-1"),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            LockoutEnd = lockedByAdmin ? DateTimeOffset.MaxValue : null,
        };
        user.ChangeFullName(OriginalFullName);
        if (lockedByAdmin)
            user.MarkLockedByAdmin();
        var row = new UserRowEmulator(user);

        var db = OfflineCoreDbContext.Create(_tenantId, new ConnectionSuppressor(), new ExistsFalseReader(), row);
        db.Attach(user);

        var store = new UserStore<AppUser, AppRole, CoreDbContext, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(db);
        var manager = new TrackedLookupUserManager(store, user);
        return new Fixture(new UserAdminService(manager, db, TimeProvider.System, Substitute.For<ICurrentUser>()), new UserProfileService(manager, db), row, user, db);
    }

    // Mỗi thao tác kèm trạng thái xuất phát khiến lượt ghi của nó CÓ nghĩa: mở khoá thì bắt đầu từ tài khoản đang khoá.
    private Fixture BuildFor(string operation) => Build(lockedByAdmin: operation == "users.unlock");

    private static async Task<Result> Run(string operation, Fixture f, string? version) => operation switch
    {
        "users.update" => await f.Admin.UpdateAsync(
            f.User.Id, new UpdateUserInput("an.moi@vd.vn", "Nguyễn Văn An (mới)", version), CancellationToken.None),
        "users.reset-password" => await f.Admin.ResetPasswordAsync(f.User.Id, TempPassword, version, CancellationToken.None),
        "users.lock" => await f.Admin.LockAsync(f.User.Id, version, CancellationToken.None),
        "users.unlock" => await f.Admin.UnlockAsync(f.User.Id, version, CancellationToken.None),
        "profile.update" => await f.Profile.UpdateAsync(
            f.User.Id, new UpdateProfileInput("Nguyễn Văn An (mới)", "0912345678", "vi", version), CancellationToken.None),
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
    };

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task StaleVersion_ReturnsConcurrencyConflict_AndWritesNothing(string operation)
    {
        var f = BuildFor(operation);
        await using var _ = f.Db;
        var before = f.Row.Snapshot();

        var result = await Run(operation, f, "stamp-cu-cua-lan-doc-truoc");

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        f.Row.Writes.ShouldBe(0);
        f.Row.Snapshot().ShouldBe(before);
    }

    // `null` không bao giờ khớp — thiếu version là 409, không phải "bỏ qua phép so".
    [Theory]
    [MemberData(nameof(Operations))]
    public async Task MissingVersion_ReturnsConcurrencyConflict_AndWritesNothing(string operation)
    {
        var f = BuildFor(operation);
        await using var _ = f.Db;
        var before = f.Row.Snapshot();

        var result = await Run(operation, f, null);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        f.Row.Writes.ShouldBe(0);
        f.Row.Snapshot().ShouldBe(before);
    }

    // Client giữ đúng stamp đã đọc, nhưng một lượt ghi khác commit xen giữa lúc service đọc và lúc UPDATE: câu UPDATE
    // (WHERE concurrency_stamp = stamp đã đọc) phải bắt được — không chỉ phép so ở đầu service.
    [Theory]
    [MemberData(nameof(Operations))]
    public async Task WriteBetweenReadAndUpdate_ReturnsConcurrencyConflict_AndKeepsTheOtherWrite(string operation)
    {
        var f = BuildFor(operation);
        await using var _ = f.Db;
        var readStamp = f.Row.Stamp;
        f.Row.CommitElsewhere("Tên của người khác");
        var afterOtherWrite = f.Row.Snapshot();

        var result = await Run(operation, f, readStamp);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        f.Row.Writes.ShouldBe(0);
        f.Row.Snapshot().ShouldBe(afterOtherWrite);
    }

    // Đối chứng dương — nếu ba ca trên xanh vì không lượt lưu nào chạy được thì ca này đỏ.
    [Theory]
    [MemberData(nameof(Operations))]
    public async Task CurrentVersion_Writes_AndIssuesANewVersion(string operation)
    {
        var f = BuildFor(operation);
        await using var _ = f.Db;
        var current = f.Row.Stamp;

        var result = await Run(operation, f, current);

        result.IsSuccess.ShouldBeTrue();
        f.Row.Writes.ShouldBeGreaterThan(0);
        f.Row.Stamp.ShouldNotBe(current);
    }

    // Đứng thay PostgreSQL ở phép so token của câu UPDATE: đọc OriginalValue — đúng giá trị EF đặt vào WHERE.
    private sealed class UserRowEmulator(AppUser seed) : SaveChangesInterceptor
    {
        public string Stamp { get; private set; } = seed.ConcurrencyStamp!;
        private string? _email = seed.Email;
        private string? _fullName = seed.FullName;
        private string? _passwordHash = seed.PasswordHash;
        private bool _lockedByAdmin = seed.LockedByAdmin;
        private DateTimeOffset? _lockoutEnd = seed.LockoutEnd;
        public int Writes { get; private set; }

        public string Snapshot() => $"{Stamp}|{_email}|{_fullName}|{_passwordHash}|{_lockedByAdmin}|{_lockoutEnd:O}";

        // Một lượt ghi khác đã commit: dòng trong "DB" đổi, bản ghi service đang giữ thì không.
        public void CommitElsewhere(string fullName)
        {
            _fullName = fullName;
            Stamp = Guid.NewGuid().ToString();
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => Save(eventData.Context!);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Save(eventData.Context!));

        private InterceptionResult<int> Save(DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries<AppUser>().Where(e => e.State == EntityState.Modified))
            {
                var expected = entry.Property(u => u.ConcurrencyStamp).OriginalValue;
                if (!string.Equals(expected, Stamp, StringComparison.Ordinal))
                    throw new DbUpdateConcurrencyException("UPDATE app_user ... WHERE concurrency_stamp = @p: 0 dòng.");

                var user = entry.Entity;
                Stamp = user.ConcurrencyStamp!;
                _email = user.Email;
                _fullName = user.FullName;
                _passwordHash = user.PasswordHash;
                _lockedByAdmin = user.LockedByAdmin;
                _lockoutEnd = user.LockoutEnd;
                Writes++;
            }

            context.ChangeTracker.AcceptAllChanges();
            return InterceptionResult<int>.SuppressWithResult(1);
        }
    }

    // Không mở kết nối nào — lệnh đọc duy nhất (kiểm trùng email) được ExistsFalseReader trả lời.
    private sealed class ConnectionSuppressor : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
            => InterceptionResult.Suppress();

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    // Trả một dòng `false` cho mọi lệnh đọc — tức "email không thuộc người khác" ở câu EXISTS của UpdateAsync.
    private sealed class ExistsFalseReader : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            => InterceptionResult<DbDataReader>.SuppressWithResult(FalseRow());

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(FalseRow()));

        private static DbDataReader FalseRow()
        {
            var table = new DataTable();
            table.Columns.Add("exists", typeof(bool));
            table.Rows.Add(false);
            return table.CreateDataReader();
        }
    }

    private sealed class TrackedLookupUserManager(IUserStore<AppUser> store, AppUser user)
        : UserManager<AppUser>(store, Microsoft.Extensions.Options.Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(),
            [], [new PasswordValidator<AppUser>()], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null!, NullLogger<UserManager<AppUser>>.Instance)
    {
        public override Task<AppUser?> FindByIdAsync(string userId)
            => Task.FromResult<AppUser?>(userId == user.Id.ToString() ? user : null);
    }
}
