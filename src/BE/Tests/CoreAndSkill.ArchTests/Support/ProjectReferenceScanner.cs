using System.Xml.Linq;

namespace CoreAndSkill.ArchTests.Support;

// Đọc <ProjectReference> trực tiếp từ .csproj — đồ thị ProjectReference là nguồn của luật R3
// (docs/kien-truc-core-module.md §3: "Tầng trong không biết tầng ngoài … Ép bằng: Đồ thị
// ProjectReference + ArchTest A1, A2"). ArchUnitNET chỉ thấy assembly ĐÃ BUILD, không thấy .csproj.
internal static class ProjectReferenceScanner
{
    public static IReadOnlyList<string> FindReferencedProjectNames(string csprojContent)
    {
        var doc = XDocument.Parse(csprojContent);

        return doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Path.GetFileNameWithoutExtension(v!.Replace('\\', '/')))
            .ToList();
    }
}
