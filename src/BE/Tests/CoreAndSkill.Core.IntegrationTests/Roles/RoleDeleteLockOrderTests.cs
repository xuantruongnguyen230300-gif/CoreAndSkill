using System.Data;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Roles;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Roles;

// DELETE /roles/{id} — nợ E17 (docs/DEBT.md), docs/contracts/roles.md §4. RoleAdminService.DeleteAsync trên RoleManager +
// RoleStore THẬT, CoreDbContext THẬT, không database (OfflineCoreDbContext + SqlRecorder có kịch bản đọc + SaveProbe).
//
// Thứ nó chứng minh: câu khoá dòng core.app_role (FOR UPDATE, có mệnh đề tenant_id — luật M6) chạy TRƯỚC câu đếm người mang,
// câu đếm chạy TRƯỚC câu DELETE, và mọi phép kiểm ra mã lỗi đọc từ dòng ĐÃ KHOÁ; có thời hạn chờ khoá đứng trước câu khoá.
// Thứ nó KHÔNG chứng minh: khoá thật của PostgreSQL làm lượt gán và lượt xoá xếp hàng — RoleAssignDeleteRaceDatabaseTests
// (RequiresDocker).
//
// FindByIdAsync của RoleManager là truy vấn DB, nên được trỏ về bản ghi đang theo dõi (TrackedLookupRoleManager) — nó không
// nằm trong trình tự SQL mà test này khẳng định.
public sealed class RoleDeleteLockOrderTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Delete_LocksTheRoleRow_ThenCountsHolders_ThenDeletes()
    {
        var (service, role, recorder, probe, db) = Build(Locked(isSystem: false), Count(0));
        await using var _ = db;
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await service.DeleteAsync(role.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        var save = probe.Saves.ShouldHaveSingleItem("RoleStore.DeleteAsync lưu đúng một lượt");
        save.Entities<AppRole>(EntityState.Deleted).ShouldHaveSingleItem().Id.ShouldBe(role.Id);

        var before = save.ExecutedBefore;
        before.Count.ShouldBe(3, Trace(recorder));
        before[0].ShouldContain("lock_timeout", Case.Sensitive, "06-concurrency-control.md §7 quy tắc 3: luôn có thời hạn chờ");
        AssertLockForDelete(before[1], role.Id);
        before[2].ShouldContain("core.app_user_role", Case.Sensitive, "đếm người mang chạy SAU câu khoá, TRƯỚC câu DELETE");
        before[2].ShouldContain("count(", Case.Insensitive);
    }

    [Fact]
    public async Task Delete_RoleStillHeld_ReturnsInUse_WithTheCountReadAfterTheLock_AndDeletesNothing()
    {
        var (service, role, recorder, probe, db) = Build(Locked(isSystem: false), Count(2));
        await using var _ = db;
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await service.DeleteAsync(role.Id, CancellationToken.None);

        result.Error!.Code.ShouldBe(RoleErrors.InUse.Code);
        result.Error.Params.ShouldContainKeyAndValue("Count", "2");
        probe.Saves.ShouldBeEmpty();
        recorder.Executed.Count.ShouldBe(3, Trace(recorder));
        AssertLockForDelete(recorder.Executed[1], role.Id);
        recorder.Executed[2].ShouldContain("core.app_user_role", Case.Sensitive);
    }

    // Lượt xoá khác commit trong lúc câu khoá chờ: PostgreSQL bỏ dòng đã xoá khỏi kết quả — câu khoá trả rỗng.
    [Fact]
    public async Task Delete_RowGoneWhenTheLockIsGranted_ReturnsNotFound_WithoutCountingOrDeleting()
    {
        var (service, role, recorder, probe, db) = Build(NoLockedRow());
        await using var _ = db;
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await service.DeleteAsync(role.Id, CancellationToken.None);

        result.Error!.Code.ShouldBe(RoleErrors.NotFound.Code);
        probe.Saves.ShouldBeEmpty();
        recorder.Executed.Count.ShouldBe(2, Trace(recorder));
        AssertLockForDelete(recorder.Executed[1], role.Id);
    }

    // Cờ hệ thống đọc từ dòng ĐÃ KHOÁ, không từ bản ghi đang theo dõi: bản theo dõi ở đây nói "không phải hệ thống".
    [Fact]
    public async Task Delete_SystemFlagReadFromTheLockedRow_ReturnsSystemImmutable_WithoutCounting()
    {
        var (service, role, recorder, probe, db) = Build(Locked(isSystem: true));
        await using var _ = db;
        await using var transaction = await db.Database.BeginTransactionAsync();
        role.IsSystem.ShouldBeFalse("chống test rỗng: mã lỗi phải đến từ dòng đã khoá");

        var result = await service.DeleteAsync(role.Id, CancellationToken.None);

        result.Error!.Code.ShouldBe(RoleErrors.SystemImmutable.Code);
        probe.Saves.ShouldBeEmpty();
        recorder.Executed.Count.ShouldBe(2, Trace(recorder));
    }

    // Ngoài transaction, khoá dòng nhả ngay sau câu lệnh — bảo vệ biến mất trong im lặng. Ném trước mọi câu SQL.
    [Fact]
    public async Task Delete_OutsideATransaction_Throws_BeforeAnySql()
    {
        var (service, role, recorder, probe, db) = Build(Locked(isSystem: false), Count(0));
        await using var _ = db;

        await Should.ThrowAsync<InvalidOperationException>(() => service.DeleteAsync(role.Id, CancellationToken.None));

        recorder.Executed.ShouldBeEmpty();
        probe.Saves.ShouldBeEmpty();
    }

    private void AssertLockForDelete(string sql, Guid roleId)
    {
        sql.ShouldContain("FROM core.app_role", Case.Sensitive);
        // FOR UPDATE là chế độ duy nhất xung đột với FOR KEY SHARE của đường gán. "FOR NO KEY UPDATE" không chứa chuỗi này.
        sql.ShouldContain("FOR UPDATE", Case.Sensitive, "khoá dòng vai trò để xoá");
        Where(sql).ShouldContain("tenant_id = @", Case.Sensitive, "luật M6: mệnh đề đơn vị viết trong chính câu SqlQuery");
        sql.ShouldContain(_tenantId.ToString(), Case.Insensitive, "tham số đơn vị là đơn vị hiện hành");
        sql.ShouldContain(roleId.ToString(), Case.Insensitive);
    }

    private (RoleAdminService Service, AppRole Role, SqlRecorder Recorder, SaveProbe Probe, CoreDbContext Db) Build(
        params DataTable[] readers)
    {
        var role = new AppRole { TenantId = _tenantId, Name = "Kế toán", NormalizedName = "KẾ TOÁN" };
        var recorder = new SqlRecorder(readers: readers);
        var probe = new SaveProbe(recorder);

        var db = OfflineCoreDbContext.Create(_tenantId, [.. recorder.Interceptors, probe]);
        db.Attach(role);

        var store = new RoleStore<AppRole, CoreDbContext, Guid, AppUserRole, AppRoleClaim>(db);
        return (new RoleAdminService(new TrackedLookupRoleManager(store, role), db), role, recorder, probe, db);
    }

    // Cột "Value" — quy ước của Database.SqlQuery<T> cho kiểu vô hướng.
    private static DataTable Locked(bool isSystem)
    {
        var table = new DataTable();
        table.Columns.Add("Value", typeof(bool));
        table.Rows.Add(isSystem);
        return table;
    }

    private static DataTable NoLockedRow()
    {
        var table = new DataTable();
        table.Columns.Add("Value", typeof(bool));
        return table;
    }

    private static DataTable Count(int count)
    {
        var table = new DataTable();
        table.Columns.Add("c", typeof(int));
        table.Rows.Add(count);
        return table;
    }

    private static string Trace(SqlRecorder recorder) => string.Join(" | ", recorder.Executed);

    private static string Where(string sql)
    {
        var at = sql.IndexOf("WHERE", StringComparison.Ordinal);
        at.ShouldBeGreaterThanOrEqualTo(0, sql);
        return sql[at..];
    }

    private sealed class TrackedLookupRoleManager(IRoleStore<AppRole> store, AppRole role)
        : RoleManager<AppRole>(store, [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            NullLogger<RoleManager<AppRole>>.Instance)
    {
        public override Task<AppRole?> FindByIdAsync(string roleId)
            => Task.FromResult<AppRole?>(roleId == role.Id.ToString() ? role : null);
    }
}
