using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Commands;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Lệnh `core bootstrap` TẠO hai tài khoản từ cấu hình Core:Bootstrap:*. Identity của Core bật RequireUniqueEmail, nên mỗi tài
// khoản cần một email hợp lệ: Core:Bootstrap:OperatorEmail cho tài khoản vận hành, Core:Bootstrap:AdminEmail cho quản trị đơn
// vị đầu tiên. Thiếu hoặc sai hình dạng thì lệnh dừng ở phép kiểm đầu lệnh (docs/quy-uoc/be-architecture.md §4.4), thoát 1,
// không ghi dòng nào. Identity từ chối vì lý do khác (chính sách mật khẩu) thì lệnh in từng fieldError kèm khoá cấu hình tương
// ứng — mã gốc ADMIN_CREATE_FAILED không nói lỗi nằm ở ô nào.
//
// Không chạm database, Identity lấy từ đăng ký thật — OfflineBootstrapHarness.
//
// Environment.ExitCode và Console.Error là trạng thái TOÀN CỤC của tiến trình — cùng collection với các test runner khác.
[Collection("Console")]
public sealed class BootstrapAccountConfigTests : IDisposable
{
    private const string OperatorEmail = "vanhanh@he-thong.example.com";
    private const string AdminEmail = "quantri@dv-dau.example.com";

    private readonly TextWriter _originalError = Console.Error;
    private readonly TextWriter _originalOut = Console.Out;
    private readonly StringWriter _error = new();
    private readonly StringWriter _out = new();

    public BootstrapAccountConfigTests()
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

    // Đối chứng chống test rỗng cho mọi ca dưới: CÙNG bộ dựng, đủ hai email ⇒ lệnh ghi hai đơn vị, và mỗi tài khoản mang
    // đúng email của khoá dành cho nó — không tráo, không bỏ trống.
    [Fact]
    public async Task BothEmailsConfigured_CreatesEachAccountWithItsOwnEmail_ExitsZero()
    {
        var harness = OfflineBootstrapHarness.Build(new OfflineBootstrapHarness.EmptySeed());

        await CoreCommandRunner.RunBootstrapAsync(harness.Services);

        Environment.ExitCode.ShouldBe(0, _error.ToString());
        harness.UnitOfWork.Commits.ShouldBe(2);
        var accounts = harness.Writes.Accounts;
        accounts.Single(a => a.UserName == "vanhanh").Email.ShouldBe(OperatorEmail);
        accounts.Single(a => a.UserName == "quantri").Email.ShouldBe(AdminEmail);
    }

    [Theory]
    [InlineData(nameof(CoreBootstrapOptions.OperatorEmail), null)]
    [InlineData(nameof(CoreBootstrapOptions.OperatorEmail), "")]
    [InlineData(nameof(CoreBootstrapOptions.OperatorEmail), "   ")]
    [InlineData(nameof(CoreBootstrapOptions.AdminEmail), null)]
    [InlineData(nameof(CoreBootstrapOptions.AdminEmail), "")]
    public async Task MissingEmail_ExitsOne_WritesNothing_NamesTheKey(string key, string? value)
        => await ShouldStopBeforeAnyWrite(key, value);

    // Hình dạng mà bộ kiểm người dùng của Identity từ chối (InvalidEmail): thiếu phần trước @, thiếu phần sau @, không có @,
    // hai @, xuống dòng.
    [Theory]
    [InlineData(nameof(CoreBootstrapOptions.OperatorEmail), "khong-co-a-cong")]
    [InlineData(nameof(CoreBootstrapOptions.OperatorEmail), "@he-thong.example.com")]
    [InlineData(nameof(CoreBootstrapOptions.AdminEmail), "quantri@")]
    [InlineData(nameof(CoreBootstrapOptions.AdminEmail), "quan@tri@dv-dau.example.com")]
    [InlineData(nameof(CoreBootstrapOptions.AdminEmail), "quantri@dv-dau.example.com\nx")]
    public async Task MalformedEmail_ExitsOne_WritesNothing_NamesTheKey(string key, string value)
        => await ShouldStopBeforeAnyWrite(key, value);

