using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// T9 — docs/RULES.md §8, chi tiết ở docs/wiki-core/be/04-testing-strategy.md §4.2.
//
// Câu ở §4.2 ("mọi test class dùng PostgresFixture mang [Trait(\"Category\", \"RequiresDocker\")]")
// hôm nay đúng chỉ vì có đúng một lớp như vậy. Lớp thứ hai quên [Trait] sẽ KHÔNG bị gì bắt: CI có
// Docker nên CI xanh, và chỗ vỡ là máy dev không Docker — nơi lệnh loại trừ tường minh
// `--filter "Category!=RequiresDocker"` lẽ ra phải loại nó ra. Cổng này là cơ chế bắt lỗi đó.
//
// Tự kiểm (T1 + kỷ luật canary của repo): đã tạm gỡ dòng [Trait("Category", "RequiresDocker")] khỏi
// TenantProvisioningDatabaseTests.cs, chạy `dotnet test` thấy đúng test rule thật
// (TestClassesUsingPostgresFixture_MustCarry_RequiresDockerTrait) chuyển đỏ với thông báo nêu đích
// danh tên lớp và tên Trait còn thiếu, rồi trả dòng đó lại và chạy lại thấy xanh — không suy diễn.
public class RequiresDockerTraitTests
{
    private const string CollectionTypeName = "PostgresCollection";
    private const string FixtureTypeName = "PostgresFixture";

    // ===== Luật thật =====

    [Fact]
    public void TestClassesUsingPostgresFixture_MustCarry_RequiresDockerTrait()
    {
        var markers = ReadMarkers();

        var offenders = EnumerateTestSourceFiles()
            .SelectMany(file => RequiresDockerTraitScanner.Scan(File.ReadAllText(file), file, markers))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 — một detector duyệt tập rỗng thì luôn PASS. Test này chứng minh phép dò trên thật sự chạm
    // ít nhất một lớp test CÓ THẬT đang dùng fixture PostgreSQL.
    [Fact]
    public void TestClassesUsingPostgresFixture_MustCarry_RequiresDockerTrait_ScansAtLeastOneRealClass()
    {
        var markers = ReadMarkers();

        var candidates = EnumerateTestSourceFiles()
            .SelectMany(file => RequiresDockerTraitScanner.ScanCandidates(File.ReadAllText(file), file, markers))
            .ToList();

        candidates.ShouldNotBeEmpty();
    }

    // Chốt đầu vào thứ hai: tên collection KHÔNG được hằng hoá trong ArchTest này mà đọc ra từ chính
    // file khai nó. Đổi tên kiểu hay dời file thì test này đỏ, thay vì cả cổng lặng lẽ quét bằng một
    // tên không còn tồn tại và luôn PASS.
    [Fact]
    public void PostgresCollectionName_IsReadFromRealSource()
    {
        ReadMarkers().CollectionName.ShouldNotBeNullOrWhiteSpace();
    }

    // ===== Detector_* (T1) =====

    [Fact]
    public void Detector_T9_Catches_MissingTrait()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            [Collection(PostgresCollection.Name)]
            public sealed class FakeDatabaseTests(PostgresFixture db)
            {
            }
            """;

        RequiresDockerTraitScanner.Scan(source, "fake.cs", FakeMarkers).ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_T9_Catches_MissingTrait_WhenCollectionNameIsLiteral()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            [Collection("Postgres")]
            public sealed class FakeDatabaseTests
            {
            }
            """;

        RequiresDockerTraitScanner.Scan(source, "fake.cs", FakeMarkers).ShouldNotBeEmpty();
    }

    // Đường thứ hai vào cùng một container: IClassFixture<PostgresFixture>, không qua collection.
    [Fact]
    public void Detector_T9_Catches_MissingTrait_OnClassFixtureRoute()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            public sealed class FakeDatabaseTests : IClassFixture<PostgresFixture>
            {
            }
            """;

        RequiresDockerTraitScanner.Scan(source, "fake.cs", FakeMarkers).ShouldNotBeEmpty();
    }

    // Trait ĐÚNG khoá nhưng SAI giá trị vẫn là vi phạm — lệnh loại trừ khớp theo giá trị.
    [Fact]
    public void Detector_T9_Catches_WrongTraitValue()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            [Trait("Category", "Slow")]
            [Collection(PostgresCollection.Name)]
            public sealed class FakeDatabaseTests
            {
            }
            """;

