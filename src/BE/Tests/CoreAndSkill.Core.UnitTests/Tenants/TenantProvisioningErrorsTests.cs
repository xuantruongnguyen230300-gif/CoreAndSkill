using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Tenants;

public class TenantProvisioningErrorsTests
{
    [Fact]
    public void AdminCreateFailed_HasExpectedCodeAndType()
    {
        TenantProvisioningErrors.AdminCreateFailed.Code.ShouldBe("CORE.TENANT.ADMIN_CREATE_FAILED");
        TenantProvisioningErrors.AdminCreateFailed.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public void NotFound_HasExpectedCodeAndType()
    {
        TenantProvisioningErrors.NotFound.Code.ShouldBe("CORE.TENANT.NOT_FOUND");
        TenantProvisioningErrors.NotFound.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void CodeDuplicate_HasExpectedCodeAndType()
    {
        TenantProvisioningErrors.CodeDuplicate.Code.ShouldBe("CORE.TENANT.CODE_DUPLICATE");
        TenantProvisioningErrors.CodeDuplicate.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void SeedFailed_HasExpectedCodeAndType()
    {
        TenantProvisioningErrors.SeedFailed.Code.ShouldBe("CORE.TENANT.SEED_FAILED");
        TenantProvisioningErrors.SeedFailed.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public void RecoveryTargetNotEligible_HasExpectedCodeAndType()
    {
        TenantProvisioningErrors.RecoveryTargetNotEligible.Code.ShouldBe("CORE.TENANT.RECOVERY_TARGET_NOT_ELIGIBLE");
        TenantProvisioningErrors.RecoveryTargetNotEligible.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public void RecoveryResetFailed_HasExpectedCodeAndType()
    {
        TenantProvisioningErrors.RecoveryResetFailed.Code.ShouldBe("CORE.TENANT.RECOVERY_RESET_FAILED");
        TenantProvisioningErrors.RecoveryResetFailed.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public void OperatorNotFound_HasExpectedCodeAndType()
    {
        TenantProvisioningErrors.OperatorNotFound.Code.ShouldBe("CORE.TENANT.OPERATOR_NOT_FOUND");
        TenantProvisioningErrors.OperatorNotFound.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public void OperatorPasswordResetFailed_HasExpectedCodeAndType()
    {
        TenantProvisioningErrors.OperatorPasswordResetFailed.Code.ShouldBe("CORE.TENANT.OPERATOR_PASSWORD_RESET_FAILED");
        TenantProvisioningErrors.OperatorPasswordResetFailed.Type.ShouldBe(ErrorType.BusinessRule);
    }
}
