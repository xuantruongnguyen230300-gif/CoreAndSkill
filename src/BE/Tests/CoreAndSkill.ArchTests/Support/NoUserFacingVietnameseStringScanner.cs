using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreAndSkill.ArchTests.Support;

// Luật R8 (docs/RULES-BE.md) — định nghĩa gốc: docs/quy-uoc/be-cqrs-handler.md §7.2, "Ranh giới của luật R8 —
// tiếng Việt chỉ ở tám hình dạng cú pháp". Người dùng chốt 2026-09-29, hình dạng 8 và mở rộng hình dạng 3 chốt
// 2026-09-30. Lý do từng hình dạng an toàn, ví dụ đỏ hay gặp, hai điểm mù đã biết:
// docs/wiki-core/be/ly-do/be-cqrs-handler.md §7.2 — đọc ở đó, không chép lại ở đây.
//
// BẢY BƯỚC của bộ dò (docs/quy-uoc/be-cqrs-handler.md §7.2, dưới bảng tám hình dạng):
//   1. Phân tích cú pháp (Roslyn), không grep.
//   2. Tầm quét ProductSourceFiles.Core() — khai ở NƠI GỌI, không ở đây.
//   3. Chuỗi tiếng Việt: literal chuỗi (thường/verbatim/raw/UTF-8) và PHẦN VĂN BẢN của chuỗi nội suy — sau khi
//      chuẩn hoá NFC chứa ít nhất một chữ riêng (đ ă â ê ô ơ ư, hoa lẫn thường) hay một chữ mang dấu thanh
//      (sắc/huyền/hỏi/ngã/nặng). Literal KÝ TỰ đơn ('đ') nằm NGOÀI tầm (người dùng chốt 2026-10-01): một ký tự
//      không thể là câu hiển thị, nó dùng để so khớp, chuẩn hoá chữ. Chú thích là trivia của Roslyn, tự nằm
//      ngoài DescendantNodes().
//   4. Leo tới hình dạng: hình dạng 3 xét theo TỔ TIÊN ở mọi độ sâu (IsInsideThrowOrExceptionBaseCtor); bảy hình
//      dạng còn lại xét theo CHỖ ĐỨNG sau khi leo qua các nút trong suốt (ClimbThroughTransparentNodes) — chuỗi
//      nội suy và lỗ của nó, dấu ngoặc, phép nối `+`, hai nhánh `?:`, vế phải `??`.
//   5. Đóng mặc định — không hình dạng nào khớp thì là vi phạm. KHÔNG danh sách miễn trừ theo tệp hay theo dòng.
//   6. T6 — ở NƠI GỌI: tổng literal đã xét và số khớp từng hình dạng; FAIL khi tổng về 0 hay hình dạng 1 về 0.
//   7. T1 — ở NƠI GỌI: nhóm Detector_R8_*.
//
// ĐIỂM MÙ ĐÃ BIẾT (docs/quy-uoc/be-cqrs-handler.md §7.2, mục "Điểm mù"): chuỗi tiếng Việt viết KHÔNG DẤU, và câu
// hiển thị viết bằng TIẾNG ANH. Bắt được cả hai đòi hiểu nghĩa câu — review là lớp bắt, không phải cổng này.
internal static class NoUserFacingVietnameseStringScanner
{
    // Một chỗ vi phạm — không khớp hình dạng nào trong tám hình dạng.
    internal sealed record Hit(string FilePath, int Line, string Literal);

    // Một literal tiếng Việt đã xét, và hình dạng nó khớp (null = vi phạm). Dùng cho cả Scan (lọc Shape == null)
    // lẫn T6 (đếm theo Shape) ở nơi gọi — một lượt duyệt cây, hai câu hỏi.
    internal sealed record Classification(string FilePath, int Line, string Literal, Shape? Shape);

    // Đúng số thứ tự của bảng tám hình dạng ở docs/quy-uoc/be-cqrs-handler.md §7.2.
    internal enum Shape
    {
        ErrorCatalogMessage = 1,
        LoggerTemplate = 2,
        ExceptionMessage = 3,
        AttributeArgument = 4,
        MetricDescription = 5,
        HealthCheckDescription = 6,
        CommandOutput = 7,
        ConfigCheckMessage = 8,
    }

    private static readonly string[] LoggerShapeMethodNames =
        ["LogTrace", "LogDebug", "LogInformation", "LogWarning", "LogError", "LogCritical"];

