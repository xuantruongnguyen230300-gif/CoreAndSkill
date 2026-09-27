using CoreAndSkill.Core.Application;
using CoreAndSkill.Core.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreAndSkill.Core.Web.DependencyInjection;

public static class CoreServiceCollectionExtensions
{
    // Bề mặt lắp ghép duy nhất của Core, gọi TRƯỚC WebApplicationBuilder.Build() —
    // docs/quy-uoc/be-architecture.md §5.1, §3.
    public static IServiceCollection AddCore(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddCoreOptions(configuration);
        services.AddCoreApplication();
        services.AddCorePersistence();
        services.AddCoreIdentity();
        services.AddCoreBackgroundJobs();
        services.AddCoreFilesAndMessaging();
        services.AddCoreWeb(environment);

        return services;
    }
}
