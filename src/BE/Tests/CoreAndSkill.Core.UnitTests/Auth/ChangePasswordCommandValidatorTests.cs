using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Old!Passw0rd", "New!Passw0rd"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyCurrentPassword_FailsWithRequired()
    {
        var result = _validator.Validate(new ChangePasswordCommand("", "New!Passw0rd"));

        result.Errors.ShouldContain(e => e.PropertyName == "CurrentPassword" && e.ErrorCode == CommonErrors.Required.Code);
    }

    [Fact]
    public void Validate_NewPasswordSameAsCurrent_FailsWithDedicatedCode()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Same!Passw0rd", "Same!Passw0rd"));

        result.Errors.ShouldContain(e =>
            e.PropertyName == "NewPassword" && e.ErrorCode == AuthErrors.NewPasswordSameAsCurrent.Code);
    }
}
