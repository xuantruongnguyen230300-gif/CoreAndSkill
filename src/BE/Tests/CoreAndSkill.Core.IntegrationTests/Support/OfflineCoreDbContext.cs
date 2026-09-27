using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NSubstitute;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// CoreDbContext THẬT (mô hình, bộ lọc, ChangeTracker, interceptor thật) trên provider Npgsql, nhưng KHÔNG có database:
// mở kết nối, mở transaction và thực thi lệnh đều bị chặn ở tầng interceptor của EF. Dùng cho test chỉ cần biết
// "EF đã phát lệnh SQL nào, theo thứ tự nào" hoặc "lượt SaveChanges đã dàn dựng những dòng nào" — không cần Docker.
//
// Thứ nó KHÔNG chứng minh: câu SQL chạy được trên PostgreSQL, hay hành vi đồng thời thật. Đó là việc của test
// RequiresDocker.
internal static class OfflineCoreDbContext
{
    private const string NoDatabase = "Host=127.0.0.1;Port=1;Database=offline;Username=x;Password=x";

    public static CoreDbContext Create(Guid? tenantId, params IInterceptor[] interceptors)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);

        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(NoDatabase)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptors)
            .Options;

        return new CoreDbContext(options, tenantContext);
    }
}

// Ghi lại MỌI câu SQL EF phát ra, theo thứ tự, không câu nào tới database. Lệnh không trả dòng (ExecuteSql,
// ExecuteUpdate) coi như thành công và báo `nonQueryResult` dòng bị đổi — mặc định -1 như ExecuteSql thô; test một câu
// UPDATE có điều kiện đặt 1 (khớp) hoặc 0 (không khớp). Lệnh đọc: mặc định lệnh đầu tiên ném ReaderReached — tới đó test đã
// có đủ trình tự cần xem; `emptyReaders: true` thì mọi lệnh đọc trả tập rỗng ("database không có dòng nào khớp");
// `readers:` trả từng bảng cho từng lệnh đọc THEO THỨ TỰ, hết kịch bản thì ném ReaderReached. Khác ScriptedReader (chặn lệnh
// đọc TRƯỚC khi nó tới lệnh giả, nên không ghi lại): ở đây lệnh đọc có kịch bản VẪN nằm trong Executed, xen đúng chỗ giữa
// các lệnh không trả dòng — test khẳng định được thứ tự khoá / đọc / ghi trên cùng một danh sách.
internal sealed class SqlRecorder(int nonQueryResult = -1, bool emptyReaders = false, DataTable[]? readers = null)
{
    private readonly List<string> _executed = [];
    private readonly int _nonQueryResult = nonQueryResult;
    private readonly bool _emptyReaders = emptyReaders;
    private readonly Queue<DataTable> _scripted = new(readers ?? []);

    // Test đã mở transaction (giả) và chưa kết thúc nó. Xem CommandFaker.
    private bool _inTransaction;

    public IReadOnlyList<string> Executed => _executed;

    public IInterceptor[] Interceptors => [new ConnectionSuppressor(), new TransactionFaker(this), new CommandFaker(this)];

    public sealed class ReaderReachedException(string sql) : Exception($"Lệnh đọc đầu tiên: {sql}");

