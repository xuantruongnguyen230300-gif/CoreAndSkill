using CoreAndSkill.Core.Application.Configuration;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Configuration;

public class CoreAuthOptionsTests
{
    [Fact]
    public void SectionName_IsCoreAuth()
    {
        CoreAuthOptions.SectionName.ShouldBe("Core:Auth");
    }

    [Fact]
    public void SessionAbsoluteHours_DefaultsToTwelve()
    {
        new CoreAuthOptions { CookieName = "c", AntiforgeryCookieName = "a" }.SessionAbsoluteHours.ShouldBe(12);
    }

    [Fact]
    public void Properties_RoundTripValues()
    {
        var options = new CoreAuthOptions
        {
            CookieName = "session",
            AntiforgeryCookieName = "af",
            SessionMinutes = 45,
            SessionAbsoluteHours = 8,
            AllowedOrigins = ["https://a", "https://b"],
        };

        options.CookieName.ShouldBe("session");
        options.AntiforgeryCookieName.ShouldBe("af");
        options.SessionMinutes.ShouldBe(45);
        options.SessionAbsoluteHours.ShouldBe(8);
        options.AllowedOrigins.ShouldBe(["https://a", "https://b"]);
    }
}
