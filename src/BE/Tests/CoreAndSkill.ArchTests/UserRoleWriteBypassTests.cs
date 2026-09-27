using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// S21 lớp (2) — docs/RULES.md §6, docs/adr/0083-doi-vai-tro-nguoi-dung-luon-vao-nhat-ky-kiem-toan.md, docs/contracts/users.md
// §7 mục "Nhật ký kiểm toán" luật 3. Nhật ký đổi vai trò người dùng đi qua AuditLogInterceptor, tức qua ChangeTracker. Một
// lần ghi core.app_user_role bằng ExecuteUpdate/ExecuteDelete hay SQL thô không để lại dòng core.user.role_assign nào.
// Cùng phép dò với S17 lớp (2) — ChangeTrackerBypassScanner, đích ChangeTrackerBypassTarget.AppUserRole.
//
// Tầm quét: mọi tệp .cs sản phẩm dưới src/BE/Core (năm project Core) và host src/BE/CoreAndSkill.Api. Loại bin/, obj/ và
// mọi thư mục Migrations/.
//
// T12:cap-xanh-do — mọi ca Detector_* kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng phép dò, dựng từ CÙNG khuôn nguồn
// và khác đúng một đối số của khuôn (docs/RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
public class UserRoleWriteBypassTests
{
    private static readonly ChangeTrackerBypassTarget Target = ChangeTrackerBypassTarget.AppUserRole;

