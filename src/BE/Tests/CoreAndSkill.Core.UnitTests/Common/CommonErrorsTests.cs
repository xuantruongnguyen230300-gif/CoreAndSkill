using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

// Catalog dùng chung — mã là hợp đồng công khai, đổi mã là breaking change
// (docs/quy-uoc/be-cqrs-handler.md §7.3). Một test khẳng định đúng mã + ErrorType là hàng rào
// chống gõ nhầm ở catalog, thứ không lỗi biên dịch mà chỉ lộ ra khi FE tra bảng dịch không thấy.
public class CommonErrorsTests
{
    [Fact]
    public void ValidationFailed_HasExpectedCodeAndType()
    {
        CommonErrors.ValidationFailed.Code.ShouldBe("CORE.VALIDATION.FAILED");
        CommonErrors.ValidationFailed.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void ConcurrencyConflict_HasExpectedCodeAndType()
    {
        CommonErrors.ConcurrencyConflict.Code.ShouldBe("CORE.CONCURRENCY.CONFLICT");
        CommonErrors.ConcurrencyConflict.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void Unexpected_HasExpectedCodeAndType()
    {
        CommonErrors.Unexpected.Code.ShouldBe("CORE.SYSTEM.UNEXPECTED");
        CommonErrors.Unexpected.Type.ShouldBe(ErrorType.Unexpected);
    }

    [Fact]
    public void Required_HasExpectedCodeAndType()
    {
        CommonErrors.Required.Code.ShouldBe("CORE.VALIDATION.REQUIRED");
        CommonErrors.Required.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void MaxLength_HasExpectedCodeAndType()
    {
        CommonErrors.MaxLength.Code.ShouldBe("CORE.VALIDATION.MAX_LENGTH");
        CommonErrors.MaxLength.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void MinLength_HasExpectedCodeAndType()
    {
        CommonErrors.MinLength.Code.ShouldBe("CORE.VALIDATION.MIN_LENGTH");
        CommonErrors.MinLength.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Format_HasExpectedCodeAndType()
    {
        CommonErrors.Format.Code.ShouldBe("CORE.VALIDATION.FORMAT");
        CommonErrors.Format.Type.ShouldBe(ErrorType.Validation);
    }

    // docs/quy-uoc/be-cqrs-handler.md §7.1 — trần số phần tử của một danh sách; tham số MaxItems (docs/contracts/users.md §5, §7).
    [Fact]
    public void MaxItems_HasExpectedCodeAndType_AndNamesTheMaxItemsParam()
    {
        CommonErrors.MaxItems.Code.ShouldBe("CORE.VALIDATION.MAX_ITEMS");
        CommonErrors.MaxItems.Type.ShouldBe(ErrorType.Validation);
        CommonErrors.MaxItems.MessageTemplate.ShouldContain("{MaxItems}");
    }
}
