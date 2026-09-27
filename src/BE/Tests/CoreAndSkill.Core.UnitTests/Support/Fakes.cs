using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Files;

namespace CoreAndSkill.Core.UnitTests.Support;

// Đồng hồ cố định — test không phụ thuộc giờ hệ thống (cùng nguyên tắc AuditInterceptor nhận TimeProvider).
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

// ICurrentUser giả có thể đặt tay — không dùng NSubstitute ở đây vì nhiều test cần đọc lại giá trị.
internal sealed class FakeCurrentUser(Guid? userId = null, string? userName = null) : ICurrentUser
{
    public Guid? UserId { get; set; } = userId;

    public string? UserName { get; set; } = userName;
}

// Kho tệp trong bộ nhớ — seam hạ tầng được phép giả lập (luật T8). Ghi lại THỨ TỰ thao tác để test
// khẳng định "ghi tệp TRƯỚC, ghi bản ghi SAU" (14-file-storage.md §5.1).
internal sealed class InMemoryFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, byte[]> Temp { get; } = new(StringComparer.Ordinal);

    public List<string> Operations { get; } = [];

    public async Task<string> SaveAsync(Stream content, string purpose, string extension, CancellationToken ct)
    {
        var key = $"{purpose}/2026/09/{Guid.NewGuid():N}{extension}";
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        Files[key] = buffer.ToArray();
        Operations.Add("storage.save");
        return key;
    }

    public Task<Result<Stream>> OpenAsync(string key, CancellationToken ct)
        => Task.FromResult(Files.TryGetValue(key, out var bytes)
            ? Result.Success<Stream>(new MemoryStream(bytes))
            : Result.Failure<Stream>(FileErrors.ContentMissing));

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        Files.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct) => Task.FromResult(Files.ContainsKey(key));

    public async IAsyncEnumerable<StoredObject> ListAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        foreach (var key in Files.Keys)
            yield return new StoredObject(key, DateTimeOffset.UnixEpoch);
    }

    public async Task<string> SaveTempAsync(Stream content, CancellationToken ct)
    {
        var key = $"_tmp/{Guid.NewGuid():N}";
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        Temp[key] = buffer.ToArray();
        Operations.Add("storage.save-temp");
        return key;
    }

    public Task<Result<Stream>> OpenTempAsync(string tempKey, CancellationToken ct)
        => Task.FromResult(Temp.TryGetValue(tempKey, out var bytes)
            ? Result.Success<Stream>(new MemoryStream(bytes))
            : Result.Failure<Stream>(FileErrors.ContentMissing));

    public Task DeleteTempAsync(string tempKey, CancellationToken ct)
    {
        Temp.Remove(tempKey);
        return Task.CompletedTask;
    }

    public Task<int> PurgeTempAsync(DateTimeOffset olderThan, CancellationToken ct) => Task.FromResult(0);
}

internal sealed class InMemoryFileRepository(List<string>? operations = null) : IFileRepository
{
    public Dictionary<Guid, StoredFile> Files { get; } = [];

    public Task AddAsync(StoredFile file, CancellationToken ct)
    {
        Files[file.Id] = file;
        operations?.Add("repository.add");
        return Task.CompletedTask;
    }

    public Task<StoredFile?> FindByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(Files.GetValueOrDefault(id));
}

// IUnitOfWork giả: chạy operation ngay, ghi lại việc nó có được commit không — đủ để khẳng định handler /
// runner quyết ShouldCommit đúng. KHÔNG chứng minh giao dịch thật (việc của integration test — luật T8).
internal sealed class PassThroughUnitOfWork : IUnitOfWork
{
    public List<bool> Commits { get; } = [];

    public int Saves { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        Saves++;
        return Task.FromResult(0);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<TransactionOutcome<T>>> operation, CancellationToken ct = default)
    {
        var outcome = await operation(ct);
        Commits.Add(outcome.ShouldCommit);
        return outcome.Value;
    }
}

// Kho việc trong bộ nhớ, mô phỏng câu UPDATE có điều kiện của TryClaimAsync: chỉ MỘT lần claim thắng.
internal sealed class InMemoryJobRepository : CoreAndSkill.Core.Application.Jobs.IJobRepository
{
    public Dictionary<Guid, CoreAndSkill.Core.Domain.Jobs.Job> Jobs { get; } = [];

    public List<int> ProgressUpdates { get; } = [];

