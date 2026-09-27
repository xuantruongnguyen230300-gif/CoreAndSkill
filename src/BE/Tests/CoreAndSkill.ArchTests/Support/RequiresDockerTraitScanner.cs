using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Ba định danh mà phép dò cần biết. KHÔNG hằng hoá giá trị tên collection ở đây — nó được đọc ra
// từ chính file khai nó (TryReadCollectionName), nên đổi giá trị const bên IntegrationTests thì
// phép dò đi theo, không lệch thành nguồn thứ hai.
internal sealed record DockerFixtureMarkers(
    string CollectionName,
    string CollectionTypeName,
    string FixtureTypeName);

// Quét AST (Roslyn syntax), KHÔNG quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.4.
//
// Luật T9 (docs/RULES.md §8): mọi test class dùng fixture PostgreSQL phải mang
// [Trait("Category", "RequiresDocker")]. Không có nó, `dotnet test --filter "Category!=RequiresDocker"`
// (lối loại trừ tường minh ở 04-testing-strategy.md §4.2) KHÔNG loại được lớp đó, nên trên máy dev
// không Docker nó đỏ ở bước dựng container — đúng kiểu hỏng gây hiểu nhầm nhất, vì thông báo nói về
// Docker chứ không nói về test. CI luôn có Docker nên CI xanh và không bắt được gì.
//
// Hai đường "dùng fixture" đều bắt, vì cả hai đều làm xUnit dựng container:
//   1. [Collection(PostgresCollection.Name)] hoặc [Collection("<tên collection>")] — vào collection
//      thì collection fixture được dựng, dù lớp có nhận fixture làm tham số hay không.
//   2. Nhận fixture làm tham số constructor (kể cả primary constructor), hoặc khai
//      IClassFixture<PostgresFixture>.
internal static class RequiresDockerTraitScanner
{
    public const string TraitCategoryKey = "Category";
    public const string RequiresDockerCategory = "RequiresDocker";

    // Đọc giá trị `const string` đầu tiên khai trong kiểu collection — tức chuỗi thật mà
    // [Collection(...)] và [CollectionDefinition(...)] cùng trỏ tới. Không thấy kiểu đó (đổi tên,
    // dời file) thì trả null, và test gọi nó phải đỏ thay vì lặng lẽ quét bằng một tên sai.
    public static string? TryReadCollectionName(string source, string collectionTypeName)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (!string.Equals(type.Identifier.Text, collectionTypeName, StringComparison.Ordinal))
                continue;

            foreach (var field in type.Members.OfType<FieldDeclarationSyntax>())
            {
                if (!field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
                    continue;

                foreach (var declarator in field.Declaration.Variables)
                {
                    if (declarator.Initializer?.Value is LiteralExpressionSyntax literal
                        && literal.IsKind(SyntaxKind.StringLiteralExpression))
                        return literal.Token.ValueText;
                }
            }
        }

        return null;
    }

    // Tập ĐẦU VÀO của phép dò: mọi kiểu có dùng fixture, bất kể đã mang Trait hay chưa. Test T6 dùng
    // hàm này để khẳng định tập duyệt khác rỗng.
    public static IReadOnlyList<string> ScanCandidates(string source, string filePath, DockerFixtureMarkers markers)
        => Types(source)
            .Where(t => UsesDockerFixture(t, markers))
            .Select(t => $"{filePath}: {t.Identifier.Text}")
            .ToList();

    public static IReadOnlyList<string> Scan(string source, string filePath, DockerFixtureMarkers markers)
        => Types(source)
            .Where(t => UsesDockerFixture(t, markers) && !HasRequiresDockerTrait(t))
            .Select(t => $"{filePath}: {t.Identifier.Text} dùng {markers.FixtureTypeName} nhưng thiếu "
                       + $"[Trait(\"{TraitCategoryKey}\", \"{RequiresDockerCategory}\")]")
            .ToList();

    private static IEnumerable<TypeDeclarationSyntax> Types(string source)
        => CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()
            .DescendantNodes().OfType<TypeDeclarationSyntax>();

    private static bool UsesDockerFixture(TypeDeclarationSyntax type, DockerFixtureMarkers markers)
        => IsInDockerCollection(type, markers) || TakesFixture(type, markers);

    private static bool IsInDockerCollection(TypeDeclarationSyntax type, DockerFixtureMarkers markers)
    {
        foreach (var attribute in Attributes(type))
        {
            // [CollectionDefinition(...)] là NƠI KHAI collection, không phải một lớp test trong đó —
            // chính nó không dựng container nào, nên không phải mang Trait.
            if (NameOf(attribute) is "CollectionDefinition" or "CollectionDefinitionAttribute")
                return false;
        }

        foreach (var attribute in Attributes(type))
        {
            if (NameOf(attribute) is not ("Collection" or "CollectionAttribute"))
                continue;

            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression;

            if (argument is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.StringLiteralExpression)
                && string.Equals(literal.Token.ValueText, markers.CollectionName, StringComparison.Ordinal))
                return true;

            if (argument is MemberAccessExpressionSyntax member
                && member.ToString().Contains(markers.CollectionTypeName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool TakesFixture(TypeDeclarationSyntax type, DockerFixtureMarkers markers)
    {
        // Chính kiểu fixture thì không "dùng" fixture.
        if (string.Equals(type.Identifier.Text, markers.FixtureTypeName, StringComparison.Ordinal))
            return false;

        if (type.ParameterList is { } primary
            && primary.Parameters.Any(p => MentionsFixture(p.Type, markers)))
            return true;

        if (type.Members.OfType<ConstructorDeclarationSyntax>()
                .Any(c => c.ParameterList.Parameters.Any(p => MentionsFixture(p.Type, markers))))
            return true;

        // IClassFixture<PostgresFixture> — KHÔNG tính ICollectionFixture<>, vì đó là khai báo
        // collection (PostgresCollection), không phải một lớp test.
        return type.BaseList?.Types.Any(b =>
            b.Type is GenericNameSyntax { Identifier.Text: "IClassFixture" } generic
            && generic.TypeArgumentList.Arguments.Any(a => MentionsFixture(a, markers))) == true;
    }

    private static bool MentionsFixture(TypeSyntax? type, DockerFixtureMarkers markers)
        => type is not null && type.ToString().Contains(markers.FixtureTypeName, StringComparison.Ordinal);

    private static bool HasRequiresDockerTrait(TypeDeclarationSyntax type)
    {
        foreach (var attribute in Attributes(type))
        {
            if (NameOf(attribute) is not ("Trait" or "TraitAttribute"))
                continue;

            var arguments = attribute.ArgumentList?.Arguments;

            if (arguments is not { Count: 2 })
                continue;

            if (LiteralValue(arguments.Value[0]) == TraitCategoryKey
                && LiteralValue(arguments.Value[1]) == RequiresDockerCategory)
                return true;
        }

        return false;
    }

    private static string? LiteralValue(AttributeArgumentSyntax argument)
        => argument.Expression is LiteralExpressionSyntax literal
           && literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;

    private static IEnumerable<AttributeSyntax> Attributes(TypeDeclarationSyntax type)
        => type.AttributeLists.SelectMany(list => list.Attributes);

    private static string NameOf(AttributeSyntax attribute) => attribute.Name switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
        SimpleNameSyntax simple => simple.Identifier.Text,
        _ => attribute.Name.ToString(),
    };
}
