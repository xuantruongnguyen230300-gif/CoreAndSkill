using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Outbox;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Outbox;

// Bộ phát outbox khi một lần phát HỎNG — docs/wiki-core/be/12-notifications.md §2.4-§2.5. Chạy KHÔNG cần Docker: host thật
// trên bảng outbox trong bộ nhớ (OfflineOutboxHost). Cùng kịch bản trên PostgreSQL thật, gồm khoá ngoại thật:
// OutboxDatabaseTests (RequiresDocker).
//
// Hai khuôn hỏng được khoá ở đây:
//   1. Bộ lọc ngoại lệ theo KIỂU. Hết thời gian chờ của HttpClient đi ra thành TaskCanceledException (bọc TimeoutException)
//      trong khi token của vòng phát CHƯA huỷ. Lọc theo kiểu thì nó lọt qua mọi khối bắt, BackgroundService hỏng, host dừng
//      cả tiến trình (StopHost mặc định) — còn dòng outbox vẫn `pending` nên lần khởi động sau lặp lại y hệt. "Đang tắt" chỉ
//      là: token của chính vòng đó đã huỷ.
//   2. Lỗi ở pha LƯU / COMMIT — sau bên nhận, ngoài khối bắt quanh bên nhận. Không ghi lần thử hỏng thì attempt_count giữ 0,
//      dòng luôn đứng đầu lô (lô sắp theo next_attempt_at), không bao giờ thành `dead`, và mọi dòng xếp sau nó không bao
//      giờ được phát.
//
// Ghi ảnh chụp outbox (một nhịp của OutboxDispatcherHostedService gọi MeasureAsync) — biến tĩnh toàn tiến trình, nên chung
// collection với mọi lớp khác ghi nó.
[Collection(OutboxSnapshotCollection.Name)]
public sealed class OutboxDispatchFailureTests : IDisposable
{
    private const string FakeEvent = "test.fake.v1";

    private static readonly Guid TenantId = Guid.Parse("22222222-2222-7222-8222-222222222222");

    private readonly ScriptedHandler _script = new();
    private readonly OfflineOutboxHost _host;

    public OutboxDispatchFailureTests()
    {
        _host = new OfflineOutboxHost(services => services.AddScoped<IOutboxEventHandler>(sp =>
            new RecordingHandler(_script, sp.GetRequiredService<INotificationRepository>())));
        _host.Table.KnownUsers.Add(KnownUser);
    }

    private static Guid KnownUser { get; } = Guid.Parse("33333333-3333-7333-8333-333333333333");

    public void Dispose() => _host.Dispose();

    private OutboxDispatcher Dispatcher()
    {
        var dispatcher = _host.Services.GetRequiredService<OutboxDispatcher>();
        dispatcher.JitterSource = () => 0.5; // giữa dải ±20% => khoảng lùi đúng bằng giá trị danh định
        return dispatcher;
    }

    private (OutboxDispatcherHostedService Service, CollectingLogger<OutboxDispatcherHostedService> Logger) Loop()
    {
        _ = Dispatcher();
        var logger = new CollectingLogger<OutboxDispatcherHostedService>();
        return (ActivatorUtilities.CreateInstance<OutboxDispatcherHostedService>(_host.Services, logger), logger);
    }

    // Dòng tới hạn theo thứ tự thêm vào: dòng thêm trước đứng trước trong lô.
    private Guid Pending(string kind)
    {
        var id = _host.Table.Add(TenantId, FakeEvent, $"{{\"kind\":\"{kind}\"}}", _host.Clock.GetUtcNow(), KnownUser);
        _host.Clock.Advance(TimeSpan.FromSeconds(1));
        return id;
    }

    private int MaxAttempts => _host.Services.GetRequiredService<IOptions<CoreOutboxOptions>>().Value.MaxAttempts;

    // Một nhịp không được quay mãi — lô chỉ cạn khi mọi dòng đã xử lý đều rời khỏi tập tới hạn.
    private static Task Within(Task work) => work.WaitAsync(TimeSpan.FromSeconds(30));

