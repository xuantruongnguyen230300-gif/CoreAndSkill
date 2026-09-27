using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Quét AST (Roslyn syntax), KHÔNG quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.4.
// Dùng cho R5: cấm reflection ở cầu nối Result → HTTP.
internal static class ReflectionUsageDetector
{
    private static readonly string[] BannedMemberNames = ["GetMethod", "Invoke", "MakeGenericType", "CreateInstance"];

    public static IReadOnlyList<string> Scan(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var violations = new List<string>();

        foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (usingDirective.Name?.ToString().StartsWith("System.Reflection", StringComparison.Ordinal) == true)
                violations.Add($"using {usingDirective.Name};");
        }

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && BannedMemberNames.Contains(memberAccess.Name.Identifier.Text))
            {
                violations.Add(invocation.ToString());
            }
        }

        return violations;
    }
}
