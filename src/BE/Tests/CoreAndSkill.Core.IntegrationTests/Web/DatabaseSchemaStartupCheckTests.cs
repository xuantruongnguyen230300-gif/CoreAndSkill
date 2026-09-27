using System.Net.Sockets;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Luật E8 lúc khởi động — docs/database/script-runbook.md §5.1–§5.3, nợ E8 ở docs/DEBT.md. DatabaseSchemaStartupCheck với bộ
// kiểm migration giả (kịch bản: lỗi kết nối / kết quả so migration theo từng lần gọi) và đồng hồ giả: Task.Delay đặt timer qua
// TimeProvider.CreateTimer, đồng hồ giả đẩy giờ đúng bằng khoảng chờ rồi bắn ngay — test không ngủ thật.
//
// Thứ nó chứng minh: lỗi kết nối tạm thời được thử lại cách nhau ConnectRetryIntervalSeconds, tổng không quá
// ConnectWaitSeconds; kết nối được thì phép kiểm E8 chạy như cũ (thiếu migration ⇒ từ chối khởi động); hết hạn ⇒ ném, thông
// điệp nêu không kết nối được DB để kiểm migration, số lần thử và tên khoá cấu hình; lỗi không tạm thời không được thử lại;
// lỗi không phải của database không bị nuốt; mỗi lần thử hỏng ghi ĐÚNG một dòng log có số lần thử — kể cả lần cuối, lần quyết
// định dừng tiến trình (mức Error; các lần còn được thử lại ở mức Warning).
// Nối vào đường khởi động thật (UseCoreAsync) — ca cuối, qua CoreWebApplicationFactory.
public sealed class DatabaseSchemaStartupCheckTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 24, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TransientFailuresThenTheDatabaseComesUp_RetriesAtTheInterval_ThenRefusesToStartOnMissingMigrations()
    {
        var verifier = new ScriptedVerifier(Transient(), Transient(), Missing("20260917043348_TaoLuocDoCore"));
        var (check, clock, logs) = Build(verifier, waitSeconds: 60, intervalSeconds: 5);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => check.RunAsync(CancellationToken.None));

        thrown.Message.ShouldContain("database thieu migration", Case.Sensitive, "kết nối được rồi thì phép kiểm E8 chạy như cũ");
        thrown.Message.ShouldContain("20260917043348_TaoLuocDoCore", Case.Sensitive);
        verifier.Calls.ShouldBe(3);
        clock.Delays.ShouldBe([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)]);
        logs.Warnings.Count(w => w.Contains("Lan thu", StringComparison.Ordinal)).ShouldBe(2);
        logs.Warnings.ShouldContain(w => w.Contains("Lan thu 1:", StringComparison.Ordinal));
        logs.Warnings.ShouldContain(w => w.Contains("Lan thu 2:", StringComparison.Ordinal));
        logs.Informations.ShouldContain(i => i.Contains("lan thu 3", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TransientFailureThenTheDatabaseComesUp_WithNoDrift_Starts()
    {
        var verifier = new ScriptedVerifier(Transient(), NoDrift());
        var (check, clock, _) = Build(verifier, waitSeconds: 60, intervalSeconds: 5);

        await Should.NotThrowAsync(() => check.RunAsync(CancellationToken.None));

        verifier.Calls.ShouldBe(2);
        clock.Delays.ShouldBe([TimeSpan.FromSeconds(5)]);
    }

    // Lần thử ở mốc 0, 5, 10, 15, 20 giây — lần cuối đúng hạn chót, hỏng thì ném.
    [Fact]
    public async Task TransientFailuresPastTheDeadline_Throw_SayingTheDatabaseIsUnreachable_WithTheAttemptsAndTheKeys()
    {
        var verifier = new ScriptedVerifier(Enumerable.Range(0, 50).Select(_ => Transient()).ToArray());
        var (check, clock, logs) = Build(verifier, waitSeconds: 20, intervalSeconds: 5);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => check.RunAsync(CancellationToken.None));

        thrown.Message.ShouldContain("khong ket noi duoc database de kiem migration", Case.Sensitive);
        thrown.Message.ShouldContain("sau 5 lan thu", Case.Sensitive);
        thrown.Message.ShouldContain("Core:SchemaCheck:ConnectWaitSeconds", Case.Sensitive);
        thrown.Message.ShouldContain("Core:SchemaCheck:ConnectRetryIntervalSeconds", Case.Sensitive);
        thrown.Message.ShouldNotContain("TU CHOI — database thieu migration", Case.Sensitive, "không kiểm được KHÔNG phải thiếu migration — hai lý do khác nhau");
        thrown.InnerException.ShouldBeAssignableTo<NpgsqlException>();
        verifier.Calls.ShouldBe(5);
        clock.Delays.Sum(d => d.TotalSeconds).ShouldBe(20);
        logs.Warnings.Count(w => w.Contains("Lan thu", StringComparison.Ordinal)).ShouldBe(4, "mỗi lần thử hỏng còn được thử lại ghi một dòng");
        logs.Errors.ShouldHaveSingleItem("lần thử CUỐI — lần quyết định dừng tiến trình — cũng có dòng của nó")
            .ShouldContain("Lan thu 5:", Case.Sensitive);
        logs.AttemptLines.ShouldBe(verifier.Calls, "mỗi lần thử hỏng đúng một dòng log");
    }

    [Fact]
    public async Task TheLastWait_IsCutToTheDeadline()
    {
        var verifier = new ScriptedVerifier(Enumerable.Range(0, 50).Select(_ => Transient()).ToArray());
        var (check, clock, _) = Build(verifier, waitSeconds: 12, intervalSeconds: 5);

        await Should.ThrowAsync<InvalidOperationException>(() => check.RunAsync(CancellationToken.None));

        clock.Delays.ShouldBe([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2)]);
        verifier.Calls.ShouldBe(4);
    }

    [Fact]
    public async Task ZeroWait_TriesExactlyOnce()
    {
        var verifier = new ScriptedVerifier(Transient(), NoDrift());
        var (check, clock, logs) = Build(verifier, waitSeconds: 0, intervalSeconds: 5);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => check.RunAsync(CancellationToken.None));

        thrown.Message.ShouldContain("sau 1 lan thu", Case.Sensitive);
        verifier.Calls.ShouldBe(1);
        clock.Delays.ShouldBeEmpty();
        logs.Errors.ShouldHaveSingleItem().ShouldContain("Lan thu 1:", Case.Sensitive);
        logs.AttemptLines.ShouldBe(1);
    }

    // Sai mật khẩu, database không tồn tại, thiếu quyền: chờ thêm không đổi được kết quả — dừng ngay.
    [Fact]
    public async Task NonTransientDatabaseFailure_ThrowsAtOnce_WithoutRetrying()
    {
        var verifier = new ScriptedVerifier(new PostgresException("password authentication failed", "FATAL", "FATAL", "28P01"), NoDrift());
        var (check, clock, logs) = Build(verifier, waitSeconds: 60, intervalSeconds: 5);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => check.RunAsync(CancellationToken.None));

        thrown.Message.ShouldContain("khong ket noi duoc database de kiem migration", Case.Sensitive);
        thrown.Message.ShouldContain("khong tam thoi", Case.Sensitive);
        verifier.Calls.ShouldBe(1);
        clock.Delays.ShouldBeEmpty();
        logs.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            e => e.ShouldContain("Lan thu 1:", Case.Sensitive),
            e => e.ShouldContain("khong tam thoi", Case.Sensitive));
        logs.Warnings.ShouldBeEmpty("lỗi không tạm thời không được thử lại — không có dòng 'thử lại sau'");
    }

    // EF có thể bọc lỗi của Npgsql — phân loại dò dọc chuỗi InnerException.
    [Fact]
    public async Task TransientFailureWrappedByAnotherException_IsStillRetried()
    {
        var verifier = new ScriptedVerifier(new InvalidOperationException("bọc bởi EF", Transient()), NoDrift());
        var (check, _, _) = Build(verifier, waitSeconds: 60, intervalSeconds: 5);

        await Should.NotThrowAsync(() => check.RunAsync(CancellationToken.None));

        verifier.Calls.ShouldBe(2);
    }

    // Lỗi không phải của database (cấu hình, lập trình) đi thẳng ra, nguyên dạng — không thử lại, không đổi thông điệp.
    [Fact]
    public async Task NonDatabaseFailure_IsNotRetried_NorRewrapped()
    {
        var original = new InvalidOperationException("lỗi cấu hình EF");
        var verifier = new ScriptedVerifier(original, NoDrift());
        var (check, clock, _) = Build(verifier, waitSeconds: 60, intervalSeconds: 5);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => check.RunAsync(CancellationToken.None));

        thrown.ShouldBeSameAs(original);
        verifier.Calls.ShouldBe(1);
        clock.Delays.ShouldBeEmpty();
    }

    // Đường khởi động thật: UseCoreAsync → DatabaseSchemaStartupCheck → ISchemaVerifier THẬT trên một chuỗi kết nối không tới
    // được. Không phải chế độ "không database" của factory (chế độ đó thay bộ kiểm), nên phép kiểm chạy thật và tiến trình
    // không lên. ConnectWaitSeconds = 0 để test không chờ.
    [Fact]
    public void UnreachableDatabase_OnTheRealStartupPath_PreventsStartup_AndSaysWhy()
    {
        using var factory = new CoreWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Core"] = "Host=127.0.0.1;Port=1;Database=e8_startup_probe;Username=test;Password=test;Timeout=2",
            ["Core:SchemaCheck:ConnectWaitSeconds"] = "0",
        });

        var thrown = Should.Throw<Exception>(() => factory.CreateClient());

        Messages(thrown).ShouldContain(m => m.Contains("khong ket noi duoc database de kiem migration", StringComparison.Ordinal));
    }

    private static (DatabaseSchemaStartupCheck Check, AutoAdvancingClock Clock, CollectingLogger Logs) Build(
        ISchemaVerifier verifier, int waitSeconds, int intervalSeconds)
    {
        var clock = new AutoAdvancingClock(Start);
        var logs = new CollectingLogger();
        var options = Options.Create(new CoreSchemaCheckOptions
        {
            ConnectWaitSeconds = waitSeconds,
            ConnectRetryIntervalSeconds = intervalSeconds,
        });

        return (new DatabaseSchemaStartupCheck(verifier, options, clock, logs), clock, logs);
    }

    // Lỗi kết nối tạm thời — hình dạng Npgsql trả khi máy chủ từ chối kết nối (NpgsqlException.IsTransient: bọc SocketException).
    private static NpgsqlException Transient()
    {
        var exception = new NpgsqlException("Failed to connect", new SocketException((int)SocketError.ConnectionRefused));
        exception.IsTransient.ShouldBeTrue("chống test rỗng: kịch bản phải thật sự là lỗi tạm thời");
        return exception;
    }

    private static IReadOnlyList<SchemaDriftReport> NoDrift() => [new SchemaDriftReport("CoreDbContext", [], [])];

    private static IReadOnlyList<SchemaDriftReport> Missing(string migration)
        => [new SchemaDriftReport("CoreDbContext", [migration], [])];

    private static IEnumerable<string> Messages(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            yield return current.Message;
    }

    // Mỗi lần gọi lấy phần tử kế tiếp của kịch bản: ngoại lệ thì ném, danh sách báo cáo thì trả.
    private sealed class ScriptedVerifier(params object[] script) : ISchemaVerifier
    {
        private readonly Queue<object> _script = new(script);

        public int Calls { get; private set; }

        public Task<IReadOnlyList<SchemaDriftReport>> InspectAsync(CancellationToken ct)
        {
            Calls++;
            return _script.Dequeue() switch
            {
                Exception ex => Task.FromException<IReadOnlyList<SchemaDriftReport>>(ex),
                IReadOnlyList<SchemaDriftReport> reports => Task.FromResult(reports),
                var other => throw new InvalidOperationException($"Kịch bản không hiểu phần tử {other}"),
            };
        }
    }

    // Đồng hồ giả tự đẩy giờ: mỗi timer đặt qua CreateTimer đẩy giờ đúng bằng khoảng chờ rồi bắn ngay (trên thread pool — không
    // bắn đồng bộ bên trong CreateTimer, trước khi Task.Delay kịp giữ timer). Ghi lại mọi khoảng chờ để test khẳng định trực tiếp.
    private sealed class AutoAdvancingClock(DateTimeOffset start) : TimeProvider
    {
        private readonly object _gate = new();
        private DateTimeOffset _now = start;

        public List<TimeSpan> Delays { get; } = [];

        public override DateTimeOffset GetUtcNow()
        {
            lock (_gate)
                return _now;
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            lock (_gate)
            {
                Delays.Add(dueTime);
                _now += dueTime;
            }

            ThreadPool.QueueUserWorkItem(_ => callback(state));
            return new FiredTimer();
        }

        private sealed class FiredTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class CollectingLogger : ILogger<DatabaseSchemaStartupCheck>
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<string> Warnings => Of(LogLevel.Warning);

        public IReadOnlyList<string> Informations => Of(LogLevel.Information);

        public IReadOnlyList<string> Errors => Of(LogLevel.Error);

        // Số dòng log nói về MỘT lần thử hỏng ("Lan thu N:"), ở mọi mức.
        public int AttemptLines
        {
            get
            {
                lock (_entries)
                    return _entries.Count(e => e.Message.StartsWith("Lan thu ", StringComparison.Ordinal));
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_entries)
                _entries.Add((logLevel, formatter(state, exception)));
        }

        private List<string> Of(LogLevel level)
        {
            lock (_entries)
                return [.. _entries.Where(e => e.Level == level).Select(e => e.Message)];
        }
    }
}
