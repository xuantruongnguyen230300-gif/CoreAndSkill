using System.Data;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Domain.Permissions;
using CoreAndSkill.Core.Infrastructure.Permissions;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// PUT /permissions/matrix — nợ E17 áp cho ma trận quyền (docs/DEBT.md), docs/contracts/permissions.md §6. role_permission.role_id
// là ON DELETE CASCADE như app_user_role.role_id. PermissionMatrixService trên CoreDbContext THẬT, không database
// (OfflineCoreDbContext + SqlRecorder có kịch bản đọc + SaveProbe).
//
// Thứ nó chứng minh: sau khoá tư vấn của ma trận và TRƯỚC mọi câu đọc khác, service khoá dòng MỌI vai trò của đơn vị (FOR KEY
// SHARE, theo thứ tự id, có mệnh đề tenant_id — luật M6); phép kiểm "vai trò tồn tại" đọc từ tập đã khoá, không đọc lại
// app_role ngoài khoá; vai trò vắng mặt dưới khoá ⇒ ROLE_NOT_FOUND và không dàn dựng dòng nào; khoá đứng trước lượt lưu chứa
// câu INSERT. Thứ nó KHÔNG chứng minh: khoá thật của PostgreSQL làm lượt ghi ma trận và lượt xoá vai trò xếp hàng —
// PermissionMatrixRoleDeleteRaceDatabaseTests (RequiresDocker).
public sealed class PermissionMatrixRoleLockTests
{
    // Băm của tập (vai trò, quyền) rỗng — docs/contracts/permissions.md §3.3: "sha256:" + 8 ký tự hex đầu của SHA-256("").
    private const string EmptyMatrixVersion = "sha256:e3b0c442";

    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task ReplaceMatrix_LocksEveryRoleOfTheTenant_AfterTheMatrixLock_BeforeAnyOtherRead()
    {
        var recorder = new SqlRecorder(readers: [Ids(Guid.NewGuid())]);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        // Câu đọc sau câu khoá (danh mục quyền) hết kịch bản và ném — tới đó đã đủ trình tự cần xem.
        await Should.ThrowAsync<SqlRecorder.ReaderReachedException>(
            () => new PermissionMatrixService(db).ReplaceMatrixAsync(EmptyMatrixVersion, Entries(), CancellationToken.None));

        var executed = recorder.Executed;
        var matrixLock = IndexOf(executed, "pg_advisory_xact_lock");
        var roleLock = IndexOf(executed, "FOR KEY SHARE");
        roleLock.ShouldBeGreaterThan(matrixLock, "khoá tư vấn của ma trận TRƯỚC, khoá dòng vai trò SAU — lý do ở PermissionMatrixService");
        roleLock.ShouldBe(executed.Count - 2, $"khoá dòng vai trò là câu ngay trước câu đọc đầu tiên. Trình tự: {Trace(recorder)}");
        executed.Take(roleLock).ShouldContain(sql => sql.Contains("lock_timeout", StringComparison.Ordinal));
        executed[^1].ShouldContain("core.permission", Case.Sensitive, "câu đọc đầu tiên sau khoá: danh mục quyền");

        AssertLocksEveryRoleOfTheTenant(executed[roleLock]);
    }

