using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Web.Http;

// Chỗ DUY NHẤT dựng ApiEnvelope<T> — docs/quy-uoc/be-api-controller.md §2.1.
public static class Envelope
{
    public static ApiEnvelope<T> Success<T>(T? data, string traceId) => new(true, data, null, traceId);

    public static ApiEnvelope<object> Failure(Error error, string traceId) => new(false, null, ToApiError(error), traceId);

    private static ApiError ToApiError(Error error)
    {
        var message = MessageTemplateRenderer.Render(error.MessageTemplate, error.Params);
        var messageParams = error.Params.Count == 0 ? null : error.Params;
        var fieldErrors = error.FieldErrors.Count == 0 ? null : ToApiFieldErrors(error.FieldErrors);

        return new ApiError(error.Code, error.Type.ToString(), message, messageParams, fieldErrors);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<ApiFieldError>> ToApiFieldErrors(
        IReadOnlyDictionary<string, IReadOnlyList<FieldError>> fieldErrors)
    {
        var result = new Dictionary<string, IReadOnlyList<ApiFieldError>>(fieldErrors.Count, StringComparer.Ordinal);

        foreach (var (field, errors) in fieldErrors)
        {
            var mapped = new List<ApiFieldError>(errors.Count);
            foreach (var fieldError in errors)
            {
                var fieldParams = fieldError.Params.Count == 0 ? null : fieldError.Params;
                mapped.Add(new ApiFieldError(fieldError.Code, fieldParams));
            }

            result[field] = mapped;
        }

        return result;
    }
}
