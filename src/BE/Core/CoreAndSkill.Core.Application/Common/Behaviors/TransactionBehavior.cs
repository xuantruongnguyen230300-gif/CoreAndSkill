using CoreAndSkill.Core.Application.Common.Cqrs;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Common.Behaviors;

// Chỉ bọc COMMAND — docs/quy-uoc/be-cqrs-handler.md §5.3. Query loại trừ bằng RÀNG BUỘC GENERIC
// (where TRequest : ICommandBase), không phải if. Commit CHỈ KHI response.IsSuccess — vì thế
// handler KHÔNG được tự gọi SaveChangesAsync.
internal sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork uow)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommandBase
    where TResponse : IResult
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is INoTransaction)
            return next(cancellationToken);

        return uow.ExecuteInTransactionAsync(async innerCt =>
        {
            var response = await next(innerCt);
            return new TransactionOutcome<TResponse>(response, response.IsSuccess);
        }, cancellationToken);
    }
}
