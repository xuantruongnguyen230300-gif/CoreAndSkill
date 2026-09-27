using System.Reflection;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Cổng cho luật E15 (docs/RULES.md §4; định nghĩa gốc docs/quy-uoc/be-entity-domain.md §6.2–§6.3): concurrency token
// của một entity là `uint` mang `.IsRowVersion()` — không `byte[]`, không shadow property.
//
// 🛑 TẬP ĐẦU VÀO HÔM NAY RỖNG. Không entity nào của Core có concurrency token — token của người dùng và vai trò đi
// bằng `ConcurrencyStamp` của Identity (§6.4). Nghĩa là cổng này KHÔNG đi được bằng chốt đếm: một chốt kiểu "có ít
// nhất một IsRowVersion" sẽ đỏ ngay hôm nay và bị gỡ, còn cổng không có chốt nào thì PASS mãi mãi vì không có gì để
// xét. Nó đi bằng CANARY: nhóm Detector_E15_* dựng cấu hình giả cho cả hai hình dạng sai và bắt buộc phép dò phải
// kêu, cộng một chốt T6 cho TẦM QUÉT (tệp IEntityTypeConfiguration<T> thật phải đọc được) — thứ độc lập hoàn toàn
// với việc có bao nhiêu token.
//
// VÌ SAO ĐÁNG CÓ CỔNG DÙ PHƠI NHIỄM BẰNG 0: trên Npgsql, `byte[]` + `.IsRowVersion()` tạo một cột `bytea` mà không
// ai cập nhật ⇒ `WHERE "RowVersion" = @original` luôn khớp ⇒ check đồng thời vô hiệu IM LẶNG, và một test kiểu "hai
// lượt ghi, lượt sau thắng" vẫn xanh. Người đầu tiên thêm token sẽ làm một mình; cổng này là người thứ hai.
//
// Điểm mù của phép dò khai ngay đầu Support/ConcurrencyTokenScanner.cs — đọc ở đó, không chép lại ở đây.
public class ConcurrencyTokenTypeTests
{
    // ===== Luật thật =====

    [Fact]
    public void EveryConcurrencyToken_IsAUintRowVersion()
    {
        var offenders = Evaluate(RealCalls());

        offenders.ShouldBeEmpty();
    }

    // ===== Chốt chống xanh rỗng (T6) — canh TẦM QUÉT, không canh số token =====

    // Không có chốt này thì cổng trên PASS kể cả khi phép liệt kê tệp hỏng hoàn toàn — và với tập token rỗng, "hỏng
    // hoàn toàn" trông giống hệt "không có vi phạm". Chốt bám vào thứ KHÔNG rỗng hôm nay: tệp cấu hình entity.
    [Fact]
    public void TheConcurrencyTokenGate_ScansTheRealEntityConfigurationFiles()
    {
        var configurations = ConcurrencyTokenScanner.ConfigurationFiles(ProductSourceFiles.Core());

        configurations.ShouldNotBeEmpty(
            "không thấy tệp IEntityTypeConfiguration<T> nào trong source sản phẩm — tệp cấu hình đã dời chỗ, hoặc "
          + "phép liệt kê hỏng. Với tập concurrency token đang RỖNG, cổng E15 khi đó xanh vì không nhìn gì cả.");

        configurations.ShouldContain(f => ProductSourceFiles.EndsWith(f, "Configurations", "JobConfiguration.cs"));
        configurations.ShouldContain(f => ProductSourceFiles.EndsWith(f, "Configurations", "OutboxMessageConfiguration.cs"));
    }

    // Phép đọc kiểu qua reflection là nửa thứ hai của cổng, và nó hỏng theo cách riêng: đổi tên assembly, đổi cách
    // nạp, một entity nằm ngoài tầm tìm kiếm — mọi ca đó làm `ResolvePropertyType` trả null, tức cổng đỏ chứ không
    // xanh. Chốt này chứng minh chiều NGƯỢC LẠI còn sống: nó thật sự đọc ra được kiểu của một property có thật.
    [Fact]
    public void TheConcurrencyTokenGate_CanReadTheClrTypeOfARealEntityProperty()
    {
        ResolvePropertyType("Job", "TenantId").ShouldBe(typeof(Guid),
            "phép đọc kiểu qua reflection không tìm ra property của một entity CÓ THẬT — mọi phán quyết về kiểu của "
          + "concurrency token sau đó đều vô nghĩa.");

        ResolvePropertyType("FakeUintTokenEntity", nameof(FakeUintTokenEntity.Version)).ShouldBe(typeof(uint));
        ResolvePropertyType("Job", "KhongCoPropertyNao").ShouldBeNull();
    }

    // ===== Detector_E15_* (T1) — cổng đi bằng canary vì tập đầu vào rỗng =====

