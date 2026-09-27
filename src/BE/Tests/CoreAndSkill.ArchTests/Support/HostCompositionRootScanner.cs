using System.Reflection;
using System.Text.RegularExpressions;

namespace CoreAndSkill.ArchTests.Support;

// Luật A7 (docs/RULES.md §3): host chỉ là composition root — docs/quy-uoc/be-architecture.md §3,
// docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md quyết định 6.
//
// Ba phép dò, mỗi phép bịt một lối mà hai phép kia không thấy:
//   - TỆP: thư mục host chỉ chứa tập tệp cho phép. Bịt tệp không phải mã (wwwroot, .cshtml, cấu hình lạ) — không phép dò
//     kiểu nào nhìn thấy chúng.
//   - KIỂU: assembly host không khai kiểu nào ngoài `Program`. Bịt một lớp khai ngay trong Program.cs — kể cả kiểu `file`
//     — và một tệp .cs ngoài thư mục host kéo vào bằng <Compile Include>, thứ phép dò tệp không bao giờ thấy.
//   - HÌNH DẠNG `Program`: không gốc kế thừa, không interface, không thành viên nào ngoài phần trình biên dịch sinh cho câu
//     lệnh cấp cao. Bịt ca biến CHÍNH `Program` thành controller (kế thừa ControllerBase), middleware (IMiddleware, hoặc
//     middleware theo quy ước: constructor nhận RequestDelegate + InvokeAsync — không interface nào để bắt) hay filter.
//
// NGOÀI TẦM: logic nằm trong project khác mà host tham chiếu; logic viết thẳng trong câu lệnh cấp cao (bị chặn bằng ngưỡng
// dòng, không bằng phép dò này); constructor không tham số tự viết của `Program` — metadata không phân biệt nó với
// constructor ngầm định.
internal static partial class HostCompositionRootScanner
{
    // "dưới 50 dòng" — docs/quy-uoc/be-architecture.md §3. Số dòng phải NHỎ HƠN số này.
    public const int ProgramLineLimit = 50;

    private const string ProgramTypeName = "Program";

    private const BindingFlags DeclaredMembers =
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    // ===== Tệp =====

