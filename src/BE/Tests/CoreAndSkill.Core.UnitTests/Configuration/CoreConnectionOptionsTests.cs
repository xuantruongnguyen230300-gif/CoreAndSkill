using CoreAndSkill.Core.Application.Configuration;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Configuration;

public class CoreConnectionOptionsTests
{
    [Fact]
    public void Core_ReturnsBoundValue()
    {
        var options = new CoreConnectionOptions { Core = "Host=localhost;Database=x" };

        options.Core.ShouldBe("Host=localhost;Database=x");
    }
}
