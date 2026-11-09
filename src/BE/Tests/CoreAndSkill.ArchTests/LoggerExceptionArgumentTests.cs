using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Luật B28 — docs/RULES-BE.md, định nghĩa gốc ở docs/wiki-core/be/07-observability.md §5 mục "Ghi một ngoại lệ vào
// log". Vì sao cổng tồn tại, ba vế bắt gì, và điểm mù đã biết: đầu Support/LoggerExceptionArgumentScanner.cs — đọc
// ở đó, không chép lại ở đây.
//
// Tầm quét: ProductSourceFiles.Core() — năm project Core cộng host
// (docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md quyết định 8), cùng khuôn mọi cổng quét cú pháp khác
// (SystemActorLiteralTests, RolePermissionWriteBypassTests).
//
// T12:cap-xanh-do — mọi ca Detector_B28_* kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng phép dò, khác đúng một chỗ
// (docs/RULES.md §8 luật T12); chú thích "Cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
public class LoggerExceptionArgumentTests
{
    // ===== Vế 1 — luật thật =====

    [Fact]
    public void CoreSource_MustNotPass_AnExceptionObject_ToILogger()
    {
        var offenders = ScannedFiles()
            .SelectMany(file => LoggerExceptionArgumentScanner.ScanLoggerCalls(File.ReadAllText(file), file))
            .Select(Format)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // ===== Vế 2 — luật thật =====

    [Fact]
    public void CoreSource_MustNotPass_AnException_ToHealthCheckResult()
    {
        var offenders = ScannedFiles()
            .SelectMany(file => LoggerExceptionArgumentScanner.ScanHealthCheckResultCalls(File.ReadAllText(file), file))
            .Select(Format)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // ===== Vế 3 — luật thật =====

    [Fact]
    public void CoreSource_WarningOrHigherLog_MustNotUse_FailureTextDescribe()
    {
        var offenders = ScannedFiles()
            .SelectMany(file => LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(File.ReadAllText(file), file))
            .Select(Format)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // ===== T6 — chốt chống xanh rỗng =====

    // Đúng cột "Đỏ khi" ở docs/wiki-core/be/07-observability.md §5 (luật B28): bộ dò phải THẤY ít nhất một lời gọi Log<Mức>( thật VÀ ít nhất một
    // HealthCheckResult.* thật trên source thật — nếu không, ba test luật ở trên xanh vì tầm quét rỗng, không phải
    // vì Core sạch.
    [Fact]
    public void CoreSource_MustNotPass_AnExceptionObject_ToILogger_ScansRealLoggerCalls()
    {
        var files = ScannedFiles();

        var loggerCallSites = files.Sum(f => LoggerExceptionArgumentScanner.CountLoggerCallSites(File.ReadAllText(f)));
        var healthCheckCallSites = files.Sum(f => LoggerExceptionArgumentScanner.CountHealthCheckResultCallSites(File.ReadAllText(f)));

        loggerCallSites.ShouldBeGreaterThan(0, "bộ dò không thấy lời gọi Log<Mức>( nào trên source thật — tầm quét rỗng");
        healthCheckCallSites.ShouldBeGreaterThan(0, "bộ dò không thấy lời gọi HealthCheckResult.* nào trên source thật — tầm quét rỗng");

        // Hai tệp cụ thể đã biết mang cả hai hình dạng — nếu một trong hai dời chỗ mà không cập nhật lại việc quét,
        // ca này đỏ thay vì âm thầm giảm độ phủ.
        files.ShouldContain(f => f.EndsWith("JobRunner.cs", StringComparison.Ordinal));
        files.ShouldContain(f => f.EndsWith("DatabaseHealthCheck.cs", StringComparison.Ordinal));
    }

    // ===== Detector_B28_* (T1) — Vế 1: đối số thông điệp không phải chuỗi =====

    [Fact]
    public void Detector_B28_Catches_ExceptionObjectAsFirstArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(ex, \"Lỗi khi xử lý.\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_ExceptionObjectAsFirstArgument — cùng khuôn catch + LogError, khác đúng việc có
    // trao `ex` làm đối số đầu hay không.
    [Fact]
    public void Detector_B28_Ignores_OrdinaryStringLiteralFirstArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi khi xử lý.\");"), "fake.cs")
            .ShouldBeEmpty();

    [Fact]
    public void Detector_B28_Catches_InterpolatedStringConcatenation_WithExceptionInTheAddition()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: \" + ex, \"…\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_InterpolatedStringConcatenation_WithExceptionInTheAddition — cùng phép nối `+`,
    // khác đúng vế phải: chuỗi literal thay vì `ex`.
    [Fact]
    public void Detector_B28_Ignores_StringConcatenationOfTwoLiterals()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: \" + \"chi tiết\", \"…\");"), "fake.cs")
            .ShouldBeEmpty();

