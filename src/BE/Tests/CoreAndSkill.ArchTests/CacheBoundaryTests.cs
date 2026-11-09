using ArchUnitNET.Loader;
using CoreAndSkill.ArchTests.Support;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Luật A22 — docs/RULES-BE.md hàng A22, docs/quy-uoc/be-performance.md §8.2: Core.Application không chạm thẳng
// HybridCache / IMemoryCache / IDistributedCache; cache chỉ đi qua ICacheStore. Hôm nay ICacheStore chưa tồn tại và
// không có implementation nào (be-performance.md §8.1) — cổng canh vế "không chạm thẳng", đúng cái luật áp ngay.
// Bộ dò và điểm mù: Support/CacheDependencyScanner.cs.
//
// T12:cap-xanh-do — mọi ca Detector_A22_* kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng phép dò, khác đúng một chỗ
// (docs/RULES.md §8 luật T12); chú thích "Cặp đỏ:" nói cặp ở đâu.
public class CacheBoundaryTests
{
    private const string FixHint =
        "Luật A22 (docs/RULES-BE.md; docs/quy-uoc/be-performance.md §8.2): Core.Application chỉ cache qua ICacheStore, "
      + "không chạm thẳng HybridCache / IMemoryCache / IDistributedCache.";

    [Fact]
    public void CoreApplication_MustNotDependOn_CacheLibraries()
    {
        var offenders = CacheDependencyScanner.FindTypesInAssemblyDependingOnCacheLibraries(
            ArchitectureFixture.Architecture,
            ArchitectureFixture.ApplicationAssembly.GetName().Name!);

        offenders.ShouldBeEmpty(FixHint);
    }

    // T6 — tập quét phải khác rỗng để rule trên không xanh vì không quét gì.
    [Fact]
    public void CoreApplication_MustNotDependOn_CacheLibraries_ScansAtLeastOneRealType()
    {
        var applicationName = ArchitectureFixture.ApplicationAssembly.GetName().Name!;

        ArchitectureFixture.Architecture.Types
            .Count(type => type.Assembly.Name == applicationName)
            .ShouldBeGreaterThan(0);
    }

    // ===== Detector_A22_* (T1) — mỗi ca đỏ có ca xanh sinh đôi cùng hình dạng, khác đúng một phụ thuộc (T12) =====

    [Fact]
    public void Detector_A22_Catches_DirectCacheDependency_IMemoryCache()
    {
        var offenders = Scan(
            """
            namespace CoreAndSkill.Core.Application.FakeA22 { public class Consumer { public Microsoft.Extensions.Caching.Memory.IMemoryCache? Cache; } }
            """,
            typeof(IMemoryCache).Assembly);

        offenders.ShouldContain("CoreAndSkill.Core.Application.FakeA22.Consumer");
    }

    [Fact]
    public void Detector_A22_Catches_DirectCacheDependency_IDistributedCache()
    {
        var offenders = Scan(
            """
            namespace CoreAndSkill.Core.Application.FakeA22 { public class Consumer { public Microsoft.Extensions.Caching.Distributed.IDistributedCache? Cache; } }
            """,
            typeof(IDistributedCache).Assembly);

        offenders.ShouldContain("CoreAndSkill.Core.Application.FakeA22.Consumer");
    }

    // HybridCache ở gói riêng (không nằm trong shared framework) — dựng một assembly giả mang đúng tên đầy đủ.
    [Fact]
    public void Detector_A22_Catches_DirectCacheDependency_HybridCache()
    {
        var hybrid = SyntheticAssembly.Compile(
            "FakeHybridCacheLib",
            "namespace Microsoft.Extensions.Caching.Hybrid { public abstract class HybridCache { } }");

        var offenders = Scan(
            """
            namespace CoreAndSkill.Core.Application.FakeA22 { public class Consumer { public Microsoft.Extensions.Caching.Hybrid.HybridCache? Cache; } }
            """,
            hybrid);

        offenders.ShouldContain("CoreAndSkill.Core.Application.FakeA22.Consumer");
    }

