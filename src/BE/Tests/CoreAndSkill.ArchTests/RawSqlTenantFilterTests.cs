using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Cổng cho luật M6 (docs/RULES.md §9; văn bản gốc docs/wiki-core/be/17-multi-tenant.md §8; ADR-0071 mục *Việc test*):
// truy vấn SQL thô trên bảng có `tenant_id` phải TỰ thêm điều kiện đơn vị, hoặc nằm trong allowlist khai kèm lý do.
//
// M5 hỏi *ai được bỏ bộ lọc*; M6 hỏi *câu SQL nào tự lọc lấy*. Hai câu hỏi, hai danh sách, không giao nhau.
//
// Điểm mù của phép dò khai ngay đầu Support/RawSqlTenantScanner.cs — đọc ở đó, không chép lại ở đây.
public class RawSqlTenantFilterTests
{
    // Allowlist khai MỘT chỗ, mỗi mục kèm lý do. Đường dẫn tương đối so với src/BE, dùng dấu `/`.
    //
    // 🛑 KHÔNG có chốt "allowlist khác rỗng" — khác hẳn T11. Ở T11 tệp dùng chung PHẢI luôn được miễn trừ; ở đây
    // RỖNG LÀ TRẠNG THÁI ĐÍCH (ADR-0071 quyết định 5). Thêm một chốt như vậy là biến đích thành vi phạm.
    //
    // RỖNG HÔM NAY, và rỗng vì một lý do đã đạt chứ không vì chưa ai điền. ADR-0071 khai đúng một mục —
    // `JobRecoveryHostedService.CandidateSql` — kèm ĐIỀU KIỆN GỠ là phương án C của chính ADR đó (viết lại câu SQL
    // thô thành LINQ kèm `IgnoreQueryFilters([CoreQueryFilters.TenantKey])`). Phương án C đã về: `Candidates(db)`
    // trong `JobRecoveryHostedService` là LINQ, không còn hằng `CandidateSql` nào. Mục vì thế thành mục ruỗng và
    // được gỡ — đúng lối mà chốt EveryAllowlistEntry_PointsAtACallThatWouldOtherwiseViolate đòi.
    //
    // Mỗi mục thêm vào sau này phải nêu CÁI GÌ LẬP LẠI PHẠM VI ĐƠN VỊ (quyết định 4). Lý do kiểu "job nền thì phải
    // quét toàn hệ" chưa đủ: nó nói vì sao ĐỌC rộng, không nói vì sao GHI không rộng theo.
    //
    // ⚠️ Allowlist rỗng nghĩa là hai chốt hygiene dưới đây xét 0 mục — chúng đúng nhưng KHÔNG chứng minh được gì về
    // bộ máy allowlist. Chỗ đó đi bằng canary, không đi bằng chốt đếm: nhóm Detector_M6_Allowlist_* dựng một
    // allowlist giả và chứng minh cả ba hành vi (miễn trừ đúng chỗ · không miễn trừ sai chỗ · bắt mục ruỗng).
    private static readonly AllowlistEntry[] Allowlist = [];

    internal sealed record AllowlistEntry(string RelativePath, string Symbol, string Reason);

    // ===== Luật thật =====

    [Fact]
    public void EveryRawSqlOnTenantTable_FiltersByTenant()
    {
        var offenders = Evaluate(AllCalls(), TenantTables(), Allowlist).Select(v => v.Message).ToList();

        offenders.ShouldBeEmpty();
    }

    // ===== Chốt chống xanh rỗng (T6) — ba cái, ADR-0071 điểm 6 =====

