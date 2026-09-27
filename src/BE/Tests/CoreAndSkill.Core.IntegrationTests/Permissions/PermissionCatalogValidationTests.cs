using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Infrastructure.DependencyInjection;
using CoreAndSkill.Core.Infrastructure.Permissions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// Luật B7 NỬA (1) — docs/RULES.md §6, docs/quy-uoc/be-architecture.md §1.1.
//
// B7 có hai nửa và thiếu nửa nào cũng hở. Nửa (2) là cổng đối chiếu hai chiều trong ArchTests. Nửa
// (1) là PermissionCatalogValidationHostedService: kiểm danh mục ĐÃ GỘP lúc khởi động — khoá trùng
// giữa hai nguồn, sai khuôn, nhãn rỗng. Trước file này nửa (1) KHÔNG có test nào.
//
// VÌ SAO NỬA (1) KHÔNG THỂ DỰA VÀO NỬA (2). Cổng B7 trong ArchTests quét bằng REFLECTION trên
// assembly, không đi qua DI. Xoá dòng `services.AddSingleton<IPermissionCatalogSource,
// CorePermissionCatalogSource>()` thì cổng đó vẫn xanh — nó vẫn tìm thấy lớp trong assembly. Nhưng
// lúc chạy, `IEnumerable<IPermissionCatalogSource>` thành RỖNG:
//   • hosted service validate một tập rỗng và trả về xanh;
//   • MenuQueryService nhận danh mục rỗng.
// Không lỗi biên dịch, không cổng nào đỏ. Đó là lý do phần dưới có test seam ĐỘNG (luật T7): dựng
// container thật rồi hỏi nó, thay vì hỏi assembly.
//
// Không chạm database — chỉ danh mục trong bộ nhớ và đồ thị DI, nên KHÔNG mang
// [Trait("Category", "RequiresDocker")] (luật T9).
public sealed class PermissionCatalogValidationTests
{
    private sealed class FixedSource(
        IReadOnlyCollection<PermissionResourceDefinition> resources,
        IReadOnlyCollection<PermissionDefinition> permissions) : IPermissionCatalogSource
    {
        public IReadOnlyCollection<PermissionResourceDefinition> GetResources() => resources;
        public IReadOnlyCollection<PermissionDefinition> GetPermissions() => permissions;
    }

    private static IPermissionCatalogSource Source(string resourceKey, string action, string? nameKey = null)
        => new FixedSource(
            [new(resourceKey, $"resource.{resourceKey}", ModuleKey: null, DisplayOrder: 10)],
            [new($"{resourceKey}.{action}", resourceKey, action, nameKey ?? $"permission.{resourceKey}.{action}", 10)]);

    private static Task StartAsync(params IPermissionCatalogSource[] sources)
        => new PermissionCatalogValidationHostedService(sources).StartAsync(CancellationToken.None);

    // ===== Nửa (1): danh mục hợp lệ thì khởi động được =====

    [Fact]
    public async Task Validation_Accepts_TwoSourcesWithDisjointKeys()
    {
        await StartAsync(Source("mod.a", "read"), Source("mod.b", "read"));
    }

    [Fact]
    public async Task Validation_Accepts_TheRealCoreCatalog()
    {
        await StartAsync(new CorePermissionCatalogSource());
    }

    // ===== Nửa (1): mỗi luật một ca hỏng =====
    //
    // Ném từ StartAsync nghĩa là host DỪNG, không mở cổng — đó là hành vi được khẳng định ở đây,
    // không phải "có ghi log".

    // Hai nguồn khai TRÙNG Code trong khi tài nguyên của chúng khác nhau — ca này lọt qua mọi phép
    // kiểm nội bộ từng nguồn, chỉ phép kiểm trên danh mục ĐÃ GỘP mới thấy. Đó đúng là lý do nửa (1)
    // tồn tại: không nguồn nào một mình sai cả.
    [Fact]
    public async Task Validation_Rejects_DuplicateCode_AcrossSources()
    {
        var collider = new FixedSource(
            [new("mod.b", "resource.mod.b", ModuleKey: null, DisplayOrder: 20)],
            [new("mod.a.read", "mod.b", "read", "permission.mod.a.read", 10)]);

        var error = await Should.ThrowAsync<InvalidOperationException>(
            () => StartAsync(Source("mod.a", "read"), collider));

        error.Message.ShouldContain("mod.a.read");
        error.Message.ShouldContain("trùng");
    }

