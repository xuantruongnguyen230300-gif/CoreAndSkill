using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Luật S20 (docs/RULES.md §6, docs/adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md): một action dùng method
// AN TOÀN (GET/HEAD) mà có TÁC DỤNG PHỤ — xuất dữ liệu, hoặc ghi nhật ký kiểm toán — phải khai dấu [RequireAntiforgery].
// Thiếu dấu thì AntiforgeryValidationMiddleware nhường nó qua ở bước 1, và một trang lạ dẫn người dùng đang đăng nhập tới
// URL đó là tạo được một dòng nhật ký giả mang tên họ (cookie SameSite=Lax vẫn đi theo điều hướng xuyên site, và trình
// duyệt không gắn Origin cho điều hướng GET nên lớp 1 không thấy gì).
//
// Phép dò đi hai đường, vì "tác dụng phụ" không nhìn thấy được từ một đường duy nhất:
//   1. CÚ PHÁP (tệp .cs của controller): action có gọi HandleExport hay dựng ExportFileResult không, và nó gửi những kiểu
//      request nào (kiểu của tham số + kiểu được `new` trong thân). Thân phương thức không đọc được bằng reflection.
//   2. PHẢN CHIẾU (assembly): handler của một request có với tới đường ghi nhật ký kiểm toán không — đi theo đồ thị tham số
//      constructor, giải interface về hiện thực trong chính các assembly Core.
//
// NGOÀI TẦM (biết trước, không giả vờ bắt được):
//   - Request gửi qua một biến dựng ở nơi khác, hay qua phản chiếu: phép dò chỉ thấy `new X(...)` và kiểu tham số.
//   - Tác dụng phụ KHÔNG phải nhật ký kiểm toán và không phải xuất (ví dụ một GET ghi bảng nghiệp vụ). ADR-0062 chốt danh
//     sách ngoại lệ chỉ mở cho hai loại đó; loại khác cần ADR mới, và cổng này mở rộng cùng lúc.
//   - Endpoint không phải MVC controller (minimal API) — solution hiện không có.
internal static class AntiforgeryMarkScanner
{
    private const string MarkAttribute = "RequireAntiforgery";

    // Hai dấu hiệu của một action XUẤT, cố ý giữ cả hai (ADR-0064 quyết định 4):
    //   - HandleExport: lối CHÍNH THỨC, và là lối duy nhất module dùng được (ExportFileResult là internal của Core.Web).
    //   - ExportFileResult: một action TRONG Core.Web vẫn `new` thẳng được, đi vòng qua helper — vẫn phải bị thấy.
    private const string ExportHelperMethod = "HandleExport";
    private const string ExportResultType = "ExportFileResult";

    private static readonly string[] SafeMethodAttributes = ["HttpGet", "HttpHead"];

    // offender = "<Controller>.<Action>: <lý do>". auditWritingRequests: tên KIỂU request mà handler của nó ghi nhật ký
    // kiểm toán (Find bên dưới dựng; test đối chứng truyền tập tự khai).
    public static IReadOnlyList<string> Scan(string source, IReadOnlySet<string> auditWritingRequests)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var offenders = new List<string>();

        foreach (var type in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            foreach (var action in type.Members.OfType<MethodDeclarationSyntax>())
            {
                if (!UsesSafeHttpMethod(action) || HasAttribute(action, MarkAttribute))
                    continue;

                if (ExportsAFile(action) is { } how)
                    offenders.Add($"{type.Identifier.Text}.{action.Identifier.Text}: xuất dữ liệu bằng {how}");
                else if (SentRequestTypes(action).Any(auditWritingRequests.Contains))
                    offenders.Add($"{type.Identifier.Text}.{action.Identifier.Text}: gửi request ghi nhật ký kiểm toán");
            }
        }

