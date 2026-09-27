using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Quét AST (Roslyn syntax), KHÔNG quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.4.
// Dùng cho S1 (docs/RULES.md §6): Core không được khai hằng số vai trò cụ thể — role là DỮ LIỆU
// trong bảng core.role, không phải một định danh gắn cứng trong code.
//
// Ba hình dạng bắt, cùng một heuristic cốt lõi — TÊN gợi ý "đây là một vai trò" (chứa "Role") VÀ
// GIÁ TRỊ trông như tên vai trò cụ thể (chỉ chữ cái, chữ đầu viết hoa, kiểu "Admin",
// "SystemOperator"):
//   1. Hằng số (const, field hoặc local) hoặc trường static readonly kiểu string vô hướng.
//   2. Trường static readonly (hoặc local không const) kiểu mảng/tập hợp string (string[],
//      List<string>, IReadOnlyList<string>, v.v.) với initializer là mảng/tập hợp literal — TÊN
//      biến/field gợi ý vai trò và MỌI phần tử literal đều trông như tên vai trò cụ thể.
//   3. enum có TÊN gợi ý vai trò (chứa "Role") — mỗi THÀNH VIÊN trông như tên vai trò cụ thể là một
//      vi phạm riêng.
// Khác với mã quyền dạng "core.role.read" (CorePermissions.RoleRead — tên chứa "Role" nhưng giá
// trị là mã quyền, chữ thường, có dấu chấm) hay khoá kỹ thuật dạng "roleId"/"RoleNameIndex" (chữ
// đầu thường, hoặc không phải giá trị của MỘT hằng số string).
internal static class RoleConstantScanner
{
    public static IReadOnlyList<string> Scan(string source, string filePath)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var violations = new List<string>();

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var type = field.Declaration.Type;

