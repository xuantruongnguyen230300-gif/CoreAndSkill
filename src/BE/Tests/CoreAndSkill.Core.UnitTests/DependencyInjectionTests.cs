using CoreAndSkill.Core.Application;
using CoreAndSkill.Core.Application.Auth;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests;

// docs/quy-uoc/be-cqrs-handler.md §5.1 — MỘT lời quét cho MediatR + FluentValidation, hai
// behavior đăng ký đúng một lần.
public class DependencyInjectionTests
{
    [Fact]
    public void AddCoreApplication_RegistersSenderAndValidators()
    {
        var services = new ServiceCollection();
        services.AddCoreApplication();
        var provider = services.BuildServiceProvider();

        provider.GetService<ISender>().ShouldNotBeNull();
        provider.GetService<IValidator<LoginCommand>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddCoreApplication_RegistersSessionDtoFactory()
    {
        // Không dựng provider ở đây — SessionDtoFactory cần IUserLookupService/IPermissionChecker/
        // IOptions<CoreAuthOptions>, các seam đó chỉ có hiện thực ở Infrastructure/Web. Chỉ kiểm ĐÃ
        // ĐĂNG KÝ, không kiểm resolve được.
        var services = new ServiceCollection();
        services.AddCoreApplication();

        services.Any(d => d.ServiceType == typeof(SessionDtoFactory)).ShouldBeTrue();
    }
}
