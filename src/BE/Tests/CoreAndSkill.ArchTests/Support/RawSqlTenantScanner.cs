using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Luật M6 (docs/RULES.md §9, văn bản gốc docs/wiki-core/be/17-multi-tenant.md §8): truy vấn SQL thô trên bảng có
// `tenant_id` phải TỰ thêm điều kiện đơn vị, hoặc nằm trong allowlist khai tường minh kèm lý do.
//
// Bộ lọc toàn cục là cơ chế của EF Core gắn vào cây biểu thức LINQ; một chuỗi SQL viết tay chạm thẳng bảng chứa dữ
// liệu của MỌI đơn vị. Chỗ rò không gây lỗi, không gây ngoại lệ — truy vấn chạy bình thường, chỉ trả về nhiều hơn
// đáng ra được thấy.
//
// BA NGUỒN DỮ LIỆU, KHÔNG HẰNG HOÁ CÁI NÀO:
//   • tập bảng có đơn vị     — đọc từ DDL dưới database/scripts/core/ (ADR-0071 điểm 3);
//   • tập lời gọi SQL thô     — đọc từ AST của source sản phẩm;
//   • tập được miễn trừ       — allowlist khai trong RawSqlTenantFilterTests, không khai ở đây và không khai trong docs/.
//
// ĐIỂM MÙ ĐÃ BIẾT — nói ra để không ai đọc cổng này rộng hơn thực tế:
//   1. Cổng đọc VĂN BẢN, không đọc NGỮ NGHĨA. Nó bắt được câu thiếu hẳn mệnh đề `tenant_id`; nó KHÔNG bắt được
//      `WHERE tenant_id = <đơn vị sai>`. Giới hạn này đã khai ở §8 văn bản gốc và ADR-0071 không thu hẹp nó.
//   2. Câu SQL ghép từ nhiều mảnh LÚC CHẠY nằm ngoài tầm mọi bộ đọc văn bản tĩnh. Chuỗi mà bộ đọc KHÔNG hiểu thì
//      bị trả về với `Sql == null` và cổng làm ĐỎ — không `continue` yên lặng (cùng chiều với B7).
//   3. Tập bảng đọc từ DDL. Một bảng mới có `tenant_id` mà script chưa về thì cổng không biết bảng đó cần lọc, và
//      một câu SQL trên bảng ấy đi lọt — im lặng. Chỗ này chỉ lộ ra khi có người so hai nguồn (ADR-0071, hệ quả tiêu cực).
//   4. Phép nhận diện bảng chỉ xét VỊ TRÍ BẢNG — từ đứng ngay sau FROM / JOIN / INTO / UPDATE / TABLE / ONLY, kể cả
//      danh sách ngăn bằng dấu phẩy. Bài học docs/audit/2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md —
//      neo vào VAI của chuỗi, không neo vào vùng chứa nó. Quét cả văn bản sẽ bắt nhầm `'core.permission'` khi nó
//      là GIÁ TRỊ trong câu seed chứ không phải bảng.
//   5. Mệnh đề đơn vị phải là VỊ TỪ, không phải phép chiếu: `tenant_id` bám một toán tử so sánh. `j.tenant_id AS
//      "TenantId"` trong danh sách SELECT KHÔNG tính — đúng hình dạng của `JobRecoveryHostedService.CandidateSql`,
//      nên một phép dò tìm chữ `tenant_id` ở bất kỳ đâu sẽ cho ca đó đi lọt và mục allowlist của nó thành mục ruỗng.
//   6. Chú thích C# là trivia của Roslyn, không phải lời gọi — SQL viết trong chú thích không bao giờ vào tầm. SQL
//      nằm trong CHUỖI KÝ TỰ của chính tệp cổng cũng không vào tầm, vì bộ dò chỉ xét ĐỐI SỐ của một lời gọi thật.
internal static class RawSqlTenantScanner
{
    // Tên phương thức mang câu SQL ở đối số đầu. `ExecuteSql*` gom mọi hậu tố (Raw / Interpolated / Async).
    // `Sql` chỉ tính khi bên nhận là migrationBuilder — `.Sql` là tên quá phổ thông để bắt vô điều kiện.
    private static readonly string[] ExactNames =
    [
        "FromSql", "FromSqlRaw", "FromSqlInterpolated",
        "SqlQuery", "SqlQueryRaw",
    ];

