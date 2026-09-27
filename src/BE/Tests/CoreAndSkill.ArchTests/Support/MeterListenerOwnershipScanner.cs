using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Một chỗ trong mã test tự dựng `MeterListener`, và nó nằm ở tệp nào / trong kiểu nào.
internal sealed record MeterListenerConstruction(string TypeName, string FilePath);

// Luật T11 (docs/DEBT.md): kỹ thuật cô lập phép đo chỉ có MỘT bản trong src/BE/Tests. Lọc phép đo theo
// luồng logic sống ở tệp dùng chung `Tests/Shared/MeterProbe.cs` (ADR-0070); ngoài chỗ đó, không lớp
// test nào được tự dựng `MeterListener` của riêng nó.
//
// VÌ SAO CÓ CỔNG NÀY. Bản chép trong `CoreAndSkill.Core.IntegrationTests` đã LỆCH khỏi bản gốc ngay từ
// lúc sinh ra — nó thiếu đúng hai tính chất mà bản gốc phải thêm sau khi gặp ca hỏng thật: khôi phục
// giá trị `AsyncLocal` cũ khi dispose, và lọc được nhiều `Instrument`. Gỡ bản chép đi mà không dựng
// cổng thì người viết lớp test thứ ba sẽ dựng bản thứ hai, và không gì đỏ.
//
// Quét AST (Roslyn syntax), KHÔNG quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.4. Ở cổng
// này AST không phải sở thích mà là điều kiện đủ, vì HAI lý do ngược chiều nhau:
//
//   • Quét văn bản `new MeterListener()` bỏ sót đúng hình dạng mà bản thật đang dùng —
//     `private readonly MeterListener _listener = new();`. Một bộ dò như vậy đọc được 0 chỗ hợp lệ,
//     tức nó sẽ PASS trên một tập rỗng (T6) trong khi khoe đang canh.
//   • Quét văn bản lại BÁO OAN chính tệp test của cổng này: các ca `Detector_*` bên dưới mang mã vi
//     phạm trong chuỗi ký tự. AST thấy chúng là token chuỗi, không phải lời gọi khởi tạo.
internal static class MeterListenerOwnershipScanner
{
    // Kiểu bị canh. Đổi tên kiểu bên BCL thì `ScansAtLeastOneRealConstruction` đỏ, chứ cổng không lặng
    // lẽ quét một cái tên không còn tồn tại rồi luôn PASS (luật T6).
    public const string ListenerTypeName = "MeterListener";

    public static IReadOnlyList<MeterListenerConstruction> Constructions(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var found = new List<MeterListenerConstruction>();

        foreach (var node in root.DescendantNodes())
        {
            var constructs = node switch
            {
                ObjectCreationExpressionSyntax explicitNew => IsListener(explicitNew.Type),
                ImplicitObjectCreationExpressionSyntax implicitNew => IsListener(TargetTypeOf(implicitNew)),
                _ => false,
            };

            if (constructs)
                found.Add(new MeterListenerConstruction(OuterTypeName(node), filePath));
        }

        return found;
    }

    // `new()` không nói ra kiểu của nó, nên phải đọc kiểu ở chỗ khai. Bốn hình dạng khai có thật trong
    // mã test — trường, biến cục bộ, thuộc tính có giá trị đầu, và thân biểu thức `=> new()`; cộng
    // `return new()` trong một thành viên khai kiểu trả về tường minh.
    //
    // GIỚI HẠN ĐÃ BIẾT, nói ra thay vì để người đọc tưởng cổng phủ hết: `new()` ở vị trí đối số của một
    // lời gọi — `Watch(new())` — thì kiểu đích chỉ đọc được bằng semantic model, mà cổng này cố ý chạy
    // thuần syntax để không phải dựng Compilation. Hình dạng đó không xuất hiện trong mã test hôm nay,
    // và nó vẫn bị chặn ở cửa khác: một bản chép của kỹ thuật lọc luôn phải GIỮ listener lại trong một
    // trường hoặc biến để `Dispose`, tức phải khai kiểu ra.
    private static TypeSyntax? TargetTypeOf(ImplicitObjectCreationExpressionSyntax node)
    {
        switch (node.Parent)
        {
            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } }:
                return declaration.Type;

            case EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax property }:
                return property.Type;

            case ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax property }:
                return property.Type;

            case ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax method }:
                return method.ReturnType;
        }

        if (node.Ancestors().OfType<ReturnStatementSyntax>().FirstOrDefault() is null)
            return null;

        return node.Ancestors().FirstOrDefault(a => a is MethodDeclarationSyntax or PropertyDeclarationSyntax) switch
        {
            MethodDeclarationSyntax method => method.ReturnType,
            PropertyDeclarationSyntax property => property.Type,
            _ => null,
        };
    }

    private static bool IsListener(TypeSyntax? type) => type switch
    {
        null => false,
        IdentifierNameSyntax identifier => identifier.Identifier.Text == ListenerTypeName,
        QualifiedNameSyntax qualified => IsListener(qualified.Right),
        AliasQualifiedNameSyntax alias => IsListener(alias.Name),
        NullableTypeSyntax nullable => IsListener(nullable.ElementType),
        _ => false,
    };

    // Kiểu NGOÀI CÙNG chứa chỗ dựng — đó là thứ người đọc thông điệp lỗi cần biết để đi sửa. Mã không
    // nằm trong kiểu nào (top-level statement) thì nói thẳng thế, không trả chuỗi rỗng.
    private static string OuterTypeName(SyntaxNode node)
        => node.Ancestors().OfType<TypeDeclarationSyntax>().LastOrDefault()?.Identifier.Text
           ?? "<không nằm trong kiểu nào>";
}
