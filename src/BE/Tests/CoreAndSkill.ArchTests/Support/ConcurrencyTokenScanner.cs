using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Luật E15 (docs/RULES.md §4; định nghĩa gốc docs/quy-uoc/be-entity-domain.md §6.2–§6.3): concurrency token của một
// entity là `uint` mang `.IsRowVersion()` — không `byte[]`, không shadow property.
//
// VÌ SAO LUẬT NÀY CÓ CỔNG DÙ PHƠI NHIỄM HÔM NAY BẰNG 0. Trên Npgsql, `byte[]` + `.IsRowVersion()` KHÔNG tạo một cột
// tự tăng: nó tạo một cột `bytea` bình thường mà không ai cập nhật, nên `WHERE "RowVersion" = @original` LUÔN khớp và
// check đồng thời vô hiệu — hoàn toàn, im lặng, không cảnh báo. Một test kiểu "hai lượt ghi, lượt sau thắng" vẫn
// xanh. Đây là luật canh một HỎNG IM LẶNG, không canh một lỗi; người đầu tiên thêm token sẽ làm một mình.
//
// PHÉP DÒ đọc CÚ PHÁP của tệp cấu hình và đọc KIỂU qua reflection:
//   • cú pháp cho biết `.IsRowVersion()` bám vào `Property` nào, và bám theo dạng nào (biểu thức lambda hay chuỗi);
//   • reflection cho biết kiểu CLR thật của property đó trên chính `T` — thứ mà một phép đọc cú pháp thuần phải đi
//     tìm trong tệp entity và sẽ đọc sai khi entity kế thừa property từ lớp cha.
//
// ĐIỂM MÙ ĐÃ BIẾT:
//   1. Chỉ xét tệp khai `IEntityTypeConfiguration<T>`. Cấu hình viết thẳng trong `OnModelCreating` hoặc qua một
//      convention nằm ngoài tầm. Chỗ này im lặng.
//   2. `.IsRowVersion()` đứng sau một biến trung gian (`var p = builder.Property(...); p.IsRowVersion();`) thì phép
//      đi ngược chuỗi không tới được `Property`. Ca đó trả về `Unresolved` và cổng làm ĐỎ, không bỏ qua.
//   3. Phép dò KHÔNG canh entity nào CẦN token — đó là §6.1 của be-entity-domain.md, một câu hỏi thiết kế. Nó chỉ
//      canh: đã khai token thì khai đúng kiểu.
internal static class ConcurrencyTokenScanner
{
    private const string RowVersionMethod = "IsRowVersion";
    private const string PropertyMethod = "Property";
    private const string ConfigurationInterface = "IEntityTypeConfiguration";

    // Một lời gọi `.IsRowVersion()` đọc được từ một tệp cấu hình.
    //   EntityType   — tham số kiểu của IEntityTypeConfiguration<T>; null nghĩa là không đọc được.
    //   PropertyName — tên property mà lời gọi bám vào; null nghĩa là phép đi ngược chuỗi không tới được `Property`.
    //   IsShadow     — dạng `builder.Property<T>("tên")`: không có property CLR nào để kiểm kiểu.
    internal sealed record RowVersionCall(
        string FilePath, string? EntityType, string? PropertyName, bool IsShadow, int Line);

    public static bool IsEntityConfigurationFile(string source)
        => source.Contains(ConfigurationInterface, StringComparison.Ordinal);

    public static IReadOnlyList<RowVersionCall> Calls(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        return [.. root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(IsRowVersionCall)
            .Select(call =>
            {
                var (propertyName, isShadow) = PropertyOf(call);
                return new RowVersionCall(
                    filePath,
                    EntityTypeOf(call),
                    propertyName,
                    isShadow,
                    call.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
            })];
    }

    // Số tệp khai IEntityTypeConfiguration<T> trong tập — chốt T6 cho TẦM QUÉT, độc lập với số lời gọi IsRowVersion.
    public static IReadOnlyList<string> ConfigurationFiles(IEnumerable<string> files)
        => [.. files.Where(f => IsEntityConfigurationFile(File.ReadAllText(f)))];

    // ---- nội bộ ---------------------------------------------------------------------------------

    private static bool IsRowVersionCall(InvocationExpressionSyntax call)
        => call.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text == RowVersionMethod,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text == RowVersionMethod,
            _ => false,
        };

    // `IEntityTypeConfiguration<Job>` trên lớp bao quanh lời gọi.
    private static string? EntityTypeOf(SyntaxNode call)
    {
        for (var node = call.Parent; node is not null; node = node.Parent)
        {
            if (node is not TypeDeclarationSyntax type || type.BaseList is null)
                continue;

            foreach (var baseType in type.BaseList.Types)
            {
                if (baseType.Type is GenericNameSyntax { Identifier.Text: ConfigurationInterface } generic
                    && generic.TypeArgumentList.Arguments.Count == 1)
                {
                    return generic.TypeArgumentList.Arguments[0].ToString();
                }
            }
        }

        return null;
    }

    // Đi NGƯỢC chuỗi gọi tới lời gọi `Property` gần nhất — `.HasColumnName(...)` xen giữa không được làm mất dấu.
    private static (string? Name, bool IsShadow) PropertyOf(InvocationExpressionSyntax call)
    {
        for (var node = Receiver(call.Expression); node is not null; node = Receiver(node))
        {
            if (node is not InvocationExpressionSyntax invocation)
                continue;

            var name = invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name,
                MemberBindingExpressionSyntax b => b.Name,
                _ => null,
            };

            if (name is null || NameText(name) != PropertyMethod || invocation.ArgumentList.Arguments.Count == 0)
                continue;

            var argument = invocation.ArgumentList.Arguments[0].Expression;

            // Dạng shadow: `builder.Property<byte[]>("RowVersion")` — đối số là một chuỗi, không có property CLR nào.
            if (argument is LiteralExpressionSyntax { Token.Value: string shadowName })
                return (shadowName, true);

            // Dạng lambda: `builder.Property(x => x.Version)`.
            if (argument is SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax body })
                return (body.Name.Identifier.Text, false);

            return (null, false);
        }

        return (null, false);
    }

    private static string NameText(SimpleNameSyntax name)
        => name is GenericNameSyntax generic ? generic.Identifier.Text : name.Identifier.Text;

    private static SyntaxNode? Receiver(SyntaxNode? expression)
        => expression switch
        {
            MemberAccessExpressionSyntax member => member.Expression,
            ConditionalAccessExpressionSyntax conditional => conditional.Expression,
            InvocationExpressionSyntax invocation => invocation.Expression,
            _ => null,
        };
}
