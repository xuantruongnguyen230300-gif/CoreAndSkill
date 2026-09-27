using System.Data;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Users;
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

// PUT /users/{id}/roles và POST /users — nợ E17 (docs/DEBT.md), docs/contracts/users.md §5, §7. UserAdminService trên
// CoreDbContext THẬT, không database (OfflineCoreDbContext + SqlRecorder có kịch bản đọc + SaveProbe).
//
// Thứ nó chứng minh: vai trò được THÊM bị khoá dòng core.app_role (FOR KEY SHARE, theo thứ tự id, có mệnh đề tenant_id —
// luật M6) SAU phép so-và-đổi token và TRƯỚC lượt lưu chứa câu INSERT; vai trò vắng mặt dưới khoá ⇒ ROLE_NOT_FOUND và không
// dàn dựng dòng nào; không thêm vai trò nào thì không khoá. Thứ nó KHÔNG chứng minh: khoá thật của PostgreSQL làm lượt gán và
// lượt xoá xếp hàng — RoleAssignDeleteRaceDatabaseTests (RequiresDocker).
public sealed class UserRoleAssignmentLockOrderTests
{
    private const string ClientVersion = "9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d";

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Assign_LocksOnlyTheAddedRoles_AfterTheTokenClaim_AndBeforeTheInsert()
    {
        var kept = Guid.NewGuid();
        var added1 = Guid.NewGuid();
        var added2 = Guid.NewGuid();
        var recorder = new SqlRecorder(nonQueryResult: 1, readers: [RoleIds(kept), RoleIds(added1, added2)]);
        var probe = new SaveProbe(recorder);
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. recorder.Interceptors, probe]);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await Service(db).AssignRolesAsync(_userId, [kept, added2, added1], ClientVersion, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.Error?.Code);

        // Lượt lưu của UnitOfWork sau handler — nơi câu INSERT vào app_user_role thật sự chạy.
        await db.SaveChangesAsync();

        var save = probe.Saves.ShouldHaveSingleItem();
        save.Entities<AppUserRole>(EntityState.Added).Select(r => r.RoleId).ShouldBe([added1, added2], ignoreOrder: true);

        var before = save.ExecutedBefore;
        before.Count.ShouldBe(5, string.Join(" | ", before));
        before[0].ShouldContain("lock_timeout", Case.Sensitive, "06-concurrency-control.md §7 quy tắc 3: thời hạn chờ có trước câu giữ khoá đầu tiên");
        before[1].ShouldStartWith("UPDATE core.app_user ", Case.Sensitive, "phép so-và-đổi token vẫn là câu đầu tiên chạm dữ liệu (ADR-0082)");
        before[2].ShouldContain("core.app_user_role", Case.Sensitive, "đọc tập hiện có để biết vai trò nào được THÊM");
        before[3].ShouldContain("lock_timeout", Case.Sensitive, "RoleRowLocks tự đặt thời hạn chờ — đặt lại cùng giá trị là vô hại");

        var lockSql = before[4];
        AssertLockForAssign(lockSql);
        lockSql.ShouldContain(added1.ToString(), Case.Insensitive);
        lockSql.ShouldContain(added2.ToString(), Case.Insensitive);
        lockSql.ShouldNotContain(kept.ToString(), Case.Insensitive, "vai trò giữ nguyên không cần khoá");
    }

    // Lượt xoá commit trong lúc câu khoá chờ: PostgreSQL bỏ dòng đã xoá khỏi kết quả. Id cố định để thứ tự payload KHÁC thứ tự
    // id: vai trò vắng mặt ĐẦU TIÊN theo payload quyết định messageParams — cùng quy tắc với phép kiểm của handler.
    [Fact]
    public async Task Assign_AddedRoleGoneUnderTheLock_ReturnsRoleNotFound_ForTheFirstMissingInPayloadOrder_AndStagesNothing()
    {
        var id1 = new Guid("00000000-0000-0000-0000-000000000001");
        var id2 = new Guid("00000000-0000-0000-0000-000000000002");
        var id3 = new Guid("00000000-0000-0000-0000-000000000003");
        var recorder = new SqlRecorder(nonQueryResult: 1, readers: [RoleIds(), RoleIds(id1)]);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await Service(db).AssignRolesAsync(_userId, [id3, id2, id1], ClientVersion, CancellationToken.None);

        result.Error!.Code.ShouldBe(UserErrors.RoleNotFound.Code);
        result.Error.Params.ShouldContainKeyAndValue("RoleId", id3.ToString());
        db.ChangeTracker.Entries<AppUserRole>().ShouldBeEmpty();
        recorder.Executed.Count.ShouldBe(5, string.Join(" | ", recorder.Executed));
        AssertLockForAssign(recorder.Executed[4]);
    }

    // Không vai trò nào được thêm — chỉ gỡ, hoặc giữ nguyên — thì không có gì để khoá: lượt xoá đếm người mang dưới khoá của
    // nó và thấy dòng còn đó.
    [Fact]
    public async Task Assign_NothingAdded_TakesNoRoleLock()
    {
        var kept = Guid.NewGuid();
        var recorder = new SqlRecorder(nonQueryResult: 1, readers: [RoleIds(kept)]);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await Service(db).AssignRolesAsync(_userId, [kept], ClientVersion, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        recorder.Executed.Count.ShouldBe(3, string.Join(" | ", recorder.Executed)); // thời hạn chờ, so-và-đổi token, đọc tập hiện có
        recorder.Executed.ShouldNotContain(sql => sql.Contains("core.app_role ", StringComparison.Ordinal));
    }

    // POST /users (§5): mọi vai trò của tài khoản mới đều là vai trò được THÊM — cùng khoá, cùng vị trí trước lượt lưu.
    [Fact]
    public async Task GrantInitialRoles_LocksTheRoles_BeforeTheInsert()
    {
        var roleId = Guid.NewGuid();
        var recorder = new SqlRecorder(readers: [RoleIds(roleId)]);
        var probe = new SaveProbe(recorder);
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. recorder.Interceptors, probe]);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var service = Service(db);
        var created = await service.CreateAsync(NewAccountInput(), CancellationToken.None);
        created.IsSuccess.ShouldBeTrue(created.Error?.Code);

        var result = await service.GrantInitialRolesAsync(created.Value, [roleId], CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        await db.SaveChangesAsync();

        probe.Saves.Count.ShouldBe(2, "lượt lưu của CreateAsync, rồi lượt lưu của UnitOfWork");
        var save = probe.Saves[1];
        save.Entities<AppUserRole>(EntityState.Added).ShouldHaveSingleItem().RoleId.ShouldBe(roleId);
        save.ExecutedBefore.Count.ShouldBe(2, string.Join(" | ", save.ExecutedBefore));
        save.ExecutedBefore[0].ShouldContain("lock_timeout", Case.Sensitive);
        AssertLockForAssign(save.ExecutedBefore[1]);
        save.ExecutedBefore[1].ShouldContain(roleId.ToString(), Case.Insensitive);
    }

    [Fact]
    public async Task GrantInitialRoles_RoleGoneUnderTheLock_ReturnsRoleNotFound_AndStagesNothing()
    {
        var roleId = Guid.NewGuid();
        var recorder = new SqlRecorder(readers: [RoleIds()]);
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. recorder.Interceptors, new SaveCapture()]);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var service = Service(db);
        var created = await service.CreateAsync(NewAccountInput(), CancellationToken.None);
        created.IsSuccess.ShouldBeTrue(created.Error?.Code);

        var result = await service.GrantInitialRolesAsync(created.Value, [roleId], CancellationToken.None);

        result.Error!.Code.ShouldBe(UserErrors.RoleNotFound.Code);
        result.Error.Params.ShouldContainKeyAndValue("RoleId", roleId.ToString());
        db.ChangeTracker.Entries<AppUserRole>().ShouldBeEmpty();
    }

    // Ngoài transaction, khoá dòng nhả ngay sau câu lệnh — bảo vệ biến mất trong im lặng. Ném trước mọi câu SQL.
    [Fact]
    public async Task GrantInitialRoles_OutsideATransaction_Throws_BeforeAnySql()
    {
        var recorder = new SqlRecorder(readers: [RoleIds()]);
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. recorder.Interceptors, new SaveCapture()]);
        var service = Service(db);
        var created = await service.CreateAsync(NewAccountInput(), CancellationToken.None);
        created.IsSuccess.ShouldBeTrue(created.Error?.Code);

        await Should.ThrowAsync<InvalidOperationException>(
            () => service.GrantInitialRolesAsync(created.Value, [Guid.NewGuid()], CancellationToken.None));

        recorder.Executed.ShouldBeEmpty();
        db.ChangeTracker.Entries<AppUserRole>().ShouldBeEmpty();
    }

    private void AssertLockForAssign(string sql)
    {
        sql.ShouldContain("FROM core.app_role", Case.Sensitive);
        // Chế độ yếu nhất còn xung đột với FOR UPDATE của lượt xoá — RoleRowLocks.
        sql.ShouldContain("FOR KEY SHARE", Case.Sensitive, "khoá dòng vai trò sắp gán");
        sql.ShouldContain("ORDER BY id", Case.Sensitive, "06-concurrency-control.md §7 quy tắc 1: khoá nhiều dòng theo thứ tự id");
        Where(sql).ShouldContain("tenant_id = @", Case.Sensitive, "luật M6: mệnh đề đơn vị viết trong chính câu SqlQuery");
        sql.ShouldContain(_tenantId.ToString(), Case.Insensitive, "tham số đơn vị là đơn vị hiện hành");
    }

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

    private static CreateUserInput NewAccountInput() => new("an", "an@dv.vn", "Nguyễn Văn An", "MatKhauTam!2026");

    // Một cột: câu đọc tập hiện có chiếu role_id; câu khoá chiếu id AS "Value" (quy ước của Database.SqlQuery<T>). Cả hai đọc
    // theo vị trí cột đầu tiên, nên dùng chung một khuôn — tên cột đặt theo câu khoá.
    private static DataTable RoleIds(params Guid[] ids)
    {
        var table = new DataTable();
        table.Columns.Add("Value", typeof(Guid));
        foreach (var id in ids)
            table.Rows.Add(id);
        return table;
    }

    private static string Where(string sql)
    {
        var at = sql.IndexOf("WHERE", StringComparison.Ordinal);
        at.ShouldBeGreaterThanOrEqualTo(0, sql);
        return sql[at..];
    }
}
