using ArchUnitNET.Loader;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// B1 — docs/wiki-core/be/trien-khai/02-b1-du-lieu-don-vi-danh-tinh.md §4 "Nghiệm thu B1": ArchTest
// phải bắt được Core.Application lọt tham chiếu EF Core / ASP.NET Core. Trước B1 (docs/RULES.md A2)
// nhánh này không có ý nghĩa vì Infrastructure chưa mang gói đó; nay CoreDbContext tồn tại nên rule
// dựng được trên assembly thật.
public class EfCoreLeakTests
{
    private static readonly IReadOnlyList<string> ForbiddenPrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Microsoft.AspNetCore",
    ];

    [Fact]
    public void Core_Application_MustNotReference_EfCoreOrAspNetCore()
    {
        var offenders = BoundaryRules.FindTypesInAssemblyDependingOnForbiddenAssemblyPrefixes(
            ArchitectureFixture.Architecture,
            ArchitectureFixture.ApplicationAssembly.GetName().Name!,
            ForbiddenPrefixes);

        offenders.ShouldBeEmpty();
    }

    // T6 — tập hợp phải khác rỗng để rule phía trên không xanh vì không quét gì.
    [Fact]
    public void Core_Application_MustNotReference_EfCoreOrAspNetCore_ScansAtLeastOneRealType()
    {
        var applicationTypeCount = ArchitectureFixture.Architecture.Classes
            .Count(type => type.Assembly.Name == ArchitectureFixture.ApplicationAssembly.GetName().Name!);

        applicationTypeCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Detector_Catches_RealViolation()
    {
        var efCoreLikeAssembly = SyntheticAssembly.Compile(
            "Microsoft.EntityFrameworkCore.FakeCatches",
            "namespace Microsoft.EntityFrameworkCore.FakeCatches { public class DbContext { } }");

        var applicationAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Application.FakeEfConsumer",
            "namespace CoreAndSkill.Core.Application.FakeEfConsumer { public class Consumer { public Microsoft.EntityFrameworkCore.FakeCatches.DbContext? Db; } }",
            efCoreLikeAssembly);

        var architecture = new ArchLoader().LoadAssemblies(applicationAssembly, efCoreLikeAssembly).Build();

        var offenders = BoundaryRules.FindTypesInAssemblyDependingOnForbiddenAssemblyPrefixes(
            architecture,
            applicationAssembly.GetName().Name!,
            ForbiddenPrefixes);

        offenders.ShouldContain("CoreAndSkill.Core.Application.FakeEfConsumer.Consumer");
    }

    [Fact]
    public void Detector_Ignores_ApplicationNotReferencingEfCore()
    {
        var unrelatedAssembly = SyntheticAssembly.Compile(
            "SomeOther.Library.FakeIgnores",
            "namespace SomeOther.Library.FakeIgnores { public class Marker { } }");

        var applicationAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Application.FakeEfConsumerIgnores",
            "namespace CoreAndSkill.Core.Application.FakeEfConsumerIgnores { public class Consumer { public SomeOther.Library.FakeIgnores.Marker? M; } }",
            unrelatedAssembly);

        var architecture = new ArchLoader().LoadAssemblies(applicationAssembly, unrelatedAssembly).Build();

        var offenders = BoundaryRules.FindTypesInAssemblyDependingOnForbiddenAssemblyPrefixes(
            architecture,
            applicationAssembly.GetName().Name!,
            ForbiddenPrefixes);

        offenders.ShouldBeEmpty();
    }
}
