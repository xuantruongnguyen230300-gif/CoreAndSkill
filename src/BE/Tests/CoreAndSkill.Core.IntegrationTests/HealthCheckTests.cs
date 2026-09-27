using System.Net;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests;

// docs/wiki-core/be/07-observability.md §8 — sống và sẵn sàng là hai câu hỏi khác nhau.
//
// /health/ready nay kiểm HAI thứ, không phải một: kết nối được PostgreSQL, VÀ schema không lệch
// khỏi model (DatabaseHealthCheck gọi ISchemaVerifier.InspectAsync ở MỖI lần gọi — mặt còn lại của
// luật E8). Câu cũ ở đây khai "chưa kiểm migration (chưa có CoreDbContext — B1)"; CoreDbContext và
// SchemaVerifier đều đã có, và CoreApplicationBuilderExtensions.UseCoreAsync gọi
// VerifyDatabaseSchemaAsync trước mọi middleware.
//
// Host ở đây là chế độ "không database" của CoreWebApplicationFactory: phép kiểm E8 LÚC KHỞI ĐỘNG (không kết nối được thì
// từ chối khởi động) được thay bằng bộ kiểm báo "khớp" để host lên được — xem chú thích trong factory. Readiness vẫn mở kết
// nối thật trước khi tới bộ kiểm, nên nhánh "không kết nối được" dưới đây là thật.
//
// Ba test trong file này vẫn chỉ phủ nhánh KHÔNG kết nối được DB — đó là nhánh duy nhất chạy được
// mà không cần container (luật T9). Nhánh "kết nối được nhưng schema lệch" cần một database thật;
// HÔM NAY CHƯA CÓ TEST NÀO PHỦ NÓ, và chỗ của nó là một test mang
// [Trait("Category", "RequiresDocker")] chứ không phải file này.
public sealed class HealthCheckTests : IDisposable
{
    private readonly CoreWebApplicationFactory _factory = new(); // mặc định: chuỗi kết nối không tới được
    private readonly HttpClient _client;

    public HealthCheckTests()
        => _client = _factory.CreateHttpsClient();

    [Fact]
    public async Task Live_IsHealthy_RegardlessOfDatabase()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_IsUnhealthy_WhenDatabaseUnreachable()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Live_And_Ready_Differ_WhenDatabaseUnreachable()
    {
        var live = await _client.GetAsync("/health/live");
        var ready = await _client.GetAsync("/health/ready");

        live.StatusCode.ShouldNotBe(ready.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