    private sealed class ConnectionSuppressor : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
            => InterceptionResult.Suppress();

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class TransactionFaker(SqlRecorder recorder) : DbTransactionInterceptor
    {
        public override InterceptionResult<DbTransaction> TransactionStarting(
            DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            recorder._inTransaction = true;
            return InterceptionResult<DbTransaction>.SuppressWithResult(new FakeTransaction(connection));
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(TransactionStarting(connection, eventData, result));

        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
            => recorder._inTransaction = false;

        public override Task TransactionCommittedAsync(
            DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            recorder._inTransaction = false;
            return Task.CompletedTask;
        }

        public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
            => recorder._inTransaction = false;

        public override Task TransactionRolledBackAsync(
            DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            recorder._inTransaction = false;
            return Task.CompletedTask;
        }
    }

    // Kết nối không bao giờ mở thật (ConnectionSuppressor), nên trạng thái của nó mãi là Closed. Mỗi lệnh, EF mở kết nối và
    // thấy nó "chưa mở" — rồi huỷ transaction đang có, vì một transaction không sống qua được một kết nối vừa đóng. Không bù
    // chỗ này thì transaction test mở chỉ sống tới lệnh SQL ĐẦU TIÊN, và mọi phép kiểm "đang trong transaction" đứng sau lệnh
    // đó thấy null — một điều database thật không bao giờ làm. Test đã mở transaction mà chưa kết thúc nó thì mở lại một
    // transaction giả ở HAI chỗ, vì EF mở kết nối ở hai thời điểm khác nhau tuỳ loại lệnh: lệnh đọc mở TRƯỚC khi dựng lệnh
    // (bù ở CommandCreating — EF gắn transaction vào lệnh ngay sau đó), lệnh không trả dòng (ExecuteSql*) dựng lệnh TRƯỚC rồi
    // mới mở (bù ở *Executing — tự gắn transaction vào lệnh).
    private sealed class CommandFaker(SqlRecorder recorder) : DbCommandInterceptor
    {
        public override InterceptionResult<DbCommand> CommandCreating(CommandCorrelatedEventData eventData, InterceptionResult<DbCommand> result)
        {
            Resume(eventData.Context, command: null);
            return InterceptionResult<DbCommand>.SuppressWithResult(new FakeCommand(recorder));
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Resume(eventData.Context, command);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Resume(eventData.Context, command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Resume(eventData.Context, command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Resume(eventData.Context, command);
            return ValueTask.FromResult(result);
        }

        private void Resume(DbContext? context, DbCommand? command)
        {
            if (!recorder._inTransaction || context is null || context.Database.CurrentTransaction is not null)
                return;

            var transaction = context.Database.BeginTransaction();
            if (command is not null)
                command.Transaction = transaction.GetDbTransaction();
        }
    }

    private sealed class FakeTransaction(DbConnection connection) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        protected override DbConnection DbConnection => connection;

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }
    }

    private sealed class FakeCommand(SqlRecorder recorder) : DbCommand
    {
        // Tham số do Npgsql dựng (EF ép kiểu về NpgsqlParameter) — mượn bộ sưu tập của một NpgsqlCommand thật.
        private readonly NpgsqlCommand _parameters = new();

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string CommandText { get; set; } = string.Empty;

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection => _parameters.Parameters;

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery()
        {
            recorder._executed.Add(Describe());
            return recorder._nonQueryResult;
        }

        public override object? ExecuteScalar()
        {
            recorder._executed.Add(Describe());
            return null;
        }

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => _parameters.CreateParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            var described = Describe();
            recorder._executed.Add(described);
            if (recorder._scripted.Count > 0)
                return recorder._scripted.Dequeue().CreateDataReader();

            return recorder._emptyReaders ? new DataTable().CreateDataReader() : throw new ReaderReachedException(described);
        }

        // Câu SQL kèm giá trị tham số — test khẳng định được cả KHOÁ truyền vào, không chỉ tên hàm.
        // Tham số mảng (id = ANY(@p)) in từng phần tử, không in tên kiểu.
        private string Describe()
            => _parameters.Parameters.Count == 0
                ? CommandText
                : $"{CommandText} -- {string.Join(", ", _parameters.Parameters.Select(p => Format(p.Value)))}";

        private static string? Format(object? value)
            => value is System.Collections.IEnumerable items and not string
                ? $"[{string.Join(", ", items.Cast<object?>())}]"
                : value?.ToString();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _parameters.Dispose();
            base.Dispose(disposing);
        }
    }
}

// Chặn SaveChanges TRƯỚC khi tới database, sau khi mọi interceptor đứng trước nó đã dàn dựng xong: chụp các dòng
// AuditLog đang ở trạng thái Added, rồi chấp nhận thay đổi như một lượt lưu thành công. PHẢI đăng ký SAU interceptor
// cần quan sát.
internal sealed class SaveCapture : SaveChangesInterceptor
{
    public List<CoreAndSkill.Core.Domain.Audit.AuditLog> AuditRows { get; } = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        => Capture(eventData.Context!);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Capture(eventData.Context!));

    private InterceptionResult<int> Capture(DbContext context)
    {
        AuditRows.AddRange(context.ChangeTracker.Entries<CoreAndSkill.Core.Domain.Audit.AuditLog>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity));

        context.ChangeTracker.AcceptAllChanges();
        return InterceptionResult<int>.SuppressWithResult(1);
    }
}

// Chặn SaveChanges TRƯỚC database như SaveCapture, và chụp tại ĐÚNG thời điểm lưu hai thứ: các lệnh SQL SqlRecorder đã ghi
// trước lượt lưu, và các thực thể đang chờ ghi kèm trạng thái. Câu INSERT/DELETE sinh ra từ ChangeTracker chỉ chạy trong
// lượt lưu — nên "lệnh X nằm trong ảnh chụp" nghĩa là "lệnh X chạy TRƯỚC câu ghi". Mỗi lượt lưu một ảnh chụp.
internal sealed class SaveProbe(SqlRecorder recorder) : SaveChangesInterceptor
{
    internal sealed record Snapshot(IReadOnlyList<string> ExecutedBefore, IReadOnlyList<(object Entity, EntityState State)> Staged)
    {
        public IReadOnlyList<T> Entities<T>(EntityState state)
            => [.. Staged.Where(s => s.State == state && s.Entity is T).Select(s => (T)s.Entity)];
    }

    public List<Snapshot> Saves { get; } = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        => Capture(eventData.Context!);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Capture(eventData.Context!));

    private InterceptionResult<int> Capture(DbContext context)
    {
        Saves.Add(new Snapshot(
            [.. recorder.Executed],
            [.. context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(e => (e.Entity, e.State))]));

        context.ChangeTracker.AcceptAllChanges();
        return InterceptionResult<int>.SuppressWithResult(1);
    }
}
