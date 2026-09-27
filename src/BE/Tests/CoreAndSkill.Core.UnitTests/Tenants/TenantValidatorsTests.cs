using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Application.Users;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Tenants;

public class TenantValidatorsTests
{
    private static CreateTenantCommand ValidCreateCommand() => new(
        "SYT-HN", "Sở Y tế Hà Nội", "quantri.syt-hn", "quantri@syt-hn.gov.vn", "Nguyễn Văn An", "Temp@12345");

    [Fact]
    public void CreateTenantCommandValidator_Valid_IsValid()
        => new CreateTenantCommandValidator().Validate(ValidCreateCommand()).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("syt-hn")] // chuẩn hoá về HOA trước khi kiểm khuôn — vẫn hợp lệ
    [InlineData("A")]
    [InlineData("A_B-1")]
    public void CreateTenantCommandValidator_ValidCodeShapes_AreValid(string code)
        => new CreateTenantCommandValidator().Validate(ValidCreateCommand() with { Code = code }).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("-ABC")] // ký tự đầu không phải chữ/số
    [InlineData("AB C")] // khoảng trắng
    [InlineData("")]
    public void CreateTenantCommandValidator_InvalidCodeShapes_AreInvalid(string code)
        => new CreateTenantCommandValidator().Validate(ValidCreateCommand() with { Code = code }).IsValid.ShouldBeFalse();

    [Fact]
    public void CreateTenantCommandValidator_CodeTooLong_IsInvalid()
        => new CreateTenantCommandValidator().Validate(ValidCreateCommand() with { Code = new string('A', 51) })
            .IsValid.ShouldBeFalse();

    [Fact]
    public void CreateTenantCommandValidator_EmptyName_IsInvalid()
        => new CreateTenantCommandValidator().Validate(ValidCreateCommand() with { Name = "" }).IsValid.ShouldBeFalse();

    [Fact]
    public void CreateTenantCommandValidator_InvalidEmail_IsInvalid()
        => new CreateTenantCommandValidator().Validate(ValidCreateCommand() with { AdminEmail = "khong-hop-le" })
            .IsValid.ShouldBeFalse();

    [Fact]
    public void CreateTenantCommandValidator_EmptyTempPassword_IsInvalid()
        => new CreateTenantCommandValidator().Validate(ValidCreateCommand() with { AdminTempPassword = "" })
            .IsValid.ShouldBeFalse();

    [Fact]
    public void SetTenantActiveCommandValidator_IsActiveGiven_IsValid()
        => new SetTenantActiveCommandValidator().Validate(new SetTenantActiveCommand(Guid.NewGuid(), true))
            .IsValid.ShouldBeTrue();

    [Fact]
    public void SetTenantActiveCommandValidator_IsActiveMissing_IsInvalid()
        => new SetTenantActiveCommandValidator().Validate(new SetTenantActiveCommand(Guid.NewGuid(), null))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void RecoveryResetTenantAdminPasswordCommandValidator_Valid_IsValid()
        => new RecoveryResetTenantAdminPasswordCommandValidator()
            .Validate(new RecoveryResetTenantAdminPasswordCommand(Guid.NewGuid(), "quantri", "Temp@12345"))
            .IsValid.ShouldBeTrue();

    [Fact]
    public void RecoveryResetTenantAdminPasswordCommandValidator_EmptyUserName_IsInvalid()
        => new RecoveryResetTenantAdminPasswordCommandValidator()
            .Validate(new RecoveryResetTenantAdminPasswordCommand(Guid.NewGuid(), "", "Temp@12345"))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void RecoveryResetTenantAdminPasswordCommandValidator_EmptyTempPassword_IsInvalid()
        => new RecoveryResetTenantAdminPasswordCommandValidator()
            .Validate(new RecoveryResetTenantAdminPasswordCommand(Guid.NewGuid(), "quantri", ""))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void CreateTenantAdminCommandValidator_Valid_IsValid()
        => new CreateTenantAdminCommandValidator()
            .Validate(new CreateTenantAdminCommand(Guid.NewGuid(), "quantri2", "quantri2@vd.vn", "Trần Thị Bình", "Temp@12345"))
            .IsValid.ShouldBeTrue();

    // tenants.md §2 — tài khoản quản trị đầu tiên không được mang danh tính hệ thống (SystemActor). Ô là AdminUserName.
    [Theory]
    [InlineData("system")]
    [InlineData("System")]
    [InlineData("SYSTEM")]
    [InlineData(" system ")]
    public void CreateTenantCommandValidator_ReservedSystemAdminUserName_IsInvalid_OnAdminUserName(string adminUserName)
    {
        var command = ValidCreateCommand() with { AdminUserName = adminUserName };

        new CreateTenantCommandValidator().Validate(command).Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            e => e.PropertyName.ShouldBe(nameof(CreateTenantCommand.AdminUserName)),
            e => e.ErrorCode.ShouldBe(UserErrors.UsernameReserved.Code));
    }

    // tenants.md §6 — tài khoản quản trị bổ sung. Ô là UserName.
    [Theory]
    [InlineData("system")]
    [InlineData("System")]
    [InlineData("SYSTEM")]
    [InlineData(" system ")]
    public void CreateTenantAdminCommandValidator_ReservedSystemUserName_IsInvalid_OnUserName(string userName)
        => new CreateTenantAdminCommandValidator()
            .Validate(new CreateTenantAdminCommand(Guid.NewGuid(), userName, "quantri2@vd.vn", "Trần Thị Bình", "Temp@12345"))
            .Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
                e => e.PropertyName.ShouldBe(nameof(CreateTenantAdminCommand.UserName)),
                e => e.ErrorCode.ShouldBe(UserErrors.UsernameReserved.Code));

    [Fact]
    public void CreateTenantAdminCommandValidator_InvalidEmail_IsInvalid()
        => new CreateTenantAdminCommandValidator()
            .Validate(new CreateTenantAdminCommand(Guid.NewGuid(), "quantri2", "khong-hop-le", "Trần Thị Bình", "Temp@12345"))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void GetTenantsListQueryValidator_Defaults_IsValid()
        => new GetTenantsListQueryValidator().Validate(new GetTenantsListQuery()).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("code")]
    [InlineData("name")]
    [InlineData("createdAt")]
    public void GetTenantsListQueryValidator_AllowedSortBy_IsValid(string sortBy)
        => new GetTenantsListQueryValidator().Validate(new GetTenantsListQuery(SortBy: sortBy)).IsValid.ShouldBeTrue();

    [Fact]
    public void GetTenantsListQueryValidator_SortByOutsideAllowlist_IsInvalid()
        => new GetTenantsListQueryValidator().Validate(new GetTenantsListQuery(SortBy: "id")).IsValid.ShouldBeFalse();

    [Fact]
    public void GetTenantsListQueryValidator_PageBelowOne_IsInvalid()
        => new GetTenantsListQueryValidator().Validate(new GetTenantsListQuery(Page: 0)).IsValid.ShouldBeFalse();

    [Fact]
    public void GetTenantsListQueryValidator_PageSizeTooLarge_IsInvalid()
        => new GetTenantsListQueryValidator().Validate(new GetTenantsListQuery(PageSize: 500)).IsValid.ShouldBeFalse();
}
