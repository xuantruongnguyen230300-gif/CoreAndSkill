using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Users;

// PUT /users/{id}/roles — docs/contracts/users.md §7 mục "Ghi chú — đồng thời",
// docs/adr/0082-gan-vai-tro-dung-token-cua-tai-khoan.md. UserAdminService.AssignRolesAsync trên CoreDbContext THẬT, không
// database (OfflineCoreDbContext + SqlRecorder).
//
// Thứ nó chứng minh: phép so-và-đổi token là MỘT câu UPDATE có điều kiện trên core.app_user, là câu ĐẦU TIÊN chạm dữ liệu —
// sau đúng một câu đặt thời hạn chờ khoá, trước cả lần đọc tập vai trò hiện có, nên trước mọi thay đổi app_user_role, bất kể
// tập đích có khác tập hiện có không; 0 dòng ⇒ 409 và không dàn dựng dòng vai trò nào. Thứ nó KHÔNG chứng minh: khoá dòng thật của PostgreSQL khiến hai PUT đồng thời xếp hàng —
// UserRoleAssignmentDatabaseTests (RequiresDocker).
public sealed class UserRoleAssignmentVersionTests
{
    private const string ClientVersion = "9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d";

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private static UserAdminService Service(CoreDbContext db)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserName.Returns("quantri");
        var store = new UserStore<AppUser, AppRole, CoreDbContext, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(db);
        var manager = new UserManager<AppUser>(
            store, Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(), [], [], new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, NullLogger<UserManager<AppUser>>.Instance);

