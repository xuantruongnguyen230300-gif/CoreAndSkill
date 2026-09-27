using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Permissions;

// Chỉ kiểm thứ tính được từ payload — Entries null (docs/contracts/permissions.md §6), phần tử null và RoleIds null
// (entries[].roleIds là field bắt buộc). Không chặn ở đây thì handler/service chạm null ⇒ 500. Phủ đủ danh mục và
// trùng lặp là mã RIÊNG, chặn ở handler trước khi ghi (§6 "Ghi chú").
internal sealed class UpdatePermissionMatrixCommandValidator : AbstractValidator<UpdatePermissionMatrixCommand>
{
    public UpdatePermissionMatrixCommandValidator()
    {
        RuleFor(x => x.Entries).NotNull().WithErrorCode(CommonErrors.Required.Code);

        RuleForEach(x => x.Entries)
            .NotNull().WithErrorCode(CommonErrors.Required.Code)
            .ChildRules(entry => entry.RuleFor(e => e.RoleIds).NotNull().WithErrorCode(CommonErrors.Required.Code));
    }
}
