using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Audit;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Execution;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Hai đường đặt lại mật khẩu hộ của TenantProvisioningService — lệnh `core reset-operator-password` (ADR-0023 §5) và
// POST /system/tenants/{id}/recovery-reset-password (docs/contracts/tenants.md §4) — chạy cùng một chuỗi lệnh ghi qua
// UserManager: gỡ mật khẩu, đặt mật khẩu, lưu cờ phải đổi mật khẩu, đổi security stamp. Mỗi lệnh tự lưu, và UserStore trả
// xung đột đồng thời dưới dạng IdentityResult thất bại, không ném. Lệnh nào hỏng thì đường đó dừng NGAY: không lệnh ghi nào
// chạy sau, không dòng nhật ký nào được thêm, và trả mã lỗi của chính đường đó để người gọi rollback (ADR-0092 điều kiện a).
//
// Không database: CoreDbContext thật trên OfflineCoreDbContext; câu tra đơn vị trả một dòng dựng tay; UserManager có kịch
// bản (ScriptedWriteUserManager) quyết lệnh ghi nào hỏng và ghi lại thứ tự lệnh ghi.
public sealed class PasswordResetWriteFailureTests
{
    private const string NewPassword = "Passw0rd-Tam1";

    private static readonly string[] AllWrites =
    [
        nameof(ScriptedWriteUserManager.RemovePasswordAsync),
        nameof(ScriptedWriteUserManager.AddPasswordAsync),
        nameof(ScriptedWriteUserManager.UpdateAsync),
        nameof(ScriptedWriteUserManager.UpdateSecurityStampAsync),
    ];

    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly SaveCapture _capture = new();

    public static TheoryData<string> FailingWrite => [.. AllWrites];

    // ===== core reset-operator-password =====

    [Theory]
    [MemberData(nameof(FailingWrite))]
    public async Task ResetOperator_WriteFails_StopsThere_ReturnsResetFailed(string failAt)
    {
        var users = new ScriptedWriteUserManager(AppUser.NewAccount("vanhanh", "vanhanh@dv.vn", "Vận hành", isSystemOperator: true), failAt);
        var (service, db) = Build(users, isSystemTenant: true);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.ResetOperatorPasswordAsync("vanhanh", NewPassword, CancellationToken.None);

        result.Error.ShouldNotBeNull().Code.ShouldBe(TenantProvisioningErrors.OperatorPasswordResetFailed.Code);
        users.Writes.ShouldBe(WritesUpTo(failAt), Case.Sensitive, "lệnh ghi hỏng là lệnh ghi CUỐI CÙNG được gọi");
    }

