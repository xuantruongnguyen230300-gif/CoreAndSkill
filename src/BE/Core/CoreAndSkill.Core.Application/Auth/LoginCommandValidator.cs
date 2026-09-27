using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Auth;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        // Trần 50 — khuôn mã đơn vị ở docs/contracts/tenants.md §2 (cột core.tenant.code). Chỉ trần độ dài, KHÔNG kiểm
        // khuôn ký tự: mã sai khuôn đi tiếp và nhận CORE.AUTH.INVALID_CREDENTIALS như mọi mã không tồn tại (auth.md §3).
        RuleFor(x => x.TenantCode)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(50).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.UserName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code);
    }
}
