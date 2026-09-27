using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Luật B2 (docs/RULES.md): thứ tự đăng ký pipeline behavior là thứ tự chạy — ValidationBehavior phải đứng TRƯỚC
// TransactionBehavior. Hai phía đọc cùng một hình dạng `AddOpenBehavior(typeof(X<,>))`:
//   • code: mọi lời gọi AddOpenBehavior trong thân AddCoreApplication(), theo thứ tự xuất hiện (Roslyn syntax);
//   • luật: khối mã của docs/quy-uoc/be-cqrs-handler.md §5.1 — định nghĩa gốc của danh sách và thứ tự.
internal static partial class PipelineBehaviorOrderScanner
{
    public static IReadOnlyList<string> ReadRegistrationOrder(string source, string methodName = "AddCoreApplication")
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .SingleOrDefault(m => m.Identifier.Text == methodName)
            ?? throw new InvalidOperationException($"Không tìm thấy phương thức {methodName} — tệp đã đổi hình dạng, cập nhật test.");

        return method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "AddOpenBehavior" })
            .Select(i => i.ArgumentList.Arguments.FirstOrDefault()?.Expression)
            .OfType<TypeOfExpressionSyntax>()
            .Select(t => BehaviorName(t.Type))
            .ToList();
    }

    public static IReadOnlyList<string> ReadDocumentedOrder(string markdown)
    {
        var lines = markdown.Split('\n');
        var start = Array.FindIndex(lines, l => l.StartsWith("### 5.1", StringComparison.Ordinal));
        if (start < 0)
            throw new InvalidOperationException("Không tìm thấy mục ### 5.1 trong tài liệu — mục đã đổi chỗ, cập nhật test.");

        var end = Array.FindIndex(lines, start + 1, l => l.StartsWith("### ", StringComparison.Ordinal));
        var section = lines[start..(end < 0 ? lines.Length : end)];

        return section
            .Select(l => DocumentedBehavior().Match(l))
            .Where(m => m.Success)
            .Select(m => m.Groups["name"].Value)
            .ToList();
    }

    private static string BehaviorName(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => generic.Identifier.Text,
        QualifiedNameSyntax { Right: GenericNameSyntax generic } => generic.Identifier.Text,
        _ => type.ToString(),
    };

    [GeneratedRegex(@"AddOpenBehavior\(\s*typeof\(\s*(?:[\w.]+\.)?(?<name>\w+)\s*<")]
    private static partial Regex DocumentedBehavior();
}
