using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Một lớp test có ghi trạng thái tĩnh của CoreMetrics hay không, và nó có nằm trong collection canh
// trạng thái đó hay không. Trả về TÊN KIỂU chứ không phải một câu kết luận, để chỗ gọi tự hợp nhất
// theo tên — lớp `partial` trải trên nhiều file thì thuộc tính `[Collection]` chỉ nằm ở MỘT phần, và
// một phép dò xét từng file riêng lẻ sẽ báo oan đúng phần còn lại.
//
// Quét AST (Roslyn syntax), KHÔNG quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.4. Nhờ
// vậy một dòng chú thích nhắc tên hàm không bị tính là một lời gọi.
//
// VÌ SAO CÓ CỔNG NÀY. `CoreMetrics.SetOutboxSnapshot` ghi vào biến tĩnh toàn tiến trình, và ba
// ObservableGauge đọc thẳng từ đó. xUnit chạy các lớp test song song trong cùng tiến trình, nên hai
// lớp cùng ghi mà không chung collection thì lớp này đọc ra giá trị lớp kia vừa đặt. Đã dựng lại:
// gỡ `[Collection]` khỏi một lớp rồi cho một lớp khác ghi đè trong 3 giây → hai test gauge đỏ 3/3 lần;
// trả thuộc tính lại → xanh 3/3. Không có cổng thì luật này chỉ sống trong một dòng chú thích, và
// lớp test thứ ba viết sau sẽ không đọc dòng đó.
internal sealed record MetricStateTouch(string TypeName, string FilePath);

internal static class GlobalMetricStateScanner
{
    // Hàm ghi trạng thái tĩnh. Đổi tên hàm bên Core thì `TouchesRealSource` đỏ, chứ cổng không lặng
    // lẽ quét một cái tên không còn tồn tại rồi luôn PASS (luật T6).
    public const string GlobalStateMethod = "SetOutboxSnapshot";

    // Các kiểu KHAI Ở NGOÀI CÙNG có gọi `…SetOutboxSnapshot(…)` ở bất kỳ đâu bên trong — kể cả trong
    // kiểu lồng, lambda hay hàm cục bộ. Lấy kiểu ngoài cùng vì `[Collection]` gắn cho lớp test, không
    // gắn cho kiểu lồng bên trong nó.
    public static IReadOnlyList<MetricStateTouch> TouchPoints(string source, string filePath)
        => TopLevelTypes(source)
            .Where(WritesGlobalState)
            .Select(t => new MetricStateTouch(t.Identifier.Text, filePath))
            .ToList();

    // Tên các kiểu ngoài cùng mang `[Collection(<tên collection>)]` — cả dạng chuỗi thẳng lẫn dạng
    // trỏ vào hằng số của kiểu khai collection.
    public static IReadOnlyList<string> GuardedTypeNames(string source, string collectionName, string collectionTypeName)
        => TopLevelTypes(source)
            .Where(t => IsInCollection(t, collectionName, collectionTypeName))
            .Select(t => t.Identifier.Text)
            .ToList();

    private static IEnumerable<TypeDeclarationSyntax> TopLevelTypes(string source)
        => CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()
            .DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Where(t => t.Parent is not TypeDeclarationSyntax);

    private static bool WritesGlobalState(TypeDeclarationSyntax type)
        => type.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Any(invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.Text == GlobalStateMethod,
                IdentifierNameSyntax identifier => identifier.Identifier.Text == GlobalStateMethod,
                _ => false,
            });

    private static bool IsInCollection(TypeDeclarationSyntax type, string collectionName, string collectionTypeName)
    {
        foreach (var attribute in type.AttributeLists.SelectMany(list => list.Attributes))
        {
            // Nơi KHAI collection không phải một lớp test nằm trong nó.
            if (NameOf(attribute) is "CollectionDefinition" or "CollectionDefinitionAttribute")
                return false;
        }

        foreach (var attribute in type.AttributeLists.SelectMany(list => list.Attributes))
        {
            if (NameOf(attribute) is not ("Collection" or "CollectionAttribute"))
                continue;

            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression;

            if (argument is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.StringLiteralExpression)
                && string.Equals(literal.Token.ValueText, collectionName, StringComparison.Ordinal))
                return true;

            if (argument is MemberAccessExpressionSyntax member
                && member.ToString().Contains(collectionTypeName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string NameOf(AttributeSyntax attribute) => attribute.Name switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
        SimpleNameSyntax simple => simple.Identifier.Text,
        _ => attribute.Name.ToString(),
    };
}
