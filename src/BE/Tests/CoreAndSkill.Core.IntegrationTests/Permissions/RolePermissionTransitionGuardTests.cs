using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Permissions;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// Luật S18 (docs/RULES.md §6), docs/adr/0054-o-ma-tran-chi-doi-bang-them-dong-va-xoa-mem.md: một ô của ma trận chỉ đổi
// bằng THÊM dòng (cấp) và XOÁ MỀM (thu). Khôi phục một dòng đã xoá mềm là một lần cấp không để lại dòng nhật ký; xoá cứng
// qua ChangeTracker là một lần thu (hoặc một lần xoá lịch sử) cũng không để lại dòng nào. AuditLogInterceptor chặn cả hai
// LÚC CHẠY, tại chỗ phân loại ô — mọi đường qua ChangeTracker đều đi qua đó, không phụ thuộc cú pháp của chỗ gọi.
//
// Ngoại lệ có tên duy nhất: cascade của một lần xoá vai trò mà EF đang theo dõi (vai trò Deleted cùng lượt).
//
// AuditLogInterceptor THẬT trên CoreDbContext THẬT, không database (OfflineCoreDbContext + SaveCapture).
public sealed class RolePermissionTransitionGuardTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly SaveCapture _capture = new();

    [Theory]
    [InlineData("restore")]
    [InlineData("hard-delete-active")]
    [InlineData("hard-delete-soft-deleted")]
    public async Task AuditLogInterceptor_Rejects_RolePermissionRestoreOrHardDelete(string transition)
    {
        await using var db = Build();
        var roleId = Guid.NewGuid();
        var row = TrackedGrant(db, roleId, Guid.NewGuid(), isDeleted: transition != "hard-delete-active");

        switch (transition)
        {
            case "restore":
                row.IsDeleted = false;
                break;
            default:
                db.RolePermissions.Remove(row);
                break;
        }

        db.ChangeTracker.Entries<RolePermission>().ShouldHaveSingleItem().State
            .ShouldBe(transition == "restore" ? EntityState.Modified : EntityState.Deleted, "chống test rỗng: chuyển trạng thái phải thật sự xảy ra");

        var error = await Should.ThrowAsync<InvalidOperationException>(() => db.SaveChangesAsync());

        error.Message.ShouldContain(roleId.ToString());
        _capture.AuditRows.ShouldBeEmpty("lượt lưu phải hỏng TRƯỚC khi tới bước lưu");
    }

    // Nhánh đồng bộ của interceptor đi cùng một phép chặn.
    [Fact]
    public void AuditLogInterceptor_Rejects_RolePermissionRestore_OnSynchronousSave()
    {
        using var db = Build();
        var row = TrackedGrant(db, Guid.NewGuid(), Guid.NewGuid(), isDeleted: true);
        row.IsDeleted = false;

        Should.Throw<InvalidOperationException>(() => db.SaveChanges());
    }

    // Đối chứng — ngoại lệ có tên: xoá một vai trò mà EF đang theo dõi cùng ô của nó. EF đánh dấu ô Deleted theo cascade;
    // lượt lưu phải đi qua, và dòng core.role.delete đóng chuỗi nhật ký của vai trò đó (ADR-0054 quyết định 4).
    [Fact]
    public async Task AuditLogInterceptor_Allows_CascadeOfTrackedRoleDeletion()
    {
        await using var db = Build();
        var role = new AppRole { TenantId = _tenantId, Name = "Kế toán", NormalizedName = "KẾ TOÁN" };
        db.Attach(role);
        TrackedGrant(db, role.Id, Guid.NewGuid(), isDeleted: false);
        TrackedGrant(db, role.Id, Guid.NewGuid(), isDeleted: true);

        db.Remove(role);

        db.ChangeTracker.Entries<RolePermission>().ShouldAllBe(
            e => e.State == EntityState.Deleted, "chống test rỗng: cascade của EF phải thật sự đánh dấu ô Deleted");

        await db.SaveChangesAsync();

        _capture.AuditRows.ShouldContain(r => r.ActionCode == AuditActionCodes.RoleDelete && r.TargetId == role.Id.ToString());
    }

    // Hai đường hợp lệ (thêm dòng, xoá mềm) đi qua cùng interceptor này ở PermissionMatrixAuditShapeTests — test đó là đối
    // chứng thứ hai: phép chặn chặn nhầm đường hợp lệ thì nó đỏ.

    private CoreDbContext Build()
    {
        var auditLog = new AuditLogInterceptor(
            TimeProvider.System, Substitute.For<ICurrentUser>(), Substitute.For<IClientAddressAccessor>(), new PasswordRehashScope(), new CrossTenantActorScope());

        return OfflineCoreDbContext.Create(_tenantId, auditLog, _capture);
    }

    private RolePermission TrackedGrant(CoreDbContext db, Guid roleId, Guid permissionId, bool isDeleted)
    {
        var row = RolePermission.Create(roleId, permissionId).Value;
        row.IsDeleted = isDeleted;
        var entry = db.Entry(row);
        entry.Property(x => x.TenantId).CurrentValue = _tenantId;
        entry.State = EntityState.Unchanged;
        return row;
    }
}
