using ArchUnitNET.Loader;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// A4 — docs/RULES.md A4. Hôm nay tệp này CHỈ có hai meta-test của detector, chạy trên assembly biên
// dịch giả lập — T1 (docs/wiki-core/be/04-testing-strategy.md §3). ArchTest A4 thật chưa có.
//
// AI DỰNG A4 THẬT: Core, trong chính project ArchTests này — KHÔNG phải dự án hạ nguồn
// (docs/adr/0102-module-o-src-be-modules-test-canh-module.md, quyết định 4, 5 và bảng thi công).
// ArchTests ở lại vùng Core và quét cả src/BE/Modules/; module không mang ArchTests riêng thay thế.
// Tập module lấy theo THƯ MỤC, nạp qua host, không danh sách module viết tay. Ở repo Core thư mục đó
// rỗng là trạng thái hợp lệ và vĩnh viễn, nên chốt T6 của A4 đi qua một MODULE GIẢ làm canary, không
// đi qua tập module thật. Ba vế của A4: docs/adr/0101-hop-dong-giua-module-o-contracts-cua-module-phat.md.
//
// Khuôn detector: BoundaryRules.FindTypesInNamespaceDependingOnNamespace — CÙNG hàm dùng cho rule A3
// thật (BoundaryRuleTests.cs), KHÔNG dùng "Should().NotDependOnAny(...)" của ArchUnitNET Fluent.
// Đã đo bằng dotnet test: WithoutRequiringPositiveResults() trên combinator đó khiến Evaluate()
// luôn trả tập rỗng — rule dựng theo cách đó KHÔNG BAO GIỜ bắt được gì, bất kể có vi phạm thật hay
// không; bỏ flag đó thì ngược lại, luôn ném bất kể có vi phạm hay không. Khuôn hai-biến-thể đó đã
// bị xác nhận là sai (F1/F2, core-reviewer) — A4 thật dựng tiếp từ khuôn NÀY, không từ
// "Should().NotDependOnAny(...).WithoutRequiringPositiveResults()".
public class ModuleIsolationTests
{
    [Fact]
    public void Detector_A4_Catches_RealViolation()
    {
        var moduleB = SyntheticAssembly.Compile(
            "CoreAndSkill.Modules.B.Fake",
            "namespace CoreAndSkill.Modules.B.Fake { public class Marker { } }");

        var moduleA = SyntheticAssembly.Compile(
            "CoreAndSkill.Modules.A.Fake",
            "namespace CoreAndSkill.Modules.A.Fake { public class Consumer { public CoreAndSkill.Modules.B.Fake.Marker? M; } }",
            moduleB);

        var architecture = new ArchLoader().LoadAssemblies(moduleA, moduleB).Build();

        var offenders = BoundaryRules.FindTypesInNamespaceDependingOnNamespace(
            architecture,
            "CoreAndSkill.Modules.A.",
            "CoreAndSkill.Modules.B.");

        offenders.ShouldContain("CoreAndSkill.Modules.A.Fake.Consumer");
    }

    [Fact]
    public void Detector_A4_Ignores_ModuleNotReferencingOtherModule()
    {
        var moduleB = SyntheticAssembly.Compile(
            "CoreAndSkill.Modules.B.Fake2",
            "namespace CoreAndSkill.Modules.B.Fake2 { public class Marker { } }");

        var moduleA = SyntheticAssembly.Compile(
            "CoreAndSkill.Modules.A.Fake2",
            "namespace CoreAndSkill.Modules.A.Fake2 { public class Consumer { } }");

        var architecture = new ArchLoader().LoadAssemblies(moduleA, moduleB).Build();

        var offenders = BoundaryRules.FindTypesInNamespaceDependingOnNamespace(
            architecture,
            "CoreAndSkill.Modules.A.",
            "CoreAndSkill.Modules.B.");

        offenders.ShouldBeEmpty();
    }
}
