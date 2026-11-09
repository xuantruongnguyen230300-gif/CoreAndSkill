using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Lệnh ghi có văn bản bắt đầu bằng `prefix` vỡ ràng buộc `constraint` với mã `sqlState` — hình dạng PostgreSQL trả khi một lượt
// khác vừa commit bản trùng. sqlState null ⇒ ném một lỗi KHÔNG phải của PostgreSQL. EF bắt ngoại lệ từ lệnh của lượt lưu và
// bọc nó thành DbUpdateException, như với lỗi thật từ Npgsql.
internal sealed class UniqueViolationOnWrite(string prefix, string table, string? sqlState, string? constraint) : DbCommandInterceptor
{
    public int Thrown { get; private set; }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        ThrowIfTargeted(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ThrowIfTargeted(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        ThrowIfTargeted(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ThrowIfTargeted(command);
        return ValueTask.FromResult(result);
    }

    private void ThrowIfTargeted(DbCommand command)
    {
        if (!command.CommandText.TrimStart().StartsWith(prefix, StringComparison.Ordinal))
            return;

        Thrown++;
        if (sqlState is null)
            throw new InvalidOperationException("Lỗi lưu không đến từ PostgreSQL.");

        throw new PostgresException(
            "duplicate key value violates unique constraint", "ERROR", "ERROR", sqlState,
            schemaName: "core", tableName: table, constraintName: constraint);
    }
}
