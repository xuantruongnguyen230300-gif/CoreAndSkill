using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Một host cho CẢ lớp test: khởi động host thật tốn vài giây, nên dựng lại mỗi test là phí. Trạng thái trong bộ nhớ được
// Reset() trước MỖI test — xUnit chạy tuần tự các test trong một lớp nên không có hai test dùng chung trạng thái cùng lúc.
//
// Bộ giới hạn tần suất toàn cục (200 lần/phút theo IP) dùng chung cho host, nên mỗi lớp test giữ số request dưới mức
// đó — đó là lý do B4 tách làm ba lớp thay vì một lớp lớn.
public sealed class InMemoryHostFixture : IAsyncLifetime
{
    internal InMemoryCoreHost Host { get; } = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await Host.DisposeAsync();
}

public abstract class B4EndpointTestBase
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected static readonly byte[] PdfBytes =
        System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF\n");

    private protected InMemoryCoreHost _host;

    private protected B4EndpointTestBase(InMemoryHostFixture fixture)
    {
        _host = fixture.Host;
        _host.Reset();
    }

    protected static async Task<ApiEnvelope<JsonElement>> ReadEnvelope(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(body, Json)!;
    }

    protected static MultipartFormDataContent Upload(byte[] bytes, string fileName, string? purpose = "ho-so")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        if (purpose is not null)
            form.Add(new StringContent(purpose), "purpose");
        return form;
    }

    protected static async Task<Guid> UploadOk(HttpClient client, byte[]? bytes = null)
    {
        var response = await client.PostAsync("/api/v1/core/files", Upload(bytes ?? PdfBytes, "quyet-dinh.pdf"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await ReadEnvelope(response)).Data.GetProperty("id").GetGuid();
    }
}
