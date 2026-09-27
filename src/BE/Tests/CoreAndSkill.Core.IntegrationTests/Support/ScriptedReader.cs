using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Trả sẵn kết quả cho từng câu đọc, THEO THỨ TỰ EF phát lệnh. Hết kịch bản thì ném — một câu đọc ngoài dự kiến phải làm
// test đỏ, không được âm thầm nhận bảng rỗng.
internal sealed class ScriptedReader(params DataTable[] tables) : DbCommandInterceptor
{
    private readonly Queue<DataTable> _remaining = new(tables);

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        => InterceptionResult<DbDataReader>.SuppressWithResult(Next(command));

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(Next(command)));

    private DbDataReader Next(DbCommand command)
        => _remaining.Count > 0
            ? _remaining.Dequeue().CreateDataReader()
            : throw new InvalidOperationException($"Câu đọc ngoài kịch bản: {command.CommandText}");
}
