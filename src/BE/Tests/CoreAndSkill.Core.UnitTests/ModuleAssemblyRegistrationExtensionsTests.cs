using System.Reflection;
using CoreAndSkill.Core.Application;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests;

// docs/quy-uoc/be-architecture.md §5.1 — ghi nhận TRƯỚC AddCoreApplication(); gọi muộn (sau khi đã
// niêm) phải ném ngoại lệ nêu tên assembly.
public class ModuleAssemblyRegistrationExtensionsTests
{
    [Fact]
    public void SealModuleAssemblies_ReturnsRegisteredAssemblies()
    {
        var services = new ServiceCollection();
        var assembly = typeof(ModuleAssemblyRegistrationExtensionsTests).Assembly;

        services.RegisterModuleAssembly(assembly);
        var sealedAssemblies = services.SealModuleAssemblies();

        sealedAssemblies.ShouldContain(assembly);
    }

    [Fact]
    public void RegisterModuleAssembly_AfterSeal_Throws()
    {
        var services = new ServiceCollection();
        services.SealModuleAssemblies();

        Should.Throw<InvalidOperationException>(() => services.RegisterModuleAssembly(Assembly.GetExecutingAssembly()));
    }

    [Fact]
    public void SealModuleAssemblies_NoRegistrations_ReturnsEmpty()
    {
        var services = new ServiceCollection();

        var sealedAssemblies = services.SealModuleAssemblies();

        sealedAssemblies.ShouldBeEmpty();
    }
}