        RequiresDockerTraitScanner.Scan(source, "fake.cs", FakeMarkers).ShouldNotBeEmpty();
    }

    // Đúng hình dạng thật của TenantProvisioningDatabaseTests — KHÔNG bị bắt.
    [Fact]
    public void Detector_T9_Ignores_ClassWithTrait()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            [Trait("Category", "RequiresDocker")]
            [Collection(PostgresCollection.Name)]
            public sealed class FakeDatabaseTests(PostgresFixture db)
            {
            }
            """;

        RequiresDockerTraitScanner.Scan(source, "fake.cs", FakeMarkers).ShouldBeEmpty();
    }

    // Lớp test trong một collection KHÁC, không chạm PostgreSQL — không cần Docker, không bị bắt.
    [Fact]
    public void Detector_T9_Ignores_UnrelatedCollection()
    {
        const string source = """
            namespace CoreAndSkill.Core.UnitTests.Fake;

            [Collection("SomeOtherCollection")]
            public sealed class FakeUnitTests
            {
            }
            """;

        RequiresDockerTraitScanner.Scan(source, "fake.cs", FakeMarkers).ShouldBeEmpty();
    }

    // Đúng hình dạng thật của PostgresCollection.cs: nơi KHAI collection, không phải lớp test trong
    // đó. Nó có nhắc tên fixture nhưng không dựng container nào — KHÔNG bị bắt.
    [Fact]
    public void Detector_T9_Ignores_CollectionDefinitionType()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            [CollectionDefinition(Name)]
            public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
            {
                public const string Name = "Postgres";
            }
            """;

        RequiresDockerTraitScanner.Scan(source, "fake.cs", FakeMarkers).ShouldBeEmpty();
    }

    // Chính kiểu fixture — KHÔNG bị bắt.
    [Fact]
    public void Detector_T9_Ignores_FixtureTypeItself()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            public sealed class PostgresFixture : IAsyncLifetime
            {
            }
            """;

        RequiresDockerTraitScanner.Scan(source, "fake.cs", FakeMarkers).ShouldBeEmpty();
    }

    [Fact]
    public void Detector_T9_ReadsCollectionName_FromConstant()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            [CollectionDefinition(Name)]
            public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
            {
                public const string Name = "Postgres";
            }
            """;

        RequiresDockerTraitScanner.TryReadCollectionName(source, CollectionTypeName).ShouldBe("Postgres");
    }

    [Fact]
    public void Detector_T9_ReadsCollectionName_ReturnsNull_WhenTypeMissing()
    {
        const string source = """
            namespace CoreAndSkill.Core.IntegrationTests.Fake;

            public sealed class SomethingElse
            {
            }
            """;

        RequiresDockerTraitScanner.TryReadCollectionName(source, CollectionTypeName).ShouldBeNull();
    }

    // ===== Hạ tầng =====

    private static DockerFixtureMarkers FakeMarkers => new("Postgres", CollectionTypeName, FixtureTypeName);

    private static DockerFixtureMarkers ReadMarkers()
    {
        var name = EnumerateTestSourceFiles()
            .Select(file => RequiresDockerTraitScanner.TryReadCollectionName(File.ReadAllText(file), CollectionTypeName))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        name.ShouldNotBeNullOrWhiteSpace(
            $"Không tìm thấy hằng số tên collection trong kiểu {CollectionTypeName} dưới src/BE/Tests — "
          + "kiểu đã bị đổi tên hoặc dời đi, và cổng T9 đang không quét đúng thứ nó tưởng.");

        return new DockerFixtureMarkers(name!, CollectionTypeName, FixtureTypeName);
    }

    // Mọi file .cs dưới src/BE/Tests, loại bin/obj — MỘT nguồn duy nhất cho cả T9, T10 và T11
    // (.claude/CLAUDE.md §5). Gốc quét nằm ở Support/TestSourceFiles.cs, nên Tests/Shared/ không bị
    // cổng này bỏ sót trong khi cổng kia thấy.
    private static IReadOnlyList<string> EnumerateTestSourceFiles() => TestSourceFiles.All();
}
