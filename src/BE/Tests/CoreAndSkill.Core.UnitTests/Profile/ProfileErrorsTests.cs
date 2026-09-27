using CoreAndSkill.Core.Application.Profile;
using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Profile;

public class ProfileErrorsTests
{
    [Fact]
    public void PermissionBypassNotHeld_HasExpectedCodeAndType()
    {
        ProfileErrors.PermissionBypassNotHeld.Code.ShouldBe("CORE.PROFILE.PERMISSION_BYPASS_NOT_HELD");
        ProfileErrors.PermissionBypassNotHeld.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public void NoOtherPermissionAdmin_HasExpectedCodeAndType()
    {
        ProfileErrors.NoOtherPermissionAdmin.Code.ShouldBe("CORE.PROFILE.NO_OTHER_PERMISSION_ADMIN");
        ProfileErrors.NoOtherPermissionAdmin.Type.ShouldBe(ErrorType.BusinessRule);
    }
}
