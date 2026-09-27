using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// B7, bản sao thứ ba của danh mục khoá — docs/RULES.md §6.
//
// CHỖ HỎNG MÀ CỔNG NÀY CANH. Danh mục khoá quyền sống ở BA nơi phải khớp nhau:
//   (a) nguồn C#            — CorePermissionCatalogSource;
//   (b) dòng seed migration — Migration.UpOperations;
//   (c) script SQL          — database/scripts/core/*.sql.
// PermissionSeedParityTests đối chiếu (a) ↔ (b). Không cổng nào chạm (c), trong khi (c) mới là thứ
// THẬT SỰ chạy vào database: docs/database/script-runbook.md §0 chốt KHÔNG auto-migrate, schema và
// seed áp bằng script chạy tay. Nghĩa là (a) và (b) khớp nhau hoàn hảo vẫn không nói gì về tập khoá
// thật trong DB.
//
// Checksum ở script-runbook §3.2/§3.5 KHÔNG thay được cổng này: nó bắt ca "sửa script SAU KHI đã
// áp", còn ca ở đây là "script và migration lệch nhau NGAY TỪ ĐẦU" — checksum vẫn khớp chính nó.
//
// RANH GIỚI ĐÃ BIẾT:
//   • chỉ so cặp code + resource_key và key tài nguyên, đúng phạm vi B7 — cùng ranh giới với
//     PermissionSeedParityTests;
//   • chỉ ĐỐI CHIẾU INSERT … VALUES có danh sách cột. Mọi thao tác GHI khác trên hai bảng này nằm
//     ngoài tầm đối chiếu — nhưng chúng làm cổng ĐỎ chứ không im lặng
//     (PermissionSeedScanner.AssertNoDataMutation), cùng cách phía migration ném khi gặp
//     Update/DeleteDataOperation. Danh sách dạng đang bắt: UPDATE · DELETE FROM · TRUNCATE (kể cả
//     danh sách nhiều bảng ngăn bằng dấu phẩy) · MERGE INTO, mỗi dạng kể cả khi mang từ khoá tuỳ
//     chọn ONLY; COPY <bảng> … FROM, đường nạp dữ liệu không đi qua INSERT nào (chiều TO chỉ xuất
//     dữ liệu nên đi qua yên lặng); cộng INSERT … ON CONFLICT … DO UPDATE, dạng ghi đè giá trị bằng
//     mệnh đề SET trong khi cổng chỉ đọc mệnh đề VALUES. Và MỌI dạng INSERT vào hai bảng đó mà bộ
//     đọc không hiểu — `INSERT … SELECT`, `INSERT` không khai danh sách cột — nay cũng ném: chúng
//     từng `continue` yên lặng, và vì AssertNoUpsertUpdate chỉ chạy sau mệnh đề VALUES, viết
//     `DO UPDATE` kèm một trong hai dạng đó là đường vòng qua chính nó. Tất cả từng đi qua YÊN
//     LẶNG — xem khối Detector_ tương ứng;
//   • `INSERT` viết trong một CHUỖI NHÁY ĐƠN — `EXECUTE 'INSERT INTO core.permission …'` — cũng
//     ném. Dạng này khác họ trên theo chiều nguy hiểm hơn: nó không làm tập khoá ngắn đi mà làm nó
//     DÀI ra, im lặng — dòng vào database thật trong khi ParseInsertRows đọc thêm 0 dòng, nên phép
//     so vẫn khớp và `core.permission` có một khoá mồ côi. Đo được trước khi sửa. Dạng `INSERT`
//     CHỈ bị bắt trong chuỗi nháy đơn: ở mức tệp nó là thứ cổng ĐỌC ĐƯỢC (bắt là làm đỏ chính câu
//     seed), và trong thân `$tag$…$tag$` thì CollectInsertRows đọc đệ quy vào nên dòng vẫn được
//     đối chiếu. Phép phân biệt đi theo "cổng có đọc được dòng này không", không theo "có phải SQL
//     động không";
//   • CHÚ THÍCH không phải câu lệnh. Bộ đọc bỏ qua `--` và `/* */` TRƯỚC khi tìm, nên một dòng dặn
//     nhau "đừng DELETE FROM core.permission" không làm cổng đỏ, và một câu seed bị chú thích để
//     tạm vô hiệu KHÔNG còn được đếm như dòng sống. Cả hai chiều từng hỏng;
//   • câu mutation viết bằng SQL ĐỘNG trong một chuỗi vẫn bị bắt (một lớp quét riêng), kể cả khi
//     LỒNG NHIỀU TẦNG (`EXECUTE 'EXECUTE ''DELETE …'''`) — phép bóc chuỗi đi đệ quy. Ba dạng chuỗi
//     đều được nhận: `'…'`, `E'…'` (gạch chéo ngược thoát), `$tag$…$tag$`. Một chuỗi ghép từ nhiều
//     mảnh LÚC CHẠY thì nằm ngoài tầm mọi bộ đọc văn bản tĩnh; và nội dung nằm SAU một dấu nháy lẻ
//     hoặc một `/*` chưa đóng BÊN TRONG một chuỗi cũng không được quét tiếp — ở đó phép quét dừng
//     lại vì nghiêm ngặt là báo đỏ sai (docs/RULES.md §6, ô B7);
//   • RANH GIỚI THEO CHIỀU NGƯỢC LẠI — CỔNG ĐỎ SAI, và nó chưa cắn ai nhưng sẽ cắn. Lớp quét
//     trong chuỗi gom MỌI chuỗi của toàn văn rồi tìm câu ghi trong từng chuỗi; nó KHÔNG biết chuỗi
//     đó thuộc câu lệnh nào, nên không phân biệt được SQL động với DỮ LIỆU tình cờ chứa văn bản
//     SQL. Hai câu HỢP LỆ dưới đây làm cả bốn test B7 phía script đỏ dù không câu nào ghi vào
//     core.permission:
//         COMMENT ON TABLE core.permission IS '… không bao giờ DELETE FROM core.permission …';
//         INSERT INTO core.audit_log (note) VALUES ('Yeu cau: DELETE FROM core.permission …');
//     Câu đầu là cách viết chú thích DDL chuẩn của Postgres, và repo này viết chú thích tiếng Việt
//     rất dày trong script — nên đây là chuyện sớm muộn, không phải ca dựng ra. Đã kiểm
//     `COMMENT ON` trong database/scripts/core/*.sql lúc viết khối này: không tệp nào có.
//     KHÔNG mở rộng bộ đọc để chữa, và KHÔNG gỡ lớp quét: gỡ nó là mở lại đúng lỗ SQL động ở ba
//     gạch đầu dòng trên. Ranh giới được GHIM bằng
//     Detector_B7Script_Pins_KnownFalseRed_WhenDataMerelyContainsSqlText, và thông điệp lỗi nói ra
//     cả hai khả năng thay vì chỉ dặn "mở rộng bộ đọc" — lời dặn đó, với một chuỗi dữ liệu, đẩy
//     người sửa thẳng tới việc gỡ lớp quét;
//   • chỉ đọc database/scripts/core/. Script của module nằm ở database/scripts/modules/ và đi cùng
//     cổng của module đó (nợ B9, docs/RULES.md §10).
public class PermissionScriptParityTests
{
    private const string PermissionTable = "core.permission";
    private const string ResourceTable = "core.permission_resource";
    private const string ScriptLabel = "script database/scripts/core/*.sql";
    private const string MigrationLabel = "dòng seed trong migration";