    // Rộng hơn cột core.app_user.email (varchar 256) — thiếu phép kiểm này thì lượt ghi vỡ ở database thành ngoại lệ thô.
    [Fact]
    public async Task EmailWiderThanTheColumn_ExitsOne_WritesNothing_NamesTheKey()
        => await ShouldStopBeforeAnyWrite(nameof(CoreBootstrapOptions.AdminEmail), new string('a', 245) + "@example.com");

    // Cấu hình qua phép kiểm đầu lệnh nhưng Identity vẫn từ chối tài khoản (mật khẩu ngắn hơn chính sách). Mã gốc gộp mọi lý do,
    // nên lệnh phải in fieldError — mã, khoá của service, và khoá cấu hình người vận hành cần sửa.
    [Fact]
    public async Task OperatorRejectedByIdentity_PrintsFieldErrorWithItsConfigKey_WritesNothing()
    {
        var harness = OfflineBootstrapHarness.Build(
            new OfflineBootstrapHarness.EmptySeed(), OfflineBootstrapHarness.ValidOptions(operatorPassword: "ngan1"));

        await CoreCommandRunner.RunBootstrapAsync(harness.Services);

        Environment.ExitCode.ShouldBe(1);
        var error = _error.ToString();
        error.ShouldContain(TenantProvisioningErrors.AdminCreateFailed.Code, Case.Sensitive);
        error.ShouldContain(
            $"AdminTempPassword (khoá Core:Bootstrap:{nameof(CoreBootstrapOptions.OperatorPassword)}): {AuthErrors.PasswordTooShort.Code} (MinLength=8)",
            Case.Sensitive);
        error.ShouldNotContain("ngan1", Case.Sensitive, "không in mật khẩu");
        harness.UnitOfWork.Commits.ShouldBe(0);
    }

    // Đơn vị hệ thống đã commit, tài khoản quản trị của đơn vị đầu tiên bị từ chối: khoá cấu hình in ra là khoá của tài khoản
    // quản trị, không phải của tài khoản vận hành.
    [Fact]
    public async Task AdminRejectedByIdentity_PrintsFieldErrorWithTheAdminConfigKey()
    {
        var harness = OfflineBootstrapHarness.Build(
            new OfflineBootstrapHarness.EmptySeed(), OfflineBootstrapHarness.ValidOptions(adminPassword: "ngan1"));

        await CoreCommandRunner.RunBootstrapAsync(harness.Services);

        Environment.ExitCode.ShouldBe(1);
        var error = _error.ToString();
        error.ShouldContain($"AdminTempPassword (khoá Core:Bootstrap:{nameof(CoreBootstrapOptions.AdminPassword)})", Case.Sensitive);
        error.ShouldNotContain(nameof(CoreBootstrapOptions.OperatorPassword), Case.Sensitive);
        harness.UnitOfWork.Commits.ShouldBe(1, "đơn vị hệ thống đã commit ở transaction riêng của nó (ADR-0023 §1)");
    }

    private async Task ShouldStopBeforeAnyWrite(string key, string? value)
    {
        var options = OfflineBootstrapHarness.ValidOptions(
            operatorEmail: key == nameof(CoreBootstrapOptions.OperatorEmail) ? value : OperatorEmail,
            adminEmail: key == nameof(CoreBootstrapOptions.AdminEmail) ? value : AdminEmail);
        var harness = OfflineBootstrapHarness.Build(new OfflineBootstrapHarness.EmptySeed(), options);

        await CoreCommandRunner.RunBootstrapAsync(harness.Services);

        Environment.ExitCode.ShouldBe(1);
        _error.ToString().ShouldContain(key, Case.Sensitive);
        harness.ShouldHaveWrittenNothing();
    }
}
