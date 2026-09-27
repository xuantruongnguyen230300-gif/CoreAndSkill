namespace CoreAndSkill.Core.Application.Common.Interfaces;

// docs/quy-uoc/be-cqrs-handler.md §4. Một lần ghi là MỘT transaction trên MỌI DbContext đã đăng ký
// (Core và mọi module) — docs/adr/0025-luu-du-lieu-module-mot-transaction.md. Hiện thực ở
// Core.Infrastructure/Persistence/UnitOfWork.cs.
public readonly record struct TransactionOutcome<T>(T Value, bool ShouldCommit);

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<TransactionOutcome<T>>> operation,
        CancellationToken ct = default);
}
