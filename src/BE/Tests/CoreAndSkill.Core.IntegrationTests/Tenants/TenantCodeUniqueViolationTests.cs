using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Execution;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// docs/contracts/tenants.md §2 "Hai lượt tạo cùng code chạy đồng thời": lượt kia chưa commit nên phép tra trùng thấy "không
// trùng" (mọi câu đọc trả rỗng), và lệnh INSERT core.tenant vỡ uq_tenant_code. Không database. Thứ KHÔNG chứng minh: hai lượt
// đồng thời thật trên PostgreSQL — TenantCodeUniqueViolationDatabaseTests (RequiresDocker).
public sealed class TenantCodeUniqueViolationTests
{
    private const string Code = "so-gd";

    private (TenantProvisioningService Service, CoreDbContext Db, UniqueViolationOnWrite Violation) Build(string? sqlState, string? constraint)
    {
        var violation = new UniqueViolationOnWrite("INSERT INTO core.tenant ", "tenant", sqlState, constraint);
        var db = OfflineCoreDbContext.Create(null, [violation, .. new SqlRecorder(emptyReaders: true).Interceptors]);

        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUser>();
        var store = new UserStore<AppUser, AppRole, CoreDbContext, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(db);
        var users = new UserManager<AppUser>(
            store, Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(),
            [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null!, NullLogger<UserManager<AppUser>>.Instance);

        var service = new TenantProvisioningService(
            new ExecutionContextScope(), db, users, null!, [], [], currentUser, tenantContext,
            Substitute.For<IClientAddressAccessor>(), TimeProvider.System, new CrossTenantActorScope(),
            NullLogger<TenantProvisioningService>.Instance);

        return (service, db, violation);
    }

    private static CreateTenantInput Input() => new(
        Code, "Sở Giáo dục", IsSystem: false, "quantri", "Passw0rd-Tam1", AdminHasPermissionBypass: true,
        AdminIsSystemOperator: false, FailIfExists: true, AdminEmail: "quantri@vd.vn", AdminFullName: "Quản trị");

    [Fact]
    public async Task Create_UniqueViolationOnTenantCodeIndex_IsCodeDuplicate_WithTheCodeAsSent_AndTheTenantLeavesTheTracker()
    {
        var (service, db, violation) = Build(PostgresErrorCodes.UniqueViolation, UniqueViolations.TenantCode);
        await using var _ = db;
        db.Database.AutoSavepointsEnabled = false; // transaction giả không tạo được savepoint thật
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateTenantAsync(Input(), CancellationToken.None);

        violation.Thrown.ShouldBe(1, "chống test rỗng: lệnh ghi phải thật sự tới chỗ vỡ index");
        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.CodeDuplicate.Code);
        result.Error.Params.ShouldContainKeyAndValue("Code", Code);
        db.ChangeTracker.Entries().ShouldBeEmpty("không dòng nào còn chờ ghi sau lượt thua");
    }

    // Ràng buộc khác, mã lỗi khác 23505, hay lỗi không phải của PostgreSQL: không phải "trùng mã" — vẫn là lỗi hệ thống.
    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "pk_tenant")]
    [InlineData(PostgresErrorCodes.UniqueViolation, "UQ_TENANT_CODE")]
    [InlineData(PostgresErrorCodes.CheckViolation, UniqueViolations.TenantCode)]
    public async Task Create_AnyOtherViolation_IsNotSwallowed(string sqlState, string constraint)
    {
        var (service, db, violation) = Build(sqlState, constraint);
        await using var _ = db;
        db.Database.AutoSavepointsEnabled = false; // transaction giả không tạo được savepoint thật
        await db.Database.BeginTransactionAsync();

        var thrown = await Should.ThrowAsync<DbUpdateException>(() => service.CreateTenantAsync(Input(), CancellationToken.None));

        violation.Thrown.ShouldBe(1);
        thrown.InnerException.ShouldBeOfType<PostgresException>().ConstraintName.ShouldBe(constraint);
    }

    [Fact]
    public async Task TheConstraintName_IsTheUniqueIndexOfTheModel_OnCode()
    {
        await using var db = OfflineCoreDbContext.Create(null);
        var entity = db.Model.FindEntityType(typeof(CoreAndSkill.Core.Domain.Tenants.Tenant)).ShouldNotBeNull();

        var index = entity.GetIndexes().SingleOrDefault(i => i.GetDatabaseName() == UniqueViolations.TenantCode).ShouldNotBeNull();

        index.IsUnique.ShouldBeTrue();
        index.Properties.Select(p => p.Name).ShouldBe([nameof(CoreAndSkill.Core.Domain.Tenants.Tenant.Code)]);
    }
}
