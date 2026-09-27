using CoreAndSkill.Core.Domain.Tenants;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Tenants;

public class TenantTests
{
    [Fact]
    public void Create_TrimsAndUppercasesCode_TrimsName()
    {
        var result = Tenant.Create("  so-gd  ", "  Sở Giáo dục  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Code.ShouldBe("SO-GD");
        result.Value.Name.ShouldBe("Sở Giáo dục");
        result.Value.IsActive.ShouldBeTrue();
        result.Value.IsSystem.ShouldBeFalse();
    }

    [Fact]
    public void Create_IsSystemTrue_SetsFlag()
    {
        var result = Tenant.Create("HETHONG", "Đơn vị hệ thống", isSystem: true);

        result.Value.IsSystem.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_OnSystemTenant_Fails()
    {
        var tenant = Tenant.Create("HETHONG", "Đơn vị hệ thống", isSystem: true).Value;

        var result = tenant.Deactivate();

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantErrors.SystemImmutable.Code);
        tenant.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_OnBusinessTenant_Succeeds()
    {
        var tenant = Tenant.Create("SO-GD", "Sở Giáo dục").Value;

        var result = tenant.Deactivate();

        result.IsSuccess.ShouldBeTrue();
        tenant.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var tenant = Tenant.Create("SO-GD", "Sở Giáo dục").Value;
        tenant.Deactivate();

        var result = tenant.Activate();

        result.IsSuccess.ShouldBeTrue();
        tenant.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_GeneratesNonEmptyId()
    {
        var tenant = Tenant.Create("SO-GD", "Sở Giáo dục").Value;

        tenant.Id.ShouldNotBe(Guid.Empty);
    }
}
