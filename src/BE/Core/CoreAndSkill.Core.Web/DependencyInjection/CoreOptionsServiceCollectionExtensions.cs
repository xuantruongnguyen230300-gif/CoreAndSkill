using CoreAndSkill.Core.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAndSkill.Core.Web.DependencyInjection;

public static class CoreOptionsServiceCollectionExtensions
{
    // Cấu hình fail-fast — docs/quy-uoc/be-architecture.md §4. Thiếu ConnectionStrings:Core thì
    // ValidateOnStart() chặn tiến trình khởi động, không đợi tới lúc chạm tính năng.
    public static IServiceCollection AddCoreOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CoreConnectionOptions>()
            .Bind(configuration.GetSection(CoreConnectionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // be-api-controller.md §7.5 — hai cookie hệ thống không được trùng tên: trùng thì cookie này ghi đè cookie kia
        // và người dùng bị đẩy về màn đăng nhập vô tận. So không phân biệt hoa thường — chặt hơn trình duyệt, cố ý.
        services.AddOptions<CoreAuthOptions>()
            .Bind(configuration.GetSection(CoreAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.CookieName is null || options.AntiforgeryCookieName is null
                           || !string.Equals(options.CookieName, options.AntiforgeryCookieName, StringComparison.OrdinalIgnoreCase),
                $"{CoreAuthOptions.SectionName}:{nameof(CoreAuthOptions.CookieName)} và " +
                $"{CoreAuthOptions.SectionName}:{nameof(CoreAuthOptions.AntiforgeryCookieName)} không được trùng nhau " +
                "(không phân biệt hoa thường) — docs/quy-uoc/be-api-controller.md §7.5.")
            .ValidateOnStart();

        // Luật S9 — Core:Identity:Password chỉ được khai ở appsettings.json (không theo môi trường).
        services.AddOptions<CoreIdentityPasswordOptions>()
            .Bind(configuration.GetSection(CoreIdentityPasswordOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CoreIdentityLockoutOptions>()
            .Bind(configuration.GetSection(CoreIdentityLockoutOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Luật S19 — sàn thời gian của phản hồi đăng nhập trượt (ADR-0058); 0 là tắt sàn trong im lặng nên bị chặn lúc khởi động.
        services.AddOptions<CoreIdentityLoginOptions>()
            .Bind(configuration.GetSection(CoreIdentityLoginOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CoreBackgroundJobOptions>()
            .Bind(configuration.GetSection(CoreBackgroundJobOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Pha B4 — mỗi nhóm khoá một lớp Options, ValidateOnStart ở MỌI môi trường (be-architecture.md §4.3).
        // Core:File:RootPath không có mặc định: thiếu thì tiến trình không khởi động, nêu tên khoá.
        services.AddOptions<CoreFileOptions>()
            .Bind(configuration.GetSection(CoreFileOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CoreExportOptions>()
            .Bind(configuration.GetSection(CoreExportOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CoreImportOptions>()
            .Bind(configuration.GetSection(CoreImportOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CoreJobOptions>()
            .Bind(configuration.GetSection(CoreJobOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CoreOutboxOptions>()
            .Bind(configuration.GetSection(CoreOutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CoreNotificationOptions>()
            .Bind(configuration.GetSection(CoreNotificationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Luật E8 — thời gian chờ database lúc khởi động (DatabaseSchemaStartupCheck). Có mặc định; khai sai dạng thì không
        // khởi động.
        services.AddOptions<CoreSchemaCheckOptions>()
            .Bind(configuration.GetSection(CoreSchemaCheckOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Proxy tin cậy — be-architecture.md §3.1 ràng buộc 4. Không khai gì là hợp lệ (không tin ai); khai sai
        // dạng thì không khởi động.
        services.AddOptions<CoreNetworkOptions>()
            .Bind(configuration.GetSection(CoreNetworkOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Ngoại lệ có tên duy nhất — KHÔNG ValidateOnStart (be-architecture.md §4.4). Tiến trình API
        // phục vụ thật không đọc nhóm này; kiểm ở đầu lệnh `core bootstrap`, trước dòng ghi đầu tiên.
        services.AddOptions<CoreBootstrapOptions>()
            .Bind(configuration.GetSection(CoreBootstrapOptions.SectionName));

        return services;
    }
}