    [Fact]
    public void AppUserRole_IsNeverWritten_BypassingChangeTracker()
    {
        var offenders = ScannedFiles()
            .SelectMany(file => ChangeTrackerBypassScanner.Scan(File.ReadAllText(file), file, Target))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 — tầm quét phải chạm tệp thật: tệp ghi app_user_role qua ChangeTracker, một tệp dùng ghi hàng loạt thật (trên
    // bảng khác), và host. Cổng phải NHÌN thấy đường ghi hàng loạt thì chữ "không bắt" của nó mới có nghĩa.
    [Fact]
    public void AppUserRole_IsNeverWritten_BypassingChangeTracker_ScansRealWritersAndRealBulkUpdates()
    {
        var files = ScannedFiles();

        files.ShouldContain(f => ProductSourceFiles.EndsWith(f, "Identity", "UserAdminService.cs"));
        files.ShouldContain(f => File.ReadAllText(f).Contains(".ExecuteUpdateAsync(", StringComparison.Ordinal));
        files.ShouldContain(f => ProductSourceFiles.EndsWith(f, "CoreAndSkill.Api", "Program.cs"));
        files.ShouldNotContain(f => f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    // T6 — phép dò khớp theo TÊN: tên DbSet, tên kiểu, tên bảng. Một tên đã chết (DbSet đổi tên, bảng đổi tên) làm cổng
    // xanh vì không lời gọi nào còn nhắc tới nó — trong khi ca Detector_* vẫn xanh vì nguồn giả dùng đúng chuỗi của đích.
    // Ca này đối chiếu từng tên với mã sản phẩm THẬT.
    [Fact]
    public void AppUserRole_BypassTarget_NamesTheRealDbSetEntityAndTable()
    {
        var sources = ScannedFiles().Select(File.ReadAllText).ToList();

        sources.ShouldContain(s => s.Contains($".{Target.DbSetName}", StringComparison.Ordinal),
            $"không tệp sản phẩm nào nhắc DbSet '{Target.DbSetName}'");
        sources.ShouldContain(s => s.Contains($"class {Target.EntityTypeName} ", StringComparison.Ordinal),
            $"không tệp sản phẩm nào khai kiểu '{Target.EntityTypeName}'");
        sources.ShouldContain(s => s.Contains($"ToTable(\"{Target.TableName}\")", StringComparison.Ordinal),
            $"không cấu hình EF nào ánh xạ bảng '{Target.TableName}'");
    }

    // ------------------------------------------------------------------ ghi hàng loạt

    [Theory]
    [InlineData("db.UserRoles", "ExecuteDeleteAsync")]
    [InlineData("db.UserRoles", "ExecuteDelete")]
    [InlineData("db.UserRoles", "ExecuteUpdateAsync")]
    [InlineData("db.UserRoles", "ExecuteUpdate")]
    [InlineData("db.Set<CoreAndSkill.Core.Infrastructure.Identity.AppUserRole>()", "ExecuteDeleteAsync")]
    [InlineData("db.Set<AppUserRole>()", "ExecuteDelete")]
    public void Detector_S21_Catches_BulkWrite_OnAppUserRole(string receiver, string method)
    {
        ChangeTrackerBypassScanner.Scan(BulkWrite(receiver, method), "fake.cs", Target).ShouldHaveSingleItem();
    }

    // Đúng hình dạng thật của EfJobRepository.cs: ghi hàng loạt trên bảng KHÁC — không phải thứ S21 cấm.
    // Cặp đỏ: Detector_S21_Catches_BulkWrite_OnAppUserRole("db.UserRoles", "ExecuteDeleteAsync") — cùng khuôn, cùng phương
    // thức, khác đúng DbSet nhận lời gọi.
    [Fact]
    public void Detector_S21_Ignores_BulkWrite_OnOtherTable()
    {
        ChangeTrackerBypassScanner.Scan(BulkWrite("db.Jobs", "ExecuteDeleteAsync"), "fake.cs", Target).ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ ghi qua ChangeTracker

    // Đúng hình dạng thật của UserAdminService.AssignRolesAsync: RemoveRange / Add qua ChangeTracker — đường S21 đòi.
    // Cặp đỏ: Detector_S21_Catches_SameRowsRemoved_ByExecuteDelete — cùng khuôn, cùng tập dòng, khác đúng cách xoá.
    [Fact]
    public void Detector_S21_Ignores_ChangeTrackerWrites()
    {
        ChangeTrackerBypassScanner.Scan(RemoveRows("db.UserRoles.RemoveRange(db.UserRoles.Where(ur => ur.UserId == userId))"),
            "fake.cs", Target).ShouldBeEmpty();
    }

    [Fact]
    public void Detector_S21_Catches_SameRowsRemoved_ByExecuteDelete()
    {
        ChangeTrackerBypassScanner.Scan(RemoveRows("db.UserRoles.Where(ur => ur.UserId == userId).ExecuteDelete()"),
            "fake.cs", Target).ShouldHaveSingleItem();
    }

    // ------------------------------------------------------------------ SQL thô

    // Đúng hình dạng thật của AppUserRoleConfiguration.cs: tên bảng trong một literal, không động từ SQL nào.
    // Cặp đỏ: Detector_S21_Catches_RawSqlWritingAppUserRole("TRUNCATE app_user_role") — cùng khuôn, cùng tên bảng, khác
    // đúng một động từ SQL.
    [Fact]
    public void Detector_S21_Ignores_TableMappingLiteral()
    {
        ChangeTrackerBypassScanner.Scan(Literal("app_user_role"), "fake.cs", Target).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("TRUNCATE app_user_role")]
    [InlineData("DELETE FROM core.app_user_role WHERE user_id = @p0")]
    [InlineData("insert into core.app_user_role (user_id, role_id) values (@p0, @p1)")]
    public void Detector_S21_Catches_RawSqlWritingAppUserRole(string sql)
    {
        ChangeTrackerBypassScanner.Scan(Literal(sql), "fake.cs", Target).ShouldHaveSingleItem();
    }

    [Fact]
    public void Detector_S21_Catches_InterpolatedAndRawStringSql()
    {
        const string interpolated = """
            public sealed class Fake
            {
                public System.Threading.Tasks.Task<int> Run(FakeDb db, System.Guid id)
                    => db.Database.ExecuteSqlAsync($"delete from core.app_user_role where user_id = {id}");
            }
            """;

        const string raw = """"
            public sealed class Fake
            {
                private const string Sql = """
                    UPDATE core.app_user_role
                    SET role_id = @role
                    """;
            }
            """";

        ChangeTrackerBypassScanner.Scan(interpolated, "fake.cs", Target).ShouldNotBeEmpty();
        ChangeTrackerBypassScanner.Scan(raw, "fake.cs", Target).ShouldNotBeEmpty();
    }

    // Tên ràng buộc và bảng có tên bắt đầu bằng tên bảng đích không phải bảng đích — tên bảng khớp như một TỪ.
    // Cặp đỏ: Detector_S21_Catches_RawSqlWritingAppUserRole("DELETE FROM core.app_user_role WHERE user_id = @p0") — cùng
    // khuôn, cùng động từ, khác đúng tên bảng.
    [Fact]
    public void Detector_S21_Ignores_WriteOnATableThatOnlySharesThePrefix()
    {
        ChangeTrackerBypassScanner.Scan(Literal("DELETE FROM core.app_user_role_archive WHERE user_id = @p0"), "fake.cs", Target)
            .ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ khuôn nguồn

    private static string BulkWrite(string receiver, string method) => $$"""
        public sealed class Fake
        {
            public void Run(FakeDb db, System.Guid userId)
                => {{receiver}}.Where(ur => ur.UserId == userId).{{method}}();
        }
        """;

    private static string RemoveRows(string call) => $$"""
        public sealed class Fake
        {
            public void Run(FakeDb db, System.Guid userId) => {{call}};
        }
        """;

    private static string Literal(string text) => $$"""
        internal sealed class Fake
        {
            public object Run(FakeBuilder builder) => builder.Use("{{text}}");
        }
        """;

    private static IReadOnlyList<string> ScannedFiles()
        => ProductSourceFiles.Core()
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();
}