    // ===== Luật thật =====

    [Fact]
    public void CoreScripts_MustMatch_MigrationSeed_BothWays()
    {
        var violations = PermissionSeedScanner.Diff(
            ScriptRows(PermissionTable)
                .Select(row => $"{PermissionSeedScanner.RequiredValue(row, "code", PermissionTable)}"
                             + $"|{PermissionSeedScanner.RequiredValue(row, "resource_key", PermissionTable)}")
                .ToList(),
            MigrationRows(PermissionTable)
                .Select(row => $"{PermissionSeedScanner.RequiredValue(row, "code", PermissionTable)}"
                             + $"|{PermissionSeedScanner.RequiredValue(row, "resource_key", PermissionTable)}")
                .ToList(),
            ScriptLabel,
            MigrationLabel);

        violations.ShouldBeEmpty(
            "Luật B7: script SQL là thứ thật sự chạy vào database (script-runbook.md §0 — không "
          + "auto-migrate). Script lệch khỏi migration nghĩa là tập khoá trong DB lệch khỏi tập khoá "
          + "mà mã nguồn tin là mình có.");
    }

    [Fact]
    public void CoreScriptResources_MustMatch_MigrationSeed_BothWays()
    {
        var violations = PermissionSeedScanner.Diff(
            ScriptRows(ResourceTable)
                .Select(row => PermissionSeedScanner.RequiredValue(row, "key", ResourceTable)).ToList(),
            MigrationRows(ResourceTable)
                .Select(row => PermissionSeedScanner.RequiredValue(row, "key", ResourceTable)).ToList(),
            ScriptLabel,
            MigrationLabel);

        violations.ShouldBeEmpty("Luật B7: tài nguyên quyền trong script phải khớp migration hai chiều.");
    }

    // ===== T6 — chứng minh cổng không xanh rỗng =====
    //
    // Cả hai đầu vào biến mất theo cách riêng: thư mục script đổi chỗ, hoặc bảng đổi tên. Thiếu chốt
    // này thì một phép so hai tập rỗng vẫn in "không có vi phạm".

    [Fact]
    public void CoreScripts_MustMatch_MigrationSeed_ReadsAtLeastOneRealScriptFile()
    {
        Directory.Exists(ScriptDirectory()).ShouldBeTrue(
            $"Không thấy thư mục script '{ScriptDirectory()}' — script đã dời chỗ và cổng đang so một "
          + "tập rỗng. Sửa đường dẫn, đừng để test xanh vì không tìm thấy gì.");

        ScriptFiles().ShouldNotBeEmpty("Thư mục script không có tệp .sql nào.");
    }

