namespace CoreAndSkill.ArchTests.Support;

// Thư mục tạm dùng cho meta-test cần một cây tệp giả lập (solution, .csproj) — không chạm cây
// thật của repo. Dọn best-effort ở Dispose.
internal sealed class TempSandbox : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "archtests-" + Guid.NewGuid().ToString("N"));

    public TempSandbox() => Directory.CreateDirectory(Root);

    public void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); }
        catch { /* dọn best-effort — không làm test đỏ vì lý do không liên quan */ }
    }
}
