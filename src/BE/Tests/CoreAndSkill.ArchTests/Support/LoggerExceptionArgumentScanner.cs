using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Luật B28 (docs/RULES-BE.md) — định nghĩa gốc: docs/wiki-core/be/07-observability.md §5 mục "Ghi một ngoại lệ vào
// log". Người dùng chốt 2026-09-29. Ba vế, MỘT bộ dò cú pháp (Roslyn), dùng chung tầm quét ProductSourceFiles.Core()
// ở nơi gọi — năm project Core cộng host, như mọi cổng quét cú pháp khác (host lọt vì quyết định 8 của
// docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md).
//
// VẾ 1 (ScanLoggerCalls) — Core không trao đối tượng ngoại lệ cho ILogger. Ba điều kiện ĐỘC LẬP, không cái nào
// thay được cái kia:
//   (a) Đối số ở VỊ TRÍ THÔNG ĐIỆP của lời gọi `Log<Mức>(` — đối số TÊN `message:` khi có, không thì đối số đầu
//       không mang tên — phải là một BIỂU THỨC CHUỖI — literal
//       (thường/verbatim/raw/UTF-8), chuỗi nội suy, hay phép nối `+` của các biểu thức chuỗi (đệ quy). Bắt được dạng
//       cổ `LogError(ex, "…", …)` vì `ex` không phải chuỗi, BẤT KỂ `ex` có phải biến catch hay không — một tham số
//       method tên `exception` truyền thẳng vào vị trí này cũng bị bắt (xem CoreExceptionHandler.cs — nó KHÔNG làm
//       vậy, luôn bọc bằng FailureText.*). Riêng lời gọi `.Log(…)` là ĐỎ NGAY, bất kể đối số — mức log viết `LogLevel.*` hay
//       là một biến (`logger.Log(level, ex, "…")`) — đúng cột "Đỏ khi" của docs/wiki-core/be/07-observability.md §5:
//       Core dùng `Log<Mức>(`, không dùng overload tổng quát. Lời gọi qua `?.` (`logger?.LogError(…)`) được nhận như
//       lời gọi qua `.`, ở cả ba vế.
//   (c) Đối số TÊN `exception:` có giá trị khác literal `null` là ĐỎ, bất kể giá trị là gì (`exception: attempt.Exception`,
//       `exception: ex`…) — đối số tên không đứng ở vị trí thông điệp nên (a) không thấy, và `attempt.Exception` không phải
//       biến catch nên (b) không thấy. `exception: null` tường minh KHÔNG bị bắt, cùng khuôn với vế 2.
//   (b) BẤT KỲ đối số nào của lời gọi (không riêng vị trí thông điệp) có CÂY CON chứa một tham chiếu tới biến khai
//       TẠI mệnh đề `catch (… tên)` BAO QUANH lời gọi, HOẶC tới THAM SỐ có kiểu khai báo kết thúc bằng `Exception`
//       của thành viên bao quanh (phương thức, hàm dựng, hàm cục bộ, lambda/anonymous method có kiểu tường minh,
//       primary constructor — mở rộng 2026-10-03, người dùng chốt) thì bị bắt — trơ trọi, `.Message`, `.ToString()`,
//       `.InnerException`, `.GetBaseException()`, `(object)ex`, `ex!`, `ex.GetType().Name`, hay nằm trong một LỖ của
//       chuỗi nội suy. Lý do nới (người dùng chốt 2026-10-03): provider log gọi `ToString()` của giá trị đối số, nên
//       `ex.InnerException` đưa ngăn xếp chưa lọc vào log. NGOẠI LỆ duy nhất: tham chiếu nằm trong đối số của một
//       lời gọi `FailureText.<tên hàm>(…)` (ví dụ `FailureText.DescribeWithStack(ex.InnerException!)`).
//
// VẾ 2 (ScanHealthCheckResultCalls) — Core không trao ngoại lệ cho HealthCheckResult: `HealthCheckResult.Unhealthy(`
// / `.Degraded(` có đối số VỊ TRÍ THỨ HAI (index 1) hay đối số TÊN `exception:` khác literal `null`; `new
// HealthCheckResult(` có đối số VỊ TRÍ THỨ BA (index 2) hay `exception:` khác `null`. `exception: null` tường minh
// KHÔNG bị bắt — đó không phải trao ngoại lệ.
//
// VẾ 3 (ScanWarningOrHigherFailureTextDescribe) — dòng log mức Warning trở lên (`LogWarning(` / `LogError(` /
// `LogCritical(` — dạng `.Log(<mức>, …)` đã đỏ ở vế 1, đúng như cột "Đỏ khi" ở docs/wiki-core/be/07-observability.md §5) không
// được mang một đối số là lời gọi `FailureText.Describe(` — dò trong TOÀN BỘ cây con của mỗi đối số, không chỉ đối
// số đứng trơ trọi, để bắt cả dạng `FailureText.Describe(ex) + " …"`.
//
// ĐIỂM MÙ ĐÃ BIẾT — nói ra để không ai tin cổng bắt được nhiều hơn nó thật sự bắt (docs/wiki-core/be/07-observability.md
// §5 mục "Điểm mù"):
//   1. Ngoại lệ GÁN SANG BIẾN KHÁC rồi mới vào log (`var e2 = ex;`), hay biến mẫu `is XException x`: vế 1(b) chỉ
//      nhận diện tham chiếu tới biến khai TRỰC TIẾP ở mệnh đề `catch (… tên)` hoặc ở danh sách tham số có kiểu
//      tường minh kết thúc bằng `Exception`. Cùng lớp: GIÁ TRỊ của `FailureText.Describe(ex)`
//      gán vào một FIELD/PROPERTY rồi field đó mới vào log (ví dụ GIẢ ĐỊNH — một lớp gán `FailureText.Describe(ex)`
//      vào `attempt.Error` trong một method rồi `logger.LogError("…", attempt.Error)` ở method khác) — vế 1 không xét vì không có biến catch nào bị trao; vế 3 không xét vì đối số tại
//      chỗ log KHÔNG PHẢI cú pháp `FailureText.Describe(…)`, mà là một truy cập thành viên trên biến trung gian.
//      Báo cáo lượt viết cổng này (2026-09-30) đã nêu phát hiện này cho người sửa xem xét — KHÔNG mở rộng cổng để
//      bắt, vì "Đỏ khi" đã chốt ở docs/wiki-core/be/07-observability.md §5 chỉ nói lời gọi TRỰC TIẾP.
//   2. Tham số KHÔNG khai kiểu bằng cú pháp — lambda `ex => …` (kiểu suy ra), hay tham số có kiểu không kết thúc
//      bằng `Exception` (ví dụ `Exception` bị đặt bí danh `using E = System.Exception;`, hay kiểu cơ sở `object`):
//      không biết kiểu nếu không có semantic model nên không theo dõi.
//   3. Log qua `LoggerMessage.Define` hay phương thức sinh từ `[LoggerMessage]` nhận tham số `Exception`: bộ dò chỉ
//      biết `Log<Mức>(` và `.Log(` (kể cả qua `?.`). Core hôm nay không dùng hai cách này.
//   4. Log do FRAMEWORK tự ghi (middleware, bộ chạy health check, EF Core) — ngoài tầm vì cổng chỉ đọc source Core.
//      Nửa (a) của nợ B11.
//   5. Giá trị nhạy cảm gõ thẳng vào template — việc của luật S13 và review, không phải B28.
internal static class LoggerExceptionArgumentScanner
{
    // Một chỗ vi phạm. Reason đủ chữ để người đọc log CI hiểu ngay không cần mở tệp scanner.
    internal sealed record Hit(string FilePath, int Line, string Snippet, string Reason);