    // Tập bảng đọc từ DDL. Thư mục đổi chỗ hay cú pháp `CREATE TABLE` đổi dạng thì tập về rỗng, và khi tập bảng rỗng
    // thì MỌI câu SQL đều "không chạm bảng có đơn vị" — cổng in PASS trong khi nó không xét gì.
    [Fact]
    public void TheRawSqlGate_ReadsTheTenantTablesFromTheRealDdl()
    {
        var tables = TenantTables();

        tables.ShouldNotBeEmpty(
            $"không đọc được bảng nào có cột tenant_id từ '{ProductSourceFiles.CoreDdlDirectory()}' — thư mục DDL đã "
          + "dời, hoặc cú pháp CREATE TABLE đổi dạng. Tập bảng rỗng làm MỌI câu SQL đi lọt và cổng M6 xanh rỗng.");

        tables.ShouldContain("core.job");
        tables.ShouldContain("core.outbox_message");
        tables.ShouldContain("core.notification_recipient", "bảng dùng cho canary phải có thật trong DDL");
        tables.ShouldContain("core.audit_log");

        // Ca đối chứng ÂM: bảng danh mục quyền không có cột tenant_id. Một phép đọc DDL hỏng theo chiều "cho mọi bảng
        // vào tập" vẫn qua được ba dòng trên; chỉ dòng này bắt được nó.
        tables.ShouldNotContain("core.permission");
        tables.ShouldNotContain("core.tenant");
    }

    // Tập lời gọi SQL thô đọc từ AST. Chốt này không đếm — nó ghim từng HÌNH DẠNG phải đọc được; một con số ở đây
    // sẽ mục ruỗng ngay lần thêm lời gọi tiếp theo (.claude/CLAUDE.md §6).
    [Fact]
    public void TheRawSqlGate_ReadsTheRealRawSqlCalls()
    {
        var calls = AllCalls();

        calls.ShouldNotBeEmpty("không đọc được lời gọi SQL thô nào trong source sản phẩm — bộ dò hỏng, cổng xanh rỗng.");

        // Hình dạng dễ quên nhất: hằng gán ở CHỖ KHÁC rồi truyền vào (ADR-0071 điểm 2). Một bộ dò chỉ đọc chuỗi viết
        // thẳng trong ngoặc sẽ đọc `SetLockTimeoutSql` thành "không có SQL" và bỏ qua trong im lặng.
        calls.ShouldContain(
            c => c.Symbol == "SetLockTimeoutSql" && c.Sql != null && c.Sql.Contains("lock_timeout"),
            "không gỡ được câu SQL ra khỏi một hằng khai ở chỗ khác — phép dò bỏ sót đúng hình dạng khó nhất");

        // Ba hình dạng còn lại phải cùng vào tầm: chuỗi nội suy, chuỗi thô nhiều dòng, và lối migration.
        calls.ShouldContain(c => c.Method == "FromSql", "không đọc được lời gọi FromSql nào");
        calls.ShouldContain(c => c.Method.StartsWith("ExecuteSql"), "không đọc được lời gọi ExecuteSql* nào");
        calls.ShouldContain(c => c.Method == "migrationBuilder.Sql", "không đọc được migrationBuilder.Sql nào");

        // Không lời gọi nào được rơi vào nhánh "bộ đọc không hiểu" một cách âm thầm — nếu có, nó phải là FAIL của
        // cổng chính, và chốt này nói ra ngay tại đây rằng hôm nay không có ca nào như thế.
        calls.Where(c => c.Sql is null).ShouldBeEmpty(
            "có lời gọi SQL thô mà bộ đọc không gỡ được câu SQL — xem thông điệp của cổng chính");
    }

    // Canary — ADR-0071 điểm 6. Dò trên một chuỗi dựng tại chỗ, nên chốt này đúng kể cả khi repo hết sạch vi phạm.
    [Fact]
    public void Detector_M6_Catches_ASelectOnATenantTableWithoutATenantClause()
    {
        const string sql = "SELECT id, created_at FROM core.notification_recipient WHERE is_read = false";

        RawSqlTenantScanner.TenantTablesTouched(sql, TenantTables())
            .ShouldContain("core.notification_recipient", "phép nhận diện bảng chết — mọi số 0 của cổng M6 là giả");

        RawSqlTenantScanner.HasTenantPredicate(sql)
            .ShouldBeFalse("phép nhận diện mệnh đề đơn vị báo có trong khi câu không hề nhắc tenant_id");
    }