    [Fact]
    public void CoreScripts_MustMatch_MigrationSeed_ReadsAtLeastOneRealSeedRow()
    {
        ScriptRows(PermissionTable).ShouldNotBeEmpty(
            $"Không đọc được dòng INSERT nào cho '{PermissionTable}' từ script — tên bảng đã đổi, hoặc "
          + "seed chuyển sang dạng cú pháp mà bộ đọc không nhận ra.");

        ScriptRows(ResourceTable).ShouldNotBeEmpty();
        MigrationRows(PermissionTable).ShouldNotBeEmpty();

        // Đầu vào thứ TƯ của hai luật trên. Chốt này KHÔNG bắt thêm ca nào mà ba chốt trên chưa
        // bắt — đã đo bằng A/B: làm dòng tài nguyên của migration biến mất thì
        // CoreScriptResources_MustMatch_MigrationSeed_BothWays đỏ ngay, và ca "cả hai vế cùng rỗng"
        // thì ScriptRows(ResourceTable) ở ngay trên đã chặn. Giữ nó vì hai lý do khác: nó neo ĐÚNG
        // đầu vào của mình, nên khi hỏng thì thông điệp chỉ thẳng phía migration thay vì bắt người
        // đọc suy ngược từ một phép so; và chốt anh em ở PermissionSeedParityTests neo cả hai bảng,
        // nên bất đối xứng ở đây sẽ đọc như một chỗ sót.
        MigrationRows(ResourceTable).ShouldNotBeEmpty();
    }

    // ===== Detector_* (T1) =====

