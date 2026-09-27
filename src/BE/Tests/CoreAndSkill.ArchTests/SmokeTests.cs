using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchUnitNET.xUnit;
using Xunit;

namespace CoreAndSkill.ArchTests;

public class SmokeTests
{
    [Fact]
    public void Architecture_LoadsAllFiveCoreAssemblies()
    {
        var rule = Types().Should().Exist();
        rule.Check(ArchitectureFixture.Architecture);
    }
}
