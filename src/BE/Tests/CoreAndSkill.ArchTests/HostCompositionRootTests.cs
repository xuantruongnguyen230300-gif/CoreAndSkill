using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// A7 — docs/RULES.md §3, docs/quy-uoc/be-architecture.md §3: host (CoreAndSkill.Api) chỉ là composition root; Program.cs
// dưới 50 dòng.
//
// VÌ SAO LÀ ARCHTEST, KHÔNG PHẢI REVIEW. Host đã ra khỏi khối core-paths theo quyết định 1 của
// docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md, nên sửa host ở repo Core KHÔNG còn đòi một lượt core-reviewer.
// Bộ test này là thứ thay chỗ lượt review đó (quyết định 6). ArchTests vẫn nằm trong khối, nên nới cổng này vẫn đòi review.
//
// Ba phép dò, lý do từng phép và phần ngoài tầm: Support/HostCompositionRootScanner.cs.
public class HostCompositionRootTests
{
    private static readonly string HostRoot = ProductSourceFiles.HostDirectory();

    private const string FixHint =
        "Luật A7 (docs/quy-uoc/be-architecture.md §3): host chỉ chứa Program.cs với lời gọi đăng ký, appsettings*.json, "
      + ".csproj, Properties/launchSettings.json, Dockerfile. Thứ thừa chuyển vào Core.Web hoặc vào module — không nới tập "
      + "cho phép.";

    // ===== Tầng tệp =====

    [Fact]
    public void Host_MustContainOnly_AllowedFiles()
    {
        HostCompositionRootScanner.DisallowedFiles(HostRoot).ShouldBeEmpty(
            FixHint + " Tệp *.user do IDE sinh ở gốc project được phép; tệp cục bộ khác vẫn bị tính — xoá nó khỏi đĩa.");
    }

    // T6 — phép dò tệp đọc ĐÚNG thư mục host thật và đi được vào thư mục con: thiếu một trong ba tệp này nghĩa là nó đang
    // liệt kê một thư mục khác, hoặc không đệ quy, và "không có tệp lạ" là câu nói về một tập rỗng.
    [Fact]
    public void Host_MustContainOnly_AllowedFiles_ReadsRealHostTree()
    {
        var files = HostCompositionRootScanner.Files(HostRoot);

        files.ShouldContain("Program.cs");
        files.ShouldContain("CoreAndSkill.Api.csproj");
        files.ShouldContain("Properties/launchSettings.json");
    }

    // ===== Tầng kiểu =====

    [Fact]
    public void Host_MustNotDeclare_AnyTypeButProgram()
    {
        HostCompositionRootScanner.TypesOtherThanProgram(ArchitectureFixture.HostAssembly).ShouldBeEmpty(FixHint);
    }

    // T6 — phép dò kiểu nhìn ĐÚNG assembly host, và bộ lọc "trình biên dịch tự sinh" không nuốt luôn kiểu người viết:
    // nếu nó nuốt cả `Program` thì nó cũng nuốt mọi lớp khác, và test trên xanh vì không còn gì để xét.
    [Fact]
    public void Host_MustNotDeclare_AnyTypeButProgram_ReadsRealHostAssembly()
    {
        ArchitectureFixture.HostAssembly.GetName().Name.ShouldBe("CoreAndSkill.Api");

        HostCompositionRootScanner.DeclaredTypes(ArchitectureFixture.HostAssembly)
            .Select(type => type.FullName)
            .ShouldContain("Program");
    }

    [Fact]
    public void Host_Program_MustNotCarry_BaseTypeInterfaceOrMember()
    {
        HostCompositionRootScanner.ProgramShapeViolations(ArchitectureFixture.HostAssembly).ShouldBeEmpty(FixHint);
    }

