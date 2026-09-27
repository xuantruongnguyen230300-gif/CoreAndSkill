using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// B7, nửa thứ TƯ — đối chiếu với CHỦ SỞ HỮU của danh mục, docs/database/schema-core.md §5.2.
//
// CHỖ HỎNG MÀ CỔNG NÀY CANH. Câu luật B7 (docs/RULES.md §6) đòi khoá khớp *danh mục ở §5.2*. Ba nửa
// đã có chỉ đối chiếu ba bản CÀI ĐẶT với nhau:
//   (1) PermissionCatalogValidationHostedService — khuôn, trùng, nhãn rỗng, trên danh mục đã gộp;
//   (2) PermissionSeedParityTests               — C# ↔ dòng seed migration;
//   (3) PermissionScriptParityTests             — migration ↔ script SQL.
// Không nửa nào MỞ tài liệu. Ba bản khớp nhau hoàn hảo ở 12 khoá cũ vẫn không nói gì về việc tập đó
// có còn bằng danh mục gốc hay không, và docs/OWNERSHIP.md khai §5.2 là nguồn duy nhất — tức là
// người thêm khoá được bảo hãy sửa đúng file mà không cổng nào đọc.
//
// Cổng chạy KHÔNG cần database và không cần build FE: nó đọc một tệp markdown và một assembly.
//
// RANH GIỚI ĐÃ BIẾT:
//   • so cặp code + resource_key, đúng phạm vi B7 — cùng ranh giới với hai nửa kia. Cột `action`
//     KHÔNG so với C#, nhưng được kiểm nội bộ trong tài liệu (xem test khuôn `code` bên dưới);
//   • tập tài nguyên suy từ cột resource_key — §5.2 không có bảng tài nguyên riêng
//     (PermissionCatalogDocScanner nói rõ hệ quả);
//   • chỉ phủ khoá của CORE. Module khai khoá của mình theo cùng khuôn nhưng §5.2 không liệt kê
//     chúng — nợ B9/B10, docs/RULES.md §10;
//   • đối chiếu với nguồn C#. Hai chiều còn lại (migration, script) khớp với C# qua nửa (2) và (3),
//     nên chuỗi tài liệu ↔ C# ↔ migration ↔ script khép kín; nếu một mắt xích bị gỡ thì mắt đó đỏ
//     chứ không phải mắt này.
public class PermissionDocParityTests
{
    private const string DocLabel = "danh mục gốc ở database/schema-core.md §5.2";
    private const string CatalogLabel = "nguồn IPermissionCatalogSource (C#)";

    // ===== Luật thật =====

    [Fact]
    public void CorePermissionCatalog_MustMatch_SchemaCoreDoc_BothWays()
    {
        var violations = PermissionSeedScanner.Diff(
            DocumentedPermissions(),
            CorePermissionCatalog.Permissions(ArchitectureFixture.InfrastructureAssembly),
            DocLabel,
            CatalogLabel);

        violations.ShouldBeEmpty(
            "Luật B7: danh mục ở schema-core.md §5.2 là nguồn duy nhất của tập khoá Core "
          + "(docs/OWNERSHIP.md). Một hàng thêm vào đó mà không có PermissionDefinition tương ứng là "
          + "403 im lặng cho người tin vào tài liệu; một khoá trong C# không có trong đó làm chính "
          + "nguồn duy nhất thành nguồn thiếu.");
    }

    [Fact]
    public void CorePermissionResources_MustMatch_SchemaCoreDoc_BothWays()
    {
        var violations = PermissionSeedScanner.Diff(
            DocumentedResources(),
            CorePermissionCatalog.Resources(ArchitectureFixture.InfrastructureAssembly),
            DocLabel,
            CatalogLabel);

        violations.ShouldBeEmpty(
            "Luật B7: mọi resource_key trong danh mục gốc phải có đúng một "
          + "PermissionResourceDefinition, và ngược lại.");
    }