    public static IReadOnlyList<Hit> Scan(string source, string filePath)
        => [.. Classify(source, filePath)
                .Where(c => c.Shape is null)
                .Select(c => new Hit(c.FilePath, c.Line, c.Literal))];

    public static IReadOnlyList<Classification> Classify(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var results = new List<Classification>();

        foreach (var node in root.DescendantNodes())
        {
            var vietnameseText = VietnameseTextOf(node);
            if (vietnameseText is null)
                continue;

            var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            results.Add(new Classification(filePath, line, node.ToString(), ClassifyNode(node, filePath)));
        }

        return results;
    }

    // ===== Bước 3 — nhận diện một literal/phần văn bản TIẾNG VIỆT =====

    private static string? VietnameseTextOf(SyntaxNode node)
    {
        var raw = node switch
        {
            LiteralExpressionSyntax literal
                when literal.IsKind(SyntaxKind.StringLiteralExpression)
                     || literal.IsKind(SyntaxKind.Utf8StringLiteralExpression)
                => literal.Token.ValueText,

            InterpolatedStringTextSyntax text => text.TextToken.ValueText,

            _ => null,
        };

        if (string.IsNullOrEmpty(raw))
            return null;

        var normalized = raw.Normalize(NormalizationForm.FormC);
        return ContainsVietnameseLetter(normalized) ? normalized : null;
    }

    // đ ă â ê ô ơ ư, hoa lẫn thường — chữ RIÊNG của bảng chữ tiếng Việt, không cần dấu thanh đi kèm.
    private static readonly char[] VietnameseSpecialLetters = "đĐăĂâÂêÊôÔơƠưƯ".ToCharArray();

    // Năm dấu thanh (sắc, huyền, hỏi, ngã, nặng) dưới dạng combining mark sau khi NFD — bắt MỌI nguyên âm mang
    // dấu thanh, không riêng bảy chữ ở trên (ví dụ "á", "ù" không có chữ riêng nhưng vẫn là tiếng Việt).
    private static readonly char[] ToneMarks = ['̀', '́', '̃', '̉', '̣'];

    private static bool ContainsVietnameseLetter(string normalizedNfc)
    {
        if (normalizedNfc.IndexOfAny(VietnameseSpecialLetters) >= 0)
            return true;

        // Chuỗi đã chuẩn hoá NFC (bước 3 đòi hỏi) rồi PHÂN TÁCH LẠI (NFD) chỉ để lộ combining mark của dấu thanh
        // — không đổi hành vi nhận diện so với đầu vào NFC, vì canonical decomposition là một ánh xạ xác định.
        var decomposed = normalizedNfc.Normalize(NormalizationForm.FormD);
        foreach (var ch in decomposed)
        {
            if (Array.IndexOf(ToneMarks, ch) >= 0)
                return true;
        }

        return false;
    }

    // ===== Bước 4 — leo tới hình dạng =====

    private static Shape? ClassifyNode(SyntaxNode node, string filePath)
    {
        // Hình dạng 3 xét theo TỔ TIÊN, độc lập với phép leo nút trong suốt của bảy hình dạng kia.
        if (IsInsideThrowOrExceptionBaseCtor(node))
            return Shape.ExceptionMessage;

        var top = ClimbThroughTransparentNodes(node);

        if (IsErrorCatalogMessageSlot(top)) return Shape.ErrorCatalogMessage;
        if (IsLoggerTemplateSlot(top)) return Shape.LoggerTemplate;
        if (IsAttributeArgumentSlot(top)) return Shape.AttributeArgument;
        if (IsMetricDescriptionSlot(top)) return Shape.MetricDescription;
        if (IsHealthCheckDescriptionSlot(top)) return Shape.HealthCheckDescription;
        if (IsCommandOutputSlot(top, filePath)) return Shape.CommandOutput;
        if (IsConfigCheckMessageSlot(top)) return Shape.ConfigCheckMessage;

        return null;
    }

