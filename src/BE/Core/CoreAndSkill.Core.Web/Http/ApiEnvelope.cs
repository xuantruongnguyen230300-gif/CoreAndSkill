namespace CoreAndSkill.Core.Web.Http;

// Hình dạng dây của mọi response dưới /api — định nghĩa gốc, docs/quy-uoc/be-api-controller.md §2.1.
public sealed record ApiEnvelope<T>(bool Success, T? Data, ApiError? Error, string TraceId);

public sealed record ApiError(
    string Code, string Type, string Message,
    IReadOnlyDictionary<string, string>? MessageParams,
    IReadOnlyDictionary<string, IReadOnlyList<ApiFieldError>>? FieldErrors);

public sealed record ApiFieldError(string Code, IReadOnlyDictionary<string, string>? MessageParams);
