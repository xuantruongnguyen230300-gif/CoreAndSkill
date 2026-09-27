using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAndSkill.Core.Application;

// Ghi nhận assembly handler/validator của một module trước AddCoreApplication() — docs/quy-uoc/be-architecture.md
// §5.1. AddXModule() của module gọi RegisterModuleAssembly TRƯỚC AddCore(); AddCoreApplication() gom
// danh sách rồi NIÊM lại — gọi muộn (sau khi đã gom) ném ngoại lệ nêu tên assembly.
internal sealed record ModuleAssemblyRegistration(Assembly Assembly);

internal sealed class ModuleAssembliesSealedMarker;

public static class ModuleAssemblyRegistrationExtensions
{
    public static IServiceCollection RegisterModuleAssembly(this IServiceCollection services, Assembly assembly)
    {
        if (services.Any(d => d.ServiceType == typeof(ModuleAssembliesSealedMarker)))
            throw new InvalidOperationException(
                $"Assembly module '{assembly.GetName().Name}' ghi nhận SAU khi AddCoreApplication() đã niêm danh sách. " +
                "Gọi RegisterModuleAssembly() trong AddXModule(), TRƯỚC AddCore() (be-architecture.md §5.1, §3).");

        services.AddSingleton(new ModuleAssemblyRegistration(assembly));
        return services;
    }

    internal static IReadOnlyList<Assembly> SealModuleAssemblies(this IServiceCollection services)
    {
        var assemblies = services
            .Where(d => d.ServiceType == typeof(ModuleAssemblyRegistration))
            .Select(d => ((ModuleAssemblyRegistration)d.ImplementationInstance!).Assembly)
            .ToArray();

        services.AddSingleton<ModuleAssembliesSealedMarker>();

        return assemblies;
    }
}