    [Fact]
    public async Task Validation_Rejects_DuplicateResourceKey_AcrossSources()
    {
        var duplicate = new FixedSource(
            [new("mod.a", "resource.mod.a", ModuleKey: null, DisplayOrder: 10)],
            []);

        var error = await Should.ThrowAsync<InvalidOperationException>(
            () => StartAsync(Source("mod.a", "read"), duplicate));

        error.Message.ShouldContain("mod.a");
    }

    // Một permission trỏ sang tài nguyên của NGUỒN KHÁC là cách một module mượn tài nguyên của Core
    // rồi lệ thuộc vào thứ tự nạp. Luật: tài nguyên phải nằm trong CÙNG nguồn.
    [Fact]
    public async Task Validation_Rejects_PermissionPointingAtResourceOfAnotherSource()
    {
        var borrower = new FixedSource(
            [],
            [new("mod.a.write", "mod.a", "write", "permission.mod.a.write", 20)]);

        var error = await Should.ThrowAsync<InvalidOperationException>(
            () => StartAsync(Source("mod.a", "read"), borrower));

        error.Message.ShouldContain("mod.a.write");
    }

    [Fact]
    public async Task Validation_Rejects_EmptyPermissionNameKey()
    {
        var error = await Should.ThrowAsync<InvalidOperationException>(
            () => StartAsync(Source("mod.a", "read", nameKey: "   ")));

        error.Message.ShouldContain("mod.a.read");
    }

    [Fact]
    public async Task Validation_Rejects_EmptyResourceNameKey()
    {
        var source = new FixedSource(
            [new("mod.a", "  ", ModuleKey: null, DisplayOrder: 10)],
            []);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(source));

        error.Message.ShouldContain("mod.a");
    }

    [Theory]
    [InlineData("Mod.A", "read")] // chữ hoa
    [InlineData("mod_a", "read")] // gạch dưới ở vị trí đoạn
    [InlineData("mod.a", "Read")] // hành động chữ hoa
    [InlineData("mod.a", "re ad")] // khoảng trắng
    public async Task Validation_Rejects_CodeOutsideTheShape(string resourceKey, string action)
    {
        await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(Source(resourceKey, action)));
    }

    // Code phải bằng ResourceKey + "." + Action. Lệch ở đây thì khoá trong DB và khoá mã nguồn dùng
    // để kiểm quyền là hai chuỗi khác nhau.
    [Fact]
    public async Task Validation_Rejects_CodeNotEqualToResourceKeyPlusAction()
    {
        var mismatched = new FixedSource(
            [new("mod.a", "resource.mod.a", ModuleKey: null, DisplayOrder: 10)],
            [new("mod.a.read", "mod.a", "write", "permission.mod.a.read", 10)]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAsync(mismatched));

        error.Message.ShouldContain("mod.a.write");
    }

    // ===== Seam động (T7): nửa (1) phải ĐƯỢC NỐI vào DI, không chỉ tồn tại =====

    [Fact]
    public void CoreCatalogSource_IsRegistered_InTheContainer()
    {
        using var provider = new ServiceCollection().AddCoreIdentity().BuildServiceProvider();

        var sources = provider.GetServices<IPermissionCatalogSource>().ToList();

        sources.ShouldNotBeEmpty(
            "Không nguồn IPermissionCatalogSource nào được đăng ký. Cổng B7 trong ArchTests quét "
          + "assembly nên vẫn xanh, nhưng lúc chạy danh mục khoá RỖNG: hosted service validate tập "
          + "rỗng, và MenuQueryService không thấy khoá nào.");

        sources.ShouldContain(source => source is CorePermissionCatalogSource);
    }

    [Fact]
    public void ValidationHostedService_IsRegistered_InTheContainer()
    {
        using var provider = new ServiceCollection().AddCoreIdentity().BuildServiceProvider();

        provider.GetServices<IHostedService>()
            .ShouldContain(service => service is PermissionCatalogValidationHostedService,
                "Nửa (1) của B7 chỉ có hiệu lực khi hosted service được đăng ký — một lớp validate "
              + "không ai gọi là luật ép bằng niềm tin (luật T7).");
    }
}
