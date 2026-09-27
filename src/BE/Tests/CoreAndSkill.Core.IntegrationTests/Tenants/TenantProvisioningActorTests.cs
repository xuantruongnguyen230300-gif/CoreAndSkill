using System.Data;
using System.Data.Common;
using System.Security.Claims;
using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Audit;
using CoreAndSkill.Core.Infrastructure.Execution;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// docs/contracts/tenants.md §5 — mọi thao tác của khu quản trị hệ thống ghi nhật ký "kèm danh tính tài khoản vận hành
// đã thực hiện"; không có NGƯỜI mới được ghi `system` (docs/quy-uoc/be-architecture.md §1.1). Service mở phạm vi của
// đơn vị đích để ghi tài khoản — phạm vi đó thay danh tính mà ICurrentUser trả, nên nó phải mang theo người vận hành,
// nếu không AuditInterceptor (created_by) và AuditLogInterceptor (dòng core.user.*) ghi `system` cho một việc có người
// làm.
//
// Không cần database: CoreDbContext thật trên OfflineCoreDbContext, UserManager + UserStore thật, ICurrentUser /
// ITenantContext THẬT của Core.Web đọc HttpContext của người vận hành. Câu tra đơn vị đích được trả một dòng dựng tay;
// lượt SaveChanges bị SaveCapture chặn sau khi mọi interceptor đã chạy.
public sealed class TenantProvisioningActorTests
{
    private const string OperatorName = "vanhanh";
    private const string TempPassword = "Passw0rd-Tam1";

    private readonly Guid _operatorId = Guid.CreateVersion7();
    private readonly Guid _systemTenantId = Guid.CreateVersion7();
    private readonly Guid _targetTenantId = Guid.CreateVersion7();
    private readonly SaveCapture _capture = new();

