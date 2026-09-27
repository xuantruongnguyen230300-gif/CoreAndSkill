using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Application.Identity;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Tên tác nhân hệ thống chỉ có MỘT nguồn — SystemActor.UserName. Luật nằm ở docs/RULES.md, dòng nêu tên
// CoreSource_MustNotHandType_TheSystemActorName ở cột "Ép bằng gì". Vì sao cổng tồn tại và nó KHÔNG bắt gì: đầu
// Support/SystemActorLiteralScanner.cs — đọc ở đó, không chép lại ở đây.
//
// Tầm quét: ProductSourceFiles.Core — định nghĩa tầm Core DÙNG CHUNG với các cổng khác (năm project dưới src/BE/Core và
// host src/BE/CoreAndSkill.Api; loại bin/, obj/). Host thuộc vùng dự án, ngoài khối core-paths, nhưng vẫn quét: cổng biết
// nó bằng tên project — docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md quyết định 8. Cổng này chỉ
// TRỪ BỚT, không tự dựng tầm riêng: mọi thư mục Migrations/ (migration là lịch sử, không sửa lại) và ĐÚNG MỘT tệp theo
// đường dẫn đầy đủ — tệp khai hằng. Script SQL không nằm trong tầm.
//
// T12:cap-xanh-do — mọi ca Detector_* kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng phép dò, dựng từ CÙNG khuôn nguồn và
// khác đúng một chỗ (docs/RULES.md §8 luật T12); chú thích "Cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
//
// Tự kiểm (2026-09-25) — đột biến chỉ trên tệp test và scanner, KHÔNG chạm mã sản phẩm; mỗi vòng khôi phục rồi so SHA-256
// với bản gốc: (1) đọc tệp thật qua `.Replace("SystemActor.UserName", "\"system\"")` — cổng chính đỏ đúng mười chỗ: chín
// chỗ đã thay literal bằng hằng ở Core.Infrastructure và lỗ nội suy của NotReservedUserNameAttribute; (2) bỏ miễn trừ
// SystemActor.cs — cổng chính đỏ ở dòng khai hằng; (3) so bằng Contains thay vì Equals — cổng chính đỏ oan trên mười một
// chuỗi thật chứa chữ system (mã lỗi CORE.*.SYSTEM_*, "is_system", "/system/tenants"…); (4) dò bằng văn bản thay vì AST —
// cổng chính đỏ oan trên chú thích thật; (5) tầm quét rỗng — cổng chính VẪN XANH, chỉ ba chốt T6 đỏ; (6) bỏ loại trừ
// Migrations/ — cổng chính vẫn xanh: chuỗi SQL `'system'` trong migration seed là chuỗi dài hơn, không phải literal đúng bằng.
// Sau khi mở tầm ra host (cùng ngày): (7) nối vào văn bản đọc được của CoreAndSkill.Api/Program.cs một lớp mang literal
// "system" — cổng chính đỏ đúng một chỗ ở tệp đó; cùng đột biến trên bản tầm cũ (chỉ Core/) thì xanh; (8) loại host khỏi tầm
// quét — hai chốt T6 đỏ.
public class SystemActorLiteralTests
{
    // Đường dẫn tương đối so với src/BE, dấu `/` — khớp ProductSourceFiles.RelativePath trên cả Windows lẫn Linux. Miễn trừ
    // theo ĐƯỜNG DẪN ĐẦY ĐỦ, không theo tên tệp: một tệp SystemActor.cs thứ hai ở chỗ khác không được miễn trừ theo.
    private const string OwnerFile = "Core/CoreAndSkill.Core.Application/Identity/SystemActor.cs";

    // ===== Luật thật =====