    // Kiểu KHÔNG phải class: một interface có tham số IMemoryCache cũng là chạm thẳng.
    [Fact]
    public void Detector_A22_Catches_DirectCacheDependency_OnInterfaceMethodParameter()
    {
        var offenders = Scan(
            """
            namespace CoreAndSkill.Core.Application.FakeA22 { public interface IConsumer { void Use(Microsoft.Extensions.Caching.Memory.IMemoryCache cache); } }
            """,
            typeof(IMemoryCache).Assembly);

        offenders.ShouldContain("CoreAndSkill.Core.Application.FakeA22.IConsumer");
    }

    // Ca xanh sinh đôi: cùng hình dạng (một class giữ một trường kiểu cache), khác đúng đích — một ICacheStore nội bộ.
    [Fact]
    public void Detector_A22_Ignores_DependencyOnOwnCacheStoreInterface()
    {
        var offenders = Scan(
            """
            namespace CoreAndSkill.Core.Application.FakeA22
            {
                public interface ICacheStore { }
                public class Consumer { public ICacheStore? Cache; }
            }
            """);

        offenders.ShouldBeEmpty();
    }

    // Ca xanh sinh đôi: assembly CÓ tham chiếu gói cache nhưng kiểu của Application không dùng ba kiểu cấm.
    [Fact]
    public void Detector_A22_Ignores_ApplicationTypeNotTouchingTheThreeCacheTypes()
    {
        var offenders = Scan(
            """
            namespace CoreAndSkill.Core.Application.FakeA22 { public class Consumer { public Microsoft.Extensions.Caching.Memory.MemoryCacheOptions? Options; } }
            """,
            typeof(MemoryCacheOptions).Assembly);

        offenders.ShouldBeEmpty();
    }

    // Cặp xanh: Detector_A22_Ignores_CacheDependencyOutsideTheScannedAssembly — CÙNG cách dựng hai assembly (Application +
    // Infrastructure), khác đúng chỗ đặt trường cache: trong assembly Application thay vì Infrastructure.
    [Fact]
    public void Detector_A22_Catches_CacheDependencyInTheScannedAssembly_WhenTwoAssembliesAreLoaded()
    {
        var application = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Application.FakeA22ScopedRed",
            "namespace CoreAndSkill.Core.Application.FakeA22ScopedRed { public class Dirty { public Microsoft.Extensions.Caching.Memory.IMemoryCache? Cache; } }",
            typeof(IMemoryCache).Assembly);
        var infrastructure = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Infrastructure.FakeA22ScopedRed",
            "namespace CoreAndSkill.Core.Infrastructure.FakeA22ScopedRed { public class Impl { } }");

        var architecture = new ArchLoader().LoadAssemblies(application, infrastructure).Build();

        CacheDependencyScanner.FindTypesInAssemblyDependingOnCacheLibraries(
            architecture,
            application.GetName().Name!).ShouldContain("CoreAndSkill.Core.Application.FakeA22ScopedRed.Dirty");
    }

    // Kiểu cache nằm ở assembly KHÁC Application không bị tính — luật chỉ áp cho Core.Application.
    [Fact]
    public void Detector_A22_Ignores_CacheDependencyOutsideTheScannedAssembly()
    {
        var application = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Application.FakeA22Scoped",
            "namespace CoreAndSkill.Core.Application.FakeA22Scoped { public class Clean { } }");
        var infrastructure = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Infrastructure.FakeA22Scoped",
            "namespace CoreAndSkill.Core.Infrastructure.FakeA22Scoped { public class Impl { public Microsoft.Extensions.Caching.Memory.IMemoryCache? Cache; } }",
            typeof(IMemoryCache).Assembly);

        var architecture = new ArchLoader().LoadAssemblies(application, infrastructure).Build();

        CacheDependencyScanner.FindTypesInAssemblyDependingOnCacheLibraries(
            architecture,
            application.GetName().Name!).ShouldBeEmpty();
    }

    private static IReadOnlyList<string> Scan(string source, params System.Reflection.Assembly[] references)
    {
        var applicationAssembly = SyntheticAssembly.Compile(
            $"CoreAndSkill.Core.Application.FakeA22Assembly{Guid.NewGuid():N}",
            source,
            references);

        var architecture = new ArchLoader().LoadAssemblies([applicationAssembly, .. references]).Build();

        return CacheDependencyScanner.FindTypesInAssemblyDependingOnCacheLibraries(
            architecture,
            applicationAssembly.GetName().Name!);
    }
}