    // Lượt xoá vai trò commit trong lúc câu khoá chờ: PostgreSQL bỏ dòng đã xoá khỏi kết quả — vai trò vắng mặt trong tập khoá.
    [Fact]
    public async Task ReplaceMatrix_GrantToARoleGoneUnderTheLock_ReturnsRoleNotFound_WithoutReadingAppRoleAgain_AndStagesNothing()
    {
        var kept = Guid.NewGuid();
        var gone = Guid.NewGuid();
        var permission = Guid.NewGuid();
        var recorder = new SqlRecorder(readers: [Ids(kept), Ids(permission)]);
        await using var db = OfflineCoreDbContext.Create(_tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await new PermissionMatrixService(db).ReplaceMatrixAsync(
            EmptyMatrixVersion, Entries((permission, [kept, gone])), CancellationToken.None);

        result.Error!.Code.ShouldBe(PermissionErrors.RoleNotFound.Code);
        result.Error.Params.ShouldContainKeyAndValue("RoleId", gone.ToString());
        db.ChangeTracker.Entries<RolePermission>().ShouldBeEmpty();

        var roleLock = IndexOf(recorder.Executed, "FOR KEY SHARE");
        recorder.Executed.Skip(roleLock + 1).ShouldNotContain(
            sql => sql.Contains("core.app_role", StringComparison.Ordinal),
            $"phép kiểm tồn tại đọc từ tập đã khoá — một câu đọc app_role thứ hai ngoài khoá mở lại khe hở E17. Trình tự: {Trace(recorder)}");
    }

    [Fact]
    public async Task ReplaceMatrix_Grant_TheRoleLockRunsBeforeTheInsert()
    {
        var role = Guid.NewGuid();
        var permission = Guid.NewGuid();
        var recorder = new SqlRecorder(readers:
        [
            Ids(role),             // khoá dòng vai trò
            Ids(permission),       // danh mục quyền
            Grants(),              // ma trận hiện có — rỗng
            Ids(),                 // vai trò hệ thống — không có
        ]);
        var probe = new SaveProbe(recorder);
        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. recorder.Interceptors, probe]);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await new PermissionMatrixService(db).ReplaceMatrixAsync(
            EmptyMatrixVersion, Entries((permission, [role])), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.Error?.Code);

        // Lượt lưu của UnitOfWork sau handler — nơi câu INSERT vào role_permission thật sự chạy.
        await db.SaveChangesAsync();

        var save = probe.Saves.ShouldHaveSingleItem();
        var granted = save.Entities<RolePermission>(EntityState.Added).ShouldHaveSingleItem();
        granted.RoleId.ShouldBe(role);
        granted.PermissionId.ShouldBe(permission);
        AssertLocksEveryRoleOfTheTenant(save.ExecutedBefore[IndexOf(save.ExecutedBefore, "FOR KEY SHARE")]);
    }

    private void AssertLocksEveryRoleOfTheTenant(string sql)
    {
        sql.ShouldContain("FROM core.app_role", Case.Sensitive);
        sql.ShouldContain("ORDER BY id", Case.Sensitive, "06-concurrency-control.md §7 quy tắc 1: khoá nhiều dòng theo thứ tự id");
        Where(sql).ShouldContain("tenant_id = @", Case.Sensitive, "luật M6: mệnh đề đơn vị viết trong chính câu SqlQuery");
        sql.ShouldContain(_tenantId.ToString(), Case.Insensitive, "tham số đơn vị là đơn vị hiện hành");
        sql.ShouldNotContain("ANY(", Case.Sensitive, "PUT ma trận là thay thế toàn bộ — khoá mọi vai trò của đơn vị, không một tập con");
    }

    private static IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> Entries(params (Guid PermissionId, Guid[] RoleIds)[] rows)
        => rows.ToDictionary(r => r.PermissionId, r => (IReadOnlyList<Guid>)r.RoleIds);

    private static int IndexOf(IReadOnlyList<string> executed, string fragment)
    {
        var index = executed.ToList().FindIndex(sql => sql.Contains(fragment, StringComparison.Ordinal));
        index.ShouldBeGreaterThanOrEqualTo(0, $"không có câu chứa '{fragment}'. Trình tự: {string.Join(" | ", executed)}");
        return index;
    }

    // Một cột Guid — câu khoá chiếu id AS "Value" (quy ước của Database.SqlQuery<T>); các câu LINQ đọc theo vị trí cột.
    private static DataTable Ids(params Guid[] ids)
    {
        var table = new DataTable();
        table.Columns.Add("Value", typeof(Guid));
        foreach (var id in ids)
            table.Rows.Add(id);
        return table;
    }

    private static DataTable Grants()
    {
        var table = new DataTable();
        table.Columns.Add("role_id", typeof(Guid));
        table.Columns.Add("permission_id", typeof(Guid));
        return table;
    }

    private static string Trace(SqlRecorder recorder) => string.Join(" | ", recorder.Executed);

    private static string Where(string sql)
    {
        var at = sql.IndexOf("WHERE", StringComparison.Ordinal);
        at.ShouldBeGreaterThanOrEqualTo(0, sql);
        return sql[at..];
    }
}
