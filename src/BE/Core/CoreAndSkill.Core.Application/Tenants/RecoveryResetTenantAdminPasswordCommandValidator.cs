using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Tenants;

internal sealed class RecoveryResetTenantAdminPasswordCommandValidator : AbstractValidator<RecoveryResetTenantAdminPasswordCommand>
{
    public RecoveryResetTenantAdminPasswordCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithErrorCode(CommonErrors.Required.Code);
        RuleFor(x => x.TempPassword).NotEmpty().WithErrorCode(CommonErrors.Required.Code);
    }
}