    // Nút trong suốt: chuỗi nội suy và lỗ của nó, dấu ngoặc, phép nối `+`, hai nhánh của `?:`, vế phải của `??`.
    // Leo từ node lên tới nút ĐẦU TIÊN không trong suốt — slot thật nằm ở cha của giá trị trả về.
    private static SyntaxNode ClimbThroughTransparentNodes(SyntaxNode node)
    {
        var current = node;

        while (true)
        {
            var parent = current.Parent;
            if (parent is null)
                return current;

            if (parent is InterpolationSyntax interpolation && interpolation.Expression == current)
            {
                current = interpolation; // leo ra khỏi lỗ nội suy trước, rồi xét tiếp ở vòng sau
                continue;
            }

            if (parent is InterpolatedStringExpressionSyntax && current is InterpolatedStringContentSyntax)
            {
                current = parent;
                continue;
            }

            if (parent is ParenthesizedExpressionSyntax)
            {
                current = parent;
                continue;
            }

            if (parent is BinaryExpressionSyntax binary)
            {
                if (binary.IsKind(SyntaxKind.AddExpression))
                {
                    current = parent;
                    continue;
                }

                if (binary.IsKind(SyntaxKind.CoalesceExpression) && binary.Right == current)
                {
                    current = parent;
                    continue;
                }
            }

            if (parent is ConditionalExpressionSyntax conditional
                && (conditional.WhenTrue == current || conditional.WhenFalse == current))
            {
                current = parent;
                continue;
            }

            return current;
        }
    }

    // ===== Hình dạng 1 — thông điệp Error trong catalog =====

    private static bool IsErrorCatalogMessageSlot(SyntaxNode top)
    {
        if (top.Parent is not ArgumentSyntax argument || argument.Parent is not ArgumentListSyntax argList)
            return false;

        if (argList.Parent is ObjectCreationExpressionSyntax creation
            && LastIdentifier(creation.Type) == "Error"
            && IsMessageTemplatePosition(argument, argList))
        {
            return IsInsideStaticErrorsClass(creation);
        }

        if (argList.Parent is ImplicitObjectCreationExpressionSyntax implicitCreation
            && IsMessageTemplatePosition(argument, argList)
            && IsDirectFieldInitializerOfTypeError(implicitCreation))
        {
            return IsInsideStaticErrorsClass(implicitCreation);
        }

        return false;
    }

    private static bool IsMessageTemplatePosition(ArgumentSyntax argument, ArgumentListSyntax argList)
    {
        if (argument.NameColon is { Name.Identifier.Text: "MessageTemplate" })
            return true;

        return argument.NameColon is null && IndexOfArgument(argList.Arguments, argument) == 1;
    }

    private static bool IsDirectFieldInitializerOfTypeError(ExpressionSyntax creation)
        => creation.Parent is EqualsValueClauseSyntax equals
           && equals.Parent is VariableDeclaratorSyntax declarator
           && declarator.Parent is VariableDeclarationSyntax declaration
           && declaration.Parent is FieldDeclarationSyntax
           && LastIdentifier(declaration.Type) == "Error";

    private static bool IsInsideStaticErrorsClass(SyntaxNode node)
    {
        var classDecl = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        return classDecl is not null
               && classDecl.Modifiers.Any(SyntaxKind.StaticKeyword)
               && classDecl.Identifier.Text.EndsWith("Errors", StringComparison.Ordinal);
    }

    // ===== Hình dạng 2 — template của ILogger =====

    private static bool IsLoggerTemplateSlot(SyntaxNode top)
    {
        if (top.Parent is not ArgumentSyntax argument || argument.NameColon is not null)
            return false;

        if (argument.Parent is not ArgumentListSyntax argList || argList.Parent is not InvocationExpressionSyntax invocation)
            return false;

        // `logger.LogError(…)` (MemberAccess) và `logger?.LogError(…)` (MemberBinding) là cùng một lời gọi.
        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text,
            _ => null,
        };