            if (IsStringConstantDeclaration(field.Modifiers, type))
                CheckScalarDeclarators(field.Declaration.Variables, filePath, violations);
            else if (IsStaticReadonlyStringCollectionDeclaration(field.Modifiers, type))
                CheckCollectionDeclarators(field.Declaration.Variables, filePath, violations);
        }

        foreach (var local in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
        {
            var type = local.Declaration.Type;

            if (local.IsConst && IsStringType(type))
                CheckScalarDeclarators(local.Declaration.Variables, filePath, violations);
            else if (!local.IsConst && IsStringCollectionType(type))
                CheckCollectionDeclarators(local.Declaration.Variables, filePath, violations);
        }

        foreach (var enumDeclaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
            CheckEnumMembers(enumDeclaration, filePath, violations);

        return violations;
    }

    // ===== 1. Hằng số string vô hướng (hình dạng gốc) =====

    private static void CheckScalarDeclarators(
        SeparatedSyntaxList<VariableDeclaratorSyntax> declarators, string filePath, List<string> violations)
    {
        foreach (var declarator in declarators)
        {
            if (declarator.Initializer?.Value is not LiteralExpressionSyntax literal
                || !literal.IsKind(SyntaxKind.StringLiteralExpression))
                continue;

            var name = declarator.Identifier.Text;
            var value = literal.Token.ValueText;

            if (LooksLikeRoleConstant(name, value))
                violations.Add($"{filePath}: {name} = \"{value}\"");
        }
    }

    private static bool IsStringConstantDeclaration(SyntaxTokenList modifiers, TypeSyntax type)
    {
        if (!IsStringType(type))
            return false;

        var modifierTexts = modifiers.Select(m => m.Text).ToHashSet(StringComparer.Ordinal);
        return modifierTexts.Contains("const")
            || (modifierTexts.Contains("static") && modifierTexts.Contains("readonly"));
    }

    private static bool IsStringType(TypeSyntax type)
        => type is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.StringKeyword);

    // ===== 2. Mảng/tập hợp string với initializer literal =====

    private static void CheckCollectionDeclarators(
        SeparatedSyntaxList<VariableDeclaratorSyntax> declarators, string filePath, List<string> violations)
    {
        foreach (var declarator in declarators)
        {
            var name = declarator.Identifier.Text;

            if (!name.Contains("Role", StringComparison.Ordinal))
                continue;

            var elements = TryGetStringLiteralElements(declarator.Initializer?.Value);

            if (elements is { Count: > 0 } && elements.All(IsRoleLikeLiteral))
                violations.Add($"{filePath}: {name} = [{string.Join(", ", elements.Select(e => $"\"{e}\""))}]");
        }
    }

    private static bool IsStaticReadonlyStringCollectionDeclaration(SyntaxTokenList modifiers, TypeSyntax type)
    {
        if (!IsStringCollectionType(type))
            return false;

        var modifierTexts = modifiers.Select(m => m.Text).ToHashSet(StringComparer.Ordinal);
        return modifierTexts.Contains("static") && modifierTexts.Contains("readonly");
    }

    private static bool IsStringCollectionType(TypeSyntax type) => type switch
    {
        ArrayTypeSyntax arrayType => IsStringType(arrayType.ElementType),
        GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic =>
            IsStringType(generic.TypeArgumentList.Arguments[0]),
        QualifiedNameSyntax qualified => IsStringCollectionType(qualified.Right),
        _ => false,
    };

    // Trả về danh sách phần tử NẾU mọi phần tử của initializer đều là literal string (mảng/tập hợp
    // "trong suốt" — toàn giá trị cố định, không có biến/lời gọi hàm nào). Có phần tử không phải
    // literal string thì trả null — không suy diễn, bỏ qua khai báo đó (an toàn hơn báo sai).
    private static IReadOnlyList<string>? TryGetStringLiteralElements(ExpressionSyntax? initializer) => initializer switch
    {
        CollectionExpressionSyntax collection => TryGetElementsFromCollectionExpression(collection),
        ArrayCreationExpressionSyntax { Initializer: { } arrayInit } => TryGetElementsFromInitializer(arrayInit),
        ImplicitArrayCreationExpressionSyntax { Initializer: { } implicitArrayInit } => TryGetElementsFromInitializer(implicitArrayInit),
        ObjectCreationExpressionSyntax { Initializer: { } objectInit } => TryGetElementsFromInitializer(objectInit),
        ImplicitObjectCreationExpressionSyntax { Initializer: { } implicitObjectInit } => TryGetElementsFromInitializer(implicitObjectInit),
        _ => null,
    };

    private static IReadOnlyList<string>? TryGetElementsFromCollectionExpression(CollectionExpressionSyntax collection)
    {
        var elements = new List<string>();

        foreach (var element in collection.Elements)
        {
            if (element is not ExpressionElementSyntax { Expression: LiteralExpressionSyntax literal }
                || !literal.IsKind(SyntaxKind.StringLiteralExpression))
                return null;

            elements.Add(literal.Token.ValueText);
        }

        return elements;
    }

    private static IReadOnlyList<string>? TryGetElementsFromInitializer(InitializerExpressionSyntax initializer)
    {
        var elements = new List<string>();

        foreach (var expression in initializer.Expressions)
        {
            if (expression is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression))
                return null;

            elements.Add(literal.Token.ValueText);
        }

        return elements;
    }

    // ===== 3. enum vai trò =====

    private static void CheckEnumMembers(EnumDeclarationSyntax enumDeclaration, string filePath, List<string> violations)
    {
        var enumName = enumDeclaration.Identifier.Text;

        if (!enumName.Contains("Role", StringComparison.Ordinal))
            return;

        foreach (var member in enumDeclaration.Members)
        {
            var memberName = member.Identifier.Text;

            if (IsRoleLikeLiteral(memberName))
                violations.Add($"{filePath}: enum {enumName}.{memberName}");
        }
    }

    // ===== heuristic dùng chung =====

    private static bool LooksLikeRoleConstant(string name, string value)
        => name.Contains("Role", StringComparison.Ordinal) && IsRoleLikeLiteral(value);

    private static bool IsRoleLikeLiteral(string value)
        => value.Length > 0 && char.IsUpper(value[0]) && value.All(char.IsLetter);
}
