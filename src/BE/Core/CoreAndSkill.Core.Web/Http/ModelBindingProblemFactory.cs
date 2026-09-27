using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CoreAndSkill.Core.Web.Http;

// Thay InvalidModelStateResponseFactory mặc định để lỗi model binding cũng ra ĐÚNG envelope —
// docs/quy-uoc/be-api-controller.md §2.4 ("Body không parse được, kiểu sai, thiếu tham số bắt buộc — lỗi xảy
// ra trước khi vào action"), docs/quy-uoc/be-architecture.md §2.1.
//
// Mặc định của framework trả `application/problem+json` kèm CHÍNH thông điệp của exception nội bộ
// ("Failed to read the request form. Multipart body length limit … exceeded.") — sai hình dạng dây, rò chi
// tiết framework, và FE không có mã để tra. Ở đây: một mã nghiệp vụ (CORE.VALIDATION.FAILED), và với khoá
// trường gọi tên được thì fieldErrors[<Trường>] mang CORE.VALIDATION.FORMAT. KHÔNG bao giờ đưa thông điệp
// của ModelState ra dây: nó do framework sinh, không có khuôn, không dịch được.
internal static class ModelBindingProblemFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var fieldErrors = new Dictionary<string, IReadOnlyList<FieldError>>(StringComparer.Ordinal);
        var bodyParameters = BodyParameterNames(context);

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.ValidationState != ModelValidationState.Invalid || bodyParameters.Contains(key))
                continue;

            var field = FieldName(key);
            if (field is null)
                continue;

            fieldErrors[field] =
            [
                new FieldError(CommonErrors.Format.Code, new Dictionary<string, string>()),
            ];
        }

        var error = fieldErrors.Count == 0
            ? CommonErrors.ValidationFailed
            : CommonErrors.ValidationFailed.WithFieldErrors(fieldErrors);

        return new ObjectResult(Envelope.Failure(error, context.HttpContext.TraceIdentifier))
        {
            StatusCode = ResultToHttpMapper.ToStatusCode(error.Type),
        };
    }

    // Body đọc hỏng (JSON sai kiểu, thân rỗng) thì MVC gắn thêm một lỗi mang tên CHÍNH tham số action (`body`,
    // `command`) — tên đó là của code C#, không phải trường nào người dùng thấy, nên không vào fieldErrors.
    private static HashSet<string> BodyParameterNames(ActionContext context) =>
        context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.BindingInfo?.BinderModelName ?? p.Name)
            .ToHashSet(StringComparer.Ordinal);

    // Khoá rỗng (lỗi của cả form/body) hay bắt đầu bằng `$` (đường dẫn JSON của bộ đọc body) không gọi tên
    // được trường nào của người dùng -> không có fieldErrors, chỉ có mã chung. Khoá lồng (`request.Email`)
    // lấy đoạn cuối: FE tra theo tên trường (§2.3).
    private static string? FieldName(string key)
    {
        if (string.IsNullOrEmpty(key) || key[0] == '$')
            return null;

        var last = key[(key.LastIndexOf('.') + 1)..];
        var bracket = last.IndexOf('[', StringComparison.Ordinal);
        if (bracket >= 0)
            last = last[..bracket];

        return last.Length == 0 ? null : last;
    }
}
