using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Luật M16 (ADR-0122): service tạo đơn vị tự ép "mọi tài khoản của đơn vị hệ thống mang is_system_operator". Không cần database:
// phép kiểm chạy trước mọi lời gọi hạ tầng, nên các phụ thuộc truyền null.
public class TenantProvisioningSystemOperatorInvariantTests
{
    private static TenantProvisioningService NewServiceWithoutDependencies()
        => new(null!, null!, null!, null!, [], [], null!, null!, null!, null!, null!, null!);

    private static CreateTenantInput Input(bool isSystem, bool bypass, bool operatorFlag)
        => new(
            Code: "SYSTEM", Name: "Hệ thống", IsSystem: isSystem, AdminUserName: "superadmin",
            AdminPassword: "Passw0rd-Test1", AdminHasPermissionBypass: bypass, AdminIsSystemOperator: operatorFlag,
            AdminEmail: "superadmin@system.example.com", AdminFullName: "Vận hành");

    // Cặp xanh–đỏ (T12): đỏ — thiếu cờ vận hành; đỏ — mang cờ bypass; xanh — đúng cờ.
    [Fact]
    public async Task CreateTenant_SystemTenantFirstAccountWithoutOperatorFlag_Throws()
    {
        var service = NewServiceWithoutDependencies();

        await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateTenantAsync(Input(isSystem: true, bypass: false, operatorFlag: false), CancellationToken.None));
    }

    [Fact]
    public async Task CreateTenant_SystemTenantFirstAccountWithBypassFlag_Throws()
    {
        var service = NewServiceWithoutDependencies();

        await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateTenantAsync(Input(isSystem: true, bypass: true, operatorFlag: true), CancellationToken.None));
    }

    [Fact]
    public void EnsureSystemTenantAdminIsOperator_SystemTenantWithOperatorFlag_DoesNotThrow()
        => Should.NotThrow(() => TenantProvisioningService.EnsureSystemTenantAdminIsOperator(Input(true, false, true)));

    [Fact]
    public void EnsureSystemTenantAdminIsOperator_BusinessTenant_DoesNotThrow()
        => Should.NotThrow(() => TenantProvisioningService.EnsureSystemTenantAdminIsOperator(Input(false, true, false)));
}
