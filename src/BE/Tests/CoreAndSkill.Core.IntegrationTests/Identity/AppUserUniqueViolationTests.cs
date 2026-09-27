using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// docs/adr/0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md — AppUserStore trên CoreDbContext THẬT, không database
// (OfflineCoreDbContext + SqlRecorder). Kịch bản tranh chấp: lượt kia CHƯA commit, nên mọi câu đọc của phép kiểm trước và của
// bộ kiểm Identity THẬT (UserValidator) thấy "không trùng"; lệnh ghi của lượt này vỡ index duy nhất. UniqueViolationOnWrite
// ném PostgresException 23505 mang tên ràng buộc ở đúng lệnh INSERT/UPDATE core.app_user — và EF tự bọc nó thành
// DbUpdateException, đúng hình dạng database thật trả.
//
// Thứ nó chứng minh: 23505 trên UserNameIndex / EmailIndex ra CREATE_FAILED / UPDATE_FAILED kèm fieldErrors mã
// USERNAME_DUPLICATED / EMAIL_DUPLICATED và messageParams đúng khoá của card (users.md §5, §6), bản ghi hỏng rời bộ theo dõi;
// tên ràng buộc lạ, mã lỗi khác 23505, hay lỗi không phải của PostgreSQL vẫn ném (không nuốt). Thứ nó KHÔNG chứng minh: hai
// lượt đồng thời thật trên PostgreSQL ra đúng như vậy — AppUserUniqueViolationDatabaseTests (RequiresDocker).
public sealed class AppUserUniqueViolationTests
{
    private const string StrongPassword = "Temp-Passw0rd-9";

    private readonly Guid _tenantId = Guid.NewGuid();

    public static TheoryData<string, string, string, string> CreateRaces => new()
    {
        { AppUserUniqueIndexes.UserName, "UserName", UserErrors.UsernameDuplicated.Code, "binh.tv" },
        { AppUserUniqueIndexes.Email, "Email", UserErrors.EmailDuplicated.Code, "binh@vd.vn" },
    };

