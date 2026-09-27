using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Application.Permissions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Shouldly;
using Xunit;
using RuntimeAssembly = System.Reflection.Assembly;

namespace CoreAndSkill.ArchTests;

// B7 (nửa "đối chiếu hai chiều") — docs/RULES.md §6, danh mục nguồn duy nhất ở
// docs/database/schema-core.md §5.2.
//
// CHỖ HỎNG MÀ CỔNG NÀY CANH — không phải giả định, mà đọc ra từ chính PermissionChecker:
// thêm một khoá vào CorePermissions + CorePermissionCatalogSource, gắn [RequirePermission(...)] lên
// một action, nhưng QUÊN dòng seed trong migration. Khi đó:
//   • PermissionCatalogValidationHostedService xanh — khoá đúng khuôn, không trùng, có nhãn;
//   • S11 (PermissionDeclarationTests) xanh — action vẫn khai đúng một mức;
//   • tiến trình khởi động bình thường, không một cảnh báo nào;
//   • nhưng core.permission thiếu dòng đó, nên không vai trò nào gán được, và
//     PermissionChecker.GetEffectivePermissionsAsync đọc tập quyền TỪ CHÍNH BẢNG ĐÓ — kể cả nhánh
//     has_permission_bypass (`db.Permissions.Select(p => p.Code)`). Kết quả: 403 cho MỌI người, kể
//     cả quản trị viên, ở đúng một endpoint. Không log, không lỗi, không test hành vi nào đỏ.
//
// Cổng chạy KHÔNG cần database: nguồn C# lấy từ chính đối tượng IPermissionCatalogSource, nguồn seed
// lấy từ `Migration.UpOperations` — đúng chuỗi SQL migration sẽ chạy. Vì vậy nó KHÔNG mang
// [Trait("Category", "RequiresDocker")] và phải xanh trên máy không có Docker (luật T9).
//
// RANH GIỚI ĐÃ BIẾT, nói ra để không ai tưởng nó phủ rộng hơn thực tế:
//   • so đúng cặp Code + ResourceKey (đúng phạm vi B7) — name_key, action, display_order không so;
//   • chỉ đọc INSERT, và đọc CẢ HAI dạng EF phát ra: migrationBuilder.Sql (SqlOperation) lẫn
//     migrationBuilder.InsertData (InsertDataOperation). "Chỉ đọc INSERT" từng được hiểu nhầm thành
//     "đọc mọi cách viết INSERT" — InsertData CŨNG là INSERT, và trước đây nó vô hình với cổng.
//     Thao tác GHI khác trên hai bảng này vẫn ngoài tầm ĐỐI CHIẾU, nhưng nay chúng làm cổng ĐỎ
//     chứ không im lặng — ở CẢ HAI dạng: Update/DeleteDataOperation, và câu viết trong văn bản SQL
//     của migrationBuilder.Sql. Dạng thứ hai từng không được canh, và nó là dạng mà migration seed
//     duy nhất hiện có đang dùng (PermissionSeedScanner.ReadRows + AssertNoDataMutation). Kể cả
//     một câu INSERT vào ĐÚNG bảng được canh mà bộ đọc không hiểu — không danh sách cột, hoặc lấy
//     giá trị từ SELECT — cũng ném thay vì trả 0 dòng. Danh sách dạng đang bắt ở phía văn bản SQL
//     nằm ở đầu PermissionScriptParityTests — một chỗ, không hai;
//   • chỉ đối chiếu nguồn khoá trong assembly Core.Infrastructure, tức chỉ phủ KHOÁ CỦA CORE. Một
//     module cấp khoá riêng thì migration của module cũng của module — cổng đối chiếu tương ứng
//     phải đi cùng module đó, và cổng này KHÔNG thay thế được nó (nợ B9, docs/RULES.md §10). Để chỗ
//     trống đó không im lặng, CorePermissionCatalogSources_MustLiveIn_CoreInfrastructure bắt ca một
//     nguồn khoá bị đặt nhầm vào một assembly Core khác — và tầm quét của RIÊNG test ranh giới đó
//     rộng hơn: cả sáu assembly Core, HOST kể cả (xem CoreAssemblies);
//   • so cặp khoá, KHÔNG so tập khoá với danh mục gốc ở docs/database/schema-core.md §5.2. Nửa đó
//     là PermissionDocParityTests — cổng này khớp với nó qua nguồn C#, không thay nó.
public class PermissionSeedParityTests
{
    private const string PermissionTable = "core.permission";
    private const string ResourceTable = "core.permission_resource";
    private const string CatalogLabel = "nguồn IPermissionCatalogSource (C#)";
    private const string SeedLabel = "dòng seed trong migration";