    public Task AddAsync(CoreAndSkill.Core.Domain.Jobs.Job job, CancellationToken ct)
    {
        Jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<CoreAndSkill.Core.Domain.Jobs.Job?> FindByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(Jobs.GetValueOrDefault(id));

    public Task<CoreAndSkill.Core.Domain.Jobs.Job?> FindForReadAsync(Guid id, CancellationToken ct)
        => Task.FromResult(Jobs.GetValueOrDefault(id));

    public Task<bool> TryClaimAsync(Guid id, DateTimeOffset now, CancellationToken ct)
    {
        if (!Jobs.TryGetValue(id, out var job) || job.Status != CoreAndSkill.Core.Domain.Jobs.JobStatus.Queued)
            return Task.FromResult(false);

        // Cùng cách EF nạp một dòng: đặt thuộc tính có setter private.
        typeof(CoreAndSkill.Core.Domain.Jobs.Job).GetProperty(nameof(CoreAndSkill.Core.Domain.Jobs.Job.Status))!
            .SetValue(job, CoreAndSkill.Core.Domain.Jobs.JobStatus.Running);
        return Task.FromResult(true);
    }

    public Task UpdateProgressAsync(Guid id, int progress, CancellationToken ct)
    {
        ProgressUpdates.Add(progress);
        return Task.CompletedTask;
    }
}

// ---- Nhập/xuất -----------------------------------------------------------------------------------

internal sealed class FakeTabularSource(
    IReadOnlyList<string> headers, IReadOnlyList<CoreAndSkill.Core.Application.Tabular.TabularRow> rows, Exception? failAfterRows = null)
    : CoreAndSkill.Core.Application.Tabular.ITabularSource
{
    public IReadOnlyList<string> Headers => headers;

    public async IAsyncEnumerable<CoreAndSkill.Core.Application.Tabular.TabularRow> ReadRowsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        foreach (var row in rows)
            yield return row;

        if (failAfterRows is not null)
            throw failAfterRows;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeTabularReader : CoreAndSkill.Core.Application.Tabular.ITabularReader
{
    private readonly IReadOnlyList<string> _headers;
    private readonly IReadOnlyList<CoreAndSkill.Core.Application.Tabular.TabularRow> _rows;
    private readonly Exception? _openFailure;
    private readonly Exception? _failAfterRows;

    public FakeTabularReader(
        string[] headers, IEnumerable<CoreAndSkill.Core.Application.Tabular.TabularRow> rows,
        Exception? openFailure = null, Exception? failAfterRows = null)
    {
        _headers = headers;
        _rows = rows.ToList();
        _openFailure = openFailure;
        _failAfterRows = failAfterRows;
    }

    public int Opens { get; private set; }

    public Task<CoreAndSkill.Core.Application.Tabular.ITabularSource> OpenAsync(Stream content, CancellationToken ct)
    {
        Opens++;
        if (_openFailure is not null)
            throw _openFailure;

        return Task.FromResult<CoreAndSkill.Core.Application.Tabular.ITabularSource>(new FakeTabularSource(_headers, _rows, _failAfterRows));
    }

    public static CoreAndSkill.Core.Application.Tabular.TabularRow Row(int number, params (string Name, string? Value)[] cells)
        => new(number, cells.ToDictionary(c => c.Name, c => c.Value, StringComparer.OrdinalIgnoreCase));
}

// Bộ ghi bảng giả — chép lại từng dòng để test đọc được thứ đã "ghi".
internal sealed class CapturingWriterFactory : CoreAndSkill.Core.Application.Tabular.ITabularWriterFactory
{
    public List<IReadOnlyList<string?>> Rows { get; } = [];

    public CoreAndSkill.Core.Application.Tabular.TabularFormat? Format { get; private set; }

    public bool Completed { get; private set; }

    public Task<CoreAndSkill.Core.Application.Tabular.ITabularWriter> CreateAsync(
        CoreAndSkill.Core.Application.Tabular.TabularFormat format, Stream output, IReadOnlyList<string> headers, CancellationToken ct)
    {
        Format = format;
        Rows.Add([.. headers]);
        return Task.FromResult<CoreAndSkill.Core.Application.Tabular.ITabularWriter>(new Writer(this, output));
    }

    private sealed class Writer(CapturingWriterFactory owner, Stream output) : CoreAndSkill.Core.Application.Tabular.ITabularWriter
    {
        public async Task WriteRowAsync(IReadOnlyList<string?> cells, CancellationToken ct)
        {
            owner.Rows.Add([.. cells]);
            // Ghi thật ra luồng để test chứng minh luồng đầu ra được dùng.
            await output.WriteAsync(System.Text.Encoding.UTF8.GetBytes(string.Join('|', cells.Select(c => c ?? string.Empty)) + "\n"), ct);
        }

        public Task CompleteAsync(CancellationToken ct)
        {
            owner.Completed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

// Ghi lại THỨ TỰ lời gọi Save/Discard để test khẳng định "huỷ theo dõi sau MỖI dòng" (§4.2, §6.1).
internal sealed class RecordingRowWriter : CoreAndSkill.Core.Application.Import.IImportRowWriter
{
    public List<string> Calls { get; } = [];

    // Dòng nào (theo thứ tự lưu, tính từ 1) thì DB từ chối.
    public HashSet<int> RejectSaveNumbers { get; } = [];

    private int _saveCount;

    public Task<Result> SaveRowAsync(CancellationToken ct)
    {
        _saveCount++;
        Calls.Add("save");
        if (RejectSaveNumbers.Contains(_saveCount))
        {
            DiscardTrackedChanges();
            return Task.FromResult(Result.Failure(CoreAndSkill.Core.Application.Import.ImportErrors.RowNotSaved));
        }

        return Task.FromResult(Result.Success());
    }

    public void DiscardTrackedChanges() => Calls.Add("discard");
}

internal sealed class ScriptedImportDefinition(string type = "x.import") : CoreAndSkill.Core.Application.Import.IImportDefinition
{
    public string Type { get; } = type;

    public IReadOnlyList<CoreAndSkill.Core.Application.Import.ImportColumn> Columns { get; } =
        [new("Email", Required: true, Example: "an@vd.vn"), new("Name", Required: false)];

    // Khoá đã có trong DB.
    public HashSet<string> ExistingKeys { get; } = [];

    // Số dòng → kết quả xử lý; dòng không khai là thành công.
    public Dictionary<int, CoreAndSkill.Core.Application.Import.ImportRowResult> Results { get; } = [];

    public List<int> Processed { get; } = [];

    public string NaturalKey(CoreAndSkill.Core.Application.Tabular.TabularRow row)
        => row.Values.TryGetValue("Email", out var email) && email is not null ? email : $"row:{row.Number}";

    public Task<bool> NaturalKeyExistsAsync(string naturalKey, CancellationToken ct)
        => Task.FromResult(ExistingKeys.Contains(naturalKey));

    public Task<CoreAndSkill.Core.Application.Import.ImportRowResult> ImportRowAsync(
        CoreAndSkill.Core.Application.Tabular.TabularRow row, CancellationToken ct)
    {
        Processed.Add(row.Number);
        return Task.FromResult(Results.GetValueOrDefault(row.Number) ?? CoreAndSkill.Core.Application.Import.ImportRowResult.Ok);
    }
}

// Đồng hồ LÁI TAY, có cả bộ đếm giờ: Task.Delay(delay, timeProvider) đặt timer qua CreateTimer, nên một fake chỉ trả
// GetUtcNow không đủ để test một handler "chờ tới mốc" — task chờ sẽ không bao giờ xong. Advance(by) đẩy đồng hồ và
// bắn mọi timer tới hạn theo thứ tự hạn. Không dùng gói Microsoft.Extensions.TimeProvider.Testing: thêm một phụ thuộc
// ngoài là quyết định kiến trúc (docs/quy-uoc/be-architecture.md §1.3), và ~50 dòng dưới đây là đủ cho nhu cầu.
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = start;

    public DateTimeOffset Start { get; } = start;

    // Mọi khoảng chờ đã được đặt qua CreateTimer — để test khẳng định "đã chờ đúng bấy nhiêu", không đoán qua hiệu ứng.
    public List<TimeSpan> RequestedDelays { get; } = [];

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
            return _now;
    }

    public override long GetTimestamp() => GetUtcNow().UtcTicks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        List<ManualTimer> due;
        lock (_gate)
        {
            _now += by;
            due = _timers.Where(t => t.DueAt is { } at && at <= _now).OrderBy(t => t.DueAt).ToList();
            foreach (var timer in due)
                timer.DueAt = null; // bắn một lần — handler không dùng timer lặp
        }

        foreach (var timer in due)
            timer.Fire();
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        public DateTimeOffset? DueAt { get; set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (owner._gate)
            {
                if (dueTime == Timeout.InfiniteTimeSpan)
                {
                    DueAt = null;
                    owner._timers.Remove(this);
                    return true;
                }

                owner.RequestedDelays.Add(dueTime);
                DueAt = owner._now + dueTime;
                if (!owner._timers.Contains(this))
                    owner._timers.Add(this);
                return true;
            }
        }

        public void Fire() => callback(state);

        public void Dispose()
        {
            lock (owner._gate)
                owner._timers.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
