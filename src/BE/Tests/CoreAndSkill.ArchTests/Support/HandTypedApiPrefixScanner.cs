using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CoreAndSkill.ArchTests.Support;

// Quét AST (Roslyn syntax) cho luật A16 (docs/RULES.md §3): tiền tố `api/v<N>` của mọi đường dẫn trong code ghép từ MỘT
// hằng số dùng chung (Core.Web/Http/ApiRoutes.cs), không gõ tay — docs/quy-uoc/be-api-controller.md §8.1.
//
// Bắt mọi phần nội dung chuỗi — literal thường, verbatim, raw, UTF-8, và phần văn bản của chuỗi nội suy — mở đầu bằng
// `api/v` hoặc `/api/v` (không phân biệt hoa thường; khoảng trắng đầu chuỗi bỏ qua). Chú thích không phải token chuỗi
// nên tự nhiên nằm ngoài tầm quét. Tệp khai hằng số được loại ở nơi gọi.
//
// NGOÀI TẦM: tiền tố ghép từ nhiều mảnh lúc chạy (`"ap" + "i/v1"`) — không literal nào mang đủ tiền tố.
internal static partial class HandTypedApiPrefixScanner
{
    public static IReadOnlyList<string> Scan(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        return root.DescendantTokens()
            .Where(IsStringContent)
            .Where(token => HandTypedPrefix().IsMatch(token.ValueText))
            .Select(token => $"{filePath}: chuỗi gõ tay tiền tố API — {token.ValueText.Trim()}")
            .ToList();
    }

    private static bool IsStringContent(SyntaxToken token)
        => token.IsKind(SyntaxKind.StringLiteralToken)
           || token.IsKind(SyntaxKind.InterpolatedStringTextToken)
           || token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken)
           || token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken)
           || token.IsKind(SyntaxKind.Utf8StringLiteralToken)
           || token.IsKind(SyntaxKind.Utf8SingleLineRawStringLiteralToken)
           || token.IsKind(SyntaxKind.Utf8MultiLineRawStringLiteralToken);

    [GeneratedRegex(@"^\s*/?api/v", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HandTypedPrefix();
}
