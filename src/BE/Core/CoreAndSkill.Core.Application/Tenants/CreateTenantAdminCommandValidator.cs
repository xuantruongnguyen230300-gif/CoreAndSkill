using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Identity;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Tenants;

internal sealed class CreateTenantAdminCommandValidator : AbstractValidator<CreateTenantAdminCommand>
{
    public CreateTenantAdminCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code)
            .NotReservedUserName(); // danh tính hệ thống — SystemActor
        RuleFor(x => x.Email).NotEmpty().WithErrorCode(CommonErrors.Required.Code).EmailAddress().WithErrorCode(CommonErrors.Format.Code);
        RuleFor(x => x.FullName).NotEmpty().WithErrorCode(CommonErrors.Required.Code).MaximumLength(200).WithErrorCode(CommonErrors.MaxLength.Code);
        RuleFor(x => x.TempPassword).NotEmpty().WithErrorCode(CommonErrors.Required.Code);
    }
}
