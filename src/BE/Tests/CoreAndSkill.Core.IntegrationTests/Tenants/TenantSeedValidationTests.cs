using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Menu;
using CoreAndSkill.Core.Infrastructure.DependencyInjection;
using CoreAndSkill.Core.Infrastructure.Execution;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Permissions;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// docs/quy-uoc/be-architecture.md §1.1 mục "Nguồn seed cho đơn vị mới": nguồn ITenantSeedSource đã gộp được kiểm LÚC
// KHỞI ĐỘNG — khoá quyền không có trong danh mục đã gộp thì tiến trình không khởi động. Thiếu phép kiểm đó, lỗi của
// nguồn seed chỉ lộ ra ở lần tạo đơn vị đầu tiên, tức ở người vận hành, không ở lúc triển khai.
//
// Nửa sau của file: vì lỗi seed đã bị chặn lúc khởi động, TenantProvisioningService chỉ còn được đổi đúng LỖI SEED thành
// CORE.TENANT.SEED_FAILED. Một InvalidOperationException khác (lỗi lập trình, lỗi EF) không được đội lốt lỗi nghiệp vụ.
//
// Không chạm database — danh mục trong bộ nhớ, đồ thị DI, CoreDbContext trên OfflineCoreDbContext. Không mang
// [Trait("Category", "RequiresDocker")] (luật T9).
public sealed class TenantSeedValidationTests
{
    private sealed class FixedSeedSource(
        IReadOnlyCollection<SeedRole>? roles = null,
        IReadOnlyCollection<SeedRolePermission>? rolePermissions = null,
        IReadOnlyCollection<SeedMenuItem>? menuItems = null) : ITenantSeedSource
    {
        public IReadOnlyCollection<SeedRole> GetRoles() => roles ?? [];
        public IReadOnlyCollection<SeedRolePermission> GetRolePermissions() => rolePermissions ?? [];
        public IReadOnlyCollection<SeedMenuItem> GetMenuItems() => menuItems ?? [];
    }

    // Nguồn seed có LỖI LẬP TRÌNH — ném khi được đọc. Không phải một nguồn "không nhất quán".
    private sealed class BrokenSeedSource : ITenantSeedSource
    {
        public IReadOnlyCollection<SeedRole> GetRoles() => throw new InvalidOperationException("lỗi lập trình trong nguồn seed");
        public IReadOnlyCollection<SeedRolePermission> GetRolePermissions() => [];
        public IReadOnlyCollection<SeedMenuItem> GetMenuItems() => [];
    }

    // isGroup: mục CHA đúng nghĩa — chỉ đóng/mở, không điều hướng, nên Route rỗng (schema-core.md §6.1 luật 2).
    private static SeedMenuItem Menu(string code, string? parent = null, string? permission = null, bool isGroup = false)
        => new(code, $"menu.{code}", Icon: null, Route: isGroup ? null : $"/{code}", ParentCode: parent, DisplayOrder: 10,
            RequiredPermissionCode: permission, ModuleKey: null);

    private static async Task StartAllHostedServicesAsync(params ITenantSeedSource[] extraSources)
    {
        var services = new ServiceCollection().AddCoreIdentity();
        foreach (var source in extraSources)
            services.AddSingleton(source);

        await using var provider = services.BuildServiceProvider();
        foreach (var hosted in provider.GetServices<IHostedService>())
            await hosted.StartAsync(CancellationToken.None);
    }

    // ===== Lúc khởi động: nguồn seed đã gộp phải khớp danh mục đã gộp =====

    [Fact]
    public async Task Startup_Accepts_TheRealCoreSeed()
    {
        await StartAllHostedServicesAsync();
    }

