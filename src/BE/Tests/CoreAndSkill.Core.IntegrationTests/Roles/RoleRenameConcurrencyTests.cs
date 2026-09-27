using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Roles;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Roles;

// RoleAdminService.RenameAsync trên RoleManager + RoleStore THẬT, ChangeTracker THẬT, không database
// (OfflineCoreDbContext). docs/wiki-core/be/06-concurrency-control.md §6.3 luật 3. RoleRowEmulator đứng thay PostgreSQL
// ở phép so của câu UPDATE — WHERE concurrency_stamp = <OriginalValue lúc SaveChanges>, 0 dòng ⇒
// DbUpdateConcurrencyException — và giữ "dòng trong DB".
//
// Thứ nó chứng minh: token client gửi lên là token được so; lệch/thiếu ⇒ CORE.CONCURRENCY.CONFLICT và không lượt lưu
// nào thành công; lượt ghi chen giữa lúc đọc và lúc UPDATE cũng bị bắt; khớp ⇒ tên đổi và token đổi. Emulator đọc
// OriginalValue SAU khi RoleStore.UpdateAsync đã Attach — nên một bản hiện thực chỉ đặt OriginalValue = version (bị
// Attach ghi đè) sẽ đỏ ở đây. Thứ nó KHÔNG chứng minh: câu SQL thật — RoleRenameDatabaseTests.
//
// Hai tra cứu theo id/tên của RoleManager là truy vấn DB, nên được trỏ về bản ghi đang theo dõi (TrackedLookupRoleManager).
public sealed class RoleRenameConcurrencyTests
{
    private const string OldName = "Kế toán";
    private const string NewName = "Kế toán trưởng";

    private readonly Guid _tenantId = Guid.NewGuid();

    private (RoleAdminService Service, RoleRowEmulator Row, AppRole Role, CoreDbContext Db) Build()
    {
        var role = new AppRole { TenantId = _tenantId, Name = OldName, NormalizedName = OldName.ToUpperInvariant() };
        var row = new RoleRowEmulator(role.ConcurrencyStamp!, OldName);

        var db = OfflineCoreDbContext.Create(_tenantId, row);
        db.Attach(role);

        var store = new RoleStore<AppRole, CoreDbContext, Guid, AppUserRole, AppRoleClaim>(db);
        var manager = new TrackedLookupRoleManager(store, role);
        return (new RoleAdminService(manager, db), row, role, db);
    }

    [Fact]
    public async Task StaleVersion_ReturnsConcurrencyConflict_AndWritesNothing()
    {
        var (service, row, role, db) = Build();
        await using var _ = db;
        var stampBefore = row.Stamp;

        var result = await service.RenameAsync(role.Id, NewName, "stamp-cu-cua-lan-doc-truoc", CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        row.Writes.ShouldBe(0);
        row.Name.ShouldBe(OldName);
        row.Stamp.ShouldBe(stampBefore);
    }

    // `null` không bao giờ khớp — thiếu version là 409, không phải "bỏ qua phép so".
    [Fact]
    public async Task MissingVersion_ReturnsConcurrencyConflict_AndWritesNothing()
    {
        var (service, row, role, db) = Build();
        await using var _ = db;

        var result = await service.RenameAsync(role.Id, NewName, null, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        row.Writes.ShouldBe(0);
        row.Name.ShouldBe(OldName);
    }

    [Fact]
    public async Task CurrentVersion_Renames_AndIssuesANewVersion()
    {
        var (service, row, role, db) = Build();
        await using var _ = db;
        var current = row.Stamp;

        var result = await service.RenameAsync(role.Id, NewName, current, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        row.Writes.ShouldBe(1);
        row.Name.ShouldBe(NewName);
        row.Stamp.ShouldNotBe(current);
    }

    // Client giữ đúng stamp đã đọc, nhưng một lượt ghi khác commit xen giữa lúc service đọc và lúc UPDATE: câu UPDATE
    // (WHERE concurrency_stamp = stamp đã đọc) phải bắt được — không chỉ phép so ở đầu service.
    [Fact]
    public async Task WriteBetweenReadAndUpdate_ReturnsConcurrencyConflict_AndKeepsTheOtherWrite()
    {
        var (service, row, role, db) = Build();
        await using var _ = db;
        var readStamp = row.Stamp;
        row.CommitElsewhere("Tên của người khác");

        var result = await service.RenameAsync(role.Id, NewName, readStamp, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        row.Writes.ShouldBe(0);
        row.Name.ShouldBe("Tên của người khác");
    }

    // Đứng thay PostgreSQL ở phép so token của câu UPDATE: đọc OriginalValue — đúng giá trị EF đặt vào WHERE.
    private sealed class RoleRowEmulator(string stamp, string name) : SaveChangesInterceptor
    {
        public string Stamp { get; private set; } = stamp;
        public string Name { get; private set; } = name;
        public int Writes { get; private set; }

        // Một lượt ghi khác đã commit: dòng trong "DB" đổi, bản ghi service đang giữ thì không.
        public void CommitElsewhere(string name)
        {
            Name = name;
            Stamp = Guid.NewGuid().ToString();
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => Save(eventData.Context!);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Save(eventData.Context!));

        private InterceptionResult<int> Save(DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries<AppRole>().Where(e => e.State == EntityState.Modified))
            {
                var expected = entry.Property(r => r.ConcurrencyStamp).OriginalValue;
                if (!string.Equals(expected, Stamp, StringComparison.Ordinal))
                    throw new DbUpdateConcurrencyException("UPDATE app_role ... WHERE concurrency_stamp = @p: 0 dòng.");

                Stamp = entry.Entity.ConcurrencyStamp!;
                Name = entry.Entity.Name!;
                Writes++;
            }

            context.ChangeTracker.AcceptAllChanges();
            return InterceptionResult<int>.SuppressWithResult(1);
        }
    }

    private sealed class TrackedLookupRoleManager(IRoleStore<AppRole> store, AppRole role)
        : RoleManager<AppRole>(store, [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            NullLogger<RoleManager<AppRole>>.Instance)
    {
        public override Task<AppRole?> FindByIdAsync(string roleId)
            => Task.FromResult<AppRole?>(roleId == role.Id.ToString() ? role : null);

        public override Task<AppRole?> FindByNameAsync(string roleName)
            => Task.FromResult<AppRole?>(
                string.Equals(roleName.ToUpperInvariant(), role.NormalizedName, StringComparison.Ordinal) ? role : null);
    }
}