        return new UserAdminService(manager, db, TimeProvider.System, currentUser);
    }

    [Fact]
    public async Task VersionMatches_TheConditionalUpdateOnTheAccount_RunsBeforeAnythingTouchesUserRoles()
    {
        var recorder = new SqlRecorder(nonQueryResult: 1);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        // Sau câu UPDATE khớp, service đọc tập hiện có — lệnh đọc đầu tiên ném, tới đó đã đủ trình tự cần xem.
        await Should.ThrowAsync<SqlRecorder.ReaderReachedException>(
            () => Service(db).AssignRolesAsync(_userId, [Guid.NewGuid()], ClientVersion, CancellationToken.None));

        recorder.Executed.Count.ShouldBe(3, string.Join(" | ", recorder.Executed));
        recorder.Executed[0].ShouldContain("lock_timeout", Case.Sensitive,
            "06-concurrency-control.md §7 quy tắc 3: thời hạn chờ có TRƯỚC câu giữ khoá đầu tiên — câu UPDATE là chỗ PUT thứ hai chờ");
        var claim = recorder.Executed[1];
        claim.ShouldStartWith("UPDATE core.app_user ", Case.Sensitive, "câu đầu tiên chạm dữ liệu phải là phép so-và-đổi token trên tài khoản");
        claim.ShouldContain("concurrency_stamp = @", Case.Sensitive, "đổi token trong CÙNG câu với phép so");
        Where(claim).ShouldContain(".concurrency_stamp = @", Case.Sensitive, "phép so nằm trong WHERE, không phải đọc-rồi-so");
        Where(claim).ShouldContain(".tenant_id = @", Case.Sensitive, "câu đi qua bộ lọc đơn vị (luật M6)");
        claim.ShouldContain(ClientVersion, Case.Sensitive, "token được so là token client gửi lên");
        claim.ShouldContain(_tenantId.ToString(), Case.Insensitive);
        claim.ShouldContain(_userId.ToString(), Case.Insensitive);

        recorder.Executed[2].ShouldContain("core.app_user_role", Case.Sensitive, "đọc tập vai trò hiện có chạy SAU phép so");
    }

    // 0 dòng: token lệch — hoặc một PUT khác vừa commit sau khi câu này chờ khoá dòng của nó. 409, không đọc, không ghi.
    [Fact]
    public async Task VersionDoesNotMatch_ReturnsConflict_AfterTheSingleUpdate_AndStagesNoUserRoleChange()
    {
        var recorder = new SqlRecorder(nonQueryResult: 0);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await Service(db).AssignRolesAsync(_userId, [Guid.NewGuid()], "stamp-cu", CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        ShouldBeOnlyTheTimeoutAndTheClaim(recorder.Executed);
        db.ChangeTracker.Entries<AppUserRole>().ShouldBeEmpty();
    }

    // Tập đích rỗng — trùng tập hiện có của một người chưa có vai trò nào, tức ca "không đổi vai trò nào". Phép so vẫn chạy
    // (card §7 ràng buộc 3): service không được quyết "không có gì đổi" trước khi so.
    [Fact]
    public async Task EmptyTargetSet_StillRunsTheComparison()
    {
        var recorder = new SqlRecorder(nonQueryResult: 0);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await Service(db).AssignRolesAsync(_userId, [], "stamp-cu", CancellationToken.None);

        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        ShouldBeOnlyTheTimeoutAndTheClaim(recorder.Executed);
    }

    // null không bao giờ khớp — và EF dịch "== null" thành IS NULL, nên phép chặn đứng trước khi dựng câu.
    [Fact]
    public async Task MissingVersion_ReturnsConflict_WithoutAnySql()
    {
        var recorder = new SqlRecorder(nonQueryResult: 1);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await Service(db).AssignRolesAsync(_userId, [Guid.NewGuid()], null, CancellationToken.None);

        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        recorder.Executed.ShouldBeEmpty();
    }

    // Ngoài transaction, khoá dòng nhả ngay sau câu UPDATE và token đã đổi dù phần ghi vai trò sau đó hỏng.
    [Fact]
    public async Task OutsideATransaction_Throws_BeforeAnySql()
    {
        var recorder = new SqlRecorder(nonQueryResult: 1);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);

        await Should.ThrowAsync<InvalidOperationException>(
            () => Service(db).AssignRolesAsync(_userId, [Guid.NewGuid()], ClientVersion, CancellationToken.None));

        recorder.Executed.ShouldBeEmpty();
    }

    // Đường của POST /users (§5): tài khoản do CreateAsync của CHÍNH service này tạo — không token, một dòng cho mỗi vai trò
    // khác nhau. SaveCapture chặn lượt SaveChanges của UserStore.CreateAsync trước database và chấp nhận thay đổi, như một
    // lượt lưu thành công: tài khoản vừa tạo nằm lại trong bộ theo dõi ở trạng thái Unchanged. Câu SQL duy nhất là phần khoá
    // dòng vai trò (nợ E17 — UserRoleAssignmentLockOrderTests); không có câu so-và-đổi token nào trên core.app_user.
    [Fact]
    public async Task GrantInitialRoles_ForAnAccountCreatedByThisService_StagesOneRowPerDistinctRole_WithoutATokenClaim()
    {
        var roleId = Guid.NewGuid();
        var lockedRoles = new System.Data.DataTable();
        lockedRoles.Columns.Add("Value", typeof(Guid));
        lockedRoles.Rows.Add(roleId);
        var recorder = new SqlRecorder(readers: [lockedRoles]);
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. recorder.Interceptors, new SaveCapture()]);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var service = Service(db);
        var created = await service.CreateAsync(NewAccountInput("an", "an@dv.vn"), CancellationToken.None);
        created.IsSuccess.ShouldBeTrue(created.Error?.Code);

        var result = await service.GrantInitialRolesAsync(created.Value, [roleId, roleId], CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        db.ChangeTracker.Entries<AppUserRole>().ShouldHaveSingleItem().State.ShouldBe(EntityState.Added);
        recorder.Executed.ShouldNotContain(sql => sql.StartsWith("UPDATE core.app_user ", StringComparison.Ordinal));
        recorder.Executed.ShouldAllBe(sql => !sql.Contains("core.app_user_role", StringComparison.Ordinal));
    }

    // Tài khoản cũ vừa được đọc CÓ theo dõi (FindByIdAsync) nằm trong ChangeTracker ở trạng thái Unchanged — đúng trạng thái
    // của một tài khoản vừa tạo sau SaveChanges, nên "có trong bộ theo dõi" không phân biệt được hai ca. Service đã tạo một
    // tài khoản KHÁC trong phạm vi này: phép chặn phải theo đúng Id, không theo "đã từng tạo gì đó". Ném, không dàn dựng gì —
    // đổi vai trò của tài khoản đã có đi qua AssignRolesAsync và phép so token (ADR-0082).
    [Fact]
    public async Task GrantInitialRoles_ForAnExistingAccountTrackedInThisScope_Throws_AndStagesNothing()
    {
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. new SqlRecorder().Interceptors, new SaveCapture()]);
        var service = Service(db);
        var created = await service.CreateAsync(NewAccountInput("an", "an@dv.vn"), CancellationToken.None);
        created.IsSuccess.ShouldBeTrue(created.Error?.Code);
        var existing = AppUser.NewAccount("binh", "binh@dv.vn", "Trần Văn Bình");
        db.Attach(existing); // như sau userManager.FindByIdAsync: đọc có theo dõi, trạng thái Unchanged
        db.Entry(existing).State.ShouldBe(EntityState.Unchanged);

        await Should.ThrowAsync<InvalidOperationException>(
            () => service.GrantInitialRolesAsync(existing.Id, [Guid.NewGuid()], CancellationToken.None));

        db.ChangeTracker.Entries<AppUserRole>().ShouldBeEmpty();
    }

    // Không phải đường vòng qua phép so token: tài khoản không do phạm vi này tạo thì ném, không dàn dựng gì.
    [Fact]
    public async Task GrantInitialRoles_ForAnAccountNotCreatedInThisScope_Throws_AndStagesNothing()
    {
        await using var db = OfflineCoreDbContext.Create(_tenantId, new SqlRecorder().Interceptors);

        await Should.ThrowAsync<InvalidOperationException>(
            () => Service(db).GrantInitialRolesAsync(_userId, [Guid.NewGuid()], CancellationToken.None));

        db.ChangeTracker.Entries<AppUserRole>().ShouldBeEmpty();
    }

    private static CreateUserInput NewAccountInput(string userName, string email)
        => new(userName, email, "Nguyễn Văn An", "MatKhauTam!2026");

    // Đúng hai câu: đặt thời hạn chờ khoá, rồi phép so-và-đổi token. 0 dòng thì dừng — không đọc, không khoá gì thêm.
    private static void ShouldBeOnlyTheTimeoutAndTheClaim(IReadOnlyList<string> executed)
    {
        executed.Count.ShouldBe(2, string.Join(" | ", executed));
        executed[0].ShouldContain("lock_timeout", Case.Sensitive);
        executed[1].ShouldStartWith("UPDATE core.app_user ", Case.Sensitive);
    }

    private static string Where(string sql)
    {
        var at = sql.IndexOf("WHERE", StringComparison.Ordinal);
        at.ShouldBeGreaterThanOrEqualTo(0, sql);
        return sql[at..];
    }
}
