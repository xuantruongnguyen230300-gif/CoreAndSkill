using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Cổng cho một luật T10, docs/RULES-BE.md — nửa ĐỌC. Nửa GHI ở GlobalMetricStateTests: lớp test ghi
// `CoreMetrics.SetOutboxSnapshot` phải chung `OutboxSnapshotCollection`. Nửa này: mã nguồn của
// `CoreAndSkill.Core.IntegrationTests` KHÔNG được đọc ba gauge outbox (`core.outbox.pending`,
// `core.outbox.pending_oldest_age`, `core.outbox.dead_count`) — lớp đọc gauge chỉ được sống trong
// `CoreAndSkill.Core.UnitTests`.
//
// Vì sao: `OutboxDatabaseTests` ghi ảnh chụp outbox theo đường GIÁN TIẾP (vòng phát thật gọi `MeasureAsync`) và nằm
// trong `PostgresCollection`, nên không vào được `OutboxSnapshotCollection`; T10 chỉ so lời gọi trực tiếp nên không
// thấy nó. Một lớp đọc gauge thêm vào IntegrationTests sẽ chạy song song với nó và đọc ra số bị ghi đè — đỏ ngẫu nhiên
// mà cổng T10 vẫn xanh. Hôm nay chưa hỏng vì lớp đọc gauge chỉ ở UnitTests, một tiến trình khác; cổng này giữ cho nó
// đúng như vậy. Điểm mù của bộ dò khai ở chú thích đầu `Support/OutboxGaugeReadScanner.cs`.
public class OutboxGaugeReadTests
{
    private const string IntegrationProject = "CoreAndSkill.Core.IntegrationTests/";
    private const string UnitProject = "CoreAndSkill.Core.UnitTests/";

    // ===== Luật thật =====