    // ---- 1. Lọc theo token, không theo kiểu --------------------------------------------------------------------------

    [Fact]
    public async Task AHandlerTimingOutAsTaskCanceled_WhileTheLoopIsNotStopping_IsTransient_TheLoopLives_AndTheRowWaitsWithOneAttempt()
    {
        var id = Pending(ScriptedHandler.TimeOut);
        var before = _host.Clock.GetUtcNow();
        var (loop, logger) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None)); // KHÔNG ném — vòng phát sống tiếp

        var row = _host.Table.Get(id);
        row.Status.ShouldBe(OutboxStatus.Pending);
        row.AttemptCount.ShouldBe(1);
        row.NextAttemptAt.ShouldNotBeNull().ShouldBeGreaterThan(before, "lỗi tạm thời: lùi dần, không thử lại ngay");
        row.LastError.ShouldNotBeNull().ShouldContain(nameof(TaskCanceledException));
        logger.Entries.ShouldNotContain(e => e.Level == LogLevel.Error, "một lần phát hỏng không phải một nhịp hỏng");
        _script.Calls.ShouldBe(1);
    }

    // Chiều ngược lại — thiếu nó thì một bộ lọc nuốt MỌI ngoại lệ cũng làm test trên xanh.
    [Fact]
    public async Task WhenTheLoopsOwnTokenIsCancelled_TheCancellationPropagates_AndNoAttemptIsBurnt()
    {
        var id = Pending(ScriptedHandler.Stop);
        var (loop, _) = Loop();
        using var stopping = new CancellationTokenSource();
        _script.Stopping = stopping;

        await Should.ThrowAsync<OperationCanceledException>(() => Within(loop.RunOnceAsync(stopping.Token)));

        var row = _host.Table.Get(id);
        row.Status.ShouldBe(OutboxStatus.Pending);
        row.AttemptCount.ShouldBe(0, "tiến trình dừng không phải lỗi của dòng — không được đốt một lần thử");
    }

    // ---- 2. Lỗi ở pha lưu / commit -----------------------------------------------------------------------------------

    // Bên nhận tạo thông báo cho một người dùng không tồn tại: SaveChanges gặp 23503. Máy chủ đã trả lời và từ chối bằng một
    // mã không tạm thời — thử lại y như cũ sẽ bị từ chối y như cũ, nên dòng chết ngay ở lần thử đầu.
    [Fact]
    public async Task ASaveRejectedByAForeignKey_BurnsAnAttempt_EndsDead_AndTheValidRowQueuedBehindIsStillDone()
    {
        var poison = Pending(ScriptedHandler.UnknownRecipient);
        var valid = Pending(ScriptedHandler.Ok);
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var dead = _host.Table.Get(poison);
        dead.Status.ShouldBe(OutboxStatus.Dead);
        dead.AttemptCount.ShouldBe(1);
        dead.NextAttemptAt.ShouldBeNull();
        dead.LastError.ShouldNotBeNull().ShouldContain(PostgresErrorCodes.ForeignKeyViolation, Case.Sensitive,
            "last_error phải mang câu trả lời của máy chủ, không phải câu chung của lớp bọc DbUpdateException");

        _host.Table.Get(valid).Status.ShouldBe(OutboxStatus.Done, "một dòng hỏng không được chặn dòng xếp sau nó");
        _script.Calls.ShouldBe(2, "23503 không tạm thời — chiến lược thử lại không được chạy lại bên nhận của dòng hỏng");
    }

    // Commit hỏng vì đường truyền: UnitOfWork bọc thành CommitOutcomeUnknownException (luật E12). Không biết máy chủ đã commit
    // chưa — thử lại có lùi dần; nếu nó đã commit thì lần sau dòng không còn `pending` và không bị phát lại. Lỗi cứ lặp lại thì
    // dòng chết đúng sau MaxAttempts lần.
    [Fact]
    public async Task ACommitWithUnknownOutcome_IsTransient_BurnsOneAttemptPerTry_AndEndsDeadAfterMaxAttempts()
    {
        var failing = Pending(ScriptedHandler.Ok);
        var valid = Pending(ScriptedHandler.Ok);
        _host.Table.FailCommitsWriting(
            failing,
            when: row => row.Status == OutboxStatus.Done,
            failure: () => new NpgsqlException("Đứt kết nối lúc commit.", new IOException("mất gói")));
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var first = _host.Table.Get(failing);
        first.Status.ShouldBe(OutboxStatus.Pending);
        first.AttemptCount.ShouldBe(1);
        first.LastError.ShouldNotBeNull().ShouldContain(nameof(CommitOutcomeUnknownException));
        _host.Table.Get(valid).Status.ShouldBe(OutboxStatus.Done);

        for (var attempt = 2; attempt <= MaxAttempts; attempt++)
        {
            _host.Clock.Advance(TimeSpan.FromHours(1));
            await Within(loop.RunOnceAsync(CancellationToken.None));
        }

        var last = _host.Table.Get(failing);
        last.Status.ShouldBe(OutboxStatus.Dead);
        last.AttemptCount.ShouldBe(MaxAttempts);
    }

    // Không ghi được cả lần thử hỏng (database rớt đúng lúc đó): dòng nằm nguyên — không còn gì trung thực để ghi, và không
    // được đốt một lần thử mà không lưu được. Nhịp phải NÓI RA (log Error của vòng phát) và phải KẾT THÚC — không quay lại
    // dòng vẫn còn tới hạn đó ngay trong nhịp. Test canh, không phải test đỏ-rồi-xanh: code cũ cũng dừng nhịp ở đây.
    [Fact]
    public async Task WhenEvenTheFailedAttemptCannotBeRecorded_TheRowStaysAsIs_AndTheTickEndsLoudly_WithoutSpinning()
    {
        var failing = Pending(ScriptedHandler.Ok);
        _host.Table.FailCommitsWriting(
            failing,
            when: _ => true,
            failure: () => new NpgsqlException("Đứt kết nối lúc commit.", new IOException("mất gói")));
        var (loop, logger) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var stuck = _host.Table.Get(failing);
        stuck.Status.ShouldBe(OutboxStatus.Pending);
        stuck.AttemptCount.ShouldBe(0);
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains("outbox", StringComparison.OrdinalIgnoreCase));
        _script.Calls.ShouldBe(1, "nhịp không được phát lại dòng đó ngay trong nhịp");
    }

    internal sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    // Hành vi chọn theo thân sự kiện; bản scoped chỉ chuyển tiếp tới đây.
    private sealed class ScriptedHandler
    {
        public const string Ok = "ok";
        public const string TimeOut = "timeout";
        public const string Stop = "stop";
        public const string UnknownRecipient = "unknown-recipient";

        public int Calls { get; private set; }

        public CancellationTokenSource? Stopping { get; set; }

        public async Task<Result> InvokeAsync(OutboxEnvelope envelope, INotificationRepository notifications, CancellationToken ct)
        {
            Calls++;
            await Task.Yield();

            if (envelope.PayloadJson.Contains(TimeOut, StringComparison.Ordinal))
                throw new TaskCanceledException("", new TimeoutException()); // hình dạng HttpClient.Timeout để lại

            if (envelope.PayloadJson.Contains(Stop, StringComparison.Ordinal))
            {
                Stopping!.Cancel();
                throw new OperationCanceledException(Stopping.Token);
            }

            if (envelope.PayloadJson.Contains(UnknownRecipient, StringComparison.Ordinal))
            {
                var notification = Notification.Create("test.thong-bao", null, null).Value;
                var recipient = NotificationRecipient.Create(notification.Id, Guid.NewGuid()).Value; // người dùng không tồn tại
                await notifications.AddAsync(notification, [recipient], ct);
            }

            return Result.Success();
        }
    }

    private sealed class RecordingHandler(ScriptedHandler script, INotificationRepository notifications) : IOutboxEventHandler
    {
        public string EventType => FakeEvent;

        public Task<Result> HandleAsync(OutboxEnvelope envelope, CancellationToken ct) => script.InvokeAsync(envelope, notifications, ct);
    }
}
