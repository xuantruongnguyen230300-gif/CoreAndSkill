using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Commands;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Lệnh `core bootstrap` chạy trong một scope DI TRƯỚC app.Run() — không hosted service nào khởi động, kể cả
// TenantSeedValidationHostedService. Nguồn seed sai vì thế chỉ bị chặn nếu chính đường ghi tự kiểm
// (TenantProvisioningService.CreateTenantAsync gọi TenantSeedValidator trước dòng ghi đầu tiên). Thiếu lời gọi đó: lệnh
// commit seed sai, thoát 0, và lần khởi động kế tiếp tiến trình từ chối lên — dữ liệu sai đã nằm trong database, chỉ sửa
// được bằng SQL tay.
//
// Không chạm database, Identity lấy từ đăng ký thật — OfflineBootstrapHarness. Không mang [Trait("Category", "RequiresDocker")].
//
// Environment.ExitCode và Console.Error là trạng thái TOÀN CỤC của tiến trình — cùng collection với OutboxReplayCommandTests.
[Collection("Console")]
public sealed class BootstrapSeedValidationTests : IDisposable
{
    private readonly TextWriter _originalError = Console.Error;
    private readonly TextWriter _originalOut = Console.Out;
    private readonly StringWriter _error = new();
    private readonly StringWriter _out = new();

    public BootstrapSeedValidationTests()
    {
        Console.SetError(_error);
        Console.SetOut(_out);
        Environment.ExitCode = 0;
    }

    public void Dispose()
    {
        Console.SetError(_originalError);
        Console.SetOut(_originalOut);
        Environment.ExitCode = 0;
    }

    // Luật 2 của schema-core.md §6.1 — mục có con mà mang route. Đây đúng là ca ApplySeedAsync KHÔNG tự kiểm: trước bản
    // sửa, lệnh ghi cả hai mục rồi thoát 0.
    [Fact]
    public async Task Bootstrap_MenuParentCarryingARoute_WritesNothing_ExitsOne()
    {
        var harness = OfflineBootstrapHarness.Build(new MenuSeed(
            Menu("bao-cao", route: "/bao-cao"),
            Menu("bao-cao-thang", route: "/bao-cao/thang", parent: "bao-cao")));

        await CoreCommandRunner.RunBootstrapAsync(harness.Services);

        Environment.ExitCode.ShouldBe(1);
        _error.ToString().ShouldContain(TenantProvisioningErrors.SeedFailed.Code);
        harness.ShouldHaveWrittenNothing();
    }

    // Mã menu trùng — trước bản sửa, ApplySeedAsync dàn dựng hai dòng cùng mã và lỗi chỉ lộ ra ở
    // ux_menu_item_tenant_code_active dưới dạng DbUpdateException thô. Giờ nó là SEED_FAILED, trước dòng ghi đầu tiên.
    [Fact]
    public async Task Bootstrap_DuplicateMenuCode_WritesNothing_ExitsOneWithSeedFailed()
    {
        var harness = OfflineBootstrapHarness.Build(new MenuSeed(
            Menu("bao-cao", route: "/bao-cao"),
            Menu("bao-cao", route: "/bao-cao-2")));

        await CoreCommandRunner.RunBootstrapAsync(harness.Services);

        Environment.ExitCode.ShouldBe(1);
        _error.ToString().ShouldContain(TenantProvisioningErrors.SeedFailed.Code);
        harness.ShouldHaveWrittenNothing();
    }

    // Đối chứng chống test rỗng: CÙNG bộ dựng, nguồn seed hợp lệ ⇒ lệnh ghi thật (hai đơn vị, hai commit, có lượt lưu).
    // Nếu bộ dựng không quan sát được lượt ghi nào thì hai test trên xanh vì mù, và test này đỏ. Tài khoản đi qua bộ kiểm
    // người dùng THẬT của Identity — lệnh không truyền email thì ca này đỏ với ADMIN_CREATE_FAILED.
    [Fact]
    public async Task Bootstrap_ValidSeed_WritesBothTenants_ExitsZero()
    {
        var harness = OfflineBootstrapHarness.Build(new MenuSeed(
            Menu("bao-cao", route: null),
            Menu("bao-cao-thang", route: "/bao-cao/thang", parent: "bao-cao")));

        await CoreCommandRunner.RunBootstrapAsync(harness.Services);

        Environment.ExitCode.ShouldBe(0, _error.ToString());
        harness.UnitOfWork.Commits.ShouldBe(2);
        harness.Writes.Saves.ShouldBeGreaterThan(0);
        harness.Writes.StagedRows.ShouldContain(row => row == nameof(CoreAndSkill.Core.Domain.Menu.MenuItem));
    }

    private static SeedMenuItem Menu(string code, string? route, string? parent = null)
        => new(code, $"menu.{code}", Icon: null, Route: route, ParentCode: parent, DisplayOrder: 10,
            RequiredPermissionCode: null, ModuleKey: null);

    // Chỉ menu, không vai trò: đường ghi không gọi tới RoleManager.
    private sealed class MenuSeed(params SeedMenuItem[] menuItems) : ITenantSeedSource
    {
        public IReadOnlyCollection<SeedRole> GetRoles() => [];
        public IReadOnlyCollection<SeedRolePermission> GetRolePermissions() => [];
        public IReadOnlyCollection<SeedMenuItem> GetMenuItems() => menuItems;
    }
}
