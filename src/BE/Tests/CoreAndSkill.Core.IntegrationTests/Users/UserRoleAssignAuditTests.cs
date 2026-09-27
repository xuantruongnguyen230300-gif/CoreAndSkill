using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Audit;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Users;

// Luật S21 lớp (1) (docs/RULES.md §6, docs/adr/0083-doi-vai-tro-nguoi-dung-luon-vao-nhat-ky-kiem-toan.md) — hình dạng dòng
// core.user.role_assign, định nghĩa gốc ở docs/contracts/users.md §7 mục "Nhật ký kiểm toán". Cùng khuôn S17
// (PermissionMatrixAuditShapeTests): AuditLogInterceptor THẬT trên CoreDbContext THẬT, không database — lượt SaveChanges bị
// chặn sau khi interceptor đã dàn dựng dòng nhật ký (OfflineCoreDbContext + SaveCapture). Đi qua SaveChanges, không gọi
// thẳng phần dựng dòng: luật là "mọi lần ghi có dòng", nên phần dựng dòng chưa được nối vào lượt lưu là một vi phạm test
// phải thấy.
//
// Mặc định người dùng và vai trò được đưa vào bộ theo dõi TRƯỚC khi lưu (như vừa đọc lên), nên interceptor lấy họ tên và
// tên vai trò từ bộ theo dõi, không truy vấn — một câu truy vấn lọt tới đây mở kết nối tới cổng 1 và làm test đỏ. Nhánh tra
// database (vai trò KHÔNG có trong bộ theo dõi) chạy offline qua SqlRecorder(emptyReaders: true): mọi lệnh đọc trả tập rỗng,
// tức "database không có vai trò này", nên ca `name` = null phủ được ở đây, kèm khẳng định nhánh tra app_role thật sự chạy.
// Thứ offline KHÔNG chứng minh: câu tra tên chạy được trên PostgreSQL và trả đúng tên khi vai trò có ở đó — cần database thật.
//
// Mã hành động viết bằng chuỗi của hợp đồng, không qua hằng số sản phẩm: test ghim GIÁ TRỊ đã hứa ở contracts/users.md —
// nhật ký chỉ ghi thêm (M13), đổi mã về sau là hai mã cùng nghĩa trong một bảng.
public sealed class UserRoleAssignAuditTests
{
    private const string RoleAssign = "core.user.role_assign";
    private const string UserCreate = "core.user.create";
    private const string RoleDelete = "core.role.delete";

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly SaveCapture _capture = new();

    private CoreDbContext Build(params IInterceptor[] extra)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(_actorId);
        currentUser.UserName.Returns("quantri");
        var auditLog = new AuditLogInterceptor(
            TimeProvider.System, currentUser, Substitute.For<IClientAddressAccessor>(), new PasswordRehashScope(), new CrossTenantActorScope());

