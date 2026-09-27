using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// A6 — docs/RULES.md A6. Hai con số "project trên đĩa" và "project trong solution" có thể khác
// nhau — docs/kien-truc-core-module.md §8.
public class SolutionDeclarationTests
{
    [Fact]
    public void EveryProjectOnDisk_IsDeclared_InSolution()
    {
        var missing = SolutionScanner.FindProjectsMissingFromSolution(SolutionRoot(), "CoreAndSkill.slnx");

        missing.ShouldBeEmpty();
    }

    [Fact]
    public void Detector_A6_Catches_RealViolation()
    {
        using var sandbox = new TempSandbox();
        sandbox.WriteFile("CoreAndSkill.slnx", """
            <Solution>
              <Project Path="A/A.csproj" />
            </Solution>
            """);
        sandbox.WriteFile("A/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        sandbox.WriteFile("B/B.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />"); // KHÔNG khai trong slnx

        var missing = SolutionScanner.FindProjectsMissingFromSolution(sandbox.Root, "CoreAndSkill.slnx");

        missing.ShouldContain(p => p.EndsWith("B.csproj", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Detector_A6_Ignores_EveryProjectDeclared()
    {
        using var sandbox = new TempSandbox();
        sandbox.WriteFile("CoreAndSkill.slnx", """
            <Solution>
              <Project Path="A/A.csproj" />
              <Project Path="B/B.csproj" />
            </Solution>
            """);
        sandbox.WriteFile("A/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        sandbox.WriteFile("B/B.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var missing = SolutionScanner.FindProjectsMissingFromSolution(sandbox.Root, "CoreAndSkill.slnx");

        missing.ShouldBeEmpty();
    }

    private static string SolutionRoot([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
}
