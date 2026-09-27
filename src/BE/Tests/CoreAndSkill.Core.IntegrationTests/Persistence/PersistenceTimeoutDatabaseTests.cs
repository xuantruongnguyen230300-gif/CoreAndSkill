using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Luật E11 (docs/RULES.md §4), lớp 2 — nửa trên PostgreSQL thật của CoreExecutionStrategyTests. Ba điều lớp 1 (ngoại lệ
// dựng bằng tay) không chứng minh được:
//   1. Hết lock_timeout thật đi qua đúng đường ghi ma trận (PermissionMatrixService, khoá tư vấn) và ra 500 sau MỘT lần
//      chờ, không phải bốn.
//   2. Hết CommandTimeout phía client THẬT SỰ có hình dạng NpgsqlException bọc TimeoutException — ADR-0053 mới dựa vào
//      tài liệu Npgsql cho điều này, chưa tái hiện. Nếu hình dạng khác (ví dụ PostgresException 57014), test đỏ ở dòng
//      khẳng định kiểu và phép loại trong CoreExecutionStrategy phải được xét lại.
//   3. Hết hạn chờ POOL cũng mang hình dạng đó và không bị thử lại (ADR-0055 quyết định 2) — cùng lý do như điều 2.
// Host ở đây là chế độ PostgreSQL thật: strategy của production, không phải NonRetryingExecutionStrategy của chế độ không
// database (CoreWebApplicationFactory).
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class PersistenceTimeoutDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string MatrixUrl = "/api/v1/core/permissions/matrix";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // PermissionMatrixService đặt lock_timeout = 5s qua LockTimeout. Một lần chờ ≈ 5 giây; bốn lần (thử lại 3) ≥ 20 giây
    // cộng nhịp lùi.
    private static readonly TimeSpan OneLockWait = TimeSpan.FromSeconds(5);

    private readonly B4DockerHost _host = new(db.ConnectionString, configure: services =>
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = InMemoryCoreHost.TestScheme;
                options.DefaultAuthenticateScheme = InMemoryCoreHost.TestScheme;
                options.DefaultChallengeScheme = InMemoryCoreHost.TestScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, InMemoryCoreHost.TestAuthHandler>(InMemoryCoreHost.TestScheme, _ => { });
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
    });

    public Task InitializeAsync() => db.ResetAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task MatrixWrite_LockTimeout_FailsWithoutRetry()
    {
        var tenant = await _host.CreateTenantAsync("DV-LOCK");
        using var client = Client(tenant);
        var (version, entries) = await GetMatrixPayloadAsync(client);
        var auditRowsBefore = await CountMatrixAuditRowsAsync(tenant.Id);

        // Một kết nối khác giữ khoá tư vấn của ma trận đơn vị này, cấp PHIÊN — cùng không gian khoá với
        // pg_advisory_xact_lock mà PermissionMatrixService xin, nên lượt PUT phải chờ tới hết lock_timeout.
        await using var holder = new NpgsqlConnection(db.ConnectionString);
        await holder.OpenAsync();
        await using (var take = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key, 0))", holder))
        {
            take.Parameters.AddWithValue("key", $"core.permission_matrix:{tenant.Id}");
            await take.ExecuteNonQueryAsync();
        }

        var clock = Stopwatch.StartNew();
        var response = await client.PutAsJsonAsync(MatrixUrl, new { version, entries });
        clock.Stop();

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError, await response.Content.ReadAsStringAsync());
        clock.Elapsed.ShouldBeGreaterThanOrEqualTo(OneLockWait - TimeSpan.FromMilliseconds(500), "chống test rỗng: lượt PUT phải thật sự chờ khoá");
        clock.Elapsed.ShouldBeLessThan(OneLockWait * 2, $"PUT chờ {clock.Elapsed} — hết lock_timeout đã bị thử lại");
        (await CountMatrixAuditRowsAsync(tenant.Id)).ShouldBe(auditRowsBefore);
    }

    [Fact]
    public async Task CommandTimeout_SurfacesAsNpgsqlExceptionWrappingTimeout_AndIsNotRetried()
    {
        using var scope = _host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        context.Database.SetCommandTimeout(1);

        var strategy = context.Database.CreateExecutionStrategy();
        strategy.ShouldBeOfType<CoreExecutionStrategy>("chế độ PostgreSQL thật phải chạy strategy của production");

        var attempts = 0;
        var thrown = await Should.ThrowAsync<Exception>(() => strategy.ExecuteAsync(async () =>
        {
            attempts++;
            await context.Database.ExecuteSqlRawAsync("SELECT pg_sleep(5)");
        }));

        attempts.ShouldBe(1, "hết CommandTimeout đã bị thử lại");
        thrown.ShouldBeOfType<NpgsqlException>($"hình dạng thật: {thrown}");
        thrown.InnerException.ShouldBeOfType<TimeoutException>($"hình dạng thật: {thrown}");
    }

    // Luật E11 phần hết hạn CHỜ POOL — docs/adr/0055-commit-khong-nhan-token-huy-va-het-han-mo-ket-noi-khong-thu-lai.md
    // quyết định 2. Lớp 1 (CoreExecutionStrategyTests) ghim phân loại theo HÌNH DẠNG NpgsqlException bọc TimeoutException,
    // dựng tay; ADR-0055 dựa vào hiểu biết rằng Npgsql báo hết chờ pool bằng đúng hình dạng đó mà chưa ai tái hiện. Test này
    // tái hiện: pool một chỗ, một kết nối giữ chỗ, lần mở thứ hai phải hết hạn — và strategy của Core không thử lại nó.
    // Nếu hình dạng thật khác, test đỏ ở dòng khẳng định kiểu và IsWaitLimitExceeded phải được xét lại.
    [Fact]
    public async Task PoolWaitTimeout_SurfacesAsNpgsqlExceptionWrappingTimeout_AndIsNotRetried()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(db.ConnectionString)
        {
            MaxPoolSize = 1,
            Timeout = 1,
            ApplicationName = "e11-pool-wait-probe", // pool RIÊNG — chuỗi kết nối khác thì pool khác
        }.ConnectionString;

        await using var holder = new NpgsqlConnection(connectionString);
        await holder.OpenAsync(); // chiếm chỗ duy nhất của pool

        await using var context = new CoreDbContext(
            new DbContextOptionsBuilder<CoreDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.ExecutionStrategy(dependencies => new CoreExecutionStrategy(
                    dependencies, CoreExecutionStrategy.CoreMaxRetryCount, TimeSpan.FromMilliseconds(1))))
                .UseSnakeCaseNamingConvention()
                .Options,
            NSubstitute.Substitute.For<CoreAndSkill.Core.Application.Common.Interfaces.ITenantContext>());

        var strategy = context.Database.CreateExecutionStrategy();
        strategy.ShouldBeOfType<CoreExecutionStrategy>();

        var attempts = 0;
        var thrown = await Should.ThrowAsync<Exception>(() => strategy.ExecuteAsync(async () =>
        {
            attempts++;
            await using var second = new NpgsqlConnection(connectionString);
            await second.OpenAsync();
        }));

        attempts.ShouldBe(1, $"hết hạn chờ pool đã bị thử lại — hình dạng thật: {thrown}");
        thrown.ShouldBeOfType<NpgsqlException>($"hình dạng thật: {thrown}");
        thrown.InnerException.ShouldBeOfType<TimeoutException>($"hình dạng thật: {thrown}");
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    private Task<long> CountMatrixAuditRowsAsync(Guid tenantId)
        => db.CountAsync(
            "SELECT count(*) FROM core.audit_log WHERE tenant_id = @tenant AND action_code = @action",
            new NpgsqlParameter("tenant", tenantId),
            new NpgsqlParameter("action", AuditActionCodes.PermissionMatrixUpdate));

    // Payload cấp thêm một ô chưa cấp cho vai trò thử — một lượt ghi thật, phải xin khoá.
    private static async Task<(string Version, object[] Entries)> GetMatrixPayloadAsync(HttpClient client)
    {
        var response = await client.GetAsync(MatrixUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var data = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!.Data;

        var role = data.GetProperty("roles").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == FakeTenantSeedSource.RoleName).GetProperty("id").GetGuid();
        var granted = false;

        var entries = data.GetProperty("rows").EnumerateArray().Select(r =>
        {
            var roleIds = r.GetProperty("grantedRoleIds").EnumerateArray().Select(x => x.GetGuid()).ToList();
            if (!granted && !roleIds.Contains(role))
            {
                roleIds.Add(role);
                granted = true;
            }

            return (object)new { permissionId = r.GetProperty("permissionId").GetGuid(), roleIds };
        }).ToArray();

        granted.ShouldBeTrue("chống test rỗng: cần một ô chưa cấp để lượt PUT thật sự ghi");
        return (data.GetProperty("version").GetString()!, entries);
    }

    private HttpClient Client(TestTenant tenant)
    {
        var client = _host.Factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-User", tenant.AdminUserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-UserName", tenant.AdminUserName);
        client.DefaultRequestHeaders.Add("X-Test-Tenant", tenant.Id.ToString());

        var token = client.GetFromJsonAsync<JsonElement>("/api/v1/core/antiforgery/token", Json).GetAwaiter().GetResult()
            .GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        return client;
    }
}
