using ArchUnitNET.Domain;

namespace CoreAndSkill.ArchTests.Support;

// A2 — docs/RULES.md A2. Quét trực tiếp đồ thị dependency của ArchUnitNET (Class.Dependencies),
// KHÔNG dùng "Should().NotDependOnAny(...)" của ArchUnitNET Fluent — đã đo bằng dotnet test:
// WithoutRequiringPositiveResults() trên combinator đó khiến Evaluate() luôn trả về DANH SÁCH
// RỖNG (0 phần tử được chấm) bất kể có vi phạm thật hay không, tức rule dựng theo cách đó
// KHÔNG BAO GIỜ bắt được gì — và bỏ flag thì ngược lại, luôn ném ngoại lệ generic "requires
// positive evaluation" bất kể có vi phạm hay không (đã thử bằng thực nghiệm cô lập, không suy
// diễn từ tài liệu). Cách quét đồ thị dưới đây tránh hẳn cơ chế "positive results" đó — đúng
// khuôn S4 (AnonymousEndpointScanner.FindAnonymousEndpoints): MỘT hàm, dùng CHUNG cho rule thật
// (BoundaryRuleTests.Core_Application_MustNotDependOn_InfrastructureOrWeb) và bốn meta-test
// Detector_A2_* — xoá nhánh so khớp Web khỏi hàm này là xoá luôn khỏi rule thật, và
// Detector_A2_Catches_RealViolation_ViaWeb phải đỏ theo (đã tự kiểm, xem BoundaryRuleTests.cs).
internal static class BoundaryRules
{
    public static IReadOnlyList<string> FindApplicationTypesDependingOnInfrastructureOrWeb(
        Architecture architecture,
        string applicationAssemblyName,
        string infrastructureAssemblyName,
        string webAssemblyName)
    {
        var offenders = new List<string>();

        foreach (var type in architecture.Classes)
        {
            if (type.Assembly.Name != applicationAssemblyName)
                continue;

            // `Target.Assembly?` — đích là THAM SỐ KIỂU GENERIC (trình biên dịch sinh khi gọi hàm
            // `params ReadOnlySpan<T>` với từ hai đối số) không thuộc assembly nào; nó không thể thuộc
            // assembly bị cấm. Ghim ở BoundaryRulesGenericTargetTests.
            var dependsOnForbidden = type.Dependencies.Any(dependency =>
                dependency.Target.Assembly?.Name == infrastructureAssemblyName ||
                dependency.Target.Assembly?.Name == webAssemblyName);

            if (dependsOnForbidden)
                offenders.Add(type.FullName);
        }

        return offenders;
    }

    // A3/A4 — docs/RULES.md A3, A4. Cùng lý do và cùng khuôn với hàm phía trên: đã đo bằng
    // dotnet test rằng "Should().NotDependOnAny(...).WithoutRequiringPositiveResults()" khiến
    // Evaluate() luôn trả tập rỗng — rule dựng theo combinator đó KHÔNG BAO GIỜ bắt được gì, bất
    // kể có vi phạm thật hay không (xem BoundaryRuleTests.cs, ModuleIsolationTests.cs). Quét trực
    // tiếp theo TIỀN TỐ NAMESPACE trên FullName (không theo tên assembly, khác hàm phía trên) vì
    // A3/A4 là luật về namespace ("CoreAndSkill.Core." không được đụng "CoreAndSkill.Modules.",
    // "CoreAndSkill.Modules.A." không được đụng "CoreAndSkill.Modules.B."), không phải luật về
    // assembly — một assembly có thể (về sau) chứa nhiều namespace. MỘT hàm dùng CHUNG cho rule
    // thật lẫn mọi meta-test Detector_A3_*/Detector_A4_*: xoá hàm này là xoá luôn khỏi rule thật.
    // B1 — docs/wiki-core/be/trien-khai/02-b1-du-lieu-don-vi-danh-tinh.md §4: "không project nào
    // lọt tham chiếu EF Core lên tầng ứng dụng". Trước B1, Infrastructure chưa mang gói EF Core nên
    // nhánh này không có nghĩa (docs/RULES.md A2); từ B1, CoreDbContext tồn tại nên rule dựng được
    // trên assembly THẬT. Quét theo TIỀN TỐ TÊN ASSEMBLY của dependency target — không theo tên
    // project — vì EF Core/ASP.NET Core là gói bên ngoài, không phải project trong solution.
    public static IReadOnlyList<string> FindTypesInAssemblyDependingOnForbiddenAssemblyPrefixes(
        Architecture architecture,
        string sourceAssemblyName,
        IReadOnlyList<string> forbiddenAssemblyNamePrefixes)
    {
        var offenders = new List<string>();

        foreach (var type in architecture.Classes)
        {
            if (type.Assembly.Name != sourceAssemblyName)
                continue;

            var dependsOnForbidden = type.Dependencies.Any(dependency => forbiddenAssemblyNamePrefixes.Any(prefix =>
                dependency.Target.Assembly?.Name is { } targetAssembly && targetAssembly.StartsWith(prefix, StringComparison.Ordinal)));

            if (dependsOnForbidden)
                offenders.Add(type.FullName);
        }

        return offenders;
    }

    public static IReadOnlyList<string> FindTypesInNamespaceDependingOnNamespace(
        Architecture architecture,
        string sourceNamespacePrefix,
        string targetNamespacePrefix)
    {
        var offenders = new List<string>();

        foreach (var type in architecture.Classes)
        {
            if (!type.FullName.StartsWith(sourceNamespacePrefix, StringComparison.Ordinal))
                continue;

            var dependsOnForbidden = type.Dependencies.Any(dependency =>
                dependency.Target.FullName.StartsWith(targetNamespacePrefix, StringComparison.Ordinal));

            if (dependsOnForbidden)
                offenders.Add(type.FullName);
        }

        return offenders;
    }
}
