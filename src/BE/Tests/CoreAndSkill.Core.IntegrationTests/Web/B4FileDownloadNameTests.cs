using System.IO.Compression;
using System.Net;
using System.Text;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// ADR-0050, RULES.md S16, docs/contracts/files.md §2 — tên tải về là tên gốc với ĐUÔI THEO KIỂU
// HỆ THỐNG ĐÃ XÁC ĐỊNH, không phải đuôi người tải lên đặt. Không vậy thì một văn bản thuần tên "x.bat"/"x.hta"/"x.html" lọt
// qua bước nhận diện (nó đúng là văn bản thuần) rồi tới máy người khác với đúng đuôi đó; một .docm được xếp vào Docx vẫn về
// máy dưới tên .docm.
public sealed class B4FileDownloadNameTests(InMemoryHostFixture fixture) : B4EndpointTestBase(fixture), IClassFixture<InMemoryHostFixture>
{
    private async Task<HttpResponseMessage> UploadThenDownload(byte[] bytes, string clientName)
    {
        using var client = _host.CreateClient(Guid.NewGuid());
        var upload = await client.PostAsync("/api/v1/core/files", Upload(bytes, clientName, purpose: "ghi-chu"));
        upload.StatusCode.ShouldBe(HttpStatusCode.OK);
        var id = (await ReadEnvelope(upload)).Data.GetProperty("id").GetGuid();

        var download = await client.GetAsync($"/api/v1/core/files/{id}");
        download.StatusCode.ShouldBe(HttpStatusCode.OK);
        return download;
    }

    private static readonly byte[] Text = Encoding.UTF8.GetBytes("@echo off\r\necho xin chao\r\n");

    private static byte[] MinimalWordPackage()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            zip.CreateEntry("[Content_Types].xml");
            zip.CreateEntry("word/document.xml");
            zip.CreateEntry("word/vbaProject.bin");
        }

        return buffer.ToArray();
    }

    [Theory]
    [InlineData("x.bat", "x.txt")]
    [InlineData("x.hta", "x.txt")]
    [InlineData("trang.html", "trang.txt")]
    [InlineData("ghichu", "ghichu.txt")]
    [InlineData("a.b.cmd", "a.b.txt")]
    public async Task Download_FileName_UsesExtensionOfDetectedType(string clientName, string expected)
    {
        var response = await UploadThenDownload(Text, clientName);

        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");
        response.Content.Headers.ContentDisposition!.FileNameStar.ShouldBe(expected);
    }

    [Fact]
    public async Task AMacroEnabledWordFile_DetectedAsDocx_IsDownloadedAsDocx_NotDocm()
    {
        var response = await UploadThenDownload(MinimalWordPackage(), "hop-dong.docm");

        response.Content.Headers.ContentDisposition!.FileNameStar.ShouldBe("hop-dong.docx");
    }

    [Fact]
    public async Task AVietnameseName_KeepsItsDiacritics_AndIsEncodedAsFileNameStar()
    {
        var response = await UploadThenDownload(Text, "Quyết định số 12.bat");

        response.Content.Headers.ContentDisposition!.FileNameStar.ShouldBe("Quyết định số 12.txt");
        var raw = response.Content.Headers.GetValues("Content-Disposition").Single();
        raw.ShouldContain("filename*=UTF-8''", Case.Insensitive);
        raw.ShouldNotContain(".bat");
    }

    [Fact]
    public async Task TheUploadResponse_StillEchoesTheSanitizedClientName_OnlyTheDownloadNameIsRewritten()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        var upload = await client.PostAsync("/api/v1/core/files", Upload(Text, "x.bat", purpose: "ghi-chu"));

        (await ReadEnvelope(upload)).Data.GetProperty("originalName").GetString().ShouldBe("x.bat");
    }
}
