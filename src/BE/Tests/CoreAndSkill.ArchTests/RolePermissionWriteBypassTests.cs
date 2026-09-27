using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// S17 — docs/RULES.md §6, docs/adr/0052-ghi-ma-tran-phan-quyen-luon-vao-nhat-ky-kiem-toan.md luật 4. Nhật ký kiểm toán của
// ma trận phân quyền đi qua AuditLogInterceptor, tức qua ChangeTracker. Một lần ghi core.role_permission bằng
// ExecuteUpdate/ExecuteDelete hay SQL thô không để lại dòng nào — đúng loại leo thang quyền mà ADR-0052 muốn truy được.
//
// Tầm quét: mọi tệp .cs sản phẩm dưới src/BE/Core (năm project Core) và host src/BE/CoreAndSkill.Api. Loại bin/, obj/ và
// mọi thư mục Migrations/. Host thuộc vùng dự án, ngoài khối core-paths, nhưng vẫn quét: composition root là chỗ tự nhiên
// để cắm nhầm một đường ghi vòng, và cổng biết host bằng tên project — quyết định 8 của
// docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md.
public class RolePermissionWriteBypassTests
{
    [Fact]
    public void RolePermission_IsNeverWritten_BypassingChangeTracker()
    {
        var offenders = EnumerateProductSourceFiles()
            .SelectMany(file => RolePermissionWriteBypassScanner.Scan(File.ReadAllText(file), file))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 — tầm quét phải chạm tệp thật: tệp ghi ma trận qua ChangeTracker, và một tệp dùng ExecuteUpdateAsync thật (trên
    // bảng khác) — cổng phải NHÌN thấy đường ghi hàng loạt thì chữ "không bắt" của nó mới có nghĩa.
    [Fact]
    public void RolePermission_IsNeverWritten_BypassingChangeTracker_ScansRealWritersAndRealBulkUpdates()
    {
        var files = EnumerateProductSourceFiles();

        files.ShouldContain(f => f.EndsWith("PermissionMatrixService.cs", StringComparison.Ordinal));
        files.ShouldContain(f => File.ReadAllText(f).Contains(".ExecuteUpdateAsync(", StringComparison.Ordinal));
        files.ShouldContain(f => f.EndsWith("CoreAndSkill.Api" + Path.DirectorySeparatorChar + "Program.cs", StringComparison.Ordinal));
        files.ShouldNotContain(f => f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_S17_Catches_ExecuteUpdate_OnRolePermissions()
    {
        const string source = """
            public sealed class Fake
            {
                public async System.Threading.Tasks.Task Run(FakeDb db, System.Guid roleId)
                    => await db.RolePermissions
                        .Where(rp => rp.RoleId == roleId)
                        .ExecuteUpdateAsync(s => s.SetProperty(rp => rp.IsDeleted, true));
            }
            """;

        RolePermissionWriteBypassScanner.Scan(source, "fake.cs").ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_S17_Catches_ExecuteDelete_OnSetOfRolePermission()
    {
        const string source = """
            public sealed class Fake
            {
                public int Run(FakeDb db) => db.Set<CoreAndSkill.Core.Domain.Permissions.RolePermission>().ExecuteDelete();
            }
            """;

        RolePermissionWriteBypassScanner.Scan(source, "fake.cs").ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_S17_Catches_RawSqlWritingRolePermission()
    {
        const string source = """
            public sealed class Fake
            {
                public System.Threading.Tasks.Task<int> Run(FakeDb db)
                    => db.Database.ExecuteSqlRawAsync("UPDATE core.role_permission SET is_deleted = true WHERE role_id = @p0");
            }
            """;

        RolePermissionWriteBypassScanner.Scan(source, "fake.cs").ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_S17_Catches_InterpolatedAndRawStringSql()
    {
        const string interpolated = """
            public sealed class Fake
            {
                public System.Threading.Tasks.Task<int> Run(FakeDb db, System.Guid id)
                    => db.Database.ExecuteSqlAsync($"insert into core.role_permission (id) values ({id})");
            }
            """;

        const string raw = """"
            public sealed class Fake
            {
                private const string Sql = """
                    DELETE FROM core.role_permission
                    WHERE tenant_id = @tenant
                    """;
            }
            """";

        RolePermissionWriteBypassScanner.Scan(interpolated, "fake.cs").ShouldNotBeEmpty();
        RolePermissionWriteBypassScanner.Scan(raw, "fake.cs").ShouldNotBeEmpty();
    }

    // Đúng hình dạng thật của EfJobRepository.cs: ghi hàng loạt trên bảng KHÁC — không phải thứ S17 cấm.
    [Fact]
    public void Detector_S17_Ignores_ExecuteUpdate_OnOtherTable()
    {
        const string source = """
            public sealed class Fake
            {
                public System.Threading.Tasks.Task<int> Run(FakeDb db, System.Guid id)
                    => db.Jobs.Where(j => j.Id == id).ExecuteUpdateAsync(s => s.SetProperty(j => j.Progress, 5));
            }
            """;

        RolePermissionWriteBypassScanner.Scan(source, "fake.cs").ShouldBeEmpty();
    }

    // Đúng hình dạng thật của RolePermissionConfiguration.cs: tên bảng, tên khoá, tên index — không động từ SQL nào.
    [Fact]
    public void Detector_S17_Ignores_TableMappingLiterals()
    {
        const string source = """
            internal sealed class FakeConfiguration
            {
                public void Configure(FakeBuilder builder)
                {
                    builder.ToTable("role_permission");
                    builder.HasKey(x => x.Id).HasName("pk_role_permission");
                    builder.HasIndex(x => x.RoleId).HasDatabaseName("ux_role_permission_tenant_role_perm_active");
                }
            }
            """;

        RolePermissionWriteBypassScanner.Scan(source, "fake.cs").ShouldBeEmpty();
    }

    // Đúng hình dạng thật của PermissionMatrixService.cs: ghi qua ChangeTracker — đường S17 đòi.
    [Fact]
    public void Detector_S17_Ignores_ChangeTrackerWrites()
    {
        const string source = """
            public sealed class Fake
            {
                public void Run(FakeDb db, FakeRow row, FakeRow added)
                {
                    row.IsDeleted = true;
                    db.RolePermissions.Add(added);
                }
            }
            """;

        RolePermissionWriteBypassScanner.Scan(source, "fake.cs").ShouldBeEmpty();
    }

    private static IReadOnlyList<string> EnumerateProductSourceFiles([CallerFilePath] string here = "")
    {
        var beRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
        var separator = Path.DirectorySeparatorChar;

        return new[] { Path.Combine(beRoot, "Core"), Path.Combine(beRoot, "CoreAndSkill.Api") }
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{separator}Migrations{separator}", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }
}