    // ===== Chốt hygiene của allowlist (ADR-0071 điểm 7) =====
    //
    // Hai chốt này xét 0 mục hôm nay. Chúng vẫn phải có mặt: mục đầu tiên được thêm vào sẽ được xét ngay, và chi phí
    // của việc thêm mục — phải trỏ vào một lời gọi thật, phải nêu lý do, phải nêu cơ chế — là thứ giữ allowlist nhỏ.

    [Fact]
    public void EveryAllowlistEntry_PointsAtACallThatWouldOtherwiseViolate()
        => StaleEntries(AllCalls(), TenantTables(), Allowlist).ShouldBeEmpty();

    [Fact]
    public void EveryAllowlistEntry_StatesAReason()
        => Allowlist
            .Where(entry => string.IsNullOrWhiteSpace(entry.Reason))
            .Select(entry => $"{entry.RelativePath} · {entry.Symbol}")
            .ShouldBeEmpty();

    // Quyết định 4 của ADR-0071: lý do phải nêu CƠ CHẾ lập lại phạm vi đơn vị, không chỉ nêu "vì sao cần". Cổng không
    // đọc hiểu được văn xuôi, nên nó đòi cái máy kiểm được: tên cơ chế đó phải có mặt trong lý do.
    [Fact]
    public void EveryAllowlistEntry_NamesTheMechanismThatReopensTenantScope()
        => VagueEntries(Allowlist).ShouldBeEmpty();

    // ===== Detector_* (T1) — ADR-0071 điểm 8 =====

    [Fact]
    public void Detector_M6_Catches_ASqlQueryRawOnATenantTableWithoutATenantClause()
    {
        // Hình dạng của bản `CandidateSql` đã được gỡ khỏi JobRecoveryHostedService bằng phương án C của ADR-0071.
        // Giữ nó ở đây làm ca hồi quy: viết lại đúng hình dạng đó thì cổng phải đỏ, không im lặng.
        const string source = """"
            namespace Fake;

            internal sealed class FakeService
            {
                private const string CandidateSql = """
                    SELECT j.id AS "Id", j.tenant_id AS "TenantId"
                    FROM core.job j
                    WHERE j.is_deleted = false AND j.status = 'running'
                    """;

                public void Run(FakeDb db) => db.Database.SqlQueryRaw<Row>(CandidateSql);
            }
            """";

        Offenders(source).ShouldNotBeEmpty();
    }

    // SQL thô KHÔNG chạm bảng nào — `SET LOCAL lock_timeout`. Hình dạng thật của Persistence/LockTimeout.cs.
    [Fact]
    public void Detector_M6_Ignores_AStatementThatTouchesNoTable()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeService
            {
                private const string SetLockTimeout = "SET LOCAL lock_timeout = '5s'";

                public void Run(FakeDb db) => db.Database.ExecuteSqlRawAsync(SetLockTimeout);
            }
            """;

        Offenders(source).ShouldBeEmpty();
    }

    [Fact]
    public void Detector_M6_Ignores_ASqlThatCarriesATenantPredicate()
    {
        const string source = """"
            namespace Fake;

            internal sealed class FakeService
            {
                public void Run(FakeDb db, System.Guid tenantId) => db.MenuItems.FromSql($"""
                    SELECT * FROM core.menu_item WHERE tenant_id = {tenantId} AND is_deleted = false
                    """);
            }
            """";

        Offenders(source).ShouldBeEmpty();
    }

    // Chú thích nhắc câu SQL KHÔNG phải câu SQL — chỗ một phép dò quét văn bản báo oan. Roslyn coi chú thích là
    // trivia, nên chốt này ghim rằng cổng đi bằng AST chứ không bằng grep.
    [Fact]
    public void Detector_M6_Ignores_SqlWrittenInAComment()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeService
            {
                // Trước đây chỗ này chạy db.Database.SqlQueryRaw<Row>("SELECT * FROM core.job") — đã bỏ.
                /* var rows = db.Database.SqlQueryRaw<Row>("SELECT * FROM core.outbox_message"); */
                public int Run() => 0;
            }
            """;

