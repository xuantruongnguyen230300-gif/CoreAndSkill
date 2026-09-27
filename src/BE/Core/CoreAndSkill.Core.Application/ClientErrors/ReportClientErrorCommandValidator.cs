using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.ClientErrors;

// docs/contracts/client-errors.md §1. TỪ CHỐI khi vượt giới hạn độ dài — KHÔNG cắt bớt rồi lưu
// (cắt bớt vẫn có thể để lọt dữ liệu nhạy cảm vào log dưới dạng đã cụt, xem "Ghi chú" của card).
internal sealed class ReportClientErrorCommandValidator : AbstractValidator<ReportClientErrorCommand>
{
    public ReportClientErrorCommandValidator()
    {
        RuleFor(x => x.Kind).NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(100).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.Message).NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(500).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.Stack).MaximumLength(4000).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.DuongDan).NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(2000).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.TraceId).MaximumLength(100).WithErrorCode(CommonErrors.MaxLength.Code);

        RuleFor(x => x.PhienBanApp).NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(100).WithErrorCode(CommonErrors.MaxLength.Code);
    }
}
