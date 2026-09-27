using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

public class ChangePasswordRequiredCommandValidatorTests
{
    private readonly ChangePasswordRequiredCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new ChangePasswordRequiredCommand("Temp!Passw0rd", "New!Passw0rd"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NewPasswordSameAsCurrent_Fails()
    {
        var result = _validator.Validate(new ChangePasswordRequiredCommand("Same!Passw0rd", "Same!Passw0rd"));

        result.Errors.ShouldContain(e =>
            e.PropertyName == "NewPassword" && e.ErrorCode == AuthErrors.NewPasswordSameAsCurrent.Code);
    }

    [Fact]
    public void Validate_EmptyNewPassword_FailsWithRequired()
    {
        var result = _validator.Validate(new ChangePasswordRequiredCommand("Temp!Passw0rd", ""));

        result.Errors.ShouldContain(e => e.PropertyName == "NewPassword" && e.ErrorCode == CommonErrors.Required.Code);
    }
}
