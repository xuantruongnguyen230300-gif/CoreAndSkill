using CoreAndSkill.Core.Application.Users;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = _validator.Validate(new UpdateUserCommand(Guid.NewGuid(), "an@vd.vn", "An", "v1"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "An")]
    [InlineData("not-an-email", "An")]
    [InlineData("an@vd.vn", "")]
    public void Validate_InvalidFields_IsInvalid(string email, string fullName)
    {
        var result = _validator.Validate(new UpdateUserCommand(Guid.NewGuid(), email, fullName, "v1"));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_NullVersion_IsStillValid()
    {
        var result = _validator.Validate(new UpdateUserCommand(Guid.NewGuid(), "an@vd.vn", "An", null));

        result.IsValid.ShouldBeTrue();
    }
}