        return offenders;
    }

    // Mọi action method an toàn mà phép dò thấy — để test T6 chứng minh nó không quét rỗng.
    public static IReadOnlyList<string> SafeMethodActions(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        return [.. from type in root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                   from action in type.Members.OfType<MethodDeclarationSyntax>()
                   where UsesSafeHttpMethod(action)
                   select $"{type.Identifier.Text}.{action.Identifier.Text}"];
    }

    // Tên kiểu request mà handler của nó với tới đường ghi nhật ký kiểm toán. Hạt giống: tham số constructor kiểu
    // IAuditTrail (seam ghi tường minh) cộng các kiểu do nơi gọi khai thêm — directWriters: kiểu ghi thẳng entity AuditLog
    // (AuditLog.Record(...) trong source), thứ không lộ ra ở chữ ký nào.
    public static IReadOnlySet<string> AuditWritingRequests(IReadOnlySet<string> directWriters, params Assembly[] assemblies)
    {
        var types = assemblies.SelectMany(a => a.GetTypes()).ToList();

        // interface -> các hiện thực trong chính các assembly này.
        var implementations = types
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces().Select(i => (Interface: i, Implementation: t)))
            .GroupBy(x => x.Interface)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Implementation).ToList());

        var requests = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handler in types.Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            var requestType = handler.GetInterfaces()
                .Where(i => i.IsGenericType && i.Name.StartsWith("IRequestHandler", StringComparison.Ordinal))
                .Select(i => i.GetGenericArguments()[0])
                .FirstOrDefault();

            if (requestType is not null && WritesAudit(handler, implementations, directWriters, []))
                requests.Add(requestType.Name);
        }

        return requests;
    }

    // Kiểu tự khai ghi nhật ký kiểm toán (source có `AuditLog.Record(`) — hạt giống cho đồ thị phụ thuộc bên trên.
    public static IReadOnlySet<string> DirectAuditWriters(IEnumerable<string> sourceFiles)
    {
        var writers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in sourceFiles)
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();

            foreach (var type in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var writesAuditRow = type.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Any(i => i.Expression.ToString().EndsWith("AuditLog.Record", StringComparison.Ordinal)
                              || i.Expression.ToString().EndsWith("AuditLogs.Add", StringComparison.Ordinal));

                if (writesAuditRow)
                    writers.Add(type.Identifier.Text);
            }
        }

        return writers;
    }

    private static bool WritesAudit(
        Type type,
        Dictionary<Type, List<Type>> implementations,
        IReadOnlySet<string> directWriters,
        HashSet<Type> visited)
    {
        if (!visited.Add(type))
            return false;

        if (directWriters.Contains(type.Name))
            return true;

        foreach (var parameter in type.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType))
        {
            if (parameter.Name == "IAuditTrail" || directWriters.Contains(parameter.Name))
                return true;

            if (implementations.TryGetValue(parameter, out var impls) && impls.Any(i => WritesAudit(i, implementations, directWriters, visited)))
                return true;

            if (!parameter.IsInterface && parameter.Assembly == type.Assembly && WritesAudit(parameter, implementations, directWriters, visited))
                return true;
        }

        return false;
    }

    private static bool UsesSafeHttpMethod(MethodDeclarationSyntax action)
        => SafeMethodAttributes.Any(name => HasAttribute(action, name));

    private static bool HasAttribute(MethodDeclarationSyntax action, string name)
        => action.AttributeLists
            .SelectMany(list => list.Attributes)
            .Select(a => a.Name.ToString().Split('.').Last())
            .Any(a => a == name || a == name + "Attribute");

    // null = action không xuất tệp; khác null = tên dấu hiệu đã thấy, để dòng offender nói rõ nó bị bắt bằng đường nào.
    private static string? ExportsAFile(MethodDeclarationSyntax action)
    {
        var callsHelper = action.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Any(i => i.Expression.ToString().Split('.').Last() == ExportHelperMethod);

        if (callsHelper)
            return ExportHelperMethod;

        var buildsResult = action.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Any(o => o.Type.ToString().Split('.').Last() == ExportResultType);

        return buildsResult ? ExportResultType : null;
    }

    // Kiểu request đi vào mediator: kiểu của tham số action (binding [FromQuery]/[FromBody]) và kiểu được `new` trong thân.
    private static IEnumerable<string> SentRequestTypes(MethodDeclarationSyntax action)
    {
        foreach (var parameter in action.ParameterList.Parameters)
        {
            if (parameter.Type is not null)
                yield return parameter.Type.ToString().Split('.').Last();
        }

        foreach (var creation in action.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            yield return creation.Type.ToString().Split('.').Last();
    }
}
