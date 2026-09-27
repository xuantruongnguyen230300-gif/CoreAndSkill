using CoreAndSkill.Core.Application.Users;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = _validator.Validate(new CreateUserCommand("an.nv", "an@vd.vn", "Nguyễn Văn An", "Temp@123", []));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "an@vd.vn", "An", "Temp@123")]
    [InlineData("an.nv", "", "An", "Temp@123")]
    [InlineData("an.nv", "not-an-email", "An", "Temp@123")]
    [InlineData("an.nv", "an@vd.vn", "", "Temp@123")]
    [InlineData("an.nv", "an@vd.vn", "An", "")]
    public void Validate_InvalidFields_IsInvalid(string userName, string email, string fullName, string tempPassword)
    {
        var result = _validator.Validate(new CreateUserCommand(userName, email, fullName, tempPassword, []));

        result.IsValid.ShouldBeFalse();
    }

    // Danh tính hệ thống (SystemActor) — mọi biến thể hoa thường / khoảng trắng; lỗi nằm ở đúng ô UserName, đúng mã.
    [Theory]
    [InlineData("system")]
    [InlineData("System")]
    [InlineData("SYSTEM")]
    [InlineData(" system ")]
    [InlineData("\tSyStEm")]
    public void Validate_ReservedSystemUserName_IsInvalid_OnUserName_WithUsernameReserved(string userName)
    {
        var result = _validator.Validate(new CreateUserCommand(userName, "an@vd.vn", "Nguyễn Văn An", "Temp@123", []));

        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            e => e.PropertyName.ShouldBe(nameof(CreateUserCommand.UserName)),
            e => e.ErrorCode.ShouldBe(UserErrors.UsernameReserved.Code));
    }

    // Đối chứng: tên CHỨA "system" nhưng không phải nó — hợp lệ.
    [Theory]
    [InlineData("system1")]
    [InlineData("quantri.system")]
    public void Validate_NameMerelyContainingSystem_IsValid(string userName)
        => _validator.Validate(new CreateUserCommand(userName, "an@vd.vn", "Nguyễn Văn An", "Temp@123", [])).IsValid.ShouldBeTrue();

    [Fact]
    public void Validate_NullRoleIds_IsInvalid()
    {
        var result = _validator.Validate(new CreateUserCommand("an.nv", "an@vd.vn", "An", "Temp@123", null!));

        result.IsValid.ShouldBeFalse();
    }
}
