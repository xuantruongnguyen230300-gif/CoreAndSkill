using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Import;

internal sealed class StartImportCommandValidator : AbstractValidator<StartImportCommand>
{
    public StartImportCommandValidator()
    {
        RuleFor(x => x.File).NotNull().WithErrorCode(CommonErrors.Required.Code);
        RuleFor(x => x.Type).NotEmpty().WithErrorCode(CommonErrors.Required.Code);
    }
}
