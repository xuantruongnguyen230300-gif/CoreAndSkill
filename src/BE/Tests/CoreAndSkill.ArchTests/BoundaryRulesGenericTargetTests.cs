using ArchUnitNET.Loader;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Chốt T6/T1 cho hai bộ dò ranh giới A2 (BoundaryRules): một phụ thuộc mà đích của nó là THAM SỐ KIỂU
// GENERIC không thuộc assembly nào (`Target.Assembly` là null). Trình biên dịch sinh ra loại phụ thuộc này
// mỗi khi mã gọi một hàm `params ReadOnlySpan<T>` với từ hai đối số trở lên (Error.WithParams(("A", 1),
// ("B", 2)) chẳng hạn): nó dựng bộ đệm tạm qua `<PrivateImplementationDetails>::InlineArray*`, các hàm
// generic mà ArchUnitNET không gắn vào assembly nào.
//
// Trước khi bộ dò biết bỏ qua chúng, mọi lần Core.Application gọi WithParams hai đối số là hai ArchTest
// A2 chết bằng NullReferenceException — tức cổng đỏ vì chính cổng, không vì luật bị vi phạm. Đích không có
// assembly thì không thể thuộc assembly bị cấm, nên bỏ qua là đúng; test dưới ghim hành vi đó và ghim
// rằng bỏ qua KHÔNG làm mất khả năng bắt vi phạm thật (hai Detector_* cũ vẫn canh chiều đó).
public class BoundaryRulesGenericTargetTests
{
    private const string ParamsSpanCaller = """
        namespace CoreAndSkill.Core.Application.FakeParamsSpan
        {
            public static class Helper
            {
                public static void Take(params System.ReadOnlySpan<(string Name, object? Value)> args) { }
            }

            public class Consumer
            {
                // Hai đối số trở lên: trình biên dịch dùng InlineArray thay vì mảng.
                public void Call() => Helper.Take(("a", 1), ("b", 2), ("c", 3));
            }
        }
        """;

    private static (ArchUnitNET.Domain.Architecture Architecture, string AssemblyName) Build()
    {
        // Tên assembly DUY NHẤT mỗi lần dựng: các test chạy song song và nạp cùng một danh tính assembly hai lần
        // thì ArchLoader trả đồ thị của lần nạp đầu tiên cho cả hai.
        var assembly = SyntheticAssembly.Compile($"CoreAndSkill.Core.Application.FakeParamsSpan{Guid.NewGuid():N}", ParamsSpanCaller);
        var architecture = new ArchLoader().LoadAssemblies(assembly).Build();
        return (architecture, assembly.GetName().Name!);
    }

    [Fact]
    public void Fixture_ProducesAtLeastOneDependencyWithoutAssembly()
    {
        // T6: nếu trình biên dịch/ArchUnitNET ngừng sinh loại phụ thuộc này thì hai test dưới xanh vì
        // tập đầu vào rỗng chứ không vì bộ dò đúng.
        var (architecture, assemblyName) = Build();

        var withoutAssembly = architecture.Classes
            .Where(type => type.Assembly.Name == assemblyName)
            .SelectMany(type => type.Dependencies)
            .Count(dependency => dependency.Target.Assembly is null);

        withoutAssembly.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Detector_A2_Infrastructure_DoesNotThrow_OnGenericParameterTargets()
    {
        var (architecture, assemblyName) = Build();

        var offenders = BoundaryRules.FindApplicationTypesDependingOnInfrastructureOrWeb(
            architecture, assemblyName, "CoreAndSkill.Core.Infrastructure", "CoreAndSkill.Core.Web");

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Detector_A2_EfCoreOrAspNetCore_DoesNotThrow_OnGenericParameterTargets()
    {
        var (architecture, assemblyName) = Build();

        var offenders = BoundaryRules.FindTypesInAssemblyDependingOnForbiddenAssemblyPrefixes(
            architecture, assemblyName, ["Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore"]);

        offenders.ShouldBeEmpty();
    }
}
