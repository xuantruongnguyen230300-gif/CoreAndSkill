using System.Text.RegularExpressions;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Identity;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Tenants;

// docs/contracts/tenants.md §2. Khuôn mã kiểm SAU khi chuẩn hoá về chữ HOA — chuẩn hoá thật (lưu,
// so trùng) vẫn do Tenant.Create() làm; validator chỉ kiểm hình dạng trên giá trị đã chuẩn hoá cùng
// khuôn, để không chấp nhận một chuỗi mà sau khi chuẩn hoá mới lộ ra là sai khuôn.
internal sealed partial class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        // .When(...) cuối rule Code khai ApplyConditionTo.CurrentValidator — mặc định FluentValidation áp .When() cho MỌI
        // rule ĐỨNG TRƯỚC trong cùng RuleFor (AllValidators). Không khai rõ thì Code="" tắt luôn cả NotEmpty() của rule
        // này, và chuỗi rỗng lọt qua thành "hợp lệ" — bẫy đã dính khi viết rule này.
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(50).WithErrorCode(CommonErrors.MaxLength.Code)
            .Must(code => CodePattern().IsMatch(code.Trim().ToUpperInvariant()))
            .WithErrorCode(CommonErrors.Format.Code)
            .When(x => !string.IsNullOrEmpty(x.Code), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(200).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.AdminUserName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code)
            .NotReservedUserName(); // danh tính hệ thống — SystemActor

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .EmailAddress().WithErrorCode(CommonErrors.Format.Code)
            .MaximumLength(256).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.AdminFullName)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(200).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.AdminTempPassword)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code);
    }

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9_-]{0,49}$")]
    private static partial Regex CodePattern();
}
