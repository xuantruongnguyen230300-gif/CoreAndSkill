using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Audit;
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

// Luật S17 (docs/RULES.md §6) — hình dạng dòng core.permission.matrix_update, định nghĩa gốc ở
// docs/contracts/permissions.md §6 mục "Nhật ký kiểm toán". AuditLogInterceptor THẬT trên CoreDbContext THẬT, không
// database: lượt SaveChanges bị chặn sau khi interceptor đã dàn dựng dòng nhật ký (OfflineCoreDbContext + SaveCapture).
//
// Vai trò và quyền được đưa vào bộ theo dõi TRƯỚC khi lưu (như vừa đọc lên), nên interceptor lấy mã khoá và tên vai trò
// từ bộ theo dõi, không truy vấn. Nhánh tra database (vai trò/quyền KHÔNG có trong bộ theo dõi — đúng đường PUT thật)
// chỉ được chứng minh ở PermissionMatrixAuditDatabaseTests (RequiresDocker).
public sealed class PermissionMatrixAuditShapeTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly SaveCapture _capture = new();

    private CoreDbContext Build()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(_actorId);
        currentUser.UserName.Returns("quantri");
        var auditLog = new AuditLogInterceptor(
            TimeProvider.System, currentUser, Substitute.For<IClientAddressAccessor>(), new PasswordRehashScope(), new CrossTenantActorScope());

        return OfflineCoreDbContext.Create(_tenantId, auditLog, _capture);
    }

    private IReadOnlyList<AuditLog> MatrixRows
        => _capture.AuditRows.Where(r => r.ActionCode == AuditActionCodes.PermissionMatrixUpdate).ToList();

    [Fact]
    public async Task GrantAndRevoke_WriteOneRowPerTouchedRole_WithRoleNameAndSortedCodes()
    {
        await using var db = Build();
        var accountant = TrackedRole(db, "Kế toán");
        var auditor = TrackedRole(db, "Kiểm soát");
        var userRead = TrackedPermission(db, "core.user.read");
        var userWrite = TrackedPermission(db, "core.user.write");
        var userLock = TrackedPermission(db, "core.user.lock");
        var revokedRow = TrackedGrant(db, auditor.Id, userRead.Id);
        TrackedGrant(db, accountant.Id, userRead.Id); // ô không đổi — không được xuất hiện trong danh sách nào

        // Cấp theo thứ tự KHÔNG sắp — dòng nhật ký phải tự sắp theo chữ.
        AddGrant(db, accountant.Id, userWrite.Id);
        AddGrant(db, accountant.Id, userLock.Id);
        revokedRow.IsDeleted = true;

        await db.SaveChangesAsync();

        MatrixRows.Count.ShouldBe(2);

        var accountantRow = MatrixRows.Single(r => r.TargetId == accountant.Id.ToString());
        accountantRow.TenantId.ShouldBe(_tenantId);
        accountantRow.TargetType.ShouldBe("core.role");
        accountantRow.TargetDisplay.ShouldBe("Kế toán");
        accountantRow.ActorUserId.ShouldBe(_actorId);
        accountantRow.BeforeValue.ShouldBeNull();
        var (accountantGranted, accountantRevoked) = Lists(accountantRow);
        accountantGranted.ShouldBe(["core.user.lock", "core.user.write"]);
        accountantRevoked.ShouldBeEmpty();

        var auditorRow = MatrixRows.Single(r => r.TargetId == auditor.Id.ToString());
        auditorRow.TargetDisplay.ShouldBe("Kiểm soát");
        auditorRow.BeforeValue.ShouldBeNull();
        var (auditorGranted, auditorRevoked) = Lists(auditorRow);
        auditorGranted.ShouldBeEmpty();
        auditorRevoked.ShouldBe(["core.user.read"]);
    }

    // Dấu hiệu sai thứ nhất của ADR-0052: đọc cờ IsModified thay vì giá trị. DbContext.Update(entity) đánh dấu mọi cột
    // "đã sửa" dù giá trị không đổi — không ô nào đổi thì không dòng nào.
    [Fact]
    public async Task IsDeletedFlaggedModified_ButValueUnchanged_WritesNoRow()
    {
        await using var db = Build();
        var role = TrackedRole(db, "Kế toán");
        var permission = TrackedPermission(db, "core.user.read");
        var row = TrackedGrant(db, role.Id, permission.Id);

        db.Update(row);
        db.Entry(row).Property(x => x.IsDeleted).IsModified.ShouldBeTrue("chống test rỗng: cờ phải thật sự bật");

        await db.SaveChangesAsync();

        MatrixRows.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoCellChanged_WritesNoRow()
    {
        await using var db = Build();
        var role = TrackedRole(db, "Kế toán");
        var permission = TrackedPermission(db, "core.user.read");
        TrackedGrant(db, role.Id, permission.Id);

        await db.SaveChangesAsync();

        MatrixRows.ShouldBeEmpty();
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private static (string[] Granted, string[] Revoked) Lists(AuditLog row)
    {
        row.AfterValue.ShouldNotBeNull();
        using var json = JsonDocument.Parse(row.AfterValue);
        json.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(["granted", "revoked"]);

        return (Read(json.RootElement, "granted"), Read(json.RootElement, "revoked"));

        static string[] Read(JsonElement root, string name)
            => root.GetProperty(name).EnumerateArray().Select(e => e.GetString()!).ToArray();
    }

    private AppRole TrackedRole(CoreDbContext db, string name)
    {
        var role = new AppRole { TenantId = _tenantId, Name = name, NormalizedName = name.ToUpperInvariant() };
        db.Attach(role);
        return role;
    }

    // Permission không có factory công khai — hàng danh mục chỉ vào bằng migration. Dựng như EF vật liệu hoá nó: ctor
    // không tham số rồi gán giá trị qua bộ theo dõi.
    private static Permission TrackedPermission(CoreDbContext db, string code)
    {
        var permission = (Permission)Activator.CreateInstance(typeof(Permission), nonPublic: true)!;
        var entry = db.Entry(permission);
        entry.Property(p => p.Code).CurrentValue = code;
        entry.State = EntityState.Unchanged;
        return permission;
    }

    private RolePermission TrackedGrant(CoreDbContext db, Guid roleId, Guid permissionId)
    {
        var row = RolePermission.Create(roleId, permissionId).Value;
        var entry = db.Entry(row);
        entry.Property(x => x.TenantId).CurrentValue = _tenantId;
        entry.State = EntityState.Unchanged;
        return row;
    }

    private void AddGrant(CoreDbContext db, Guid roleId, Guid permissionId)
    {
        var row = RolePermission.Create(roleId, permissionId).Value;
        db.RolePermissions.Add(row);
        db.Entry(row).Property(x => x.TenantId).CurrentValue = _tenantId;
    }
}