    // POST /users (users.md §5 "Hai lượt tạo cùng userName hoặc cùng email chạy đồng thời").
    [Theory]
    [MemberData(nameof(CreateRaces))]
    public async Task Create_UniqueViolationOnTheIndex_IsCreateFailed_WithTheFieldErrorAndItsParam(
        string constraint, string field, string expectedCode, string expectedValue)
    {
        var violation = new UniqueViolationOnWrite("INSERT INTO core.app_user ", PostgresErrorCodes.UniqueViolation, constraint);
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. new SqlRecorder(emptyReaders: true).Interceptors, violation]);
        var service = new UserAdminService(Manager(db), db, TimeProvider.System, Substitute.For<ICurrentUser>());

        var result = await service.CreateAsync(
            new CreateUserInput("binh.tv", "binh@vd.vn", "Trần Văn Bình", StrongPassword), CancellationToken.None);

        violation.Thrown.ShouldBe(1, "chống test rỗng: lệnh ghi phải thật sự tới chỗ vỡ index");
        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.CreateFailed.Code);
        result.Error.FieldErrors.Keys.ShouldBe([field]);
        var fieldError = result.Error.FieldErrors[field].ShouldHaveSingleItem();
        fieldError.Code.ShouldBe(expectedCode);
        fieldError.Params.ShouldContainKeyAndValue(field, expectedValue);
        db.ChangeTracker.Entries<AppUser>().ShouldBeEmpty("bản ghi vừa hỏng phải rời bộ theo dõi — ADR-0089 quyết định 4");
    }

    // PUT /users/{id} (users.md §6 "Ghi chú — sửa email đồng thời"): email trong tham số là email MỚI vừa bị từ chối.
    [Fact]
    public async Task Update_UniqueViolationOnEmailIndex_IsUpdateFailed_WithTheNewEmail()
    {
        var user = TrackedAccount();

        // Câu EXISTS kiểm trùng email của UserAdminService.UpdateAsync: người kia CHƯA commit ⇒ "không trùng". Các câu đọc
        // sau (bộ kiểm Identity) trả rỗng — cũng vì dòng kia chưa commit.
        var notTakenYet = new DataTable();
        notTakenYet.Columns.Add("exists", typeof(bool));
        notTakenYet.Rows.Add(false);

        var violation = new UniqueViolationOnWrite("UPDATE core.app_user ", PostgresErrorCodes.UniqueViolation, AppUserUniqueIndexes.Email);
        await using var db = OfflineCoreDbContext.Create(
            _tenantId, [.. new SqlRecorder(emptyReaders: true, readers: [notTakenYet]).Interceptors, violation]);
        db.Attach(user);
        var service = new UserAdminService(Manager(db, tracked: user), db, TimeProvider.System, Substitute.For<ICurrentUser>());

        var result = await service.UpdateAsync(
            user.Id, new UpdateUserInput("an.moi@vd.vn", "Nguyễn Văn An", user.ConcurrencyStamp), CancellationToken.None);

        violation.Thrown.ShouldBe(1, "chống test rỗng: lệnh ghi phải thật sự tới chỗ vỡ index");
        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.UpdateFailed.Code);
        var fieldError = result.Error.FieldErrors["Email"].ShouldHaveSingleItem();
        fieldError.Code.ShouldBe(UserErrors.EmailDuplicated.Code);
        fieldError.Params.ShouldContainKeyAndValue("Email", "an.moi@vd.vn");
        db.ChangeTracker.Entries<AppUser>().ShouldBeEmpty("bản ghi vừa hỏng phải rời bộ theo dõi — ADR-0089 quyết định 4");
    }

    // Ở mức kho: cả hai lệnh ghi, cả hai index, ra ĐÚNG lỗi bộ kiểm Identity dựng khi tự thấy bản trùng — cùng Code, nên mọi bộ
    // ánh xạ riêng (IdentityErrorMapper, MapFirstAdminCreateErrors, WithPasswordOnlyFieldErrors của tenants.md §6) chạy như cũ.
    [Theory]
    [InlineData(true, AppUserUniqueIndexes.UserName, "DuplicateUserName")]
    [InlineData(true, AppUserUniqueIndexes.Email, "DuplicateEmail")]
    [InlineData(false, AppUserUniqueIndexes.UserName, "DuplicateUserName")]
    [InlineData(false, AppUserUniqueIndexes.Email, "DuplicateEmail")]
    public async Task Store_UniqueViolationOnTheIndex_ReturnsTheIdentityDuplicateError(bool create, string constraint, string identityCode)
    {
        var (store, db, user, violation) = Store(create, PostgresErrorCodes.UniqueViolation, constraint);
        await using var _ = db;

        var result = create ? await store.CreateAsync(user) : await store.UpdateAsync(user);

        violation.Thrown.ShouldBe(1);
        result.Succeeded.ShouldBeFalse();
        var error = result.Errors.ShouldHaveSingleItem();
        var expected = identityCode == "DuplicateUserName"
            ? new IdentityErrorDescriber().DuplicateUserName(user.UserName!)
            : new IdentityErrorDescriber().DuplicateEmail(user.Email!);
        error.Code.ShouldBe(expected.Code);
        error.Description.ShouldBe(expected.Description);
        db.ChangeTracker.Entries<AppUser>().ShouldBeEmpty();
    }

    // Ràng buộc ngoài tập: vẫn là lỗi hệ thống. Gồm hai ràng buộc cùng lớp mà ADR-0089 để ngoài phạm vi (uq_tenant_code,
    // RoleNameIndex), khoá chính, và một tên chỉ khác hoa thường — PostgreSQL giữ nguyên hoa thường của tên có nháy kép.
    [Theory]
    [InlineData("pk_app_user")]
    [InlineData("uq_tenant_code")]
    [InlineData("RoleNameIndex")]
    [InlineData("usernameindex")]
    public async Task Store_UniqueViolationOnAnotherConstraint_IsNotSwallowed(string constraint)
    {
        var (store, db, user, violation) = Store(create: true, PostgresErrorCodes.UniqueViolation, constraint);
        await using var _ = db;

        var thrown = await Should.ThrowAsync<DbUpdateException>(() => store.CreateAsync(user));

        violation.Thrown.ShouldBe(1);
        thrown.InnerException.ShouldBeOfType<PostgresException>().ConstraintName.ShouldBe(constraint);
    }

    // Đúng tên index nhưng không phải vi phạm unique — không phải "trùng", không được dịch thành "trùng".
    [Theory]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    public async Task Store_OtherSqlStateOnAKnownIndex_IsNotSwallowed(string sqlState)
    {
        var (store, db, user, _) = Store(create: false, sqlState, AppUserUniqueIndexes.UserName);
        await using var __ = db;

        var thrown = await Should.ThrowAsync<DbUpdateException>(() => store.UpdateAsync(user));

        thrown.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe(sqlState);
    }

    // Lỗi lưu không đến từ PostgreSQL (đứt kết nối giữa chừng, lỗi lập trình): đi thẳng ra.
    [Fact]
    public async Task Store_NonPostgresFailure_IsNotSwallowed()
    {
        var violation = new UniqueViolationOnWrite("INSERT INTO core.app_user ", sqlState: null, constraint: null);
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. new SqlRecorder().Interceptors, violation]);
        var store = new AppUserStore(db);

        var thrown = await Should.ThrowAsync<DbUpdateException>(() => store.CreateAsync(NewAccount()));

        thrown.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    private (AppUserStore Store, CoreDbContext Db, AppUser User, UniqueViolationOnWrite Violation) Store(
        bool create, string sqlState, string constraint)
    {
        var violation = new UniqueViolationOnWrite(create ? "INSERT INTO core.app_user " : "UPDATE core.app_user ", sqlState, constraint);
        var db = OfflineCoreDbContext.Create(_tenantId, [.. new SqlRecorder().Interceptors, violation]);
        var user = create ? NewAccount() : TrackedAccount();
        if (!create)
            db.Attach(user);

        return (new AppUserStore(db), db, user, violation);
    }

    private AppUser NewAccount()
    {
        var user = AppUser.NewAccount("binh.tv", "binh@vd.vn", "Trần Văn Bình");
        user.NormalizedUserName = "BINH.TV";
        user.NormalizedEmail = "BINH@VD.VN";
        return user;
    }

    private AppUser TrackedAccount() => new()
    {
        TenantId = _tenantId,
        UserName = "an.nguyen",
        NormalizedUserName = "AN.NGUYEN",
        Email = "an@vd.vn",
        NormalizedEmail = "AN@VD.VN",
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
    };

    // UserManager THẬT trên AppUserStore, với bộ kiểm người dùng THẬT của Identity (tra trùng qua database — ở đây trả rỗng)
    // và RequireUniqueEmail = true như AddCoreIdentity.
    private static UserManager<AppUser> Manager(CoreDbContext db, AppUser? tracked = null)
    {
        var options = new IdentityOptions();
        options.User.RequireUniqueEmail = true;
        return new TrackedLookupUserManager(new AppUserStore(db), Options.Create(options), tracked);
    }

    // FindByIdAsync trỏ về bản ghi đang theo dõi — không database.
    private sealed class TrackedLookupUserManager(IUserStore<AppUser> store, IOptions<IdentityOptions> options, AppUser? tracked)
        : UserManager<AppUser>(store, options, new PasswordHasher<AppUser>(),
            [new UserValidator<AppUser>()], [new PasswordValidator<AppUser>()], new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, NullLogger<UserManager<AppUser>>.Instance)
    {
        public override Task<AppUser?> FindByIdAsync(string userId)
            => Task.FromResult(tracked is not null && userId == tracked.Id.ToString() ? tracked : null);
    }

    // Lệnh ghi có văn bản bắt đầu bằng `prefix` vỡ ràng buộc `constraint` với mã `sqlState` — hình dạng PostgreSQL trả khi một
    // lượt khác vừa commit bản trùng. sqlState null ⇒ ném một lỗi KHÔNG phải của PostgreSQL. EF bắt ngoại lệ từ lệnh của lượt
    // lưu và bọc nó thành DbUpdateException, như với lỗi thật từ Npgsql.
    private sealed class UniqueViolationOnWrite(string prefix, string? sqlState, string? constraint) : DbCommandInterceptor
    {
        public int Thrown { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            ThrowIfTargeted(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfTargeted(command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            ThrowIfTargeted(command);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ThrowIfTargeted(command);
            return ValueTask.FromResult(result);
        }

        private void ThrowIfTargeted(DbCommand command)
        {
            if (!command.CommandText.TrimStart().StartsWith(prefix, StringComparison.Ordinal))
                return;

            Thrown++;
            if (sqlState is null)
                throw new InvalidOperationException("Lỗi lưu không đến từ PostgreSQL.");

            throw new PostgresException(
                "duplicate key value violates unique constraint", "ERROR", "ERROR", sqlState,
                schemaName: "core", tableName: "app_user", constraintName: constraint);
        }
    }
}
