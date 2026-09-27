using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Quét AST (Roslyn syntax) cho luật B5 (docs/RULES.md): handler không tự lưu, không tự quản transaction —
// docs/quy-uoc/be-cqrs-handler.md §3.1. Một handler tự lưu ghi NGOÀI transaction của TransactionBehavior, và ca đó chỉ lộ
// khi nửa sau của handler hỏng.
//
// Phạm vi là KIỂU CÀI IRequestHandler<…> (danh sách kế thừa của khai báo kiểu có tên IRequestHandler), không phải cả
// assembly: ImportJobExecutor gọi SaveChangesAsync hợp lệ vì nó chạy trong job nền, ngoài pipeline. Nó nằm ngoài tầm vì nó
// không phải handler — không có allowlist nào.
internal static class HandlerSelfPersistenceScanner
{
    // Lưu và quản transaction — cột "Không làm" của bảng §3.1.
    private static readonly HashSet<string> ForbiddenCalls = new(StringComparer.Ordinal)
    {
        "SaveChanges", "SaveChangesAsync",
        "BeginTransaction", "BeginTransactionAsync",
        "ExecuteInTransactionAsync",
        "Commit", "CommitAsync",
        "Rollback", "RollbackAsync",
    };

    public static IReadOnlyList<string> Scan(string source, string filePath)
        => Handlers(source)
            .SelectMany(handler => handler.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Select(invocation => (Handler: handler, Name: CalledName(invocation)))
                .Where(x => x.Name is not null && ForbiddenCalls.Contains(x.Name))
                .Select(x => $"{filePath}: handler {x.Handler.Identifier.Text} tự gọi {x.Name}"))
            .ToList();

    public static IReadOnlyList<string> HandlerNames(string source)
        => Handlers(source).Select(h => h.Identifier.Text).ToList();

    private static IEnumerable<TypeDeclarationSyntax> Handlers(string source)
        => CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()
            .DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Where(type => type.BaseList?.Types.Any(b => IsRequestHandler(b.Type)) == true);

    private static bool IsRequestHandler(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => generic.Identifier.Text == "IRequestHandler",
        QualifiedNameSyntax { Right: GenericNameSyntax generic } => generic.Identifier.Text == "IRequestHandler",
        _ => false,
    };

    private static string? CalledName(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text,
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        GenericNameSyntax generic => generic.Identifier.Text,
        _ => null,
    };
}