    private const string MigrationBuilderMethod = "Sql";

    // Một lời gọi SQL thô đọc được từ source.
    //   Symbol — tên hằng mang câu SQL nếu đối số là một định danh, ngược lại tên thành viên bao quanh lời gọi.
    //            Allowlist khai theo tên này, nên nó phải là thứ người đi sửa tìm thấy trong tệp.
    //   Sql    — null nghĩa là bộ đọc KHÔNG hiểu đối số; đó là FAIL, không phải bỏ qua.
    internal sealed record RawSqlCall(string FilePath, string Method, string Symbol, string? Sql, int Line);

    public static IReadOnlyList<RawSqlCall> Calls(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        return [.. root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => (Call: call, Name: MethodName(call)))
            .Where(x => x.Name is not null && x.Call.ArgumentList.Arguments.Count > 0)
            .Select(x =>
            {
                var argument = x.Call.ArgumentList.Arguments[0].Expression;
                return new RawSqlCall(
                    filePath,
                    x.Name!,
                    SymbolOf(argument, x.Call),
                    ResolveText(argument, root),
                    x.Call.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
            })];
    }

    // Bảng có cột `tenant_id`, đọc từ DDL. Trả về tên đầy đủ (`core.job`); so khớp thì so cả dạng trần.
    public static IReadOnlyList<string> TenantTables(string ddlDirectory)
    {
        var tables = new List<string>();

        foreach (var file in Directory.EnumerateFiles(ddlDirectory, "*.sql").OrderBy(f => f, StringComparer.Ordinal))
        {
            string? table = null;
            var hasTenantColumn = false;

            foreach (var raw in File.ReadLines(file))
            {
                var line = raw.Trim();

                var createAt = line.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase);
                if (createAt >= 0)
                {
                    var rest = line[(createAt + "CREATE TABLE".Length)..].TrimStart();
                    if (rest.StartsWith("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase))
                        rest = rest["IF NOT EXISTS".Length..].TrimStart();

                    var end = rest.IndexOfAny([' ', '(']);
                    table = (end < 0 ? rest : rest[..end]).Trim();
                    hasTenantColumn = false;
                    continue;
                }

                if (table is null)
                    continue;

                // Cột PHẢI tên đúng `tenant_id` — `actor_tenant_id` của core.audit_log không tính.
                if (line.StartsWith("tenant_id", StringComparison.OrdinalIgnoreCase)
                    && line.Length > "tenant_id".Length
                    && char.IsWhiteSpace(line["tenant_id".Length]))
                {
                    hasTenantColumn = true;
                }

                if (line.StartsWith(");", StringComparison.Ordinal))
                {
                    if (hasTenantColumn)
                        tables.Add(table.ToLowerInvariant());

                    table = null;
                }
            }
        }

        return [.. tables.Distinct(StringComparer.Ordinal).OrderBy(t => t, StringComparer.Ordinal)];
    }

    // Bảng mà câu SQL chạm tới, nhận diện theo VỊ TRÍ BẢNG (điểm mù 4).
    public static IReadOnlyList<string> TableReferences(string sql)
    {
        string[] introducers = ["FROM", "JOIN", "INTO", "UPDATE", "TABLE", "ONLY"];

        var tokens = Tokenize(sql);
        var found = new List<string>();

        for (var i = 0; i < tokens.Count; i++)
        {
            if (!introducers.Contains(tokens[i], StringComparer.OrdinalIgnoreCase))
                continue;

            // `FOR UPDATE [SKIP LOCKED]` là mệnh đề KHOÁ DÒNG, không phải câu `UPDATE <bảng>` — từ sau nó không
            // phải tên bảng. Hình dạng có thật ở OutboxDispatcher.
            if (string.Equals(tokens[i], "UPDATE", StringComparison.OrdinalIgnoreCase)
                && i > 0 && string.Equals(tokens[i - 1], "FOR", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Danh sách ngăn bằng dấu phẩy: `FROM a, b, c`.
            var j = i + 1;
            while (j < tokens.Count)
            {
                var candidate = tokens[j].Trim('"');
                if (candidate.Length == 0 || !IsIdentifierLike(candidate))
                    break;

                found.Add(candidate.ToLowerInvariant());

                if (j + 1 < tokens.Count && tokens[j + 1] == ",")
                    j += 2;
                else
                    break;
            }
        }

        return [.. found.Distinct(StringComparer.Ordinal).OrderBy(t => t, StringComparer.Ordinal)];
    }

    // Bảng có đơn vị mà câu SQL chạm tới. So cả dạng đủ lược đồ lẫn dạng trần — `FROM job` và `FROM core.job` là một.
    public static IReadOnlyList<string> TenantTablesTouched(string sql, IReadOnlyList<string> tenantTables)
    {
        var touched = TableReferences(sql);

        return [.. tenantTables
            .Where(t => touched.Any(r => string.Equals(r, t, StringComparison.Ordinal)
                                      || string.Equals(r, Bare(t), StringComparison.Ordinal)))
            .OrderBy(t => t, StringComparer.Ordinal)];
    }

    // `tenant_id` ở vị trí VỊ TỪ (điểm mù 5): bám một toán tử so sánh, ở một trong hai phía.
    public static bool HasTenantPredicate(string sql)
    {
        string[] operators = ["=", "<>", "!=", "<", ">", "<=", ">=", "IN", "IS", "ANY"];

        var tokens = Tokenize(sql);

        for (var i = 0; i < tokens.Count; i++)
        {
            if (!IsTenantColumn(tokens[i]))
                continue;

            if (i + 1 < tokens.Count && operators.Contains(tokens[i + 1], StringComparer.OrdinalIgnoreCase))
                return true;

            if (i > 0 && operators.Contains(tokens[i - 1], StringComparer.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // ---- nội bộ ---------------------------------------------------------------------------------

    private static string Bare(string table)
    {
        var dot = table.LastIndexOf('.');
        return dot < 0 ? table : table[(dot + 1)..];
    }

    // `tenant_id`, `j.tenant_id`, `"tenant_id"` — nhưng KHÔNG `actor_tenant_id`.
    private static bool IsTenantColumn(string token)
    {
        var name = token.Trim('"');
        var dot = name.LastIndexOf('.');
        if (dot >= 0)
            name = name[(dot + 1)..].Trim('"');

        return string.Equals(name, "tenant_id", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIdentifierLike(string token)
        => token.Length > 0 && (char.IsLetter(token[0]) || token[0] == '_') && !IsSqlKeyword(token);

    // Từ khoá đứng ngay sau một từ dẫn bảng nghĩa là KHÔNG có tên bảng ở đó (`DELETE FROM ONLY x`, `INSERT INTO SELECT`).
    private static bool IsSqlKeyword(string token)
    {
        string[] keywords = ["select", "only", "lateral", "values", "set", "where", "as", "on", "using", "distinct"];
        return keywords.Contains(token, StringComparer.OrdinalIgnoreCase);
    }

    // Tách SQL thành token thô. Chuỗi nháy đơn gom thành MỘT token — để `'pending'` không bị đọc thành tên bảng.
    private static List<string> Tokenize(string sql)
    {
        var tokens = new List<string>();
        var i = 0;

        while (i < sql.Length)
        {
            var c = sql[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (c == '\'')
            {
                var start = i++;
                while (i < sql.Length && sql[i] != '\'') i++;
                if (i < sql.Length) i++;
                tokens.Add(sql[start..i]);
                continue;
            }

            if (char.IsLetter(c) || c == '_' || c == '"')
            {
                var start = i;
                while (i < sql.Length
                       && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_' || sql[i] == '.' || sql[i] == '"'))
                    i++;
                tokens.Add(sql[start..i]);
                continue;
            }

            if (char.IsDigit(c))
            {
                var start = i;
                while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '.')) i++;
                tokens.Add(sql[start..i]);
                continue;
            }

            // Toán tử hai ký tự trước toán tử một ký tự.
            if (i + 1 < sql.Length && (sql[i..(i + 2)] is "<>" or "!=" or "<=" or ">=" or "->" or "::"))
            {
                tokens.Add(sql[i..(i + 2)]);
                i += 2;
                continue;
            }

            tokens.Add(sql[i].ToString());
            i++;
        }

        return tokens;
    }

    private static string? MethodName(InvocationExpressionSyntax call)
    {
        var name = call.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null,
        };

        if (name is null)
            return null;

        if (ExactNames.Contains(name, StringComparer.Ordinal))
            return name;

        if (name.StartsWith("ExecuteSql", StringComparison.Ordinal))
            return name;

        if (name == MigrationBuilderMethod
            && call.Expression is MemberAccessExpressionSyntax { Expression: var receiver }
            && receiver.ToString().Contains("igrationBuilder", StringComparison.Ordinal))
        {
            return "migrationBuilder.Sql";
        }

        return null;
    }

    // Tên mà allowlist khai theo: hằng mang câu SQL nếu có, ngược lại thành viên bao quanh lời gọi.
    private static string SymbolOf(ExpressionSyntax argument, SyntaxNode call)
    {
        if (argument is IdentifierNameSyntax identifier)
            return identifier.Identifier.Text;

        if (argument is MemberAccessExpressionSyntax member)
            return member.Name.Identifier.Text;

        for (var node = call.Parent; node is not null; node = node.Parent)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method: return method.Identifier.Text;
                case PropertyDeclarationSyntax property: return property.Identifier.Text;
                case ConstructorDeclarationSyntax constructor: return constructor.Identifier.Text;
                case VariableDeclaratorSyntax declarator: return declarator.Identifier.Text;
            }
        }

        return "<không rõ>";
    }

    // Gỡ câu SQL ra khỏi đối số. Hằng gán ở chỗ khác rồi truyền vào — đúng hình dạng của `CandidateSql` — phải
    // đọc được, nếu không thì phép dò bỏ sót đúng ca đang có thật (ADR-0071 điểm 2).
    private static string? ResolveText(ExpressionSyntax expression, CompilationUnitSyntax root, int depth = 0)
    {
        if (depth > 8)
            return null;

        switch (expression)
        {
            case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                return literal.Token.ValueText;

            case InterpolatedStringExpressionSyntax interpolated:
                // Lỗ nội suy thay bằng một chỗ giữ: giá trị lúc chạy không đọc được, nhưng KHUNG câu thì đọc được —
                // và khung mới là thứ nói câu này chạm bảng nào.
                return string.Concat(interpolated.Contents.Select(part => part switch
                {
                    InterpolatedStringTextSyntax text => text.TextToken.ValueText,
                    _ => " @p ",
                }));

            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression):
                {
                    var left = ResolveText(binary.Left, root, depth + 1);
                    var right = ResolveText(binary.Right, root, depth + 1);
                    return left is null || right is null ? null : left + right;
                }

            case ParenthesizedExpressionSyntax parenthesized:
                return ResolveText(parenthesized.Expression, root, depth + 1);

            case IdentifierNameSyntax identifier:
                return ResolveDeclared(identifier.Identifier.Text, root, depth);

            case MemberAccessExpressionSyntax member:
                return ResolveDeclared(member.Name.Identifier.Text, root, depth);

            default:
                return null;
        }
    }

    private static string? ResolveDeclared(string name, CompilationUnitSyntax root, int depth)
    {
        var declarator = root.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault(d => d.Identifier.Text == name && d.Initializer is not null);

        return declarator?.Initializer is null ? null : ResolveText(declarator.Initializer.Value, root, depth + 1);
    }
}
