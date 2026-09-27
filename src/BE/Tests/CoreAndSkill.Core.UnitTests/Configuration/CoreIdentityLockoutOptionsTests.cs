using CoreAndSkill.Core.Application.Configuration;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Configuration;

public class CoreIdentityLockoutOptionsTests
{
    [Fact]
    public void SectionName_IsCoreIdentityLockout()
    {
        CoreIdentityLockoutOptions.SectionName.ShouldBe("Core:Identity:Lockout");
    }

    [Fact]
    public void Defaults_MatchDocumentedPolicy()
    {
        // docs/wiki-core/be/02-identity-auth.md §4.2 — 5 lần sai, khoá 15 phút.
        var options = new CoreIdentityLockoutOptions();

        options.MaxFailedAttempts.ShouldBe(5);
        options.DurationMinutes.ShouldBe(15);
    }
}
