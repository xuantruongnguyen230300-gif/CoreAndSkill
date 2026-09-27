using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Web.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Lệnh `core bootstrap` nhận hai tên đăng nhập từ cấu hình (Core:Bootstrap:OperatorUserName, Core:Bootstrap:AdminUserName) và
// TẠO hai tài khoản mang chúng. Tên là danh tính hệ thống (SystemActor) ⇒ lệnh dừng ở phép kiểm đầu lệnh, thoát 1, không gọi
// service tạo đơn vị lần nào — tức không dòng nào được ghi.
//
// Environment.ExitCode và Console.Error là trạng thái TOÀN CỤC của tiến trình — cùng collection với BootstrapSeedValidationTests.
[Collection("Console")]
public sealed class BootstrapReservedUserNameTests : IDisposable
{
    private readonly TextWriter _originalError = Console.Error;
    private readonly TextWriter _originalOut = Console.Out;
    private readonly StringWriter _error = new();
    private readonly StringWriter _out = new();
    private readonly ITenantProvisioningService _provisioning = Substitute.For<ITenantProvisioningService>();

    public BootstrapReservedUserNameTests()
    {
        Console.SetError(_error);
        Console.SetOut(_out);
        Environment.ExitCode = 0;
    }

    public void Dispose()
    {
        Console.SetError(_originalError);
        Console.SetOut(_originalOut);
        Environment.ExitCode = 0;
    }

    [Theory]
    [InlineData(nameof(CoreBootstrapOptions.OperatorUserName), "system")]
    [InlineData(nameof(CoreBootstrapOptions.OperatorUserName), "System")]
    [InlineData(nameof(CoreBootstrapOptions.AdminUserName), " SYSTEM ")]
    public async Task ReservedUserNameInConfig_ExitsOne_BeforeAnyWrite(string key, string userName)
    {
        var services = Services(
            operatorUserName: key == nameof(CoreBootstrapOptions.OperatorUserName) ? userName : "vanhanh",
            adminUserName: key == nameof(CoreBootstrapOptions.AdminUserName) ? userName : "quantri");

        await CoreCommandRunner.RunBootstrapAsync(services);

        Environment.ExitCode.ShouldBe(1);
        _error.ToString().ShouldContain(key, Case.Sensitive);
        _provisioning.ReceivedCalls().ShouldBeEmpty("phép kiểm phải chạy TRƯỚC lời gọi tạo đơn vị đầu tiên");
    }

    private IServiceProvider Services(string operatorUserName, string adminUserName)
        => new ServiceCollection()
            .AddSingleton(Options.Create(new CoreBootstrapOptions
            {
                SystemTenantCode = "HE-THONG",
                SystemTenantName = "Đơn vị hệ thống",
                FirstTenantCode = "DV-DAU",
                FirstTenantName = "Đơn vị đầu tiên",
                OperatorUserName = operatorUserName,
                OperatorPassword = "Passw0rd-Tam1",
                OperatorEmail = "vanhanh@he-thong.example.com",
                AdminUserName = adminUserName,
                AdminPassword = "Passw0rd-Tam1",
                AdminEmail = "quantri@dv-dau.example.com",
            }))
            .AddSingleton(_provisioning)
            .AddSingleton(Substitute.For<IUnitOfWork>())
            .BuildServiceProvider();
}
