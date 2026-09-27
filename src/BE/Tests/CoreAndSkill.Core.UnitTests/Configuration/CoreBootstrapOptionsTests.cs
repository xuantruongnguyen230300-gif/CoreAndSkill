using System.ComponentModel.DataAnnotations;
using CoreAndSkill.Core.Application.Configuration;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Configuration;

public class CoreBootstrapOptionsTests
{
    [Fact]
    public void SectionName_IsCoreBootstrap()
    {
        CoreBootstrapOptions.SectionName.ShouldBe("Core:Bootstrap");
    }

    [Fact]
    public void Properties_RoundTripValues()
    {
        var options = new CoreBootstrapOptions
        {
            SystemTenantCode = "HETHONG",
            SystemTenantName = "Đơn vị hệ thống",
            FirstTenantCode = "DEMO",
            FirstTenantName = "Đơn vị demo",
            OperatorUserName = "superadmin",
            OperatorPassword = "pw1",
            OperatorEmail = "superadmin@dv.vn",
            AdminUserName = "admin",
            AdminPassword = "pw2",
            AdminEmail = "admin@dv.vn",
        };

        options.SystemTenantCode.ShouldBe("HETHONG");
        options.OperatorUserName.ShouldBe("superadmin");
        options.OperatorEmail.ShouldBe("superadmin@dv.vn");
        options.AdminUserName.ShouldBe("admin");
        options.AdminEmail.ShouldBe("admin@dv.vn");
    }

    // Hai tên đăng nhập lệnh bootstrap TẠO không được là danh tính hệ thống (SystemActor) — kiểm bằng CHÍNH phép mà runner
    // chạy ở đầu lệnh (Validator.TryValidateObject, validateAllProperties: true — CoreCommandRunner.TryValidate).
    [Theory]
    [InlineData(nameof(CoreBootstrapOptions.OperatorUserName), "system")]
    [InlineData(nameof(CoreBootstrapOptions.OperatorUserName), " System ")]
    [InlineData(nameof(CoreBootstrapOptions.AdminUserName), "SYSTEM")]
    [InlineData(nameof(CoreBootstrapOptions.AdminUserName), "\tsystem")]
    public void ReservedSystemUserName_FailsValidation_OnThatKey(string key, string userName)
    {
        var options = Valid(key == nameof(CoreBootstrapOptions.OperatorUserName) ? userName : "vanhanh",
            key == nameof(CoreBootstrapOptions.AdminUserName) ? userName : "quantri");

        var results = Validate(options);

        results.ShouldHaveSingleItem().MemberNames.ShouldBe([key]);
        results[0].ErrorMessage.ShouldNotBeNull().ShouldContain(key, Case.Sensitive);
    }

    // Đối chứng chống test rỗng: cùng bộ dựng, tên thường ⇒ hợp lệ.
    [Fact]
    public void OrdinaryUserNames_PassValidation() => Validate(Valid("vanhanh", "quantri")).ShouldBeEmpty();

    // Hai email bắt buộc, đúng hình dạng mà bộ kiểm người dùng của Identity chấp nhận, không rộng hơn cột core.app_user.email.
    [Theory]
    [InlineData(nameof(CoreBootstrapOptions.OperatorEmail), null)]
    [InlineData(nameof(CoreBootstrapOptions.OperatorEmail), " ")]
    [InlineData(nameof(CoreBootstrapOptions.OperatorEmail), "khong-co-a-cong")]
    [InlineData(nameof(CoreBootstrapOptions.AdminEmail), "")]
    [InlineData(nameof(CoreBootstrapOptions.AdminEmail), "quantri@")]
    [InlineData(nameof(CoreBootstrapOptions.AdminEmail), "@dv.vn")]
    public void MissingOrMalformedEmail_FailsValidation_OnThatKey(string key, string? email)
    {
        var options = Valid("vanhanh", "quantri",
            operatorEmail: key == nameof(CoreBootstrapOptions.OperatorEmail) ? email : "vanhanh@dv.vn",
            adminEmail: key == nameof(CoreBootstrapOptions.AdminEmail) ? email : "quantri@dv.vn");

        var results = Validate(options);

        results.ShouldHaveSingleItem().MemberNames.ShouldBe([key]);
        results[0].ErrorMessage.ShouldNotBeNull().ShouldContain(key, Case.Sensitive);
    }

    [Fact]
    public void EmailWiderThan256_FailsValidation_OnThatKey()
    {
        var at256 = new string('a', 244) + "@example.com";
        Validate(Valid("vanhanh", "quantri", adminEmail: at256)).ShouldBeEmpty();

        var results = Validate(Valid("vanhanh", "quantri", adminEmail: "a" + at256));

        results.ShouldHaveSingleItem().MemberNames.ShouldBe([nameof(CoreBootstrapOptions.AdminEmail)]);
    }

    private static CoreBootstrapOptions Valid(
        string operatorUserName, string adminUserName,
        string? operatorEmail = "vanhanh@dv.vn", string? adminEmail = "quantri@dv.vn") => new()
        {
            SystemTenantCode = "HETHONG",
            SystemTenantName = "Đơn vị hệ thống",
            FirstTenantCode = "DEMO",
            FirstTenantName = "Đơn vị demo",
            OperatorUserName = operatorUserName,
            OperatorPassword = "pw1",
            OperatorEmail = operatorEmail!,
            AdminUserName = adminUserName,
            AdminPassword = "pw2",
            AdminEmail = adminEmail!,
        };

    private static List<ValidationResult> Validate(CoreBootstrapOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
