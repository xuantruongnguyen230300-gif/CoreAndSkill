using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Roles;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Roles;

// docs/contracts/roles.md §2, §3 "Hai lượt ghi cùng name chạy đồng thời": lượt kia CHƯA commit nên phép tra trùng thấy "không
// trùng" (FindByNameAsync trả null), và lệnh INSERT/UPDATE vỡ RoleNameIndex. Không database — OfflineCoreDbContext +
// UniqueViolationOnWrite ném PostgresException 23505 đúng hình dạng database thật. Thứ KHÔNG chứng minh: hai lượt đồng thời
// thật trên PostgreSQL — RoleNameUniqueViolationDatabaseTests (RequiresDocker).
public sealed class RoleNameUniqueViolationTests
{
    private const string Name = "Kế toán";

    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Create_UniqueViolationOnRoleNameIndex_IsNameDuplicate_AndTheRoleLeavesTheTracker()
    {
        var (service, db, violation) = Build("INSERT INTO core.app_role ", PostgresErrorCodes.UniqueViolation, UniqueViolations.NormalizedNameIndex);
        await using var _ = db;

        var result = await service.CreateAsync(Name, CancellationToken.None);

        violation.Thrown.ShouldBe(1, "chống test rỗng: lệnh ghi phải thật sự tới chỗ vỡ index");
        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(RoleErrors.NameDuplicate.Code);
        result.Error.Type.ShouldBe(RoleErrors.NameDuplicate.Type);
        result.Error.Params.ShouldContainKeyAndValue("Name", Name);
        db.ChangeTracker.Entries<AppRole>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Rename_UniqueViolationOnRoleNameIndex_IsNameDuplicate_AndTheRoleLeavesTheTracker()
    {
        var (service, db, violation) = Build("UPDATE core.app_role ", PostgresErrorCodes.UniqueViolation, UniqueViolations.NormalizedNameIndex);
        await using var _ = db;
        var role = Tracked(db);

        var result = await service.RenameAsync(role.Id, "Kế toán trưởng", role.ConcurrencyStamp, CancellationToken.None);

        violation.Thrown.ShouldBe(1, "chống test rỗng: lệnh ghi phải thật sự tới chỗ vỡ index");
        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(RoleErrors.NameDuplicate.Code);
        result.Error.Params.ShouldContainKeyAndValue("Name", "Kế toán trưởng");
        db.ChangeTracker.Entries<AppRole>().ShouldBeEmpty();
    }

    // Ràng buộc khác, mã lỗi khác 23505, hay lỗi không phải của PostgreSQL: không phải "trùng tên" — vẫn là lỗi hệ thống.
    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "pk_app_role")]
    [InlineData(PostgresErrorCodes.UniqueViolation, "rolenameindex")]
    [InlineData(PostgresErrorCodes.CheckViolation, UniqueViolations.NormalizedNameIndex)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, UniqueViolations.NormalizedNameIndex)]
    public async Task Create_AnyOtherViolation_IsNotSwallowed(string sqlState, string constraint)
    {
        var (service, db, violation) = Build("INSERT INTO core.app_role ", sqlState, constraint);
        await using var _ = db;

        var thrown = await Should.ThrowAsync<DbUpdateException>(() => service.CreateAsync(Name, CancellationToken.None));

        violation.Thrown.ShouldBe(1);
        thrown.InnerException.ShouldBeOfType<PostgresException>().ConstraintName.ShouldBe(constraint);
    }

    [Fact]
    public async Task Rename_NonPostgresFailure_IsNotSwallowed()
    {
        var (service, db, _) = Build("UPDATE core.app_role ", sqlState: null, constraint: null);
        await using var __ = db;
        var role = Tracked(db);

        var thrown = await Should.ThrowAsync<DbUpdateException>(
            () => service.RenameAsync(role.Id, "Kế toán trưởng", role.ConcurrencyStamp, CancellationToken.None));

        thrown.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    // Tên index khai ở một chỗ, đối chiếu với mô hình EF và script thật (cùng cách AppUserUniqueIndexesTests).
    [Fact]
    public void TheConstraintName_IsTheUniqueIndexOfTheModel_OnTenantAndNormalizedName()
    {
        using var db = OfflineCoreDbContext.Create(_tenantId);
        var entity = db.Model.FindEntityType(typeof(AppRole)).ShouldNotBeNull();

        var index = entity.GetIndexes().SingleOrDefault(i => i.GetDatabaseName() == UniqueViolations.NormalizedNameIndex).ShouldNotBeNull();

        index.IsUnique.ShouldBeTrue();
        index.Properties.Select(p => p.Name).ShouldBe([nameof(AppRole.TenantId), nameof(AppRole.NormalizedName)]);
    }

    private (RoleAdminService Service, CoreDbContext Db, UniqueViolationOnWrite Violation) Build(string prefix, string? sqlState, string? constraint)
    {
        var violation = new UniqueViolationOnWrite(prefix, "app_role", sqlState, constraint);
        var db = OfflineCoreDbContext.Create(_tenantId, [.. new SqlRecorder(emptyReaders: true).Interceptors, violation]);
        var store = new RoleStore<AppRole, CoreDbContext, Guid, AppUserRole, AppRoleClaim>(db);
        return (new RoleAdminService(new TrackedLookupRoleManager(store, db), db), db, violation);
    }

    private AppRole Tracked(CoreDbContext db)
    {
        var role = new AppRole { TenantId = _tenantId, Name = Name, NormalizedName = Name.ToUpperInvariant() };
        db.Attach(role);
        return role;
    }

    // Tra cứu theo id/tên là truy vấn DB: id trỏ về bản ghi đang theo dõi, tên mới luôn "chưa ai dùng" (lượt kia chưa commit).
    private sealed class TrackedLookupRoleManager(IRoleStore<AppRole> store, CoreDbContext db)
        : RoleManager<AppRole>(store, [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            NullLogger<RoleManager<AppRole>>.Instance)
    {
        public override Task<AppRole?> FindByIdAsync(string roleId)
            => Task.FromResult(db.ChangeTracker.Entries<AppRole>().Select(e => e.Entity).FirstOrDefault(r => r.Id.ToString() == roleId));

        public override Task<AppRole?> FindByNameAsync(string roleName) => Task.FromResult<AppRole?>(null);
    }
}
