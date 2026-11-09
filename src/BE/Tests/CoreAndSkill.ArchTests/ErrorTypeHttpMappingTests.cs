using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// R4 — docs/RULES.md R4. Bảng ánh xạ định nghĩa gốc: docs/quy-uoc/be-api-controller.md §1.1.
public class ErrorTypeHttpMappingTests
{
    public static TheoryData<ErrorType, int> ExpectedMappings => new()
    {
        { ErrorType.Validation, StatusCodes.Status400BadRequest },
        { ErrorType.Unauthorized, StatusCodes.Status401Unauthorized },
        { ErrorType.Forbidden, StatusCodes.Status403Forbidden },
        { ErrorType.NotFound, StatusCodes.Status404NotFound },
        { ErrorType.Conflict, StatusCodes.Status409Conflict },
        { ErrorType.BusinessRule, StatusCodes.Status422UnprocessableEntity },
        { ErrorType.Unexpected, StatusCodes.Status500InternalServerError },
    };

    [Theory]
    [MemberData(nameof(ExpectedMappings))]
    public void EveryErrorType_MapsTo_AValidHttpStatus(ErrorType type, int expectedStatus)
    {
        ErrorTypeHttpMappingScanner.FindViolation(ResultToHttpMapper.ToStatusCode, type, expectedStatus).ShouldBeNull();
    }

    // T6 — bảng trên phải phủ MỌI giá trị enum thật, không phải một tập con quên cập nhật.
    [Fact]
    public void ExpectedMappings_CoversEveryErrorTypeValue()
    {
        var allValues = Enum.GetValues<ErrorType>().ToHashSet();
        var coveredValues = ExpectedMappings.Select(row => (ErrorType)row[0]!).ToHashSet();

        coveredValues.ShouldBe(allValues, ignoreOrder: true);
    }

    // Cặp bắt / bỏ qua của bộ dò R4 (luật T1). Hàm ánh xạ giả đi qua đúng ErrorTypeHttpMappingScanner mà
    // EveryErrorType_MapsTo_AValidHttpStatus chạy trên ResultToHttpMapper thật.
    // T12:cap-xanh-do — mỗi ca Ignores có ca Catches cùng đường dò, khác đúng một vi phạm: ca xanh
    // "hàm giả trả đúng status" cặp với ca đỏ "cùng hàm giả trả status khác".

    // Cặp xanh: Detector_R4_Ignores_AMappingThatReturnsTheTableStatus.
    [Fact]
    public void Detector_R4_Catches_AMappingThatReturnsTheWrongStatus()
    {
        var violation = ErrorTypeHttpMappingScanner.FindViolation(
            _ => StatusCodes.Status200OK, ErrorType.NotFound, StatusCodes.Status404NotFound);

        violation.ShouldNotBeNull();
        violation.ShouldContain("NotFound");
    }

    [Fact]
    public void Detector_R4_Catches_AMappingThatThrowsForAnUnmappedValue()
    {
        var violation = ErrorTypeHttpMappingScanner.FindViolation(
            _ => throw new InvalidOperationException("chưa ánh xạ"), ErrorType.Conflict, StatusCodes.Status409Conflict);

        violation.ShouldNotBeNull();
        violation.ShouldContain("InvalidOperationException");
    }

    // Cặp đỏ: Detector_R4_Catches_AMappingThatReturnsTheWrongStatus.
    [Fact]
    public void Detector_R4_Ignores_AMappingThatReturnsTheTableStatus()
    {
        var violation = ErrorTypeHttpMappingScanner.FindViolation(
            _ => StatusCodes.Status404NotFound, ErrorType.NotFound, StatusCodes.Status404NotFound);

        violation.ShouldBeNull();
    }

    // Cặp đỏ: Detector_R4_Catches_AMappingThatReturnsTheWrongStatus — cùng hàm giả sai ở NotFound, ở đây
    // hỏi về giá trị khác: bộ dò chấm từng giá trị riêng, không đổ vi phạm của giá trị này sang giá trị kia.
    [Fact]
    public void Detector_R4_Ignores_AWrongMappingForAnotherValue_WhenAskedAboutACorrectOne()
    {
        Func<ErrorType, int> map = t => t == ErrorType.NotFound ? StatusCodes.Status200OK : StatusCodes.Status409Conflict;

        ErrorTypeHttpMappingScanner.FindViolation(map, ErrorType.Conflict, StatusCodes.Status409Conflict).ShouldBeNull();
    }
}
