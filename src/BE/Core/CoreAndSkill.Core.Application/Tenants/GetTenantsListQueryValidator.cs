using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Tenants;

// docs/contracts/README.md §8 (khuôn chung) + docs/contracts/tenants.md §1 (allowlist riêng).
internal sealed class GetTenantsListQueryValidator : AbstractValidator<GetTenantsListQuery>
{
    private static readonly string[] SortByAllowlist = ["code", "name", "createdAt"];

    public GetTenantsListQueryValidator()
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
