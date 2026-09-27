using CoreAndSkill.Core.Application.Common;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Files;

// Chỉ kiểm thứ tính được từ payload; dung lượng, kiểu tệp và purpose có tồn tại không là việc của
// handler (cần catalog + nội dung).
internal sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithErrorCode(CommonErrors.Required.Code);

        RuleFor(x => x.Purpose)
            .NotEmpty().WithErrorCode(CommonErrors.Required.Code)
            .MaximumLength(50).WithErrorCode(CommonErrors.MaxLength.Code);
    }
}
