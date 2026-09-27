using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Profile;

internal sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    // docs/contracts/profile.md §2 — chỉ chữ số, khoảng trắng và + - ( ).
    private const string PhoneNumberPattern = @"^[0-9\s+\-()]+$";

    // Hai chữ thường, tuỳ chọn thêm - và hai chữ hoa (vi, en-US).
    private const string LanguagePattern = "^[a-z]{2}(-[A-Z]{2})?$";

    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(200).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithErrorCode(CommonErrors.MaxLength.Code)
            .Matches(PhoneNumberPattern).WithErrorCode(CommonErrors.Format.Code)
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.PreferredLanguage)
            .Matches(LanguagePattern).WithErrorCode(CommonErrors.Format.Code)
            .When(x => x.PreferredLanguage is not null);

        // KHÔNG validate Version ở đây, có chủ đích — docs/contracts/profile.md §2: "Thiếu hoặc
        // lệch ⇒ 409, vì null không bao giờ khớp". Thiếu là một ca CONCURRENCY.CONFLICT, không phải
        // VALIDATION.FAILED — phép so token nằm ở IUserProfileService.UpdateAsync.
    }
}
