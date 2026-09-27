using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Quét AST (Roslyn syntax) cho luật B6 (docs/RULES.md): mỗi lời gọi IgnoreQueryFilters phải NÊU TÊN filter được bỏ. Dạng
// không tham số bỏ CẢ HAI filter cùng lúc — người viết định bỏ lọc xoá mềm lại bỏ luôn lọc đơn vị, và không gì báo.
//
// Bắt cả dạng mở rộng (`query.IgnoreQueryFilters()`) lẫn dạng gọi tĩnh
// (`EntityFrameworkQueryableExtensions.IgnoreQueryFilters(query)`) — ở dạng tĩnh, đối số đầu là truy vấn, không phải tên.
internal static class UnnamedQueryFilterBypassScanner
{
    private const string MethodName = "IgnoreQueryFilters";
    private const string StaticHost = "EntityFrameworkQueryableExtensions";

    public static IReadOnlyList<string> Scan(string source, string filePath)
        => Calls(source)
            .Where(call => FilterArgumentCount(call) == 0)
            .Select(call => $"{filePath}: IgnoreQueryFilters không nêu tên filter — {call}")
            .ToList();

    public static int CountCalls(string source) => Calls(source).Count();

    private static IEnumerable<InvocationExpressionSyntax> Calls(string source)
        => CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.Text == MethodName,
                MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text == MethodName,
                _ => false,
            });

    private static int FilterArgumentCount(InvocationExpressionSyntax call)
    {
        var arguments = call.ArgumentList.Arguments.Count;
        var isStaticForm = call.Expression is MemberAccessExpressionSyntax { Expression: var receiver }
                           && receiver.ToString().Split('.').Last() == StaticHost;

        return isStaticForm ? arguments - 1 : arguments;
    }
}