    // Danh mục ca biên "Unicode và dấu tiếng Việt" (docs/wiki-core/be/04-testing-strategy.md §9) — thông điệp log
    // thật của Core luôn mang dấu tiếng Việt; chuỗi verbatim/raw nhiều dòng cũng phải được nhận ra là một biểu thức
    // chuỗi hợp lệ, không riêng literal thường.
    // Cặp đỏ: Detector_B28_Catches_ExceptionObjectAsFirstArgument — cùng vị trí thông điệp, khác đúng DẠNG chuỗi:
    // verbatim nhiều dòng mang dấu tiếng Việt thay vì `ex`.
    [Fact]
    public void Detector_B28_Ignores_VerbatimVietnameseStringAsMessage()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(@\"Lỗi khi xử lý — dòng một\ndòng hai có dấu: ă â ê ô ơ ư đ.\");"), "fake.cs")
            .ShouldBeEmpty();

    // Lỗ nội suy mang một biến KHÔNG liên quan tới catch — chuỗi nội suy bình thường phải xanh, không riêng dạng có
    // FailureText.DescribeWithStack trong lỗ.
    // Cặp đỏ: Detector_B28_Catches_BareCatchVariable_InsideAnInterpolationHole — cùng chuỗi nội suy một lỗ, khác
    // đúng biểu thức trong lỗ: `jobId` (biến bình thường) thay vì `ex` (biến catch).
    [Fact]
    public void Detector_B28_Ignores_InterpolationHole_WithAnUnrelatedVariable()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("var jobId = 1; logger.LogError($\"Việc {jobId} lỗi.\");"), "fake.cs")
            .ShouldBeEmpty();

    // Đúng hình dạng `.Log(LogLevel.Error, …)` — vị trí thông điệp là đối số THỨ HAI (sau LogLevel).
    [Fact]
    public void Detector_B28_Catches_ExceptionAsMessagePosition_OfTheGenericLogOverload()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, ex);"), "fake.cs")
            .ShouldNotBeEmpty();

    // Luật chủ (07-observability.md §5): mọi `.Log(LogLevel.*, …)` là đỏ, kể cả khi thông điệp là chuỗi — Core dùng
    // `Log<Mức>(`, không dùng overload tổng quát.
    [Fact]
    public void Detector_B28_Catches_GenericLogOverload_EvenWithAStringMessage()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, \"Lỗi.\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_GenericLogOverload_EvenWithAStringMessage — cùng mức Error, cùng thông điệp chuỗi,
    // khác đúng dạng gọi: `LogError("…")` thay vì `.Log(LogLevel.Error, "…")`.
    [Fact]
    public void Detector_B28_Ignores_LogErrorWithTheSameStringMessage_AsTheGenericLogOverload()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi.\");"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_ExceptionObjectAsFirstArgument — cùng đối số đầu là `ex`, khác đúng tên phương
    // thức được gọi: không phải một trong sáu tên Log<Mức>, nên KHÔNG phải hình dạng cổng này xét.
    [Fact]
    public void Detector_B28_Ignores_ANonLoggerMethodNamedSimilarly()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("someOtherRecorder.Record(ex, \"Lỗi khi xử lý.\");"), "fake.cs")
            .ShouldBeEmpty();

    // ----- Lời gọi qua `?.` và `.Log(` có mức log là biến -----

    // Roslyn dựng `logger?.LogError(…)` với Expression là MemberBindingExpression, không phải MemberAccess.
    // Cặp đỏ: Detector_B28_Ignores_ConditionalAccessLogError_WithStringMessageAndDescribeWithStack — cùng `?.LogError`,
    // khác đúng đối số: `ex` ở vị trí thông điệp.
    [Fact]
    public void Detector_B28_Catches_ConditionalAccessLogError_WithExceptionObjectAsFirstArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger?.LogError(ex, \"Lỗi khi xử lý.\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp xanh sinh đôi của hai ca `?.` đỏ ở dưới (vế 1 và vế 3): cùng `?.LogError`, thông điệp chuỗi, đối số cuối
    // là DescribeWithStack(ex).
    [Fact]
    public void Detector_B28_Ignores_ConditionalAccessLogError_WithStringMessageAndDescribeWithStack()
    {
        var source = LoggerCallInCatch(
            "logger?.LogError(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.DescribeWithStack(ex));");

        LoggerExceptionArgumentScanner.ScanLoggerCalls(source, "fake.cs").ShouldBeEmpty();
        LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(source, "fake.cs").ShouldBeEmpty();
    }

    // Vế 3 qua `?.`. Cặp xanh: Detector_B28_Ignores_ConditionalAccessLogError_WithStringMessageAndDescribeWithStack.
    [Fact]
    public void Detector_B28_Catches_FailureTextDescribe_InConditionalAccessLogWarning()
        => LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(
                LoggerCallInCatch("logger?.LogWarning(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.Describe(ex));"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_ConditionalAccessLogError_WithExceptionObjectAsFirstArgument ở vế 1(b) — cùng `?.`,
    // khác: biến catch nằm ở đối số thứ hai (`ex.Message`), không ở vị trí thông điệp.
    [Fact]
    public void Detector_B28_Catches_CatchVariableMessage_InConditionalAccessLogError()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger?.LogError(\"Lỗi: {Msg}\", ex.Message);"), "fake.cs")
            .ShouldNotBeEmpty();

    // Mức log là BIẾN — IsLogLevelMember cũ gặp IdentifierName nên trả false.
    // Cặp xanh: Detector_B28_Ignores_LogErrorWithTheSameStringMessage_AsTheGenericLogOverload — cùng thông điệp, khác
    // đúng dạng gọi.
    [Fact]
    public void Detector_B28_Catches_GenericLogOverload_WhenTheLevelIsAVariable()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("var level = Microsoft.Extensions.Logging.LogLevel.Error; logger.Log(level, ex, \"Lỗi.\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_GenericLogOverload_WhenTheLevelIsAVariable — cùng receiver, cùng đối số `level`,
    // khác đúng tên phương thức: không phải `Log` và không phải sáu tên Log<Mức>.
    [Fact]
    public void Detector_B28_Ignores_ANonLogMethod_TakingALevelVariable()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("var level = 1; logger.Record(level, \"Lỗi.\");"), "fake.cs")
            .ShouldBeEmpty();

    // T6 đếm lời gọi `?.`. Cặp: Detector_B28_Ignores_ConditionalAccessOfANonLogMethod_InTheCallSiteCount.
    [Fact]
    public void Detector_B28_CountsAConditionalAccessLogCall_AsALoggerCallSite()
        => LoggerExceptionArgumentScanner.CountLoggerCallSites(
                LoggerCallInCatch("logger?.LogError(\"Lỗi.\");"))
            .ShouldBe(1);

    [Fact]
    public void Detector_B28_Ignores_ConditionalAccessOfANonLogMethod_InTheCallSiteCount()
        => LoggerExceptionArgumentScanner.CountLoggerCallSites(
                LoggerCallInCatch("logger?.Record(\"Lỗi.\");"))
            .ShouldBe(0);

    // ===== Detector_B28_* (T1) — Vế 1: biến catch làm một đối số / trong lỗ nội suy =====

    [Fact]
    public void Detector_B28_Catches_CatchVariableMessageAsArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Msg}\", ex.Message);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_CatchVariableToString_AsArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Msg}\", ex.ToString());"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_BareCatchVariable_AsATrailingArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogWarning(\"Lỗi {Info}\", \"chi tiết\", ex);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_BareCatchVariable_InsideAnInterpolationHole()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError($\"Lỗi: {ex}\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_CatchVariableMessageAsArgument — cùng khuôn catch + LogError với hai đối số,
    // khác đúng đối số thứ hai: gọi FailureText.DescribeWithStack(ex) thay vì `ex.Message` — đúng hình dạng đích
    // của luật B28 (docs/RULES-BE.md).
    [Fact]
    public void Detector_B28_Ignores_DescribeWithStackAsArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.DescribeWithStack(ex));"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_BareCatchVariable_AsATrailingArgument — cùng hai đối số, khác đúng đối số cuối:
    // DescribeWithStack(ex) thay vì `ex` trơ trọi.
    [Fact]
    public void Detector_B28_Ignores_DescribeWithStackAsTrailingArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogWarning(\"Lỗi {Info}\", \"chi tiết\", CoreAndSkill.Core.Application.Common.Logging.FailureText.DescribeWithStack(ex));"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_BareCatchVariable_InsideAnInterpolationHole — cùng chuỗi nội suy một lỗ, khác
    // đúng biểu thức trong lỗ: DescribeWithStack(ex) thay vì `ex` trơ trọi.
    [Fact]
    public void Detector_B28_Ignores_DescribeWithStackInsideAnInterpolationHole()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError($\"Lỗi: {CoreAndSkill.Core.Application.Common.Logging.FailureText.DescribeWithStack(ex)}\");"),
                "fake.cs")
            .ShouldBeEmpty();

    // Biến KHÔNG khai ở mệnh đề `catch` và KHÔNG phải tham số kiểu *Exception: `ex` ở đây là tham số kiểu `ErrorInfo`
    // (`ErrorInfo`, tên kiểu không kết thúc bằng `Exception`) nên `.Message` của nó không bị bắt. Điểm mù cũ "tham số Exception lọt" đã đóng 2026-10-03.
    // Cặp đỏ: Detector_B28_Catches_ExceptionTypedMethodParameter_DotMessage — cùng `ex.<thành viên>` làm đối số của
    // LogError, khác đúng KIỂU tham số: `System.Exception` thay vì `ErrorInfo`.
    [Fact]
    public void Detector_B28_Ignores_DotMember_OnAParameterWhoseTypeIsNotAnException()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("ErrorInfo ex", "logger.LogError(\"Lỗi: {Msg}\", ex.Message);"), "fake.cs")
            .ShouldBeEmpty();

    // ----- Mở rộng 2026-10-03: tham số kiểu *Exception (không qua catch) -----

    [Fact]
    public void Detector_B28_Catches_ExceptionTypedMethodParameter_DotMessage()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("System.Exception ex", "logger.LogError(\"Lỗi: {Msg}\", ex.Message);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_ExceptionParameterNamedException_InVoidHandler()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("Exception exception", "logger.LogError(\"Lỗi: {M}\", exception.Message);"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_ExceptionParameterNamedException_InVoidHandler — cùng phương thức, cùng tham số
    // `Exception exception`, khác đúng đối số log: FailureText.DescribeWithStack(exception) thay vì `exception.Message`.
    [Fact]
    public void Detector_B28_Ignores_ExceptionParameter_InsideDescribeWithStack()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("Exception exception", "logger.LogError(\"Lỗi: {M}\", FailureText.DescribeWithStack(exception));"), "fake.cs")
            .ShouldBeEmpty();

    [Fact]
    public void Detector_B28_Catches_NullableDbUpdateExceptionParameter()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("DbUpdateException? ex", "logger.LogError(\"Lỗi: {M}\", ex);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_NullableSystemExceptionParameter_InterpolationHole()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("System.Exception? ex", "logger.LogError($\"Lỗi: {ex.InnerException}\");"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_ExplicitlyTypedLambdaParameter()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("", "System.Action<Exception> h = (Exception e) => logger.LogError(\"Lỗi: {I}\", e.InnerException);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_AnonymousMethodExceptionParameter()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("", "System.Action<Exception> h = delegate (Exception e) { logger.LogError(\"Lỗi: {M}\", e.Message); };"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_LocalFunctionExceptionParameter()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("", "void Local(IOException e) { logger.LogError(\"Lỗi: {M}\", e.Message); }"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_ConstructorExceptionParameter()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                """
                internal sealed class C
                {
                    public C(Microsoft.Extensions.Logging.ILogger logger, Exception ex)
                    {
                        logger.LogError("Lỗi: {M}", ex.Message);
                    }
                }
                """, "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_PrimaryConstructorExceptionParameter()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                """
                internal sealed class C(Microsoft.Extensions.Logging.ILogger logger, Exception ex)
                {
                    public void Run() => logger.LogError("Lỗi: {M}", ex.Message);
                }
                """, "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_ExplicitlyTypedLambdaParameter — cùng lambda, cùng `.InnerException`, khác đúng
    // việc tham số có khai kiểu hay không: `e => …` không biết kiểu bằng cú pháp — điểm mù đã khai (mục 2).
    [Fact]
    public void Detector_B28_Ignores_UntypedLambdaParameter_KnownBlindSpot()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInMember("", "System.Action<Exception> h = e => logger.LogError(\"Lỗi: {I}\", e.InnerException);"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_ExceptionTypedMethodParameter_DotMessage — cùng `ex.Message`, khác đúng tầm: tham
    // số của một phương thức KHÁC (`Other`) không phủ thân phương thức `Run` chứa lời gọi log.
    [Fact]
    public void Detector_B28_Ignores_ExceptionParameter_OfASiblingMember()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                """
                internal sealed class C
                {
                    private readonly Microsoft.Extensions.Logging.ILogger logger = null!;
                    public void Other(Exception ex) { }
                    public void Run(string ex) { logger.LogError("Lỗi: {M}", ex.Length); }
                }
                """, "fake.cs")
            .ShouldBeEmpty();

    // ----- Nới phép dò 2026-10-03: mọi tham chiếu biến catch trong cây con đối số, trừ trong FailureText.*(…) -----

    [Fact]
    public void Detector_B28_Catches_CatchVariableInnerException_AsArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Inner}\", ex.InnerException);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_CatchVariableGetBaseException_AsArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Base}\", ex.GetBaseException());"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_CatchVariableCastToObject_AsArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Obj}\", (object)ex);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_CatchVariableWithNullForgiving_AsArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Obj}\", ex!);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_CatchVariableGetTypeName_AsArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Type}\", ex.GetType().Name);"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_CatchVariableInnerException_InsideAnInterpolationHole()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError($\"Lỗi: {ex.InnerException}\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_CatchVariableInnerException_AsArgument — cùng `ex.InnerException` làm đối số thứ
    // hai, khác đúng việc nó nằm trong đối số của FailureText.DescribeWithStack(…) (ngoại lệ duy nhất của vế 1(b)).
    [Fact]
    public void Detector_B28_Ignores_InnerException_InsideDescribeWithStack()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.DescribeWithStack(ex.InnerException!));"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_CatchVariableGetBaseException_AsArgument — cùng `ex.GetBaseException()`, khác
    // đúng việc nằm trong đối số của một hàm FailureText.* (tên hàm bất kỳ).
    [Fact]
    public void Detector_B28_Ignores_GetBaseException_InsideAFailureTextCall()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.DescribeWithStack(ex.GetBaseException()));"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_CatchVariableInnerException_AsArgument — cùng tên thành viên `InnerException`,
    // khác đúng gốc: `other` không phải biến catch.
    [Fact]
    public void Detector_B28_Ignores_InnerException_OfAVariableNotDeclaredByACatchClause()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("var other = new System.Exception(); logger.LogError(\"Lỗi: {Inner}\", other.InnerException);"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_CatchVariableMessageAsArgument — cùng `.Message`, khác đúng gốc: tên `ex` đứng
    // SAU dấu chấm (thành viên của `holder`), không phải biến catch.
    [Fact]
    public void Detector_B28_Ignores_MemberNamedLikeTheCatchVariable_AfterADot()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Msg}\", holder.ex);"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_BareCatchVariable_AsATrailingArgument — cùng tên `ex` ở đối số cuối, khác đúng gốc:
    // `ex` đứng SAU `?.` (thành viên của `holder`, MemberBindingExpression), không phải biến catch. T12:cap-xanh-do
    [Fact]
    public void Detector_B28_Ignores_MemberNamedLikeTheCatchVariable_AfterAConditionalAccess()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Msg}\", holder?.ex);"), "fake.cs")
            .ShouldBeEmpty();

    // ----- Đối số TÊN: `exception:` (vế 1(c)) và `message:` (vế 1(a)) -----

    // `attempt.Exception` không phải biến catch (vế 1(b) không thấy) và đối số tên không đứng ở vị trí thông điệp (vế 1(a)
    // không thấy) — chỉ vế 1(c) bắt được.
    [Fact]
    public void Detector_B28_Catches_NamedExceptionArgument_WithANonNullValue()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogWarning(message: \"Lỗi: {Msg}\", exception: attempt.Exception);"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_NamedExceptionArgument_WithANonNullValue — cùng `LogWarning(message:, exception:)`,
    // khác đúng giá trị: literal `null` tường minh thay vì `attempt.Exception` — không phải trao ngoại lệ (cùng khuôn vế 2).
    [Fact]
    public void Detector_B28_Ignores_NamedExceptionArgument_ExplicitNull()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogWarning(message: \"Lỗi: {Msg}\", exception: null);"), "fake.cs")
            .ShouldBeEmpty();

    // Template đứng SAU một đối số tên khác: đối số đầu là `exception: null`, không phải chuỗi — dò theo vị trí 0 sẽ báo
    // nhầm. Cặp đỏ: Detector_B28_Catches_NamedMessageArgument_ThatIsNotAString — cùng thứ tự đối số, khác đúng giá trị của
    // `message:`: chuỗi literal thay vì biến không phải chuỗi.
    [Fact]
    public void Detector_B28_Ignores_NamedMessageArgument_WhenItIsNotTheFirstArgument()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger.LogError(exception: null, message: \"Lỗi.\");"), "fake.cs")
            .ShouldBeEmpty();

    [Fact]
    public void Detector_B28_Catches_NamedMessageArgument_ThatIsNotAString()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("var payload = 1; logger.LogError(exception: null, message: payload);"), "fake.cs")
            .ShouldNotBeEmpty();

    // ----- Canary: `?.Log(…)` và tham số Exception của toán tử -----

    // Cặp xanh: Detector_B28_Ignores_ConditionalAccessLogError_WithAStringMessage_NoGenericLogOverload.
    [Fact]
    public void Detector_B28_Catches_ConditionalAccessGenericLog_WithALevelVariable()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("var level = Microsoft.Extensions.Logging.LogLevel.Error; logger?.Log(level, ex, \"Lỗi.\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_ConditionalAccessGenericLog_WithALevelVariable — cùng receiver `?.`, cùng thông điệp,
    // khác đúng tên phương thức: `LogError("…")` thay vì `Log(level, ex, "…")`.
    [Fact]
    public void Detector_B28_Ignores_ConditionalAccessLogError_WithAStringMessage_NoGenericLogOverload()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                LoggerCallInCatch("logger?.LogError(\"Lỗi.\");"), "fake.cs")
            .ShouldBeEmpty();

    [Fact]
    public void Detector_B28_Catches_ExceptionParameterOfAnOperator_DotMessage()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                OperatorWithLog("logger.LogError(\"Lỗi: {M}\", e.Message);"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_ExceptionParameterOfAnOperator_DotMessage — cùng toán tử, cùng tham số `Exception e`,
    // khác đúng đối số log: FailureText.DescribeWithStack(e) thay vì `e.Message`.
    [Fact]
    public void Detector_B28_Ignores_ExceptionParameterOfAnOperator_InsideDescribeWithStack()
        => LoggerExceptionArgumentScanner.ScanLoggerCalls(
                OperatorWithLog("logger.LogError(\"Lỗi: {M}\", FailureText.DescribeWithStack(e));"), "fake.cs")
            .ShouldBeEmpty();

    // ===== Detector_B28_* (T1) — Vế 2: HealthCheckResult =====

    [Fact]
    public void Detector_B28_Catches_UnhealthyWithPositionalException()
        => LoggerExceptionArgumentScanner.ScanHealthCheckResultCalls(
                HealthCheckMethod("Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(\"Lỗi.\", ex)"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_UnhealthyWithPositionalException — cùng lời gọi Unhealthy, khác đúng việc có đối
    // số vị trí thứ hai hay không.
    [Fact]
    public void Detector_B28_Ignores_UnhealthyWithDescriptionOnly()
        => LoggerExceptionArgumentScanner.ScanHealthCheckResultCalls(
                HealthCheckMethod("Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(\"Lỗi.\")"),
                "fake.cs")
            .ShouldBeEmpty();

    [Fact]
    public void Detector_B28_Catches_DegradedWithNamedExceptionArgument()
        => LoggerExceptionArgumentScanner.ScanHealthCheckResultCalls(
                HealthCheckMethod("Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(\"Lỗi.\", exception: ex)"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_DegradedWithNamedExceptionArgument — cùng đối số tên `exception:`, khác đúng
    // giá trị: literal `null` tường minh thay vì `ex` — KHÔNG phải trao ngoại lệ.
    [Fact]
    public void Detector_B28_Ignores_DegradedWithExplicitNullException()
        => LoggerExceptionArgumentScanner.ScanHealthCheckResultCalls(
                HealthCheckMethod("Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(\"Lỗi.\", exception: null)"),
                "fake.cs")
            .ShouldBeEmpty();

    [Fact]
    public void Detector_B28_Catches_NewHealthCheckResult_WithPositionalException()
        => LoggerExceptionArgumentScanner.ScanHealthCheckResultCalls(
                HealthCheckMethod("new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult("
                    + "Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, \"Lỗi.\", ex)"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_NewHealthCheckResult_WithPositionalException — cùng lời gọi dựng, khác đúng việc
    // có đối số vị trí thứ ba (exception) hay không.
    [Fact]
    public void Detector_B28_Ignores_NewHealthCheckResult_WithoutException()
        => LoggerExceptionArgumentScanner.ScanHealthCheckResultCalls(
                HealthCheckMethod("new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult("
                    + "Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, \"Lỗi.\")"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_UnhealthyWithPositionalException — cùng hai đối số vị trí, khác đúng KIỂU nhận:
    // một lớp khác tên `Unhealthy` giống hệt chữ ký nhưng KHÔNG phải HealthCheckResult — ngoài tầm luật B28.
    [Fact]
    public void Detector_B28_Ignores_UnhealthyOnAnUnrelatedType()
        => LoggerExceptionArgumentScanner.ScanHealthCheckResultCalls(
                HealthCheckMethod("FakeUnrelatedResultBuilder.Unhealthy(\"Lỗi.\", ex)"), "fake.cs")
            .ShouldBeEmpty();

    // ===== Detector_B28_* (T1) — Vế 3: FailureText.Describe ở Warning trở lên =====

    [Fact]
    public void Detector_B28_Catches_FailureTextDescribe_InLogWarning()
        => LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(
                LoggerCallInCatch("logger.LogWarning(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.Describe(ex));"),
                "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_FailureTextDescribe_InLogError()
        => LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(
                LoggerCallInCatch("logger.LogError(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.Describe(ex));"),
                "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_B28_Catches_FailureTextDescribe_InLogCritical()
        => LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(
                LoggerCallInCatch("logger.LogCritical(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.Describe(ex));"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_FailureTextDescribe_InLogWarning — cùng đối số Describe(ex), khác đúng phương
    // thức được gọi: DescribeWithStack thay vì Describe.
    [Fact]
    public void Detector_B28_Ignores_DescribeWithStack_InLogWarning()
        => LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(
                LoggerCallInCatch("logger.LogWarning(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.DescribeWithStack(ex));"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_FailureTextDescribe_InLogWarning — cùng đối số Describe(ex), khác đúng mức log:
    // LogInformation thay vì LogWarning — Describe được PHÉP ở mức Information theo §5.
    [Fact]
    public void Detector_B28_Ignores_FailureTextDescribe_InLogInformation()
        => LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(
                LoggerCallInCatch("logger.LogInformation(\"Lỗi: {Failure}\", CoreAndSkill.Core.Application.Common.Logging.FailureText.Describe(ex));"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_B28_Catches_FailureTextDescribe_InLogWarning — cùng mức LogWarning, khác đúng dạng gọi:
    // Describe nằm trong phép nối `+` (vẫn phải bắt — "dò trong toàn bộ cây con của mỗi đối số").
    [Fact]
    public void Detector_B28_Catches_FailureTextDescribe_InsideAConcatenatedArgument()
        => LoggerExceptionArgumentScanner.ScanWarningOrHigherFailureTextDescribe(
                LoggerCallInCatch("logger.LogWarning(\"Lỗi: {Failure}\", \"tiền tố \" + CoreAndSkill.Core.Application.Common.Logging.FailureText.Describe(ex));"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // ---- khuôn nguồn ---------------------------------------------------------------------------------

    private static string LoggerCallInCatch(string logCall) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal sealed class FakeService
        {
            private readonly Microsoft.Extensions.Logging.ILogger logger = null!;

            public void Run()
            {
                try
                {
                    DoWork();
                }
                catch (System.Exception ex)
                {
                    {{logCall}}
                }
            }

            private static void DoWork() { }
        }
        """;

    private static string LoggerCallInMember(string parameterList, string body) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal sealed class FakeService
        {
            private readonly Microsoft.Extensions.Logging.ILogger logger = null!;

            public void Run({{parameterList}})
            {
                {{body}}
            }
        }
        """;

    private static string OperatorWithLog(string logCall) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal sealed class X
        {
            private static readonly Microsoft.Extensions.Logging.ILogger logger = null!;

            public static X operator +(X a, Exception e)
            {
                {{logCall}}
                return a;
            }
        }
        """;

    private static string HealthCheckMethod(string returnExpression) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal sealed class FakeHealthCheck
        {
            public Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult Run(System.Exception ex)
                => {{returnExpression}};
        }
        """;

    // ---- tầm quét --------------------------------------------------------------------------------------

    private static IReadOnlyList<string> ScannedFiles() => ProductSourceFiles.Core();

    private static string Format(LoggerExceptionArgumentScanner.Hit hit)
        => $"{ProductSourceFiles.RelativePath(hit.FilePath)}:{hit.Line} · {hit.Snippet} — {hit.Reason}";
}
