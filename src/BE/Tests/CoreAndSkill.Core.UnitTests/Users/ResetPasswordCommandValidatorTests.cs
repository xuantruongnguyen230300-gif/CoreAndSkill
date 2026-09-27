using CoreAndSkill.Core.Application.Users;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = _validator.Validate(new ResetPasswordCommand(Guid.NewGuid(), "Temp@123", "v1"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyTempPassword_IsInvalid()
    {
        var result = _validator.Validate(new ResetPasswordCommand(Guid.NewGuid(), "", "v1"));

        result.IsValid.ShouldBeFalse();
    }
}