        return OfflineCoreDbContext.Create(_tenantId, [.. extra, auditLog, _capture]);
    }

    private IReadOnlyList<AuditLog> RoleAssignRows
        => _capture.AuditRows.Where(r => r.ActionCode == RoleAssign).ToList();

    // Tên cố định — RULES.md §6 S21 lớp (1). Cấp cho A và gỡ của B trong CÙNG một lượt lưu: đúng hai dòng, mỗi dòng mang
    // đúng danh sách của người đó. Vai trò giữ nguyên của mỗi người không được xuất hiện trong danh sách nào.
    [Fact]
    public async Task UserRoleAssign_WritesOneAuditRowPerTouchedUser_WithGrantedAndRevokedRoles()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var binh = TrackedUser(db, "binh", "Trần Thị Bình");
        var view = TrackedRole(db, "Xem");
        var accountant = TrackedRole(db, "Kế toán");
        var cashier = TrackedRole(db, "Thủ quỹ");
        TrackedAssignment(db, an.Id, view.Id);
        TrackedAssignment(db, binh.Id, view.Id);
        var binhCashier = TrackedAssignment(db, binh.Id, cashier.Id);

        AddAssignment(db, an.Id, accountant.Id);
        db.Remove(binhCashier);

        await db.SaveChangesAsync();

        RoleAssignRows.Count.ShouldBe(2);

        var (anGranted, anRevoked) = Lists(RoleAssignRows.Single(r => r.TargetId == an.Id.ToString()));
        anGranted.ShouldBe([(accountant.Id, "Kế toán")]);
        anRevoked.ShouldBeEmpty();

        var (binhGranted, binhRevoked) = Lists(RoleAssignRows.Single(r => r.TargetId == binh.Id.ToString()));
        binhGranted.ShouldBeEmpty();
        binhRevoked.ShouldBe([(cashier.Id, "Thủ quỹ")]);
    }

    // contracts/users.md §7 luật 1: MỘT dòng cho mỗi người dùng bị chạm — không tách một dòng cấp, một dòng gỡ. Cặp đỏ:
    // UserRoleAssign_WritesOneAuditRowPerTouchedUser_WithGrantedAndRevokedRoles — cùng vai trò, cùng một lần cấp và một lần
    // gỡ trong một lượt, khác đúng việc lần gỡ rơi vào CÙNG người được cấp: ở đó hai dòng, ở đây một.
    [Fact]
    public async Task UserRoleAssign_SameUserGrantedAndRevokedInOneSave_WritesOneRowCarryingBothLists()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var view = TrackedRole(db, "Xem");
        var accountant = TrackedRole(db, "Kế toán");
        var cashier = TrackedRole(db, "Thủ quỹ");
        TrackedAssignment(db, an.Id, view.Id);
        var anCashier = TrackedAssignment(db, an.Id, cashier.Id);

        AddAssignment(db, an.Id, accountant.Id);
        db.Remove(anCashier);

        await db.SaveChangesAsync();

        var row = RoleAssignRows.ShouldHaveSingleItem();
        row.TargetId.ShouldBe(an.Id.ToString());
        var (granted, revoked) = Lists(row);
        granted.ShouldBe([(accountant.Id, "Kế toán")]);
        revoked.ShouldBe([(cashier.Id, "Thủ quỹ")]);
    }

    // target_display là họ tên của CHÍNH người trên dòng đó — hai người trong một lượt không được mượn tên của nhau.
    [Fact]
    public async Task UserRoleAssign_TwoUsersInOneSave_EachRowTargetsItsOwnUsersFullName()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var binh = TrackedUser(db, "binh", "Trần Thị Bình");
        var accountant = TrackedRole(db, "Kế toán");

        AddAssignment(db, an.Id, accountant.Id);
        AddAssignment(db, binh.Id, accountant.Id);
        await db.SaveChangesAsync();

        RoleAssignRows.Single(r => r.TargetId == an.Id.ToString()).TargetDisplay.ShouldBe("Nguyễn Văn An");
        RoleAssignRows.Single(r => r.TargetId == binh.Id.ToString()).TargetDisplay.ShouldBe("Trần Thị Bình");
    }

    [Fact]
    public async Task UserRoleAssign_AuditRow_TargetsTheUserByIdAndFullName_WithActorAndNoBeforeValue()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var accountant = TrackedRole(db, "Kế toán");

        AddAssignment(db, an.Id, accountant.Id);
        await db.SaveChangesAsync();

        var row = RoleAssignRows.ShouldHaveSingleItem();
        row.TenantId.ShouldBe(_tenantId);
        row.TargetType.ShouldBe("core.user");
        row.TargetId.ShouldBe(an.Id.ToString());
        row.TargetDisplay.ShouldBe("Nguyễn Văn An");
        row.ActorUserId.ShouldBe(_actorId);
        row.BeforeValue.ShouldBeNull();
    }

    // contracts/users.md §7: "Sắp theo name (so ordinal)". Ba tên chọn để thứ tự ordinal KHÁC thứ tự theo văn hoá: ordinal
    // đặt chữ hoa ASCII trước chữ thường, và `Đ` (U+0110) sau mọi chữ ASCII; sắp theo văn hoá tiếng Việt thì
    // "báo cáo" < "Đối soát" < "Kế toán". Cấp theo thứ tự KHÔNG sắp — dòng nhật ký phải tự sắp.
    [Fact]
    public async Task UserRoleAssign_GrantedRoles_AreSortedByOrdinalName_NotByCulture()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var reconcile = TrackedRole(db, "Đối soát");
        var reports = TrackedRole(db, "báo cáo");
        var accountant = TrackedRole(db, "Kế toán");

        AddAssignment(db, an.Id, reconcile.Id);
        AddAssignment(db, an.Id, reports.Id);
        AddAssignment(db, an.Id, accountant.Id);
        await db.SaveChangesAsync();

        var (granted, _) = Lists(RoleAssignRows.ShouldHaveSingleItem());
        granted.ShouldBe([(accountant.Id, "Kế toán"), (reports.Id, "báo cáo"), (reconcile.Id, "Đối soát")]);
    }

    // contracts/users.md §7 cột after_value: tên không tra được thì `name` là null — KHÔNG thay bằng id (ADR-0083: một danh
    // sách trộn hai kiểu). Hai phần tử cùng null tên vẫn có thứ tự xác định: theo id, ordinal trên chuỗi. Hai id ngẫu nhiên
    // được cấp theo thứ tự chuỗi GIẢM DẦN, nên một danh sách không tự sắp ra sai thứ tự.
    // Cặp đỏ: UserRoleAssign_OnlyOneRoleNameResolvable_KeepsThatName_AndPutsTheNullNameFirst.
    [Fact]
    public async Task UserRoleAssign_RoleNameNotResolvable_WritesNullName_NeverTheId_AndOrdersById()
    {
        var recorder = new SqlRecorder(emptyReaders: true);
        await using var db = Build(recorder.Interceptors);
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var ghosts = new[] { Guid.NewGuid(), Guid.NewGuid() }
            .OrderByDescending(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (var ghost in ghosts)
            AddAssignment(db, an.Id, ghost);

        await db.SaveChangesAsync();

        recorder.Executed.ShouldContain(sql => sql.Contains("app_role", StringComparison.Ordinal),
            "chống test rỗng: nhánh tra tên vai trò từ database phải thật sự chạy");
        var (granted, revoked) = Lists(RoleAssignRows.ShouldHaveSingleItem());
        granted.ShouldBe(ghosts
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .Select(id => (id, (string?)null))
            .ToArray());
        revoked.ShouldBeEmpty();
    }

    // Cặp đỏ của ca trên: cùng harness SqlRecorder, cùng người, cùng hai lần cấp — khác đúng việc MỘT trong hai vai trò có
    // trong bộ theo dõi. Vai trò đó ra tên thật, vai trò kia vẫn null; contracts/users.md §7: `name` null đứng ĐẦU danh
    // sách. Vai trò có tên được cấp TRƯỚC — danh sách không tự sắp thì phần tử null không đứng đầu.
    [Fact]
    public async Task UserRoleAssign_OnlyOneRoleNameResolvable_KeepsThatName_AndPutsTheNullNameFirst()
    {
        var recorder = new SqlRecorder(emptyReaders: true);
        await using var db = Build(recorder.Interceptors);
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var accountant = TrackedRole(db, "Kế toán");
        var ghost = Guid.NewGuid();

        AddAssignment(db, an.Id, accountant.Id);
        AddAssignment(db, an.Id, ghost);
        await db.SaveChangesAsync();

        var (granted, _) = Lists(RoleAssignRows.ShouldHaveSingleItem());
        granted.ShouldBe([(ghost, null), (accountant.Id, "Kế toán")]);
    }

    // Cặp đỏ: UserRoleAssign_WritesOneAuditRowPerTouchedUser_WithGrantedAndRevokedRoles — cùng người, cùng vai trò đã gán,
    // khác đúng việc có thêm/xoá dòng.
    [Fact]
    public async Task UserRoleAssign_NothingAddedOrRemoved_WritesNoRow()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var accountant = TrackedRole(db, "Kế toán");
        TrackedAssignment(db, an.Id, accountant.Id);

        await db.SaveChangesAsync();

        RoleAssignRows.ShouldBeEmpty();
    }

    // contracts/users.md §7 luật 1 nói "thêm hoặc xoá": dòng chỉ bị đánh dấu sửa (DbContext.Update) không đổi tập vai trò.
    // Cặp đỏ: UserRoleAssign_AssignmentRemoved_WhileTheRoleStays_WritesARevocationRow — cùng dòng gán, khác đúng trạng thái
    // (Modified thay vì Deleted).
    [Fact]
    public async Task UserRoleAssign_AssignmentOnlyMarkedModified_WritesNoRow()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var accountant = TrackedRole(db, "Kế toán");
        var assignment = TrackedAssignment(db, an.Id, accountant.Id);

        db.Update(assignment);
        db.Entry(assignment).State.ShouldBe(EntityState.Modified, "chống test rỗng: dòng gán phải thật sự ở trạng thái sửa");

        await db.SaveChangesAsync();

        RoleAssignRows.ShouldBeEmpty();
    }

    [Fact]
    public async Task UserRoleAssign_AssignmentRemoved_WhileTheRoleStays_WritesARevocationRow()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var accountant = TrackedRole(db, "Kế toán");
        var assignment = TrackedAssignment(db, an.Id, accountant.Id);

        db.Remove(assignment);
        await db.SaveChangesAsync();

        var (granted, revoked) = Lists(RoleAssignRows.ShouldHaveSingleItem());
        granted.ShouldBeEmpty();
        revoked.ShouldBe([(accountant.Id, "Kế toán")]);
    }

    // contracts/users.md §7 luật 2: dòng gán bị xoá vì vai trò chủ cũng bị xoá cùng lượt không tính — dòng core.role.delete
    // đóng chuỗi. Cặp đỏ: UserRoleAssign_AssignmentRemoved_WhileTheRoleStays_WritesARevocationRow — cùng dòng gán bị xoá,
    // khác đúng việc vai trò chủ cũng bị xoá.
    [Fact]
    public async Task UserRoleAssign_RoleDeletedInTheSameSave_WritesNoRowForItsAssignments()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var accountant = TrackedRole(db, "Kế toán");
        var assignment = TrackedAssignment(db, an.Id, accountant.Id);

        db.Remove(accountant);
        db.Remove(assignment);
        db.Entry(assignment).State.ShouldBe(EntityState.Deleted, "chống test rỗng: dòng gán phải thật sự ở trạng thái xoá");
        await db.SaveChangesAsync();

        _capture.AuditRows.ShouldContain(r => r.ActionCode == RoleDelete,
            "chống test rỗng: lượt lưu phải thật sự đi qua interceptor — dòng đóng chuỗi có mặt");
        RoleAssignRows.ShouldBeEmpty();
    }

    // Ngoại lệ của luật 2 chỉ bỏ ĐÚNG dòng gán của vai trò bị xoá: cùng người, cùng lượt, một lần cấp khác vẫn phải có dòng —
    // và danh sách bị thu không được mang vai trò đã xoá.
    [Fact]
    public async Task UserRoleAssign_RoleDeletedInTheSameSave_IsLeftOutOfRevoked_WhileAnotherGrantStillWritesARow()
    {
        await using var db = Build();
        var an = TrackedUser(db, "an", "Nguyễn Văn An");
        var accountant = TrackedRole(db, "Kế toán");
        var cashier = TrackedRole(db, "Thủ quỹ");
        var assignment = TrackedAssignment(db, an.Id, accountant.Id);

        db.Remove(accountant);
        db.Remove(assignment);
        AddAssignment(db, an.Id, cashier.Id);
        await db.SaveChangesAsync();

        var (granted, revoked) = Lists(RoleAssignRows.ShouldHaveSingleItem());
        granted.ShouldBe([(cashier.Id, "Thủ quỹ")]);
        revoked.ShouldBeEmpty();
    }

    // contracts/users.md §7 luật 1: tạo người dùng kèm vai trò sinh HAI dòng — core.user.create và dòng này — không gộp.
    // Luật không phụ thuộc ranh giới lượt lưu: vai trò rơi vào cùng lượt với tài khoản hay vào lượt kế sau đều ra đúng hai
    // dòng cho người đó, không thêm không bớt.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UserRoleAssign_NewUserCreatedWithARole_WritesItsOwnRowBesideUserCreate(bool rolesInALaterSave)
    {
        await using var db = Build();
        var accountant = TrackedRole(db, "Kế toán");
        var an = NewUser(db, "an", "Nguyễn Văn An");
        if (rolesInALaterSave)
            await db.SaveChangesAsync();

        AddAssignment(db, an.Id, accountant.Id);
        await db.SaveChangesAsync();

        var userId = an.Id.ToString();
        _capture.AuditRows.Where(r => r.TargetId == userId).Select(r => r.ActionCode).OrderBy(c => c, StringComparer.Ordinal)
            .ShouldBe([UserCreate, RoleAssign]);
        var (granted, revoked) = Lists(RoleAssignRows.ShouldHaveSingleItem());
        granted.ShouldBe([(accountant.Id, "Kế toán")]);
        revoked.ShouldBeEmpty();
    }

    // target_display của người vừa tạo là họ tên, không phải tên đăng nhập — lấy được cả khi tài khoản còn ở trạng thái
    // Added trong cùng lượt lẫn khi nó đã được lưu ở lượt trước.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UserRoleAssign_NewUserCreatedWithARole_RowTargetsTheNewUsersFullName(bool rolesInALaterSave)
    {
        await using var db = Build();
        var accountant = TrackedRole(db, "Kế toán");
        var chi = NewUser(db, "chi", "Lê Văn Chi");
        if (rolesInALaterSave)
            await db.SaveChangesAsync();

        AddAssignment(db, chi.Id, accountant.Id);
        await db.SaveChangesAsync();

        var row = RoleAssignRows.ShouldHaveSingleItem();
        row.TargetId.ShouldBe(chi.Id.ToString());
        row.TargetDisplay.ShouldBe("Lê Văn Chi");
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    // after_value: đúng hai khoá "granted", "revoked" theo thứ tự đó; mỗi phần tử PHẢI là object đúng hai khoá "id", "name" —
    // không chuỗi trần, không khoá thừa, `name` null vẫn khai tường minh.
    private static ((Guid Id, string? Name)[] Granted, (Guid Id, string? Name)[] Revoked) Lists(AuditLog row)
    {
        row.AfterValue.ShouldNotBeNull();
        using var json = JsonDocument.Parse(row.AfterValue);
        json.RootElement.EnumerateObject().Select(p => p.Name).ShouldBe(["granted", "revoked"]);

        return (Read(json.RootElement, "granted"), Read(json.RootElement, "revoked"));

        static (Guid Id, string? Name)[] Read(JsonElement root, string name)
            => root.GetProperty(name).EnumerateArray()
                .Select(element =>
                {
                    element.ValueKind.ShouldBe(JsonValueKind.Object);
                    element.EnumerateObject().Select(p => p.Name).ShouldBe(["id", "name"]);
                    var nameElement = element.GetProperty("name");
                    return (element.GetProperty("id").GetGuid(),
                        nameElement.ValueKind == JsonValueKind.Null ? null : nameElement.GetString());
                })
                .ToArray();
    }

    private AppUser TrackedUser(CoreDbContext db, string userName, string fullName)
    {
        var user = AppUser.NewAccount(userName, $"{userName}@dv.vn", fullName);
        var entry = db.Entry(user);
        entry.Property(x => x.TenantId).CurrentValue = _tenantId;
        entry.State = EntityState.Unchanged;
        return user;
    }

    // Tài khoản ở trạng thái Added — như vừa tạo, chưa lưu.
    private AppUser NewUser(CoreDbContext db, string userName, string fullName)
    {
        var user = AppUser.NewAccount(userName, $"{userName}@dv.vn", fullName);
        db.Add(user);
        db.Entry(user).Property(x => x.TenantId).CurrentValue = _tenantId;
        return user;
    }

    private AppRole TrackedRole(CoreDbContext db, string name)
    {
        var role = new AppRole { TenantId = _tenantId, Name = name, NormalizedName = name.ToUpperInvariant() };
        db.Attach(role);
        return role;
    }

    private AppUserRole TrackedAssignment(CoreDbContext db, Guid userId, Guid roleId)
    {
        var row = new AppUserRole { TenantId = _tenantId, UserId = userId, RoleId = roleId };
        db.Attach(row);
        return row;
    }

    // TenantId gán tay: harness không có TenantAssignmentInterceptor (đường thật gán nó lúc Added, trước AuditLogInterceptor).
    private void AddAssignment(CoreDbContext db, Guid userId, Guid roleId)
        => db.Add(new AppUserRole { TenantId = _tenantId, UserId = userId, RoleId = roleId });
}
