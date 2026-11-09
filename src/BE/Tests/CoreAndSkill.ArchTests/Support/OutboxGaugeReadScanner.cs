using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

internal enum OutboxGaugeReadKind
{
    // `CoreMetrics.OutboxPending` (hoặc tên đứng trần khi có `using static`).
    MemberReference,

    // Chuỗi `"core.outbox.pending"` — tên instrument dùng với `MeterListener` / `MeterProbe.Value(...)`.
    InstrumentName,
}

// Một chỗ trong mã test ĐỌC một trong ba gauge outbox, nằm ở tệp nào / kiểu ngoài cùng nào.
internal sealed record OutboxGaugeRead(string TypeName, string FilePath, OutboxGaugeReadKind Kind, string Text);

// Mã test nào ĐỌC ba ObservableGauge của outbox (`core.outbox.pending`, `core.outbox.pending_oldest_age`,
// `core.outbox.dead_count`). Nửa còn lại của luật T10: T10 (GlobalMetricStateScanner) bắt lớp test GHI ảnh chụp
// outbox mà thiếu collection; bộ dò này bắt lớp test ĐỌC gauge ở chỗ không được phép đọc.
//
// VÌ SAO CÓ CỔNG NÀY. Ảnh chụp outbox là biến tĩnh toàn tiến trình và ba gauge đọc thẳng từ nó. `OutboxSnapshotCollection`
// chỉ tuần tự hoá được các lớp CÙNG khai collection đó. Một lớp test có ghi ảnh chụp theo ĐƯỜNG GIÁN TIẾP —
// `OutboxDatabaseTests` chạy vòng phát thật, rồi vòng phát gọi `MeasureAsync`, rồi `MeasureAsync` gọi
// `CoreMetrics.SetOutboxSnapshot` — nằm trong `PostgresCollection`, nên không thể đồng thời nằm trong collection kia,
// và T10 chỉ so lời gọi TRỰC TIẾP nên không thấy nó. Hôm nay chưa hỏng vì lớp đọc gauge chỉ sống ở
// `CoreAndSkill.Core.UnitTests` — một tiến trình khác. Hướng đã chốt (2026-11-04): giữ đúng như vậy, và cổng này canh
// rằng mã nguồn của `CoreAndSkill.Core.IntegrationTests` không bao giờ đọc ba gauge.
//
// Quét AST (Roslyn syntax), KHÔNG quét văn bản — docs/wiki-core/be/04-testing-strategy.md §2.4: chú thích nhắc tên gauge
// không phải lần đọc, và tên bảng `core.outbox_message` không bị khớp nhầm với `core.outbox.pending` (so chuỗi
// NGUYÊN VẸN, không so tiền tố).
//
// Bắt hai đường đọc có thật trong repo (`Tests/CoreAndSkill.Core.UnitTests/Common/B4SupportTests.cs`):
//   • tham chiếu thành viên `CoreMetrics.OutboxPending|OutboxOldestPendingAge|OutboxDeadCount` — truyền vào
//     `new MeterProbe(...)`, hoặc lấy `.Name`;
//   • chuỗi tên instrument nguyên vẹn — `probe.Value("core.outbox.pending")`; cả dạng chuỗi nội suy không có chỗ trống.
//
// ĐIỂM MÙ ĐÃ BIẾT, nói ra thay vì để người đọc tưởng cổng phủ hết (cổng chạy thuần syntax, không dựng Compilation):
//   • tên ghép lúc chạy — `"core.outbox." + "pending"`, `$"core.outbox.{x}"`, hằng `const` trỏ sang chuỗi khác;
//   • alias `using M = ...CoreMetrics;` rồi `M.OutboxPending` — chỉ nhận vế trái có tên cuối cùng là `CoreMetrics`;
//   • đọc qua phản chiếu — `typeof(CoreMetrics).GetField("OutboxPending")`: chuỗi `"OutboxPending"` là tên thành viên,
//     không phải tên instrument, nên không khớp hình dạng nào ở trên;
//   • một helper nằm NGOÀI thư mục quét đọc gauge hộ rồi trả về giá trị — lời gọi helper không chứa tên gauge;
//   • mã nguồn nối vào IntegrationTests từ NGOÀI thư mục project bằng `<Compile Include="..\Shared\...">` — chỉ quét
//     tệp nằm dưới thư mục project. Tệp dùng chung `Tests/Shared/MeterProbe.cs` hôm nay không đọc gauge nào; tệp
//     mới được nối vào mà đọc gauge thì cổng này không thấy;
//   • đường đọc bên ngoài mã C# — `/metrics` của một exporter. Sản phẩm chưa cắm exporter nào (xem chú thích đầu
//     `CoreMetrics.cs`); khi cắm, tên bị đổi dạng (`core_outbox_pending`) và bộ dò này không nhận ra.
//   • `IdentifierName` đứng trần trùng tên gauge (một biến cục bộ tên `OutboxPending`) bị tính là lần đọc — báo oan có
//     chủ ý, rẻ hơn bỏ sót `using static`.
internal static class OutboxGaugeReadScanner
{
    // Ba thành viên gauge của `CoreMetrics`. Khớp với nguồn thật bằng `DeclaredOutboxGauges` — thêm gauge thứ tư bên Core
    // mà quên thêm vào đây thì test `GaugeSets_MatchTheRealCoreMetricsSource` đỏ.
    public static readonly IReadOnlySet<string> MemberNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "OutboxPending",
        "OutboxOldestPendingAge",
        "OutboxDeadCount",
    };

    public static readonly IReadOnlySet<string> InstrumentNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "core.outbox.pending",
        "core.outbox.pending_oldest_age",
        "core.outbox.dead_count",
    };

    private const string MetricsTypeName = "CoreMetrics";

    public static IReadOnlyList<OutboxGaugeRead> Reads(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var found = new List<OutboxGaugeRead>();

        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case IdentifierNameSyntax identifier when IsGaugeMemberReference(identifier):
                    found.Add(new OutboxGaugeRead(OuterTypeName(node), filePath, OutboxGaugeReadKind.MemberReference, identifier.Identifier.Text));
                    break;

                case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
                                                         && InstrumentNames.Contains(literal.Token.ValueText):
                    found.Add(new OutboxGaugeRead(OuterTypeName(node), filePath, OutboxGaugeReadKind.InstrumentName, literal.Token.ValueText));
                    break;

                case InterpolatedStringExpressionSyntax interpolated when HolelessText(interpolated) is { } text
                                                                            && InstrumentNames.Contains(text):
                    found.Add(new OutboxGaugeRead(OuterTypeName(node), filePath, OutboxGaugeReadKind.InstrumentName, text));
                    break;
            }
        }

        return found;
    }

    // Các gauge outbox khai trong nguồn `CoreMetrics.cs`: cặp (tên thành viên, tên instrument) của mọi trường được gán
    // `...CreateObservableGauge("core.outbox.…", …)`. Nguồn thật là chủ của tập này; hai tập hằng ở trên chỉ là bản
    // đối chiếu, và test so chúng với kết quả hàm này.
    public static IReadOnlyList<(string Member, string InstrumentName)> DeclaredOutboxGauges(string coreMetricsSource)
    {
        var root = CSharpSyntaxTree.ParseText(coreMetricsSource).GetCompilationUnitRoot();
        var declared = new List<(string, string)>();

        foreach (var variable in root.DescendantNodes().OfType<FieldDeclarationSyntax>().SelectMany(f => f.Declaration.Variables))
        {
            if (variable.Initializer?.Value is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call)
                continue;

            if (access.Name.Identifier.Text != "CreateObservableGauge")
                continue;

            if (call.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.StringLiteralExpression)
                && literal.Token.ValueText.StartsWith("core.outbox.", StringComparison.Ordinal))
                declared.Add((variable.Identifier.Text, literal.Token.ValueText));
        }

        return declared;
    }

    private static bool IsGaugeMemberReference(IdentifierNameSyntax identifier)
    {
        if (!MemberNames.Contains(identifier.Identifier.Text))
            return false;

        // `Name: value` ở đối số có tên không phải một lần đọc gauge.
        if (identifier.Parent is NameColonSyntax)
            return false;

        // `x.OutboxPending`: chỉ tính khi vế trái kết thúc bằng `CoreMetrics` — `thing.OutboxPending` của kiểu khác thì không.
        if (identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier)
            return LastIdentifierOf(access.Expression) == MetricsTypeName;

        // `using static CoreMetrics;` rồi `OutboxPending` đứng trần (xem điểm mù cuối chú thích đầu tệp).
        return true;
    }

    private static string? LastIdentifierOf(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        MemberAccessExpressionSyntax access => access.Name.Identifier.Text,
        _ => null,
    };

    // Chuỗi nội suy KHÔNG có chỗ trống `{…}` thì là một hằng — trả nội dung. Có chỗ trống thì không biết giá trị lúc chạy.
    private static string? HolelessText(InterpolatedStringExpressionSyntax interpolated)
        => interpolated.Contents.All(part => part is InterpolatedStringTextSyntax)
            ? string.Concat(interpolated.Contents.Cast<InterpolatedStringTextSyntax>().Select(part => part.TextToken.ValueText))
            : null;

    // Kiểu NGOÀI CÙNG chứa chỗ đọc — thứ người đọc thông điệp lỗi cần để đi sửa.
    private static string OuterTypeName(SyntaxNode node)
        => node.Ancestors().OfType<TypeDeclarationSyntax>().LastOrDefault()?.Identifier.Text
           ?? "<không nằm trong kiểu nào>";
}
