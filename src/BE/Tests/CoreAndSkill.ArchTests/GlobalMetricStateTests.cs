using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Cổng cho một luật CHƯA có số hiệu trong docs/RULES.md §8: mọi lớp test ghi
// `CoreMetrics.SetOutboxSnapshot` phải khai `[Collection(OutboxSnapshotCollection.Name)]`.
//
// Vì sao cần cổng chứ không phải một dòng chú thích: ảnh chụp outbox là biến TĨNH toàn tiến trình và
// ba ObservableGauge đọc thẳng từ nó, còn xUnit chạy các lớp test song song trong cùng tiến trình.
// Hai lớp cùng ghi mà không chung collection thì lớp này đọc ra giá trị lớp kia vừa đặt — test đỏ
// ngẫu nhiên, và thông điệp lỗi nói về một con số chứ không nói về nguyên nhân.
//
// Tự kiểm (T1 + kỷ luật canary của repo): đã gỡ `[Collection]` khỏi `B4SupportTests` rồi cho một lớp
// test khác ghi đè ảnh chụp trong 3 giây — hai test gauge đỏ 3/3 lượt chạy; đưa lớp ghi đè vào cùng
// collection → xanh 3/3 lượt. Không suy diễn.
//
// Điều cổng này KHÔNG canh: các chỉ số kiểu Counter. Chúng không cần collection vì `MeterProbe` đã
// lọc theo luồng của từng test (`src/BE/Tests/Shared/MeterProbe.cs`).
// Chỉ trạng thái tĩnh dùng chung mới cần tuần tự hoá.
public class GlobalMetricStateTests
{
    private const string CollectionTypeName = "OutboxSnapshotCollection";

    // ===== Luật thật =====

    [Fact]
    public void TestClassesWritingTheOutboxSnapshot_MustShareTheSameCollection()
    {
        var collectionName = ReadCollectionName();
        var files = EnumerateTestSourceFiles();

        // Hợp nhất theo TÊN KIỂU, không theo file: lớp `partial` có thể mang `[Collection]` ở một
        // phần và lời gọi ở phần khác, và `[Collection]` không được lặp trên hai phần (CS0579).
        var guarded = files
            .SelectMany(file => GlobalMetricStateScanner.GuardedTypeNames(File.ReadAllText(file), collectionName, CollectionTypeName))
            .ToHashSet(StringComparer.Ordinal);

        var offenders = files
            .SelectMany(file => GlobalMetricStateScanner.TouchPoints(File.ReadAllText(file), file))
            .Where(touch => !guarded.Contains(touch.TypeName))
            .Select(touch => $"{touch.FilePath}: {touch.TypeName} ghi CoreMetrics.{GlobalMetricStateScanner.GlobalStateMethod} "
                           + $"nhưng thiếu [Collection({CollectionTypeName}.Name)]")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 — bộ dò duyệt tập rỗng thì luôn PASS. Hai test dưới chứng minh cả hai tập đầu vào đều chạm
    // mã CÓ THẬT: có lớp test thật ghi ảnh chụp, và có lớp test thật mang thuộc tính collection.
    [Fact]
    public void TheOutboxSnapshotGate_ScansAtLeastOneRealClass()
    {
        EnumerateTestSourceFiles()
            .SelectMany(file => GlobalMetricStateScanner.TouchPoints(File.ReadAllText(file), file))
            .ShouldNotBeEmpty();
    }

    [Fact]
    public void TheOutboxSnapshotGate_FindsAtLeastOneRealClassAlreadyInTheCollection()
    {
        var collectionName = ReadCollectionName();

        EnumerateTestSourceFiles()
            .SelectMany(file => GlobalMetricStateScanner.GuardedTypeNames(File.ReadAllText(file), collectionName, CollectionTypeName))
            .ShouldNotBeEmpty();
    }

    // Tên collection đọc ra từ chính file khai nó, không hằng hoá lại ở đây — đổi tên kiểu hay dời
    // file thì test này đỏ, thay vì cổng lặng lẽ quét bằng một tên không còn tồn tại.
    [Fact]
    public void TheOutboxSnapshotCollectionName_IsReadFromRealSource()
        => ReadCollectionName().ShouldNotBeNullOrWhiteSpace();

    // ===== Detector_* (T1) =====

    [Fact]
    public void Detector_OutboxSnapshotCollection_Catches_AClassWritingTheSnapshot()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                [Fact]
                public void Writes() => CoreMetrics.SetOutboxSnapshot(1, 2, 3);
            }
            """;

        GlobalMetricStateScanner.TouchPoints(source, "fake.cs").ShouldHaveSingleItem()
            .TypeName.ShouldBe("FakeMetricTests");
    }

    // Lời gọi nằm sâu trong một lambda vẫn là lời gọi — chỗ dễ lọt nhất của phép dò theo AST.
    [Fact]
    public void Detector_OutboxSnapshotCollection_Catches_ACallNestedInsideALambda()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                [Fact]
                public void Writes() => Enumerable.Range(0, 2).ToList().ForEach(_ => CoreMetrics.SetOutboxSnapshot(1, 2, 3));
            }
            """;

