using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Export;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Users;

// Cùng allowlist với GetUsersListQueryValidator (UsersListAllowlists) cộng `format`. Không có Page /
// PageSize — xuất là xuất cả tập.
internal sealed class ExportUsersCommandValidator : AbstractValidator<ExportUsersCommand>
{
    public ExportUsersCommandValidator()
    {
        RuleFor(x => x.Format)
            .Must(ExportFormats.IsSupported).WithErrorCode(CommonErrors.Format.Code);

        RuleFor(x => x.SortBy)
            .Must(sortBy => sortBy is null || UsersListAllowlists.SortBy.Contains(sortBy))
            .WithErrorCode(CommonErrors.Format.Code);

        RuleFor(x => x.SearchText)
            .MaximumLength(UsersListAllowlists.SearchTextMaxLength).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.Status)
            .Must(status => status is null || UsersListAllowlists.Status.Contains(status))
            .WithErrorCode(CommonErrors.Format.Code);
    }
}
