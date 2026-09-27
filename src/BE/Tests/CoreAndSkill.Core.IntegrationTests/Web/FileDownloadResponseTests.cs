using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Controllers;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// ADR-0068 — lối thứ năm `ApiControllerBase.HandleFile(Result<FileDownload>)`. Ba điều phải chứng minh, và điều thứ
// ba là thứ ADR nói thẳng là "một câu trong tài liệu" nếu không có test:
//   1. Thành công: ba header bắt buộc của một phản hồi tệp có mặt, do ExportFileResult đặt — action không đặt gì.
//   2. Thất bại: đi chung `Failure` với bốn lối kia, tức envelope, KHÔNG phải tệp.
//   3. Vòng đời luồng: FileDownload.Content do NGƯỜI NHẬN đóng, và người nhận nay là HandleFile — luồng phải đóng
//      cả khi ghi xong bình thường LẪN khi client ngắt giữa chừng.
//
// Endpoint thử nằm trong CHÍNH project test, cùng lý do với ClientAbortTests: không thêm endpoint sản phẩm chỉ để
// test, và luồng của FilesController thật là FileStream của kho tệp — không quan sát được lúc nó đóng.
public sealed class FileDownloadResponseTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CoreWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FileDownloadResponseTests()
    {
        _factory = new CoreWebApplicationFactory(configureServices: services =>
            services.AddControllers().AddApplicationPart(typeof(FileProbeController).Assembly));
        _client = _factory.CreateHttpsClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static readonly byte[] Content = Encoding.UTF8.GetBytes("nội dung tệp thử — ADR-0068\n");

    [Fact]
    public async Task Download_WritesTheFile_WithTheThreeMandatoryHeaders_AndClosesTheStream()
    {
        var id = FileProbeController.Register(Content);

        var response = await _client.GetAsync($"{FileProbeController.Path}?id={id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(Content);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        response.Content.Headers.ContentDisposition.FileNameStar.ShouldBe(FileProbeController.DownloadName);
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();

        // Luồng seekable thì độ dài biết trước — FileStreamResult của framework vẫn khai nó trước ADR-0068, và bỏ
        // đi trong im lặng là đổi hình dạng phản hồi (mất thanh tiến trình phía trình duyệt).
        response.Content.Headers.ContentLength.ShouldBe(Content.Length);

        FileProbeController.Stream(id).Disposed.ShouldBeTrue("HandleFile là NGƯỜI NHẬN luồng — nó phải đóng");
    }

    [Fact]
    public async Task Download_WhenTheClientIsAlreadyGone_StillClosesTheStream()
    {
        var id = FileProbeController.Register(Content);

        var response = await _client.GetAsync($"{FileProbeController.Path}?id={id}&aborted=true");

        (await response.Content.ReadAsByteArrayAsync()).ShouldBeEmpty("client đã đi — không có tệp nào tới nơi");
        FileProbeController.Stream(id).Disposed.ShouldBeTrue(
            "ngắt giữa chừng là đường rò luồng KHÔNG ai thấy — chỉ số bộ mô tả tệp mới nói, và lúc đó đã muộn");
    }

    [Fact]
    public async Task Download_OnFailure_ReturnsTheEnvelope_LikeEveryOtherEndpoint()
    {
        var response = await _client.GetAsync($"{FileProbeController.Path}/failure");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentDisposition.ShouldBeNull("nhánh lỗi không phải tệp");

        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(
            await response.Content.ReadAsStringAsync(), Json)!;
        envelope.Success.ShouldBeFalse();
        envelope.Error!.Code.ShouldBe("CORE.FILE.NOT_FOUND");
    }
}

// Lớp ở cấp namespace vì MVC chỉ nhận controller public KHÔNG lồng; chỉ vào host khi test gắn assembly này bằng
// AddApplicationPart. Kế thừa ApiControllerBase để gọi được HandleFile — đó chính là thứ đang được kiểm.
[ApiController]
[Route(Path)]
public sealed class FileProbeController : ApiControllerBase
{
    public const string Path = "api/v1/core/test/file-probe";

    // Tên có dấu và có đuôi: đường mã hoá filename* của ContentDispositionHeaderValue phải còn nguyên sau ADR-0068.
    public const string DownloadName = "ghi chú.txt";

    private static readonly ConcurrentDictionary<Guid, ProbeStream> Streams = new();

    public static Guid Register(byte[] content)
    {
        var id = Guid.NewGuid();
        Streams[id] = new ProbeStream(content);
        return id;
    }

    public static ProbeStream Stream(Guid id) => Streams[id];

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] Guid id, [FromQuery] bool aborted)
    {
        // RequestAborted của TestServer gán được — cách duy nhất mô phỏng "client đã đi" mà phía client vẫn đọc
        // được phản hồi (huỷ thật từ HttpClient thì chính client ném, không còn gì để so). Cùng thủ thuật với
        // AbortProbeController của ClientAbortTests.
        if (aborted)
            HttpContext.RequestAborted = new CancellationToken(canceled: true);

        return HandleFile(Result.Success(new FileDownload(Streams[id], "text/plain", DownloadName)));
    }

    [HttpGet("failure")]
    [AllowAnonymous]
    public IActionResult Failure() => HandleFile(Result.Failure<FileDownload>(FileErrors.NotFound));
}

// MemoryStream: seekable, độ dài biết trước — đúng hình dạng FileStream mà LocalFileStorage trả về.
public sealed class ProbeStream(byte[] content) : MemoryStream(content, writable: false)
{
    public bool Disposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}
