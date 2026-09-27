using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Roles;

// docs/contracts/README.md §8 (khuôn chung) + docs/contracts/roles.md §1 (allowlist riêng).
internal sealed class GetRolesListQueryValidator : AbstractValidator<GetRolesListQuery>
{
    private static readonly string[] SortByAllowlist = ["name", "createdAt"];

    public GetRolesListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode(CommonErrors.Format.Code);

        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithErrorCode(CommonErrors.Format.Code);

        RuleFor(x => x.SortBy)
            .Must(sortBy => sortBy is null || SortByAllowlist.Contains(sortBy))
            .WithErrorCode(CommonErrors.Format.Code);

        RuleFor(x => x.SearchText)
            .MaximumLength(200).WithErrorCode(CommonErrors.MaxLength.Code);
    }
}