    // T6 — phép liệt kê thành viên thấy được thành viên private static: điểm vào `<Main>$` do trình biên dịch sinh chính
    // là loại đó. Thiếu cờ NonPublic/Static thì một hàm trợ giúp private static khai trong `partial class Program` lọt
    // qua, và test trên vẫn xanh.
    [Fact]
    public void Host_Program_MustNotCarry_BaseTypeInterfaceOrMember_SeesCompilerEntryPoint()
    {
        HostCompositionRootScanner.ProgramMembers(ArchitectureFixture.HostAssembly)
            .Select(member => member.Name)
            .ShouldContain("<Main>$");
    }

    // ===== Program.cs =====

    [Fact]
    public void Host_ProgramCs_IsUnder50Lines()
    {
        var text = File.ReadAllText(ProgramCsPath());

        HostCompositionRootScanner.IsProgramWithinLineLimit(text).ShouldBeTrue(
            $"Program.cs có {HostCompositionRootScanner.CountLines(text)} dòng, ngưỡng là dưới "
          + $"{HostCompositionRootScanner.ProgramLineLimit}. Chuyển thứ thừa vào Core.Web — không nới ngưỡng "
          + "(docs/quy-uoc/be-architecture.md §3).");
    }

    // T6 — đếm trên tệp thật có nội dung: đọc nhầm một tệp rỗng thì 0 < 50 và test trên xanh vô nghĩa.
    [Fact]
    public void Host_ProgramCs_IsUnder50Lines_ReadsRealProgramFile()
    {
        var text = File.ReadAllText(ProgramCsPath());

        text.ShouldContain("WebApplication.CreateBuilder");
        HostCompositionRootScanner.CountLines(text).ShouldBeGreaterThan(0);
    }

    // ===== Detector_* (T1) — mỗi ca đỏ là ca xanh cùng hình dạng CỘNG đúng một vi phạm (T12) =====

    // --- Tệp ---

