using ArchUnitNET.Loader;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Ba luật ranh giới — docs/kien-truc-core-module.md §3, docs/RULES.md §3 (A2-A4).
public class BoundaryRuleTests
{
    // A2 — docs/RULES.md A2. B0 kiểm được đúng phần "Application không tham chiếu
    // Infrastructure/Web" (cả hai assembly có type thật, nạp được vào Architecture). Phần
    // "không tham chiếu EF Core/ASP.NET Core" chỉ có nghĩa (non-vacuous) khi Infrastructure
    // thật sự mang gói đó — tới ở B1 cùng CoreDbContext; thêm detector đó ở B1, không viết một
    // rule luôn đúng vì chưa có gì để so (T6, docs/RULES.md §8).
    // Dựng bằng BoundaryRules.FindApplicationTypesDependingOnInfrastructureOrWeb — quét đồ thị
    // dependency của ArchUnitNET trực tiếp, KHÔNG dùng "Should().NotDependOnAny(...)" của
    // ArchUnitNET Fluent (xem chú thích trong BoundaryRules.cs: WithoutRequiringPositiveResults()
    // trên combinator đó khiến Evaluate() luôn rỗng — rule không bao giờ bắt được gì, bất kể có
    // vi phạm thật hay không; đã đo bằng dotnet test, không suy diễn). Rule thật và bốn
    // Detector_A2_* dưới đây gọi ĐÚNG một hàm này — cùng khuôn S4
    // (AnonymousEndpointScanner.FindAnonymousEndpoints): xoá một nhánh so khớp khỏi hàm dùng
    // chung là xoá luôn khỏi rule thật, và ít nhất một meta-test phải đỏ theo (đã tự kiểm bằng
    // cách xoá tạm nhánh Web rồi chạy lại dotnet test, sau đó hoàn nguyên).
    [Fact]
    public void Core_Application_MustNotDependOn_InfrastructureOrWeb()
    {
        var offenders = BoundaryRules.FindApplicationTypesDependingOnInfrastructureOrWeb(
            ArchitectureFixture.Architecture,
            ArchitectureFixture.ApplicationAssembly.GetName().Name!,
            ArchitectureFixture.InfrastructureAssembly.GetName().Name!,
            ArchitectureFixture.WebAssembly.GetName().Name!);

        offenders.ShouldBeEmpty();
    }

    // T6 (docs/wiki-core/be/04-testing-strategy.md §3.4): detector duyệt một tập rỗng luôn
    // PASS. Không có test này, xoá toàn bộ nội dung FindApplicationTypesDependingOnInfrastructureOrWeb
    // (trả về [] vô điều kiện) vẫn để test phía trên xanh mãi mãi.
    [Fact]
    public void Core_Application_MustNotDependOn_InfrastructureOrWeb_ScansAtLeastOneRealType()
    {
        var applicationTypeCount = ArchitectureFixture.Architecture.Classes
            .Count(type => type.Assembly.Name == ArchitectureFixture.ApplicationAssembly.GetName().Name!);

        applicationTypeCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Detector_A2_Catches_RealViolation()
    {
        var infrastructureAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Infrastructure.FakeCatches",
            "namespace CoreAndSkill.Core.Infrastructure.FakeCatches { public class Marker { } }");

        var webAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Web.FakeCatchesUnrelated",
            "namespace CoreAndSkill.Core.Web.FakeCatchesUnrelated { public class Marker { } }");

