using System.Runtime.CompilerServices;

namespace CoreAndSkill.ArchTests.Support;

// Mọi tệp .cs dưới src/BE/Tests — ba project test CỘNG thư mục Tests/Shared/ (mã test dùng chung,
// ADR-0070). Loại bin/ và obj/.
//
// VÌ SAO LÀ MỘT CHỖ. Ba cổng đang quét cùng tập tệp này (T9, T10, T11). Mỗi cổng giữ một bản chép của
// vòng liệt kê thì bản nào sửa trước sẽ khác bản kia — đúng khuôn .claude/CLAUDE.md §5 cấm. Điều dễ
// lệch nhất không phải vòng lặp mà là GỐC: ai đó thêm một thư mục dưới Tests/ rồi chỉ một cổng thấy nó.
//
// Tests/Shared/ là ca thật của chuyện đó: tệp ở đó không thuộc thư mục của project test nào, nên một
// phép liệt kê bám theo thư mục project sẽ bỏ sót nó trong im lặng — và cổng vẫn in PASS.
internal static class TestSourceFiles
{
    public static IReadOnlyList<string> All()
    {
        var root = TestsRoot();
        var separator = Path.DirectorySeparatorChar;

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    // Đường dẫn của một tệp test, quy về dạng tương đối so với src/BE/Tests và dùng dấu `/` — để một
    // allowlist khai bằng đường dẫn đọc được vẫn khớp trên cả Windows lẫn Linux.
    public static string RelativePath(string absolutePath)
        => Path.GetRelativePath(TestsRoot(), absolutePath).Replace(Path.DirectorySeparatorChar, '/');

    // CallerFilePath lấy ở CHÍNH tệp này (Tests/CoreAndSkill.ArchTests/Support/), không ở chỗ gọi —
    // cùng khuôn với ProductSourceFiles, nên gốc không đổi theo nơi gọi.
    private static string TestsRoot() => Up(ThisFile(), 3);

    private static string ThisFile([CallerFilePath] string here = "") => here;

    private static string Up(string path, int levels)
    {
        var current = path;
        for (var i = 0; i < levels; i++)
            current = Path.GetDirectoryName(current)!;

        return Path.GetFullPath(current);
    }
}
