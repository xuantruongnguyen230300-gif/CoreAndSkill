using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

public enum EnterUseKind
{
    // Lời gọi `Enter(a, b, c)` với đúng ba đối số.
    Call,

    // `x.Enter` hay `x?.Enter` không phải callee của lời gọi nào — method group, đem đi gọi ở chỗ khác.
    MethodGroup,
}

// Một chỗ dùng `Enter`. `Receiver` là văn bản biểu thức nhận (rỗng với `Enter(` trần), chỉ để chốt T6 và thông báo —
// KHÔNG dùng để khoá phán quyết.
internal sealed record EnterUse(
    string Type, string Member, string FilePath, int Line, EnterUseKind Kind, string Receiver, bool InConditional);

internal sealed record TypeDeclarationKey(string Type, string FilePath, int Line);

// Luật A12 (docs/RULES-BE.md §3): chỉ nơi gọi thuộc allowlist ở docs/quy-uoc/be-architecture.md, đoạn mốc
// `**Allowlist A12 —`, được mở phạm vi ngữ cảnh thực thi (`IExecutionContextScope.Enter`).
//
// Quét cú pháp Roslyn, không dựng Compilation. Nhận ra:
//   (H1) lời gọi có ĐÚNG 3 đối số, callee tên đơn `Enter`, dạng `a.Enter`, `this.Enter`, `base.Enter`, `a?.Enter`, `Enter(`;
//   (H2) `a.Enter` / `a?.Enter` không phải callee của lời gọi nào (method group).
// Không tính: khai báo phương thức `Enter`, chú thích, chuỗi, lời gọi `Enter` có số đối số khác 3.
// KHÔNG miễn trừ theo tệp và KHÔNG khoá theo tên biến nhận: một nơi gọi thật có biến nhận tên `scopeFactory`.
//
// Khoá allowlist = (kiểu gần nhất, thành viên gần nhất). Kiểu lồng ghi `Ngoài.Trong`. Thành viên: phương thức lấy tên,
// constructor `.ctor`, accessor quy về property, field lấy tên biến đầu tiên; lambda và hàm cục bộ tính cho thành viên
// bao ngoài. Câu lệnh top-level thuộc kiểu `<top-level>` nên luôn là vi phạm.
//
// ĐIỂM MÙ (khai, không giấu):
//   1. Phương thức bọc trả `IDisposable` của `Enter` được mở rộng tầm truy cập: lời gọi bọc nằm ở một thành viên khác,
//      không tên `Enter` nên không bị bắt. `TenantProvisioningService.EnterTenantScope` chính là một wrapper như vậy
//      nhưng nằm trong allowlist; một wrapper mới `internal`/`public` thì cổng không thấy ai gọi nó.
//   2. Reflection hoặc `dynamic` (`GetMethod("Enter")`, `((dynamic)s).Enter(...)` có 3 đối số thì bị bắt, nhưng gọi qua
//      chuỗi tên thì không).
//   3. Nhánh `#if` bị tắt: Roslyn không sinh nút cho văn bản bị loại, cổng không đọc nó.
//   4. Mã ngoài `ProductSourceFiles.Core()` (module dự án ngoài năm project Core và host).
//   5. API khác mang tên `Enter` nhận 3 đối số bị bắt NHẦM. Đây là chiều an toàn: xử lý bằng đổi tên hoặc review,
//      KHÔNG thêm vào allowlist.
//   6. (bổ sung khi viết) `Enter` trần làm method group (`Func<…> f = Enter;`) không có `.` đứng trước, không thuộc H2.
internal static class ExecutionContextScopeEntryScanner
{
    public const string Marker = "**Allowlist A12 —";
    public const string TypeColumn = "Kiểu";
    public const string MemberColumn = "Thành viên";

    public const string TopLevelType = "<top-level>";

    private const string EnterName = "Enter";
    private const int EnterArity = 3;

    // Allowlist của cổng — đúng sáu dòng của bảng tài liệu; `A12Allowlist_MatchesTheDocumentTable_BothWays` giữ hai
    // bản không trôi khỏi nhau. Nới nó là nới một luật MUST NOT: buộc một lượt core-reviewer.
    public static readonly IReadOnlySet<(string Type, string Member)> Allowlist = new HashSet<(string, string)>
    {
        ("BackgroundJobQueueHostedService", "RunAsync"),
        ("JobRecoveryHostedService", "RecoverAsync"),
        ("OutboxDispatcher", "DispatchOneAsync"),
        ("TenantProvisioningService", "EnterTenantScope"),
        ("LoginCommandHandler", "Handle"),
        ("CoreWebServiceCollectionExtensions", "ValidatePrincipalAsync"),
    };

    // ===== Quét =====

