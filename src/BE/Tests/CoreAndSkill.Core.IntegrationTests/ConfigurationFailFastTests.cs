using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests;

// Luật A8 + docs/quy-uoc/be-architecture.md §4 — xoá/rỗng ConnectionStrings:Core thì tiến trình
// không khởi động, và thông báo phải nêu tên khoá thiếu.
public class ConfigurationFailFastTests
{
    [Fact]
    public void MissingConnectionString_PreventsStartup_AndNamesTheMissingKey()
    {
        using var factory = new CoreWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = "",
        });

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        CollectMessages(exception).ShouldContain(m => m.Contains("Core", StringComparison.Ordinal));
    }

    // Pha B4 (be-architecture.md §4.3, 14-file-storage.md §2): mỗi khoá cấu hình mới chặn tiến trình khởi động khi sai,
    // và thông báo nêu tên khoá — chứ không đợi tới lần tải tệp / lần nhập đầu tiên mới lộ ra.
    [Theory]
    [InlineData("Core:File:RootPath", "", "RootPath")]
    [InlineData("Core:File:RootPath", "thu-muc/tuong-doi", "Core:File:RootPath")]
    [InlineData("Core:File:RootPath", "Z:\\khong\\ton\\tai\\coreandskill", "Core:File:RootPath")]
    [InlineData("Core:File:MaxUploadMb", "0", "MaxUploadMb")]
    [InlineData("Core:File:UnattachedRetentionHours", "0", "UnattachedRetentionHours")]
    [InlineData("Core:Import:MaxRows", "0", "MaxRows")]
    [InlineData("Core:Export:MaxRows", "0", "MaxRows")]
    [InlineData("Core:Jobs:MaxConcurrent", "0", "MaxConcurrent")]
    [InlineData("Core:Outbox:MaxAttempts", "0", "MaxAttempts")]
    [InlineData("Core:Notification:DefaultLanguage", "Tiếng Việt", "DefaultLanguage")]
    // Luật S19 (ADR-0058, 02-identity-auth.md §4.2): sàn phải > 0 (0 là tắt sàn trong im lặng) và không vượt 5000.
    [InlineData("Core:Identity:Login:FailureFloorMs", "0", "FailureFloorMs")]
    [InlineData("Core:Identity:Login:FailureFloorMs", "5001", "FailureFloorMs")]
    // Luật E8 — thời gian chờ database lúc khởi động (DatabaseSchemaStartupCheck).
    [InlineData("Core:SchemaCheck:ConnectWaitSeconds", "-1", "ConnectWaitSeconds")]
    [InlineData("Core:SchemaCheck:ConnectRetryIntervalSeconds", "0", "ConnectRetryIntervalSeconds")]
    public void AnInvalidB4Key_PreventsStartup_AndNamesTheKey(string key, string value, string expectedInMessage)
    {
        using var factory = new CoreWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString,
            [key] = value,
        });

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        CollectMessages(exception).ShouldContain(m => m.Contains(expectedInMessage, StringComparison.Ordinal));
    }

    // docs/quy-uoc/repo-artifact.md §6.2, be-architecture.md §4.2 — tệp appsettings.json gốc đi theo artifact lên máy chủ nên
    // KHÔNG mang origin của máy dev. Nếu nó mang, một môi trường thật quên khai Core:Auth:AllowedOrigins vẫn khởi động — với
    // allowlist của máy dev — và [MinLength(1)] không bao giờ bắn. Test khai khoá với giá trị null để factory không chen
    // origin thử vào: giá trị còn lại là của đúng hai tệp appsettings, môi trường Production.
    [Fact]
    public void OutsideDevelopment_WithoutAllowedOrigins_PreventsStartup_AndNamesTheKey()
    {
        using var factory = new CoreWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString,
            [CoreWebApplicationFactory.AllowedOriginsKey] = null,
        });

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        CollectMessages(exception).ShouldContain(m => m.Contains("AllowedOrigins", StringComparison.Ordinal));
    }

    // Đối chứng: môi trường Development đọc origin từ appsettings.Development.json và khởi động được — giá trị đã CHUYỂN chỗ,
    // không mất. Máy dev vẫn chạy mà không phải khai gì thêm (repo-artifact.md §6.3).
    [Fact]
    public void InDevelopment_AllowedOrigins_ComeFromTheDevelopmentFile()
    {
        using var factory = new CoreWebApplicationFactory(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString,
                [CoreWebApplicationFactory.AllowedOriginsKey] = null,
            },
            environment: "Development");

        factory.Services.GetRequiredService<IOptions<CoreAuthOptions>>().Value.AllowedOrigins
            .ShouldBe(["https://localhost:4200"]);
    }

    // docs/quy-uoc/be-api-controller.md §7.5 — hai cookie hệ thống không được trùng tên. Trùng thì cookie antiforgery ghi
    // đè cookie phiên (hay ngược lại) và người dùng bị đẩy về màn đăng nhập vô tận. So không phân biệt hoa thường.
    [Theory]
    [InlineData("coreandskill.session")]
    [InlineData("COREANDSKILL.SESSION")]
    public void AntiforgeryCookieNameEqualToSessionCookieName_PreventsStartup_AndNamesBothKeys(string antiforgeryName)
    {
        using var factory = new CoreWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString,
            ["Core:Auth:CookieName"] = "coreandskill.session",
            ["Core:Auth:AntiforgeryCookieName"] = antiforgeryName,
        });

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        var messages = CollectMessages(exception).ToList();
        messages.ShouldContain(m => m.Contains("Core:Auth:CookieName", StringComparison.Ordinal)
                                    && m.Contains("Core:Auth:AntiforgeryCookieName", StringComparison.Ordinal));
    }

    // Đối chứng: hai tên khác nhau thì khởi động bình thường.
    [Fact]
    public void DistinctSessionAndAntiforgeryCookieNames_StartNormally()
    {
        using var factory = new CoreWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString,
            ["Core:Auth:CookieName"] = "coreandskill.session",
            ["Core:Auth:AntiforgeryCookieName"] = "coreandskill.antiforgery",
        });

        Should.NotThrow(() => factory.CreateClient().Dispose());
    }

    [Fact]
    public void ARootPathInsideTheApplicationDirectory_PreventsStartup()
    {
        using var factory = new CoreWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString,
            ["Core:File:RootPath"] = AppContext.BaseDirectory,
        });

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        CollectMessages(exception).ShouldContain(m => m.Contains("Core:File:RootPath", StringComparison.Ordinal));
    }

    // ADR-0050 (docs/adr/0050-v1-chua-quet-ma-doc-siet-zip-va-duoi-tai-xuong.md): một module khai purpose nhận
    // application/zip thì tiến trình KHÔNG lên — lộ ra lúc triển khai, không ở lần tải tệp đầu tiên.
    [Fact]
    public void AModulePurposeAcceptingZip_PreventsStartup_AndNamesThePurpose()
    {
        using var factory = new CoreWebApplicationFactory(
            new Dictionary<string, string?> { ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString },
            services => services.AddSingleton<IFilePurposeSource>(new ZipPurpose()));

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        CollectMessages(exception).ShouldContain(m => m.Contains("nen-tai-lieu", StringComparison.Ordinal));
    }

    private sealed class ZipPurpose : IFilePurposeSource
    {
        public IReadOnlyCollection<FilePurposeDefinition> GetPurposes()
            => [new("nen-tai-lieu", [FileContentDetector.Pdf, FileContentDetector.Zip])];
    }

    private static IEnumerable<string> CollectMessages(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            yield return current.Message;
    }
}
