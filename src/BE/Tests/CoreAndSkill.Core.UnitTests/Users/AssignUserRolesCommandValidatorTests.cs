using CoreAndSkill.Core.Application.Users;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class AssignUserRolesCommandValidatorTests
{
    private readonly AssignUserRolesCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyList_IsValid()
    {
        var result = _validator.Validate(new AssignUserRolesCommand(Guid.NewGuid(), [], "v1"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullList_IsInvalid()
    {
        var result = _validator.Validate(new AssignUserRolesCommand(Guid.NewGuid(), null!, "v1"));

        result.IsValid.ShouldBeFalse();
    }
}
