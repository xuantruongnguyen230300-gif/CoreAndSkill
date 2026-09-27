using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Phép dò CHUNG của các luật "không ghi bảng X bằng đường bỏ qua ChangeTracker" — S17 (core.role_permission) và S21
// (core.app_user_role), docs/RULES.md §6. Nhật ký kiểm toán của hai bảng đó đi qua AuditLogInterceptor, tức qua
// ChangeTracker; một lần ghi bằng ExecuteUpdate/ExecuteDelete hay SQL thô không để lại dòng nào. Một phép dò, nhiều
// đích: luật mới cùng khuôn chỉ khai thêm một ChangeTrackerBypassTarget, không chép lại phép dò.
//
// Quét AST (Roslyn syntax), hai hình dạng bắt:
//   1. Lời gọi ExecuteUpdate/ExecuteUpdateAsync/ExecuteDelete/ExecuteDeleteAsync mà biểu thức nhận (chuỗi truy vấn đứng
//      trước) có nhắc tới DbSet của đích (`RolePermissions`, `UserRoles`) hoặc `Set<…Kiểu>()`.
//   2. SQL thô: một literal chuỗi (thường, nội suy, raw) chứa tên bảng của đích như một TỪ VÀ một động từ SQL
//      (select/insert/update/delete/merge/truncate/copy). Literal chỉ mang tên bảng — `ToTable("app_user_role")`, tên
//      index, tên ràng buộc — không có động từ nên không bị bắt; tên ràng buộc `fk_app_user_role_…` không chứa tên bảng
//      như một từ.
//
// NGOÀI TẦM: SQL ghép từ nhiều mảnh lúc chạy (không literal nào mang đủ cả tên bảng lẫn động từ), DbSet gán qua biến
// trung gian trước khi gọi ExecuteDelete, và script dưới database/scripts/ do người vận hành chạy. Thư mục Migrations/
// được loại ở nơi gọi.
internal sealed record ChangeTrackerBypassTarget(string DbSetName, string EntityTypeName, string TableName)
{
    public static readonly ChangeTrackerBypassTarget RolePermission = new("RolePermissions", "RolePermission", "role_permission");

    public static readonly ChangeTrackerBypassTarget AppUserRole = new("UserRoles", "AppUserRole", "app_user_role");
}

internal static partial class ChangeTrackerBypassScanner
{
    private static readonly string[] BulkMethods = ["ExecuteUpdate", "ExecuteUpdateAsync", "ExecuteDelete", "ExecuteDeleteAsync"];

    public static IReadOnlyList<string> Scan(string source, string filePath, ChangeTrackerBypassTarget target)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var tableName = new Regex($@"\b{Regex.Escape(target.TableName)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var violations = new List<string>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            if (!BulkMethods.Contains(memberAccess.Name.Identifier.Text, StringComparer.Ordinal))
                continue;

            if (Targets(memberAccess.Expression, target))
                violations.Add($"{filePath}: {memberAccess.Name.Identifier.Text} trên {target.EntityTypeName} — {memberAccess.Expression}");
        }

        foreach (var token in root.DescendantTokens())
        {
            if (!IsStringContent(token))
                continue;

            var text = token.ValueText;
            if (tableName.IsMatch(text) && SqlVerb().IsMatch(text))
                violations.Add($"{filePath}: SQL thô chạm {target.TableName} — {text.Trim()}");
        }

        return violations;
    }

    private static bool Targets(ExpressionSyntax receiver, ChangeTrackerBypassTarget target)
        => receiver.DescendantNodesAndSelf().Any(node => node switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text == target.DbSetName,
            GenericNameSyntax generic => generic.TypeArgumentList.Arguments.Any(a => a.ToString().EndsWith(target.EntityTypeName, StringComparison.Ordinal)),
            _ => false,
        });

    private static bool IsStringContent(SyntaxToken token)
        => token.IsKind(SyntaxKind.StringLiteralToken)
           || token.IsKind(SyntaxKind.InterpolatedStringTextToken)
           || token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken)
           || token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken)
           || token.IsKind(SyntaxKind.Utf8StringLiteralToken)
           || token.IsKind(SyntaxKind.Utf8SingleLineRawStringLiteralToken)
           || token.IsKind(SyntaxKind.Utf8MultiLineRawStringLiteralToken);

    [GeneratedRegex(@"\b(select|insert|update|delete|merge|truncate|copy)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SqlVerb();
}
