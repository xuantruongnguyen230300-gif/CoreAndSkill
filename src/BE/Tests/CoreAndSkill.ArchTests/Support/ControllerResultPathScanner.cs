using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Luật B16 — docs/quy-uoc/be-api-controller.md §3, ADR-0064, ADR-0068: controller KHÔNG tự dựng IActionResult nào.
// Mọi biểu thức trả về của một action phải là lời gọi một trong BỐN tên `Handle*` của ApiControllerBase, nơi status
// code, envelope và — với nhánh tệp — ba header bắt buộc được đặt ở đúng một chỗ.
//
// Vì sao quét CÚ PHÁP chứ không phản chiếu: thân phương thức không đọc được bằng reflection, và chính thân là nơi
// `return File(...)` hay `return StatusCode(500, ...)` nằm. Kiểu trả về `IActionResult` thì mọi action đều khai —
// nó không phân biệt được gì.
//
// NGOÀI TẦM (biết trước, không giả vờ bắt được):
//   - Action gọi một helper riêng của chính controller rồi trả kết quả helper đó: phép dò thấy tên helper, không
//     thấy helper dựng gì. Nó bị tính là VI PHẠM — đúng chiều an toàn, và hôm nay không action nào làm vậy.
//   - Endpoint không phải MVC controller (minimal API) — solution hiện không có.
//   - Middleware và IExceptionHandler tự dựng phản hồi: chúng KHÔNG phải controller, và §2.4 của file luật khai
//     chúng là đường dựng envelope hợp lệ.
internal static class ControllerResultPathScanner
{
    // Bốn tên duy nhất — docs/quy-uoc/be-api-controller.md §3. Đổi tên một lối ở ApiControllerBase mà quên danh
    // sách này thì cổng đỏ ngay ở chính action đang dùng nó, không xanh trong im lặng.
    private static readonly string[] HandleMethods = ["HandleResult", "HandleCreated", "HandleExport", "HandleFile"];

    private static readonly string[] HttpMethodAttributes =
        ["HttpGet", "HttpPost", "HttpPut", "HttpPatch", "HttpDelete", "HttpHead", "HttpOptions"];

    // offender = "<Controller>.<Action>: <biểu thức bị bắt>". namedExceptions: "<Controller>.<Action>" được miễn,
    // khai ở ĐÚNG MỘT chỗ phía nơi gọi.
    public static IReadOnlyList<string> Scan(string source, IReadOnlySet<string> namedExceptions)
    {
        var offenders = new List<string>();

        foreach (var (type, action) in Actions(CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()))
        {
            var name = $"{type.Identifier.Text}.{action.Identifier.Text}";
            if (namedExceptions.Contains(name))
                continue;

            foreach (var returned in ReturnedExpressions(action).SelectMany(Leaves))
            {
                if (IsAllowed(returned))
                    continue;

                offenders.Add($"{name}: trả `{Compact(returned)}` — không phải một trong {string.Join(" / ", HandleMethods)}");
            }
        }

        return offenders;
    }

    // Mọi action HTTP phép dò thấy — để T6 chứng minh nó không quét rỗng.
    public static IReadOnlyList<string> ActionNames(string source)
        => [.. Actions(CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot())
               .Select(pair => $"{pair.Type.Identifier.Text}.{pair.Action.Identifier.Text}")];

    private static IEnumerable<(ClassDeclarationSyntax Type, MethodDeclarationSyntax Action)> Actions(CompilationUnitSyntax root)
        => from type in root.DescendantNodes().OfType<ClassDeclarationSyntax>()
           from action in type.Members.OfType<MethodDeclarationSyntax>()
           where HttpMethodAttributes.Any(attribute => HasAttribute(action, attribute))
           select (type, action);

    private static bool HasAttribute(MethodDeclarationSyntax action, string name)
        => action.AttributeLists
            .SelectMany(list => list.Attributes)
            .Select(a => a.Name.ToString().Split('.').Last())
            .Any(a => a == name || a == name + "Attribute");

    // Thân biểu thức (`=> ...`) và mọi `return ...;` của CHÍNH action — không tính `return` nằm trong một lambda
    // hay hàm cục bộ bên trong nó: những cái đó trả về kiểu khác, không phải phản hồi HTTP.
    private static IEnumerable<ExpressionSyntax> ReturnedExpressions(MethodDeclarationSyntax action)
    {
        if (action.ExpressionBody is { } arrow)
            yield return arrow.Expression;

        if (action.Body is null)
            yield break;

        foreach (var statement in action.Body.DescendantNodes().OfType<ReturnStatementSyntax>())
        {
            if (statement.Expression is { } expression && !InsideNestedScope(statement, action))
                yield return expression;
        }
    }

    private static bool InsideNestedScope(SyntaxNode node, MethodDeclarationSyntax action)
    {
        for (var current = node.Parent; current is not null && current != action; current = current.Parent)
        {
            if (current is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
                return true;
        }

        return false;
    }

    // Một biểu thức trả về có thể là một cây quyết định. Bóc tới các lá thật sự dựng phản hồi.
    private static IEnumerable<ExpressionSyntax> Leaves(ExpressionSyntax expression)
        => expression switch
        {
            ParenthesizedExpressionSyntax parenthesized => Leaves(parenthesized.Expression),
            AwaitExpressionSyntax awaited => Leaves(awaited.Expression),
            ConditionalExpressionSyntax conditional => Leaves(conditional.WhenTrue).Concat(Leaves(conditional.WhenFalse)),
            SwitchExpressionSyntax @switch => @switch.Arms.SelectMany(arm => Leaves(arm.Expression)),
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression)
                => Leaves(binary.Left).Concat(Leaves(binary.Right)),
            _ => [expression],
        };

    private static bool IsAllowed(ExpressionSyntax leaf)
        => leaf switch
        {
            // `throw` không đặt status nào — IExceptionHandler làm việc đó (§2.4).
            ThrowExpressionSyntax => true,
            InvocationExpressionSyntax invocation => HandleMethods.Contains(MethodName(invocation)),
            _ => false,
        };

    // Tên phương thức được gọi, bỏ tham số kiểu: `HandleResult<UserDto>(...)` và `this.HandleResult(...)` đều là
    // `HandleResult`. So bằng chuỗi thô sẽ trượt cả hai.
    private static string MethodName(InvocationExpressionSyntax invocation)
    {
        var target = invocation.Expression;
        if (target is MemberAccessExpressionSyntax member)
            target = member.Name;

        return target switch
        {
            GenericNameSyntax generic => generic.Identifier.Text,
            SimpleNameSyntax simple => simple.Identifier.Text,
            _ => target.ToString(),
        };
    }

    // Dòng offender phải đọc được trong log CI: gộp khoảng trắng, cắt cho gọn.
    private static string Compact(ExpressionSyntax expression)
    {
        var text = string.Join(' ', expression.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length <= 80 ? text : text[..77] + "...";
    }
}
