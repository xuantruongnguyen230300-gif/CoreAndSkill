using System.Runtime.CompilerServices;

namespace CoreAndSkill.ArchTests.Support;

// Tệp .cs SẢN PHẨM mà các cổng quét cú pháp canh: năm project dưới src/BE/Core và host src/BE/CoreAndSkill.Api. Loại
// bin/ và obj/.
//
// Host thuộc VÙNG DỰ ÁN và nằm ngoài khối core-paths (docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md, quyết
// định 1). Nó vẫn ở trong tầm quét vì quyết định 8 của cùng ADR: cổng biết host bằng TÊN PROJECT, không bằng khối
// core-paths — composition root là chỗ tự nhiên nhất để cắm nhầm một thứ Core cấm (một nguồn khoá, một literal, một
// đường ghi vòng), nên bỏ nó khỏi tầm quét là mở một lỗ mù, không phải vẽ một ranh giới.
internal static class ProductSourceFiles
{
    public static IReadOnlyList<string> Core()
    {
        var beRoot = BackendRoot();
        var separator = Path.DirectorySeparatorChar;

        return new[] { Path.Combine(beRoot, "Core"), HostDirectory() }
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    // Thư mục project host — composition root (docs/quy-uoc/be-architecture.md §3). Một chỗ khai, để cổng A7 và tầm quét
    // ở trên không giữ hai bản của cùng một đường dẫn.
    public static string HostDirectory() => Path.Combine(BackendRoot(), "CoreAndSkill.Api");

    public static bool EndsWith(string path, params string[] segments)
        => path.EndsWith(Path.DirectorySeparatorChar + Path.Combine(segments), StringComparison.Ordinal);

    // Đường dẫn một tệp sản phẩm, quy về dạng tương đối so với src/BE và dùng dấu `/` — để một allowlist khai bằng
    // đường dẫn đọc được vẫn khớp trên cả Windows lẫn Linux. Cùng khuôn TestSourceFiles.RelativePath.
    public static string RelativePath(string absolutePath)
        => Path.GetRelativePath(BackendRoot(), absolutePath).Replace(Path.DirectorySeparatorChar, '/');

    // Thư mục DDL — nguồn DUY NHẤT của câu hỏi "bảng nào có tenant_id". Không hằng hoá danh sách bảng ở cổng nào.
    public static string CoreDdlDirectory()
        => Path.GetFullPath(Path.Combine(BackendRoot(), "..", "..", "database", "scripts", "core"));

    // CallerFilePath lấy ở CHÍNH tệp này (Tests/CoreAndSkill.ArchTests/Support/), không ở chỗ gọi — gọi nội bộ, không mở
    // tham số ra ngoài.
    private static string BackendRoot() => Up(ThisFile(), 4);

    private static string ThisFile([CallerFilePath] string here = "") => here;

    private static string Up(string path, int levels)
    {
        var current = path;
        for (var i = 0; i < levels; i++)
            current = Path.GetDirectoryName(current)!;

        return Path.GetFullPath(current);
    }
}