    // Kiểm nội bộ của chính tài liệu: khuôn `code` = `<resource_key>.<action>` là câu luật ngay trên
    // bảng đó. Nửa (1) ép khuôn này ở phía C#; không có gì ép nó ở phía tài liệu, nên một hàng gõ sai
    // sẽ lặng lẽ trở thành "danh mục gốc" cho người đọc tiếp theo.
    [Fact]
    public void SchemaCoreDoc_CatalogRows_FollowCodeShape()
    {
        var violations = Catalog()
            .Where(row => !PermissionCatalogDocScanner.FollowsCodeShape(row))
            .Select(row => $"{row.Code} ≠ {row.ResourceKey}.{row.Action}")
            .ToList();

        violations.ShouldBeEmpty(
            "Luật B7: khuôn code là <resource_key>.<action>, và chính bảng §5.2 khai cả ba cột.");
    }

    // ===== T6 — chứng minh cổng không xanh rỗng =====

    [Fact]
    public void CorePermissionCatalog_MustMatch_SchemaCoreDoc_ReadsAtLeastOneRealDocRow()
    {
        File.Exists(DocPath()).ShouldBeTrue(
            $"Không thấy tài liệu chủ '{DocPath()}' — danh mục gốc đã dời chỗ và cổng đang so một tập "
          + "rỗng.");

        DocumentedPermissions().ShouldNotBeEmpty();
        DocumentedResources().ShouldNotBeEmpty();
    }

    [Fact]
    public void CorePermissionCatalog_MustMatch_SchemaCoreDoc_ReadsAtLeastOneRealCatalogSource()
    {
        CorePermissionCatalog.Sources(ArchitectureFixture.InfrastructureAssembly).ShouldNotBeEmpty(
            "Không tìm thấy hiện thực IPermissionCatalogSource nào trong Core.Infrastructure — cổng "
          + "đang so danh mục gốc với một tập rỗng.");

        // Neo ĐÚNG hai thứ mà hai luật ở trên đem đi so, không chỉ cái sinh ra chúng: một nguồn có
        // thật nhưng `GetPermissions()` trả tập rỗng đi qua được chốt "có nguồn" mà vẫn để phép so
        // thành rỗng-với-rỗng. Chốt anh em ở PermissionSeedParityTests neo cả hai mức; bất đối
        // xứng giữa hai chốt là dấu hiệu sót chứ không phải khác biệt có chủ ý.
        CorePermissionCatalog.Permissions(ArchitectureFixture.InfrastructureAssembly)
            .ShouldNotBeEmpty();

        CorePermissionCatalog.Resources(ArchitectureFixture.InfrastructureAssembly)
            .ShouldNotBeEmpty();
    }

    // ===== Detector_* (T1) =====