    private static readonly string[] LogMethodNames =
        ["LogTrace", "LogDebug", "LogInformation", "LogWarning", "LogError", "LogCritical"];

    private static readonly string[] WarningOrHigherLogMethodNames = ["LogWarning", "LogError", "LogCritical"];

    // ===== Vế 1 =====

    public static IReadOnlyList<Hit> ScanLoggerCalls(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var hits = new List<Hit>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!TryGetLogCall(invocation, out var arguments, out var messageIndex))
                continue;

            // `.Log(…)` — đỏ NGAY (07-observability.md §5), mức log viết `LogLevel.*` hay là biến: Core chỉ dùng `Log<Mức>(`.
            if (messageIndex == 1)
            {
                hits.Add(new Hit(filePath, Line(invocation), Compact(invocation),
                    "lời gọi `.Log(<mức>, …)` — dùng `LogTrace/Debug/Information/Warning/Error/Critical(…)` với thông "
                    + "điệp là biểu thức chuỗi"));
                continue;
            }

            // Template tìm theo TÊN `message:` khi có (đối số tên không nhất thiết đứng đầu: `LogError(exception: null,
            // message: "…")`), không thì đối số đầu KHÔNG mang tên.
            var messageArg = FindMessageArgument(arguments);
            if (messageArg is not null && !IsStringExpression(messageArg))
            {
                hits.Add(new Hit(filePath, Line(invocation), Compact(invocation),
                    $"đối số ở vị trí thông điệp không phải biểu thức chuỗi: `{Compact(messageArg)}` — có thể là "
                    + "đối tượng ngoại lệ trao thẳng cho ILogger"));
            }

