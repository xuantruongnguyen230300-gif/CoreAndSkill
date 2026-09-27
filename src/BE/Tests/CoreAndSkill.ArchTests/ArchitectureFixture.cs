using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using RuntimeAssembly = System.Reflection.Assembly;

namespace CoreAndSkill.ArchTests;

// Nạp đồ thị assembly của năm project Core MỘT LẦN, dùng chung cho mọi ArchTest —
// docs/wiki-core/be/04-testing-strategy.md §2.4.
internal static class ArchitectureFixture
{
    // Host KHÔNG nằm trong đồ thị ArchUnit dưới đây — luật layer của Core không áp cho composition
    // root, và kéo nó vào đồ thị sẽ đổi nghĩa của mọi ArchTest đang dùng `Architecture`.
    //
    // Host thuộc vùng dự án và nằm ngoài khối core-paths, theo quyết định 1 của
    // docs/adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md. Nó vẫn phải nằm trong tầm của những
    // cổng quét theo assembly — cổng A7 (HostCompositionRootTests), ranh giới nguồn khoá phân quyền
    // (luật B7) — vì quyết định 8 của cùng ADR: cổng biết host bằng TÊN PROJECT, không bằng khối
    // core-paths. Composition root là chỗ TỰ NHIÊN
    // nhất để cắm nhầm một nguồn khoá: đặt một IPermissionCatalogSource vào đó thì khoá nó cấp chạy thật
    // mà không cổng nào đối chiếu. Đã đo: cả 129 ArchTest xanh nguyên với một nguồn như vậy trong host,
    // trước khi có tay cầm này.
    public static readonly RuntimeAssembly HostAssembly = typeof(global::Program).Assembly;

    public static readonly RuntimeAssembly DomainAssembly = typeof(CoreAndSkill.Core.Domain.Common.Result).Assembly;
    public static readonly RuntimeAssembly ApplicationAssembly = typeof(CoreAndSkill.Core.Application.Common.CommonErrors).Assembly;
    public static readonly RuntimeAssembly InfrastructureAssembly = typeof(CoreAndSkill.Core.Infrastructure.DependencyInjection.CoreInfrastructureServiceCollectionExtensions).Assembly;
    public static readonly RuntimeAssembly WebAssembly = typeof(CoreAndSkill.Core.Web.Http.Envelope).Assembly;
    public static readonly RuntimeAssembly ContractsAssembly = typeof(CoreAndSkill.Core.Contracts.AssemblyMarker).Assembly;

    public static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, InfrastructureAssembly, WebAssembly, ContractsAssembly)
        .Build();
}
