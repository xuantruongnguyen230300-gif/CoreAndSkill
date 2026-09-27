using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Tenants;
using Microsoft.AspNetCore.Identity;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Logic ánh xạ lỗi Identity → fieldErrors của TenantProvisioningService — thuần, không chạm DB.
// docs/contracts/tenants.md §2, §4, §6. Mã và tham số đi qua IdentityErrorMapper, cùng một nguồn với đổi mật khẩu.
public class TenantProvisioningServiceMappingTests
{
    private static readonly PasswordOptions Policy = new() { RequiredLength = 10, RequiredUniqueChars = 4 };

    // Tài khoản quản trị đầu tiên mà UserManager vừa từ chối — nguồn tham số của mã trùng lặp.
    private static AppUser RejectedAdmin() => AppUser.NewAccount("quantri.hn", "quantri@hn.vn", "Quản trị Hà Nội");

    // §4/§6 — CHỈ lý do chính sách mật khẩu lộ ra; userName/email bị bỏ qua (chống dò userName).
    [Fact]
    public void WithPasswordOnlyFieldErrors_PasswordError_MapsToTempPasswordField()
    {
        var errors = new[] { new IdentityError { Code = "PasswordTooShort" } };

        var error = TenantProvisioningService.WithPasswordOnlyFieldErrors(TenantProvisioningErrors.RecoveryResetFailed, errors, Policy);

        error.FieldErrors.ShouldContainKey("TempPassword");
        error.FieldErrors["TempPassword"].ShouldContain(fe => fe.Code == "CORE.AUTH.PASSWORD_TOO_SHORT");
    }

    [Fact]
    public void WithPasswordOnlyFieldErrors_UsernameError_IsNotExposed()
    {
        var errors = new[] { new IdentityError { Code = "DuplicateUserName" } };

        var error = TenantProvisioningService.WithPasswordOnlyFieldErrors(TenantProvisioningErrors.RecoveryResetFailed, errors, Policy);

        // Không có fieldErrors nào — người gọi chỉ thấy mã gộp, không dò được userName đã tồn tại.
        error.FieldErrors.ShouldBeEmpty();
        error.Code.ShouldBe(TenantProvisioningErrors.RecoveryResetFailed.Code);
    }

    [Fact]
    public void WithPasswordOnlyFieldErrors_NoErrors_ReturnsRootErrorUnchanged()
    {
        var error = TenantProvisioningService.WithPasswordOnlyFieldErrors(TenantProvisioningErrors.AdminCreateFailed, [], Policy);

        error.ShouldBe(TenantProvisioningErrors.AdminCreateFailed);
    }

    // §2 — KHÁC §4/§6: userName/email được lộ tường minh (đơn vị vừa tạo, không phải phép dò). Mã trùng lặp mang tham số
    // cùng khoá với card users.md §5 — cùng mã thì cùng câu dịch, thiếu tham số thì FE hiện "{{UserName}}" nguyên chữ.
    [Fact]
    public void MapFirstAdminCreateErrors_DuplicateUserName_MapsToAdminUserNameField_WithUserNameParam()
    {
        var errors = new[] { new IdentityError { Code = "DuplicateUserName" } };

        var error = TenantProvisioningService.MapFirstAdminCreateErrors(errors, RejectedAdmin(), Policy);

        error.FieldErrors.ShouldContainKey("AdminUserName");
        error.FieldErrors["AdminUserName"].Single(fe => fe.Code == "CORE.USER.USERNAME_DUPLICATED")
            .Params.ShouldContainKeyAndValue("UserName", "quantri.hn");
    }

    [Fact]
    public void MapFirstAdminCreateErrors_DuplicateEmail_MapsToAdminEmailField_WithEmailParam()
    {
        var errors = new[] { new IdentityError { Code = "DuplicateEmail" } };

        var error = TenantProvisioningService.MapFirstAdminCreateErrors(errors, RejectedAdmin(), Policy);

        error.FieldErrors.ShouldContainKey("AdminEmail");
        error.FieldErrors["AdminEmail"].Single(fe => fe.Code == "CORE.USER.EMAIL_DUPLICATED")
            .Params.ShouldContainKeyAndValue("Email", "quantri@hn.vn");
    }

    // Cùng mã VÀ cùng tham số với đường đổi mật khẩu — docs/contracts/auth.md §6 "Ghi chú".
    [Fact]
    public void MapFirstAdminCreateErrors_PasswordPolicy_MapsToAdminTempPasswordField_WithPolicyValues()
    {
        var errors = new[]
        {
            new IdentityError { Code = "PasswordTooShort" },
            new IdentityError { Code = "PasswordRequiresUniqueChars" },
        };

        var error = TenantProvisioningService.MapFirstAdminCreateErrors(errors, RejectedAdmin(), Policy);

        var fieldErrors = error.FieldErrors["AdminTempPassword"];
        fieldErrors.Single(fe => fe.Code == "CORE.AUTH.PASSWORD_TOO_SHORT").Params.ShouldContainKeyAndValue("MinLength", "10");
        fieldErrors.Single(fe => fe.Code == "CORE.AUTH.PASSWORD_REQUIRES_UNIQUE_CHARS").Params
            .ShouldContainKeyAndValue("MinUniqueChars", "4");
    }

    [Fact]
    public void MapFirstAdminCreateErrors_RootCode_IsAdminCreateFailed()
        => TenantProvisioningService.MapFirstAdminCreateErrors([new IdentityError { Code = "DuplicateUserName" }], RejectedAdmin(), Policy)
            .Code.ShouldBe("CORE.TENANT.ADMIN_CREATE_FAILED");
}
