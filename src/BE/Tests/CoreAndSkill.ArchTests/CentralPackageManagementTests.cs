using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// A14 — docs/RULES.md §3, lớp (2). NU1008 chỉ bắn khi ManagePackageVersionsCentrally=true còn tồn tại —
// gỡ thuộc tính đó thì NU1008 im lặng theo. Vì vậy test đọc trực tiếp Directory.Packages.props, không dựa vào NU1008.
public class CentralPackageManagementTests
{
    [Fact]
    public void DirectoryPackagesProps_HasCentralPackageManagementEnabled()
    {
        var content = File.ReadAllText(DirectoryPackagesPropsPath());

        content.ShouldContain("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>");
    }

    [Fact]
    public void NoCsproj_DeclaresPackageVersionDirectly()
    {
        var csprojFiles = Directory.EnumerateFiles(SolutionRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();

        csprojFiles.ShouldNotBeEmpty();

        var offenders = PackageReferenceScanner.FindCsprojWithVersionAttribute(csprojFiles);

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Detector_A14_Catches_RealViolation()
    {
        using var sandbox = new TempSandbox();
        sandbox.WriteFile("A.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """);

        var offenders = PackageReferenceScanner.FindCsprojWithVersionAttribute(
            [Path.Combine(sandbox.Root, "A.csproj")]);

        offenders.ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_A14_Ignores_CsprojWithoutVersionAttribute()
    {
        using var sandbox = new TempSandbox();
        sandbox.WriteFile("A.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """);

        var offenders = PackageReferenceScanner.FindCsprojWithVersionAttribute(
            [Path.Combine(sandbox.Root, "A.csproj")]);

        offenders.ShouldBeEmpty();
    }

    private static string DirectoryPackagesPropsPath([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "Directory.Packages.props"));

    private static string SolutionRoot([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
}
