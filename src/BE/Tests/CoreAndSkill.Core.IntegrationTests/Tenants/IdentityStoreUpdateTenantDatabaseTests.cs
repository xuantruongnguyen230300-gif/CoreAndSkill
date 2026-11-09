using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Luật M8 — đường UserManager thật trên PostgreSQL: ChangePasswordAsync và AccessFailedAsync đều qua UserStore.UpdateAsync
// (DbContext.Update đánh dấu mọi cột Modified, kể cả tenant_id). Không được ném "TenantId không bao giờ đổi".
// Ca không cần database nằm ở TenantAssignmentInterceptorTests.
// Cần Docker daemon: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class IdentityStoreUpdateTenantDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private B4DockerHost _host = null!;

    public async Task InitializeAsync()
    {
        await db.ResetAsync();
        _host = new B4DockerHost(db.ConnectionString);
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task ChangePasswordAsync_SavesThroughUserStoreUpdate_WithoutTenantIdError()
    {
        var tenant = await _host.CreateTenantAsync("DV-IDUPD-1");

        var result = await _host.AsAsync(tenant, async sp =>
        {
            var users = sp.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByIdAsync(tenant.AdminUserId.ToString());
            user.ShouldNotBeNull();
            return await users.ChangePasswordAsync(user, B4DockerHost.Password, "Moi-Passw0rd-Test2");
        });

        result.Succeeded.ShouldBeTrue(string.Join(";", result.Errors.Select(e => e.Code)));
    }

    [Fact]
    public async Task AccessFailedAsync_SavesThroughUserStoreUpdate_WithoutTenantIdError()
    {
        var tenant = await _host.CreateTenantAsync("DV-IDUPD-2");

        var result = await _host.AsAsync(tenant, async sp =>
        {
            var users = sp.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByIdAsync(tenant.AdminUserId.ToString());
            user.ShouldNotBeNull();
            return await users.AccessFailedAsync(user);
        });

        result.Succeeded.ShouldBeTrue(string.Join(";", result.Errors.Select(e => e.Code)));
    }
}
