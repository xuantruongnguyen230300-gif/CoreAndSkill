using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Cổng cho luật T11 (docs/DEBT.md, [ADR-0070]): kỹ thuật cô lập phép đo chỉ có MỘT bản trong
// src/BE/Tests. Lọc phép đo theo luồng logic sống ở `Tests/Shared/MeterProbe.cs` — tệp nguồn nối vào
// từng project test bằng <Compile Include> — và không lớp test nào tự dựng `MeterListener` của riêng nó.
//
// Vì sao cần cổng chứ không phải một dòng chú thích trong ADR: bản chép trước đây trong
// `CoreAndSkill.Core.IntegrationTests` (`CountFailedJobMetricDuring`) đã lệch khỏi bản gốc NGAY TỪ LÚC
// SINH RA — nó không khôi phục `AsyncLocal` cũ và chỉ đếm được một `Instrument`. Gỡ bản chép mà không
// dựng cổng thì người viết lớp test thứ ba lặp lại đúng chuyện đó, và không gì đỏ.
//
// Điều cổng này KHÔNG canh, nói ra để không ai đọc nó rộng hơn thực tế:
//   • nó canh chỗ DỰNG listener, không canh việc dùng `AsyncLocal` — `AsyncLocal` là công cụ chung của
//     BCL, cấm nó sẽ báo oan;
//   • nó không phân biệt được một lớp test CÓ LÝ DO CHÍNH ĐÁNG để tự dựng listener (đo một `Meter`
//     không phải `CoreMetrics`) với một bản chép lại. Chỗ đó xử bằng allowlist dưới đây: mỗi mục phải
//     khai LÝ DO, tức người thêm phải viết ra vì sao, chứ không lặng lẽ nới cổng.
public class MeterListenerOwnershipTests
{
    // Allowlist khai MỘT chỗ, mỗi mục kèm lý do. Đường dẫn tương đối so với src/BE/Tests, dùng dấu `/`.
    private static readonly (string RelativePath, string Reason)[] Allowlist =
    [
        ("Shared/MeterProbe.cs",
         "Dụng cụ đo dùng chung — bản DUY NHẤT của kỹ thuật lọc phép đo theo luồng logic. ADR-0070: tệp "
       + "nguồn này nối vào từng project test bằng <Compile Include>, không nhân bản sang project nào."),
    ];

    // ===== Luật thật =====

    [Fact]
    public void TestCodeConstructingAMeterListener_MustLiveInTheSharedProbeFile()
    {
        var offenders = AllConstructions()
            .Where(c => !IsAllowed(c.FilePath))
            .Select(c => $"{c.FilePath}: {c.TypeName} tự dựng {MeterListenerOwnershipScanner.ListenerTypeName} của riêng nó — "
                       + "kỹ thuật cô lập phép đo chỉ có một bản, ở Tests/Shared/MeterProbe.cs (T11, ADR-0070). "
                       + "Có lý do chính đáng thì thêm một mục kèm LÝ DO vào Allowlist của MeterListenerOwnershipTests.")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // ===== Chốt chống xanh rỗng (T6) =====

    // Không có chốt này thì cổng trên PASS ngay cả khi phép dò đọc được 0 chỗ — và đó không phải giả
    // thiết: một bộ dò chỉ bắt chuỗi `new MeterListener()` sẽ đọc được đúng 0 chỗ trong repo hôm nay,
    // vì bản thật viết là `private readonly MeterListener _listener = new();`.
    [Fact]
    public void TheMeterListenerGate_ScansAtLeastOneRealConstruction()
        => AllConstructions().ShouldNotBeEmpty(
            $"không đọc được chỗ nào dựng {MeterListenerOwnershipScanner.ListenerTypeName} dưới src/BE/Tests — "
          + "bộ dò hỏng hoặc tệp dùng chung đã dời đi, và cổng T11 đang xanh rỗng.");

    // Allowlist mục ruỗng cũng là một cách cổng nới ra trong im lặng: mục trỏ vào tệp đã dời đi thì nó
    // không miễn trừ cho ai nữa, nhưng cũng không ai biết — và người đọc vẫn tưởng chỗ đó được phép.
    [Fact]
    public void EveryAllowlistEntry_PointsAtAFileThatReallyConstructsAMeterListener()
    {
        var constructingFiles = AllConstructions()
            .Select(c => TestSourceFiles.RelativePath(c.FilePath))
            .ToHashSet(StringComparer.Ordinal);

        var stale = Allowlist
            .Where(entry => !constructingFiles.Contains(entry.RelativePath))
            .Select(entry => $"{entry.RelativePath} — mục allowlist không còn dựng {MeterListenerOwnershipScanner.ListenerTypeName} (tệp đã dời, đổi tên, hoặc mã đã đổi)")
            .ToList();

        stale.ShouldBeEmpty();
    }

    [Fact]
    public void EveryAllowlistEntry_StatesAReason()
        => Allowlist
            .Where(entry => string.IsNullOrWhiteSpace(entry.Reason))
            .Select(entry => entry.RelativePath)
            .ShouldBeEmpty();

    [Fact]
    public void TheAllowlist_IsNotEmpty()
        => Allowlist.ShouldNotBeEmpty("allowlist rỗng nghĩa là tệp dùng chung cũng bị cấm — cổng đang cấm sai chỗ.");

    // ===== Detector_* (T1) =====

    [Fact]
    public void Detector_T11_Catches_AnExplicitNew()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                public void Measure()
                {
                    using var listener = new MeterListener();
                }
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldHaveSingleItem()
            .TypeName.ShouldBe("FakeMetricTests");
    }

