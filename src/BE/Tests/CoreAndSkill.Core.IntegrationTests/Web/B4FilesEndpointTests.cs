using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Pha B4, tầng HTTP — docs/contracts/files.md. Chạy trên pipeline THẬT của Core (định tuyến, xác thực, antiforgery,
// phân quyền, ràng buộc kích thước, envelope, MediatR, validator, kho tệp thật trên thư mục tạm) với các seam chạm DB
// thay bằng bản trong bộ nhớ (Support/InMemoryCoreHost.cs).
//
// Thứ chúng KHÔNG chứng minh — và không được đọc như đã chứng minh: SQL của repository, bộ lọc đơn vị và xoá mềm ở
// tầng dữ liệu, giao dịch. Đó là việc của FilesDatabaseTests (RequiresDocker), chưa chạy được ở môi trường không có Docker.
public sealed class B4FilesEndpointTests(InMemoryHostFixture fixture) : B4EndpointTestBase(fixture), IClassFixture<InMemoryHostFixture>
{
    // ---- Tệp: đường vào -----------------------------------------------------------------------

    [Fact]
    public async Task Upload_Unauthenticated_Returns401_AndStoresNothing()
    {
        using var client = _host.CreateClient(userId: null);

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfBytes, "a.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.NOT_AUTHENTICATED");
        _host.Files.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Upload_WithoutAntiforgeryToken_Returns403_CsrfRejected_AndStoresNothing()
    {
        using var client = _host.CreateClient(Guid.NewGuid(), withCsrf: false);

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfBytes, "a.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.CSRF_REJECTED");
        _host.Files.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Upload_DisallowedOrigin_Returns403_OriginRejected()
    {
        using var client = _host.CreateClient(Guid.NewGuid());
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/core/files") { Content = Upload(PdfBytes, "a.pdf") };
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.ORIGIN_REJECTED");
    }

    [Fact]
    public async Task Upload_Pdf_Returns200_WithTheFourContractFields_AndNothingElse()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfBytes, "quyet-dinh-12.pdf"));
        var envelope = await ReadEnvelope(response);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var names = envelope.Data.EnumerateObject().Select(p => p.Name).ToArray();
        names.ShouldBe(["id", "originalName", "contentType", "sizeBytes"], ignoreOrder: true,
            "khoá kho lưu, owner_table, owner_id không đi trên dây (contracts/files.md §1)");
        envelope.Data.GetProperty("originalName").GetString().ShouldBe("quyet-dinh-12.pdf");
        envelope.Data.GetProperty("contentType").GetString().ShouldBe("application/pdf");
        envelope.Data.GetProperty("sizeBytes").GetInt64().ShouldBe(PdfBytes.Length);
    }

    [Fact]
    public async Task Upload_TheBytesLandUnderTheConfiguredRoot_UnderASystemGeneratedName_NeverTheClientName()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var id = await UploadOk(client);

        var stored = _host.Files.Items[id];
        stored.StorageKey.ShouldNotContain("quyet-dinh", Case.Insensitive);
        stored.StorageKey.ShouldStartWith("ho-so/");
        Path.IsPathRooted(stored.StorageKey).ShouldBeFalse("khoá kho lưu là khoá tương đối, không phải đường dẫn tuyệt đối");
        File.Exists(Path.Combine(_host.Factory.FileRootPath, stored.StorageKey.Replace('/', Path.DirectorySeparatorChar))).ShouldBeTrue();
    }

    [Fact]
    public async Task Upload_ATraversalFileName_DoesNotChangeWhereTheBytesLand()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfBytes, "..\\..\\..\\windows\\system32\\evil.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var id = (await ReadEnvelope(response)).Data.GetProperty("id").GetGuid();
        var stored = _host.Files.Items[id];
        stored.StorageKey.ShouldStartWith("ho-so/");
        stored.StorageKey.ShouldNotContain("..");
        stored.OriginalName.ShouldNotContain("\\");
        stored.OriginalName.ShouldNotContain("/");
    }

    [Fact]
    public async Task Upload_TextContentNamedDotPdf_IsRejected_TheExtensionIsNoEvidence()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload(Encoding.ASCII.GetBytes("day khong phai pdf"), "gia-mao.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.FILE.TYPE_NOT_ALLOWED");
        _host.Files.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Upload_AnExecutableRenamedToPdf_IsRejected()
    {
        using var client = _host.CreateClient(Guid.NewGuid());
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };

        var response = await client.PostAsync("/api/v1/core/files", Upload(exe, "bao-cao.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.FILE.TYPE_NOT_ALLOWED");
    }

    [Fact]
    public async Task Upload_MissingPurpose_Returns400_ValidationFailed()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfBytes, "a.pdf", purpose: null));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.VALIDATION.FAILED");
    }

    [Fact]
    public async Task Upload_AnUnknownPurpose_Returns400_ValidationFailed()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfBytes, "a.pdf", purpose: "khong-ton-tai"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.VALIDATION.FAILED");
        _host.Files.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Upload_AnEmptyFile_Returns400_ValidationFailed()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload([], "trong.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.VALIDATION.FAILED");
    }

    // ---- Tệp: giới hạn dung lượng -----------------------------------------------------------

    private static byte[] PdfOfSize(int bytes)
    {
        var data = new byte[bytes];
        Array.Fill(data, (byte)'x');
        "%PDF-1.4\n"u8.CopyTo(data);
        return data;
    }

    [Fact]
    public async Task Upload_FarOverTheLimit_IsRefusedByContentLength_BeforeTheBodyIsRead_WithTooLarge()
    {
        await using var host = new InMemoryCoreHost(new Dictionary<string, string?> { ["Core:File:MaxUploadMb"] = "1" });
        using var client = host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfOfSize(3 * 1024 * 1024), "lon.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await ReadEnvelope(response)).Error!;
        error.Code.ShouldBe("CORE.FILE.TOO_LARGE");
        error.MessageParams!["MaxBytes"].ShouldBe((1024 * 1024).ToString(System.Globalization.CultureInfo.InvariantCulture));
        host.Files.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Upload_JustOverTheLimit_StillInsideTheMultipartOverhead_IsRefusedByTheHandler_WithTheSameCode()
    {
        await using var host = new InMemoryCoreHost(new Dictionary<string, string?> { ["Core:File:MaxUploadMb"] = "1" });
        using var client = host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfOfSize((1024 * 1024) + 100), "sat-tran.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.FILE.TOO_LARGE");
        host.Files.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Upload_ABodyThatIsNotMultipart_Returns400_AsAnEnvelope_NotAFrameworkProblemDocument()
    {
        using var client = _host.CreateClient(Guid.NewGuid());
        using var content = new StringContent("{\"purpose\":\"ho-so\"}", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/core/files", content);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        body.ShouldNotContain("tools.ietf.org");
        body.ShouldContain("\"traceId\"");
    }

    [Fact]
    public async Task ModelBinding_AQueryValueOfTheWrongType_Returns400_ValidationFailed_WithTheFieldNamed_AndNoFrameworkMessage()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var response = await client.GetAsync("/api/v1/core/notifications?page=abc");
        var body = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(body, Json)!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        envelope.Error!.Code.ShouldBe("CORE.VALIDATION.FAILED");
        envelope.Error.FieldErrors!["Page"].Single().Code.ShouldBe("CORE.VALIDATION.FORMAT");
        body.ShouldNotContain("could not be converted", Case.Insensitive);
        body.ShouldNotContain("tools.ietf.org");
    }

    [Fact]
    public async Task Upload_AtTheLimit_IsAccepted()
    {
        await using var host = new InMemoryCoreHost(new Dictionary<string, string?> { ["Core:File:MaxUploadMb"] = "1" });
        using var client = host.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/core/files", Upload(PdfOfSize(1024 * 1024), "vua-du.pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ---- Tệp: tải xuống và quyền theo bản ghi chủ -------------------------------------------

    [Fact]
    public async Task Download_Unauthenticated_Returns401()
    {
        using var client = _host.CreateClient(userId: null);

        var response = await client.GetAsync($"/api/v1/core/files/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.NOT_AUTHENTICATED");
    }

    [Fact]
    public async Task Download_ByTheUploader_WhileUnattached_ReturnsTheExactBytes_AsAnAttachment_NeverCached()
    {
        var uploader = Guid.NewGuid();
        using var client = _host.CreateClient(uploader);
        var id = await UploadOk(client);

        var response = await client.GetAsync($"/api/v1/core/files/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(PdfBytes);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        response.Content.Headers.ContentDisposition.FileNameStar.ShouldBe("quyet-dinh.pdf");
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Download_ByAnotherUser_WhileUnattached_Returns404_NotFound_Never403()
    {
        using var uploader = _host.CreateClient(Guid.NewGuid(), "nguoi.tai");
        var id = await UploadOk(uploader);
        using var other = _host.CreateClient(Guid.NewGuid(), "nguoi.la");

        var response = await other.GetAsync($"/api/v1/core/files/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.FILE.NOT_FOUND");
    }

    [Fact]
    public async Task Download_ADeniedFile_AndANonexistentOne_AreIndistinguishable_OnTheWire()
    {
        using var uploader = _host.CreateClient(Guid.NewGuid(), "nguoi.tai");
        var id = await UploadOk(uploader);
        using var other = _host.CreateClient(Guid.NewGuid(), "nguoi.la");

        var denied = await other.GetAsync($"/api/v1/core/files/{id}");
        var missing = await other.GetAsync($"/api/v1/core/files/{Guid.NewGuid()}");

        denied.StatusCode.ShouldBe(missing.StatusCode);
        var deniedError = (await ReadEnvelope(denied)).Error!;
        var missingError = (await ReadEnvelope(missing)).Error!;
        deniedError.Code.ShouldBe(missingError.Code);
        deniedError.Message.ShouldBe(missingError.Message, "hai ca phải không phân biệt được — 403 hay lệch thông điệp đều xác nhận tệp có tồn tại (M7)");
    }

    [Fact]
    public async Task Download_WhenAttached_FollowsThePermissionOfTheOwnerRecord()
    {
        var uploaderId = Guid.NewGuid();
        var reader = Guid.NewGuid();
        using var uploader = _host.CreateClient(uploaderId, "nguoi.tai");
        var id = await UploadOk(uploader);
        var ownerId = Guid.NewGuid();
        _host.Files.Items[id].AttachTo(InMemoryCoreHost.OwnerTable, ownerId).IsSuccess.ShouldBeTrue();
        _host.OwnerChecker.AllowRead(reader, ownerId);

        using var readerClient = _host.CreateClient(reader, "nguoi.doc");
        using var stranger = _host.CreateClient(Guid.NewGuid(), "nguoi.la");

        (await readerClient.GetAsync($"/api/v1/core/files/{id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await stranger.GetAsync($"/api/v1/core/files/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        // Sau khi gắn, quyền là của bản ghi chủ — người tải lên KHÔNG còn được đọc chỉ vì đã tải lên.
        (await uploader.GetAsync($"/api/v1/core/files/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_AFileAttachedToATableWithoutAChecker_IsDeniedToEveryone()
    {
        var uploaderId = Guid.NewGuid();
        using var uploader = _host.CreateClient(uploaderId, "nguoi.tai");
        var id = await UploadOk(uploader);
        _host.Files.Items[id].AttachTo("khong.co.checker", Guid.NewGuid()).IsSuccess.ShouldBeTrue();

        var response = await uploader.GetAsync($"/api/v1/core/files/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_WhenTheContentIsGoneFromTheStore_IsAnErrorEnvelope_NotASilentEmptyFile()
    {
        using var client = _host.CreateClient(Guid.NewGuid());
        var id = await UploadOk(client);
        var physical = Path.Combine(_host.Factory.FileRootPath, _host.Files.Items[id].StorageKey.Replace('/', Path.DirectorySeparatorChar));
        File.Delete(physical);

        var response = await client.GetAsync($"/api/v1/core/files/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.FILE.CONTENT_MISSING");
    }

    [Fact]
    public async Task Download_IsNeverServedByAStaticPath_TheStorageKeyUnderTheHostRootIsNotReachable()
    {
        using var client = _host.CreateClient(Guid.NewGuid());
        var id = await UploadOk(client);
        var key = _host.Files.Items[id].StorageKey;
        using var anonymous = _host.CreateClient(userId: null);

        foreach (var url in new[] { $"/{key}", $"/files/{key}", $"/static/{key}", $"/api/v1/core/files/{key}" })
        {
            var response = await anonymous.GetAsync(url);
            response.StatusCode.ShouldNotBe(HttpStatusCode.OK, $"{url} không được phục vụ tệp");
        }
    }

    // ---- Tệp: gỡ ----------------------------------------------------------------------------

    [Fact]
    public async Task Delete_WithoutAntiforgeryToken_Returns403()
    {
        using var client = _host.CreateClient(Guid.NewGuid(), withCsrf: false);

        var response = await client.DeleteAsync($"/api/v1/core/files/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.CSRF_REJECTED");
    }

    [Fact]
    public async Task Delete_ByTheUploader_UnlinksTheFile_ThenDownloadIsNotFound_ButTheBytesAreLeftForTheReconciliationJob()
    {
        using var client = _host.CreateClient(Guid.NewGuid());
        var id = await UploadOk(client);
        var physical = Path.Combine(_host.Factory.FileRootPath, _host.Files.Items[id].StorageKey.Replace('/', Path.DirectorySeparatorChar));

        var delete = await client.DeleteAsync($"/api/v1/core/files/{id}");
        var afterwards = await client.GetAsync($"/api/v1/core/files/{id}");

        delete.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterwards.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        File.Exists(physical).ShouldBeTrue("tệp vật lý không xoá trong request — job đối soát dọn sau khoảng an toàn (§5.2)");
    }

    [Fact]
    public async Task Delete_ByAnotherUser_Returns404_AndLeavesTheFileLinked()
    {
        using var uploader = _host.CreateClient(Guid.NewGuid(), "nguoi.tai");
        var id = await UploadOk(uploader);
        using var other = _host.CreateClient(Guid.NewGuid(), "nguoi.la");

        var response = await other.DeleteAsync($"/api/v1/core/files/{id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _host.Files.Items[id].IsDeleted.ShouldBeFalse();
    }

}