    [Fact]
    public async Task Startup_Rejects_RolePermissionWithUnknownPermissionCode()
    {
        var source = new FixedSeedSource(
            roles: [new SeedRole("KeToan", IsSystem: false)],
            rolePermissions: [new SeedRolePermission("KeToan", "mod.khong.ton-tai")]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain("mod.khong.ton-tai");
    }

    [Fact]
    public async Task Startup_Rejects_MenuItemWithUnknownRequiredPermission()
    {
        var source = new FixedSeedSource(menuItems: [Menu("bao-cao", permission: "mod.bao-cao.read")]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain("mod.bao-cao.read");
    }

    [Fact]
    public async Task Startup_Rejects_RoleDeclaredTwice_AcrossSources()
    {
        var first = new FixedSeedSource(roles: [new SeedRole("KeToan", IsSystem: false)]);
        var second = new FixedSeedSource(roles: [new SeedRole("ketoan", IsSystem: true)]); // Identity chuẩn hoá chữ HOA

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(first, second));

        error.Message.ShouldContain("KeToan", Case.Insensitive);
    }

    [Fact]
    public async Task Startup_Rejects_MenuCodeDeclaredTwice_AcrossSources()
    {
        // "quan-tri" là mục cha menu của chính Core (CoreTenantSeedSource) — trùng nó là trùng giữa hai nguồn.
        var source = new FixedSeedSource(menuItems: [Menu("quan-tri")]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain("quan-tri");
    }

    [Fact]
    public async Task Startup_Rejects_RolePermissionPairDeclaredTwice()
    {
        var source = new FixedSeedSource(
            roles: [new SeedRole("KeToan", IsSystem: false)],
            rolePermissions:
            [
                new SeedRolePermission("KeToan", CorePermissions.UserRead),
                new SeedRolePermission("KeToan", CorePermissions.UserRead),
            ]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain(CorePermissions.UserRead);
    }

    [Fact]
    public async Task Startup_Rejects_RolePermissionPointingAtUndeclaredRole()
    {
        var source = new FixedSeedSource(rolePermissions: [new SeedRolePermission("KhongCo", CorePermissions.UserRead)]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain("KhongCo");
    }

    [Fact]
    public async Task Startup_Rejects_MenuItemPointingAtUndeclaredParent()
    {
        var source = new FixedSeedSource(menuItems: [Menu("con-mo-coi", parent: "khong-co-cha")]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain("khong-co-cha");
    }

    // ===== Hai luật của schema-core.md §6.1 mà constraint không diễn đạt được — kiểm ở TenantSeedValidator, lớp mà cả
    // hosted service lẫn đường ghi (TenantProvisioningService.CreateTenantAsync) cùng gọi. Ở đây chứng minh nửa LÚC
    // KHỞI ĐỘNG; nửa ĐƯỜNG GHI (lệnh `core bootstrap`, không hosted service nào chạy) ở BootstrapSeedValidationTests.
    // Bỏ chúng, seed dựng thẳng cây ba cấp vào core.menu_item, và triệu chứng duy nhất là một mục người dùng CÓ QUYỀN
    // nhưng không thấy trên sidebar =====

    // Luật 1 — cây ĐÚNG MỘT CẤP. Chuỗi ba cấp: cap-3 trỏ vào cap-2, mà cap-2 đã có cha là cap-1.
    [Fact]
    public async Task Startup_Rejects_MenuItemPointingAtAnItemThatItselfHasAParent()
    {
        var source = new FixedSeedSource(menuItems:
        [
            Menu("cap-1", isGroup: true),
            Menu("cap-2", parent: "cap-1", isGroup: true),
            Menu("cap-3", parent: "cap-2"),
        ]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain("cap-3");
        error.Message.ShouldContain("cap-2");
    }

    // Luật 2 — mục có con là mục cha, và mục cha chỉ đóng/mở nên route phải rỗng.
    [Fact]
    public async Task Startup_Rejects_MenuParentThatAlsoCarriesARoute()
    {
        var source = new FixedSeedSource(menuItems:
        [
            Menu("bao-cao"), // route "/bao-cao" — nhưng dòng dưới biến nó thành mục cha
            Menu("bao-cao-thang", parent: "bao-cao"),
        ]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain("bao-cao");
        error.Message.ShouldContain("/bao-cao");
    }

    // Đối chứng: phép kiểm siết ở trên KHÔNG được từ chối hình dạng hợp lệ của một nguồn seed module.
    [Fact]
    public async Task Startup_Accepts_TwoLevelMenuWithARoutelessParent()
    {
        var source = new FixedSeedSource(menuItems:
        [
            Menu("bao-cao", isGroup: true),
            Menu("bao-cao-thang", parent: "bao-cao"),
            Menu("bao-cao-nam", parent: "bao-cao"),
        ]);

        await StartAllHostedServicesAsync(source);
    }

    // ===== Mã menu so theo dạng MenuItem.Create LƯU (MenuItem.NormalizeCode), không theo chuỗi thô của nguồn seed.
    // ux_menu_item_tenant_code_active so cột code — tức mã ĐÃ chuẩn hoá. Bộ kiểm so chuỗi thô thì "bao-cao" và "bao-cao "
    // lọt qua, rồi vỡ unique ở SaveChangesAsync: 500 thay vì CORE.TENANT.SEED_FAILED, và `core bootstrap` văng exception
    // chưa xử lý. =====

    [Fact]
    public async Task Startup_Rejects_MenuCodesThatCollideOnlyAfterNormalization()
    {
        var source = new FixedSeedSource(menuItems: [Menu("bao-cao"), Menu("bao-cao ")]);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => StartAllHostedServicesAsync(source));

        error.Message.ShouldContain("bao-cao");
    }

    // Đường ghi (lệnh `core bootstrap` và POST /system/tenants): cùng nguồn đó phải ra lỗi seed đọc được, trước dòng ghi nào.
    [Fact]
    public async Task CreateTenant_MenuCodesThatCollideOnlyAfterNormalization_ReturnsSeedFailed_WithoutStagingAnyMenuRow()
    {
        var source = new FixedSeedSource(menuItems: [Menu("bao-cao"), Menu(" bao-cao")]);
        var (service, db) = BuildProvisioning(source);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateTenantAsync(NewTenantInput(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.SeedFailed.Code);
        db.ChangeTracker.Entries<MenuItem>().ShouldBeEmpty();
    }

    // Ca cặp: ParentCode cũng tra theo mã đã chuẩn hoá. "bao-cao " trỏ đúng mục cha đã lưu là "bao-cao" — bộ kiểm nhận,
    // VÀ đường ghi nối đúng con vào cha. Bộ kiểm nhận mà đường ghi không nối được thì lỗi chỉ dời từ lúc khởi động sang
    // lần tạo đơn vị đầu tiên.
    [Fact]
    public async Task Startup_Accepts_ParentCodeThatMatchesOnlyAfterNormalization()
    {
        var source = new FixedSeedSource(menuItems:
        [
            Menu("bao-cao", isGroup: true),
            Menu("bao-cao-thang", parent: "bao-cao "),
        ]);

        await StartAllHostedServicesAsync(source);
    }

    [Fact]
    public async Task CreateTenant_ParentCodeThatMatchesOnlyAfterNormalization_LinksTheChildToThatParent()
    {
        var source = new FixedSeedSource(menuItems:
        [
            Menu(" bao-cao", isGroup: true),
            Menu("bao-cao-thang", parent: "bao-cao "),
        ]);
        var (service, db) = BuildProvisioning(source);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateTenantAsync(NewTenantInput(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var menu = db.ChangeTracker.Entries<MenuItem>().Select(e => e.Entity).ToList();
        var parent = menu.Single(m => m.Code == "bao-cao");
        menu.Single(m => m.Code == "bao-cao-thang").ParentId.ShouldBe(parent.Id);
    }

    // ===== Seam động (T7): phép kiểm phải ĐƯỢC NỐI vào DI, không chỉ tồn tại — cùng khuôn B7 nửa (1) =====

    [Fact]
    public void CoreTenantSeedSource_IsRegistered_InTheContainer()
    {
        using var provider = new ServiceCollection().AddCoreIdentity().BuildServiceProvider();

        provider.GetServices<ITenantSeedSource>().ShouldContain(source => source is CoreTenantSeedSource,
            "Nguồn seed menu của Core không được đăng ký — đơn vị mới không có mục quản trị nào, và không cổng nào đỏ.");
    }

    [Fact]
    public void TenantSeedValidationHostedService_IsRegistered_InTheContainer()
    {
        using var provider = new ServiceCollection().AddCoreIdentity().BuildServiceProvider();

        provider.GetServices<IHostedService>()
            .ShouldContain(service => service is TenantSeedValidationHostedService,
                "Nguồn seed chỉ được kiểm lúc khởi động khi hosted service được đăng ký — một lớp kiểm không ai gọi là "
              + "luật ép bằng niềm tin (luật T7).");
    }

    // ===== Thu hẹp phép bắt ở TenantProvisioningService =====

    // Đối chứng: lỗi SEED thật (ánh xạ trỏ tới vai trò không khai) vẫn ra CORE.TENANT.SEED_FAILED — không phải 500.
    [Fact]
    public async Task CreateTenant_InconsistentSeed_StillReturnsSeedFailed()
    {
        var source = new FixedSeedSource(rolePermissions: [new SeedRolePermission("KhongCo", CorePermissions.UserRead)]);
        var (service, db) = BuildProvisioning(source);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateTenantAsync(NewTenantInput(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.SeedFailed.Code);
    }

    // Nửa PHỤ THUỘC DATABASE của lỗi seed — thứ TenantSeedValidator không thấy được: khoá có trong danh mục C# (nên phép
    // kiểm đầu thao tác cho qua) mà core.permission chưa có dòng (EmptyReader trả tập rỗng). Nhánh bắt TenantSeedException
    // quanh ApplySeedAsync phải vẫn đổi nó thành CORE.TENANT.SEED_FAILED — ca trên không còn tới được nhánh đó, vì phép kiểm
    // đầu thao tác chặn nó sớm hơn.
    [Fact]
    public async Task CreateTenant_SeedKeyMissingFromTheDatabase_StillReturnsSeedFailed()
    {
        var source = new FixedSeedSource(menuItems: [Menu("bao-cao", permission: CorePermissions.UserRead)]);
        var (service, db) = BuildProvisioning(source);
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var result = await service.CreateTenantAsync(NewTenantInput(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.SeedFailed.Code);
    }

    // Lỗi lập trình trong một nguồn seed KHÔNG phải lỗi seed nghiệp vụ: nó phải đi ra như exception (500 kèm log Error),
    // không bị đổi thành một mã mà người vận hành đọc là "dữ liệu mặc định có vấn đề".
    [Fact]
    public async Task CreateTenant_NonSeedInvalidOperation_IsNotDisguisedAsSeedFailed()
    {
        var (service, db) = BuildProvisioning(new BrokenSeedSource());
        await using var _ = db;
        await db.Database.BeginTransactionAsync();

        var error = await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateTenantAsync(NewTenantInput(), CancellationToken.None));

        error.Message.ShouldContain("lỗi lập trình trong nguồn seed");
    }

    private static CreateTenantInput NewTenantInput() => new(
        Code: "DV-MOI", Name: "Đơn vị mới", IsSystem: false, AdminUserName: "quantri", AdminPassword: "Passw0rd-Tam1",
        AdminHasPermissionBypass: true, AdminIsSystemOperator: false, AdminEmail: "quantri@dv-moi.example.com");

    private static (TenantProvisioningService Service, CoreDbContext Db) BuildProvisioning(ITenantSeedSource seedSource)
    {
        var scope = new ExecutionContextScope();
        var currentUser = Substitute.For<ICurrentUser>();
        var tenantContext = Substitute.For<ITenantContext>();

        var recorder = new SqlRecorder();
        var db = OfflineCoreDbContext.Create(null, [.. recorder.Interceptors, new EmptyReader(), new SaveCapture()]);

        // Identity THẬT — bộ kiểm người dùng, RequireUniqueEmail: ca thành công ở đây phải là thành công trên host thật.
        var identity = OfflineBootstrapHarness.CoreIdentityOver(db);

        var service = new TenantProvisioningService(
            scope, db, identity.GetRequiredService<UserManager<AppUser>>(), identity.GetRequiredService<RoleManager<AppRole>>(),
            [seedSource], [new CorePermissionCatalogSource()], currentUser, tenantContext,
            Substitute.For<IClientAddressAccessor>(),
            TimeProvider.System, new CrossTenantActorScope(), NullLogger<TenantProvisioningService>.Instance);

        return (service, db);
    }

    // Mọi câu đọc trả tập RỖNG: đơn vị chưa có, tên đăng nhập chưa dùng, danh mục quyền không có dòng nào.
    private sealed class EmptyReader : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            => InterceptionResult<DbDataReader>.SuppressWithResult(Empty());

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(Empty()));

        private static DbDataReader Empty()
        {
            var table = new DataTable();
            table.Columns.Add("x", typeof(object));
            return table.CreateDataReader();
        }
    }
}
