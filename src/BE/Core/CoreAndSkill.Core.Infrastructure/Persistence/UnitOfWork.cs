using System.Data.Common;
using System.Diagnostics;
using CoreAndSkill.Core.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CoreAndSkill.Core.Infrastructure.Persistence;

// docs/quy-uoc/be-cqrs-handler.md §4. Một lần ghi là MỘT transaction trên MỌI DbContext đã đăng ký
// — ở B1 chỉ CoreDbContext; module thêm DbContext của mình vào IEnumerable<DbContext> khi lắp vào.
// Ba bẫy PHẢI giữ đúng thứ tự: execution strategy bọc ngoài cùng, ChangeTracker.Clear() đầu MỖI
// lượt thử lại, gắn TRANSACTION cho MỌI context trước khi chạy operation.
public sealed class UnitOfWork(DbConnection connection, IEnumerable<DbContext> contexts, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private readonly IReadOnlyList<DbContext> _contexts = contexts.ToList();

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var total = 0;
        foreach (var context in _contexts)
            total += await context.SaveChangesAsync(ct);

        return total;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<TransactionOutcome<T>>> operation,
        CancellationToken ct = default)
    {
        // Dùng execution strategy của context ĐẦU TIÊN — mọi context trỏ vào cùng một provider/kết
        // nối (Npgsql), nên chiến lược thử lại giống nhau cho cả nhóm.
        var strategy = _contexts[0].Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Đầu MỖI lượt thử lại — bắt buộc, tránh entity của lượt trước còn tracked sai trạng thái.
            foreach (var context in _contexts)
                context.ChangeTracker.Clear();

            // Kết nối Scoped dùng chung do UoW mở, không phải EF — nên lượt thử lại sau khi kết nối đứt phải tự mở lại.
            // Kết nối đứt có thể ở trạng thái Broken: Close trước để đưa về Closed (no-op khi đã Closed) rồi mới Open — cùng
            // cách RelationalConnection của EF xử lý kết nối Broken.
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.CloseAsync();
                await connection.OpenAsync(ct);
            }

            await using var transaction = await connection.BeginTransactionAsync(ct);

            foreach (var context in _contexts)
                await context.Database.UseTransactionAsync(transaction, ct);

            try
            {
                var outcome = await operation(ct);

                if (outcome.ShouldCommit)
                {
                    foreach (var context in _contexts)
                        await context.SaveChangesAsync(ct);

                    await CommitAsync(transaction);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                }

                return outcome.Value;
            }
            finally
            {
                foreach (var context in _contexts)
                    await context.Database.UseTransactionAsync(null, ct);
            }
        });
    }

    // Luật E12 — docs/quy-uoc/be-cqrs-handler.md §4 luật 2. Lỗi ném từ commit mà KHÔNG phải PostgresException là lỗi
    // đường truyền: không biết máy chủ đã commit hay chưa, và chạy lại operation có thể ghi hai lần (command tạo mới sinh
    // id mới ở mỗi lượt, không ràng buộc duy nhất nào chặn). Bọc thành ngoại lệ KHÔNG tạm thời để execution strategy
    // không chạy lại; đi ra thành 500. PostgresException là máy chủ ĐÃ trả lời rằng nó không commit — để nguyên cho
    // phân loại thường (40001 vẫn được thử lại).
    //
    // Luật E14 — docs/adr/0055-commit-khong-nhan-token-huy-va-het-han-mo-ket-noi-khong-thu-lai.md quyết định 1: commit nhận
    // CancellationToken.None, KHÔNG nhận token của request. Tới được đây thì client rời đi không đổi câu trả lời cho câu hỏi
    // "dữ liệu có nên được ghi không"; nhận token thì một lần huỷ đúng lúc này đi ra thành CommitOutcomeUnknownException.
    // Huỷ TRƯỚC commit (trong operation, trong SaveChangesAsync) vẫn đi như thường — không tới khối này.
    private async Task CommitAsync(DbTransaction transaction)
    {
        try
        {
            await transaction.CommitAsync(CancellationToken.None);
        }
        catch (Exception ex) when (ex is not PostgresException)
        {
            var traceId = Activity.Current?.TraceId.ToHexString();
            logger.LogError(
                ex,
                "Commit không rõ kết quả (traceId {TraceId}) — dữ liệu có thể đã được ghi; KHÔNG chạy lại đơn vị công việc. Cần người đối soát.",
                traceId);
            throw new CommitOutcomeUnknownException(traceId, ex);
        }
    }
}
