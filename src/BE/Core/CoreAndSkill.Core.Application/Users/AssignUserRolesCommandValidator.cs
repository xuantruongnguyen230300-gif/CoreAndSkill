using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Users;

// Chỉ kiểm thứ tính được từ payload — CORE.VALIDATION.FAILED khi RoleIds là null hoặc quá trần số phần tử
// (docs/contracts/users.md §7, "Ghi chú — trần 50 phần tử của roleIds"). Trùng lặp phần tử là mã RIÊNG (CORE.USER.DUPLICATE_ROLE_ENTRY),
// chặn ở HANDLER trước khi chạm dữ liệu — không đi qua ValidationBehavior vì root code không phải
// CORE.VALIDATION.FAILED (be-cqrs-handler.md §5.2, §7.5).
internal sealed class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.RoleIds)
            .NotNull().WithErrorCode(CommonErrors.Required.Code)
            .MaximumItems(UserRoleAssignmentRules.MaxRoleIds); // CORE.VALIDATION.MAX_ITEMS, tham số MaxItems
    }
}