    [Fact]
    public void Detector_A7_Files_Ignores_AllowedSetAndRootBuildOutput()
    {
        using var sandbox = AllowedHostTree();

        HostCompositionRootScanner.DisallowedFiles(sandbox.Root).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Middleware/RequestTimingMiddleware.cs")]
    [InlineData("Controllers/PingController.cs")]
    [InlineData("HostExtensions.cs")]
    [InlineData("Sub/Program.cs")]
    [InlineData("Tools/Seeder/Seeder.csproj")]
    [InlineData("launchSettings.json")]
    [InlineData("Properties/appsettings.json")]
    [InlineData("wwwroot/index.html")]
    [InlineData("Views/Home/Index.cshtml")]
    [InlineData("Sub/bin/Leftover.cs")]
    public void Detector_A7_Files_Catches_StrayFile(string strayFile)
    {
        using var sandbox = AllowedHostTree();
        sandbox.WriteFile(strayFile, "// vi phạm cố ý");

        HostCompositionRootScanner.DisallowedFiles(sandbox.Root).ShouldBe(new[] { strayFile });
    }

    // Tệp *.user do IDE sinh ở gốc project được phép (người dùng chốt 2026-09-26). Cặp đỏ: ca ngay dưới — cùng cây, cùng tệp
    // .user, cộng đúng một tệp lạ. Tệp .user không được thành tấm che cho tệp khác nằm cạnh nó.
    [Fact]
    public void Detector_A7_Files_Ignores_IdeUserFileAtRoot()
    {
        using var sandbox = AllowedHostTree();
        sandbox.WriteFile("CoreAndSkill.Api.csproj.user", "<Project />");

        HostCompositionRootScanner.DisallowedFiles(sandbox.Root).ShouldBeEmpty();
    }

    [Fact]
    public void Detector_A7_Files_Catches_StrayFile_BesideIdeUserFile()
    {
        using var sandbox = AllowedHostTree();
        sandbox.WriteFile("CoreAndSkill.Api.csproj.user", "<Project />");
        sandbox.WriteFile("Middleware/RequestTimingMiddleware.cs", "// vi phạm cố ý");

        HostCompositionRootScanner.DisallowedFiles(sandbox.Root).ShouldBe(new[] { "Middleware/RequestTimingMiddleware.cs" });
    }

    // Sai hoa thường không dựng được bằng cây tệp trên Windows (hệ tệp không phân biệt, `program.cs` ghi đè `Program.cs`),
    // nên cặp xanh/đỏ này đi thẳng vào phép so tên — cùng hàm mà DisallowedFiles gọi.
    [Theory]
    [InlineData("Program.cs", true)]
    [InlineData("program.cs", false)]
    [InlineData("appsettings.Staging.json", true)]
    [InlineData("AppSettings.json", false)]
    [InlineData("Dockerfile", true)]
    [InlineData("dockerfile", false)]
    [InlineData(".csproj", false)]
    [InlineData("CoreAndSkill.Api.csproj.user", true)]
    [InlineData(".user", false)]
    [InlineData("Properties/CoreAndSkill.Api.csproj.user", false)]
    [InlineData("HostExtensions.user.cs", false)]
    public void Detector_A7_Files_MatchesAllowedNames_CaseSensitively(string relativePath, bool allowed)
    {
        HostCompositionRootScanner.IsAllowedFile(relativePath).ShouldBe(allowed);
    }

    // --- Kiểu ---

    [Fact]
    public void Detector_A7_Types_Ignores_TopLevelStatementsLambdasAndLocalFunctions()
    {
        var host = CompileHost(string.Empty);

        HostCompositionRootScanner.TypesOtherThanProgram(host).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("public sealed class RequestTimingMiddleware { public Task InvokeAsync(object context) => Task.CompletedTask; }")]
    [InlineData("file sealed class RequestTimingMiddleware { public Task InvokeAsync(object context) => Task.CompletedTask; }")]
    [InlineData("public partial class Program { public sealed class PingController { public string Get() => \"pong\"; } }")]
    [InlineData("public static class HostExtensions { public static int Twice(this int value) => value * 2; }")]
    [InlineData("public sealed record Invoice(Guid Id, decimal Total);")]
    [InlineData("namespace CoreAndSkill.Api { internal sealed class CreateInvoiceHandler { } }")]
    public void Detector_A7_Types_Catches_TypeBesideProgram(string violation)
    {
        var host = CompileHost(violation);

        HostCompositionRootScanner.TypesOtherThanProgram(host).ShouldNotBeEmpty();
    }

    // --- Hình dạng Program ---

    [Fact]
    public void Detector_A7_ProgramShape_Ignores_TopLevelStatementsLambdasAndLocalFunctions()
    {
        var host = CompileHost(string.Empty);

        HostCompositionRootScanner.ProgramShapeViolations(host).ShouldBeEmpty();
    }

    [Theory]
    // Controller: `Program` kế thừa ControllerBase.
    [InlineData("public partial class Program : Microsoft.AspNetCore.Mvc.ControllerBase;")]
    // Middleware qua interface.
    [InlineData("""
        public partial class Program : Microsoft.AspNetCore.Http.IMiddleware
        {
            public Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.RequestDelegate next) => next(context);
        }
        """)]
    // Chỉ interface, không thành viên nào: IFilterMetadata là interface đánh dấu của filter MVC.
    [InlineData("public partial class Program : Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata;")]
    // Chỉ constructor có tham số — nửa đầu của middleware theo quy ước.
    [InlineData("public partial class Program { public Program(Microsoft.AspNetCore.Http.RequestDelegate next) { } }")]
    // Middleware theo quy ước — không interface, không gốc kế thừa; chỉ constructor + InvokeAsync.
    [InlineData("""
        public partial class Program
        {
            private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
            public Program(Microsoft.AspNetCore.Http.RequestDelegate next) => _next = next;
            public Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext context) => _next(context);
        }
        """)]
    // Hàm trợ giúp private static — logic đặt trong partial class thay cho câu lệnh cấp cao.
    [InlineData("public partial class Program { private static int Twice(int value) => value * 2; }")]
    // Trạng thái tĩnh.
    [InlineData("public partial class Program { internal static int RequestCount; }")]
    public void Detector_A7_ProgramShape_Catches_ProgramTurnedIntoLogic(string violation)
    {
        var host = CompileHost(violation);

        HostCompositionRootScanner.ProgramShapeViolations(host).ShouldNotBeEmpty();
    }

    // --- Program.cs ---

    [Theory]
    [InlineData("\n", false)]
    [InlineData("\n", true)]
    [InlineData("\r\n", false)]
    [InlineData("\r\n", true)]
    public void Detector_A7_ProgramLines_Accepts_49Lines(string newline, bool trailingNewline)
    {
        HostCompositionRootScanner.IsProgramWithinLineLimit(Lines(49, newline, trailingNewline)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("\n", false)]
    [InlineData("\n", true)]
    [InlineData("\r\n", false)]
    [InlineData("\r\n", true)]
    public void Detector_A7_ProgramLines_Rejects_50Lines(string newline, bool trailingNewline)
    {
        HostCompositionRootScanner.IsProgramWithinLineLimit(Lines(50, newline, trailingNewline)).ShouldBeFalse();
    }

    // Dòng trống và dòng chú thích là dòng: "dưới 50 dòng" đọc theo nghĩa đen của be-architecture.md §3.
    [Fact]
    public void Detector_A7_ProgramLines_CountsBlankAndCommentLines()
    {
        HostCompositionRootScanner.CountLines("var a = 1;\n\n// chú thích\n\nvar b = 2;\n").ShouldBe(5);
    }

    // ===== Hạ tầng =====

    private static string ProgramCsPath() => Path.Combine(HostRoot, "Program.cs");

    // Cây host hợp lệ đủ mọi loại tệp của tập cho phép, kèm đầu ra build ở gốc — đầu vào CHUNG của ca xanh và mọi ca đỏ.
    private static TempSandbox AllowedHostTree()
    {
        var sandbox = new TempSandbox();
        sandbox.WriteFile("Program.cs", "var builder = WebApplication.CreateBuilder(args);");
        sandbox.WriteFile("CoreAndSkill.Api.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        sandbox.WriteFile("appsettings.json", "{}");
        sandbox.WriteFile("appsettings.Development.json", "{}");
        sandbox.WriteFile("Properties/launchSettings.json", "{}");
        sandbox.WriteFile("Dockerfile", "FROM scratch");
        sandbox.WriteFile("bin/Debug/net10.0/CoreAndSkill.Api.dll", string.Empty);
        sandbox.WriteFile("obj/Debug/net10.0/Leftover.cs", "public sealed class Leftover { }");
        return sandbox;
    }

    // Chương trình đúng hình dạng host thật: câu lệnh cấp cao có await (máy trạng thái async lồng trong Program), lambda
    // (lớp đóng `<>c`), hàm cục bộ (thành viên `<<Main>$>g__…` của Program), cả hai mang chú thích nullable, và
    // `public partial class Program;` cho WebApplicationFactory. `violation` được nối vào cuối — ca xanh truyền chuỗi rỗng.
    private static System.Reflection.Assembly CompileHost(string violation)
        => SyntheticAssembly.CompileProgram(
            $"A7FakeHost{Guid.NewGuid():N}",
            $$"""
            using System;
            using System.Threading.Tasks;

            Func<string, string?>[] registrations = [name => name.Length > 0 ? name : null];
            static string? Describe(string? name) => name is null ? null : name + "!";
            await Task.Yield();
            _ = Describe(registrations[0]("module"));

            public partial class Program;

            {{violation}}
            """,
            typeof(Microsoft.AspNetCore.Mvc.ControllerBase).Assembly,
            typeof(Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata).Assembly,
            typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly,
            typeof(Microsoft.AspNetCore.Http.IMiddleware).Assembly);

    private static string Lines(int count, string newline, bool trailingNewline)
    {
        var text = string.Join(newline, Enumerable.Range(1, count).Select(i => $"// dòng {i}"));
        return trailingNewline ? text + newline : text;
    }
}
