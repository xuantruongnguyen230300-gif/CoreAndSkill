using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Roles;

internal sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code);
    }
}
