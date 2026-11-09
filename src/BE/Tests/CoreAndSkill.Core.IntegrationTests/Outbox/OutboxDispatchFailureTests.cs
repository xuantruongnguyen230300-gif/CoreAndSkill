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
using Microsoft.EntityFrameworkCore;
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

    // ---- 3. Phân loại lỗi dùng CHUNG cho bên nhận và pha lưu/commit (B30, quyết định 2026-10-04) ----------------------------
    //
    // Một phân loại: PostgresException.IsTransient của Npgsql, cộng 57014 (statement_timeout) coi là tạm thời. Tạm thời thì lùi
    // dần tới trần số lần rồi mới `dead`; không tạm thời (vd. 23514) thì `dead` ngay.

    [Fact]
    public async Task AHandlerThrowingANonTransientPostgresError_23514_EndsDeadAtOnce_AndTheRowQueuedBehindIsStillDone()
    {
        var poison = Pending(ScriptedHandler.Pg23514);
        var valid = Pending(ScriptedHandler.Ok);
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var dead = _host.Table.Get(poison);
        dead.Status.ShouldBe(OutboxStatus.Dead, "23514 (vi phạm CHECK) — thử lại y như cũ sẽ bị từ chối y như cũ");
        dead.AttemptCount.ShouldBe(1);
        dead.NextAttemptAt.ShouldBeNull();
        dead.LastError.ShouldNotBeNull().ShouldContain(PostgresErrorCodes.CheckViolation, Case.Sensitive);
        _host.Table.Get(valid).Status.ShouldBe(OutboxStatus.Done);
        _script.Calls.ShouldBe(2, "dòng chết ngay không được phát lại trong nhịp");
    }

    [Fact]
    public async Task AHandlerThrowingAStatementTimeout_57014_StaysPending_WithABackoff()
    {
        var id = Pending(ScriptedHandler.Pg57014);
        var before = _host.Clock.GetUtcNow();
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var row = _host.Table.Get(id);
        row.Status.ShouldBe(OutboxStatus.Pending, "57014 là hết thời hạn chờ của câu lệnh — tạm thời theo quyết định 2026-10-04");
        row.AttemptCount.ShouldBe(1);
        row.NextAttemptAt.ShouldNotBeNull().ShouldBeGreaterThan(before, "lùi dần, không thử lại ngay");
        _script.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task AHandlerThrowingATransientNpgsqlError_40001_StaysPending_WithABackoff()
    {
        var id = Pending(ScriptedHandler.Pg40001);
        var before = _host.Clock.GetUtcNow();
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var row = _host.Table.Get(id);
        row.Status.ShouldBe(OutboxStatus.Pending);
        row.AttemptCount.ShouldBe(1);
        row.NextAttemptAt.ShouldNotBeNull().ShouldBeGreaterThan(before);
    }

    // Lỗi tạm thời của bên nhận lặp mãi thì dòng chết đúng sau MaxAttempts lần, không sớm hơn.
    [Fact]
    public async Task AHandlerThrowing57014EveryTime_EndsDeadOnlyAfterMaxAttempts()
    {
        var id = Pending(ScriptedHandler.Pg57014);
        var (loop, _) = Loop();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            _host.Table.Get(id).Status.ShouldBe(OutboxStatus.Pending, $"trước lần thử thứ {attempt}");
            _host.Clock.Advance(TimeSpan.FromHours(1));
            await Within(loop.RunOnceAsync(CancellationToken.None));
        }

        var last = _host.Table.Get(id);
        last.Status.ShouldBe(OutboxStatus.Dead);
        last.AttemptCount.ShouldBe(MaxAttempts);
    }

    // Pha lưu/commit cùng phân loại: 57014 ở commit cũng là tạm thời. Hôm nay StoreFailure xếp nó `dead` ngay vì Npgsql không
    // coi 57014 là tạm thời.
    [Fact]
    public async Task AStatementTimeout_57014_AtCommit_IsTransient_StaysPending_WithOneAttempt()
    {
        var failing = Pending(ScriptedHandler.Ok);
        _host.Table.FailCommitsWriting(
            failing,
            when: row => row.Status == OutboxStatus.Done,
            failure: () => ScriptedHandler.PostgresError(PostgresErrorCodes.QueryCanceled));
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var row = _host.Table.Get(failing);
        row.Status.ShouldBe(OutboxStatus.Pending);
        row.AttemptCount.ShouldBe(1);
        row.NextAttemptAt.ShouldNotBeNull();
    }

    // Chiều ngược lại: lỗi không tạm thời ở commit vẫn `dead` ngay (đã đúng hôm nay — ca giữ cho hai nhánh không lệch nhau
    // khi một trong hai được viết lại).
    [Fact]
    public async Task ACheckViolation_23514_AtCommit_EndsDeadAtOnce()
    {
        var failing = Pending(ScriptedHandler.Ok);
        _host.Table.FailCommitsWriting(
            failing,
            when: row => row.Status == OutboxStatus.Done,
            failure: () => ScriptedHandler.PostgresError(PostgresErrorCodes.CheckViolation));
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var row = _host.Table.Get(failing);
        row.Status.ShouldBe(OutboxStatus.Dead);
        row.AttemptCount.ShouldBe(1);
    }

    // Ghim lý do của ngoại lệ 57014: Npgsql KHÔNG coi nó là tạm thời — nên phân loại chung phải cộng thêm nó. Nâng Npgsql mà
    // dòng này đổi thì ngoại lệ không còn cần thiết: ca này đỏ để người nâng bỏ phần cộng thêm.
    [Fact]
    public void Pin_NpgsqlDoesNotTreat57014AsTransient_WhichIsWhyTheOutboxClassificationAddsIt()
    {
        ScriptedHandler.PostgresError(PostgresErrorCodes.QueryCanceled).IsTransient.ShouldBeFalse();
        ScriptedHandler.PostgresError(PostgresErrorCodes.CheckViolation).IsTransient.ShouldBeFalse("đối chứng cho ca 23514");
        ScriptedHandler.PostgresError(PostgresErrorCodes.SerializationFailure).IsTransient.ShouldBeTrue("đối chứng cho ca 40001");
    }

    // ---- 3b. Ngoại lệ bọc: phân loại theo PostgresException TRONG CHUỖI LỖI, không theo kiểu ngoài cùng ---------------------
    //
    // Hình dạng EF Core để lại là DbUpdateException bọc PostgresException. Phân loại phải đi xuống InnerException tìm nó.

    [Fact]
    public async Task AHandlerThrowingDbUpdateExceptionWrapping23514_EndsDeadAtOnce_AndLastErrorCarriesTheServerAnswer()
    {
        var poison = Pending(ScriptedHandler.WrappedCheck);
        var valid = Pending(ScriptedHandler.Ok);
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var dead = _host.Table.Get(poison);
        dead.Status.ShouldBe(OutboxStatus.Dead, "PostgresException 23514 trong DbUpdateException — thử lại y như cũ sẽ bị từ chối y như cũ");
        dead.AttemptCount.ShouldBe(1);
        dead.NextAttemptAt.ShouldBeNull();
        dead.LastError.ShouldNotBeNull().ShouldContain(PostgresErrorCodes.CheckViolation, Case.Sensitive);
        _host.Table.Get(valid).Status.ShouldBe(OutboxStatus.Done);
    }

    [Fact]
    public async Task AHandlerThrowingDbUpdateExceptionWrapping57014_StaysPending_WithABackoff()
    {
        var id = Pending(ScriptedHandler.WrappedQueryCancel);
        var before = _host.Clock.GetUtcNow();
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var row = _host.Table.Get(id);
        row.Status.ShouldBe(OutboxStatus.Pending, "57014 trong DbUpdateException vẫn là hết thời hạn chờ — tạm thời");
        row.AttemptCount.ShouldBe(1);
        row.NextAttemptAt.ShouldNotBeNull().ShouldBeGreaterThan(before);
    }

    // AggregateException mà PostgresException là ngoại lệ ĐẦU TIÊN (đúng InnerException): nằm trong chuỗi lỗi nên được nhận ra.
    [Fact]
    public async Task AHandlerThrowingAnAggregateExceptionWhoseInnerIsANonTransientPostgresError_EndsDeadAtOnce()
    {
        var id = Pending(ScriptedHandler.AggregateSingle);
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var row = _host.Table.Get(id);
        row.Status.ShouldBe(OutboxStatus.Dead);
        row.AttemptCount.ShouldBe(1);
    }

    // GHI KẾT QUẢ THỰC TẾ: AggregateException(IOException, PostgresException 23514). Phân loại đi theo InnerException (chỉ ngoại lệ
    // ĐẦU của AggregateException), KHÔNG đi InnerExceptions — nên PostgresException ở vị trí thứ hai không được thấy và dòng bị
    // coi là TẠM THỜI (lùi dần tới MaxAttempts rồi mới `dead`). Khớp đúng câu chữ quyết định "phân loại theo PostgresException
    // trong CHUỖI LỖI" (chuỗi = InnerException), nhưng một AggregateException nhiều nhánh có thể giấu một lần từ chối vĩnh viễn
    // sau một nhánh tạm thời. Ca này GHIM hành vi hôm nay; nếu người quyết định muốn đi cả InnerExceptions, đổi kỳ vọng ở đây.
    [Fact]
    public async Task AHandlerThrowingAnAggregateExceptionWithThePostgresErrorOnlyInTheSecondBranch_IsTreatedAsTransient_Pinned()
    {
        var id = Pending(ScriptedHandler.AggregateMixed);
        var (loop, _) = Loop();

        await Within(loop.RunOnceAsync(CancellationToken.None));

        var row = _host.Table.Get(id);
        row.Status.ShouldBe(OutboxStatus.Pending);
        row.AttemptCount.ShouldBe(1);
    }

    // ---- 4. Dòng bị phiên khác giữ khoá không được đếm là đã xử lý (B31) -----------------------------------------------
    //
    // `FOR UPDATE SKIP LOCKED` không lấy được khoá thì dòng đó KHÔNG được tính vào kết quả của DispatchBatchAsync (0 nghĩa là
    // không còn gì để xử lý lúc này), nếu không vòng `while (… > 0)` của nhịp quay liên tục tới khi phiên kia nhả khoá.
    // Bản PostgreSQL thật của kịch bản: OutboxDatabaseTests (RequiresDocker).

    [Fact]
    public async Task WhenAnotherSessionHoldsTheOnlyDueRow_DispatchBatchReturnsZero_AndDoesNotCallTheHandler()
    {
        var id = Pending(ScriptedHandler.Ok);
        _host.Table.LockedByOthers.Add(id);

        var processed = await Dispatcher().DispatchBatchAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30));

        processed.ShouldBe(0, "dòng bị bỏ qua vì khoá không phải dòng đã xử lý");
        _script.Calls.ShouldBe(0);
        _host.Table.Get(id).Status.ShouldBe(OutboxStatus.Pending);
    }

    [Fact]
    public async Task WithOneLockedRowAndOneFreeRow_DispatchBatchCountsOnlyTheRowItProcessed()
    {
        var locked = Pending(ScriptedHandler.Ok);
        var free = Pending(ScriptedHandler.Ok);
        _host.Table.LockedByOthers.Add(locked);

        var processed = await Dispatcher().DispatchBatchAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30));

        processed.ShouldBe(1);
        _host.Table.Get(free).Status.ShouldBe(OutboxStatus.Done);
        _host.Table.Get(locked).Status.ShouldBe(OutboxStatus.Pending);
    }

    // Giới hạn thời gian để ĐỎ thay vì treo: nhịp quay vòng chạy tới khi token này huỷ (rồi ném OperationCanceledException).
    [Fact]
    public async Task ATick_WhileAnotherSessionStillHoldsTheOnlyDueRow_EndsWithoutRevisitingTheRow()
    {
        var id = Pending(ScriptedHandler.Ok);
        _host.Table.LockedByOthers.Add(id);
        var (loop, _) = Loop();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var failure = await Record.ExceptionAsync(() => loop.RunOnceAsync(limit.Token));

        failure.ShouldBeNull("nhịp phải kết thúc trong lúc khoá còn giữ, không quay vòng tới khi hết giờ");
        limit.IsCancellationRequested.ShouldBeFalse("nhịp chỉ kết thúc vì hết giờ — nó đang quay vòng");
        _host.Table.LockAttempts.ShouldBe(1, "một nhịp thử khoá dòng bị giữ đúng một lần");
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
        public const string Pg23514 = "pg-23514";
        public const string Pg57014 = "pg-57014";
        public const string Pg40001 = "pg-40001";
        public const string WrappedCheck = "dbupdate-check";
        public const string WrappedQueryCancel = "dbupdate-cancel";
        public const string AggregateSingle = "aggregate-single";
        public const string AggregateMixed = "aggregate-mixed";

        public static PostgresException PostgresError(string sqlState)
            => new($"lỗi giả {sqlState}", "ERROR", "ERROR", sqlState);

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

            if (envelope.PayloadJson.Contains(Pg23514, StringComparison.Ordinal))
                throw PostgresError(PostgresErrorCodes.CheckViolation);

            if (envelope.PayloadJson.Contains(Pg57014, StringComparison.Ordinal))
                throw PostgresError(PostgresErrorCodes.QueryCanceled);

            if (envelope.PayloadJson.Contains(Pg40001, StringComparison.Ordinal))
                throw PostgresError(PostgresErrorCodes.SerializationFailure);

            // Hình dạng thật EF Core để lại: PostgresException nằm TRONG DbUpdateException (InnerException).
            if (envelope.PayloadJson.Contains(WrappedCheck, StringComparison.Ordinal))
                throw new DbUpdateException("lớp bọc EF", PostgresError(PostgresErrorCodes.CheckViolation));

            if (envelope.PayloadJson.Contains(WrappedQueryCancel, StringComparison.Ordinal))
                throw new DbUpdateException("lớp bọc EF", PostgresError(PostgresErrorCodes.QueryCanceled));

            // `new AggregateException(x)` đặt InnerException = x (ngoại lệ ĐẦU TIÊN), các ngoại lệ còn lại chỉ ở InnerExceptions.
            if (envelope.PayloadJson.Contains(AggregateSingle, StringComparison.Ordinal))
                throw new AggregateException(PostgresError(PostgresErrorCodes.CheckViolation));

            if (envelope.PayloadJson.Contains(AggregateMixed, StringComparison.Ordinal))
                throw new AggregateException(new IOException("mất gói"), PostgresError(PostgresErrorCodes.CheckViolation));

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