    [Fact]
    public void Detector_B7Script_Catches_RowInScriptButNotInMigration()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user", "core.ghost.read|core.user"],
            ["core.user.read|core.user"],
            ScriptLabel,
            MigrationLabel);

        violations.ShouldContain(m => m.StartsWith("core.ghost.read|core.user", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_B7Script_Catches_RowInMigrationButNotInScript()
    {
        var violations = PermissionSeedScanner.Diff(
            ["core.user.read|core.user"],
            ["core.user.read|core.user", "core.user.export|core.user"],
            ScriptLabel,
            MigrationLabel);

        violations.ShouldContain(m => m.StartsWith("core.user.export|core.user", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_B7Script_Throws_On_UpdateOnWatchedTable()
    {
        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.AssertNoDataMutation(
                "UPDATE core.permission SET resource_key = 'core.menu' WHERE code = 'core.user.read';",
                PermissionTable, "test"))
            .Message.ShouldContain("UPDATE");
    }

    [Fact]
    public void Detector_B7Script_Throws_On_DeleteOnWatchedTable()
    {
        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.AssertNoDataMutation(
                "DELETE FROM core.permission WHERE code = 'core.user.read';", PermissionTable, "test"))
            .Message.ShouldContain("DELETE FROM");
    }

    // Cùng bẫy tiền tố: câu trên bảng permission_resource KHÔNG được tính là câu trên bảng permission.
    [Fact]
    public void Detector_B7Script_Ignores_MutationOnAnotherTable()
    {
        PermissionSeedScanner.AssertNoDataMutation(
            "DELETE FROM core.permission_resource WHERE key = 'core.user'; UPDATE core.app_user SET x = 1;",
            PermissionTable, "test");
    }

    [Fact]
    public void Detector_B7Script_Ignores_PlainInsert()
    {
        PermissionSeedScanner.AssertNoDataMutation(
            "INSERT INTO core.permission (code) VALUES ('core.user.read');", PermissionTable, "test");
    }

    // ===== Detector_* cho CHÚ THÍCH (T1) =====
    //
    // Chiều ĐỎ SAI của lỗ "bộ tìm quét văn bản thô": một dòng chú thích tiếng Việt dặn nhau *đừng*
    // xoá làm cổng đỏ vì đúng câu dặn đó — và thông điệp lại đẩy người sửa đi "mở rộng bộ đọc", tức
    // đúng hướng hỏng. Đo được trước khi sửa, đúng thông điệp đó. Repo này viết chú thích tiếng Việt
    // rất dày trong script, nên đây là chuyện sớm muộn.

    [Fact]
    public void Detector_B7Script_Ignores_MutationMentionedInLineComment()
    {
        PermissionSeedScanner.AssertNoDataMutation(
            """
            -- KHÔNG XOÁ TỰ ĐỘNG: không bao giờ DELETE FROM core.permission ở script này.
            -- Cũng không UPDATE core.permission để đổi resource_key.
            INSERT INTO core.permission (code) VALUES ('core.user.read');
            """,
            PermissionTable, "test");
    }

    [Fact]
    public void Detector_B7Script_Ignores_MutationMentionedInBlockComment()
    {
        PermissionSeedScanner.AssertNoDataMutation(
            """
            /* Lịch sử: bản nháp đầu có TRUNCATE TABLE core.permission, đã bỏ. */
            INSERT INTO core.permission (code) VALUES ('core.user.read');
            """,
            PermissionTable, "test");
    }

    // KHÔNG được thu hẹp cổng khi sửa chiều đỏ-sai. `EXECUTE 'DELETE FROM core.permission'` là câu
    // xoá THẬT với Postgres dù nó nằm trong một chuỗi; bộ tìm cũ bắt được nó một cách tình cờ (quét
    // văn bản thô), nên bản mới soi thêm bên trong chuỗi, thành một lớp tường minh.
    [Fact]
    public void Detector_B7Script_Throws_On_MutationInsideDynamicSqlString()
    {
        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.AssertNoDataMutation(
                "EXECUTE 'DELETE FROM core.permission WHERE code = ''core.user.read''';",
                PermissionTable, "test"))
            .Message.ShouldContain("SQL động");
    }

    // Ranh giới của lớp trên: chuỗi trên bảng KHÁC vẫn đi qua yên lặng.
    [Fact]
    public void Detector_B7Script_Ignores_DynamicSqlOnAnotherTable()
    {
        PermissionSeedScanner.AssertNoDataMutation(
            "EXECUTE 'DELETE FROM core.app_user';", PermissionTable, "test");
    }

    // Mutation nằm trong thân `DO $EF$ … $EF$` — dạng mà mọi script sinh từ EF đều dùng — phải đỏ.
    // Bỏ qua thân dollar-quote là cách cổng này đọc 0 dòng rồi xanh vì rỗng.
    [Fact]
    public void Detector_B7Script_Throws_On_MutationInsideDollarQuotedBlock()
    {
        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.AssertNoDataMutation(
                """
                DO $EF$
                BEGIN
                    DELETE FROM core.permission WHERE code = 'core.user.export';
                END $EF$;
                """,
                PermissionTable, "test"))
            .Message.ShouldContain("DELETE FROM");
    }

    // ===== Detector_* cho dạng câu mà bộ tìm cũ KHÔNG thấy (T1) =====
    //
    // Bảy ca dưới đây đã ĐO trên assembly đã build là PASS im lặng trước khi sửa: bộ tìm đòi tên
    // bảng đứng NGAY SAU cụm từ khoá, nên mọi từ chen giữa làm hụt khớp. Chúng không phải cú pháp
    // lạ — `ONLY` là cách viết đúng khi muốn chặn thừa kế bảng, và `TRUNCATE a, b` là cách viết
    // thường gặp nhất của TRUNCATE nhiều bảng.
    //
    // Mỗi ca đi kèm ca ĐỐI — cùng cú pháp, bảng KHÁC — để bản sửa không được nới thành "thấy từ
    // khoá là đỏ".

    [Theory]
    [InlineData("DELETE FROM ONLY core.permission WHERE code = 'core.user.read';", "DELETE FROM")]
    [InlineData("UPDATE ONLY core.permission SET resource_key = 'core.menu';", "UPDATE")]
    [InlineData("TRUNCATE ONLY core.permission;", "TRUNCATE")]
    [InlineData("TRUNCATE TABLE ONLY core.permission;", "TRUNCATE TABLE")]
    public void Detector_B7Script_Throws_On_MutationWithOnlyKeyword(string sql, string shape)
    {
        Should.Throw<InvalidOperationException>(
                () => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test"))
            .Message.ShouldContain(shape);
    }

    // Ca đối của khối trên: `ONLY` không được biến phép khớp tên bảng thành khớp theo tiền tố.
    [Theory]
    [InlineData("DELETE FROM ONLY core.permission_resource WHERE key = 'core.user';")]
    [InlineData("UPDATE ONLY core.app_user SET x = 1;")]
    [InlineData("TRUNCATE ONLY core.app_user;")]
    [InlineData("TRUNCATE TABLE ONLY core.permission_resource;")]
    public void Detector_B7Script_Ignores_MutationWithOnlyKeyword_OnAnotherTable(string sql)
        => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test");

    // TRUNCATE nhận DANH SÁCH bảng: tên bảng được canh đứng thứ hai vẫn là xoá sạch bảng đó.
    [Theory]
    [InlineData("TRUNCATE core.app_user, core.permission;", "TRUNCATE")]
    [InlineData("TRUNCATE TABLE core.app_user, core.permission RESTART IDENTITY;", "TRUNCATE TABLE")]
    [InlineData("TRUNCATE ONLY core.app_user, ONLY core.permission CASCADE;", "TRUNCATE")]
    public void Detector_B7Script_Throws_On_TruncateTableList(string sql, string shape)
    {
        Should.Throw<InvalidOperationException>(
                () => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test"))
            .Message.ShouldContain(shape);
    }

    // Ca đối: một danh sách KHÔNG chứa bảng được canh vẫn đi qua, kể cả khi có đuôi CASCADE.
    [Fact]
    public void Detector_B7Script_Ignores_TruncateTableList_WithoutWatchedTable()
        => PermissionSeedScanner.AssertNoDataMutation(
            "TRUNCATE core.app_user, core.permission_resource RESTART IDENTITY CASCADE;",
            PermissionTable, "test");

    // MERGE ghi thật vào bảng đích và bộ đọc không hiểu nó — phải đỏ, không im lặng.
    [Fact]
    public void Detector_B7Script_Throws_On_MergeIntoWatchedTable()
    {
        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.AssertNoDataMutation(
                "MERGE INTO core.permission USING s ON true WHEN MATCHED THEN DELETE;",
                PermissionTable, "test"))
            .Message.ShouldContain("MERGE INTO");
    }

    [Fact]
    public void Detector_B7Script_Ignores_MergeIntoAnotherTable()
        => PermissionSeedScanner.AssertNoDataMutation(
            "MERGE INTO core.app_user USING s ON true WHEN MATCHED THEN DELETE;",
            PermissionTable, "test");

    // ===== Detector_* cho COPY — đường GHI không đi qua INSERT nào (T1) =====
    //
    // `COPY <bảng> FROM …` nạp dòng thẳng vào bảng và không có mệnh đề VALUES nào cho bộ đọc đối
    // chiếu. Đo được là PASS im lặng trước khi sửa. Chiều ngược lại — `COPY <bảng> TO …` — chỉ XUẤT
    // dữ liệu ra, không làm tập khoá lệch đi, nên cho nó đỏ là báo đỏ SAI cho SQL hợp lệ: đó là lý
    // do bộ tìm đòi thêm từ khoá `FROM` chứ không dừng ở tên bảng.

    [Theory]
    [InlineData("COPY core.permission (code, resource_key) FROM stdin;")]
    [InlineData("COPY core.permission FROM '/tmp/permission.csv' WITH (FORMAT csv);")]
    public void Detector_B7Script_Throws_On_CopyIntoWatchedTable(string sql)
        => Should.Throw<InvalidOperationException>(
                () => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test"))
            .Message.ShouldContain("COPY");

    [Theory]
    [InlineData("COPY core.permission TO stdout;")]
    [InlineData("COPY core.permission (code) TO '/tmp/permission.csv' WITH (FORMAT csv);")]
    [InlineData("COPY (SELECT code FROM core.permission) TO stdout;")]
    [InlineData("COPY core.app_user (id) FROM stdin;")]
    [InlineData("COPY core.permission_resource (key) FROM stdin;")]
    public void Detector_B7Script_Ignores_CopyThatDoesNotWriteWatchedTable(string sql)
        => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test");

    // ===== Detector_* cho hai dạng CHUỖI Postgres mà bộ đọc chưa biết (T1) =====
    //
    // Chiều ĐỎ SAI, cùng hình dạng với ca `''`: bộ đọc không biết `$$…$$` và `E'…'` là chuỗi, nên
    // dấu nháy bên trong mở ra một "chuỗi" chạy tới hết tệp rồi ném cho một tệp HỢP LỆ — và vì
    // AssertNoDataMutation chạy trên TOÀN VĂN mỗi tệp .sql, một giá trị như vậy ở bất kỳ đâu làm đỏ
    // cả bốn test B7 phía script. Lối thoát tự nhiên khi đó lại là đi nới cổng.

    [Theory]
    [InlineData("INSERT INTO core.app_user (note) VALUES ($$don't panic$$);")]
    [InlineData("INSERT INTO core.app_user (note) VALUES ($tag$don't panic$tag$);")]
    [InlineData(@"INSERT INTO core.app_user (note) VALUES (E'don\'t');")]
    public void Detector_B7Script_Ignores_QuoteInsideUnknownStringForm(string sql)
        => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test");

    // KHOAN DUNG KHÔNG ĐƯỢC THU HẸP CỔNG: hai dạng chuỗi mới vẫn là chỗ chứa SQL động, nên câu ghi
    // viết trong đó vẫn phải đỏ.
    [Theory]
    [InlineData("EXECUTE $$DELETE FROM core.permission$$;")]
    [InlineData(@"EXECUTE E'DELETE FROM core.permission WHERE name_key = ''O\'Brien''';")]
    public void Detector_B7Script_Throws_On_MutationInsideUnknownStringForm(string sql)
        => Should.Throw<InvalidOperationException>(
                () => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test"))
            .Message.ShouldContain("SQL động");

    [Fact]
    public void Detector_B7Script_Ignores_DollarQuotedDynamicSqlOnAnotherTable()
        => PermissionSeedScanner.AssertNoDataMutation(
            "EXECUTE $$DELETE FROM core.app_user$$;", PermissionTable, "test");

    // ===== Detector_* cho SQL động LỒNG HAI TẦNG (T1) =====
    //
    // Cả hai câu dưới đây chạy được thật và xoá thật một khoá. Bộ bóc chuỗi một tầng trả về nội dung
    // `EXECUTE 'DELETE FROM core.permission'`, và ở đó câu xoá chỉ là "một chuỗi đã đóng đúng" với
    // bộ tìm câu lệnh — bước qua nguyên khối, không ai thấy. Đo được: cả hai PASS im lặng trước khi
    // phép bóc chuỗi thành đệ quy.

    [Theory]
    [InlineData("EXECUTE 'EXECUTE ''DELETE FROM core.permission''';")]
    [InlineData("DO $EF$ BEGIN EXECUTE 'EXECUTE ''DELETE FROM core.permission'''; END $EF$;")]
    public void Detector_B7Script_Throws_On_MutationInsideNestedDynamicSqlString(string sql)
        => Should.Throw<InvalidOperationException>(
                () => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test"))
            .Message.ShouldContain("SQL động");

    [Fact]
    public void Detector_B7Script_Ignores_NestedDynamicSqlOnAnotherTable()
        => PermissionSeedScanner.AssertNoDataMutation(
            "EXECUTE 'EXECUTE ''DELETE FROM core.app_user''';", PermissionTable, "test");

    // ===== Detector_* cho INSERT viết bằng SQL ĐỘNG (T1) =====
    //
    // Ca này KHÁC hẳn họ UPDATE/DELETE/TRUNCATE ở trên, và khác theo chiều nguy hiểm hơn: nó không
    // làm tập khoá NGẮN đi mà làm nó DÀI ra, im lặng. Đo được trước khi sửa, trên assembly đã build:
    //
    //     EXECUTE 'INSERT INTO core.permission (code, resource_key) VALUES (''core.ghost.read'', ''core.user'')';
    //
    //   • ParseInsertRows đọc được 0 dòng — `'` tới trước, TrySkipQuoted bước qua nguyên khối;
    //   • AssertNoDataMutation KHÔNG ném — `shapes` không có dạng INSERT.
    // Nghĩa là dòng vào database thật, cả ba nguồn của B7 vẫn khớp nhau hoàn hảo, và `core.permission`
    // có một KHOÁ MỒ CÔI mà không nguồn nào khai. Chiều đó chính là thứ `Diff` tồn tại để canh.
    //
    // `INSERT` chỉ bị bắt TRONG CHUỖI NHÁY ĐƠN, không bắt ở mức tệp và không bắt trong thân nháy
    // đô la — hai ca đối ngay dưới ghim ranh giới đó. Bắt ở mức tệp là làm đỏ chính câu seed cổng
    // đang đối chiếu; bắt trong thân `DO $EF$ … $EF$` là làm đỏ đúng hình dạng mà migration seed
    // thật đang dùng.

    [Theory]
    [InlineData("EXECUTE 'INSERT INTO core.permission (code, resource_key) VALUES (''core.ghost.read'', ''core.user'')';")]
    [InlineData("EXECUTE 'EXECUTE ''INSERT INTO core.permission (code) VALUES (''''x'''')''';")]
    [InlineData("DO $EF$ BEGIN EXECUTE 'INSERT INTO core.permission (code) VALUES (''x'')'; END $EF$;")]
    public void Detector_B7Script_Throws_On_InsertInsideDynamicSqlString(string sql)
        => Should.Throw<InvalidOperationException>(
                () => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test"))
            .Message.ShouldContain("INSERT INTO");

    [Fact]
    public void Detector_B7Script_Ignores_DynamicInsertOnAnotherTable()
        => PermissionSeedScanner.AssertNoDataMutation(
            "EXECUTE 'INSERT INTO core.app_user (id) VALUES (1)';", PermissionTable, "test");

    // CA ĐỐI QUAN TRỌNG NHẤT của khối trên, và là lý do dạng INSERT KHÔNG nằm trong `shapes` chung.
    // Thân `DO $EF$ … $EF$` là MÃ chạy được chứ không phải dữ liệu, CollectInsertRows đọc thẳng vào
    // đó, và migration seed thật của repo này viết đúng hình dạng ấy. Cho nó đỏ là làm đỏ chính
    // danh mục cổng đang canh — nên hai lớp bù nhau: đọc được thì ĐỌC, không đọc được thì NÉM.
    [Fact]
    public void Detector_B7Script_Reads_InsertInsideDollarQuotedBlock_InsteadOfThrowing()
    {
        const string sql = """
            DO $EF$
            BEGIN
                INSERT INTO core.permission (code, resource_key)
                VALUES ('core.user.read', 'core.user');
            END $EF$;
            """;

        PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test");

        var rows = PermissionSeedScanner.ParseInsertRows(sql, PermissionTable);

        rows.Count.ShouldBe(1);
        rows[0]["code"].ShouldBe("core.user.read");
    }

    // `EXECUTE $$…$$` LÀ SQL động, nhưng nó KHÔNG phải lỗ: CollectInsertRows đọc đệ quy vào MỌI
    // thân nháy đô la, nên dòng này được đối chiếu như một dòng seed bình thường. Đó là lý do phép
    // phân biệt của AssertNoDataMutation đi theo DẠNG CHUỖI chứ không theo "có phải SQL động
    // không": câu hỏi đúng là "cổng có đọc được dòng này không", và với thân nháy đô la thì có.
    [Fact]
    public void Detector_B7Script_Reads_InsertInsideDollarQuotedDynamicSql_InsteadOfThrowing()
    {
        const string sql = "EXECUTE $$INSERT INTO core.permission (code) VALUES ('core.ghost.read')$$;";

        PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test");

        var rows = PermissionSeedScanner.ParseInsertRows(sql, PermissionTable);

        rows.Count.ShouldBe(1);
        rows[0]["code"].ShouldBe("core.ghost.read");
    }

    // Ca đối thứ hai: INSERT viết tĩnh ở mức tệp là thứ cổng ĐỌC ĐƯỢC. Đỏ ở đây nghĩa là dạng
    // INSERT đã bị đưa nhầm vào `shapes` chung, và cả bốn test B7 phía script sẽ đỏ theo.
    [Fact]
    public void Detector_B7Script_Ignores_PlainInsert_EvenWhenValuesContainQuotes()
    {
        const string sql = "INSERT INTO core.permission (code, name_key) VALUES ('core.user.read', 'Ng''uoi dung');";

        PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test");
        PermissionSeedScanner.ParseInsertRows(sql, PermissionTable).Count.ShouldBe(1);
    }

    // ===== Detector_* GHIM RANH GIỚI ĐÃ BIẾT của lớp quét trong chuỗi (T1) =====
    //
    // Hai câu dưới đây HỢP LỆ và không câu nào ghi vào core.permission, nhưng cổng vẫn đỏ. Lớp quét
    // trong chuỗi gom MỌI chuỗi của toàn văn rồi tìm câu ghi trong từng chuỗi — nó không biết chuỗi
    // đó thuộc câu lệnh nào, nên không phân biệt được SQL động với dữ liệu tình cờ chứa văn bản SQL.
    //
    // ĐÂY LÀ TEST GHIM, KHÔNG PHẢI TEST LUẬT. Nó tồn tại vì hai lý do, và cả hai đều là chuyện đã
    // xảy ra ở họ lỗi này:
    //   • để ranh giới có mặt trong mã chứ không chỉ trong một đoạn văn xuôi không ai đọc;
    //   • để lần sau gặp đỏ-sai, không ai "sửa" bằng cách GỠ lớp quét trong chuỗi — gỡ nó là mở lại
    //     lỗ SQL động ở hai khối Detector_ ngay trên, và đó là lỗ có hậu quả thật.
    //
    // Cách sửa ĐÚNG khi gặp đỏ-sai: đổi cách viết câu SQL (tách chú thích ra khỏi tên bảng được
    // canh), hoặc — nếu chuyện này thành thường xuyên — dạy bộ đọc biết chuỗi thuộc câu lệnh nào,
    // tức mở rộng chứ không thu hẹp. Reviewer đã kiểm `COMMENT ON` trong database/scripts/core/*.sql
    // ở thời điểm viết: không tệp nào có, nên ranh giới này CHƯA cắn ai.
    [Theory]
    [InlineData("COMMENT ON TABLE core.permission IS 'Danh muc - khong bao gio DELETE FROM core.permission bang tay';")]
    [InlineData("INSERT INTO core.audit_log (note) VALUES ('Yeu cau: DELETE FROM core.permission da bi tu choi');")]
    public void Detector_B7Script_Pins_KnownFalseRed_WhenDataMerelyContainsSqlText(string sql)
    {
        var message = Should.Throw<InvalidOperationException>(
                () => PermissionSeedScanner.AssertNoDataMutation(sql, PermissionTable, "test"))
            .Message;

        // Thông điệp PHẢI nói ra khả năng thứ hai. Bản trước chỉ dặn "Mở rộng bộ đọc, KHÔNG bỏ qua
        // câu này" — với một chuỗi dữ liệu thì lời dặn đó đẩy người sửa thẳng tới việc gỡ lớp quét.
        message.ShouldContain("SQL động");
        message.ShouldContain("RANH GIỚI ĐÃ BIẾT");
        message.ShouldContain("ĐỪNG gỡ lớp quét");
    }

    // ===== Detector_* cho ON CONFLICT … DO UPDATE (T1) =====
    //
    // Ca NGUY HIỂM NHẤT của họ này, và là ca gần nhất với thứ đang có trong repo: script seed hiện
    // dùng `ON CONFLICT (code) WHERE is_deleted = false DO NOTHING`, và đổi sang `DO UPDATE SET …`
    // là sửa đổi tự nhiên khi muốn chạy lại script sau khi sửa danh mục. Đo được trước khi sửa:
    // AssertNoDataMutation PASS im lặng VÀ ParseInsertRows đọc ra 1 dòng — tức cổng đối chiếu cặp
    // khoá lấy từ VALUES rồi xanh, trong khi database nhận giá trị từ mệnh đề SET.

    [Fact]
    public void Detector_B7Script_Throws_On_UpsertDoUpdate_OnWatchedTable()
    {
        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.ParseInsertRows(
                """
                INSERT INTO core.permission (code, resource_key)
                VALUES ('core.user.read', 'core.user')
                ON CONFLICT (code) WHERE is_deleted = false
                DO UPDATE SET resource_key = EXCLUDED.resource_key;
                """,
                PermissionTable))
            .Message.ShouldContain("DO UPDATE");
    }

    // Ca đối thứ nhất: DO NOTHING KHÔNG đặt giá trị mới, nên dòng trong DB vẫn đúng bằng dòng vừa
    // đọc — đây là hình dạng script seed thật đang dùng và nó phải đọc được như cũ.
    [Fact]
    public void Detector_B7Script_Reads_UpsertDoNothing_OnWatchedTable()
    {
        var rows = PermissionSeedScanner.ParseInsertRows(
            """
            INSERT INTO core.permission (code, resource_key)
            VALUES ('core.user.read', 'core.user')
            ON CONFLICT (code) WHERE is_deleted = false DO NOTHING;
            """,
            PermissionTable);

        rows.Count.ShouldBe(1);
        rows[0]["code"].ShouldBe("core.user.read");
    }

    // Ca đối thứ hai: `DO UPDATE` của một bảng KHÁC trong cùng tệp không được kéo bảng này theo.
    // Script seed thật có INSERT vào core.schema_script_history ngay dưới khối seed, nên hai câu
    // nằm cùng tệp là hình dạng bình thường chứ không phải ca dựng ra.
    [Fact]
    public void Detector_B7Script_Ignores_UpsertDoUpdate_OnAnotherTable()
    {
        var rows = PermissionSeedScanner.ParseInsertRows(
            """
            INSERT INTO core.permission (code, resource_key)
            VALUES ('core.user.read', 'core.user')
            ON CONFLICT (code) DO NOTHING;

            INSERT INTO core.schema_script_history (script_name, note)
            VALUES ('0002__core__seed.sql', NULL)
            ON CONFLICT (script_name) DO UPDATE SET note = EXCLUDED.note;
            """,
            PermissionTable);

        rows.Count.ShouldBe(1);
    }

    // ===== Detector_* cho chiều ĐỎ SAI của phép quét trong chuỗi (T1) =====
    //
    // `''` là cách thoát dấu nháy đơn của SQL, và nội dung chuỗi sau khi gỡ thoát mang số nháy LẺ.
    // Bộ đọc nghiêm ngặt khi đó ném "chuỗi không được đóng" cho một tệp HOÀN TOÀN HỢP LỆ — đo được
    // trước khi sửa, và vì AssertNoDataMutation chạy trên TOÀN VĂN mỗi tệp .sql, một giá trị có dấu
    // nháy ở bất kỳ đâu trong tệp làm đỏ cả bốn test B7 phía script. Thông điệp lúc đó còn dặn
    // "Sửa SQL, đừng nới bộ đọc" trong khi SQL đúng, nên lối thoát tự nhiên là đi nới
    // AssertNoDataMutation — tức mở lại đúng lỗ mà khối Detector_ phía trên vừa bịt.

    [Fact]
    public void Detector_B7Script_Ignores_EscapedQuoteInValue()
        => PermissionSeedScanner.AssertNoDataMutation(
            "INSERT INTO core.permission (code, resource_key, name_key) "
          + "VALUES ('core.user.read', 'core.user', 'Ng''uoi dung');",
            PermissionTable, "test");

    [Fact]
    public void Detector_B7Script_Reads_EscapedQuoteInValue()
    {
        var rows = PermissionSeedScanner.ParseInsertRows(
            "INSERT INTO core.permission (code, resource_key, name_key) "
          + "VALUES ('core.user.read', 'core.user', 'Ng''uoi dung');",
            PermissionTable);

        rows.Count.ShouldBe(1);
        rows[0]["name_key"].ShouldBe("Ng'uoi dung");
    }

    // Chuỗi mang dấu nháy thoát trên một bảng KHÁC cũng không được làm đỏ — đây là hình dạng của
    // 0001__core__initial.sql, tệp mà cổng cũng quét toàn văn.
    [Fact]
    public void Detector_B7Script_Ignores_EscapedQuoteOnAnotherTable()
        => PermissionSeedScanner.AssertNoDataMutation(
            "INSERT INTO core.app_user (display_name) VALUES ('O''Brien');", PermissionTable, "test");

    // KHOAN DUNG KHÔNG ĐƯỢC THU HẸP CỔNG: SQL động mang dấu nháy thoát vẫn phải đỏ. Nếu bản sửa
    // chiều đỏ-sai đi bằng cách bỏ hẳn lớp quét trong chuỗi thì ca này bắt được.
    [Fact]
    public void Detector_B7Script_Throws_On_MutationInsideDynamicSqlString_WithEscapedQuotes()
    {
        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.AssertNoDataMutation(
                "EXECUTE 'DELETE FROM core.permission WHERE name_key = ''O''''Brien''';",
                PermissionTable, "test"))
            .Message.ShouldContain("SQL động");
    }

    // Nhánh KHOAN DUNG của phép bóc chuỗi chạy THẬT, và nó là lý do lời khai "mỗi tầng mất ít nhất
    // HAI ký tự bao" ở StringLiterals từng sai: một chuỗi KHÔNG ĐÓNG chỉ mất MỘT ký tự (`'abc` →
    // `abc`). Câu dưới đây đo đúng điều đó qua hành vi quan sát được: nội dung tầng một là
    // `x 'DELETE FROM core.permission` — một chuỗi mở mà không đóng — và tầng hai vẫn bóc được
    // `DELETE FROM core.permission` ra để quét. Kết luận "phép đệ quy luôn dừng" không đổi (phép
    // giảm vẫn nghiêm ngặt), chỉ lý do là khác.
    [Fact]
    public void Detector_B7Script_Throws_On_MutationInsideUnterminatedNestedString()
        => Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.AssertNoDataMutation(
                "EXECUTE 'x ''DELETE FROM core.permission';", PermissionTable, "test"))
            .Message.ShouldContain("SQL động");

    // Ranh giới của khoan dung ở MỨC TỆP: một chuỗi không đóng trong chính tệp .sql vẫn là lỗi ồn
    // ào, vì mọi câu lệnh sau nó trở nên vô hình với cổng.
    [Fact]
    public void Detector_B7Script_Throws_On_UnterminatedStringAtFileLevel()
    {
        Should.Throw<InvalidOperationException>(() => PermissionSeedScanner.AssertNoDataMutation(
                "INSERT INTO core.permission (code) VALUES ('core.user.read);",
                PermissionTable, "test"))
            .Message.ShouldContain("không được đóng bằng nháy đơn");
    }

    // ===== Hạ tầng =====

    private static IReadOnlyList<IReadOnlyDictionary<string, string?>> ScriptRows(string table)
        => ScriptFiles()
            .SelectMany(file =>
            {
                var sql = File.ReadAllText(file);
                PermissionSeedScanner.AssertNoDataMutation(sql, table, $"Script '{Path.GetFileName(file)}'");
                return PermissionSeedScanner.ParseInsertRows(sql, table);
            })
            .ToList();

    private static IReadOnlyList<string> ScriptFiles()
        => [.. Directory.EnumerateFiles(ScriptDirectory(), "*.sql").OrderBy(f => f, StringComparer.Ordinal)];

    private static string ScriptDirectory([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(here)!, "..", "..", "..", "..", "database", "scripts", "core"));

    // Phía migration đọc qua ĐÚNG bộ đọc mà PermissionSeedParityTests dùng — nếu tách ra một bộ đọc
    // thứ hai thì hai cổng sẽ lệch nhau và không ai thấy.
    private static IReadOnlyList<IReadOnlyDictionary<string, string?>> MigrationRows(string table)
        => ArchitectureFixture.InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false }
                     && typeof(Migration).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .Select(t => (Migration)Activator.CreateInstance(t, nonPublic: true)!)
            .SelectMany(migration => migration.UpOperations)
            .SelectMany(operation => PermissionSeedScanner.ReadRows(operation, table))
            .ToList();
}