    // ===== Luật thật =====

    [Fact]
    public void CorePermissionCatalog_MustMatch_MigrationSeed_BothWays()
    {
        var violations = PermissionSeedScanner.Diff(
            DeclaredPermissions(), SeededPermissions(), CatalogLabel, SeedLabel);

        violations.ShouldBeEmpty(
            "Luật B7: mỗi PermissionDefinition phải có đúng một dòng seed core.permission cùng cặp "
          + "code + resource_key, và ngược lại. Lệch một chiều thôi là 403 im lặng cho mọi người.");
    }

    [Fact]
    public void CorePermissionResources_MustMatch_MigrationSeed_BothWays()
    {
        var violations = PermissionSeedScanner.Diff(
            DeclaredResources(), SeededResources(), CatalogLabel, SeedLabel);

        violations.ShouldBeEmpty(
            "Luật B7: mỗi PermissionResourceDefinition phải có đúng một dòng seed core.permission_resource "
          + "cùng key, và ngược lại.");
    }

    // ===== T6 — chứng minh cổng không xanh rỗng =====

    // Một phép so hai tập RỖNG luôn PASS. Ba test dưới đây neo từng đầu vào một, vì mỗi đầu vào biến
    // mất theo một cách khác nhau: đổi tên interface, dời migration ra assembly khác, đổi tên bảng.

    [Fact]
    public void CorePermissionCatalog_MustMatch_MigrationSeed_ScansAtLeastOneRealCatalogSource()
    {
        CatalogSources().ShouldNotBeEmpty(
            "Không tìm thấy hiện thực IPermissionCatalogSource nào trong Core.Infrastructure — "
          + "seam đã bị đổi tên hoặc dời đi, và cổng B7 đang so một tập rỗng với một tập rỗng.");

        DeclaredPermissions().ShouldNotBeEmpty();
    }

    [Fact]
    public void CorePermissionCatalog_MustMatch_MigrationSeed_ScansAtLeastOneRealMigration()
    {
        Migrations().ShouldNotBeEmpty(
            "Không tìm thấy lớp Migration nào trong Core.Infrastructure — migration đã bị dời đi, "
          + "và cổng B7 đang đọc một tập seed rỗng.");
    }

    [Fact]
    public void CorePermissionCatalog_MustMatch_MigrationSeed_ReadsAtLeastOneRealSeedRow()
    {
        SeededPermissions().ShouldNotBeEmpty(
            $"Không đọc được dòng seed nào cho bảng '{PermissionTable}' từ UpOperations của migration — "
          + "tên bảng đã đổi, hoặc seed chuyển sang dạng SQL mà bộ đọc không nhận ra.");

        SeededResources().ShouldNotBeEmpty();
    }

    // ===== Ranh giới quét: nguồn khoá của Core phải ở ĐÚNG assembly cổng này nhìn thấy =====

