using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// R3 — docs/kien-truc-core-module.md §3: "Tầng trong không biết tầng ngoài: Domain ⊄ Application ⊄
// Infrastructure/Web — Ép bằng: Đồ thị ProjectReference + ArchTest A1, A2". Đây là nửa "đồ thị
// ProjectReference": so trực tiếp .csproj với sơ đồ chiều phụ thuộc định nghĩa gốc ở
// docs/quy-uoc/be-architecture.md §1.2 — canary chính cho "thêm một ProjectReference sai chiều".
public class ProjectGraphTests
{
    private static readonly IReadOnlyDictionary<string, string[]> ExpectedReferences = new Dictionary<string, string[]>
    {
        ["CoreAndSkill.Core.Domain"] = [],
        ["CoreAndSkill.Core.Application"] = ["CoreAndSkill.Core.Domain"],
        ["CoreAndSkill.Core.Infrastructure"] = ["CoreAndSkill.Core.Domain", "CoreAndSkill.Core.Application"],
        ["CoreAndSkill.Core.Web"] = ["CoreAndSkill.Core.Domain", "CoreAndSkill.Core.Application", "CoreAndSkill.Core.Infrastructure"],
        ["CoreAndSkill.Core.Contracts"] = [],
    };

    [Theory]
    [MemberData(nameof(ProjectNames))]
    public void CoreProject_ReferencesExactlyItsAllowedLayers(string projectName)
    {
        var csproj = File.ReadAllText(Path.Combine(CoreDirectory(), projectName, $"{projectName}.csproj"));

        var actual = ProjectReferenceScanner.FindReferencedProjectNames(csproj);

        actual.ShouldBe(ExpectedReferences[projectName], ignoreOrder: true);
    }

    public static TheoryData<string> ProjectNames
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var name in ExpectedReferences.Keys) data.Add(name);
            return data;
        }
    }

    [Fact]
    public void Detector_ProjectGraph_Catches_WrongDirectionReference()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\CoreAndSkill.Core.Application\CoreAndSkill.Core.Application.csproj" />
              </ItemGroup>
            </Project>
            """;

        var actual = ProjectReferenceScanner.FindReferencedProjectNames(csproj);

        actual.ShouldNotBe(ExpectedReferences["CoreAndSkill.Core.Domain"]);
        actual.ShouldContain("CoreAndSkill.Core.Application");
    }

    [Fact]
    public void Detector_ProjectGraph_Ignores_EmptyProjectReferenceList()
    {
        const string csproj = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <RootNamespace>Fake</RootNamespace>
              </PropertyGroup>
            </Project>
            """;

        var actual = ProjectReferenceScanner.FindReferencedProjectNames(csproj);

        actual.ShouldBeEmpty();
    }

    private static string CoreDirectory([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "Core"));
}
