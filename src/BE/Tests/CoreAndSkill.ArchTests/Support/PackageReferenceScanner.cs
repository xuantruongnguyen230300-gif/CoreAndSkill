using System.Xml.Linq;

namespace CoreAndSkill.ArchTests.Support;

// Đọc <PackageReference> trực tiếp từ .csproj — ArchUnitNET không thấy được NuGet package
// reference (nó đọc metadata assembly đã build, không đọc tệp dự án). Dùng cho A1 và A14.
internal static class PackageReferenceScanner
{
    public static IReadOnlyList<string> FindPackageReferenceIncludes(string csprojContent)
    {
        var doc = XDocument.Parse(csprojContent);
        return doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();
    }

    public static IReadOnlyList<string> FindCsprojWithVersionAttribute(IEnumerable<string> csprojPaths)
    {
        var offenders = new List<string>();

        foreach (var path in csprojPaths)
        {
            var doc = XDocument.Parse(File.ReadAllText(path));
            var hasVersionAttribute = doc.Descendants("PackageReference")
                .Any(e => e.Attribute("Version") is not null);

            if (hasVersionAttribute)
                offenders.Add(path);
        }

        return offenders;
    }
}