        Offenders(source).ShouldBeEmpty();
    }

    // Mã vi phạm nằm trong CHUỖI KÝ TỰ — đúng hình dạng của chính các ca Detector_* trong tệp này. Quét văn bản sẽ
    // báo oan tệp cổng, và người ta sẽ gỡ cổng đi thay vì gỡ báo oan. Cùng bẫy mà
    // Detector_T11_Ignores_AConstructionInsideAStringLiteral đã gặp.
    [Fact]
    public void Detector_M6_Ignores_SqlInsideAStringLiteralOfTheGateFile()
    {
        const string source = """"
            namespace Fake;

            internal sealed class FakeGateTests
            {
                private const string Sample = "db.Database.SqlQueryRaw<Row>(\"SELECT * FROM core.job\")";

                private const string Block = """
                    public void Run(FakeDb db) => db.Database.SqlQueryRaw<Row>("SELECT * FROM core.notification");
                    """;
            }
            """";

        Offenders(source).ShouldBeEmpty();
    }

    // Phép chiếu KHÔNG phải vị từ. Đây là hình dạng của `CandidateSql` cũ: nó nhắc `tenant_id` trong danh sách SELECT
    // mà không lọc theo nó. Một phép dò tìm chữ `tenant_id` ở bất kỳ đâu sẽ cho ca này đi lọt, và mục allowlist của
    // nó lập tức thành mục ruỗng — cổng xanh cả hai đầu.
    [Fact]
    public void Detector_M6_Catches_ATenantIdThatIsOnlyProjected()
    {
        const string sql = """SELECT j.id AS "Id", j.tenant_id AS "TenantId" FROM core.job j WHERE j.status = 'running'""";

        RawSqlTenantScanner.HasTenantPredicate(sql).ShouldBeFalse();
    }

    // `actor_tenant_id` là một cột KHÁC (core.audit_log). Lọc theo nó không phải lọc theo đơn vị sở hữu dòng.
    [Fact]
    public void Detector_M6_Ignores_AColumnMerelyEndingInTenantId()
        => RawSqlTenantScanner.HasTenantPredicate("SELECT * FROM core.audit_log WHERE actor_tenant_id = @p")
            .ShouldBeFalse();

    // Tên bảng xuất hiện như GIÁ TRỊ chứ không ở vị trí bảng — `'core.job'` trong câu seed.
    // Bài học docs/audit/2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md — neo vào VAI của chuỗi, không neo vào vùng chứa nó.
    [Fact]
    public void Detector_M6_Ignores_ATableNameAppearingAsAValue()
        => RawSqlTenantScanner.TableReferences("INSERT INTO core.permission (code) VALUES ('core.job')")
            .ShouldNotContain("core.job");

    // `FOR UPDATE SKIP LOCKED` là mệnh đề khoá dòng, không phải câu UPDATE. Hình dạng thật của OutboxDispatcher.
    [Fact]
    public void Detector_M6_Ignores_ForUpdateAsATableIntroducer()
        => RawSqlTenantScanner.TableReferences("SELECT * FROM core.outbox_message WHERE id = @p FOR UPDATE SKIP LOCKED")
            .ShouldBe(["core.outbox_message"]);

    // Bảng viết không kèm lược đồ vẫn phải khớp tập đọc từ DDL — nếu không, `FROM job` là đường vòng qua cả cổng.
    [Fact]
    public void Detector_M6_Catches_ATenantTableWrittenWithoutItsSchema()
        => RawSqlTenantScanner.TenantTablesTouched("SELECT * FROM job WHERE status = 'running'", TenantTables())
            .ShouldContain("core.job");

    // Đối số mà bộ đọc KHÔNG hiểu phải làm cổng ĐỎ, không đi qua yên lặng — cùng chiều với B7. Câu ghép lúc chạy là
    // ca thật của chuyện đó: nó có thể chạm bảng nào cũng được và không ai đọc ra được từ source.
    [Fact]
    public void Detector_M6_Catches_ASqlArgumentItCannotRead()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeService
            {
                public void Run(FakeDb db, string table) => db.Database.SqlQueryRaw<Row>(Build(table));

                private static string Build(string table) => "SELECT * FROM " + table;
            }
            """;

        Offenders(source).ShouldNotBeEmpty();
    }

    // ===== Detector_M6_Allowlist_* — bộ máy allowlist khi danh sách thật đang RỖNG =====
    //
    // Allowlist rỗng là trạng thái đích (quyết định 5), nhưng nó cũng có nghĩa là không mục nào chứng minh bộ máy
    // miễn trừ còn sống. Ba ca dưới đây dựng một allowlist giả để chứng minh — cùng kỷ luật "đi bằng canary, không đi
    // bằng chốt đếm" mà E15 dùng khi tập đầu vào rỗng.

    private const string FakeViolatingSource = """
        namespace Fake;

        internal sealed class FakeService
        {
            private const string Wide = "SELECT id FROM core.job WHERE status = 'running'";

            public void Run(FakeDb db) => db.Database.SqlQueryRaw<Row>(Wide);
        }
        """;

    [Fact]
    public void Detector_M6_Allowlist_SuppressesAMatchingCall()
    {
        AllowlistEntry[] allowlist =
            [new("fake.cs", "Wide", "executionContextScope.Enter mở lại phạm vi cho từng việc ngay sau phép đọc rộng")];

        Evaluate(FakeCalls(), TenantTables(), allowlist).ShouldBeEmpty();
    }

    // Mục khai theo CẶP (đường dẫn, tên) — miễn trừ một tên không được kéo theo cả tệp.
    [Fact]
    public void Detector_M6_Allowlist_DoesNotSuppressADifferentSymbolInTheSameFile()
    {
        AllowlistEntry[] allowlist = [new("fake.cs", "SomethingElse", "executionContextScope.Enter …")];

        Evaluate(FakeCalls(), TenantTables(), allowlist).ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_M6_Allowlist_FlagsAnEntryThatMatchesNothing()
    {
        AllowlistEntry[] allowlist = [new("fake.cs", "DaDoiDi", "executionContextScope.Enter …")];

        StaleEntries(FakeCalls(), TenantTables(), allowlist).ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_M6_Allowlist_FlagsAReasonThatNamesNoMechanism()
    {
        AllowlistEntry[] allowlist = [new("fake.cs", "Wide", "Job nền thì phải quét toàn hệ.")];

        VagueEntries(allowlist).ShouldNotBeEmpty();
    }

    // ---- bộ máy dùng chung của tệp ---------------------------------------------------------------

    private static IReadOnlyList<string> TenantTables()
        => RawSqlTenantScanner.TenantTables(ProductSourceFiles.CoreDdlDirectory());

    private static IReadOnlyList<RawSqlTenantScanner.RawSqlCall> AllCalls()
        => [.. ProductSourceFiles.Core().SelectMany(file => RawSqlTenantScanner.Calls(File.ReadAllText(file), file))];

    private static IReadOnlyList<RawSqlTenantScanner.RawSqlCall> FakeCalls()
        => RawSqlTenantScanner.Calls(FakeViolatingSource, "fake.cs");

    // Phép xét của cổng, tách khỏi nguồn dữ liệu để nhóm Detector_* chạy được đúng phép xét ấy trên dữ liệu dựng sẵn.
    private static IReadOnlyList<(string RelativePath, string Symbol, string Message)> Evaluate(
        IReadOnlyList<RawSqlTenantScanner.RawSqlCall> calls,
        IReadOnlyList<string> tenantTables,
        IReadOnlyList<AllowlistEntry> allowlist)
    {
        var result = new List<(string, string, string)>();

        foreach (var (relative, call) in calls.Select(c => (Relative(c.FilePath), c)))
        {
            if (allowlist.Any(e => e.RelativePath == relative && e.Symbol == call.Symbol))
                continue;

            if (call.Sql is null)
            {
                result.Add((relative, call.Symbol,
                    $"{relative}:{call.Line} · {call.Method}({call.Symbol}) — bộ đọc KHÔNG gỡ được câu SQL ra khỏi đối "
                  + "số, nên không ai biết nó chạm bảng nào. Câu ghép lúc chạy nằm ngoài tầm cổng M6: đưa nó về một "
                  + "hằng đọc được, hoặc về LINQ."));
                continue;
            }

            var touched = RawSqlTenantScanner.TenantTablesTouched(call.Sql, tenantTables);
            if (touched.Count == 0 || RawSqlTenantScanner.HasTenantPredicate(call.Sql))
                continue;

            result.Add((relative, call.Symbol,
                $"{relative}:{call.Line} · {call.Method}({call.Symbol}) chạm {string.Join(", ", touched)} — bảng có "
              + "tenant_id — mà câu SQL không có mệnh đề nào trên tenant_id (luật M6). Ba lối hợp lệ: thêm mệnh đề "
              + "`tenant_id = {tenantId}` tham số hoá · viết lại bằng LINQ để bộ lọc toàn cục làm việc · thêm một mục "
              + "vào Allowlist của RawSqlTenantFilterTests kèm lý do NÊU TÊN cơ chế lập lại phạm vi đơn vị. "
              + "🛑 Nới câu luật hay gỡ cổng KHÔNG nằm trong ba lối đó."));
        }

        return result;
    }

    private static IReadOnlyList<string> StaleEntries(
        IReadOnlyList<RawSqlTenantScanner.RawSqlCall> calls,
        IReadOnlyList<string> tenantTables,
        IReadOnlyList<AllowlistEntry> allowlist)
    {
        var offending = Evaluate(calls, tenantTables, [])
            .Select(v => $"{v.RelativePath}|{v.Symbol}")
            .ToHashSet(StringComparer.Ordinal);

        return [.. allowlist
            .Where(entry => !offending.Contains($"{entry.RelativePath}|{entry.Symbol}"))
            .Select(entry => $"{entry.RelativePath} · {entry.Symbol} — mục allowlist không còn miễn trừ gì (lời gọi đã "
                           + "dời, đổi tên, đã tự thêm mệnh đề đơn vị, hoặc bảng không còn có tenant_id). Gỡ mục đi — "
                           + "allowlist RỖNG là trạng thái đích của M6, không phải lỗi.")];
    }

    private static IReadOnlyList<string> VagueEntries(IReadOnlyList<AllowlistEntry> allowlist)
    {
        string[] mechanisms = ["executionContextScope.Enter", "IExecutionContextScope.Enter", "backfill toàn hệ"];

        return [.. allowlist
            .Where(entry => !mechanisms.Any(m => entry.Reason.Contains(m, StringComparison.Ordinal)))
            .Select(entry => $"{entry.RelativePath} · {entry.Symbol} — lý do không nêu cơ chế nào lập lại phạm vi đơn "
                           + "vị sau phép đọc rộng. 'Job nền thì phải quét toàn hệ' nói vì sao ĐỌC rộng, không nói vì "
                           + "sao GHI không rộng theo (ADR-0071 quyết định 4).")];
    }

    // Đường dẫn của source dựng sẵn không nằm dưới src/BE — giữ nguyên để nhóm Detector_* khai allowlist bằng "fake.cs".
    private static string Relative(string path)
        => path == "fake.cs" ? path : ProductSourceFiles.RelativePath(path);

    // Dùng cho nhóm Detector_* đọc một source dựng tại chỗ với allowlist rỗng.
    private static IReadOnlyList<string> Offenders(string source)
        => [.. Evaluate(RawSqlTenantScanner.Calls(source, "fake.cs"), TenantTables(), []).Select(v => v.Message)];
}
