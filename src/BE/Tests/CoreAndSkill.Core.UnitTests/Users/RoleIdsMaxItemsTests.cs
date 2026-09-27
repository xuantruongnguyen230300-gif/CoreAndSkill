using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Users;
using FluentValidation;
using FluentValidation.Results;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

// docs/contracts/users.md §5, §7 và "Ghi chú — trần 50 phần tử của roleIds": hàng rào KỸ THUẬT chặn kích thước payload và
// giữ phép kiểm leo thang đặc quyền trong khối lượng có trần. Vượt trần ⇒ fieldErrors["RoleIds"] mã CORE.VALIDATION.MAX_ITEMS,
// messageParams khoá MaxItems (docs/quy-uoc/be-cqrs-handler.md §7.1) — cùng hình dạng ở cả hai lệnh.
public class RoleIdsMaxItemsTests
{
    private const int Cap = 50;

    private static IReadOnlyList<Guid> Ids(int count) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

    private static ValidationResult ValidateCreate(int count)
        => new CreateUserCommandValidator().Validate(new CreateUserCommand("an.nv", "an@vd.vn", "Nguyễn Văn An", "Temp@123", Ids(count)));

    private static ValidationResult ValidateAssign(int count)
        => new AssignUserRolesCommandValidator().Validate(new AssignUserRolesCommand(Guid.NewGuid(), Ids(count), "v1"));

    public static TheoryData<string, Func<int, ValidationResult>> Validators => new()
    {
        { "create", ValidateCreate },
        { "assign", ValidateAssign },
    };

    [Theory]
    [MemberData(nameof(Validators))]
    public void ExactlyAtTheCap_IsValid(string name, Func<int, ValidationResult> validate)
    {
        validate(Cap).IsValid.ShouldBeTrue($"{name}: đúng trần phải hợp lệ — trần là 'tối đa', không phải 'ít hơn'");
    }

    [Theory]
    [MemberData(nameof(Validators))]
    public void OneOverTheCap_FailsOnRoleIds_WithMaxItemsCode_AndTheCapAsParam(string name, Func<int, ValidationResult> validate)
    {
        var result = validate(Cap + 1);

        result.IsValid.ShouldBeFalse(name);
        var failure = result.Errors.ShouldHaveSingleItem();
        failure.PropertyName.ShouldBe("RoleIds");
        failure.ErrorCode.ShouldBe(CommonErrors.MaxItems.Code);

        // Đúng thứ ValidationBehavior đưa lên dây: chỉ khoá trong allowlist, giá trị là chuỗi.
        var wire = MessageParamPolicy.Filter(failure.FormattedMessagePlaceholderValues);
        wire.ShouldContainKeyAndValue("MaxItems", "50");
        wire.ShouldNotContainKey("PropertyValue");
    }

    // Null vẫn là REQUIRED, không phải MAX_ITEMS — hai rule không được dẫm lên nhau (và rule trần không được ném vì null).
    [Theory]
    [MemberData(nameof(Validators))]
    public void Null_IsRequired_NotMaxItems(string name, Func<int, ValidationResult> validate)
    {
        _ = validate;
        var result = name == "create"
            ? new CreateUserCommandValidator().Validate(new CreateUserCommand("an.nv", "an@vd.vn", "An", "Temp@123", null!))
            : new AssignUserRolesCommandValidator().Validate(new AssignUserRolesCommand(Guid.NewGuid(), null!, "v1"));

        result.Errors.Select(e => e.ErrorCode).ShouldBe([CommonErrors.Required.Code]);
    }
}
