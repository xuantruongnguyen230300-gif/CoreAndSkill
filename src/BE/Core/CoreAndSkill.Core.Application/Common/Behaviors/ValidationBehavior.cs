using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Domain.Common;
using FluentValidation;
using MediatR;

namespace CoreAndSkill.Core.Application.Common.Behaviors;

// Chạy TRƯỚC TransactionBehavior — docs/quy-uoc/be-cqrs-handler.md §5.1, §5.2. Trả Result lỗi,
// KHÔNG bao giờ ném ValidationException — mọi lỗi đi qua MỘT đường dựng envelope.
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultFactory<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var byField = new Dictionary<string, List<FieldError>>(StringComparer.Ordinal);

        foreach (var validator in validators)
        {
            // MỘT ValidationContext RIÊNG cho mỗi validator — §6.2. Dùng chung context làm lỗi bị
            // đếm lặp theo số validator khi rule Custom/CustomAsync ghi thẳng vào context.
            var context = new ValidationContext<TRequest>(request);
            var result = await validator.ValidateAsync(context, cancellationToken);

            foreach (var failure in result.Errors)
            {
                var fieldErrors = byField.TryGetValue(failure.PropertyName, out var existing)
                    ? existing
                    : byField[failure.PropertyName] = [];

                fieldErrors.Add(new FieldError(
                    failure.ErrorCode,
                    MessageParamPolicy.Filter(failure.FormattedMessagePlaceholderValues)));
            }
        }

        if (byField.Count == 0)
            return await next(cancellationToken);

        var fieldErrorsResult = byField.ToDictionary(
            kv => kv.Key,
            IReadOnlyList<FieldError> (kv) => kv.Value,
            StringComparer.Ordinal);

        return TResponse.FromError(CommonErrors.ValidationFailed.WithFieldErrors(fieldErrorsResult));
    }
}
