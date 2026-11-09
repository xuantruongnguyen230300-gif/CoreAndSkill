using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Infrastructure.Files;
using CoreAndSkill.Core.Web.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Files;

// Nợ B23 — ADR-0106 (IFileStorage là seam dự án thay được) và ADR-0107 (Core:File:RootPath chỉ kiểm khi bản lưu hiệu lực là
// LocalFileStorage của Core).
//
// Phải dựng host KHÔNG qua CoreWebApplicationFactory: cần một IFileStorage đăng ký TRƯỚC AddCore (đúng thứ tự dự án làm), mà
// configureServices của factory chạy SAU AddCore — đăng ký ở đó thắng bằng "đăng ký sau cùng", không chứng minh được dòng của
// Core có TryAdd hay không. Ở đây là đúng Program.cs thu nhỏ: một WebApplicationBuilder, dự án đăng ký, rồi AddCore.
//
// Phép kiểm lúc khởi động chạy bằng IStartupValidator — chính thứ ValidateOnStart gọi khi host lên — nên không cần mở host
// (host thật còn chạy phép kiểm migration E8 với database).
//
// Hai chỗ chặn thiếu khoá — attribute [Required] trên CoreFileOptions.RootPath và CoreFileOptionsValidator — PHẢI cùng đi theo
// điều kiện: chỉ dời validator thì ca "có bản giả, thiếu khoá" vẫn đỏ vì attribute.
//
// Điểm mù: không chạy host thật nên không chứng minh DI của cả ứng dụng dựng được với bản giả — việc đó là của test dùng
// CoreWebApplicationFactory. Và nhóm này chỉ kiểm bản giả cấp Singleton như dự án thật làm.
public sealed class FileStorageRegistrationTests : IDisposable
{
    private const string RootPathKey = "Core:File:RootPath";

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("coreandskill-b23-");

    public void Dispose() => _root.Delete(recursive: true);

    // Dự án thay bản cài: đăng ký TRƯỚC AddCore. `withFake: false` là Core tự thêm bản của nó.
    private ServiceProvider Host(bool withFake, IFileStorage? fake = null, Dictionary<string, string?>? config = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = Support.CoreWebApplicationFactory.UnreachableConnectionString,
            [Support.CoreWebApplicationFactory.AllowedOriginsKey + ":0"] = Support.CoreWebApplicationFactory.TestAllowedOrigin,
        });

        if (config is not null)
            builder.Configuration.AddInMemoryCollection(config);

        if (withFake)
            builder.Services.AddSingleton(fake ?? Substitute.For<IFileStorage>());

        builder.Services.AddCore(builder.Configuration, builder.Environment);

        // Chỉ dựng bộ chứa dịch vụ, không Build() host: không có dịch vụ nền nào chạy.
        return builder.Services.BuildServiceProvider();
    }

    private Dictionary<string, string?> ValidRoot() => new() { [RootPathKey] = _root.FullName };

    private static void Startup(IServiceProvider services) => services.GetRequiredService<IStartupValidator>().Validate();

    [Fact]
    public void AnIFileStorageRegisteredBeforeAddCore_IsTheOneEverywhereResolves()
    {
        var fake = Substitute.For<IFileStorage>();
        using var services = Host(withFake: true, fake, ValidRoot());

        services.GetRequiredService<IFileStorage>().ShouldBeSameAs(fake, "dòng đăng ký của Core không được đè bản của dự án");
        services.GetServices<IFileStorage>().ShouldHaveSingleItem().ShouldBeSameAs(fake, "không còn đăng ký nào của Core dưới nó");
    }

    [Fact]
    public void WithoutAnyRegistration_IFileStorageIsTheLocalFileStorageOfCore()
    {
        using var services = Host(withFake: false, config: ValidRoot());

        services.GetRequiredService<IFileStorage>().ShouldBeOfType<LocalFileStorage>();
    }

    [Fact]
    public void WithAFake_AMissingRootPath_DoesNotPreventStartup()
    {
        using var services = Host(withFake: true);

        Should.NotThrow(() => Startup(services), "bản lưu của dự án không đọc Core:File:RootPath — Core không được đòi nó");
        Should.NotThrow(() => _ = services.GetRequiredService<IOptions<CoreFileOptions>>().Value);
    }

    [Fact]
    public void WithoutAFake_AMissingRootPath_PreventsStartup_AndNamesTheKey()
    {
        using var services = Host(withFake: false);

        var exception = Should.Throw<OptionsValidationException>(() => Startup(services));

        exception.Message.ShouldContain("RootPath", Case.Sensitive);
    }

    // ADR-0107: "thiếu khoá lẫn thư mục hợp lệ" — cùng điều kiện.
    [Fact]
    public void WithAFake_ARelativeRootPath_DoesNotPreventStartup()
    {
        using var services = Host(withFake: true, config: new() { [RootPathKey] = "thu-muc/tuong-doi" });

        Should.NotThrow(() => Startup(services));
    }

    [Fact]
    public void WithoutAFake_ARelativeRootPath_PreventsStartup_AndNamesTheKey()
    {
        using var services = Host(withFake: false, config: new() { [RootPathKey] = "thu-muc/tuong-doi" });

        var exception = Should.Throw<OptionsValidationException>(() => Startup(services));

        exception.Message.ShouldContain(RootPathKey, Case.Sensitive);
    }

    // ADR-0107 ràng buộc 2: các khoá còn lại của Core:File:* giữ ValidateOnStart vô điều kiện — mọi bản lưu đều dùng chúng.
    // Đối chứng cho phép sửa nới quá tay (bỏ cả ValidateOnStart của CoreFileOptions khi có bản giả).
    [Fact]
    public void WithAFake_AnInvalidOtherFileKey_StillPreventsStartup_AndNamesTheKey()
    {
        var config = ValidRoot();
        config["Core:File:MaxUploadMb"] = "0";
        using var services = Host(withFake: true, config: config);

        var exception = Should.Throw<OptionsValidationException>(() => Startup(services));

        exception.Message.ShouldContain("MaxUploadMb", Case.Sensitive);
    }
}
