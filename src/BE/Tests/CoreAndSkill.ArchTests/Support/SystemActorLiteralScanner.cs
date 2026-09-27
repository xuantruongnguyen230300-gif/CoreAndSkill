using CoreAndSkill.Core.Application.Identity;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Chuỗi tên tác nhân hệ thống — thứ ghi vào created_by / updated_by / actor_display khi không có người thực hiện — chỉ có
// MỘT nguồn: SystemActor.UserName (src/BE/Core/CoreAndSkill.Core.Application/Identity/SystemActor.cs). Phép chặn tạo tài
// khoản mang tên đó (SystemActor.IsReservedUserName) và phép so người tải lên (FileAccessPolicy.IsUploader, so Ordinal)
// cùng dựa vào hằng này; một chỗ gõ tay lệch khỏi hằng làm phép chặn hết tác dụng mà không gì báo.
//
// Quét AST (Roslyn syntax), không quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.4. Kim dò ĐỌC TỪ HẰNG
// (SystemActor.UserName), không gõ lại ở đây: gõ lại là tạo đúng nguồn thứ hai mà cổng này sinh ra để cấm. Giá trị của
// hằng được ghim riêng bằng meta-test ở SystemActorLiteralTests.
//
// Bắt một biểu thức chuỗi mà GIÁ TRỊ của nó — sau khi gỡ escape, bỏ tiền tố @ / hậu tố u8 / dấu nháy raw — ĐÚNG BẰNG kim
// dò, không phân biệt hoa thường (OrdinalIgnoreCase):
//   1. literal chuỗi: thường, verbatim, raw một dòng / nhiều dòng, UTF-8 — kể cả khi đứng trong lỗ nội suy của chuỗi khác;
//   2. chuỗi nội suy KHÔNG có lỗ nào ($"system", $"""system""") — ghép mọi đoạn văn bản rồi so CẢ chuỗi. Chuỗi có lỗ
//      ($"{prefix}system") không bị so từng đoạn: một đoạn văn bản tình cờ bằng kim dò trong một chuỗi dài hơn không phải
//      literal tên tác nhân.
//
// Không bắt, và có Detector_* ghim từng ca: chú thích và chú thích XML (trivia của Roslyn), định danh (`SystemActor`,
// `is_system`, `System.Guid`), chuỗi dài hơn chứa chữ system ("system_operator", "System.Text.Json"), chuỗi "Hệ thống".
//
// ĐIỂM MÙ ĐÃ BIẾT — nói ra để không ai đọc cổng này rộng hơn thực tế:
//   1. Giá trị ghép từ nhiều mảnh, lúc biên dịch hay lúc chạy: "sys" + "tem", $"{"sys"}tem", string.Concat(…), nameof(…).
//   2. Kim dò nằm TRONG một chuỗi dài hơn — điển hình là SQL thô `SET updated_by = 'system'`. Cổng chỉ so CẢ chuỗi.
//   3. Chuỗi đệm khoảng trắng (" system ") — không đúng bằng. SystemActor.IsReservedUserName có Trim; cổng thì không.
//   4. Mã trong nhánh #if bị tắt: ParseText không khai ký hiệu tiền xử lý nào (kể cả DEBUG), nên thân `#if DEBUG` là
//      trivia với Roslyn. Hôm nay Core không có #if nào.
//   5. Một tên tác nhân hệ thống KHÁC gõ tay ("scheduler", "system-job"): cổng chỉ biết đúng một giá trị.
//   6. Thư mục Migrations/ (loại ở nơi gọi — migration là lịch sử) và script SQL dưới database/ (ngoài tầm .cs).
internal static class SystemActorLiteralScanner
{
    // Một chỗ gõ tay tên tác nhân hệ thống. Literal là văn bản nguồn của biểu thức, để người đi sửa tìm thấy nó trong tệp.
    internal sealed record Hit(string FilePath, int Line, string Literal);

    public static IReadOnlyList<Hit> Scan(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        return [.. root.DescendantNodes()
            .Select(node => (Node: node, Value: ValueOf(node)))
            .Where(x => x.Value is not null
                        && string.Equals(x.Value, SystemActor.UserName, StringComparison.OrdinalIgnoreCase))
            .Select(x => new Hit(filePath, x.Node.GetLocation().GetLineSpan().StartLinePosition.Line + 1, x.Node.ToString()))];
    }

    // Giá trị chuỗi của một nút, hoặc null nếu nút không phải một chuỗi đọc được trọn vẹn từ source.
    private static string? ValueOf(SyntaxNode node) => node switch
    {
        LiteralExpressionSyntax literal
            when literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.Utf8StringLiteralExpression)
            => literal.Token.ValueText,

        InterpolatedStringExpressionSyntax interpolated
            when interpolated.Contents.All(part => part is InterpolatedStringTextSyntax)
            => string.Concat(interpolated.Contents.Cast<InterpolatedStringTextSyntax>().Select(text => text.TextToken.ValueText)),

        _ => null,
    };
}