            foreach (var argument in arguments)
            {
                if (argument.NameColon is { Name.Identifier.Text: "exception" } && !IsNullLiteral(argument.Expression))
                {
                    hits.Add(new Hit(filePath, Line(invocation), Compact(invocation),
                        $"đối số tên `exception:` trao ngoại lệ cho ILogger: `{Compact(argument.Expression)}` — dùng "
                        + "FailureText.DescribeWithStack(ex) trong tham số của template, không trao đối tượng ngoại lệ"));
                }
            }

            var catchVars = CatchVariableNamesEnclosing(invocation);
            catchVars.UnionWith(ExceptionParameterNamesEnclosing(invocation));
            if (catchVars.Count == 0)
                continue;

            foreach (var argument in arguments)
            {
                if (ContainsCatchVariableReference(argument.Expression, catchVars))
                {
                    hits.Add(new Hit(filePath, Line(invocation), Compact(invocation),
                        $"đối số mang biến ngoại lệ (biến catch hoặc tham số kiểu *Exception) của thành viên bao quanh: `{Compact(argument.Expression)}` — dùng "
                        + "FailureText.DescribeWithStack(ex) thay vì trao thẳng biến/`.Message`/`.ToString()`"));
                }
            }
        }

        return hits;
    }

    private static ExpressionSyntax? FindMessageArgument(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument.NameColon is { Name.Identifier.Text: "message" })
                return argument.Expression;
        }

        return arguments.FirstOrDefault(argument => argument.NameColon is null)?.Expression;
    }

    // Số lời gọi mà cổng NHÌN THẤY là một lời gọi log (không phân biệt vi phạm hay không) — cho T6.
    public static int CountLoggerCallSites(string source)
        => CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Count(invocation => TryGetLogCall(invocation, out _, out _));

    // ===== Vế 2 =====

    public static IReadOnlyList<Hit> ScanHealthCheckResultCalls(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var hits = new List<Hit>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Unhealthy" or "Degraded" } memberAccess
                || !IsHealthCheckResultReceiver(memberAccess.Expression))
                continue;

            if (HasNonNullExceptionArgument(invocation.ArgumentList.Arguments, positionalIndex: 1))
            {
                hits.Add(new Hit(filePath, Line(invocation), Compact(invocation),
                    $"HealthCheckResult.{memberAccess.Name.Identifier.Text} trao ngoại lệ — mô tả bằng "
                    + "FailureText.Describe(ex) trong chuỗi mô tả, không trao đối tượng ngoại lệ"));
            }
        }

        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (creation.Type.ToString().Split('.').Last() != "HealthCheckResult")
                continue;

            var arguments = creation.ArgumentList?.Arguments ?? default;
            if (HasNonNullExceptionArgument(arguments, positionalIndex: 2))
            {
                hits.Add(new Hit(filePath, Line(creation), Compact(creation),
                    "new HealthCheckResult(…) trao ngoại lệ ở đối số exception"));
            }
        }

        return hits;
    }

    // Số lời gọi/khởi tạo mà cổng nhận diện là "một HealthCheckResult" — cho T6.
    public static int CountHealthCheckResultCallSites(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        var invocationCount = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Count(invocation => invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Unhealthy" or "Degraded" } m
                                  && IsHealthCheckResultReceiver(m.Expression));

        var creationCount = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Count(creation => creation.Type.ToString().Split('.').Last() == "HealthCheckResult");

        return invocationCount + creationCount;
    }

    // ===== Vế 3 =====

    public static IReadOnlyList<Hit> ScanWarningOrHigherFailureTextDescribe(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var hits = new List<Hit>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!TryGetInvokedMethodName(invocation, out var name)
                || !WarningOrHigherLogMethodNames.Contains(name, StringComparer.Ordinal))
                continue;

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (ContainsFailureTextDescribeCall(argument.Expression))
                {
                    hits.Add(new Hit(filePath, Line(invocation), Compact(invocation),
                        $"{name} dùng FailureText.Describe — thiếu ngăn xếp; dùng FailureText.DescribeWithStack: "
                        + $"`{Compact(argument.Expression)}`"));
                }
            }
        }

        return hits;
    }

    // ---- nhận diện lời gọi log (dùng chung vế 1 và T6) ------------------------------------------------------

    private static bool TryGetLogCall(InvocationExpressionSyntax invocation, out SeparatedSyntaxList<ArgumentSyntax> arguments, out int messageIndex)
    {
        arguments = default;
        messageIndex = 0;

        if (!TryGetInvokedMethodName(invocation, out var name))
            return false;

        arguments = invocation.ArgumentList.Arguments;

        if (LogMethodNames.Contains(name, StringComparer.Ordinal))
        {
            messageIndex = 0;
            return true;
        }

        // Mọi `.Log(` trên một receiver là hit, bất kể đối số đầu viết thế nào (mức log là biến cũng đỏ) — luật vế 1
        // cấm overload tổng quát. Lời gọi `Log(…)` trần, không receiver, không được xét.
        if (name == "Log" && arguments.Count > 0)
        {
            messageIndex = 1;
            return true;
        }

        return false;
    }

    // Tên phương thức của lời gọi có receiver: `x.Name(…)` (MemberAccess) lẫn `x?.Name(…)` (MemberBinding, nằm trong
    // ConditionalAccess). Lời gọi không receiver — `Name(…)` trần — không có tên để dò.
    private static bool TryGetInvokedMethodName(InvocationExpressionSyntax invocation, out string name)
    {
        switch (invocation.Expression)
        {
            case MemberAccessExpressionSyntax memberAccess:
                name = memberAccess.Name.Identifier.Text;
                return true;
            case MemberBindingExpressionSyntax memberBinding:
                name = memberBinding.Name.Identifier.Text;
                return true;
            default:
                name = "";
                return false;
        }
    }

    // ---- vế 1 — biểu thức chuỗi và biến catch ---------------------------------------------------------------

    private static bool IsStringExpression(ExpressionSyntax expression) => Unwrap(expression) switch
    {
        LiteralExpressionSyntax literal
            when literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.Utf8StringLiteralExpression)
            => true,
        InterpolatedStringExpressionSyntax => true,
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression)
            => IsStringExpression(binary.Left) && IsStringExpression(binary.Right),
        _ => false,
    };

    // Tên biến khai ở MỌI mệnh đề `catch (… tên)` bao quanh node — bao gồm cả catch lồng nhau. Chỉ đọc từ
    // CatchClauseSyntax.Declaration, không suy luận theo tên biến ở nơi khác (đó là ranh giới đã khai ở đầu tệp).
    private static HashSet<string> CatchVariableNamesEnclosing(SyntaxNode node)
        => [.. node.Ancestors()
                 .OfType<CatchClauseSyntax>()
                 .Select(c => c.Declaration?.Identifier.Text)
                 .Where(name => !string.IsNullOrEmpty(name))
                 .Select(name => name!)];

    // Tên THAM SỐ có kiểu khai báo mà định danh cuối kết thúc bằng `Exception` (`Exception`, `Exception?`,
    // `System.Exception`, `DbUpdateException`) của MỌI thành viên bao quanh node: phương thức, hàm dựng, toán tử,
    // hàm cục bộ, lambda có kiểu tường minh, anonymous method, primary constructor của lớp/struct/record. Tầm theo
    // dõi = thân của chính thành viên đó (node nằm trong thành viên thì mới được tính). Lambda KHÔNG khai kiểu
    // (`ex => …`) không biết kiểu bằng cú pháp — điểm mù (xem đầu tệp, mục 2).
    private static HashSet<string> ExceptionParameterNamesEnclosing(SyntaxNode node)
    {
        var names = new HashSet<string>();
        foreach (var ancestor in node.Ancestors())
        {
            var parameters = ancestor switch
            {
                BaseMethodDeclarationSyntax method => method.ParameterList.Parameters,
                LocalFunctionStatementSyntax local => local.ParameterList.Parameters,
                ParenthesizedLambdaExpressionSyntax lambda => lambda.ParameterList.Parameters,
                AnonymousMethodExpressionSyntax anonymous => anonymous.ParameterList?.Parameters ?? default,
                TypeDeclarationSyntax { ParameterList: { } primary } => primary.Parameters,
                _ => default,
            };

            foreach (var parameter in parameters)
            {
                if (parameter.Type is not null && IsExceptionTypeName(parameter.Type))
                    names.Add(parameter.Identifier.Text);
            }
        }

        return names;
    }

    private static bool IsExceptionTypeName(TypeSyntax type)
    {
        while (type is NullableTypeSyntax nullable)
            type = nullable.ElementType;

        var last = type switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
            _ => null,
        };

        return last is not null && last.EndsWith("Exception", StringComparison.Ordinal);
    }

    // Cây con của đối số có CHỨA một tham chiếu tới biến catch (mọi dạng: trơ trọi, `.Message`, `.InnerException`,
    // `.GetBaseException()`, `(object)ex`, `ex!`, `ex.GetType().Name`, lỗ `{…}` của chuỗi nội suy…) — trừ khi tham
    // chiếu nằm trong ARGUMENT LIST của một lời gọi `FailureText.<tên hàm>(…)`. Chỉ tính IdentifierName đứng ở vị
    // trí giá trị: tên đứng sau dấu chấm (`x.ex`) hay sau `?.` (`x?.ex`) là thành viên, không phải biến catch.
    private static bool ContainsCatchVariableReference(ExpressionSyntax expression, HashSet<string> catchVars)
        => expression.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => catchVars.Contains(identifier.Identifier.Text)
                               && !IsMemberNameAfterDot(identifier)
                               && !IsInsideFailureTextCall(identifier, expression));

    private static bool IsMemberNameAfterDot(IdentifierNameSyntax identifier)
        => identifier.Parent switch
        {
            MemberAccessExpressionSyntax member => member.Name == identifier,
            // `obj?.ex` — phần sau `?.` là MemberBindingExpression, không phải MemberAccess; cũng là thành viên.
            MemberBindingExpressionSyntax binding => binding.Name == identifier,
            _ => false,
        };

    // Đi từ tham chiếu lên tới gốc đối số: gặp lời gọi `FailureText.*(…)` mà tham chiếu nằm trong ArgumentList của nó.
    private static bool IsInsideFailureTextCall(SyntaxNode reference, ExpressionSyntax argumentRoot)
    {
        for (var node = reference.Parent; node is not null; node = node.Parent)
        {
            if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation
                && EndsWithIdentifier(member.Expression, "FailureText")
                && invocation.ArgumentList.Span.Contains(reference.Span))
                return true;

            if (node == argumentRoot)
                break;
        }

        return false;
    }

    // ---- vế 2 — HealthCheckResult ----------------------------------------------------------------------------

    // Chấp nhận cả tên đơn (`HealthCheckResult.Unhealthy(…)`, dạng dùng `using` — hình dạng thật trong Core) lẫn tên
    // đủ (`Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(…)`) — chỉ xét ĐOẠN CUỐI.
    private static bool IsHealthCheckResultReceiver(ExpressionSyntax expression)
        => EndsWithIdentifier(expression, "HealthCheckResult");

    private static bool HasNonNullExceptionArgument(SeparatedSyntaxList<ArgumentSyntax> arguments, int positionalIndex)
    {
        var index = 0;
        foreach (var argument in arguments)
        {
            var isExceptionSlot = argument.NameColon is { Name.Identifier.Text: "exception" }
                || (argument.NameColon is null && index == positionalIndex);

            if (isExceptionSlot && !IsNullLiteral(argument.Expression))
                return true;

            index++;
        }

        return false;
    }

    private static bool IsNullLiteral(ExpressionSyntax expression)
        => Unwrap(expression).IsKind(SyntaxKind.NullLiteralExpression);

    // ---- vế 3 — FailureText.Describe -------------------------------------------------------------------------

    private static bool ContainsFailureTextDescribeCall(ExpressionSyntax expression)
        => expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(IsFailureTextDescribeInvocation);

    private static bool IsFailureTextDescribeInvocation(InvocationExpressionSyntax invocation)
        => invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Describe" } member
           && EndsWithIdentifier(member.Expression, "FailureText");

    // ---- dùng chung ---------------------------------------------------------------------------------------

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
        => expression is ParenthesizedExpressionSyntax parenthesized ? Unwrap(parenthesized.Expression) : expression;

    // Đoạn cuối cùng của một biểu thức tên — chấp nhận cả tên đơn (`FailureText`, nhờ `using`, hình dạng thật
    // trong Core) lẫn tên đủ (`CoreAndSkill.Core.Application.Common.Logging.FailureText`). Dùng cho ba định danh
    // mà luật B28 cần nhận ra bất kể có `using` hay không: `LogLevel`, `HealthCheckResult`, `FailureText`.
    private static bool EndsWithIdentifier(ExpressionSyntax expression, string identifier)
        => Unwrap(expression) switch
        {
            IdentifierNameSyntax id => id.Identifier.Text == identifier,
            GenericNameSyntax generic => generic.Identifier.Text == identifier,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text == identifier,
            _ => false,
        };

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    // Dòng offender phải đọc được trong log CI: gộp khoảng trắng, cắt cho gọn.
    private static string Compact(SyntaxNode node)
    {
        var text = string.Join(' ', node.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length <= 100 ? text : text[..97] + "...";
    }
}
