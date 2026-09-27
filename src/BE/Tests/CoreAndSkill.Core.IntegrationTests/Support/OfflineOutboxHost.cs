using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Host THẬT của ứng dụng — DI, CoreDbContext cùng mọi interceptor của nó, UnitOfWork, chiến lược thử lại của production
// (CoreExecutionStrategy), OutboxDispatcher — đặt trên một bảng outbox TRONG BỘ NHỚ thay cho PostgreSQL. Không cần Docker.
//
// Cách làm: DbConnection scoped (thứ UnitOfWork và CoreDbContext dùng chung) được thay bằng một kết nối giả. Kết nối giả trả
// dòng của bảng trong bộ nhớ cho đúng những câu đọc mà bộ phát phát ra, và ném cho mọi câu khác. SaveChanges bị chặn trước
// database, ở cuối chuỗi interceptor của ứng dụng: thay đổi của dòng outbox được giữ lại trong transaction giả và chỉ vào
// bảng khi transaction commit, như database thật. Một dòng người nhận thông báo trỏ tới người dùng không có trong
// KnownUsers bị từ chối bằng đúng hình dạng EF để lại cho vi phạm khoá ngoại: DbUpdateException bọc PostgresException 23503.
//
// Thứ nó chứng minh: luồng điều khiển của bộ phát — ngoại lệ ở pha nào đi đâu, lần thử hỏng có được ghi không, dòng xếp sau
// có còn được phát không. Thứ nó KHÔNG chứng minh: SQL chạy được trên PostgreSQL, FOR UPDATE SKIP LOCKED, ràng buộc khoá
// ngoại thật — việc của OutboxDatabaseTests (RequiresDocker).
internal sealed class OfflineOutboxHost : IDisposable
{
    private bool _offline;

    public OfflineOutboxHost(Action<IServiceCollection>? configure = null)
    {
        Factory = new CoreWebApplicationFactory(
            configureServices: services =>
            {
                // Mọi dịch vụ nền của Core.Infrastructure bị gỡ (cùng lý do với B4DockerHost): bộ phát nền quét mỗi vài giây
                // và sẽ tranh dòng với lời gọi tay của test.
                var infrastructureAssembly = typeof(CoreDbContext).Assembly;
                foreach (var descriptor in services
                             .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Assembly == infrastructureAssembly)
                             .ToList())
                {
                    services.Remove(descriptor);
                }

                // Kết nối giả chỉ bật SAU khi host đã khởi động — phép kiểm lúc khởi động đi đường kết nối thật (không tới
                // được) như mọi test dùng factory mặc định.
                services.AddScoped<DbConnection>(_ => _offline
                    ? new OfflineConnection(Table)
                    : new NpgsqlConnection(CoreWebApplicationFactory.UnreachableConnectionString));
                services.ConfigureDbContext<CoreDbContext>(options => options.AddInterceptors(new TableWriter(Table)));
                services.AddSingleton<TimeProvider>(Clock);
                configure?.Invoke(services);
            },
            keepProductionRetryStrategy: true);