    [Fact]
    public void CoreSource_MustNotHandType_TheSystemActorName()
    {
        var offenders = ScannedFiles()
            .SelectMany(file => SystemActorLiteralScanner.Scan(File.ReadAllText(file), file))
            .Select(hit => $"{ProductSourceFiles.RelativePath(hit.FilePath)}:{hit.Line} · {hit.Literal} — gõ tay tên tác nhân "
                         + $"hệ thống. Dùng SystemActor.UserName ({OwnerFile}): phép chặn tạo tài khoản mang tên này và "
                         + "FileAccessPolicy.IsUploader dựa vào hằng đó, một chỗ lệch khỏi hằng làm phép chặn hết tác dụng.")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // ===== Meta-test — kim dò đọc từ hằng, nên giá trị của hằng phải được ghim riêng =====

    // Scanner lấy kim dò từ SystemActor.UserName chứ không gõ lại. Đổi hằng thì cổng đi dò giá trị mới — nhưng mọi dòng
    // created_by / updated_by / actor_display đã ghi, và mọi dòng seed trong migration, vẫn mang giá trị cũ. Ca này đỏ ở
    // đúng lúc đó.
    [Fact]
    public void SystemActor_UserName_IsExactly_system()
        => SystemActor.UserName.ShouldBe("system");

    // ===== Chốt chống xanh rỗng (T6) =====

    // Mỗi project của tầm Core — mọi *.csproj dưới src/BE/Core và dưới host src/BE/CoreAndSkill.Api, đọc từ đĩa chứ không
    // từ danh sách gõ tay — góp ít nhất một tệp vào tầm quét; host góp đúng tệp khởi động của nó; và tầm quét chạm đúng
    // những tệp từng mang literal khi luật ra đời.
    [Fact]
    public void TheSystemActorGate_ScansEveryCoreProject()
    {
        var scanned = ScannedFiles();
        var separator = Path.DirectorySeparatorChar;

        var projectDirectories = new[] { CoreRoot(), HostRoot() }
            .SelectMany(root => Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetDirectoryName(p)! + separator)
            .ToList();

        projectDirectories.ShouldContain(p => p.StartsWith(CoreRoot() + separator, StringComparison.Ordinal),
            $"không tìm thấy *.csproj nào dưới '{CoreRoot()}' — gốc quét đã dời");
        projectDirectories.ShouldContain(HostRoot() + separator,
            $"không tìm thấy *.csproj của host ở '{HostRoot()}' — host đã dời");

        foreach (var project in projectDirectories)
        {
            scanned.ShouldContain(f => f.StartsWith(project, StringComparison.Ordinal),
                $"project '{project}' không góp tệp nào vào tầm quét — cổng xanh trên một project nó không đọc");
        }

        scanned.ShouldContain(f => ProductSourceFiles.EndsWith(f, "CoreAndSkill.Api", "Program.cs"));

        scanned.ShouldContain(f => ProductSourceFiles.EndsWith(f, "Interceptors", "AuditInterceptor.cs"));
        scanned.ShouldContain(f => ProductSourceFiles.EndsWith(f, "Files", "FileMaintenanceHostedService.cs"));
    }

    // Tệp bị loại khỏi tầm quét chỉ được là hai thứ đã khai: tệp nằm dưới một thư mục TÊN ĐÚNG `Migrations`, và tệp khai
    // hằng. Kiểm bằng tên thư mục tổ tiên, không bằng phép so chuỗi của bộ lọc — một bộ lọc hỏng theo chiều "loại rộng"
    // (ví dụ `Contains("Migration")`) sẽ đỏ ở đây.
    [Fact]
    public void TheSystemActorGate_DropsOnlyMigrationsAndTheOwnerFile()
    {
        var all = CoreFiles();
        var scanned = ScannedFiles().ToHashSet(StringComparer.Ordinal);
        var dropped = all.Where(f => !scanned.Contains(f)).ToList();

        var migrations = dropped.Where(LivesUnderAMigrationsDirectory).ToList();
        var others = dropped.Where(f => !LivesUnderAMigrationsDirectory(f)).Select(ProductSourceFiles.RelativePath).ToList();

        migrations.ShouldNotBeEmpty("lời loại trừ Migrations/ không loại tệp nào — nó loại một thư mục ma");
        others.ShouldBe([OwnerFile]);
    }

    // Tệp được miễn trừ phải CÒN tồn tại, và phép dò chạy trên nó phải BẮT được chính dòng khai hằng — nếu không, lời miễn
    // trừ là miễn trừ một tệp ma, hoặc phép dò không đọc được hình dạng thật `public const string UserName = "…";`.
    // Đây cũng là ca ĐỎ trên đầu vào thật của cơ chế miễn trừ: bỏ lời miễn trừ thì cổng chính đỏ đúng ở tệp này.
    [Fact]
    public void TheSystemActorGate_ExemptsARealOwnerFile_WhoseDeclarationTheDetectorCatches()
    {
        var owner = CoreFiles().SingleOrDefault(f => ProductSourceFiles.RelativePath(f) == OwnerFile);

        owner.ShouldNotBeNull($"không thấy '{OwnerFile}' — tệp khai hằng đã dời, lời miễn trừ đang trỏ vào tệp ma");
        SystemActorLiteralScanner.Scan(File.ReadAllText(owner), owner)
            .ShouldContain(hit => hit.Literal == "\"system\"",
                "phép dò không bắt được literal khai hằng trong chính SystemActor.cs — mọi số 0 của cổng là giả");
    }

    // ===== Detector_* (T1) — ca ĐỎ =====

    [Theory]
    [InlineData("\"system\"")]
    [InlineData("currentUser.UserName ?? \"system\"")] // hình dạng thật của AuditInterceptor, EfJobRepository, …
    [InlineData("\"System\"")]
    [InlineData("\"SYSTEM\"")]
    [InlineData("@\"system\"")]
    [InlineData("\"\"\"system\"\"\"")]
    [InlineData("$\"system\"")]
    [InlineData("$\"\"\"system\"\"\"")]
    [InlineData("\"sys\\u0074em\"")]
    [InlineData("\"system\"u8.ToArray()")]
    [InlineData("$\"{\"system\"}\"")]
    [InlineData("currentUser.UserName is \"system\"")]
    [InlineData("currentUser.UserName switch { \"system\" => 1, _ => 0 }")]
    [InlineData("db.Files.ExecuteUpdateAsync(s => s.SetProperty(f => f.UpdatedBy, \"system\"))")] // FileMaintenanceHostedService
    public void Detector_SystemActorLiteral_Catches_AHandTypedName(string expression)
        => SystemActorLiteralScanner.Scan(Expression(expression), "fake.cs").ShouldHaveSingleItem();

    [Fact]
    public void Detector_SystemActorLiteral_Catches_AMultiLineRawString()
        => SystemActorLiteralScanner.Scan(Member(""""
            private const string Actor = """
                    system
                    """;
            """"), "fake.cs").ShouldHaveSingleItem();

    [Fact]
    public void Detector_SystemActorLiteral_Catches_AnAttributeArgument()
        => SystemActorLiteralScanner.Scan(Member("[System.ComponentModel.DefaultValue(\"system\")] public string? CreatedBy { get; set; }"),
            "fake.cs").ShouldHaveSingleItem();

    // ===== Detector_* (T1) — ca XANH, mỗi ca một cặp đỏ =====

    // Đúng hình dạng đích sau khi thay literal bằng hằng.
    // Cặp đỏ: Detector_SystemActorLiteral_Catches_AHandTypedName("currentUser.UserName ?? \"system\"") — cùng khuôn
    // Expression, khác đúng vế phải của `??`.
    [Fact]
    public void Detector_SystemActorLiteral_Ignores_TheConstant()
        => SystemActorLiteralScanner.Scan(Expression("currentUser.UserName ?? SystemActor.UserName"), "fake.cs").ShouldBeEmpty();

    // Định danh tên `system` không phải chuỗi.
    // Cặp đỏ: Detector_SystemActorLiteral_Catches_AHandTypedName("currentUser.UserName ?? \"system\"") — cùng khuôn, khác
    // đúng hai dấu nháy.
    [Fact]
    public void Detector_SystemActorLiteral_Ignores_AnIdentifierNamedSystem()
        => SystemActorLiteralScanner.Scan(Expression("currentUser.UserName ?? system"), "fake.cs").ShouldBeEmpty();

    // Chuỗi dài hơn chứa chữ system, và chuỗi cùng nghĩa bằng tiếng Việt, không phải literal tên tác nhân.
    // Cặp đỏ: Detector_SystemActorLiteral_Catches_AHandTypedName("\"system\"") — cùng khuôn, khác đúng giá trị literal.
    [Theory]
    [InlineData("\"system_operator\"")]
    [InlineData("\"is_system\"")]
    [InlineData("\"System.Text.Json\"")]
    [InlineData("\"systems\"")]
    [InlineData("\"syste\"")]
    [InlineData("\"Hệ thống\"")]
    [InlineData("\"db.Save(\\\"system\\\")\"")] // mã nằm TRONG chuỗi — hình dạng của chính các ca Detector_* ở tệp này
    public void Detector_SystemActorLiteral_Ignores_ALongerOrDifferentString(string expression)
        => SystemActorLiteralScanner.Scan(Expression(expression), "fake.cs").ShouldBeEmpty();

    // Chuỗi nội suy CÓ lỗ: đoạn văn bản "system" chỉ là đuôi của một giá trị dài hơn.
    // Cặp đỏ: Detector_SystemActorLiteral_Catches_AHandTypedName("$\"system\"") — cùng khuôn, cùng loại chuỗi, khác đúng
    // một lỗ nội suy.
    [Fact]
    public void Detector_SystemActorLiteral_Ignores_AnInterpolatedStringWithAHole()
        => SystemActorLiteralScanner.Scan(Expression("$\"{currentUser.UserName}system\""), "fake.cs").ShouldBeEmpty();

    // Chú thích là trivia — chỗ một phép dò quét văn bản báo oan (docs/wiki-core/be/04-testing-strategy.md §3.3).
    // Cặp đỏ: Detector_SystemActorLiteral_Catches_TheSameLineUncommented — cùng khuôn Member, khác đúng tiền tố `//`.
    [Fact]
    public void Detector_SystemActorLiteral_Ignores_ALineComment()
        => SystemActorLiteralScanner.Scan(Member("// private const string Actor = \"system\";"), "fake.cs").ShouldBeEmpty();

    [Fact]
    public void Detector_SystemActorLiteral_Catches_TheSameLineUncommented()
        => SystemActorLiteralScanner.Scan(Member("private const string Actor = \"system\";"), "fake.cs").ShouldHaveSingleItem();

    // Cặp đỏ: Detector_SystemActorLiteral_Catches_TheSameMemberWithAHandTypedValue — cùng khuôn Member, cùng chú thích,
    // khác đúng thân thành viên.
    [Fact]
    public void Detector_SystemActorLiteral_Ignores_BlockAndXmlDocComments()
        => SystemActorLiteralScanner.Scan(Member(DocumentedMember("SystemActor.UserName")), "fake.cs").ShouldBeEmpty();

    [Fact]
    public void Detector_SystemActorLiteral_Catches_TheSameMemberWithAHandTypedValue()
        => SystemActorLiteralScanner.Scan(Member(DocumentedMember("\"system\"")), "fake.cs").ShouldHaveSingleItem();

    // ===== Detector_* (T1) — phép miễn trừ theo đường dẫn =====

    // Cặp đỏ: Detector_SystemActorLiteral_DoesNotExempt_AFileWithTheSameNameElsewhere — cùng phép miễn trừ, khác đúng thư
    // mục chứa.
    [Fact]
    public void Detector_SystemActorLiteral_Exempts_TheOwnerFile()
        => IsExempt(OwnerFile).ShouldBeTrue();

    [Fact]
    public void Detector_SystemActorLiteral_DoesNotExempt_AFileWithTheSameNameElsewhere()
        => IsExempt("Core/CoreAndSkill.Core.Infrastructure/Identity/SystemActor.cs").ShouldBeFalse();

    // Cặp đỏ: Detector_SystemActorLiteral_DoesNotExempt_AFileMerelyNamedLikeMigrations — cùng thư mục cha, khác đúng một
    // đoạn đường dẫn.
    [Fact]
    public void Detector_SystemActorLiteral_Exempts_AMigrationFile()
        => IsExempt("Core/CoreAndSkill.Core.Infrastructure/Persistence/Migrations/20260918031217_SeedCorePermissionCatalog.cs")
            .ShouldBeTrue();

    [Theory]
    [InlineData("Core/CoreAndSkill.Core.Infrastructure/Persistence/MigrationsGuard.cs")]
    [InlineData("Core/CoreAndSkill.Core.Infrastructure/Persistence/PendingMigrations/Check.cs")]
    public void Detector_SystemActorLiteral_DoesNotExempt_AFileMerelyNamedLikeMigrations(string relativePath)
        => IsExempt(relativePath).ShouldBeFalse();

    // ---- khuôn nguồn ---------------------------------------------------------------------------------

    private static string Expression(string expression) => $$"""
        namespace CoreAndSkill.Core.Infrastructure.Fake;

        internal sealed class FakeWriter
        {
            public object Actor(FakeUser currentUser, FakeDb db) => {{expression}};
        }
        """;

    private static string Member(string member) => $$"""
        namespace CoreAndSkill.Core.Infrastructure.Fake;

        internal sealed class FakeWriter
        {
            {{member}}
        }
        """;

    private static string DocumentedMember(string value) => $$"""
        /* Không có người thực hiện thì ghi "system". */
            /// <summary>Tên ghi vào created_by — <c>"system"</c> khi không có người.</summary>
            public string Actor => {{value}};
        """;

    // ---- tầm quét --------------------------------------------------------------------------------------

    private static bool IsExempt(string relativePath)
        => relativePath == OwnerFile || relativePath.Contains("/Migrations/", StringComparison.Ordinal);

    // Tầm Core dùng chung — không lọc thêm ở đây. Cổng chỉ trừ đi phần miễn trừ của nó (IsExempt).
    private static IReadOnlyList<string> CoreFiles() => ProductSourceFiles.Core();

    private static IReadOnlyList<string> ScannedFiles()
        => [.. CoreFiles().Where(f => !IsExempt(ProductSourceFiles.RelativePath(f)))];

    private static bool LivesUnderAMigrationsDirectory(string path)
    {
        for (var directory = Path.GetDirectoryName(path); directory is not null; directory = Path.GetDirectoryName(directory))
        {
            if (Path.GetFileName(directory) == "Migrations")
                return true;
        }

        return false;
    }

    // Hai gốc CHỈ dùng cho chốt "mỗi project góp tệp" — không phải định nghĩa tầm quét thứ hai. Lấy độc lập với
    // ProductSourceFiles để chốt đó không tự so bộ liệt kê với chính nó.
    private static string CoreRoot() => Path.Combine(BackendRoot(), "Core");

    private static string HostRoot() => Path.Combine(BackendRoot(), "CoreAndSkill.Api");

    // Tệp này nằm ở src/BE/Tests/CoreAndSkill.ArchTests/ — từ thư mục của nó lên hai cấp là src/BE.
    private static string BackendRoot([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
}
