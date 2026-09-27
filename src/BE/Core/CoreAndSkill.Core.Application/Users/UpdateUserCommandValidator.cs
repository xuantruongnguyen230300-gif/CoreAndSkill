using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .EmailAddress().WithErrorCode(CommonErrors.Format.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(200).WithErrorCode(CommonErrors.MaxLength.Code);
    }
}
