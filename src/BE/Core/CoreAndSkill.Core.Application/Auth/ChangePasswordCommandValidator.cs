using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Auth;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .NotEqual(x => x.CurrentPassword).WithErrorCode(AuthErrors.NewPasswordSameAsCurrent.Code);
    }
}
