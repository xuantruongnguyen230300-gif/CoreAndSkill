using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Quét AST (Roslyn syntax), KHÔNG quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.4.
// Dùng cho S2 (docs/RULES.md §6): phân quyền phải kiểm bằng permission code (qua
// IPermissionChecker / IReadOnlySet<string> — xem PermissionChecker.cs), không được so sánh trực
// tiếp theo TÊN vai trò.
//
// Bốn hình dạng bắt, cùng chạm một vế là truy cập thành viên hoặc định danh có tên ĐÚNG "Role" hoặc
// "RoleName" (ví dụ `user.RoleName`, `x.Role`):
//   1. So sánh bằng/khác (==, !=) — `user.RoleName == "Admin"`.
//   2. Pattern hằng số (constant pattern, kể cả lồng trong not/or/and pattern) —
//      `user.RoleName is "Admin"`, `x.Role is not "Admin"`.
//   3. switch trên biểu thức đó — `switch (user.RoleName) { case "Admin": ... }`,
//      `user.RoleName switch { "Admin" => ..., _ => ... }`.
//   4. Lời gọi `.Equals(...)`/`.Contains(...)` mà vế nhận (receiver) HOẶC một đối số tham chiếu
//      biểu thức đó — `user.RoleName.Equals("Admin")`,
//      `new[] { "Admin", "Manager" }.Contains(user.RoleName)`.
// KHÔNG bắt so sánh RoleId (định danh Guid, hợp lệ — PermissionChecker, UserPrivilegeGuard đều so
// theo RoleId) hay NormalizedName (kiểm trùng tên khi CRUD vai trò — RoleQueryService, không phải
// quyết định phân quyền) — tên thành viên phải khớp ĐÚNG "Role" hoặc "RoleName", không phải chứa.
internal static class RoleNameComparisonScanner
{
    private static readonly string[] RoleNameMemberNames = ["Role", "RoleName"];
    private static readonly string[] TargetedMethodNames = ["Equals", "Contains"];

    public static IReadOnlyList<string> Scan(string source, string filePath)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var violations = new List<string>();

        foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
        {
            if (!binary.IsKind(SyntaxKind.EqualsExpression) && !binary.IsKind(SyntaxKind.NotEqualsExpression))
                continue;

            if (ReferencesRoleNameMember(binary.Left) || ReferencesRoleNameMember(binary.Right))
                violations.Add($"{filePath}: {binary}");
        }

        foreach (var isPattern in root.DescendantNodes().OfType<IsPatternExpressionSyntax>())
        {
            if (!ReferencesRoleNameMember(isPattern.Expression))
                continue;

            var hasConstantPattern = isPattern.Pattern.DescendantNodesAndSelf().OfType<ConstantPatternSyntax>().Any();

            if (hasConstantPattern)
                violations.Add($"{filePath}: {isPattern}");
        }

        foreach (var switchStatement in root.DescendantNodes().OfType<SwitchStatementSyntax>())
        {
            if (ReferencesRoleNameMember(switchStatement.Expression))
                violations.Add($"{filePath}: switch ({switchStatement.Expression}) {{ ... }}");
        }

        foreach (var switchExpression in root.DescendantNodes().OfType<SwitchExpressionSyntax>())
        {
            if (ReferencesRoleNameMember(switchExpression.GoverningExpression))
                violations.Add($"{filePath}: {switchExpression.GoverningExpression} switch {{ ... }}");
        }

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            var methodName = memberAccess.Name.Identifier.Text;

            if (!TargetedMethodNames.Contains(methodName, StringComparer.Ordinal))
                continue;

            var referencesReceiver = ReferencesRoleNameMember(memberAccess.Expression);
            var referencesArgument = invocation.ArgumentList.Arguments
                .Any(argument => ReferencesRoleNameMember(argument.Expression));

            if (referencesReceiver || referencesArgument)
                violations.Add($"{filePath}: {invocation}");
        }

        return violations;
    }

    private static bool ReferencesRoleNameMember(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax memberAccess =>
            RoleNameMemberNames.Contains(memberAccess.Name.Identifier.Text, StringComparer.Ordinal),
        IdentifierNameSyntax identifier =>
            RoleNameMemberNames.Contains(identifier.Identifier.Text, StringComparer.Ordinal),
        _ => false,
    };
}
