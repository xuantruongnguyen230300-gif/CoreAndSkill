using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Roles;

internal sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code);

        // KHÔNG validate Version ở đây, có chủ đích — 06-concurrency-control.md §6.3 luật 3: "Lệch hoặc thiếu ⇒ 409".
        // Thiếu là một ca CONCURRENCY.CONFLICT, không phải VALIDATION.FAILED — phép so nằm ở IRoleAdminService.RenameAsync.
    }
}