    // Cổng B7 quét bằng reflection trên MỘT assembly. Đặt một IPermissionCatalogSource vào bất kỳ
    // assembly Core nào khác — Application, Web, Contracts, Domain, HAY HOST — thì khoá nó cấp chạy
    // thật lúc runtime (DI gộp mọi nguồn) nhưng KHÔNG bao giờ được đối chiếu với dòng seed. Ca này
    // đã đo: một nguồn khai 'mod.invoice.read' không có dòng seed, đặt ở Core.Web, để cổng xanh
    // nguyên; và một nguồn khai 'host.ghost.read' đặt ở CoreAndSkill.Api để cả 129 ArchTest xanh
    // nguyên, vì host từng không có trong tập quét.
    //
    // Đây KHÔNG phải cổng cho khoá của module — khoá của module nằm ở assembly module, ngoài tầm
    // quét của mọi test trong project này (nợ B9, docs/RULES.md §10). Đây là cổng cho ca đặt nhầm
    // chỗ TRONG Core, và ca đó — kể cả nhánh host — nằm trọn trong tầm quét hôm nay.
    [Fact]
    public void CorePermissionCatalogSources_MustLiveIn_CoreInfrastructure()
    {
        var expected = ArchitectureFixture.InfrastructureAssembly.GetName().Name;

        var misplaced = CoreAssemblies
            .Where(assembly => assembly != ArchitectureFixture.InfrastructureAssembly)
            .SelectMany(assembly => assembly.GetTypes()
                .Where(IsCatalogSourceImplementation)
                .Select(type => $"{type.FullName} (assembly {assembly.GetName().Name})"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        misplaced.ShouldBeEmpty(
            $"Luật B7: mọi IPermissionCatalogSource của Core phải nằm trong '{expected}' — đó là "
          + "assembly DUY NHẤT cổng đối chiếu hai chiều quét. Một nguồn khoá ở assembly Core khác "
          + "vẫn cấp khoá thật lúc chạy nhưng không dòng seed nào của nó được kiểm.");
    }

    // T6 cho chính test trên: nó khẳng định một tập RỖNG, nên phải chứng minh tầm quét khác rỗng và
    // phép nhận dạng còn nhận ra được nguồn khoá thật.
    [Fact]
    public void CorePermissionCatalogSources_MustLiveIn_CoreInfrastructure_ScansEveryCoreAssembly()
    {
        CoreAssemblies.Length.ShouldBeGreaterThan(1);

        // Ghim HOST đích danh. `Length > 1` vẫn xanh sau khi ai đó lặng lẽ gỡ host khỏi tập quét, và
        // gỡ đúng phần tử đó là cách lỗ mù cũ quay lại mà không ai thấy.
        CoreAssemblies.Select(assembly => assembly.GetName().Name)
            .ShouldContain(ArchitectureFixture.HostAssembly.GetName().Name,
                "Host (CoreAndSkill.Api) phải ở trong tầm quét ranh giới nguồn khoá, không phải ngoại lệ: "
              + "composition root là chỗ dễ cắm nhầm một nguồn khoá nhất. Cổng biết host bằng tên project, "
              + "không bằng khối core-paths — docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md "
              + "quyết định 8.");

        CoreAssemblies.SelectMany(assembly => assembly.GetTypes())
            .Count(IsCatalogSourceImplementation)
            .ShouldBeGreaterThan(0,
                "Không nhận ra hiện thực IPermissionCatalogSource nào trong toàn bộ assembly Core — "
              + "phép nhận dạng đã hỏng, và test ranh giới đang khẳng định một tập rỗng vô nghĩa.");
    }

    // ===== Detector_* (T1) =====

    [Fact]
    public void Detector_B7_Catches_PermissionDeclaredButNotSeeded()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user", "core.user.export|core.user"],
            ["core.user.read|core.user"],
            CatalogLabel,
            SeedLabel);

        violations.ShouldContain(m => m.StartsWith("core.user.export|core.user", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_B7_Catches_SeededButNotDeclared()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user"],
            ["core.user.read|core.user", "core.user.ghost|core.user"],
            CatalogLabel,
            SeedLabel);

        violations.ShouldContain(m => m.StartsWith("core.user.ghost|core.user", StringComparison.Ordinal));
    }

    // Cặp khoá KHỚP nhưng gắn sai tài nguyên vẫn là vi phạm — vì vậy phép so đi theo CẶP
    // code + resource_key, không so riêng code.
    [Fact]
    public void Detector_B7_Catches_SameCode_WrongResourceKey()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user"],
            ["core.user.read|core.menu"],
            CatalogLabel,
            SeedLabel);

