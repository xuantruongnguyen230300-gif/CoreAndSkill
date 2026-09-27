using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new LoginCommand("SO-GD", "an.nv", "P@ssw0rd"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyTenantCode_FailsWithRequired()
    {
        var result = _validator.Validate(new LoginCommand("", "an.nv", "P@ssw0rd"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "TenantCode" && e.ErrorCode == CommonErrors.Required.Code);
    }

    // Trần của mã đơn vị — khuôn ở docs/contracts/tenants.md §2 (tối đa 50 ký tự, cột core.tenant.code). Không trần thì
    // chuỗi người gọi ẩn danh gửi lên đi nguyên vào khoá của bộ đếm đăng nhập (be-api-controller.md §6.5).
    [Fact]
    public void Validate_TenantCodeTooLong_FailsWithMaxLength_AndCarriesTheLimit()
    {
        var result = _validator.Validate(new LoginCommand(new string('A', 51), "an.nv", "P@ssw0rd"));

        var failure = result.Errors.ShouldHaveSingleItem();
        failure.PropertyName.ShouldBe("TenantCode");
        failure.ErrorCode.ShouldBe(CommonErrors.MaxLength.Code);
        failure.FormattedMessagePlaceholderValues["MaxLength"].ShouldBe(50);
    }

    [Fact]
    public void Validate_TenantCodeAtTheLimit_Succeeds()
    {
        var result = _validator.Validate(new LoginCommand(new string('A', 50), "an.nv", "P@ssw0rd"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyUserName_FailsWithRequired()
    {
        var result = _validator.Validate(new LoginCommand("SO-GD", "", "P@ssw0rd"));

        result.Errors.ShouldContain(e => e.PropertyName == "UserName" && e.ErrorCode == CommonErrors.Required.Code);
    }

    [Fact]
    public void Validate_UserNameTooLong_FailsWithMaxLength()
    {
        var result = _validator.Validate(new LoginCommand("SO-GD", new string('a', 257), "P@ssw0rd"));

        result.Errors.ShouldContain(e => e.PropertyName == "UserName" && e.ErrorCode == CommonErrors.MaxLength.Code);
    }

    [Fact]
    public void Validate_EmptyPassword_FailsWithRequired()
    {
        var result = _validator.Validate(new LoginCommand("SO-GD", "an.nv", ""));

        result.Errors.ShouldContain(e => e.PropertyName == "Password" && e.ErrorCode == CommonErrors.Required.Code);
    }
}
