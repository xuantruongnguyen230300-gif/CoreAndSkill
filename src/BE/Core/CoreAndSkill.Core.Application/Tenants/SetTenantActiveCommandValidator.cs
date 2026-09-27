using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Tenants;

internal sealed class SetTenantActiveCommandValidator : AbstractValidator<SetTenantActiveCommand>
{
    public SetTenantActiveCommandValidator()
    {
        RuleFor(x => x.IsActive).NotNull().WithErrorCode(CommonErrors.Required.Code);
    }
}