        _ = Factory.Services;
        _offline = true;
    }

    public OfflineOutboxTable Table { get; } = new();

    public MutableClock Clock { get; } = new(new DateTimeOffset(2026, 9, 26, 3, 0, 0, TimeSpan.Zero));

    public CoreWebApplicationFactory Factory { get; }

    public IServiceProvider Services => Factory.Services;

    public void Dispose() => Factory.Dispose();

    // Chặn SaveChanges SAU mọi interceptor của ứng dụng (ConfigureDbContext nối vào cuối chuỗi), rồi chấp nhận thay đổi như
    // một lượt lưu thành công. Chỉ dòng outbox được ghi; các thực thể khác chỉ cần qua được ràng buộc khoá ngoại giả.
    private sealed class TableWriter(OfflineOutboxTable table) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => Write(eventData.Context!);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Write(eventData.Context!));

        private InterceptionResult<int> Write(DbContext context)
        {
            var staged = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(e => e.Entity)
                .ToList();

            if (staged.OfType<NotificationRecipient>().Any(r => !table.KnownUsers.Contains(r.UserId)))
            {
                throw new DbUpdateException(
                    "An error occurred while saving the entity changes. See the inner exception for details.",
                    new PostgresException(
                        "insert or update on table \"notification_recipient\" violates foreign key constraint", "ERROR", "ERROR",
                        PostgresErrorCodes.ForeignKeyViolation,
                        schemaName: "core", tableName: "notification_recipient", constraintName: "fk_notification_recipient_user_id"));
            }

            var transaction = context.Database.CurrentTransaction?.GetDbTransaction() as OfflineTransaction
                ?? throw new InvalidOperationException("Bộ phát chỉ được ghi bên trong IUnitOfWork.ExecuteInTransactionAsync.");

            foreach (var message in staged.OfType<OutboxMessage>())
                transaction.Stage(OfflineOutboxTable.Row.From(message));

            context.ChangeTracker.AcceptAllChanges();
            return InterceptionResult<int>.SuppressWithResult(staged.Count);
        }
    }

    private sealed class OfflineConnection(OfflineOutboxTable table) : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "offline";

        public override string DataSource => "offline";

        public override string ServerVersion => "16.0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open() => _state = ConnectionState.Open;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new OfflineTransaction(this, table);

        protected override DbCommand CreateDbCommand() => new OfflineCommand(table) { Connection = this };
    }

    private sealed class OfflineTransaction(DbConnection connection, OfflineOutboxTable table) : DbTransaction
    {
        private readonly List<OfflineOutboxTable.Row> _staged = [];

        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        protected override DbConnection DbConnection => connection;

        public void Stage(OfflineOutboxTable.Row row) => _staged.Add(row);

        public override void Commit()
        {
            var staged = _staged.ToList();
            _staged.Clear();
            table.Commit(staged);
        }

        public override void Rollback() => _staged.Clear();
    }

    private sealed class OfflineCommand(OfflineOutboxTable table) : DbCommand
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

        public override int ExecuteNonQuery() => table.NonQuery(CommandText);

        public override object? ExecuteScalar() => throw new NotSupportedException($"Câu ngoài kịch bản: {CommandText}");

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => _parameters.CreateParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => table.Read(CommandText, [.. _parameters.Parameters.Select(p => p.Value)]);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _parameters.Dispose();
            base.Dispose(disposing);
        }
    }
}

// Bảng core.outbox_message trong bộ nhớ, trả lời đúng những câu bộ phát và phép đo của nó phát ra. Câu đọc chỉ thấy dòng
// có tenant_id nằm trong tham số của câu — tức bộ lọc đơn vị có hiệu lực — trừ hai câu cố ý quét toàn hệ (lô tới hạn và
// phép đo), vốn bỏ bộ lọc đó.
internal sealed class OfflineOutboxTable
{
    private static readonly Dictionary<string, (Type Type, Func<Row, object?> Read)> Columns = new(StringComparer.Ordinal)
    {
        ["id"] = (typeof(Guid), r => r.Id),
        ["tenant_id"] = (typeof(Guid), r => r.TenantId),
        ["occurred_at"] = (typeof(DateTimeOffset), r => r.OccurredAt),
        ["event_type"] = (typeof(string), r => r.EventType),
        ["payload"] = (typeof(string), r => r.Payload),
        ["trace_id"] = (typeof(string), r => r.TraceId),
        ["triggered_by_user_id"] = (typeof(Guid), r => r.TriggeredByUserId),
        ["triggered_by_user_name"] = (typeof(string), r => r.TriggeredByUserName),
        ["status"] = (typeof(string), r => r.Status),
        ["processed_at"] = (typeof(DateTimeOffset), r => r.ProcessedAt),
        ["attempt_count"] = (typeof(int), r => r.AttemptCount),
        ["next_attempt_at"] = (typeof(DateTimeOffset), r => r.NextAttemptAt),
        ["last_error"] = (typeof(string), r => r.LastError),
    };

    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, Row> _rows = [];
    private readonly Dictionary<Guid, (Func<Row, bool> When, Func<Exception> Failure)> _commitFailures = [];

    // Người dùng có thật — dòng người nhận trỏ ra ngoài tập này vi phạm khoá ngoại.
    public HashSet<Guid> KnownUsers { get; } = [];

    public Guid Add(Guid tenantId, string eventType, string payload, DateTimeOffset occurredAt, Guid? triggeredByUserId = null)
    {
        var message = OutboxMessage.Create(tenantId, occurredAt, eventType, payload, null, triggeredByUserId, triggeredByUserId is null ? null : "an.nv").Value;
        lock (_gate)
            _rows[message.Id] = Row.From(message);

        return message.Id;
    }

    public Row Get(Guid id)
    {
        lock (_gate)
            return _rows[id] with { };
    }

    // Mọi lần commit ghi dòng `id` ở dạng thoả `when` đều ném ngoại lệ do `failure` dựng — thay đổi của lần commit đó không
    // vào bảng, như một commit hỏng thật.
    public void FailCommitsWriting(Guid id, Func<Row, bool> when, Func<Exception> failure)
    {
        lock (_gate)
            _commitFailures[id] = (when, failure);
    }