        return methodName is not null
               && LoggerShapeMethodNames.Contains(methodName, StringComparer.Ordinal)
               && IndexOfArgument(argList.Arguments, argument) == 0;
    }

    // ===== Hình dạng 3 — thông điệp ngoại lệ (xét theo TỔ TIÊN, gọi riêng ở ClassifyNode) =====

    private static bool IsInsideThrowOrExceptionBaseCtor(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is ThrowStatementSyntax or ThrowExpressionSyntax)
                return true;

            if (ancestor is ConstructorInitializerSyntax initializer
                && initializer.IsKind(SyntaxKind.BaseConstructorInitializer)
                && initializer.Parent is ConstructorDeclarationSyntax ctor
                && ctor.Parent is ClassDeclarationSyntax ctorClass
                && FirstBaseTypeEndsWithException(ctorClass))
            {
                return true;
            }

            if (ancestor is PrimaryConstructorBaseTypeSyntax primaryBase
                && primaryBase.Parent is BaseListSyntax baseList
                && baseList.Parent is ClassDeclarationSyntax primaryClass
                && ReferenceEquals(FirstBaseTypeOf(primaryClass), primaryBase)
                && TypeNameEndsWithException(primaryBase.Type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool FirstBaseTypeEndsWithException(ClassDeclarationSyntax classDecl)
    {
        var first = FirstBaseTypeOf(classDecl);
        return first is not null && TypeNameEndsWithException(first.Type);
    }

    private static BaseTypeSyntax? FirstBaseTypeOf(ClassDeclarationSyntax classDecl)
        => classDecl.BaseList?.Types.FirstOrDefault();

    private static bool TypeNameEndsWithException(TypeSyntax type)
    {
        var text = type.ToString();
        var lastSegment = text.Contains('.') ? text[(text.LastIndexOf('.') + 1)..] : text;
        var nameOnly = lastSegment.Contains('<') ? lastSegment[..lastSegment.IndexOf('<')] : lastSegment;
        return nameOnly.EndsWith("Exception", StringComparison.Ordinal);
    }

    // ===== Hình dạng 4 — đối số attribute =====

    private static bool IsAttributeArgumentSlot(SyntaxNode top)
        => top.Parent is AttributeArgumentSyntax;

    // ===== Hình dạng 5 — mô tả chỉ số =====

    private static bool IsMetricDescriptionSlot(SyntaxNode top)
    {
        if (top.Parent is not ArgumentSyntax argument || argument.NameColon is not { Name.Identifier.Text: "description" })
            return false;

        if (argument.Parent is not ArgumentListSyntax argList || argList.Parent is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax member)
            return false;

        return member.Name.Identifier.Text.StartsWith("Create", StringComparison.Ordinal)
               && EndsWithIdentifier(member.Expression, "Meter");
    }

    // ===== Hình dạng 6 — mô tả health check =====

    private static bool IsHealthCheckDescriptionSlot(SyntaxNode top)
    {
        if (top.Parent is not ArgumentSyntax argument || argument.Parent is not ArgumentListSyntax argList)
            return false;

        if (argList.Parent is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberAccessExpressionSyntax member
            && member.Name.Identifier.Text is "Healthy" or "Degraded" or "Unhealthy"
            && EndsWithIdentifier(member.Expression, "HealthCheckResult"))
        {
            if (argument.NameColon is { Name.Identifier.Text: "description" })
                return true;

            return argument.NameColon is null && IndexOfArgument(argList.Arguments, argument) == 0;
        }

        if (argList.Parent is ObjectCreationExpressionSyntax creation && LastIdentifier(creation.Type) == "HealthCheckResult")
        {
            return argument.NameColon is null && IndexOfArgument(argList.Arguments, argument) == 1;
        }

        return false;
    }

    // ===== Hình dạng 7 — đầu ra của lệnh Core =====

    private static bool IsCommandOutputSlot(SyntaxNode top, string filePath)
    {
        if (!IsUnderCommandsDirectory(filePath))
            return false;

        if (top.Parent is not ArgumentSyntax argument || argument.Parent is not ArgumentListSyntax argList)
            return false;

        if (argList.Parent is not InvocationExpressionSyntax invocation || invocation.Expression is not MemberAccessExpressionSyntax member)
            return false;

        var methodName = member.Name.Identifier.Text;
        if (methodName is not ("Write" or "WriteLine" or "WriteAsync" or "WriteLineAsync"))
            return false;

        return ReceiverEndsAtConsole(member.Expression);
    }

    private static bool ReceiverEndsAtConsole(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax id => id.Identifier.Text == "Console",
        MemberAccessExpressionSyntax member when member.Name.Identifier.Text is "Error" or "Out"
            => ReceiverEndsAtConsole(member.Expression),
        _ => false,
    };

    private static bool IsUnderCommandsDirectory(string filePath)
        => filePath.Replace('\\', '/').Contains("/CoreAndSkill.Core.Web/Commands/", StringComparison.Ordinal);

    // ===== Hình dạng 8 — thông điệp kiểm cấu hình =====

    private static bool IsConfigCheckMessageSlot(SyntaxNode top)
        => IsValidationResultConstructorSlot(top)
           || IsValidateOptionsResultFailSlot(top)
           || IsOptionsBuilderValidateFailureMessageSlot(top)
           || IsFormatErrorMessageReturnSlot(top);

    private static bool IsValidationResultConstructorSlot(SyntaxNode top)
    {
        if (top.Parent is not ArgumentSyntax argument || argument.Parent is not ArgumentListSyntax argList)
            return false;

        if (argList.Parent is not ObjectCreationExpressionSyntax creation || LastIdentifier(creation.Type) != "ValidationResult")
            return false;

        if (argument.NameColon is { Name.Identifier.Text: "errorMessage" })
            return true;

        return argument.NameColon is null && IndexOfArgument(argList.Arguments, argument) == 0;
    }

    private static bool IsValidateOptionsResultFailSlot(SyntaxNode top)
    {
        if (top.Parent is not ArgumentSyntax argument || argument.Parent is not ArgumentListSyntax argList)
            return false;

        if (argList.Parent is not InvocationExpressionSyntax invocation || invocation.Expression is not MemberAccessExpressionSyntax member)
            return false;

        return member.Name.Identifier.Text == "Fail" && EndsWithIdentifier(member.Expression, "ValidateOptionsResult");
    }

    // `.Validate(` của OptionsBuilder<T> — nhận diện bằng CHỮ KÝ (đối số đầu là lambda), không bằng kiểu của
    // receiver (không có semantic model). Đối số cuối, hay đối số tên `failureMessage:`.
    private static bool IsOptionsBuilderValidateFailureMessageSlot(SyntaxNode top)
    {
        if (top.Parent is not ArgumentSyntax argument || argument.Parent is not ArgumentListSyntax argList)
            return false;

        if (argList.Parent is not InvocationExpressionSyntax invocation || invocation.Expression is not MemberAccessExpressionSyntax member)
            return false;

        if (member.Name.Identifier.Text != "Validate")
            return false;

        var arguments = argList.Arguments;
        if (arguments.Count == 0 || arguments[0].Expression is not (SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax))
            return false;

        if (argument.NameColon is { Name.Identifier.Text: "failureMessage" })
            return true;

        return argument.NameColon is null && IndexOfArgument(arguments, argument) == arguments.Count - 1;
    }

    private static bool IsFormatErrorMessageReturnSlot(SyntaxNode top)
    {
        if (top.Parent is ArrowExpressionClauseSyntax arrow)
            return arrow.Parent is MethodDeclarationSyntax method && IsOverrideFormatErrorMessage(method);

        if (top.Parent is ReturnStatementSyntax returnStatement)
        {
            var nearest = returnStatement.Ancestors()
                .FirstOrDefault(a => a is MethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);

            return nearest is MethodDeclarationSyntax method && IsOverrideFormatErrorMessage(method);
        }

        return false;
    }

    private static bool IsOverrideFormatErrorMessage(MethodDeclarationSyntax method)
        => method.Identifier.Text == "FormatErrorMessage" && method.Modifiers.Any(SyntaxKind.OverrideKeyword);

    // ---- dùng chung ---------------------------------------------------------------------------------------

    private static string? LastIdentifier(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax id => id.Identifier.Text,
        GenericNameSyntax generic => generic.Identifier.Text,
        QualifiedNameSyntax qualified => LastIdentifier(qualified.Right),
        AliasQualifiedNameSyntax alias => LastIdentifier(alias.Name),
        _ => null,
    };

    // Đoạn cuối cùng của một biểu thức tên — chấp nhận cả tên đơn (nhờ `using`) lẫn tên đủ. Cùng khuôn
    // LoggerExceptionArgumentScanner.EndsWithIdentifier — mỗi scanner tự giữ bản của mình (không module dùng
    // chung giữa các Support/*Scanner.cs trong repo này).
    private static bool EndsWithIdentifier(ExpressionSyntax expression, string identifier) => expression switch
    {
        IdentifierNameSyntax id => id.Identifier.Text == identifier,
        GenericNameSyntax generic => generic.Identifier.Text == identifier,
        MemberAccessExpressionSyntax member => member.Name.Identifier.Text == identifier,
        ParenthesizedExpressionSyntax parenthesized => EndsWithIdentifier(parenthesized.Expression, identifier),
        _ => false,
    };

    private static int IndexOfArgument(SeparatedSyntaxList<ArgumentSyntax> arguments, ArgumentSyntax argument)
    {
        for (var i = 0; i < arguments.Count; i++)
        {
            if (ReferenceEquals(arguments[i], argument))
                return i;
        }

        return -1;
    }
}
