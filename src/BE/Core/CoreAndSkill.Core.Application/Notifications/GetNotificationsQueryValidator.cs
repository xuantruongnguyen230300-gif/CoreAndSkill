using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Notifications;

internal sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode(CommonErrors.Format.Code);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithErrorCode(CommonErrors.Format.Code);
    }
}
