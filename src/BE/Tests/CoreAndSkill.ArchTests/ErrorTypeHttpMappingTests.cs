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
        ResultToHttpMapper.ToStatusCode(type).ShouldBe(expectedStatus);
    }

    // T6 — bảng trên phải phủ MỌI giá trị enum thật, không phải một tập con quên cập nhật.
    [Fact]
    public void ExpectedMappings_CoversEveryErrorTypeValue()
    {
        var allValues = Enum.GetValues<ErrorType>().ToHashSet();
        var coveredValues = ExpectedMappings.Select(row => (ErrorType)row[0]!).ToHashSet();

        coveredValues.ShouldBe(allValues, ignoreOrder: true);
    }
}
