using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Luật R8 (docs/RULES-BE.md) — định nghĩa gốc: docs/quy-uoc/be-cqrs-handler.md §7.2. Vì sao cổng tồn tại, bảy
// bước của bộ dò, và điểm mù đã biết: đầu Support/NoUserFacingVietnameseStringScanner.cs — đọc ở đó, không chép
// lại ở đây.
//
// Tầm quét: ProductSourceFiles.Core() — năm project Core cộng host, cùng khuôn mọi cổng quét cú pháp khác
// (SystemActorLiteralTests, LoggerExceptionArgumentTests).
//
// Quy ước tên (docs/wiki-core/be/04-testing-strategy.md): Catches_ = bộ dò báo đỏ (có hit); Ignores_ = bộ dò bỏ qua (rỗng).
//
// T12:cap-xanh-do — mọi ca Detector_R8_* kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng phép dò, khác đúng một
// chỗ (docs/RULES.md §8 luật T12); chú thích "Cặp đỏ:"/"Cặp xanh:" nói cặp ở đâu.
public class NoUserFacingVietnameseStringTests
{
    // ===== Luật thật =====

    [Fact]
    public void NoUserFacingVietnameseString_InBackend()
    {
        var offenders = ScannedFiles()
            .SelectMany(file => NoUserFacingVietnameseStringScanner.Scan(File.ReadAllText(file), file))
            .Select(hit => $"{ProductSourceFiles.RelativePath(hit.FilePath)}:{hit.Line} · {hit.Literal} — chuỗi tiếng Việt "
                         + "không khớp hình dạng nào trong tám hình dạng cho phép (docs/quy-uoc/be-cqrs-handler.md §7.2). "
                         + "Viết lại vào một hình dạng có sẵn, hoặc xin người dùng thêm hình dạng.")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // ===== T6 — chốt chống xanh rỗng =====

    // Bước 6 của bộ dò: tổng literal tiếng Việt đã xét và số khớp từng hình dạng phải đọc được từ thông điệp đỏ.
    // FAIL khi tổng về 0 (bộ nhận chữ tiếng Việt hỏng, hay tầm quét rỗng) hay hình dạng 1 về 0 (bộ đọc catalog
    // Error hỏng — catalog luôn mang câu tiếng Việt, §7.1).
    [Fact]
    public void TheGate_SeesRealVietnameseLiterals_AndTheCatalogShape_OnRealSource()
    {
        var classifications = ScannedFiles()
            .SelectMany(file => NoUserFacingVietnameseStringScanner.Classify(File.ReadAllText(file), file))
            .ToList();

        var total = classifications.Count;
        var perShape = Enum.GetValues<NoUserFacingVietnameseStringScanner.Shape>()
            .Select(shape => (Shape: shape, Count: classifications.Count(c => c.Shape == shape)))
            .ToList();
        var summary = string.Join(", ", perShape.Select(p => $"{p.Shape}={p.Count}"))
                      + $", viPham={classifications.Count(c => c.Shape is null)}";

        total.ShouldBeGreaterThan(0,
            $"không literal tiếng Việt nào được xét trên source thật — tầm quét rỗng hay bộ nhận chữ tiếng Việt hỏng. Tổng xét: {total}. Theo hình dạng: {summary}");

        perShape.Single(p => p.Shape == NoUserFacingVietnameseStringScanner.Shape.ErrorCatalogMessage).Count
            .ShouldBeGreaterThan(0,
                $"hình dạng 1 (thông điệp Error trong catalog) về 0 trên source thật — bộ đọc catalog hỏng. Tổng xét: {total}. Theo hình dạng: {summary}");
    }

    // ===== Đóng mặc định — nhận diện NFD như dạng dựng sẵn (bước 3) =====

    // Cùng một chuỗi viết ở dạng PHÂN TÁCH (NFD — nguyên âm + dấu thanh là hai code point) phải được nhận diện
    // tiếng Việt y hệt dạng đã GHÉP SẴN (NFC) — bước 3 đòi chuẩn hoá NFC trước khi so.
    [Fact]
    public void Detector_R8_RecognizesAnNfdWrittenVietnameseLiteral_TheSameAsItsPrecomposedForm()
    {
        // "à" viết bằng hai code point: 'a' (U+0061) + combining grave accent (U+0300) — KHÔNG phải 'à' (U+00E0).
        const string nfdChao = "chào"; // "chào" ở dạng NFD
        var source = ErrorsClass($"new Error(\"CODE\", \"Xin {nfdChao}.\", ErrorType.Validation)");

        var classifications = NoUserFacingVietnameseStringScanner.Classify(source, "fake.cs");

        classifications.ShouldContain(c => c.Shape == NoUserFacingVietnameseStringScanner.Shape.ErrorCatalogMessage);
    }

    // Ca NFD KHÔNG có dấu thanh — chỉ có dấu mũ. "Lô" = 'o' (U+006F) + combining circumflex (U+0302): chỉ sau chuẩn hoá NFC
    // mới thành 'ô' (U+00F4, một trong bảy chữ riêng). Bỏ bước NFC thì ca này đỏ (ca có dấu thanh ở trên vẫn qua nhờ
    // bước phân tách lại để lộ combining mark — nên không ghim được bước NFC).
    [Fact]
    public void Detector_R8_RecognizesAnNfdVietnameseLiteral_WithOnlyAHatMark_NoToneMark()
    {
        const string nfdLo = "Lo\u0302"; // L + o + combining U+0302
        var source = ErrorsClass($"new Error(\"CODE\", \"{nfdLo}\", ErrorType.Validation)");

        NoUserFacingVietnameseStringScanner.Classify(source, "fake.cs")
            .ShouldContain(c => c.Shape == NoUserFacingVietnameseStringScanner.Shape.ErrorCatalogMessage);
    }

    // Cặp xanh: Detector_R8_RecognizesAnNfdVietnameseLiteral_WithOnlyAHatMark_NoToneMark — cùng khuôn,
    // khác đúng một ký tự: không có combining U+0302 ("Lo") nên không phải tiếng Việt, không được xét.
    [Fact]
    public void Detector_R8_Ignores_PlainAsciiLo_WithoutTheCombiningHatMark()
    {
        var source = ErrorsClass("new Error(\"CODE\", \"Lo\", ErrorType.Validation)");

        NoUserFacingVietnameseStringScanner.Classify(source, "fake.cs").ShouldBeEmpty();
    }

    // Chú thích tiếng Việt không bao giờ vào tầm — Roslyn coi comment là trivia, DescendantNodes() không đi qua
    // nó (docs/wiki-core/be/04-testing-strategy.md §3.3, "Unicode và dấu tiếng Việt").
    [Fact]
    public void Detector_R8_Ignores_AVietnameseLineComment()
        => NoUserFacingVietnameseStringScanner.Classify(
                Member("// Đây là một ghi chú tiếng Việt không liên quan gì tới người dùng."), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_R8_Ignores_AVietnameseLineComment — CÙNG câu, cùng chỗ trong khuôn, khác đúng một chỗ: viết thành
    // literal chuỗi thay vì chú thích.
    [Fact]
    public void Detector_R8_Catches_TheSameSentence_WrittenAsAStringLiteral_InsteadOfAComment()
        => NoUserFacingVietnameseStringScanner.Classify(
                Member("public string M() => \"Đây là một ghi chú tiếng Việt không liên quan gì tới người dùng.\";"), "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Literal ký tự đơn nằm NGOÀI tầm (người dùng chốt 2026-10-01, §7.2 bước 3) =====

    // Cặp xanh: ký tự đơn tiếng Việt dùng để so khớp, ở vị trí không thuộc hình dạng nào — không bị quét.
    // Cặp đỏ: Detector_R8_Catches_SingleCharacterString_AtTheSamePositionAsACharLiteral.
    [Fact]
    public void Detector_R8_Ignores_ACharacterLiteral_OutsideEveryShape()
        => NoUserFacingVietnameseStringScanner.Classify(
                Member("public bool M(char c) { return c == 'đ' || c is 'Đ'; }"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG vị trí (vế so sánh), khác đúng loại literal — chuỗi một ký tự "đ" thay vì ký tự 'đ'.
    [Fact]
    public void Detector_R8_Catches_SingleCharacterString_AtTheSamePositionAsACharLiteral()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public bool M(string s) { return s == \"đ\"; }"), "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 1: thông điệp Error trong catalog =====

    [Fact]
    public void Detector_R8_Ignores_ErrorCatalogMessage_AsExplicitNewError()
        => NoUserFacingVietnameseStringScanner.Scan(
                ErrorsClass("new Error(\"CORE.X.Y\", \"Dữ liệu gửi lên không hợp lệ.\", ErrorType.Validation)"), "fake.cs")
            .ShouldBeEmpty();

    [Fact]
    public void Detector_R8_Ignores_ErrorCatalogMessage_AsImplicitShortNew()
        => NoUserFacingVietnameseStringScanner.Scan(
                ErrorsClass("new(\"CORE.X.Y\", \"Dữ liệu gửi lên không hợp lệ.\", ErrorType.Validation)"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: vị trí Vietnamese lệch sang đối số Code (vị trí 0) thay vì MessageTemplate (vị trí 1) — cùng lời
    // gọi, khác đúng đối số nào mang tiếng Việt.
    [Fact]
    public void Detector_R8_Catches_VietnameseAtTheCodePosition_OfNewError()
        => NoUserFacingVietnameseStringScanner.Scan(
                ErrorsClass("new Error(\"Mã lỗi tiếng Việt\", \"message\", ErrorType.Validation)"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: cùng `new(...)` ở vị trí MessageTemplate, khác đúng tên lớp bao quanh — không kết thúc bằng
    // "Errors" nên không được công nhận là catalog.
    [Fact]
    public void Detector_R8_Catches_ErrorCatalogMessage_WhenEnclosingClassIsNotNamedErrors()
        => NoUserFacingVietnameseStringScanner.Scan(
                $$"""
                namespace CoreAndSkill.Core.Fake;

                internal static class FakeCatalog
                {
                    public static readonly Error Required = new("CORE.X.Y", "Dữ liệu gửi lên không hợp lệ.", ErrorType.Validation);
                }
                """, "fake.cs")
            .ShouldNotBeEmpty();

    // Ba điều kiện của hình dạng 1, mỗi ca đỏ khác một ca xanh đúng MỘT chỗ.
    // Cặp xanh: Detector_R8_Ignores_ErrorCatalogMessage_AsImplicitShortNew — cùng `new("…", "câu", …)` trong lớp
    // `static FakeErrors`, khác đúng kiểu khai của trường: `LabelText` thay vì `Error` (new rút gọn chỉ nhận khi
    // trực tiếp là giá trị khởi tạo của một trường khai kiểu Error).
    [Fact]
    public void Detector_R8_Catches_ImplicitShortNew_WhenTheFieldIsNotTypedError()
        => NoUserFacingVietnameseStringScanner.Scan(
                $$"""
                namespace CoreAndSkill.Core.Fake;

                internal static class FakeErrors
                {
                    public static readonly LabelText Hint = new("X", "Dữ liệu gửi lên không hợp lệ.");
                }
                """, "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp xanh: Detector_R8_Ignores_ErrorCatalogMessage_AsImplicitShortNew — cùng trường kiểu Error, cùng tên lớp
    // `FakeErrors`, khác đúng việc lớp không có `static`.
    [Fact]
    public void Detector_R8_Catches_ErrorCatalogMessage_WhenTheErrorsClassIsNotStatic()
        => NoUserFacingVietnameseStringScanner.Scan(
                $$"""
                namespace CoreAndSkill.Core.Fake;

                internal class FakeErrors
                {
                    public static readonly Error Required = new("CORE.X.Y", "Dữ liệu gửi lên không hợp lệ.", ErrorType.Validation);
                }
                """, "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp xanh: Detector_R8_Ignores_ErrorCatalogMessage_AsExplicitNewError — cùng `new T("X", "câu")` trong lớp
    // `static FakeErrors`, khác đúng tên kiểu được dựng: `Foo` thay vì `Error`.
    [Fact]
    public void Detector_R8_Catches_ExplicitNew_WhenTheCreatedTypeIsNotError()
        => NoUserFacingVietnameseStringScanner.Scan(
                ErrorsClass("new Foo(\"X\", \"Dữ liệu gửi lên không hợp lệ.\")"), "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 2: template của ILogger =====

    [Fact]
    public void Detector_R8_Ignores_LoggerTemplate_AsFirstArgumentOfLogError()
        => NoUserFacingVietnameseStringScanner.Scan(
                LoggerCall("logger.LogError(\"Việc {JobId} lỗi.\", jobId);"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: cùng lời gọi LogError hai đối số, khác đúng VỊ TRÍ mang tiếng Việt — đối số thứ hai thay vì đầu.
    [Fact]
    public void Detector_R8_Catches_VietnameseAtTheSecondArgument_OfLogError()
        => NoUserFacingVietnameseStringScanner.Scan(
                LoggerCall("logger.LogError(jobId, \"Việc lỗi.\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // Lời gọi qua `?.` cùng nghĩa với `.` — hình dạng 2.
    // Cặp đỏ: Detector_R8_Catches_VietnameseAtTheSecondArgument_OfConditionalAccessLogError.
    [Fact]
    public void Detector_R8_Ignores_LoggerTemplate_AsFirstArgumentOfConditionalAccessLogError()
        => NoUserFacingVietnameseStringScanner.Scan(
                LoggerCall("logger?.LogError(\"Việc {JobId} lỗi.\", jobId);"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_R8_Ignores_LoggerTemplate_AsFirstArgumentOfConditionalAccessLogError — cùng `?.LogError`, khác
    // đúng vị trí mang tiếng Việt: đối số thứ hai.
    [Fact]
    public void Detector_R8_Catches_VietnameseAtTheSecondArgument_OfConditionalAccessLogError()
        => NoUserFacingVietnameseStringScanner.Scan(
                LoggerCall("logger?.LogError(jobId, \"Việc lỗi.\");"), "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 3(a): thông điệp ngoại lệ bên trong `throw` =====

    [Fact]
    public void Detector_R8_Ignores_ExceptionMessage_DirectlyInsideThrow()
        => NoUserFacingVietnameseStringScanner.Scan(
                ThrowingMethod("throw new System.InvalidOperationException($\"Lỗi: {detail}\");"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: cùng thông điệp, khác đúng việc exception được DỰNG RỒI GÁN VÀO BIẾN trước khi throw — literal
    // không còn nằm bên trong biểu thức sau `throw`.
    [Fact]
    public void Detector_R8_Catches_ExceptionMessage_WhenConstructedBeforeThrow()
        => NoUserFacingVietnameseStringScanner.Scan(
                ThrowingMethod("var ex = new System.InvalidOperationException($\"Lỗi: {detail}\"); throw ex;"), "fake.cs")
            .ShouldNotBeEmpty();

    // Hàm phụ dựng ngoại lệ: thân hàm phụ đỏ (nằm ngoài throw), gọi nó trong throw thì xanh — đúng ví dụ ở bước 7
    // của bộ dò (docs/quy-uoc/be-cqrs-handler.md §7.2).
    [Fact]
    public void Detector_R8_Ignores_HelperFunctionCall_InsideThrow()
        => NoUserFacingVietnameseStringScanner.Scan(
                ThrowingMethod("throw Invalid($\"Nguồn seed đơn vị không hợp lệ: {detail}\");"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG chuỗi, nhưng nằm trong THÂN của hàm phụ dựng ngoại lệ — thân hàm phụ gọi được ở chỗ khác ngoài
    // `throw`, nên không được tính an toàn (lý do ở ly-do/be-cqrs-handler.md §7.2, mục "Cái giá của mở rộng").
    [Fact]
    public void Detector_R8_Catches_VietnameseInsideTheHelperFunctionBody_NotInsideThrow()
        => NoUserFacingVietnameseStringScanner.Scan(
                $$"""
                namespace CoreAndSkill.Core.Fake;

                internal sealed class FakeValidator
                {
                    private static System.Exception Invalid(string detail)
                        => new System.InvalidOperationException($"Nguồn seed đơn vị không hợp lệ: {detail}");
                }
                """, "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 3(b): `: base(…)` / `: <Tên>(…)` của lớp kế thừa *Exception =====

    [Fact]
    public void Detector_R8_Ignores_BaseConstructorMessage_InAClassDerivedFromException()
        => NoUserFacingVietnameseStringScanner.Scan(
                $$"""
                namespace CoreAndSkill.Core.Fake;

                internal sealed class FakeDomainException : System.Exception
                {
                    public FakeDomainException(string detail) : base($"Lỗi nghiệp vụ: {detail}") { }
                }
                """, "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: cùng `: base(…)`, khác đúng KIỂU CƠ SỞ — không kết thúc bằng "Exception".
    [Fact]
    public void Detector_R8_Catches_BaseConstructorMessage_WhenBaseTypeIsNotAnException()
        => NoUserFacingVietnameseStringScanner.Scan(
                $$"""
                namespace CoreAndSkill.Core.Fake;

                internal class FakeBase
                {
                    public FakeBase(string detail) { }
                }

                internal sealed class FakeThing : FakeBase
                {
                    public FakeThing(string detail) : base($"Lỗi nghiệp vụ: {detail}") { }
                }
                """, "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_R8_Ignores_PrimaryConstructorBaseMessage_InAClassDerivedFromException()
        => NoUserFacingVietnameseStringScanner.Scan(
                "namespace CoreAndSkill.Core.Fake;\n\n"
                + "internal sealed class FakePrimaryException(string detail)\n"
                + "    : System.Exception($\"Lỗi nghiệp vụ: {detail}\");\n", "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: cùng hàm dựng chính với một đối số, khác đúng KIỂU CƠ SỞ — không kết thúc bằng "Exception".
    [Fact]
    public void Detector_R8_Catches_PrimaryConstructorBaseMessage_WhenBaseTypeIsNotAnException()
        => NoUserFacingVietnameseStringScanner.Scan(
                "namespace CoreAndSkill.Core.Fake;\n\n"
                + "internal class FakeBase(string detail);\n\n"
                + "internal sealed class FakePrimaryThing(string detail)\n"
                + "    : FakeBase($\"Lỗi nghiệp vụ: {detail}\");\n", "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 4: đối số attribute =====

    [Fact]
    public void Detector_R8_Ignores_AttributeArgument()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("[FakeAttribute(\"Đổi mật khẩu của chính mình\")] public void M() { }"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG chuỗi, nhưng viết như một lời gọi phương thức bình thường — không nằm trong `[...]`.
    [Fact]
    public void Detector_R8_Catches_SameStringAsAnOrdinaryMethodCall_NotAnAttribute()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public void M() { FakeAttribute(\"Đổi mật khẩu của chính mình\"); }"), "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 5: mô tả chỉ số =====

    [Fact]
    public void Detector_R8_Ignores_MetricDescription_AsNamedArgumentOfMeterCreateCounter()
        => NoUserFacingVietnameseStringScanner.Scan(
                MeterField("Meter.CreateCounter<long>(\"core.fake\", description: \"Số lần đăng nhập thất bại.\")"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG chuỗi, nhưng viết Ở VỊ TRÍ (không mang tên `description:`) — hình dạng 5 chỉ nhận đối số TÊN.
    [Fact]
    public void Detector_R8_Catches_MetricDescription_WhenPassedPositionally()
        => NoUserFacingVietnameseStringScanner.Scan(
                MeterField("Meter.CreateCounter<long>(\"core.fake\", \"{login}\", \"Số lần đăng nhập thất bại.\")"), "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: cùng đối số tên `description:`, khác đúng RECEIVER — không phải `Meter`.
    [Fact]
    public void Detector_R8_Catches_MetricDescription_WhenReceiverIsNotMeter()
        => NoUserFacingVietnameseStringScanner.Scan(
                $$"""
                namespace CoreAndSkill.Core.Fake;

                internal static class FakeMetrics
                {
                    private static readonly FakeMeterFactory OtherThing = new();

                    public static readonly object Counter = OtherThing.CreateCounter<long>("core.fake", description: "Số lần đăng nhập thất bại.");
                }
                """, "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 6: mô tả health check =====

    [Fact]
    public void Detector_R8_Ignores_HealthCheckDescription_AsFirstArgumentOfUnhealthy()
        => NoUserFacingVietnameseStringScanner.Scan(
                HealthCheckMethod("HealthCheckResult.Unhealthy(\"Không kết nối được PostgreSQL.\")"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG chuỗi, khác đúng RECEIVER — không phải `HealthCheckResult`.
    [Fact]
    public void Detector_R8_Catches_HealthCheckDescription_WhenReceiverIsUnrelated()
        => NoUserFacingVietnameseStringScanner.Scan(
                HealthCheckMethod("FakeResultBuilder.Unhealthy(\"Không kết nối được PostgreSQL.\")"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_R8_Ignores_HealthCheckDescription_AsSecondArgumentOfNewHealthCheckResult()
        => NoUserFacingVietnameseStringScanner.Scan(
                HealthCheckMethod("new HealthCheckResult(HealthStatus.Unhealthy, \"Không kết nối được PostgreSQL.\")"), "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG `new HealthCheckResult(...)`, khác đúng VỊ TRÍ — đối số thứ ba (khe `exception`), không phải
    // thứ hai (khe `description`).
    [Fact]
    public void Detector_R8_Catches_NewHealthCheckResult_WhenVietnameseIsAtTheThirdPosition()
        => NoUserFacingVietnameseStringScanner.Scan(
                HealthCheckMethod("new HealthCheckResult(HealthStatus.Unhealthy, \"ok\", \"Không kết nối được PostgreSQL.\")"), "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 7: đầu ra của lệnh Core =====

    [Fact]
    public void Detector_R8_Ignores_ConsoleWriteLine_UnderCommandsDirectory()
        => NoUserFacingVietnameseStringScanner.Scan(
                ConsoleWriteMethod("Console.Error.WriteLineAsync(\"Thiếu động từ sau 'core'.\");"),
                "src/BE/Core/CoreAndSkill.Core.Web/Commands/FakeCommandRunner.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG lời gọi, khác đúng ĐƯỜNG DẪN tệp — ngoài thư mục Commands/.
    [Fact]
    public void Detector_R8_Catches_ConsoleWriteLine_OutsideCommandsDirectory()
        => NoUserFacingVietnameseStringScanner.Scan(
                ConsoleWriteMethod("Console.Error.WriteLineAsync(\"Thiếu động từ sau 'core'.\");"),
                "src/BE/Core/CoreAndSkill.Core.Web/Controllers/FakeController.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: CÙNG đường dẫn dưới Commands/, khác đúng RECEIVER — không phải `Console`.
    [Fact]
    public void Detector_R8_Catches_WriteLine_OnANonConsoleReceiver_UnderCommandsDirectory()
        => NoUserFacingVietnameseStringScanner.Scan(
                ConsoleWriteMethod("FakeWriter.WriteLine(\"Thiếu động từ sau 'core'.\");"),
                "src/BE/Core/CoreAndSkill.Core.Web/Commands/FakeCommandRunner.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Hình dạng 8: thông điệp kiểm cấu hình =====

    [Fact]
    public void Detector_R8_Ignores_ValidationResultConstructorMessage()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> "
                     + "Validate() { yield return new System.ComponentModel.DataAnnotations.ValidationResult("
                     + "\"Core:File:RootPath phải là đường dẫn tuyệt đối.\"); }"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG chuỗi, khác đúng TÊN KIỂU được dựng — không phải `ValidationResult`.
    [Fact]
    public void Detector_R8_Catches_SameMessage_OnAnUnrelatedConstructor()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public object M() => new FakeValidationResultWrapper(\"Core:File:RootPath phải là đường dẫn tuyệt đối.\");"),
                "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_R8_Ignores_ValidateOptionsResultFailMessage()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public Microsoft.Extensions.Options.ValidateOptionsResult M() "
                     + "=> Microsoft.Extensions.Options.ValidateOptionsResult.Fail(\"Core:File:RootPath là thư mục không ghi được.\");"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG chuỗi, khác đúng con đường tới nó — gom vào danh sách rồi mới `.Add(...)` (ví dụ đỏ hay gặp,
    // ly-do/be-cqrs-handler.md §7.2), thay vì trao thẳng cho `Fail(`.
    [Fact]
    public void Detector_R8_Catches_SameMessage_GatheredIntoAListBeforeFail()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public void M() { var failures = new System.Collections.Generic.List<string>(); "
                     + "failures.Add(\"Core:File:RootPath là thư mục không ghi được.\"); }"),
                "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_R8_Ignores_OptionsBuilderValidateFailureMessage()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public void M(Microsoft.Extensions.DependencyInjection.OptionsBuilder<FakeOptions> b) "
                     + "=> b.Validate(o => o.IsValid, \"Cấu hình không hợp lệ.\");"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG hai đối số, khác đúng THỨ TỰ — chuỗi đứng TRƯỚC, lambda đứng SAU, nên đối số đầu không còn là
    // lambda (điều kiện bắt buộc của hình dạng này).
    [Fact]
    public void Detector_R8_Catches_ValidateCall_WhenTheFirstArgumentIsNotALambda()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public void M(Microsoft.Extensions.DependencyInjection.OptionsBuilder<FakeOptions> b) "
                     + "=> b.Validate(\"Cấu hình không hợp lệ.\", o => o.IsValid);"),
                "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_R8_Ignores_FormatErrorMessageOverrideReturn()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public override string FormatErrorMessage(string name) => $\"{name}: là danh tính hệ thống.\";"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: CÙNG chữ ký và thân hàm, khác đúng việc có từ khoá `override` hay không.
    [Fact]
    public void Detector_R8_Catches_FormatErrorMessage_WithoutOverrideKeyword()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public string FormatErrorMessage(string name) => $\"{name}: là danh tính hệ thống.\";"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Đóng mặc định — ví dụ đỏ hay gặp (ly-do/be-cqrs-handler.md §7.2) =====

    [Fact]
    public void Detector_R8_Catches_AStringReturnedDirectlyFromAController()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public object Probe() => Ok(\"Xin chào\");"), "fake.cs")
            .ShouldNotBeEmpty();

    [Fact]
    public void Detector_R8_Catches_FluentValidationWithMessage()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public void M() { RuleFor(x => x.Name).NotEmpty().WithMessage(\"Bắt buộc\"); }"), "fake.cs")
            .ShouldNotBeEmpty();

    // ===== Detector_R8_* (T1) — Bước 4: leo qua nút trong suốt (ClimbThroughTransparentNodes) =====

    // Vế phải `??`: nút trong suốt, literal leo lên tới đối số của `Fail(`.
    [Fact]
    public void Detector_R8_Ignores_CoalesceRightOperand_InsideValidateOptionsResultFail()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public Microsoft.Extensions.Options.ValidateOptionsResult M(string x, bool c) => Microsoft.Extensions.Options.ValidateOptionsResult.Fail(x ?? \"Giá trị không hợp lệ.\");"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_R8_Ignores_CoalesceRightOperand_InsideValidateOptionsResultFail — cùng `??` và cùng `Fail(`,
    // khác đúng VẾ: literal ở vế TRÁI (vế trái không trong suốt).
    [Fact]
    public void Detector_R8_Catches_CoalesceLeftOperand_InsideValidateOptionsResultFail()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public Microsoft.Extensions.Options.ValidateOptionsResult M(string x, bool c) => Microsoft.Extensions.Options.ValidateOptionsResult.Fail(\"Giá trị không hợp lệ.\" ?? x);"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Cặp đỏ: Detector_R8_Ignores_CoalesceRightOperand_InsideValidateOptionsResultFail — cùng `x ?? "câu"`, khác
    // đúng chỗ đứng: trao cho `failures.Add(` thay vì `Fail(`.
    [Fact]
    public void Detector_R8_Catches_CoalesceRightOperand_WhenTheSlotIsNotAShape()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public void M(string x, bool c) { var failures = new System.Collections.Generic.List<string>(); failures.Add(x ?? \"Giá trị không hợp lệ.\"); }"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Nhánh `?:`: hai nhánh là nút trong suốt.
    [Fact]
    public void Detector_R8_Ignores_ConditionalBranch_InsideValidateOptionsResultFail()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public Microsoft.Extensions.Options.ValidateOptionsResult M(string x, bool c) => Microsoft.Extensions.Options.ValidateOptionsResult.Fail(c ? \"Giá trị không hợp lệ.\" : x);"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_R8_Ignores_ConditionalBranch_InsideValidateOptionsResultFail — cùng `?:` và cùng `Fail(`, khác
    // đúng chỗ của literal: ĐIỀU KIỆN (`"câu" == x`), không phải nhánh.
    [Fact]
    public void Detector_R8_Catches_Literal_InTheConditionOfATernary_InsideValidateOptionsResultFail()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public Microsoft.Extensions.Options.ValidateOptionsResult M(string x, bool c) => Microsoft.Extensions.Options.ValidateOptionsResult.Fail(\"Giá trị không hợp lệ.\" == x ? \"a\" : \"b\");"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Phép nối `+`: nút trong suốt.
    [Fact]
    public void Detector_R8_Ignores_StringConcatenation_InsideValidateOptionsResultFail()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public Microsoft.Extensions.Options.ValidateOptionsResult M(string x, bool c) => Microsoft.Extensions.Options.ValidateOptionsResult.Fail(\"Giá trị không hợp lệ: \" + x);"),
                "fake.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_R8_Ignores_StringConcatenation_InsideValidateOptionsResultFail — cùng phép nối `+`, khác đúng
    // chỗ đứng: `failures.Add(` thay vì `Fail(`.
    [Fact]
    public void Detector_R8_Catches_StringConcatenation_WhenTheSlotIsNotAShape()
        => NoUserFacingVietnameseStringScanner.Scan(
                Member("public void M(string x, bool c) { var failures = new System.Collections.Generic.List<string>(); failures.Add(\"Giá trị không hợp lệ: \" + x); }"),
                "fake.cs")
            .ShouldNotBeEmpty();

    // Lỗ `{…}` của chuỗi nội suy + nhánh `?:` + dấu ngoặc, dưới `Commands/` (hình dạng 7).
    [Fact]
    public void Detector_R8_Ignores_LiteralInAnInterpolationHoleTernary_ConsoleOutput_UnderCommandsDirectory()
        => NoUserFacingVietnameseStringScanner.Scan(
                ConsoleWriteMethod("bool c = true; string x = \"\"; Console.Error.WriteLineAsync($\"{(c ? \"Giá trị không hợp lệ.\" : x)}\");"),
                "src/BE/Core/CoreAndSkill.Core.Web/Commands/FakeCommandRunner.cs")
            .ShouldBeEmpty();

    // Cặp đỏ: Detector_R8_Ignores_LiteralInAnInterpolationHoleTernary_ConsoleOutput_UnderCommandsDirectory — cùng câu,
    // khác đúng ĐƯỜNG DẪN tệp: ngoài thư mục Commands/.
    [Fact]
    public void Detector_R8_Catches_LiteralInAnInterpolationHoleTernary_ConsoleOutput_OutsideCommandsDirectory()
        => NoUserFacingVietnameseStringScanner.Scan(
                ConsoleWriteMethod("bool c = true; string x = \"\"; Console.Error.WriteLineAsync($\"{(c ? \"Giá trị không hợp lệ.\" : x)}\");"),
                "src/BE/Core/CoreAndSkill.Core.Web/Controllers/FakeController.cs")
            .ShouldNotBeEmpty();

    // ---- khuôn nguồn ---------------------------------------------------------------------------------

    private static string Member(string member) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal sealed class FakeType
        {
            {{member}}
        }
        """;

    private static string ErrorsClass(string newErrorExpression) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal static class FakeErrors
        {
            public static readonly Error Required = {{newErrorExpression}};
        }
        """;

    private static string LoggerCall(string call) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal sealed class FakeService
        {
            private readonly Microsoft.Extensions.Logging.ILogger logger = null!;

            public void Run(int jobId)
            {
                {{call}}
            }
        }
        """;

    private static string ThrowingMethod(string body) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal sealed class FakeValidator
        {
            private static System.Exception Invalid(string detail)
                => new System.InvalidOperationException(detail);

            public void Run(string detail)
            {
                {{body}}
            }
        }
        """;

    private static string MeterField(string createExpression) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal static class FakeMetrics
        {
            private static readonly System.Diagnostics.Metrics.Meter Meter = new("Fake");

            public static readonly object Counter = {{createExpression}};
        }
        """;

    private static string HealthCheckMethod(string returnExpression) => $$"""
        namespace CoreAndSkill.Core.Fake;

        internal sealed class FakeHealthCheck
        {
            public Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult Run()
                => {{returnExpression}};
        }
        """;

    private static string ConsoleWriteMethod(string call) => $$"""
        namespace CoreAndSkill.Core.Web.Commands;

        internal sealed class FakeCommandRunner
        {
            public async System.Threading.Tasks.Task RunAsync()
            {
                {{call}}
            }
        }
        """;

    // ---- tầm quét --------------------------------------------------------------------------------------

    private static IReadOnlyList<string> ScannedFiles() => ProductSourceFiles.Core();
}