    internal void Commit(IReadOnlyList<Row> staged)
    {
        lock (_gate)
        {
            foreach (var row in staged)
            {
                if (_commitFailures.TryGetValue(row.Id, out var rule) && rule.When(row))
                    throw rule.Failure();
            }

            foreach (var row in staged)
                _rows[row.Id] = row;
        }
    }

    // Câu không trả dòng duy nhất bộ phát phát ra: dọn dòng `done` quá hạn (ExecuteDelete). Không dòng nào đủ tuổi trong test.
    internal int NonQuery(string sql)
        => sql.StartsWith("DELETE FROM core.outbox_message", StringComparison.Ordinal)
            ? 0
            : throw new NotSupportedException($"Câu ngoài kịch bản: {sql}");

    internal DbDataReader Read(string sql, IReadOnlyList<object?> parameters)
    {
        if (!sql.Contains("core.outbox_message", StringComparison.Ordinal))
            throw new NotSupportedException($"Câu đọc ngoài kịch bản: {sql}");

        lock (_gate)
        {
            var onlyPending = sql.Contains("status = 'pending'", StringComparison.Ordinal);

            if (sql.Contains("count(", StringComparison.Ordinal))
            {
                var status = onlyPending ? OutboxStatus.Pending : OutboxStatus.Dead;
                return Single(typeof(long), (long)_rows.Values.Count(r => r.Status == status));
            }

            if (sql.Contains("min(", StringComparison.Ordinal))
            {
                var oldest = _rows.Values.Where(r => r.Status == OutboxStatus.Pending).Select(r => (DateTimeOffset?)r.OccurredAt).Min();
                return Single(typeof(DateTimeOffset), oldest);
            }

            IEnumerable<Row> rows;
            if (sql.Contains("ORDER BY", StringComparison.Ordinal))
            {
                // Lô tới hạn — toàn hệ: status = 'pending' AND next_attempt_at <= @now ORDER BY next_attempt_at LIMIT @n.
                var now = parameters.OfType<DateTimeOffset>().Single();
                var limit = parameters.OfType<int>().Single();
                rows = _rows.Values
                    .Where(r => r.Status == OutboxStatus.Pending && r.NextAttemptAt <= now)
                    .OrderBy(r => r.NextAttemptAt)
                    .Take(limit);
            }
            else
            {
                // Một dòng theo khoá: câu khoá FOR UPDATE, hoặc câu đọc lại để ghi lần thử hỏng. Cả hai đi qua bộ lọc đơn vị.
                var guids = parameters.OfType<Guid>().ToHashSet();
                rows = _rows.Values.Where(r => guids.Contains(r.Id) && guids.Contains(r.TenantId));
            }

            if (onlyPending)
                rows = rows.Where(r => r.Status == OutboxStatus.Pending);

            return Table(SelectList(sql), rows.ToList());
        }
    }

    // Tên cột theo ĐÚNG thứ tự EF đọc: danh sách sau SELECT ngoài cùng, bỏ bí danh bảng.
    private static List<string> SelectList(string sql)
    {
        var start = sql.IndexOf("SELECT ", StringComparison.Ordinal) + "SELECT ".Length;
        var end = sql.IndexOf("FROM", start, StringComparison.Ordinal);

        return [.. sql[start..end]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(column => column[(column.LastIndexOf('.') + 1)..].Trim('"'))];
    }

    private static DataTableReader Table(IReadOnlyList<string> columns, IReadOnlyList<Row> rows)
    {
        var table = new DataTable();
        foreach (var column in columns)
        {
            if (!Columns.TryGetValue(column, out var definition))
                throw new NotSupportedException($"Cột ngoài kịch bản: {column}");

            table.Columns.Add(column, definition.Type);
        }

        foreach (var row in rows)
            table.Rows.Add([.. columns.Select(column => Columns[column].Read(row) ?? DBNull.Value)]);

        return table.CreateDataReader();
    }

    private static DataTableReader Single(Type type, object? value)
    {
        var table = new DataTable();
        table.Columns.Add("value", type);
        table.Rows.Add(value ?? DBNull.Value);
        return table.CreateDataReader();
    }

    internal sealed record Row(
        Guid Id,
        Guid TenantId,
        DateTimeOffset OccurredAt,
        string EventType,
        string Payload,
        string? TraceId,
        Guid? TriggeredByUserId,
        string? TriggeredByUserName,
        string Status,
        DateTimeOffset? ProcessedAt,
        int AttemptCount,
        DateTimeOffset? NextAttemptAt,
        string? LastError)
    {
        public static Row From(OutboxMessage m)
            => new(m.Id, m.TenantId, m.OccurredAt, m.EventType, m.Payload, m.TraceId, m.TriggeredByUserId, m.TriggeredByUserName,
                m.Status, m.ProcessedAt, m.AttemptCount, m.NextAttemptAt, m.LastError);
    }
}
