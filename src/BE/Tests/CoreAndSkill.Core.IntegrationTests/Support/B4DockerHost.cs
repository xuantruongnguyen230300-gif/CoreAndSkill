using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Host thật + PostgreSQL thật (PostgresFixture) cho các test RequiresDocker của pha B4.
//
// KHÁC host thường ở MỘT điểm có chủ đích: mọi dịch vụ nền của Core.Infrastructure bị GỠ khỏi danh sách IHostedService.
// Bộ phát outbox quét mỗi vài giây — nếu để chạy, nó tranh dòng với chính lời gọi DispatchBatchAsync của test và mọi
// khẳng định về "dòng này ở trạng thái nào sau lượt này" trở thành phụ thuộc thời điểm. Test dựng lại đúng thứ cần
// (OutboxDispatcher là singleton trong DI; các dịch vụ nền khác dựng bằng ActivatorUtilities) và gọi từng lượt tay.
//
// Đồng hồ là MutableClock: test tua thời gian để kiểm lùi dần và "quá tuổi" mà không phải ngủ.
internal sealed class B4DockerHost : IAsyncDisposable
{
    public const string Password = "Passw0rd-Test1";

    public CoreWebApplicationFactory Factory { get; }

    public MutableClock Clock { get; } = new(DateTimeOffset.UtcNow);

    public IServiceProvider Services => Factory.Services;

    public B4DockerHost(string connectionString, IReadOnlyDictionary<string, string?>? extraConfig = null, Action<IServiceCollection>? configure = null)
    {
        var config = new Dictionary<string, string?> { ["ConnectionStrings:Core"] = connectionString };
        if (extraConfig is not null)
        {
            foreach (var pair in extraConfig)
                config[pair.Key] = pair.Value;
        }

        Factory = new CoreWebApplicationFactory(config, services =>
        {
            services.AddSingleton<ITenantSeedSource, FakeTenantSeedSource>();

            var infrastructureAssembly = typeof(CoreDbContext).Assembly;
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Assembly == infrastructureAssembly)
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<TimeProvider>(Clock);
            configure?.Invoke(services);
        });
    }

    public async Task<TestTenant> CreateTenantAsync(string code)
    {
        using var scope = Services.CreateScope();

        var result = await ProvisioningInTransaction.RunAsync(scope.ServiceProvider, (provisioning, ct) => provisioning.CreateTenantAsync(
            new CreateTenantInput(
                Code: code,
                Name: $"Đơn vị {code}",
                IsSystem: false,
                AdminUserName: "admin",
                AdminPassword: Password,
                AdminHasPermissionBypass: true,
                AdminIsSystemOperator: false,
                FailIfExists: false,
                AdminEmail: $"admin@{code.ToLowerInvariant()}.example.com",
                AdminFullName: "Quản trị đơn vị"),
            ct));

        if (result.IsFailure)
            throw new InvalidOperationException($"Không tạo được đơn vị '{code}' cho test: {result.Error!.Code}.");

        return new TestTenant(result.Value.TenantId, result.Value.AdminUserId, "admin");
    }

    // Chạy `action` như một request của người dùng đó: ngữ cảnh đơn vị/người dùng mở, DI scope mới (DbContext, kết nối
    // và UnitOfWork mới — cùng vòng đời một request thật).
    public async Task<T> AsAsync<T>(TestTenant tenant, Func<IServiceProvider, Task<T>> action)
    {
        using var context = Services.GetRequiredService<IExecutionContextScope>().Enter(tenant.Id, tenant.AdminUserId, tenant.AdminUserName);
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public Task AsAsync(TestTenant tenant, Func<IServiceProvider, Task> action)
        => AsAsync(tenant, async sp =>
        {
            await action(sp);
            return 0;
        });

    public async ValueTask DisposeAsync() => await Factory.DisposeAsync();
}

internal sealed record TestTenant(Guid Id, Guid AdminUserId, string AdminUserName);

internal sealed class MutableClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
