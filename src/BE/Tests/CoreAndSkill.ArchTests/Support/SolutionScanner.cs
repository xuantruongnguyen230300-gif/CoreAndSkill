using System.Xml.Linq;

namespace CoreAndSkill.ArchTests.Support;

// Đối chiếu project trên đĩa với project khai trong .slnx — docs/kien-truc-core-module.md §8.
internal static class SolutionScanner
{
    public static IReadOnlyList<string> FindProjectsMissingFromSolution(string solutionRoot, string solutionFileName)
    {
        var solutionPath = Path.Combine(solutionRoot, solutionFileName);
        var doc = XDocument.Parse(File.ReadAllText(solutionPath));

        var declared = doc.Descendants("Project")
            .Select(e => e.Attribute("Path")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Path.GetFullPath(Path.Combine(solutionRoot, v!)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var separator = Path.DirectorySeparatorChar;
        var onDisk = Directory.EnumerateFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFullPath)
            .ToList();

        return onDisk.Where(p => !declared.Contains(p)).ToList();
    }
}