        GlobalMetricStateScanner.TouchPoints(source, "fake.cs").ShouldNotBeEmpty();
    }

    // Kiểu lồng bên trong lớp test: quy về lớp NGOÀI CÙNG, vì đó là nơi `[Collection]` gắn được.
    [Fact]
    public void Detector_OutboxSnapshotCollection_AttributesANestedTypeToItsOuterTestClass()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                private sealed class Helper
                {
                    public void Writes() => CoreMetrics.SetOutboxSnapshot(1, 2, 3);
                }
            }
            """;

        GlobalMetricStateScanner.TouchPoints(source, "fake.cs").ShouldHaveSingleItem()
            .TypeName.ShouldBe("FakeMetricTests");
    }

    // Chú thích nhắc tên hàm KHÔNG phải lời gọi — đây là chỗ một phép dò quét văn bản sẽ báo oan.
    [Fact]
    public void Detector_OutboxSnapshotCollection_Ignores_AMentionInAComment()
    {
        const string source = """
            namespace Fake;

            // Lớp này KHÔNG gọi CoreMetrics.SetOutboxSnapshot, chỉ nhắc tới nó.
            public class FakeMetricTests
            {
                /* SetOutboxSnapshot(1, 2, 3) */
                [Fact]
                public void DoesNotWrite() { }
            }
            """;

        GlobalMetricStateScanner.TouchPoints(source, "fake.cs").ShouldBeEmpty();
    }

    [Fact]
    public void Detector_OutboxSnapshotCollection_Ignores_AClassThatNeverWritesTheSnapshot()
    {
        const string source = """
            namespace Fake;

            public class FakeMetricTests
            {
                [Fact]
                public void Reads() => CoreMetrics.OutboxDispatched.Add(1);
            }
            """;

        GlobalMetricStateScanner.TouchPoints(source, "fake.cs").ShouldBeEmpty();
    }

    [Fact]
    public void Detector_OutboxSnapshotCollection_Reads_TheAttributeInBothShapes()
    {
        const string viaConstant = """
            namespace Fake;

            [Collection(OutboxSnapshotCollection.Name)]
            public class FakeMetricTests { }
            """;

        const string viaLiteral = """
            namespace Fake;

            [Collection("ten-that")]
            public class FakeMetricTests { }
            """;

        GlobalMetricStateScanner.GuardedTypeNames(viaConstant, "ten-that", CollectionTypeName).ShouldBe(["FakeMetricTests"]);
        GlobalMetricStateScanner.GuardedTypeNames(viaLiteral, "ten-that", CollectionTypeName).ShouldBe(["FakeMetricTests"]);
    }

    [Fact]
    public void Detector_OutboxSnapshotCollection_Ignores_ADifferentCollection()
    {
        const string source = """
            namespace Fake;

            [Collection("Postgres")]
            public class FakeMetricTests { }
            """;

        GlobalMetricStateScanner.GuardedTypeNames(source, "ten-that", CollectionTypeName).ShouldBeEmpty();
    }

    // Nơi KHAI collection không phải một lớp test nằm trong nó — không được tính là "đã được canh".
    [Fact]
    public void Detector_OutboxSnapshotCollection_Ignores_TheCollectionDefinitionType()
    {
        const string source = """
            namespace Fake;

            [CollectionDefinition(Name)]
            public sealed class OutboxSnapshotCollection
            {
                public const string Name = "ten-that";
            }
            """;

        GlobalMetricStateScanner.GuardedTypeNames(source, "ten-that", CollectionTypeName).ShouldBeEmpty();
    }

    // ===== Hạ tầng =====

    // Đọc hằng số tên collection ra từ chính kiểu khai nó. Dùng lại bộ đọc của cổng T9 — nó đọc
    // `const string` đầu tiên của một kiểu theo tên, không dính gì tới PostgreSQL.
    private static string ReadCollectionName()
    {
        var name = EnumerateTestSourceFiles()
            .Select(file => RequiresDockerTraitScanner.TryReadCollectionName(File.ReadAllText(file), CollectionTypeName))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        name.ShouldNotBeNullOrWhiteSpace(
            $"Không tìm thấy hằng số tên collection trong kiểu {CollectionTypeName} dưới src/BE/Tests — "
          + "kiểu đã bị đổi tên hoặc dời đi, và cổng này đang không quét đúng thứ nó tưởng.");

        return name!;
    }

    // Mọi file .cs dưới src/BE/Tests, loại bin/obj — MỘT nguồn duy nhất cho cả T9, T10 và T11
    // (.claude/CLAUDE.md §5). Gốc quét nằm ở Support/TestSourceFiles.cs, nên Tests/Shared/ không bị
    // cổng này bỏ sót trong khi cổng kia thấy.
    private static IReadOnlyList<string> EnumerateTestSourceFiles() => TestSourceFiles.All();
}