    public static IReadOnlyList<EnterUse> Uses(string source, string filePath)
    {
        ArgumentNullException.ThrowIfNull(source);

        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var found = new List<EnterUse>();

        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation
                    when invocation.ArgumentList.Arguments.Count == EnterArity
                      && CalleeIsEnter(invocation.Expression, out var receiver):
                    found.Add(Make(invocation, EnterUseKind.Call, receiver, filePath));
                    break;

                case MemberAccessExpressionSyntax access
                    when access.Name.Identifier.Text == EnterName && !IsCallee(access):
                    found.Add(Make(access, EnterUseKind.MethodGroup, access.Expression.ToString(), filePath));
                    break;

                case MemberBindingExpressionSyntax binding
                    when binding.Name.Identifier.Text == EnterName && !IsCallee(binding):
                    found.Add(Make(binding, EnterUseKind.MethodGroup, string.Empty, filePath));
                    break;
            }
        }

        return found;
    }

    public static IReadOnlyList<TypeDeclarationKey> TypeDeclarations(string source, string filePath)
    {
        ArgumentNullException.ThrowIfNull(source);

        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        return [.. root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Select(t => new TypeDeclarationKey(
                TypeKey(t), filePath, t.GetLocation().GetLineSpan().StartLinePosition.Line + 1))];
    }

    // ===== Phán quyết =====

    public static IReadOnlyList<string> Violations(
        IEnumerable<EnterUse> uses, IReadOnlySet<(string Type, string Member)> allowlist)
        => [.. uses
            .Where(u => !allowlist.Contains((u.Type, u.Member)))
            .Select(u => $"{u.FilePath}:{u.Line} — {u.Type}.{u.Member} dùng `Enter` "
                       + $"({(u.Kind == EnterUseKind.Call ? "lời gọi" : "method group")}, biến nhận '{u.Receiver}') "
                       + "mà không thuộc allowlist A12")];

    // Cặp allowlist không còn lời gọi thật nào — mục chết là mục nới luật vô cớ.
    public static IReadOnlyList<string> DeadEntries(
        IEnumerable<EnterUse> uses, IReadOnlySet<(string Type, string Member)> allowlist)
    {
        var called = uses.Where(u => u.Kind == EnterUseKind.Call).Select(u => (u.Type, u.Member)).ToHashSet();

        return [.. allowlist.Where(pair => !called.Contains(pair))
            .OrderBy(p => p.Type, StringComparer.Ordinal).ThenBy(p => p.Member, StringComparer.Ordinal)
            .Select(p => $"{p.Type}.{p.Member} nằm trong allowlist nhưng không có lời gọi `Enter` thật nào")];
    }

    // Mỗi tên kiểu của allowlist phải được khai ĐÚNG MỘT lần: khai hai lần thì cặp (kiểu, thành viên) thôi định danh
    // duy nhất một nơi, và một bản trùng tên ở chỗ khác thừa hưởng quyền gọi.
    public static IReadOnlyList<string> TypeDeclarationProblems(
        IEnumerable<TypeDeclarationKey> declarations, IReadOnlySet<(string Type, string Member)> allowlist)
    {
        var all = declarations.ToList();

        return [.. allowlist.Select(p => p.Type).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Select(name => (Name: name, Sites: all.Where(d => d.Type == name).ToList()))
            .Where(x => x.Sites.Count != 1)
            .Select(x => $"Kiểu '{x.Name}' của allowlist được khai {x.Sites.Count} lần"
                       + (x.Sites.Count == 0
                           ? " (mất hoặc đổi tên)"
                           : $": {string.Join("; ", x.Sites.Select(s => $"{s.FilePath}:{s.Line}"))}"))];
    }

    // ===== Bảng tài liệu =====

    public static IReadOnlyList<(string Type, string Member)> ReadDocTable(string markdownPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markdownPath);

        if (!File.Exists(markdownPath))
            throw new InvalidOperationException(
                $"Không thấy tài liệu '{markdownPath}' — cổng A12 không đối chiếu được với bảng allowlist.");

        return ParseDocTable(File.ReadAllText(markdownPath), markdownPath);
    }

    public static IReadOnlyList<(string Type, string Member)> ParseDocTable(string markdown, string origin)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var markerIndex = Array.FindIndex(lines, l => l.Contains(Marker, StringComparison.Ordinal));

        if (markerIndex < 0)
            throw new InvalidOperationException(
                $"'{origin}' không còn mốc '{Marker}' — bảng allowlist đã đổi tên hoặc dời chỗ, cổng A12 đang so với "
              + "một tập rỗng. Sửa mốc ở cả hai phía, KHÔNG bỏ qua.");

        var tableStart = Array.FindIndex(lines, markerIndex + 1, l => l.TrimStart().StartsWith('|'));

        if (tableStart < 0)
            throw new InvalidOperationException($"'{origin}' có mốc '{Marker}' nhưng không có bảng nào sau nó.");

        var header = SplitRow(lines[tableStart]);

        int Column(string name)
        {
            var index = header.FindIndex(c => string.Equals(c, name, StringComparison.Ordinal));

            return index >= 0
                ? index
                : throw new InvalidOperationException(
                    $"Bảng allowlist ở '{origin}' không có cột '{name}' — cột đã bị đổi tên. "
                  + $"Cột đang có: {string.Join(", ", header)}.");
        }

        var typeColumn = Column(TypeColumn);
        var memberColumn = Column(MemberColumn);
        var rows = new List<(string, string)>();

        for (var i = tableStart + 2; i < lines.Length && lines[i].TrimStart().StartsWith('|'); i++)
        {
            var cells = SplitRow(lines[i]);

            if (cells.Count <= Math.Max(typeColumn, memberColumn))
                throw new InvalidOperationException(
                    $"'{origin}' dòng {i + 1}: bảng allowlist có {cells.Count} ô, ít hơn số cột đã khai.");

            rows.Add((cells[typeColumn], cells[memberColumn]));
        }

        if (rows.Count == 0)
            throw new InvalidOperationException(
                $"'{origin}' có bảng allowlist nhưng KHÔNG đọc được dòng nào — cổng A12 đang so rỗng với rỗng.");

        return rows;
    }

    // Hai chiều, báo riêng.
    public static IReadOnlyList<string> CompareToDocument(
        IEnumerable<(string Type, string Member)> documented, IEnumerable<(string Type, string Member)> gate)
    {
        var doc = documented.ToList();
        var code = gate.ToList();
        var problems = new List<string>();

        problems.AddRange(doc.GroupBy(p => p).Where(g => g.Count() > 1)
            .Select(g => $"Bảng tài liệu có dòng trùng: {g.Key.Type}.{g.Key.Member}"));

        problems.AddRange(doc.Distinct().Except(code)
            .Select(p => $"Chỉ có ở TÀI LIỆU, thiếu ở cổng: {p.Type}.{p.Member}"));

        problems.AddRange(code.Distinct().Except(doc)
            .Select(p => $"Chỉ có ở CỔNG, thiếu ở tài liệu: {p.Type}.{p.Member}"));

        return problems;
    }

    // ===== Chi tiết cú pháp =====

    private static bool CalleeIsEnter(ExpressionSyntax callee, out string receiver)
    {
        switch (callee)
        {
            case MemberAccessExpressionSyntax access when access.Name.Identifier.Text == EnterName:
                receiver = access.Expression.ToString();
                return true;

            case MemberBindingExpressionSyntax binding when binding.Name.Identifier.Text == EnterName:
                receiver = string.Empty;
                return true;

            case IdentifierNameSyntax identifier when identifier.Identifier.Text == EnterName:
                receiver = string.Empty;
                return true;

            case GenericNameSyntax generic when generic.Identifier.Text == EnterName:
                receiver = string.Empty;
                return true;

            default:
                receiver = string.Empty;
                return false;
        }
    }

    private static bool IsCallee(ExpressionSyntax node)
        => node.Parent is InvocationExpressionSyntax invocation && invocation.Expression == node;

    private static EnterUse Make(SyntaxNode node, EnterUseKind kind, string receiver, string filePath)
    {
        var (type, member) = Owner(node);
        var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var inConditional = node.Ancestors().TakeWhile(a => a is not MemberDeclarationSyntax)
            .OfType<ConditionalExpressionSyntax>().Any();

        return new EnterUse(type, member, filePath, line, kind, receiver, inConditional);
    }

    private static (string Type, string Member) Owner(SyntaxNode node)
    {
        string? member = null;

        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is TypeDeclarationSyntax type)
                return (TypeKey(type), member ?? ".ctor"); // primary constructor / danh sách base

            if (member is null && ancestor is MemberDeclarationSyntax declaration)
            {
                if (declaration is GlobalStatementSyntax)
                    return (TopLevelType, "<global>");

                member = MemberName(declaration);
            }
        }

        return (TopLevelType, member ?? "<global>");
    }

    private static string TypeKey(TypeDeclarationSyntax type)
        => string.Join(".", type.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text));

    private static string MemberName(MemberDeclarationSyntax member) => member switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        ConstructorDeclarationSyntax => ".ctor",
        DestructorDeclarationSyntax => ".dtor",
        PropertyDeclarationSyntax p => p.Identifier.Text,
        IndexerDeclarationSyntax => "this[]",
        EventDeclarationSyntax e => e.Identifier.Text,
        EventFieldDeclarationSyntax e => e.Declaration.Variables.First().Identifier.Text,
        FieldDeclarationSyntax f => f.Declaration.Variables.First().Identifier.Text,
        OperatorDeclarationSyntax o => "operator " + o.OperatorToken.Text,
        ConversionOperatorDeclarationSyntax => "operator <conversion>",
        _ => member.Kind().ToString(),
    };

    private static List<string> SplitRow(string line)
    {
        var trimmed = line.Trim();

        if (trimmed.StartsWith('|'))
            trimmed = trimmed[1..];

        if (trimmed.EndsWith('|'))
            trimmed = trimmed[..^1];

        return [.. trimmed.Split('|').Select(c => c.Trim().Trim('`').Trim())];
    }
}
