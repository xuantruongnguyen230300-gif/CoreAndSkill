using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Common.Cqrs;

// docs/quy-uoc/be-cqrs-handler.md §1. IQuery<T> KHÔNG cài ICommandBase — TransactionBehavior loại
// query bằng ràng buộc generic where TRequest : ICommandBase (§5.3), không bằng if.
public interface IQuery<TResult> : IRequest<Result<TResult>>;
