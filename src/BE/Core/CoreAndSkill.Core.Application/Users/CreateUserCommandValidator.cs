using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Identity;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code)
            .NotReservedUserName(); // danh tính hệ thống — SystemActor

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .EmailAddress().WithErrorCode(CommonErrors.Format.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(200).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.TempPassword)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code);

        RuleFor(x => x.RoleIds)
            .NotNull().WithErrorCode(CommonErrors.Required.Code)
            .MaximumItems(UserRoleAssignmentRules.MaxRoleIds); // contracts/users.md §5 — CORE.VALIDATION.MAX_ITEMS, tham số MaxItems
    }
}