    // Hình dạng NGUY HIỂM NHẤT, và là hình dạng người quen SQL Server sẽ viết đầu tiên.
    [Fact]
    public void Detector_E15_Catches_AByteArrayRowVersion()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeByteArrayTokenConfiguration : IEntityTypeConfiguration<FakeByteArrayTokenEntity>
            {
                public void Configure(EntityTypeBuilder<FakeByteArrayTokenEntity> builder)
                {
                    builder.Property(x => x.RowVersion).IsRowVersion();
                }
            }
            """;

        Evaluate(Scan(source)).ShouldHaveSingleItem().ShouldContain("byte[]");
    }

    // Shadow property: không có property CLR nào để kiểm kiểu, nên cổng KHÔNG THỂ biết nó là uint hay byte[]. Bắt vô
    // điều kiện là chiều an toàn duy nhất — và §6.2 đã cấm dạng này bằng chữ.
    [Fact]
    public void Detector_E15_Catches_AShadowRowVersion()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeShadowTokenConfiguration : IEntityTypeConfiguration<FakeUintTokenEntity>
            {
                public void Configure(EntityTypeBuilder<FakeUintTokenEntity> builder)
                {
                    builder.Property<byte[]>("RowVersion").IsRowVersion();
                }
            }
            """;

        Evaluate(Scan(source)).ShouldHaveSingleItem().ShouldContain("shadow");
    }

    // Shadow property khai ĐÚNG kiểu uint vẫn bị bắt: dạng shadow bị cấm theo hình dạng, không theo kiểu. Không có ca
    // này thì "bắt vô điều kiện" chỉ là một câu trong chú thích.
    [Fact]
    public void Detector_E15_Catches_AShadowRowVersion_EvenWhenDeclaredUint()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeShadowUintConfiguration : IEntityTypeConfiguration<FakeUintTokenEntity>
            {
                public void Configure(EntityTypeBuilder<FakeUintTokenEntity> builder)
                {
                    builder.Property<uint>("Version").IsRowVersion();
                }
            }
            """;

        Evaluate(Scan(source)).ShouldNotBeEmpty();
    }

    // Đúng recipe của §6.2 — bản duy nhất được phép.
    [Fact]
    public void Detector_E15_Ignores_AUintRowVersion()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeUintTokenConfiguration : IEntityTypeConfiguration<FakeUintTokenEntity>
            {
                public void Configure(EntityTypeBuilder<FakeUintTokenEntity> builder)
                {
                    builder.Property(x => x.Version).IsRowVersion();
                }
            }
            """;

        Evaluate(Scan(source)).ShouldBeEmpty();
    }

    // `.HasColumnName(...)` xen giữa `Property` và `IsRowVersion` là cách viết bình thường. Phép đi ngược chuỗi mất
    // dấu ở đây sẽ báo "không tìm ra property" cho một cấu hình HỢP LỆ — báo đỏ sai, và báo đỏ sai thì cổng bị gỡ.
    [Fact]
    public void Detector_E15_Ignores_AUintRowVersion_WithAnIntermediateCallInTheChain()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeUintTokenConfiguration : IEntityTypeConfiguration<FakeUintTokenEntity>
            {
                public void Configure(EntityTypeBuilder<FakeUintTokenEntity> builder)
                {
                    builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
                }
            }
            """;

        Evaluate(Scan(source)).ShouldBeEmpty();
    }

    // Property KHÔNG mang `.IsRowVersion()` thì ngoài tầm — cổng canh token, không canh mọi property.
    [Fact]
    public void Detector_E15_Ignores_APropertyWithoutIsRowVersion()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeByteArrayTokenConfiguration : IEntityTypeConfiguration<FakeByteArrayTokenEntity>
            {
                public void Configure(EntityTypeBuilder<FakeByteArrayTokenEntity> builder)
                {
                    builder.Property(x => x.RowVersion).HasColumnName("row_version");
                }
            }
            """;

        Scan(source).ShouldBeEmpty();
    }

    // Chuỗi gọi mà phép đi ngược KHÔNG tới được `Property` phải làm cổng ĐỎ, không đi qua yên lặng: một lời gọi
    // `.IsRowVersion()` mà cổng không biết bám vào đâu là đúng ca cổng tồn tại để bắt.
    [Fact]
    public void Detector_E15_Catches_ARowVersionItCannotTraceBackToAProperty()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeIndirectConfiguration : IEntityTypeConfiguration<FakeUintTokenEntity>
            {
                public void Configure(EntityTypeBuilder<FakeUintTokenEntity> builder)
                {
                    var property = builder.Property(x => x.Version);
                    property.IsRowVersion();
                }
            }
            """;

        Evaluate(Scan(source)).ShouldNotBeEmpty();
    }

    // Chú thích nhắc lời gọi KHÔNG phải lời gọi — phép dò đi bằng AST, không bằng grep.
    [Fact]
    public void Detector_E15_Ignores_AMentionInAComment()
    {
        const string source = """
            namespace Fake;

            internal sealed class FakeCommentedConfiguration : IEntityTypeConfiguration<FakeUintTokenEntity>
            {
                public void Configure(EntityTypeBuilder<FakeUintTokenEntity> builder)
                {
                    // Đừng viết builder.Property(x => x.RowVersion).IsRowVersion() với byte[] — xem E15.
                    /* builder.Property<byte[]>("RowVersion").IsRowVersion(); */
                }
            }
            """;

        Scan(source).ShouldBeEmpty();
    }

    // ---- kiểu đối chứng, chỉ sống trong assembly test -------------------------------------------

    public sealed class FakeUintTokenEntity
    {
        public uint Version { get; init; }
    }

    public sealed class FakeByteArrayTokenEntity
    {
        public byte[] RowVersion { get; init; } = [];
    }

    // ---- bộ máy dùng chung của tệp ---------------------------------------------------------------

    private static IReadOnlyList<ConcurrencyTokenScanner.RowVersionCall> RealCalls()
        => [.. ProductSourceFiles.Core()
            .SelectMany(file => ConcurrencyTokenScanner.Calls(File.ReadAllText(file), file))];

    private static IReadOnlyList<ConcurrencyTokenScanner.RowVersionCall> Scan(string source)
        => ConcurrencyTokenScanner.Calls(source, "fake.cs");

    private static IReadOnlyList<string> Evaluate(IReadOnlyList<ConcurrencyTokenScanner.RowVersionCall> calls)
    {
        var offenders = new List<string>();

        foreach (var call in calls)
        {
            var where = $"{call.FilePath}:{call.Line}";

            if (call.IsShadow)
            {
                offenders.Add($"{where} — concurrency token khai dạng shadow property (\"{call.PropertyName}\"). Dạng "
                            + "này bị cấm tuyệt đối: không có property CLR nào để ai đọc ra kiểu, nên không ai — kể cả "
                            + "cổng này — biết nó là uint hay byte[]. Khai một property `uint` trên entity "
                            + "(be-entity-domain.md §6.2).");
                continue;
            }

            if (call.EntityType is null)
            {
                offenders.Add($"{where} — có .IsRowVersion() nhưng không đọc được IEntityTypeConfiguration<T> bao "
                            + "quanh, nên không biết token này thuộc entity nào.");
                continue;
            }

            if (call.PropertyName is null)
            {
                offenders.Add($"{where} — .IsRowVersion() không truy được về một builder.Property(x => x.Y) nào. Viết "
                            + "liền chuỗi `builder.Property(x => x.Y)…IsRowVersion()`; cổng không đọc được dạng gán "
                            + "qua biến trung gian, và một token cổng không đọc được là một token không ai canh.");
                continue;
            }

            var clrType = ResolvePropertyType(call.EntityType, call.PropertyName);

            if (clrType is null)
            {
                offenders.Add($"{where} — không tìm ra property '{call.PropertyName}' trên '{call.EntityType}' để đọc "
                            + "kiểu CLR. Entity đổi tên, property đổi tên, hoặc kiểu nằm ngoài tầm tìm kiếm.");
                continue;
            }

            if (clrType != typeof(uint))
            {
                offenders.Add($"{where} — '{call.EntityType}.{call.PropertyName}' khai kiểu "
                            + $"`{FriendlyName(clrType)}` mang .IsRowVersion(). Trên Npgsql chỉ `uint` bind vào cột hệ "
                            + "thống `xmin`; mọi kiểu khác — đặc biệt `byte[]` — tạo một cột bình thường KHÔNG AI CẬP "
                            + "NHẬT, nên `WHERE … = @original` luôn khớp và check đồng thời vô hiệu IM LẶNG. Luật E15, "
                            + "recipe ở docs/quy-uoc/be-entity-domain.md §6.2.");
            }
        }

        return offenders;
    }

    // Kiểu CLR của một property, tìm theo TÊN ĐƠN của entity trong mọi assembly Core cộng chính assembly test — nhóm
    // Detector_* dùng kiểu đối chứng khai ngay trong tệp này, nên chúng đi qua đúng bộ giải kiểu của cổng thật.
    private static Type? ResolvePropertyType(string entityTypeName, string propertyName)
    {
        var simpleName = entityTypeName.Split('.')[^1].Split('<')[0].Trim();

        Assembly[] assemblies =
        [
            ArchitectureFixture.DomainAssembly,
            ArchitectureFixture.ApplicationAssembly,
            ArchitectureFixture.InfrastructureAssembly,
            ArchitectureFixture.WebAssembly,
            ArchitectureFixture.ContractsAssembly,
            ArchitectureFixture.HostAssembly,
            typeof(ConcurrencyTokenTypeTests).Assembly,
        ];

        return assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name == simpleName)
            .Select(t => t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .FirstOrDefault(p => p is not null)
            ?.PropertyType;
    }

    private static string FriendlyName(Type type) => type == typeof(byte[]) ? "byte[]" : type.Name;
}