        violations.Count.ShouldBe(2);
    }

    // F5 — `Diff` từng dựng HashSet cả hai vế, nên "đúng một dòng seed" là lời hứa chứ không phải
    // phép kiểm: một khoá khai 1 lần mà seed 2 dòng (khác id, cùng code + resource_key) trả về tập
    // vi phạm RỖNG. Đo được trước khi sửa.
    [Fact]
    public void Detector_B7_Catches_DuplicateSeedRow()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user"],
            ["core.user.read|core.user", "core.user.read|core.user"],
            CatalogLabel,
            SeedLabel);

        violations.ShouldContain(m => m.Contains("ĐÚNG", StringComparison.Ordinal)
                                   && m.StartsWith("core.user.read|core.user", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_B7_Catches_DuplicateDeclaration()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user", "core.user.read|core.user"],
            ["core.user.read|core.user"],
            CatalogLabel,
            SeedLabel);

        violations.ShouldContain(m => m.Contains("có 2 dòng", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_B7_Ignores_ExactMatch()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user", "core.menu.read|core.menu"],
            ["core.menu.read|core.menu", "core.user.read|core.user"],
            CatalogLabel,
            SeedLabel);

        violations.ShouldBeEmpty();
    }

    // Bẫy tiền tố: 'core.permission' là TIỀN TỐ của 'core.permission_resource'. Bộ đọc khớp theo
    // tiền tố sẽ trộn hai danh mục làm một và cổng lặng lẽ so nhầm bảng.
    [Fact]
    public void Detector_B7_Parser_DoesNotConfuse_PrefixTableName()
    {
        const string sql = """
            INSERT INTO core.permission_resource (id, key) VALUES ('1', 'core.user');
            INSERT INTO core.permission (id, code, resource_key) VALUES ('2', 'core.user.read', 'core.user');
            """;

        var permissions = PermissionSeedScanner.ParseInsertRows(sql, PermissionTable);
        var resources = PermissionSeedScanner.ParseInsertRows(sql, ResourceTable);

        permissions.Count.ShouldBe(1);
        permissions[0]["code"].ShouldBe("core.user.read");
        resources.Count.ShouldBe(1);
        resources[0]["key"].ShouldBe("core.user");
    }

    // Đảo thứ tự cột là thay đổi hợp lệ của SQL. Bộ đọc neo theo vị trí sẽ im lặng so nhầm cột.
    [Fact]
    public void Detector_B7_Parser_ReadsColumnsByName_NotByPosition()
    {
        const string sql = """
            INSERT INTO core.permission (resource_key, code, id) VALUES ('core.user', 'core.user.read', '1');
            """;

        var rows = PermissionSeedScanner.ParseInsertRows(sql, PermissionTable);

        rows[0]["code"].ShouldBe("core.user.read");
        rows[0]["resource_key"].ShouldBe("core.user");
    }

    // Nhiều dòng trong một câu, nhiều câu trong một migration, cùng lời gọi hàm và NULL trong dòng.
    [Fact]
    public void Detector_B7_Parser_ReadsMultipleRows_AndStatements()
    {
        const string sql = """
            INSERT INTO core.permission (id, code, resource_key, module_key, created_at)
            VALUES
                ('1', 'core.user.read', 'core.user', NULL, now()),
                ('2', 'core.user.write', 'core.user', NULL, now())
            ON CONFLICT (code) WHERE is_deleted = false DO NOTHING;

            INSERT INTO core.permission (id, code, resource_key, module_key, created_at)
            VALUES ('3', 'core.menu.read', 'core.menu', NULL, now());
            """;

        var rows = PermissionSeedScanner.ParseInsertRows(sql, PermissionTable);

        rows.Select(r => r["code"]).ShouldBe(["core.user.read", "core.user.write", "core.menu.read"]);
        rows[0]["module_key"].ShouldBeNull();
    }

    // Dấu phẩy và dấu ')' NẰM TRONG chuỗi không phải dấu ngăn cách.
    [Fact]
    public void Detector_B7_Parser_Ignores_SeparatorsInsideStringLiteral()
    {
        const string sql = """
            INSERT INTO core.permission (code, resource_key, name_key)
            VALUES ('core.user.read', 'core.user', 'Đọc (danh sách, chi tiết)');
            """;

        var rows = PermissionSeedScanner.ParseInsertRows(sql, PermissionTable);

        rows.Count.ShouldBe(1);
        rows[0]["name_key"].ShouldBe("Đọc (danh sách, chi tiết)");
        rows[0]["code"].ShouldBe("core.user.read");
    }

    // ===== Detector_* cho bộ đọc và CHÚ THÍCH (T1) =====
    //
    // Bộ tìm câu lệnh từng chạy `IndexOf` trên văn bản THÔ, nên chú thích được coi là câu lệnh sống.
    // Hai test dưới là chiều XANH SAI của lỗ đó: một câu seed bị chú thích để tạm vô hiệu vẫn được
    // đọc ra như dòng sống — đo được trước khi sửa, đọc ra đúng 1 dòng 'core.ghost.read'.

    [Fact]
    public void Detector_B7_Parser_Ignores_LineCommentedOutInsert()
    {
        const string sql = """
            -- Tạm vô hiệu, chờ quyết định:
            -- INSERT INTO core.permission (code, resource_key) VALUES ('core.ghost.read', 'core.user');
            INSERT INTO core.permission (code, resource_key) VALUES ('core.user.read', 'core.user');
            """;

        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read"]);
    }

    [Fact]
    public void Detector_B7_Parser_Ignores_BlockCommentedOutInsert()
    {
        const string sql = """
            /* INSERT INTO core.permission (code, resource_key) VALUES ('core.ghost.read', 'core.user'); */
            INSERT INTO core.permission (code, resource_key) VALUES ('core.user.read', 'core.user');
            """;

        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read"]);
    }

    // Một chú thích khối không đóng làm MỌI câu sau nó vô hình. Đọc rỗng rồi xanh là đúng khuôn mà
    // T6 tồn tại để chặn, nên ca này phải ném chứ không được trả rỗng.
    [Fact]
    public void Detector_B7_Parser_Throws_On_UnterminatedBlockComment()
    {
        const string sql = """
            /* quên đóng chú thích
            INSERT INTO core.permission (code, resource_key) VALUES ('core.user.read', 'core.user');
            """;

        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.ParseInsertRows(sql, PermissionTable))
            .Message.ShouldContain("không được đóng");
    }

    // Chuỗi là DỮ LIỆU: chữ INSERT nằm trong một giá trị không được sinh ra dòng seed ma.
    [Fact]
    public void Detector_B7_Parser_Ignores_InsertTextInsideStringLiteral()
    {
        const string sql = """
            INSERT INTO core.permission (code, resource_key, name_key)
            VALUES ('core.user.read', 'core.user', 'INSERT INTO core.permission (code) VALUES (''x'')');
            """;

        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read"]);
    }

    // Thân `DO $EF$ … $EF$` là nơi seed THẬT của script nằm. Bước qua nó thì cổng đọc 0 dòng rồi
    // xanh vì rỗng — đúng cái T6 tồn tại để chặn, nên ca này được ghim lại.
    [Fact]
    public void Detector_B7_Parser_ReadsSeed_InsideDollarQuotedBlock()
    {
        const string sql = """
            DO $EF$
            BEGIN
                IF NOT EXISTS(SELECT 1 FROM core.__ef_migrations_history WHERE "migration_id" = 'x') THEN
                INSERT INTO core.permission (code, resource_key) VALUES ('core.user.read', 'core.user');
                END IF;
            END $EF$;
            """;

        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read"]);
    }

    // ===== Detector_* cho dạng INSERT mà bộ đọc KHÔNG hiểu (T1) =====
    //
    // Bốn dạng dưới đây đã ĐO là PASS im lặng trước khi sửa — `ParseInsertRows` trả 0 dòng và
    // `AssertNoDataMutation` không kêu. Hai cái giá, cái thứ hai nặng hơn:
    //   • KHOÁ MỒ CÔI — `INSERT … SELECT` đưa dòng vào database thật trong khi cổng đọc 0 dòng
    //     thêm, nên `Diff` không báo gì ở đúng chiều nó khai là mình canh;
    //   • `AssertNoUpsertUpdate` CÓ ĐƯỜNG VÒNG TẦM THƯỜNG — nó chỉ chạy sau khi mệnh đề VALUES đọc
    //     xong, nên viết cùng câu đó ở dạng `SELECT`, hoặc bỏ danh sách cột đi, là vô hiệu hoá nó.
    //     `docs/RULES.md` §6 khi đó hứa chặt hơn thứ cổng kiểm.
    //
    // Mỗi ca đi kèm ca ĐỐI — cùng cú pháp, bảng KHÁC — để bản sửa không thành "thấy INSERT lạ là đỏ".

    [Theory]
    [InlineData("INSERT INTO core.permission SELECT * FROM staging.perm;")]
    [InlineData("INSERT INTO core.permission VALUES ('1', 'core.user.read', 'core.user');")]
    [InlineData("INSERT INTO core.permission VALUES ('1', 'a', 'b') "
              + "ON CONFLICT (code) DO UPDATE SET resource_key = 'z';")]
    public void Detector_B7_Parser_Throws_On_InsertWithoutColumnList(string sql)
        => Should.Throw<InvalidOperationException>(
                () => PermissionSeedScanner.ParseInsertRows(sql, PermissionTable))
            .Message.ShouldContain("KHÔNG khai danh sách cột");

    [Fact]
    public void Detector_B7_Parser_Throws_On_InsertSelect_WithColumnList()
        => Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.ParseInsertRows(
                "INSERT INTO core.permission (code, resource_key) SELECT c, r FROM staging.perm "
              + "ON CONFLICT (code) DO UPDATE SET resource_key = EXCLUDED.resource_key;",
                PermissionTable))
            .Message.ShouldContain("không dùng mệnh đề VALUES");

    [Theory]
    [InlineData("INSERT INTO core.app_user SELECT * FROM staging.u;")]
    [InlineData("INSERT INTO core.permission_resource (key) SELECT k FROM staging.r;")]
    [InlineData("INSERT INTO core.app_user VALUES ('1', 'admin');")]
    public void Detector_B7_Parser_Ignores_UnreadableInsert_OnAnotherTable(string sql)
        => PermissionSeedScanner.ParseInsertRows(sql, PermissionTable).ShouldBeEmpty();

    // ===== Detector_* cho hai dạng CHUỖI Postgres mà bộ đọc chưa biết (T1) =====
    //
    // Chiều ĐỎ SAI, cùng hình dạng với ca `''` đã phải sửa trước đó: bộ đọc không biết `$$…$$` và
    // `E'…'` là chuỗi, nên một dấu nháy bên trong mở ra một "chuỗi" chạy tới hết tệp rồi ném "chuỗi
    // không được đóng" cho SQL HOÀN TOÀN HỢP LỆ. Đo được ở cả `AssertNoDataMutation` lẫn
    // `ParseInsertRows`. Thân `DO $EF$ … $EF$` có mặt trong mọi script sinh từ EF, nên đây không
    // phải cú pháp dựng ra để thử.

    [Fact]
    public void Detector_B7_Parser_Reads_SeedAlongside_DollarQuotedValueWithQuote()
    {
        const string sql = """
            INSERT INTO core.app_user (note) VALUES ($$don't panic$$);
            INSERT INTO core.permission (code, resource_key) VALUES ('core.user.read', 'core.user');
            """;

        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read"]);
    }

    // Giá trị viết bằng `$$…$$` phải đọc ra ĐÚNG NỘI DUNG, không kèm dấu bao — nếu không phép so
    // lệch đi im lặng ở đúng cột đang đối chiếu.
    [Fact]
    public void Detector_B7_Parser_Reads_DollarQuotedValue()
    {
        var rows = PermissionSeedScanner.ParseInsertRows(
            "INSERT INTO core.permission (code, resource_key, name_key) "
          + "VALUES ($$core.user.read$$, 'core.user', $$Đọc, (xem) 'chi tiết'$$);",
            PermissionTable);

        rows.Count.ShouldBe(1);
        rows[0]["code"].ShouldBe("core.user.read");
        rows[0]["name_key"].ShouldBe("Đọc, (xem) 'chi tiết'");
    }

    [Fact]
    public void Detector_B7_Parser_Reads_ExtendedStringValue()
    {
        var rows = PermissionSeedScanner.ParseInsertRows(
            @"INSERT INTO core.permission (code, resource_key, name_key) "
          + @"VALUES ('core.user.read', 'core.user', E'Ng\'uoi dung');",
            PermissionTable);

        rows.Count.ShouldBe(1);
        rows[0]["name_key"].ShouldBe("Ng'uoi dung");
    }

    // Ranh giới: `$` KHÔNG phải lúc nào cũng mở chuỗi. `$1` là tham số vị trí, và nuốt nó như một
    // nháy đô la sẽ làm phần còn lại của tệp vô hình — đúng kiểu hỏng im lặng mà cổng này canh.
    [Fact]
    public void Detector_B7_Parser_DoesNotTreat_PositionalParameter_AsDollarQuote()
    {
        const string sql = """
            PREPARE p AS SELECT $1;
            INSERT INTO core.permission (code, resource_key) VALUES ('core.user.read', 'core.user');
            """;

        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read"]);
    }

    // Từ khoá không được là một phần của định danh dài hơn — và bộ tìm phải bước qua TRỌN định danh
    // đó, chứ không phải một ký tự, nếu không 'INSERTED' sẽ được quét lại từ 'NSERTED'.
    [Fact]
    public void Detector_B7_Parser_DoesNotMatch_KeywordInsideLongerIdentifier()
    {
        const string sql = """
            SELECT * FROM inserted_core_permission;
            INSERT INTO core.permission (code, resource_key) VALUES ('core.user.read', 'core.user');
            """;

        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read"]);
    }

    // ===== Detector_* cho mutation viết bằng migrationBuilder.Sql (T1) =====
    //
    // Nhánh SqlOperation từng đi thẳng vào ParseInsertRows, không qua AssertNoDataMutation. Migration
    // seed duy nhất hiện có viết bằng `Sql(...)`, nên đó là đường TỰ NHIÊN nhất để một câu DELETE đi
    // vào mà cả hai cổng vẫn xanh. Đo được trước khi sửa: trả rỗng, không ném.

    [Fact]
    public void Detector_B7_ReadRows_Throws_On_DeleteStatement_InSqlOperation()
    {
        var operation = new SqlOperation
        {
            Sql = "DELETE FROM core.permission WHERE code = 'core.user.export';",
        };

        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.ReadRows(operation, PermissionTable))
            .Message.ShouldContain("DELETE FROM");
    }

    [Fact]
    public void Detector_B7_ReadRows_Throws_On_UpdateStatement_InSqlOperation()
    {
        var operation = new SqlOperation
        {
            Sql = "UPDATE core.permission SET resource_key = 'core.menu' WHERE code = 'core.user.read';",
        };

        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.ReadRows(operation, PermissionTable))
            .Message.ShouldContain("UPDATE");
    }

    // Câu mutation trên bảng KHÁC vẫn phải đi qua yên lặng, nếu không mọi migration schema đều đỏ.
    [Fact]
    public void Detector_B7_ReadRows_Ignores_MutationOnAnotherTable_InSqlOperation()
    {
        var operation = new SqlOperation { Sql = "DELETE FROM core.app_user WHERE id = '1';" };

        PermissionSeedScanner.ReadRows(operation, PermissionTable).ShouldBeEmpty();
    }

    // Chú thích tiếng Việt trong chuỗi SQL của migration KHÔNG được làm cổng đỏ — chiều đỏ-sai của
    // cùng lỗ, ở phía migration.
    [Fact]
    public void Detector_B7_ReadRows_Ignores_MutationMentionedInComment_InSqlOperation()
    {
        var operation = new SqlOperation
        {
            Sql = """
                -- KHÔNG XOÁ TỰ ĐỘNG: không bao giờ DELETE FROM core.permission ở migration này.
                INSERT INTO core.permission (code, resource_key) VALUES ('core.user.read', 'core.user');
                """,
        };

        PermissionSeedScanner.ReadRows(operation, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read"]);
    }

    // ===== Detector_* cho dạng InsertData (T1) =====
    //
    // migrationBuilder.InsertData(...) KHÔNG sinh SqlOperation. Bộ đọc cũ chỉ lấy OfType<SqlOperation>
    // nên một dòng seed viết bằng InsertData vô hình với cổng — đã đo: seed 'core.ghost.read' bằng
    // InsertData để cổng xanh nguyên 110/110.

    [Fact]
    public void Detector_B7_ReadRows_Reads_InsertDataOperation()
    {
        var operation = InsertData("core", "permission",
            ["id", "code", "resource_key"],
            new object?[,] { { "1", "core.user.read", "core.user" } });

        var rows = PermissionSeedScanner.ReadRows(operation, PermissionTable);

        rows.Count.ShouldBe(1);
        rows[0]["code"].ShouldBe("core.user.read");
        rows[0]["resource_key"].ShouldBe("core.user");
    }

    [Fact]
    public void Detector_B7_ReadRows_InsertData_ReadsEveryRow()
    {
        var operation = InsertData("core", "permission",
            ["code", "resource_key"],
            new object?[,]
            {
                { "core.user.read", "core.user" },
                { "core.menu.read", "core.menu" },
            });

        PermissionSeedScanner.ReadRows(operation, PermissionTable)
            .Select(row => row["code"])
            .ShouldBe(["core.user.read", "core.menu.read"]);
    }

    // Cùng bẫy tiền tố như bộ đọc SQL: 'core.permission' không được nuốt 'core.permission_resource'.
    [Fact]
    public void Detector_B7_ReadRows_InsertData_DoesNotConfuse_PrefixTableName()
    {
        var operation = InsertData("core", "permission_resource", ["key"], new object?[,] { { "core.user" } });

        PermissionSeedScanner.ReadRows(operation, PermissionTable).ShouldBeEmpty();
        PermissionSeedScanner.ReadRows(operation, ResourceTable).Count.ShouldBe(1);
    }

    [Fact]
    public void Detector_B7_ReadRows_InsertData_IgnoresOtherTable()
    {
        var operation = InsertData("core", "app_user", ["id"], new object?[,] { { "1" } });

        PermissionSeedScanner.ReadRows(operation, PermissionTable).ShouldBeEmpty();
    }

    // Thao tác SỬA hoặc XOÁ khoá nằm ngoài tầm đối chiếu — nhưng phải ĐỎ, không được im lặng.
    [Fact]
    public void Detector_B7_ReadRows_Throws_On_UpdateData_OnWatchedTable()
    {
        var operation = new UpdateDataOperation
        {
            Schema = "core",
            Table = "permission",
            KeyColumns = ["code"],
            KeyValues = new object?[,] { { "core.user.read" } },
            Columns = ["resource_key"],
            Values = new object?[,] { { "core.menu" } },
        };

        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.ReadRows(operation, PermissionTable))
            .Message.ShouldContain("UpdateDataOperation");
    }

    [Fact]
    public void Detector_B7_ReadRows_Throws_On_DeleteData_OnWatchedTable()
    {
        var operation = new DeleteDataOperation
        {
            Schema = "core",
            Table = "permission",
            KeyColumns = ["code"],
            KeyValues = new object?[,] { { "core.user.read" } },
        };

        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.ReadRows(operation, PermissionTable))
            .Message.ShouldContain("DeleteDataOperation");
    }

    // Thao tác không ghi dữ liệu thì bỏ qua lặng lẽ — nếu không, mọi migration schema đều làm cổng đỏ.
    [Fact]
    public void Detector_B7_ReadRows_Ignores_NonDataOperation()
    {
        PermissionSeedScanner
            .ReadRows(new DropTableOperation { Schema = "core", Name = "permission" }, PermissionTable)
            .ShouldBeEmpty();
    }

    private static InsertDataOperation InsertData(string schema, string table, string[] columns, object?[,] values)
        => new() { Schema = schema, Table = table, Columns = columns, Values = values };

    [Fact]
    public void Detector_B7_Parser_ReturnsNothing_WhenTableAbsent()
    {
        const string sql = "INSERT INTO core.app_user (id) VALUES ('1');";

        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable).ShouldBeEmpty();
    }

    // ===== Hạ tầng =====

    // Năm assembly Core + HOST. Host (CoreAndSkill.Api) thuộc vùng dự án và nằm ngoài khối core-paths
    // (docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md, quyết định 1), nhưng cổng biết nó
    // bằng tên project (quyết định 8 của cùng ADR) — nên bỏ nó ra khỏi tập này là một lỗ mù, không
    // phải một ranh giới. Đường đi qua lỗ đó: đặt một IPermissionCatalogSource
    // vào composition root, chỗ tự nhiên nhất để cắm thêm một nguồn, rồi khai một khoá không có dòng
    // seed. Host không nằm trong đồ thị ArchUnit của fixture (luật A7 — xem ArchitectureFixture),
    // nhưng tập này quét bằng reflection trên từng assembly chứ không qua đồ thị đó.
    private static readonly RuntimeAssembly[] CoreAssemblies =
    [
        ArchitectureFixture.DomainAssembly,
        ArchitectureFixture.ApplicationAssembly,
        ArchitectureFixture.InfrastructureAssembly,
        ArchitectureFixture.WebAssembly,
        ArchitectureFixture.ContractsAssembly,
        ArchitectureFixture.HostAssembly,
    ];

    private static bool IsCatalogSourceImplementation(Type type)
        => CorePermissionCatalog.IsCatalogSourceImplementation(type);

    private static IReadOnlyList<IPermissionCatalogSource> CatalogSources()
        => CorePermissionCatalog.Sources(ArchitectureFixture.InfrastructureAssembly);

    private static IReadOnlyList<Migration> Migrations()
        => ArchitectureFixture.InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false }
                     && typeof(Migration).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .Select(Instantiate<Migration>)
            .ToList();

    // Không lọc bỏ kiểu "khó dựng" — lọc im lặng là cách một nguồn khoá mới biến mất khỏi tầm quét
    // mà cổng vẫn xanh. Dựng không được thì đỏ, và nói rõ kiểu nào.
    private static T Instantiate<T>(Type type)
    {
        try
        {
            return (T)Activator.CreateInstance(type, nonPublic: true)!;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Không dựng được '{type.FullName}' để đối chiếu B7. Kiểu này hiện thực {typeof(T).Name} "
              + "nhưng không có constructor không tham số — cổng B7 cần được sửa cho đọc được nó, "
              + "KHÔNG được lặng lẽ bỏ qua.", exception);
        }
    }

    private static IReadOnlyCollection<string> DeclaredPermissions()
        => CorePermissionCatalog.Permissions(ArchitectureFixture.InfrastructureAssembly);

    private static IReadOnlyCollection<string> DeclaredResources()
        => CorePermissionCatalog.Resources(ArchitectureFixture.InfrastructureAssembly);

    private static IReadOnlyCollection<string> SeededPermissions()
        => SeedRows(PermissionTable)
            .Select(row => $"{PermissionSeedScanner.RequiredValue(row, "code", PermissionTable)}"
                         + $"|{PermissionSeedScanner.RequiredValue(row, "resource_key", PermissionTable)}")
            .ToList();

    private static IReadOnlyCollection<string> SeededResources()
        => SeedRows(ResourceTable)
            .Select(row => PermissionSeedScanner.RequiredValue(row, "key", ResourceTable))
            .ToList();

    private static IReadOnlyList<IReadOnlyDictionary<string, string?>> SeedRows(string table)
        => Migrations()
            .SelectMany(migration => migration.UpOperations)
            .SelectMany(operation => PermissionSeedScanner.ReadRows(operation, table))
            .ToList();
}
