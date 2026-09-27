using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CoreAndSkill.ArchTests.Support;

// Biên dịch một assembly nhỏ trong bộ nhớ để làm mẫu vi phạm/hợp lệ cho meta-test — luật T1
// (docs/wiki-core/be/04-testing-strategy.md §3). Không chạm tới project thật nào trong solution.
internal static class SyntheticAssembly
{
    public static Assembly Compile(string assemblyName, string source, params Assembly[] extraReferences)
        => Emit(
            assemblyName,
            CSharpSyntaxTree.ParseText(source),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            extraReferences);

    // Biên dịch một CHƯƠNG TRÌNH có câu lệnh cấp cao, đúng hình dạng assembly host: tệp mang tên Program.cs (tên tệp đi
    // vào tên metadata của kiểu `file`), đầu ra là tệp chạy được, nullable bật như Directory.Build.props. Câu lệnh cấp cao
    // không biên dịch được thành thư viện (CS8805), nên Compile ở trên không dựng được mẫu này.
    public static Assembly CompileProgram(string assemblyName, string source, params Assembly[] extraReferences)
        => Emit(
            assemblyName,
            CSharpSyntaxTree.ParseText(
                source,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
                path: "Program.cs"),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable),
            extraReferences);

    private static Assembly Emit(
        string assemblyName,
        SyntaxTree syntaxTree,
        CSharpCompilationOptions options,
        Assembly[] extraReferences)
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
        };

        foreach (var extra in extraReferences)
            references.Add(MetadataReference.CreateFromFile(extra.Location));

        var compilation = CSharpCompilation.Create(assemblyName, [syntaxTree], references, options);

        // Ghi ra tệp thật, không Assembly.Load(byte[]) — ArchLoader (Mono.Cecil) và
        // MetadataReference.CreateFromFile đều cần Assembly.Location trỏ tới một đường dẫn thật,
        // thứ assembly nạp thuần trong bộ nhớ không có.
        var outputPath = Path.Combine(Path.GetTempPath(), $"{assemblyName}-{Guid.NewGuid():N}.dll");
        var result = compilation.Emit(outputPath);

        if (!result.Success)
        {
            var errors = string.Join(
                Environment.NewLine,
                result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException($"Biên dịch assembly giả lập '{assemblyName}' thất bại:{Environment.NewLine}{errors}");
        }

        return Assembly.LoadFrom(outputPath);
    }
}