    // Mọi tệp dưới thư mục host, tương đối và dùng dấu `/`. Loại đúng hai thư mục đầu ra build ở GỐC project (bin/, obj/) —
    // một bin/ lồng sâu hơn không phải thư mục đầu ra, và vẫn bị liệt kê.
    public static IReadOnlyList<string> Files(string hostRoot)
        => Directory.EnumerateFiles(hostRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(hostRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(relative => !IsBuildOutput(relative))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<string> DisallowedFiles(string hostRoot)
        => Files(hostRoot).Where(relative => !IsAllowedFile(relative)).ToList();

    // Tập cho phép — docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md quyết định 6: `Program.cs`,
    // `appsettings*.json`, tệp `.csproj`, `Properties/launchSettings.json`, `Dockerfile`. Cộng thêm tệp `*.user` do IDE
    // sinh (Visual Studio ghi `<project>.csproj.user` khi đổi launch profile) — người dùng chốt 2026-09-26: tệp cục bộ, bị
    // gitignore, không chứa logic. Danh sách viết ở đây, KHÔNG đọc .gitignore và không gọi git.
    //
    // Mọi tệp ở gốc project trừ launchSettings. So khớp phân biệt hoa thường: trên Linux `program.cs` là một tệp khác.
    public static bool IsAllowedFile(string relative)
    {
        if (relative is "Program.cs" or "Dockerfile" or "Properties/launchSettings.json")
            return true;

        if (relative.Contains('/'))
            return false;

        return HasSuffixAfterName(relative, ".csproj")
            || HasSuffixAfterName(relative, ".user")
            || (relative.StartsWith("appsettings", StringComparison.Ordinal) && relative.EndsWith(".json", StringComparison.Ordinal));
    }

    // Tên kết thúc bằng `suffix` VÀ có phần tên đứng trước — tệp tên đúng bằng `.csproj` hay `.user` không phải tệp của IDE.
    private static bool HasSuffixAfterName(string fileName, string suffix)
        => fileName.EndsWith(suffix, StringComparison.Ordinal) && fileName.Length > suffix.Length;

    private static bool IsBuildOutput(string relative)
    {
        var slash = relative.IndexOf('/');
        if (slash < 0)
            return false;

        var top = relative[..slash];
        return top.Equals("bin", StringComparison.OrdinalIgnoreCase) || top.Equals("obj", StringComparison.OrdinalIgnoreCase);
    }

    // ===== Kiểu =====

    // Kiểu mà NGƯỜI VIẾT khai — loại phần trình biên dịch tự sinh (máy trạng thái async, lớp đóng của lambda). Kiểu `file`
    // KHÔNG bị loại dù tên metadata của nó cũng mang dấu `<`.
    public static IReadOnlyList<Type> DeclaredTypes(Assembly assembly)
        => assembly.GetTypes()
            .Where(type => !IsCompilerSynthesized(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<string> TypesOtherThanProgram(Assembly assembly)
        => DeclaredTypes(assembly)
            .Where(type => type.FullName != ProgramTypeName)
            .Select(type => type.FullName!)
            .ToList();

    // ===== Hình dạng `Program` =====

    // Thành viên khai trực tiếp trên `Program`, trừ kiểu lồng (phép dò kiểu lo phần đó).
    public static IReadOnlyList<MemberInfo> ProgramMembers(Assembly assembly)
        => ProgramType(assembly)?.GetMembers(DeclaredMembers)
               .Where(member => member.MemberType != MemberTypes.NestedType)
               .ToList()
           ?? [];

    public static IReadOnlyList<string> ProgramShapeViolations(Assembly assembly)
    {
        var program = ProgramType(assembly);
        if (program is null)
            return [$"assembly '{assembly.GetName().Name}' không có kiểu `{ProgramTypeName}` ở namespace gốc"];

        var violations = new List<string>();

        if (program.BaseType != typeof(object))
            violations.Add($"`Program` kế thừa {program.BaseType?.FullName ?? "(không có gốc)"}");

        violations.AddRange(program.GetInterfaces().Select(i => $"`Program` hiện thực {i.FullName}"));

        violations.AddRange(ProgramMembers(assembly)
            .Where(member => !IsCompilerEntryPointOrDefaultConstructor(member))
            .Select(member => $"`Program` khai {member.MemberType} '{member.Name}'"));

        return violations;
    }

    // ===== Program.cs =====

    // Đếm dòng đúng nghĩa File.ReadAllLines: dấu xuống dòng cuối tệp đóng dòng cuối, không mở dòng mới; \r\n, \n, \r đều
    // là một ngắt dòng. Dòng trống và dòng chú thích đều tính.
    public static int CountLines(string text)
    {
        using var reader = new StringReader(text);
        var count = 0;
        while (reader.ReadLine() is not null)
            count++;

        return count;
    }

    public static bool IsProgramWithinLineLimit(string text) => CountLines(text) < ProgramLineLimit;

    // ===== Nhận dạng =====

    private static Type? ProgramType(Assembly assembly) => assembly.GetType(ProgramTypeName, throwOnError: false);

    private static bool IsCompilerEntryPointOrDefaultConstructor(MemberInfo member)
        => member.Name.Contains('<')
           || member is ConstructorInfo { IsStatic: false } constructor && constructor.GetParameters().Length == 0;

    private static bool IsCompilerSynthesized(Type type)
    {
        // C# không cho định danh chứa `<`, nên tên mang `<` chỉ có thể do trình biên dịch đặt — TRỪ kiểu `file`, mà
        // Roslyn đặt tên `<TênTệp>F<băm>__TênKiểu`. Kiểu `file` là mã người viết: một middleware khai `file` trong
        // Program.cs vẫn là middleware trong host.
        for (var current = type; current is not null; current = current.DeclaringType)
        {
            if (FileLocalTypeName().IsMatch(current.Name))
                return false;

            if (current.Name.Contains('<'))
                return true;
        }

        // Thuộc tính trình biên dịch NHÚNG vào assembly (NullableAttribute, EmbeddedAttribute...) có tên thường, nên rơi vào
        // nhánh này và bị tính là kiểu người viết. Cố ý: đã đo 2026-09-25 rằng câu lệnh cấp cao, lambda và hàm cục bộ có
        // chú thích nullable KHÔNG làm trình biên dịch nhúng thuộc tính nào; chúng chỉ xuất hiện cùng một kiểu hay thành
        // viên đã là vi phạm. Một phiên bản trình biên dịch sau này nhúng chúng cho host sạch thì cổng đỏ kèm tên kiểu —
        // lúc đó mới thêm luật nhận dạng, KÈM ca canary dựng được nó.
        return false;
    }

    [GeneratedRegex(@"^<[^>]*>F[0-9A-F]+__", RegexOptions.CultureInvariant)]
    private static partial Regex FileLocalTypeName();
}