    [Fact]
    public void Detector_B7Doc_Catches_RowInDocButNotInCode()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user", "core.user.export|core.user"],
            ["core.user.read|core.user"],
            DocLabel,
            CatalogLabel);

        violations.ShouldContain(m => m.StartsWith("core.user.export|core.user", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_B7Doc_Catches_RowInCodeButNotInDoc()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user"],
            ["core.user.read|core.user", "core.user.ghost|core.user"],
            DocLabel,
            CatalogLabel);

        violations.ShouldContain(m => m.StartsWith("core.user.ghost|core.user", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_B7Doc_Parser_ReadsMarkdownTable_ByColumnName()
    {
        const string markdown = """
            #### Danh mục khoá phân quyền Core — định nghĩa gốc

            Văn xuôi ở giữa không được làm bộ đọc lạc.

            | `code` | `resource_key` | `action` | Cho phép |
            | --- | --- | --- | --- |
            | `core.user.read` | `core.user` | `read` | Xem |
            | `core.menu.write` | `core.menu` | `write` | Sửa |

            Đoạn sau bảng.
            """;

        var rows = PermissionCatalogDocScanner.ParseCatalog(markdown, "test");

        rows.Select(row => row.Code).ShouldBe(["core.user.read", "core.menu.write"]);
        rows[1].ResourceKey.ShouldBe("core.menu");
        rows[1].Action.ShouldBe("write");
    }

    // Đảo thứ tự cột trong tài liệu là thay đổi hợp lệ. Bộ đọc neo theo vị trí sẽ im lặng so nhầm.
    [Fact]
    public void Detector_B7Doc_Parser_DoesNotDependOn_ColumnOrder()
    {
        const string markdown = """
            #### Danh mục khoá phân quyền Core — định nghĩa gốc

            | Cho phép | `action` | `resource_key` | `code` |
            | --- | --- | --- | --- |
            | Xem | `read` | `core.user` | `core.user.read` |
            """;

        var rows = PermissionCatalogDocScanner.ParseCatalog(markdown, "test");

        rows[0].Code.ShouldBe("core.user.read");
        rows[0].ResourceKey.ShouldBe("core.user");
        rows[0].Action.ShouldBe("read");
    }

    [Fact]
    public void Detector_B7Doc_Parser_Throws_WhenMarkerMissing()
    {
        Should.Throw<InvalidOperationException>(
                () => PermissionCatalogDocScanner.ParseCatalog("| `code` |\n| --- |\n| `x` |", "test"))
            .Message.ShouldContain(PermissionCatalogDocScanner.Marker);
    }

    [Fact]
    public void Detector_B7Doc_Parser_Throws_WhenColumnRenamed()
    {
        const string markdown = """
            #### Danh mục khoá phân quyền Core — định nghĩa gốc

            | `code` | `tai_nguyen` | `action` |
            | --- | --- | --- |
            | `core.user.read` | `core.user` | `read` |
            """;

        Should.Throw<InvalidOperationException>(
                () => PermissionCatalogDocScanner.ParseCatalog(markdown, "test"))
            .Message.ShouldContain("resource_key");
    }

    [Fact]
    public void Detector_B7Doc_Parser_Throws_WhenTableEmpty()
    {
        const string markdown = """
            #### Danh mục khoá phân quyền Core — định nghĩa gốc

            | `code` | `resource_key` | `action` |
            | --- | --- | --- |

            Không có dòng nào.
            """;

        Should.Throw<InvalidOperationException>(
                () => PermissionCatalogDocScanner.ParseCatalog(markdown, "test"))
            .Message.ShouldContain("KHÔNG đọc được dòng nào");
    }

    // Gọi thẳng vị từ mà luật thật gọi, và kiểm CẢ HAI chiều. Bản trước chỉ khẳng định lại luật
    // trên dữ liệu fixture — `rows[0].Code.ShouldNotBe($"{...}.{...}")` — nên nó không chạm tới một
    // dòng mã sản phẩm nào: làm hỏng vị từ theo chiều nào thì cả luật thật lẫn Detector_ đều xanh.
    // Một chiều thôi cũng chưa đủ: `=> true` bị ca "sai" bắt, `=> false` bị ca "đúng" bắt.
    [Fact]
    public void Detector_B7Doc_CodeShape_Catches_MismatchedAction()
    {
        const string markdown = """
            #### Danh mục khoá phân quyền Core — định nghĩa gốc

            | `code` | `resource_key` | `action` |
            | --- | --- | --- |
            | `core.user.read` | `core.user` | `read` |
            | `core.user.read` | `core.user` | `write` |
            """;

        var rows = PermissionCatalogDocScanner.ParseCatalog(markdown, "test");

        PermissionCatalogDocScanner.FollowsCodeShape(rows[0]).ShouldBeTrue();
        PermissionCatalogDocScanner.FollowsCodeShape(rows[1]).ShouldBeFalse();
    }

    // ===== Hạ tầng =====

    private static IReadOnlyList<PermissionCatalogDocScanner.Row> Catalog()
        => PermissionCatalogDocScanner.ReadCatalog(DocPath());

    private static IReadOnlyCollection<string> DocumentedPermissions()
        => Catalog().Select(row => $"{row.Code}|{row.ResourceKey}").ToList();

    // Tài nguyên xuất hiện nhiều lần trong cột resource_key theo lẽ thường — một tài nguyên có nhiều
    // khoá. Gộp TRƯỚC khi so, nếu không phép so bội số của Diff sẽ báo trùng cho một hình dạng đúng.
    private static IReadOnlyCollection<string> DocumentedResources()
        => [.. Catalog().Select(row => row.ResourceKey).Distinct(StringComparer.Ordinal)];

    // Với ra ngoài src/ theo đúng khuôn PermissionScriptParityTests dùng để đọc database/scripts/.
    private static string DocPath([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(here)!, "..", "..", "..", "..", "docs", "database", "schema-core.md"));
}
