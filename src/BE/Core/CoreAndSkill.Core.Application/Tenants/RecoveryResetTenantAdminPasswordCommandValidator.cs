using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Tenants;

internal sealed class RecoveryResetTenantAdminPasswordCommandValidator : AbstractValidator<RecoveryResetTenantAdminPasswordCommand>
{
    public RecoveryResetTenantAdminPasswordCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code); // khớp cột core.app_user.user_name
        RuleFor(x => x.TempPassword).NotEmpty().WithErrorCode(CommonErrors.Required.Code);
    }
}