        var applicationAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Application.FakeConsumerCatches",
            "namespace CoreAndSkill.Core.Application.FakeConsumerCatches { public class Consumer { public CoreAndSkill.Core.Infrastructure.FakeCatches.Marker? M; } }",
            infrastructureAssembly);

        var architecture = new ArchLoader()
            .LoadAssemblies(applicationAssembly, infrastructureAssembly, webAssembly)
            .Build();

        var offenders = BoundaryRules.FindApplicationTypesDependingOnInfrastructureOrWeb(
            architecture,
            applicationAssembly.GetName().Name!,
            infrastructureAssembly.GetName().Name!,
            webAssembly.GetName().Name!);

        offenders.ShouldContain("CoreAndSkill.Core.Application.FakeConsumerCatches.Consumer");
    }

    [Fact]
    public void Detector_A2_Ignores_ApplicationNotReferencingInfrastructureOrWeb()
    {
        var infrastructureAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Infrastructure.FakeIgnores",
            "namespace CoreAndSkill.Core.Infrastructure.FakeIgnores { public class Marker { } }");

        var webAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Web.FakeIgnoresUnrelated",
            "namespace CoreAndSkill.Core.Web.FakeIgnoresUnrelated { public class Marker { } }");

        var applicationAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Application.FakeConsumerIgnores",
            "namespace CoreAndSkill.Core.Application.FakeConsumerIgnores { public class Consumer { } }");

        var architecture = new ArchLoader()
            .LoadAssemblies(applicationAssembly, infrastructureAssembly, webAssembly)
            .Build();

        var offenders = BoundaryRules.FindApplicationTypesDependingOnInfrastructureOrWeb(
            architecture,
            applicationAssembly.GetName().Name!,
            infrastructureAssembly.GetName().Name!,
            webAssembly.GetName().Name!);

        offenders.ShouldBeEmpty();
    }

    // Nhánh Web của A2 — rule thật khớp CẢ Infrastructure LẪN Web. Hai meta-test Infrastructure
    // phía trên chỉ chứng minh nhánh Infrastructure; hai test dưới đây chứng minh vi phạm qua
    // NHÁNH WEB cũng bị hàm dùng chung bắt — không chỉ qua nhánh Infrastructure. Thiếu cặp này
    // thì ai đó xoá nhánh so khớp Web khỏi BoundaryRules vẫn không có test nào đỏ.
    [Fact]
    public void Detector_A2_Catches_RealViolation_ViaWeb()
    {
        var infrastructureAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Infrastructure.FakeWebCatchesInfra",
            "namespace CoreAndSkill.Core.Infrastructure.FakeWebCatchesInfra { public class Marker { } }");

        var webAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Web.FakeCatches",
            "namespace CoreAndSkill.Core.Web.FakeCatches { public class Marker { } }");

        var applicationAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Application.FakeConsumerWebCatches",
            "namespace CoreAndSkill.Core.Application.FakeConsumerWebCatches { public class Consumer { public CoreAndSkill.Core.Web.FakeCatches.Marker? M; } }",
            webAssembly);

        var architecture = new ArchLoader()
            .LoadAssemblies(applicationAssembly, infrastructureAssembly, webAssembly)
            .Build();

        // Vi phạm ở đây chỉ xảy ra qua nhánh Web (Consumer đụng Web.Marker, không đụng gì của
        // Infrastructure) — đúng hàm dùng chung với rule thật, không dựng bản riêng.
        var offenders = BoundaryRules.FindApplicationTypesDependingOnInfrastructureOrWeb(
            architecture,
            applicationAssembly.GetName().Name!,
            infrastructureAssembly.GetName().Name!,
            webAssembly.GetName().Name!);

        offenders.ShouldContain("CoreAndSkill.Core.Application.FakeConsumerWebCatches.Consumer");
    }

    [Fact]
    public void Detector_A2_Ignores_ApplicationNotReferencingWeb()
    {
        var infrastructureAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Infrastructure.FakeWebIgnoresInfra",
            "namespace CoreAndSkill.Core.Infrastructure.FakeWebIgnoresInfra { public class Marker { } }");

        var webAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Web.FakeIgnores",
            "namespace CoreAndSkill.Core.Web.FakeIgnores { public class Marker { } }");

        var applicationAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.Application.FakeConsumerWebIgnores",
            "namespace CoreAndSkill.Core.Application.FakeConsumerWebIgnores { public class Consumer { } }");

        var architecture = new ArchLoader()
            .LoadAssemblies(applicationAssembly, infrastructureAssembly, webAssembly)
            .Build();

        var offenders = BoundaryRules.FindApplicationTypesDependingOnInfrastructureOrWeb(
            architecture,
            applicationAssembly.GetName().Name!,
            infrastructureAssembly.GetName().Name!,
            webAssembly.GetName().Name!);

        offenders.ShouldBeEmpty();
    }

    // A3 — docs/RULES.md A3. Dựng bằng BoundaryRules.FindTypesInNamespaceDependingOnNamespace —
    // quét đồ thị dependency của ArchUnitNET trực tiếp theo tiền tố namespace trên FullName,
    // KHÔNG dùng "Should().NotDependOnAny(...)" của ArchUnitNET Fluent (xem chú thích trong
    // BoundaryRules.cs: WithoutRequiringPositiveResults() trên combinator đó khiến Evaluate()
    // luôn trả tập rỗng — rule không bao giờ bắt được gì, bất kể có vi phạm thật hay không; đã đo
    // bằng dotnet test, không suy diễn). Chưa có assembly Modules.* nào trong solution ở B0 nên
    // tập offender hôm nay rỗng — đó là trạng thái CÓ THẬT (không có gì để vi phạm), không phải
    // rule bị nới. Rule thật và hai Detector_A3_* dưới đây gọi ĐÚNG một hàm này — cùng khuôn A2/S4:
    // xoá một nhánh so khớp khỏi hàm dùng chung là xoá luôn khỏi rule thật, và ít nhất một
    // meta-test phải đỏ theo.
    [Fact]
    public void Core_MustNotReference_AnyModulesAssembly()
    {
        var offenders = BoundaryRules.FindTypesInNamespaceDependingOnNamespace(
            ArchitectureFixture.Architecture,
            "CoreAndSkill.Core.",
            "CoreAndSkill.Modules.");

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Detector_A3_Catches_RealViolation()
    {
        var moduleAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Modules.FakeCatches",
            "namespace CoreAndSkill.Modules.FakeCatches { public class Marker { } }");

        var coreAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.FakeConsumerCatches",
            "namespace CoreAndSkill.Core.FakeConsumerCatches { public class Consumer { public CoreAndSkill.Modules.FakeCatches.Marker? M; } }",
            moduleAssembly);

        var architecture = new ArchLoader().LoadAssemblies(coreAssembly, moduleAssembly).Build();

        var offenders = BoundaryRules.FindTypesInNamespaceDependingOnNamespace(
            architecture,
            "CoreAndSkill.Core.",
            "CoreAndSkill.Modules.");

        offenders.ShouldContain("CoreAndSkill.Core.FakeConsumerCatches.Consumer");
    }

    [Fact]
    public void Detector_A3_Ignores_CoreNotReferencingModule()
    {
        var moduleAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Modules.FakeIgnores",
            "namespace CoreAndSkill.Modules.FakeIgnores { public class Marker { } }");

        var coreAssembly = SyntheticAssembly.Compile(
            "CoreAndSkill.Core.FakeConsumerIgnores",
            "namespace CoreAndSkill.Core.FakeConsumerIgnores { public class Consumer { } }");

        var architecture = new ArchLoader().LoadAssemblies(coreAssembly, moduleAssembly).Build();

        var offenders = BoundaryRules.FindTypesInNamespaceDependingOnNamespace(
            architecture,
            "CoreAndSkill.Core.",
            "CoreAndSkill.Modules.");

        offenders.ShouldBeEmpty();
    }
}