    // Đối chứng chống test rỗng: cùng bộ dựng, không lệnh nào hỏng ⇒ đủ bốn lệnh ghi, đúng thứ tự, và thành công.
    [Fact]
    public async Task ResetOperator_AllWritesSucceed_RunsTheWholeChain()
    {
        var users = new ScriptedWriteUserManager(AppUser.NewAccount("vanhanh", "vanhanh@dv.vn", "Vận hành", isSystemOperator: true));
        var (service, db) = Build(users, isSystemTenant: true);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.ResetOperatorPasswordAsync("vanhanh", NewPassword, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        users.Writes.ShouldBe(AllWrites);
    }

    // ===== POST /system/tenants/{id}/recovery-reset-password =====

    [Theory]
    [MemberData(nameof(FailingWrite))]
    public async Task RecoveryReset_WriteFails_StopsThere_WritesNoAuditRow_ReturnsResetFailed(string failAt)
    {
        var users = new ScriptedWriteUserManager(AppUser.NewAccount("quantri", "quantri@dv.vn", "Quản trị", hasPermissionBypass: true), failAt);
        var (service, db) = Build(users, isSystemTenant: false);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.RecoveryResetAdminPasswordAsync(_tenantId, "quantri", NewPassword, CancellationToken.None);

        result.Error.ShouldNotBeNull().Code.ShouldBe(TenantProvisioningErrors.RecoveryResetFailed.Code);
        users.Writes.ShouldBe(WritesUpTo(failAt), Case.Sensitive, "lệnh ghi hỏng là lệnh ghi CUỐI CÙNG được gọi");
        db.ChangeTracker.Entries<AuditLog>().ShouldBeEmpty("không dòng nhật ký nào cho một lần đặt lại không xảy ra");
        _capture.AuditRows.ShouldBeEmpty("không lượt lưu nào sau lệnh ghi hỏng");
    }

    // Đối chứng: không lệnh nào hỏng ⇒ đủ bốn lệnh ghi và dòng nhật ký của thao tác được lưu.
    [Fact]
    public async Task RecoveryReset_AllWritesSucceed_RunsTheWholeChain_AndWritesTheAuditRow()
    {
        var users = new ScriptedWriteUserManager(AppUser.NewAccount("quantri", "quantri@dv.vn", "Quản trị", hasPermissionBypass: true));
        var (service, db) = Build(users, isSystemTenant: false);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.RecoveryResetAdminPasswordAsync(_tenantId, "quantri", NewPassword, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        users.Writes.ShouldBe(AllWrites);
        _capture.AuditRows.ShouldContain(r => r.ActionCode == AuditActionCodes.TenantAdminRecoveryReset);
    }

    private static string[] WritesUpTo(string failAt) => [.. AllWrites.Take(Array.IndexOf(AllWrites, failAt) + 1)];

    private (TenantProvisioningService Service, CoreDbContext Db) Build(ScriptedWriteUserManager users, bool isSystemTenant)
    {
        var recorder = new SqlRecorder();
        var db = OfflineCoreDbContext.Create(null, [.. recorder.Interceptors, new TenantRowReader(_tenantId, isSystemTenant), _capture]);

        var service = new TenantProvisioningService(
            new ExecutionContextScope(), db, users, null!, [], [],
            Substitute.For<ICurrentUser>(), Substitute.For<ITenantContext>(), Substitute.For<IClientAddressAccessor>(),
            TimeProvider.System, new CrossTenantActorScope(), NullLogger<TenantProvisioningService>.Instance);

        return (service, db);
    }

    // Trả MỘT dòng core.tenant cho câu tra đơn vị — cột theo đúng thứ tự trong câu SELECT mà EF phát ra. Hai đường này không
    // đọc gì khác (tài khoản đích mang cờ bypass nên phép kiểm vai trò không chạy); câu đọc lạ thì ném để test không xanh nhầm.
    private sealed class TenantRowReader(Guid tenantId, bool isSystem) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(Read(command.CommandText)));

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            => InterceptionResult<DbDataReader>.SuppressWithResult(Read(command.CommandText));

        private DbDataReader Read(string sql)
        {
            if (!sql.Contains("FROM core.tenant", StringComparison.Ordinal))
                throw new InvalidOperationException($"Câu đọc ngoài dự kiến: {sql}");

            var values = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["id"] = tenantId,
                ["code"] = isSystem ? "HE-THONG" : "DV-DICH",
                ["name"] = isSystem ? "Đơn vị hệ thống" : "Đơn vị đích",
                ["is_active"] = true,
                ["is_system"] = isSystem,
                ["created_at"] = DBNull.Value,
                ["created_by"] = DBNull.Value,
                ["updated_at"] = DBNull.Value,
                ["updated_by"] = DBNull.Value,
            };

            var select = sql[(sql.IndexOf("SELECT", StringComparison.Ordinal) + "SELECT".Length)..sql.IndexOf("FROM", StringComparison.Ordinal)];
            var columns = select.Split(',').Select(c => c.Trim().Split('.').Last().Trim('"')).ToList();

            var table = new DataTable();
            foreach (var column in columns)
                table.Columns.Add(column, values[column] is DBNull ? typeof(object) : values[column].GetType());
            table.Rows.Add([.. columns.Select(c => values[c])]);

            return table.CreateDataReader();
        }
    }
}
