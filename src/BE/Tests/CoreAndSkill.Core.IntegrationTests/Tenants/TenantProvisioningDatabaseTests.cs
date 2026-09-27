using System.Diagnostics;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// TenantProvisioningService trên PostgreSQL THẬT, qua DI của ứng dụng thật — luật T2, T8: không giả
// lập DbContext, UnitOfWork, UserManager. Chứng minh ba thứ mà test thuần logic không chứng minh được
// (docs/quy-uoc/be-architecture.md §9.1): transaction commit trọn vẹn, TenantId được gán đúng lúc ghi,
// và các dòng nhật ký kiểm toán thật sự vào bảng với đúng đơn vị.
//
// Nhật ký kiểm toán có HAI nguồn ghi độc lập — đừng nhầm khi đếm dòng:
//   1. Ghi TƯỜNG MINH trong TenantProvisioningService: core.tenant.create,
//      core.tenant.admin_recovery_reset… — dòng xuyên đơn vị.
//   2. AuditLogInterceptor tự ghi khi thấy AppUser/AppRole/RolePermission đổi: core.user.create,
//      core.role.create, core.permission.matrix_update, core.user.password_change. Seed ánh xạ quyền sinh MỘT dòng
//      matrix_update cho mỗi vai trò được seed khoá, after_value mang danh sách "granted" (docs/contracts/permissions.md
//      §6 mục "Nhật ký kiểm toán") — chưa test nào ở file này khẳng định dòng đó.
// Vì vậy "một dòng" / "hai dòng" ở các khẳng định dưới là theo MÃ HÀNH ĐỘNG, không phải tổng số dòng.
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class TenantProvisioningDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string Password = "Passw0rd-Test1";
    private const string ResetPassword = "New-Passw0rd-2";

    private readonly CoreWebApplicationFactory _factory = new(
        new Dictionary<string, string?> { ["ConnectionStrings:Core"] = db.ConnectionString },
        services => services.AddSingleton<ITenantSeedSource, FakeTenantSeedSource>());

    public Task InitializeAsync() => db.ResetAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ---- Tạo đơn vị: nhật ký kiểm toán -------------------------------------------------------

    [Fact]
    public async Task CreateTenant_NewTenant_WritesExactlyOneTenantCreateAuditRow_WithNewTenantId()
    {
        // Act
        var result = await CreateTenantAsync(BusinessInput("DV-AUDIT"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var tenantId = result.Value.TenantId;

        var rows = await ReadAuditRowsAsync();
        var created = rows.Where(r => r.ActionCode == AuditActionCodes.TenantCreate).ToList();

        created.Count.ShouldBe(1);
        created[0].TenantId.ShouldBe(tenantId);
        created[0].ActorTenantId.ShouldBeNull(); // không xuyên đơn vị
        created[0].TargetType.ShouldBe("core.tenant");
        created[0].TargetId.ShouldBe(tenantId.ToString());

        // Chỉ có MỘT đơn vị trong DB nên mọi dòng nhật ký (kể cả dòng của interceptor) đều mang đơn vị đó.
        rows.ShouldAllBe(r => r.TenantId == tenantId);
    }

    // ---- Tạo đơn vị: chạy lại không nhân đôi (ADR-0023 §3) -----------------------------------

    [Fact]
    public async Task CreateTenant_RerunWithSameCode_DoesNotDuplicateTenantUserRoleMenuOrAudit()
    {
        // Arrange
        var input = BusinessInput("DV-IDEM");
        var first = await CreateTenantAsync(input);
        var before = await CountAllAsync();

        // Guard chống test rỗng: có gì đó để mà "không nhân đôi".
        before.Tenants.ShouldBe(1);
        before.Users.ShouldBe(1);
        before.Roles.ShouldBe(1);           // FakeTenantSeedSource
        before.RolePermissions.ShouldBe(1); // FakeTenantSeedSource
        before.MenuItems.ShouldBeGreaterThan(1); // menu của Core + một mục của nguồn giả
        before.AuditRows.ShouldBeGreaterThan(0);

        // Act
        var second = await CreateTenantAsync(input);

        // Assert
        second.IsSuccess.ShouldBeTrue();
        second.Value.TenantWasCreated.ShouldBeFalse();
        second.Value.AdminWasCreated.ShouldBeFalse();
        second.Value.TenantId.ShouldBe(first.Value.TenantId);
        second.Value.AdminUserId.ShouldBe(first.Value.AdminUserId);

        (await CountAllAsync()).ShouldBe(before);
    }

    [Fact]
    public async Task CreateTenant_FailIfExists_WhenCodeExistsWithDifferentCaseAndPadding_ReturnsCodeDuplicate_AndWritesNothing()
    {
        // Arrange
        await CreateTenantAsync(BusinessInput("DV-DUP"));
        var before = await CountAllAsync();

        // Act — mã được chuẩn hoá (Trim + chữ HOA) trước khi so, nên biến thể này vẫn là cùng một đơn vị.
        var result = await CreateTenantAsync(BusinessInput(" dv-dup ", failIfExists: true));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.CodeDuplicate.Code);
        (await CountAllAsync()).ShouldBe(before);
    }

    // ---- Khôi phục quản trị xuyên đơn vị: hai dòng nhật ký (ADR-0029 §4) ---------------------

    [Fact]
    public async Task RecoveryResetAdminPassword_CrossTenant_WritesTwoAuditRows_SameTraceId_CorrectTenantAndActorTenant()
    {
        // Arrange — đơn vị hệ thống (chủ tài khoản vận hành) và một đơn vị nghiệp vụ.
        var system = (await CreateTenantAsync(SystemInput())).Value;
        var business = (await CreateTenantAsync(BusinessInput("DV-RECOVERY"))).Value;
        var auditIdsBefore = (await ReadAuditRowsAsync()).Select(r => r.Id).ToHashSet();

        // Trace id lấy từ Activity.Current — thứ service đọc (TraceId()). Không có Activity thì trace_id
        // là null ở cả hai dòng và khẳng định "cùng trace_id" xanh vì lý do sai.
        using var activity = new Activity("recovery-reset-test").Start();

        // Act — người gọi là tài khoản vận hành, đang đứng ở đơn vị hệ thống.
        Result result;
        using (EnterCallerScope(system.TenantId, system.AdminUserId, "superadmin"))
        {
            result = await WithServiceAsync((s, ct) => s.RecoveryResetAdminPasswordAsync(
                business.TenantId, "admin", ResetPassword, ct));
        }

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var recovery = (await ReadAuditRowsAsync())
            .Where(r => !auditIdsBefore.Contains(r.Id) && r.ActionCode == AuditActionCodes.TenantAdminRecoveryReset)
            .ToList();

        recovery.Count.ShouldBe(2);

        var operatorRow = recovery.Single(r => r.TenantId == system.TenantId); // nhật ký của người vận hành
        var targetRow = recovery.Single(r => r.TenantId == business.TenantId); // nhật ký của đơn vị đích

        operatorRow.ActorTenantId.ShouldBeNull();
        targetRow.ActorTenantId.ShouldBe(system.TenantId); // dấu hiệu xuyên đơn vị

        var traceId = activity.TraceId.ToHexString();
        operatorRow.TraceId.ShouldBe(traceId);
        targetRow.TraceId.ShouldBe(traceId);

        foreach (var row in recovery)
        {
            row.ActorUserId.ShouldBe(system.AdminUserId);
            row.ActorDisplay.ShouldBe("superadmin");
            row.TargetType.ShouldBe("core.user");
            row.TargetId.ShouldBe(business.AdminUserId.ToString());
        }
    }

    // Ca biên: người gọi đứng NGAY đơn vị đích (không xuyên đơn vị) thì chỉ MỘT dòng, không actorTenantId.
    [Fact]
    public async Task RecoveryResetAdminPassword_CallerInTargetTenant_WritesSingleAuditRow_WithoutActorTenant()
    {
        // Arrange
        var business = (await CreateTenantAsync(BusinessInput("DV-SAME"))).Value;
        var auditIdsBefore = (await ReadAuditRowsAsync()).Select(r => r.Id).ToHashSet();

        // Act
        Result result;
        using (EnterCallerScope(business.TenantId, business.AdminUserId, "admin"))
        {
            result = await WithServiceAsync((s, ct) => s.RecoveryResetAdminPasswordAsync(
                business.TenantId, "admin", ResetPassword, ct));
        }

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var recovery = (await ReadAuditRowsAsync())
            .Where(r => !auditIdsBefore.Contains(r.Id) && r.ActionCode == AuditActionCodes.TenantAdminRecoveryReset)
            .ToList();

        recovery.Count.ShouldBe(1);
        recovery[0].TenantId.ShouldBe(business.TenantId);
        recovery[0].ActorTenantId.ShouldBeNull();
    }

    // ---- Luật M14: dòng interceptor tự sinh ở đơn vị đích mang actor_tenant_id ----------------

    // docs/database/schema-core.md §9.4 điểm 4, docs/contracts/tenants.md §5: tài khoản vận hành (đơn vị hệ thống) tạo quản
    // trị cho một đơn vị nghiệp vụ. Dòng core.user.create do AuditLogInterceptor sinh ở đơn vị đích — không phải dòng
    // tường minh của service — cũng phải mang actor_tenant_id = đơn vị hệ thống; dòng ở đơn vị hệ thống thì không. Bản
    // offline cùng khẳng định: TenantProvisioningActorTests.CreateAdditionalAdmin_InterceptorRows_CarryTheOperatorTenant_AsActorTenant.
    [Fact]
    public async Task CrossTenantOperation_InterceptorRows_CarryActorTenant()
    {
        // Arrange
        var system = (await CreateTenantAsync(SystemInput())).Value;
        var business = (await CreateTenantAsync(BusinessInput("DV-ACTOR"))).Value;
        var auditIdsBefore = (await ReadAuditRowsAsync()).Select(r => r.Id).ToHashSet();

        // Act — người gọi là tài khoản vận hành, đang đứng ở đơn vị hệ thống.
        Result<Guid> result;
        using (EnterCallerScope(system.TenantId, system.AdminUserId, "superadmin"))
        {
            result = await WithServiceAsync((s, ct) => s.CreateAdditionalAdminAsync(
                business.TenantId, "quantri2", "quantri2@dv-actor.example.com", "Quản trị hai", Password, ct));
        }

        // Assert
        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        var rows = (await ReadAuditRowsAsync()).Where(r => !auditIdsBefore.Contains(r.Id)).ToList();

        // Chống test rỗng: dòng do INTERCEPTOR sinh phải có mặt, ở đơn vị đích, trỏ đúng tài khoản vừa tạo.
        var userCreate = rows.Single(r => r.ActionCode == AuditActionCodes.UserCreate);
        userCreate.TenantId.ShouldBe(business.TenantId);
        userCreate.TargetId.ShouldBe(result.Value.ToString());
        userCreate.ActorTenantId.ShouldBe(system.TenantId);
        userCreate.ActorUserId.ShouldBe(system.AdminUserId);

        // Phát biểu chung của M14 trên MỌI dòng của thao tác: ở đơn vị đích mang dấu, ở đơn vị của người gọi thì không.
        rows.Where(r => r.TenantId == business.TenantId).ShouldAllBe(r => r.ActorTenantId == system.TenantId);
        rows.Where(r => r.TenantId == system.TenantId).ShouldAllBe(r => r.ActorTenantId == null);
        rows.Select(r => r.TenantId).Distinct().ShouldBe(new[] { system.TenantId, business.TenantId }, ignoreOrder: true);
    }

    // Cùng luật cho đường TẠO ĐƠN VỊ bởi tài khoản vận hành: mọi dòng seed (core.role.create, core.permission.matrix_update,
    // core.user.create của quản trị đầu tiên) sinh ở đơn vị MỚI mang actor_tenant_id = đơn vị hệ thống, và dòng
    // core.tenant.create ghi HAI dòng theo §5 (một ở đơn vị hệ thống, một ở đơn vị mới).
    [Fact]
    public async Task CreateTenant_ByOperator_AllRowsAtTheNewTenant_CarryActorTenant_AndTenantCreateIsWrittenTwice()
    {
        var system = (await CreateTenantAsync(SystemInput())).Value;
        var auditIdsBefore = (await ReadAuditRowsAsync()).Select(r => r.Id).ToHashSet();

        Result<TenantProvisioningResult> created;
        using (EnterCallerScope(system.TenantId, system.AdminUserId, "superadmin"))
        {
            created = await CreateTenantAsync(BusinessInput("DV-BY-OP"));
        }

        created.IsSuccess.ShouldBeTrue(created.Error?.Code);
        var business = created.Value;
        var rows = (await ReadAuditRowsAsync()).Where(r => !auditIdsBefore.Contains(r.Id)).ToList();

        rows.Where(r => r.TenantId == business.TenantId).ShouldNotBeEmpty();
        rows.Where(r => r.TenantId == business.TenantId).ShouldAllBe(r => r.ActorTenantId == system.TenantId);
        rows.Where(r => r.TenantId == system.TenantId).ShouldAllBe(r => r.ActorTenantId == null);
        rows.ShouldContain(r => r.ActionCode == AuditActionCodes.UserCreate && r.TenantId == business.TenantId);

        var tenantCreate = rows.Where(r => r.ActionCode == AuditActionCodes.TenantCreate).ToList();
        tenantCreate.Count.ShouldBe(2);
        tenantCreate.Single(r => r.TenantId == system.TenantId).ActorTenantId.ShouldBeNull();
        tenantCreate.Single(r => r.TenantId == business.TenantId).ActorTenantId.ShouldBe(system.TenantId);
    }

    // ---- Seed: bản ghi mang đúng TenantId của đơn vị MỚI (finding F3) ------------------------

    // Khoá chuỗi ApplySeedAsync → SaveChangesAsync nằm TRONG phạm vi ngữ cảnh của đơn vị mới. Người tạo
    // đơn vị thật (POST /system/tenants) luôn đứng ở đơn vị hệ thống — nên ở đây cũng đứng ở đó: nếu
    // một thay đổi tương lai đẩy lần lưu ra ngoài phạm vi, RolePermission/MenuItem/AppRole của đơn vị
    // mới sẽ mang TenantId của đơn vị hệ thống (ambient) hoặc bị M8 từ chối.
    [Fact]
    public async Task CreateTenant_SeedRowsCreatedFromAnotherTenantsContext_CarryNewTenantId_NotAmbientTenant()
    {
        // Arrange
        var system = (await CreateTenantAsync(SystemInput())).Value;

        // Act
        Result<TenantProvisioningResult> created;
        using (EnterCallerScope(system.TenantId, system.AdminUserId, "superadmin"))
        {
            created = await CreateTenantAsync(BusinessInput("DV-SEED"));
        }

        // Assert
        created.IsSuccess.ShouldBeTrue();
        var business = created.Value;
        business.TenantId.ShouldNotBe(system.TenantId);

        // Ánh xạ vai trò → quyền: mỗi đơn vị đúng MỘT dòng, dòng và vai trò của nó cùng đơn vị.
        var rolePermissions = await db.QueryAsync(
            """
            SELECT rp.tenant_id, r.tenant_id, r.name, p.code
            FROM core.role_permission rp
            JOIN core.app_role r ON r.id = rp.role_id
            JOIN core.permission p ON p.id = rp.permission_id
            """,
            reader => (TenantId: reader.GetGuid(0), RoleTenantId: reader.GetGuid(1),
                RoleName: reader.GetString(2), PermissionCode: reader.GetString(3)));

        rolePermissions.Count.ShouldBe(2);
        foreach (var tenantId in new[] { system.TenantId, business.TenantId })
        {
            var mine = rolePermissions.Where(rp => rp.TenantId == tenantId).ToList();
            mine.Count.ShouldBe(1);
            mine[0].RoleTenantId.ShouldBe(tenantId);
            mine[0].RoleName.ShouldBe(FakeTenantSeedSource.RoleName);
            mine[0].PermissionCode.ShouldBe(FakeTenantSeedSource.PermissionCode);
        }

        // Vai trò: mỗi đơn vị một bản của vai trò seed.
        (await CountRolesOfAsync(business.TenantId)).ShouldBe(1);
        (await CountRolesOfAsync(system.TenantId)).ShouldBe(1);

        // Menu: hai đơn vị được seed từ CÙNG các nguồn nên số mục phải bằng nhau — đơn vị mới không
        // "ăn" mục của đơn vị hệ thống, không mục nào mang TenantId rỗng hay lạ.
        var menuByTenant = await db.QueryAsync(
            "SELECT tenant_id, count(*) FROM core.menu_item GROUP BY tenant_id",
            reader => (TenantId: reader.GetGuid(0), Count: reader.GetInt64(1)));

        menuByTenant.Count.ShouldBe(2);
        menuByTenant.Select(m => m.TenantId).ShouldBe(new[] { system.TenantId, business.TenantId }, ignoreOrder: true);
        menuByTenant.Select(m => m.Count).Distinct().Count().ShouldBe(1);
        menuByTenant.ShouldAllBe(m => m.Count > 1); // Core + nguồn giả

        (await db.CountAsync(
            "SELECT count(*) FROM core.menu_item WHERE tenant_id = @t AND code = @c",
            new NpgsqlParameter("t", business.TenantId),
            new NpgsqlParameter("c", FakeTenantSeedSource.MenuItemCode))).ShouldBe(1);

        // Mục con trỏ về mục cha CỦA CÙNG đơn vị — không mục nào bắc cầu sang đơn vị khác.
        (await db.CountAsync(
            """
            SELECT count(*) FROM core.menu_item child
            JOIN core.menu_item parent ON parent.id = child.parent_id
            WHERE parent.tenant_id <> child.tenant_id
            """)).ShouldBe(0);
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private Task<Result<TenantProvisioningResult>> CreateTenantAsync(CreateTenantInput input)
        => WithServiceAsync((s, ct) => s.CreateTenantAsync(input, ct));

    // Mỗi lời gọi một scope DI mới — cùng vòng đời một request thật (DbContext/kết nối/UserManager mới) — và
    // một transaction của người gọi, như TransactionBehavior/CoreCommandRunner mở cho service.
    private async Task<T> WithServiceAsync<T>(Func<ITenantProvisioningService, CancellationToken, Task<T>> action)
        where T : IResult
    {
        using var scope = _factory.Services.CreateScope();
        return await ProvisioningInTransaction.RunAsync(scope.ServiceProvider, action);
    }

    // Mô phỏng "người gọi đứng ở đơn vị X" — thứ mà claim của phiếu xác thực cho một request thật.
    // TenantProvisioningService đọc ITenantContext.TenantId / ICurrentUser TRƯỚC khi mở phạm vi riêng.
    private IDisposable EnterCallerScope(Guid tenantId, Guid userId, string userName)
        => _factory.Services.GetRequiredService<IExecutionContextScope>().Enter(tenantId, userId, userName);

    private static CreateTenantInput BusinessInput(string code, bool failIfExists = false)
        => new(
            Code: code,
            Name: $"Đơn vị {code.Trim()}",
            IsSystem: false,
            AdminUserName: "admin",
            AdminPassword: Password,
            AdminHasPermissionBypass: true,
            AdminIsSystemOperator: false,
            FailIfExists: failIfExists,
            AdminEmail: $"admin@{code.Trim().ToLowerInvariant()}.example.com",
            AdminFullName: "Quản trị đơn vị");

    // Cờ đặc quyền loại trừ nhau (ck_app_user_privilege_flags_exclusive): tài khoản vận hành mang
    // is_system_operator, KHÔNG mang has_permission_bypass.
    private static CreateTenantInput SystemInput()
        => new(
            Code: "SYSTEM",
            Name: "Hệ thống",
            IsSystem: true,
            AdminUserName: "superadmin",
            AdminPassword: Password,
            AdminHasPermissionBypass: false,
            AdminIsSystemOperator: true,
            AdminEmail: "superadmin@system.example.com",
            AdminFullName: "Vận hành hệ thống");

    private sealed record AuditRow(
        Guid Id, Guid TenantId, Guid? ActorTenantId, Guid? ActorUserId, string ActorDisplay,
        string ActionCode, string TargetType, string TargetId, string? TraceId);

    private Task<IReadOnlyList<AuditRow>> ReadAuditRowsAsync()
        => db.QueryAsync(
            """
            SELECT id, tenant_id, actor_tenant_id, actor_user_id, actor_display,
                   action_code, target_type, target_id, trace_id
            FROM core.audit_log
            ORDER BY occurred_at, id
            """,
            reader => new AuditRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.IsDBNull(2) ? null : reader.GetGuid(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8)));

    private sealed record TableCounts(
        long Tenants, long Users, long Roles, long RolePermissions, long MenuItems, long AuditRows);

    private async Task<TableCounts> CountAllAsync()
        => new(
            await db.CountAsync("SELECT count(*) FROM core.tenant"),
            await db.CountAsync("SELECT count(*) FROM core.app_user"),
            await db.CountAsync("SELECT count(*) FROM core.app_role"),
            await db.CountAsync("SELECT count(*) FROM core.role_permission"),
            await db.CountAsync("SELECT count(*) FROM core.menu_item"),
            await db.CountAsync("SELECT count(*) FROM core.audit_log"));

    private Task<long> CountRolesOfAsync(Guid tenantId)
        => db.CountAsync("SELECT count(*) FROM core.app_role WHERE tenant_id = @t", new NpgsqlParameter("t", tenantId));
}
