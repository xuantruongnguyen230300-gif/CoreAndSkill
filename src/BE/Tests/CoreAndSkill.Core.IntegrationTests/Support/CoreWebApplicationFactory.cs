using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// docs/wiki-core/be/04-testing-strategy.md §4 đòi PostgreSQL thật (Testcontainers) cho integration
// test. Factory này có HAI cách dùng:
//   - Không truyền gì: chuỗi kết nối KHÔNG TỚI ĐƯỢC (UnreachableConnectionString) — cho test chỉ cần
//     pass/fail ở lớp middleware, TRƯỚC khi chạm DB (SecurityPipelineTests, B3EndpointSecurityTests…).
//   - Truyền "ConnectionStrings:Core" trỏ vào PostgresFixture: test chạy trên PostgreSQL thật, schema
//     áp bằng đúng script của runbook (Support/PostgresFixture.cs). configureServices cho test thay/
//     thêm đăng ký DI (ví dụ một ITenantSeedSource giả) — chạy SAU đăng ký của ứng dụng.
//
// Chiến lược thử lại (docs/quy-uoc/be-performance.md §7.2, ADR-0053 quyết định 4): ở chế độ chuỗi kết nối KHÔNG TỚI
// ĐƯỢC, mọi lượt thử lại chắc chắn hỏng — factory thay strategy của CoreDbContext bằng NonRetryingExecutionStrategy. Chế
// độ PostgreSQL thật GIỮ NGUYÊN strategy của production (canh bằng DockerModeFactory_KeepsProductionRetryStrategy).
// Test không cần database mà cần chính strategy của production (kết nối giả, CoreDbContextResilienceTests) truyền
// keepProductionRetryStrategy: true.
internal sealed class CoreWebApplicationFactory(
    IReadOnlyDictionary<string, string?>? configOverrides = null,
    Action<IServiceCollection>? configureServices = null,
    string environment = "Production",
    bool keepProductionRetryStrategy = false)
    : WebApplicationFactory<Program>
{
    // Chuỗi hợp lệ về cú pháp nhưng trỏ tới cổng không ai lắng nghe — buộc timeout nhanh thay vì
    // treo lâu khi test cố tình mô phỏng "không kết nối được DB". "Connection Idle Lifetime" PHẢI
    // >= "Connection Pruning Interval" mặc định (10s) — Npgsql ném ArgumentException lúc DỰNG
    // NpgsqlConnection nếu không (bẫy lộ ra từ B1, khi DbConnection thật sự được tạo lần đầu).
    public const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=coreandskill_test;Username=test;Password=test;Timeout=2";

    // Allowlist CORS của host thử. Origin của máy dev nằm ở appsettings.Development.json, tệp gốc KHÔNG mang giá trị dev
    // (docs/quy-uoc/repo-artifact.md §6.2) — host thử mặc định chạy Production nên không đọc tệp đó, và thiếu khoá này thì
    // ValidateOnStart chặn khởi động (docs/quy-uoc/be-architecture.md §4.2). Factory tự khai, TRỪ KHI test tự khai bất kỳ
    // khoá nào dưới AllowedOriginsKey — kể cả giá trị null: đó là cách test nói "dùng đúng giá trị của hai tệp appsettings"
    // để kiểm fail-fast (ConfigurationFailFastTests).
    public const string AllowedOriginsKey = "Core:Auth:AllowedOrigins";
    public const string TestAllowedOrigin = "https://localhost:4200";

    // Thư mục gốc kho tệp RIÊNG cho mỗi factory — Core:File:RootPath là khoá bắt buộc, không mặc định
    // (docs/wiki-core/be/14-file-storage.md §2), và phải là thư mục có thật, ghi được, ngoài thư mục ứng
    // dụng. Nằm trong thư mục tạm của hệ điều hành nên thoả cả ba. Xoá khi factory bị huỷ.
    private readonly DirectoryInfo _fileRoot = Directory.CreateTempSubdirectory("coreandskill-files-");

    public string FileRootPath => _fileRoot.FullName;

    private readonly IReadOnlyDictionary<string, string?> _overrides = configOverrides
        ?? new Dictionary<string, string?> { ["ConnectionStrings:Core"] = UnreachableConnectionString };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Mặc định Production: test chạy trên đúng cấu hình an toàn của môi trường thật. Test cần một endpoint chỉ có
        // ngoài Production (diagnostics/probe — docs/contracts/diagnostics.md) truyền môi trường khác.
        builder.UseEnvironment(environment);

        // Test tự đặt Core:File:RootPath (để kiểm phép fail-fast) thì thắng giá trị mặc định của factory.
        var merged = new Dictionary<string, string?>(_overrides);
        merged.TryAdd("Core:File:RootPath", _fileRoot.FullName);

        if (!merged.Keys.Any(key => key.StartsWith(AllowedOriginsKey, StringComparison.OrdinalIgnoreCase)))
            merged[AllowedOriginsKey + ":0"] = TestAllowedOrigin;

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(merged));

        if (IsUnreachableMode(merged))
        {
            // Luật E8 (DatabaseSchemaStartupCheck): không kết nối được database để kiểm migration thì tiến trình TỪ CHỐI khởi
            // động. Ở chế độ này database cố ý không tới được, nên phép kiểm lúc khởi động được thay bằng một bộ kiểm báo
            // "khớp" — nếu không, mọi host của chế độ này dừng sau Core:SchemaCheck:ConnectWaitSeconds. Chỉ bộ kiểm bị thay,
            // không phải phép kiểm: vòng thử lại và phép từ chối khởi động được test riêng ở DatabaseSchemaStartupCheckTests.
            // DatabaseHealthCheck cũng dùng ISchemaVerifier, nhưng nó mở kết nối TRƯỚC — ở đây kết nối hỏng trước khi tới bộ
            // kiểm, nên /health/ready vẫn báo không sẵn sàng.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISchemaVerifier>();
                services.AddScoped<ISchemaVerifier, NoDriftSchemaVerifier>();
            });
        }

        if (!keepProductionRetryStrategy && IsUnreachableMode(merged))
        {
            // ConfigureDbContext chạy SAU cấu hình của AddDbContext trong ứng dụng; UseNpgsql không kèm kết nối giữ
            // nguyên kết nối đã khai, chỉ đổi strategy.
            builder.ConfigureTestServices(services => services.ConfigureDbContext<CoreDbContext>(options =>
                options.UseNpgsql(npgsql => npgsql.ExecutionStrategy(dependencies => new NonRetryingExecutionStrategy(dependencies)))));
        }

        if (configureServices is not null)
            builder.ConfigureTestServices(configureServices);
    }

    private static bool IsUnreachableMode(IReadOnlyDictionary<string, string?> config)
        => config.TryGetValue("ConnectionStrings:Core", out var connectionString)
           && string.Equals(connectionString, UnreachableConnectionString, StringComparison.Ordinal);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && _fileRoot.Exists)
            _fileRoot.Delete(recursive: true);
    }

    // Cookie phiên VÀ cookie antiforgery đều SecurePolicy = Always — chỉ hoạt động trên HTTPS, cùng
    // ràng buộc "cùng tên miền gốc VÀ cùng scheme" của docs/quy-uoc/be-api-controller.md §7.1.
    // CreateClient()/CreateDefaultClient() của WebApplicationFactory KHÔNG virtual ở bản package
    // đang dùng — không chặn được BaseAddress mặc định "http://localhost" bằng cách override. Test
    // cần gọi request GHI (POST/PUT/PATCH/DELETE) — tức chạm AntiforgeryValidationMiddleware — PHẢI
    // dùng phương thức này thay vì CreateClient() trần; thiếu HTTPS, IAntiforgery.IsRequestValidAsync
    // ném InvalidOperationException ("not an SSL request") thay vì trả 403 CSRF/ORIGIN như thật.
    public HttpClient CreateHttpsClient(WebApplicationFactoryClientOptions? options = null)
    {
        options ??= new WebApplicationFactoryClientOptions();
        options.BaseAddress = new Uri("https://localhost");
        return CreateClient(options);
    }
}

// Bộ kiểm migration luôn báo "khớp" — cho host không có database (chế độ chuỗi kết nối không tới được), và cho test chỉ cần
// DI của một host mà không cần database. KHÔNG dùng cho test nào khẳng định điều gì về phép kiểm E8.
internal sealed class NoDriftSchemaVerifier : ISchemaVerifier
{
    public Task<IReadOnlyList<SchemaDriftReport>> InspectAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<SchemaDriftReport>>([]);
}
