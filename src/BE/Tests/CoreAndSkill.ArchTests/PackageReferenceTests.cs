using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// A1 — docs/RULES.md A1. Package reference là dữ liệu của .csproj, không phải của assembly đã
// build — ArchUnitNET không đọc được tệp dự án (docs/wiki-core/be/04-testing-strategy.md §2.4:
// "Đồ thị assembly" chỉ đọc metadata assembly). Detector ở đây tự đọc .csproj bằng XML.
public class PackageReferenceTests
{
    [Fact]
    public void Core_Domain_MustHave_ZeroPackageReference()
    {
        var content = File.ReadAllText(DomainCsprojPath());

        var references = PackageReferenceScanner.FindPackageReferenceIncludes(content);

        references.ShouldBeEmpty();
    }

    [Fact]
    public void Detector_A1_Catches_RealViolation()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """;

        var references = PackageReferenceScanner.FindPackageReferenceIncludes(csproj);

        references.ShouldNotBeEmpty();
        references.ShouldContain("Newtonsoft.Json");
    }

    [Fact]
    public void Detector_A1_Ignores_ProjectWithoutPackageReference()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <RootNamespace>Fake</RootNamespace>
              </PropertyGroup>
            </Project>
            """;

        var references = PackageReferenceScanner.FindPackageReferenceIncludes(csproj);

        references.ShouldBeEmpty();
    }

    private static string DomainCsprojPath([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(here)!, "..", "..", "Core",
            "CoreAndSkill.Core.Domain", "CoreAndSkill.Core.Domain.csproj"));
}
