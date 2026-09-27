using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Users;

// docs/contracts/README.md §8 (khuôn chung) + docs/contracts/users.md §3 (allowlist riêng).
internal sealed class GetUsersListQueryValidator : AbstractValidator<GetUsersListQuery>
{
    private static readonly string[] SortByAllowlist = UsersListAllowlists.SortBy;
    private static readonly string[] StatusAllowlist = UsersListAllowlists.Status;

    public GetUsersListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode(CommonErrors.Format.Code);

        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithErrorCode(CommonErrors.Format.Code);

        RuleFor(x => x.SortBy)
            .Must(sortBy => sortBy is null || SortByAllowlist.Contains(sortBy))
            .WithErrorCode(CommonErrors.Format.Code);

        RuleFor(x => x.SearchText)
            .MaximumLength(UsersListAllowlists.SearchTextMaxLength).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.Status)
            .Must(status => status is null || StatusAllowlist.Contains(status))
            .WithErrorCode(CommonErrors.Format.Code);
    }
}