    // Hình dạng mà chính `MeterProbe` đang dùng, và là hình dạng một bộ dò quét chuỗi `new MeterListener()`
    // bỏ sót sạch. Đây là ca quan trọng nhất của cả nhóm này.
    [Fact]
    public void Detector_T11_Catches_AnImplicitNew_OnAFieldDeclaredAsMeterListener()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                private readonly MeterListener _listener = new();
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldHaveSingleItem()
            .TypeName.ShouldBe("FakeMetricTests");
    }

    [Fact]
    public void Detector_T11_Catches_AnImplicitNew_OnALocalDeclaredAsMeterListener()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                public void Measure()
                {
                    MeterListener listener = new();
                }
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_T11_Catches_AnImplicitNew_ReturnedFromAMethodDeclaredAsMeterListener()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                private static MeterListener Build()
                {
                    return new();
                }
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_T11_Catches_AFullyQualifiedNew()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                public void Measure()
                {
                    using var listener = new System.Diagnostics.Metrics.MeterListener();
                }
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldNotBeEmpty();
    }

    // Chỗ dễ lọt nhất của một phép dò theo AST: lời gọi nằm sâu trong lambda hoặc hàm cục bộ.
    [Fact]
    public void Detector_T11_Catches_AConstructionNestedInsideALambda()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                public void Measure() => Run(() => new MeterListener());
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldNotBeEmpty();
    }

    // Kiểu lồng bên trong lớp test: quy về lớp NGOÀI CÙNG, vì đó là nơi người đi sửa sẽ tìm.
    [Fact]
    public void Detector_T11_AttributesANestedTypeToItsOuterTestClass()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                private sealed class Probe
                {
                    private readonly MeterListener _listener = new();
                }
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldHaveSingleItem()
            .TypeName.ShouldBe("FakeMetricTests");
    }

    // Chú thích nhắc lời gọi KHÔNG phải lời gọi — chỗ một phép dò quét văn bản báo oan.
    [Fact]
    public void Detector_T11_Ignores_AMentionInAComment()
    {
        const string source = """
            namespace Fake;

            // Lớp này KHÔNG dựng new MeterListener(), nó dùng MeterProbe.
            public class FakeMetricTests
            {
                /* var listener = new MeterListener(); */
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldBeEmpty();
    }

    // Mã vi phạm nằm trong CHUỖI KÝ TỰ — đúng hình dạng của chính các ca Detector_* trong tệp này. Quét
    // văn bản sẽ báo oan tệp cổng, và người ta sẽ gỡ cổng đi thay vì gỡ báo oan.
    [Fact]
    public void Detector_T11_Ignores_AConstructionInsideAStringLiteral()
    {
        const string source = """"
            namespace Fake;

            public class FakeMetricTests
            {
                private const string Sample = "using var listener = new MeterListener();";

                private const string Block = """
                    private readonly MeterListener _listener = new();
                    """;
            }
            """";

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldBeEmpty();
    }

    [Fact]
    public void Detector_T11_Ignores_AnotherTypeBeingConstructed()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                private readonly MeterProbe _probe = new(CoreMetrics.OutboxDispatched);

                public void Measure()
                {
                    using var meter = new Meter("x");
                }
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldBeEmpty();
    }

    [Fact]
    public void Detector_T11_Ignores_AnImplicitNew_OnAFieldOfAnotherType()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                private readonly List<string> _names = new();
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldBeEmpty();
    }

    // Ca hồi quy cho ADR-0070 điểm 4: đúng hình dạng đã bị gỡ khỏi
    // `CoreAndSkill.Core.IntegrationTests/Jobs/B4BackgroundFailureVisibilityTests.cs`. Nếu ai đó viết
    // lại nó, cổng phải đỏ.
    [Fact]
    public void Detector_T11_Catches_TheExactShapeRemovedFromTheIntegrationTests()
    {
        const string source = """
            namespace Fake;

            public sealed class B4BackgroundFailureVisibilityTests
            {
                private static readonly AsyncLocal<Guid> OwningFlow = new();

                private static long CountFailedJobMetricDuring(Func<Task> action)
                {
                    var token = Guid.NewGuid();
                    OwningFlow.Value = token;

                    long total = 0;
                    using var listener = new MeterListener();
                    listener.Start();

                    action().GetAwaiter().GetResult();
                    return Interlocked.Read(ref total);
                }
            }
            """;

        MeterListenerOwnershipScanner.Constructions(source, "fake.cs").ShouldHaveSingleItem()
            .TypeName.ShouldBe("B4BackgroundFailureVisibilityTests");
    }

    // ===== Hạ tầng =====

    private static IReadOnlyList<MeterListenerConstruction> AllConstructions()
        => TestSourceFiles.All()
            .SelectMany(file => MeterListenerOwnershipScanner.Constructions(File.ReadAllText(file), file))
            .ToList();

    private static bool IsAllowed(string absolutePath)
    {
        var relative = TestSourceFiles.RelativePath(absolutePath);

        return Array.Exists(Allowlist, entry => string.Equals(entry.RelativePath, relative, StringComparison.Ordinal));
    }
}
