using CoreAndSkill.Core.Application.Configuration;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Configuration;

public class CoreIdentityPasswordOptionsTests
{
    [Fact]
    public void SectionName_IsCoreIdentityPassword()
    {
        CoreIdentityPasswordOptions.SectionName.ShouldBe("Core:Identity:Password");
    }

    [Fact]
    public void Defaults_MatchDocumentedPolicy()
    {
        var options = new CoreIdentityPasswordOptions();

        options.RequiredLength.ShouldBe(8);
        options.RequireDigit.ShouldBeTrue();
        options.RequireLowercase.ShouldBeTrue();
        options.RequireUppercase.ShouldBeFalse();
        options.RequireNonAlphanumeric.ShouldBeFalse();
        options.RequiredUniqueChars.ShouldBe(1);
    }
}