    [Fact]
    public void IntegrationTestSources_MustNotReadTheOutboxGauges()
    {
        var offenders = ReadsIn(IntegrationProject)
            .Select(read => $"{TestSourceFiles.RelativePath(read.FilePath)}: {read.TypeName} đọc gauge outbox ({read.Text}) — "
                          + "ảnh chụp outbox là biến tĩnh toàn tiến trình và OutboxDatabaseTests ghi nó theo đường gián tiếp "
                          + "ngoài OutboxSnapshotCollection; lớp đọc gauge phải nằm trong CoreAndSkill.Core.UnitTests (T10)")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // ===== Chốt chống xanh rỗng (T6) =====

    // Cổng trên PASS ngay khi thư mục quét rỗng — đường dẫn project đổi tên là đủ để nó xanh rỗng.
    [Fact]
    public void TheOutboxGaugeReadGate_ScansAtLeastOneIntegrationTestFile()
        => FilesIn(IntegrationProject).ShouldNotBeEmpty(
            $"không có tệp .cs nào dưới src/BE/Tests/{IntegrationProject} — project đã đổi tên hoặc dời đi, và cổng đang xanh rỗng.");

    // Hai test dưới chứng minh bộ dò nhận ra CẢ HAI hình dạng đọc trên mã THẬT (lớp đọc gauge có thật ở UnitTests).
    // Không có chúng thì bộ dò hỏng hoàn toàn vẫn cho `IntegrationTestSources_...` xanh.
    [Fact]
    public void TheOutboxGaugeReadGate_RecognisesARealMemberReferenceRead()
        => ReadsIn(UnitProject).Where(read => read.Kind == OutboxGaugeReadKind.MemberReference).ShouldNotBeEmpty(
            "bộ dò không thấy tham chiếu CoreMetrics.Outbox<gauge> nào ở UnitTests — hình dạng đọc đã đổi hoặc bộ dò hỏng.");

    [Fact]
    public void TheOutboxGaugeReadGate_RecognisesARealInstrumentNameRead()
        => ReadsIn(UnitProject).Where(read => read.Kind == OutboxGaugeReadKind.InstrumentName).ShouldNotBeEmpty(
            "bộ dò không thấy chuỗi tên gauge outbox nào ở UnitTests — hình dạng đọc đã đổi hoặc bộ dò hỏng.");

    // Tập tên gauge của bộ dò đối chiếu với nguồn thật: gauge outbox thứ tư thêm bên Core, hay đổi tên một gauge, là đỏ
    // ở đây chứ không để cổng lặng lẽ canh một tập cũ.
    [Fact]
    public void GaugeSets_MatchTheRealCoreMetricsSource()
    {
        var path = ProductSourceFiles.Core().FirstOrDefault(p => ProductSourceFiles.EndsWith(p, "Diagnostics", "CoreMetrics.cs"));
        path.ShouldNotBeNull("không tìm thấy Diagnostics/CoreMetrics.cs trong sản phẩm — tệp đã dời, và cổng không còn đối chiếu được.");

        var declared = OutboxGaugeReadScanner.DeclaredOutboxGauges(File.ReadAllText(path));

        declared.Select(d => d.Member).ShouldBe(OutboxGaugeReadScanner.MemberNames, ignoreOrder: true);
        declared.Select(d => d.InstrumentName).ShouldBe(OutboxGaugeReadScanner.InstrumentNames, ignoreOrder: true);
    }

    // ===== Detector_T10_* (T1) — gọi đúng OutboxGaugeReadScanner.Reads mà cổng thật đang chạy =====

    [Theory]
    [InlineData("OutboxPending")]
    [InlineData("OutboxOldestPendingAge")]
    [InlineData("OutboxDeadCount")]
    public void Detector_T10_Catches_EachGaugeMemberReference(string member)
    {
        var source = $$"""
            namespace Fake;

            public class FakeGaugeTests
            {
                [Fact]
                public void Reads() { using var probe = new MeterProbe(CoreMetrics.{{member}}); }
            }
            """;

        var read = OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldHaveSingleItem();
        read.TypeName.ShouldBe("FakeGaugeTests");
        read.Kind.ShouldBe(OutboxGaugeReadKind.MemberReference);
        read.Text.ShouldBe(member);
    }

    [Theory]
    [InlineData("core.outbox.pending")]
    [InlineData("core.outbox.pending_oldest_age")]
    [InlineData("core.outbox.dead_count")]
    public void Detector_T10_Catches_EachInstrumentNameLiteral(string name)
    {
        var source = $$"""
            namespace Fake;

            public class FakeGaugeTests
            {
                [Fact]
                public void Reads() => Probe.Value("{{name}}");
            }
            """;

        var read = OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldHaveSingleItem();
        read.Kind.ShouldBe(OutboxGaugeReadKind.InstrumentName);
        read.Text.ShouldBe(name);
    }

    [Fact]
    public void Detector_T10_Catches_AFullyQualifiedMemberReference()
    {
        const string source = """
            namespace Fake;

            public class FakeGaugeTests
            {
                private readonly object _g = CoreAndSkill.Core.Application.Diagnostics.CoreMetrics.OutboxPending;
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldHaveSingleItem().Kind.ShouldBe(OutboxGaugeReadKind.MemberReference);
    }

    [Fact]
    public void Detector_T10_Catches_ABareGaugeNameUnderUsingStatic()
    {
        const string source = """
            using static CoreAndSkill.Core.Application.Diagnostics.CoreMetrics;
            namespace Fake;

            public class FakeGaugeTests
            {
                private readonly object _g = OutboxDeadCount;
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldHaveSingleItem().Text.ShouldBe("OutboxDeadCount");
    }

    [Fact]
    public void Detector_T10_Catches_AnInstrumentNameInAnInterpolatedStringWithNoHole()
    {
        const string source = """
            namespace Fake;

            public class FakeGaugeTests
            {
                public double Reads() => Probe.Value($"core.outbox.pending");
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldHaveSingleItem().Kind.ShouldBe(OutboxGaugeReadKind.InstrumentName);
    }

    // Lời gọi lồng sâu trong lambda vẫn là lần đọc — chỗ dễ lọt nhất của phép dò theo AST.
    [Fact]
    public void Detector_T10_Catches_AReadNestedInsideALambda()
    {
        const string source = """
            namespace Fake;

            public class FakeGaugeTests
            {
                public void Reads() => Enumerable.Range(0, 2).ToList().ForEach(_ => Probe.Value("core.outbox.dead_count"));
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldHaveSingleItem();
    }

    // Kiểu lồng trong lớp test: quy về lớp NGOÀI CÙNG, là nơi người sửa cần tới.
    [Fact]
    public void Detector_T10_AttributesANestedTypeToItsOuterTestClass()
    {
        const string source = """
            namespace Fake;

            public class FakeGaugeTests
            {
                private sealed class Helper
                {
                    public object G => CoreMetrics.OutboxPending;
                }
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldHaveSingleItem().TypeName.ShouldBe("FakeGaugeTests");
    }

    // Chú thích nhắc tên gauge KHÔNG phải lần đọc — chỗ một phép dò quét văn bản sẽ báo oan.
    [Fact]
    public void Detector_T10_Ignores_AMentionInACommentOrXmlDoc()
    {
        const string source = """
            namespace Fake;

            // Lớp này KHÔNG đọc CoreMetrics.OutboxPending hay "core.outbox.pending", chỉ nhắc tới.
            public class FakeGaugeTests
            {
                /// <summary>core.outbox.dead_count và CoreMetrics.OutboxDeadCount.</summary>
                /* probe.Value("core.outbox.pending_oldest_age") */
                public void DoesNotRead() { }
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldBeEmpty();
    }

    // Tên BẢNG `core.outbox_message` có mặt khắp IntegrationTests (câu SQL) — khớp tiền tố sẽ báo oan hàng loạt.
    [Theory]
    [InlineData("SELECT count(*) FROM core.outbox_message WHERE status = 'done'")]
    [InlineData("core.outbox_message")]
    [InlineData("core.outbox.pending_something_else")]
    [InlineData("core.outbox")]
    public void Detector_T10_Ignores_AStringThatOnlyResemblesAGaugeName(string text)
    {
        var source = $$"""
            namespace Fake;

            public class FakeGaugeTests
            {
                public string Sql => "{{text}}";
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldBeEmpty();
    }

    // Counter outbox (`dispatched`, `dead`, `attempt_failed`) KHÔNG đọc từ biến tĩnh — MeterProbe cô lập được theo luồng,
    // nên IntegrationTests được đo chúng. Bộ dò đọc rộng hơn thực tế sẽ chặn nhầm đúng những test đó.
    [Fact]
    public void Detector_T10_Ignores_TheOutboxCounters()
    {
        const string source = """
            namespace Fake;

            public class FakeGaugeTests
            {
                public void Counts()
                {
                    using var probe = new MeterProbe(CoreMetrics.OutboxDispatched, CoreMetrics.OutboxDead, CoreMetrics.OutboxAttemptFailed);
                    probe.Count("core.outbox.dispatched");
                    probe.Count("core.outbox.dead");
                }
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldBeEmpty();
    }

    [Fact]
    public void Detector_T10_Ignores_AMemberOfAnotherTypeWithTheSameName()
    {
        const string source = """
            namespace Fake;

            public class FakeGaugeTests
            {
                public object Reads(Dashboard dashboard) => dashboard.OutboxPending;
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldBeEmpty();
    }

    [Fact]
    public void Detector_T10_Ignores_ANamedArgument()
    {
        const string source = """
            namespace Fake;

            public class FakeGaugeTests
            {
                public void Builds() => Build(OutboxPending: 3, OutboxDeadCount: 0);
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldBeEmpty();
    }

    [Fact]
    public void Detector_T10_Ignores_AnInterpolatedStringWithAHole()
    {
        const string source = """
            namespace Fake;

            public class FakeGaugeTests
            {
                public string Name(string suffix) => $"core.outbox.pending{suffix}";
            }
            """;

        OutboxGaugeReadScanner.Reads(source, "fake.cs").ShouldBeEmpty();
    }

    // ===== Hạ tầng =====

    private static IReadOnlyList<string> FilesIn(string projectPrefix)
        => TestSourceFiles.All()
            .Where(path => TestSourceFiles.RelativePath(path).StartsWith(projectPrefix, StringComparison.Ordinal))
            .ToList();

    private static IReadOnlyList<OutboxGaugeRead> ReadsIn(string projectPrefix)
        => FilesIn(projectPrefix)
            .SelectMany(file => OutboxGaugeReadScanner.Reads(File.ReadAllText(file), file))
            .ToList();
}
