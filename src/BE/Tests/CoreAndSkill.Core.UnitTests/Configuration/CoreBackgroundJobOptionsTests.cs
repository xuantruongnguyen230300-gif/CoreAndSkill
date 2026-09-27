using CoreAndSkill.Core.Application.Configuration;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Configuration;

public class CoreBackgroundJobOptionsTests
{
    [Fact]
    public void SectionName_IsCoreBackgroundJobs()
        => CoreBackgroundJobOptions.SectionName.ShouldBe("Core:BackgroundJobs");

    [Fact]
    public void Defaults_CleanupIntervalIsOneDay()
        => new CoreBackgroundJobOptions().StaleDataCleanupIntervalHours.ShouldBe(24);
}
