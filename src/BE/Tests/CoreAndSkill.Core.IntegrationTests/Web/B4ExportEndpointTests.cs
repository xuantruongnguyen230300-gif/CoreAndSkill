using System.Net;
using System.Text.Json;
using CoreAndSkill.Core.Application.Audit;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Pha B4, tầng HTTP — docs/contracts/exports.md. Cùng giới hạn chứng minh với B4FilesEndpointTests. Đặc biệt: việc tệp
// xuất khớp với bộ lọc của danh sách (cùng một chỗ dựng truy vấn) KHÔNG được chứng minh ở đây — repository trong bộ nhớ
// không lọc; đó là ExportUsersDatabaseTests (RequiresDocker).
public sealed class B4ExportEndpointTests(InMemoryHostFixture fixture) : B4EndpointTestBase(fixture), IClassFixture<InMemoryHostFixture>
{
    // ---- Xuất dữ liệu -----------------------------------------------------------------------

    private static UserListItemDto NewUser(string userName, string fullName, string? email = null)
        => new(Guid.NewGuid(), userName, email, fullName, [new UserRoleSummaryDto(Guid.NewGuid(), "Quản trị", false)],
            false, null, false, false, new DateTimeOffset(2026, 9, 21, 1, 0, 0, TimeSpan.Zero), "v1");

    private HttpClient ExporterClient(Guid? id = null)
    {
        var exporter = id ?? Guid.NewGuid();
        _host.Permissions.Grant(exporter, CorePermissions.UserExport);
        return _host.CreateClient(exporter, "xuat.du.lieu");
    }

    [Fact]
    public async Task Export_Unauthenticated_Returns401()
    {
        using var client = _host.CreateClient(userId: null);

        (await client.GetAsync("/api/v1/core/users/export?format=csv")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ---- ADR-0062 / luật S20: GET xuất kiểm X-XSRF-TOKEN như lệnh ghi --------------------------
    //
    // Xuất là một GET có tác dụng phụ (một dòng nhật ký kiểm toán mang tên người gọi). Cookie SameSite=Lax vẫn đi theo điều
    // hướng cấp cao nhất xuyên site, và trình duyệt không gắn Origin cho điều hướng GET — nên một trang lạ dẫn người dùng
    // tới URL này là tạo được một dòng audit giả. Action mang [RequireAntiforgery]; AntiforgeryValidationMiddleware đọc dấu
    // đó trên endpoint (không đọc đường dẫn) và kiểm token bằng ValidateRequestAsync — IsRequestValidAsync trả true cho
    // mọi GET theo hợp đồng của chính nó, dùng nó ở đây là một cổng luôn xanh.

    [Fact]
    public async Task Export_WithoutCsrfToken_Returns403_CsrfRejected_AndAuditsNothing()
    {
        var exporter = Guid.NewGuid();
        _host.Permissions.Grant(exporter, CorePermissions.UserExport);
        _host.Users.Items.Add(NewUser("an.nv", "Nguyễn An"));
        using var client = _host.CreateClient(exporter, withCsrf: false);

        var response = await client.GetAsync("/api/v1/core/users/export?format=csv");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.CSRF_REJECTED");
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        _host.Audit.Entries.ShouldBeEmpty("request bị chặn trước khi vào handler — không có dòng nhật ký nào mang tên nạn nhân");
    }

    [Fact]
    public async Task Export_WithAWrongCsrfToken_Returns403_CsrfRejected()
    {
        using var client = ExporterClient();
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "khong-phai-token");

        var response = await client.GetAsync("/api/v1/core/users/export?format=csv");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.CSRF_REJECTED");
        _host.Audit.Entries.ShouldBeEmpty();
    }

    // Lớp 1 của §7.2 cũng áp: Origin ngoài allowlist bị chặn dù token đúng.
    [Fact]
    public async Task Export_WithAValidToken_ButAForeignOrigin_Returns403_OriginRejected()
    {
        using var client = ExporterClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/core/users/export?format=csv");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.ORIGIN_REJECTED");
    }

