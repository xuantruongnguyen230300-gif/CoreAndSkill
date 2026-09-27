using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Common.Cqrs;

// Envelope Result<T> ép ở tầng KIỂU — docs/quy-uoc/be-cqrs-handler.md §1.
// ICommandBase là marker RỖNG mà TransactionBehavior ràng buộc theo (§5.3) — không khai member nào.
public interface ICommandBase;

public interface ICommand : ICommandBase, IRequest<Result>;

public interface ICommand<TResult> : ICommandBase, IRequest<Result<TResult>>;