    private (TenantProvisioningService Service, CoreDbContext Db) Build(Guid? callerTenantId = null)
    {
        var scope = new ExecutionContextScope();
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(CoreClaimTypes.UserId, _operatorId.ToString()),
                    new Claim(CoreClaimTypes.UserName, OperatorName),
                    new Claim(CoreClaimTypes.TenantId, (callerTenantId ?? _systemTenantId).ToString()),
                ], "Test")),
            },
        };

        var currentUser = new HttpContextCurrentUser(http, scope);
        var tenantContext = new HttpContextTenantContext(http, scope);
        var clientAddress = Substitute.For<IClientAddressAccessor>();
        var crossTenantActor = new CrossTenantActorScope(); // cùng một instance cho service (đặt dấu) và interceptor (đọc dấu) — như Scoped trong DI thật

        var recorder = new SqlRecorder();
        var db = OfflineCoreDbContext.Create(
            _systemTenantId,
            [
                .. recorder.Interceptors,
                new TenantRowReader(_targetTenantId),
                new AuditInterceptor(TimeProvider.System, currentUser),
                new TenantAssignmentInterceptor(tenantContext),
                new AuditLogInterceptor(TimeProvider.System, currentUser, clientAddress, new PasswordRehashScope(), crossTenantActor),
                _capture,
            ]);

        var store = new UserStore<AppUser, AppRole, CoreDbContext, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(db);

        // Không UserValidator: bộ kiểm trùng tên đăng nhập tra database. Chính sách mật khẩu vẫn qua PasswordValidator.
        var users = new UserManager<AppUser>(
            store, Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(),
            [], [new PasswordValidator<AppUser>()], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null!, NullLogger<UserManager<AppUser>>.Instance);

        var service = new TenantProvisioningService(
            scope, db, users, null!, [], [], currentUser, tenantContext, clientAddress, TimeProvider.System, crossTenantActor,
            NullLogger<TenantProvisioningService>.Instance);

        return (service, db);
    }

    [Fact]
    public async Task CreateAdditionalAdmin_RecordsTheOperator_NotSystem_OnTheAccountRowsItWrites()
    {
        var (service, db) = Build();
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateAdditionalAdminAsync(
            _targetTenantId, "quantri2", "quantri2@dv.vn", "Quản trị hai", TempPassword, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);

        // Chống test rỗng: đường ghi tài khoản thật sự chạy trong phạm vi của đơn vị ĐÍCH.
        var userCreate = _capture.AuditRows.Single(r => r.ActionCode == AuditActionCodes.UserCreate);
        userCreate.TenantId.ShouldBe(_targetTenantId);

        userCreate.ActorDisplay.ShouldBe(OperatorName);
        userCreate.ActorUserId.ShouldBe(_operatorId);

        var created = db.ChangeTracker.Entries<AppUser>().Single().Entity;
        created.CreatedBy.ShouldBe(OperatorName);
        created.UpdatedBy.ShouldBe(OperatorName);
    }

    // Luật M14 — docs/database/schema-core.md §9.4 điểm 4, docs/contracts/tenants.md §5: dòng do AuditLogInterceptor tự sinh ở
    // đơn vị ĐÍCH (core.user.create) mang actor_tenant_id = đơn vị của NGƯỜI THỰC HIỆN theo claim của phiếu — không phải
    // phạm vi đang mở, vì phạm vi lúc đó đã là đơn vị đích. Bản Docker (TenantProvisioningDatabaseTests
    // .CrossTenantOperation_InterceptorRows_CarryActorTenant) chứng minh cùng điều trên PostgreSQL thật.
    [Fact]
    public async Task CreateAdditionalAdmin_InterceptorRows_CarryTheOperatorTenant_AsActorTenant()
    {
        var (service, db) = Build();
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateAdditionalAdminAsync(
            _targetTenantId, "quantri2", "quantri2@dv.vn", "Quản trị hai", TempPassword, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        var userCreate = _capture.AuditRows.Single(r => r.ActionCode == AuditActionCodes.UserCreate);
        userCreate.TenantId.ShouldBe(_targetTenantId);
        userCreate.ActorTenantId.ShouldBe(_systemTenantId, "dòng ở đơn vị đích phải mang dấu xuyên đơn vị");

        // Hai dòng tường minh giữ đúng khuôn §5: dòng ở đơn vị hệ thống KHÔNG mang dấu, dòng ở đơn vị đích có.
        var explicitRows = _capture.AuditRows.Where(r => r.ActionCode == AuditActionCodes.TenantAdminCreate).ToList();
        explicitRows.Single(r => r.TenantId == _systemTenantId).ActorTenantId.ShouldBeNull();
        explicitRows.Single(r => r.TenantId == _targetTenantId).ActorTenantId.ShouldBe(_systemTenantId);
    }

    // Đối chứng M14: người gọi đứng NGAY đơn vị đích thì không có gì là xuyên đơn vị — dòng interceptor không mang dấu.
    [Fact]
    public async Task CreateAdditionalAdmin_CallerInTargetTenant_InterceptorRows_CarryNoActorTenant()
    {
        var (service, db) = Build(callerTenantId: _targetTenantId);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateAdditionalAdminAsync(
            _targetTenantId, "quantri2", "quantri2@dv.vn", "Quản trị hai", TempPassword, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        _capture.AuditRows.Single(r => r.ActionCode == AuditActionCodes.UserCreate).ActorTenantId.ShouldBeNull();
        _capture.AuditRows.Where(r => r.ActionCode == AuditActionCodes.TenantAdminCreate).ShouldHaveSingleItem().ActorTenantId.ShouldBeNull();
    }

    // Đối chứng: hai dòng xuyên đơn vị ghi TƯỜNG MINH (ngoài phạm vi) vốn đã mang người vận hành — nếu test trên đỏ vì
    // ICurrentUser dựng sai thì test này cũng đỏ.
    [Fact]
    public async Task CreateAdditionalAdmin_ExplicitCrossTenantRows_CarryTheOperator()
    {
        var (service, db) = Build();
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        await service.CreateAdditionalAdminAsync(
            _targetTenantId, "quantri2", "quantri2@dv.vn", "Quản trị hai", TempPassword, CancellationToken.None);

        var rows = _capture.AuditRows.Where(r => r.ActionCode == AuditActionCodes.TenantAdminCreate).ToList();
        rows.Count.ShouldBe(2);
        rows.ShouldAllBe(r => r.ActorDisplay == OperatorName && r.ActorUserId == _operatorId);
    }

    // Cùng lỗi chính sách mật khẩu, cùng mã VÀ cùng tham số với đường đổi mật khẩu (docs/contracts/auth.md §6 "Ghi chú").
    // Giá trị là thứ UserManager thật sự áp — ở harness này là mặc định của IdentityOptions.
    [Fact]
    public async Task CreateAdditionalAdmin_ShortPassword_FieldErrorCarriesTheEnforcedMinLength()
    {
        var (service, db) = Build();
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateAdditionalAdminAsync(
            _targetTenantId, "quantri2", "quantri2@dv.vn", "Quản trị hai", "Ab1!", CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        var tooShort = result.Error!.FieldErrors["TempPassword"].Single(e => e.Code == "CORE.AUTH.PASSWORD_TOO_SHORT");
        tooShort.Params.ShouldContainKeyAndValue(
            "MinLength", new IdentityOptions().Password.RequiredLength.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // Trả MỘT dòng core.tenant cho câu tra đơn vị đích — cột lấy theo đúng thứ tự trong câu SELECT mà EF phát ra, giá trị
    // theo tên cột. Câu đọc khác không có ở đường này; gặp thì ném để test không xanh nhầm.
    private sealed class TenantRowReader(Guid tenantId) : DbCommandInterceptor
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
                ["code"] = "DV-DICH",
                ["name"] = "Đơn vị đích",
                ["is_active"] = true,
                ["is_system"] = false,
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