    [Fact]
    public async Task Export_WithTheCsrfToken_Returns200_AndExposesContentDispositionToTheBrowser()
    {
        _host.Users.Items.Add(NewUser("an.nv", "Nguyễn An"));
        using var client = ExporterClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/core/users/export?format=csv");
        request.Headers.Add("Origin", CoreWebApplicationFactory.TestAllowedOrigin); // nằm trong allowlist của host thử — FE thật gọi từ origin được phép

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        // FE tải blob qua HttpClient và đọc tên tệp từ Content-Disposition (ADR-0062 quyết định 4) — header phải được CORS
        // cho phép đọc, nếu không trình duyệt giấu nó khỏi script.
        response.Headers.GetValues("Access-Control-Expose-Headers").SelectMany(v => v.Split(','))
            .Select(h => h.Trim().ToLowerInvariant()).ShouldContain("content-disposition");
    }

    // Đối chứng cho cả nhóm: một GET ĐỌC không mang dấu vẫn đi qua không cần token — luật chung của §7.2 không đổi.
    [Fact]
    public async Task ListUsers_WithoutCsrfToken_StillReturns200()
    {
        var reader = Guid.NewGuid();
        _host.Permissions.Grant(reader, CorePermissions.UserRead);
        using var client = _host.CreateClient(reader, withCsrf: false);

        (await client.GetAsync("/api/v1/core/users")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Export_WithoutTheExportPermission_Returns403_EvenWithTheReadPermission_AndAuditsNothing()
    {
        var user = Guid.NewGuid();
        _host.Permissions.Grant(user, CorePermissions.UserRead);
        _host.Users.Items.Add(NewUser("an.nv", "Nguyễn An"));
        using var client = _host.CreateClient(user);

        var response = await client.GetAsync("/api/v1/core/users/export?format=csv");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.AUTH.FORBIDDEN");
        _host.Audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Export_Csv_ReturnsTheFileItself_AsAnAttachment_WithNoEnvelope_AndNoCache()
    {
        _host.Users.Items.Clear();
        _host.Users.Items.Add(NewUser("an.nv", "Nguyễn An", "an@vd.vn"));
        using var client = ExporterClient();

        var response = await client.GetAsync("/api/v1/core/users/export?format=csv");
        var text = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        response.Content.Headers.ContentDisposition.FileName.ShouldNotBeNull().ShouldStartWith("users-");
        response.Content.Headers.ContentDisposition.FileName.ShouldEndWith(".csv");
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
        text.ShouldContain("UserName");
        text.ShouldContain("an.nv");
        text.ShouldContain("Nguyễn An");
        text.ShouldNotContain("\"success\"", Case.Insensitive, "thân là tệp, không phải envelope");
    }

    [Fact]
    public async Task Export_Xlsx_ReturnsARealZipPackage_WithTheSpreadsheetContentType()
    {
        _host.Users.Items.Clear();
        _host.Users.Items.Add(NewUser("an.nv", "Nguyễn An"));
        using var client = ExporterClient();

        var response = await client.GetAsync("/api/v1/core/users/export?format=xlsx");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        bytes.Take(4).ShouldBe(new byte[] { 0x50, 0x4B, 0x03, 0x04 });
        response.Content.Headers.ContentDisposition!.FileName.ShouldEndWith(".xlsx");
    }

    [Fact]
    public async Task Export_AFormulaLookingCell_IsNeutralizedInTheFile()
    {
        _host.Users.Items.Clear();
        _host.Users.Items.Add(NewUser("an.nv", "=HYPERLINK(\"http://evil\",\"bấm\")"));
        _host.Users.Items.Add(NewUser("binh.tt", "+cmd|' /C calc'!A0"));
        _host.Users.Items.Add(NewUser("chi.lm", "@SUM(1+1)"));
        using var client = ExporterClient();

        var text = await (await client.GetAsync("/api/v1/core/users/export?format=csv")).Content.ReadAsStringAsync();

        text.ShouldContain("'=HYPERLINK");
        text.ShouldContain("'+cmd");
        text.ShouldContain("'@SUM");
        text.ShouldNotContain(",=HYPERLINK");
        text.ShouldNotContain(",+cmd");
        text.ShouldNotContain(",@SUM");
    }

    [Fact]
    public async Task Export_UnsupportedFormat_Returns400_ValidationFailed_AndAuditsNothing()
    {
        using var client = ExporterClient();

        var response = await client.GetAsync("/api/v1/core/users/export?format=pdf");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe("CORE.VALIDATION.FAILED");
        _host.Audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Export_WritesOneAuditRow_WithFilterAndRowCount_AndNoPersonalData()
    {
        _host.Users.Items.Clear();
        _host.Users.Items.Add(NewUser("an.nv", "Nguyễn An", "an@vd.vn"));
        _host.Users.Items.Add(NewUser("binh.tt", "Trần Bình", "binh@vd.vn"));
        using var client = ExporterClient();

        var response = await client.GetAsync("/api/v1/core/users/export?format=csv&searchText=nguyen&status=active");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var entry = _host.Audit.Entries.ShouldHaveSingleItem();
        entry.ActionCode.ShouldBe(AuditActions.UserExport);
        using var after = JsonDocument.Parse(entry.AfterValueJson!);
        after.RootElement.GetProperty("rowCount").GetInt32().ShouldBe(2);
        after.RootElement.GetProperty("searchText").GetString().ShouldBe("nguyen");
        after.RootElement.GetProperty("status").GetString().ShouldBe("active");
        after.RootElement.GetProperty("format").GetString().ShouldBe("csv");
        entry.AfterValueJson!.ShouldNotContain("an.nv");
        entry.AfterValueJson!.ShouldNotContain("an@vd.vn");
        entry.AfterValueJson!.ShouldNotContain("Nguyễn An");
    }

    [Fact]
    public async Task Export_OverTheRowLimit_Returns422_NamingBothNumbers_WritesNoFile_AndAuditsNothing()
    {
        await using var host = new InMemoryCoreHost(new Dictionary<string, string?> { ["Core:Export:MaxRows"] = "2" });
        for (var i = 0; i < 3; i++)
            host.Users.Items.Add(NewUser($"u{i}", $"Người {i}"));
        var exporter = Guid.NewGuid();
        host.Permissions.Grant(exporter, CorePermissions.UserExport);
        using var client = host.CreateClient(exporter);

        var response = await client.GetAsync("/api/v1/core/users/export?format=csv");
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe((HttpStatusCode)422);
        error.Code.ShouldBe("CORE.EXPORT.TOO_MANY_ROWS");
        error.MessageParams!["RowCount"].ShouldBe("3");
        error.MessageParams["MaxRows"].ShouldBe("2");
        response.Content.Headers.ContentDisposition.ShouldBeNull();
        host.Audit.Entries.ShouldBeEmpty("lần xuất bị từ chối không đưa dữ liệu nào ra ngoài — không có gì để ghi là 'đã xuất'");
    }

    [Fact]
    public async Task Export_AtExactlyTheRowLimit_Succeeds()
    {
        await using var host = new InMemoryCoreHost(new Dictionary<string, string?> { ["Core:Export:MaxRows"] = "2" });
        host.Users.Items.Add(NewUser("a", "A"));
        host.Users.Items.Add(NewUser("b", "B"));
        var exporter = Guid.NewGuid();
        host.Permissions.Grant(exporter, CorePermissions.UserExport);
        using var client = host.CreateClient(exporter);

        var response = await client.GetAsync("/api/v1/core/users/export?format=csv");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
