using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Outbox;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Outbox;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Outbox trên PostgreSQL THẬT — docs/wiki-core/be/12-notifications.md §2. Chứng minh những thứ test thuần logic
// (OutboxInterceptorTests, OutboxHealthCheckTests, OutboxMessageTests) KHÔNG chứng minh được:
//   1. dòng outbox nằm CÙNG giao dịch với thay đổi nghiệp vụ — commit thì cùng có, quay lại thì cùng không;
//   2. câu SQL `FOR UPDATE SKIP LOCKED` của bộ phát chạy được trên PostgreSQL thật và không cho hai bộ phát cùng
//      xử lý một dòng;
//   3. đường done / dead ngay / thử lại có lùi dần / dead sau đủ lần thử;
//   4. dòng `dead` làm readiness Degraded, phát lại đưa nó về pending kèm một dòng nhật ký kiểm toán.
//
// Bộ phát nền bị gỡ khỏi host (B4DockerHost) — test gọi DispatchBatchAsync bằng tay để không tranh dòng với nó.
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class OutboxDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string FakeEvent = "test.fake.v1";

    private readonly ScriptedHandler _handler = new();
    private B4DockerHost _host = null!;
    private TestTenant _tenant = null!;

    public async Task InitializeAsync()
    {
        await db.ResetAsync();
        _host = new B4DockerHost(db.ConnectionString, configure: services =>
        {
            services.AddScoped<IOutboxEventHandler>(sp => new RecordingHandler(_handler, sp.GetRequiredService<ITenantContext>(), sp.GetRequiredService<ICurrentUser>()));
            services.AddScoped<IOutboxEventHandler>(sp => new UnknownRecipientHandler(sp.GetRequiredService<INotificationRepository>()));
        });
        _tenant = await _host.CreateTenantAsync("DV-OUTBOX");
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private OutboxDispatcher Dispatcher()
    {
        var dispatcher = _host.Services.GetRequiredService<OutboxDispatcher>();
        dispatcher.JitterSource = () => 0.5; // giữa dải ±20% => khoảng lùi đúng bằng giá trị danh định
        return dispatcher;
    }

    // ---- 1. Cùng giao dịch ---------------------------------------------------------------------

    private Task<Guid> AddJobInTransactionAsync(bool commit, bool throwAfterSave = false)
        => _host.AsAsync(_tenant, async sp =>
        {
            var jobs = sp.GetRequiredService<IJobRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var job = Job.Create("test.job", _tenant.AdminUserId, "_tmp/x").Value;

            try
            {
                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    await jobs.AddAsync(job, ct);
                    await unitOfWork.SaveChangesAsync(ct);

                    if (throwAfterSave)
                        throw new InvalidOperationException("lỗi sau khi đã lưu, trước khi commit");

                    return new TransactionOutcome<bool>(true, ShouldCommit: commit);
                });
            }
            catch (InvalidOperationException) when (throwAfterSave)
            {
                // Ngoại lệ được chủ ý — giao dịch phải quay lại.
            }

            return job.Id;
        });

    private Task<long> JobRowsAsync(Guid jobId)
        => db.CountAsync("SELECT count(*) FROM core.job WHERE id = @id", new NpgsqlParameter("id", jobId));

    private Task<long> QueuedOutboxRowsAsync(Guid jobId)
        => db.CountAsync(
            "SELECT count(*) FROM core.outbox_message WHERE event_type = 'core.job.queued.v1' AND payload ->> 'jobId' = @id",
            new NpgsqlParameter("id", jobId.ToString()));

    [Fact]
    public async Task Commit_WritesTheBusinessRowAndExactlyOneOutboxRow_Together()
    {
        var jobId = await AddJobInTransactionAsync(commit: true);

        (await JobRowsAsync(jobId)).ShouldBe(1);
        (await QueuedOutboxRowsAsync(jobId)).ShouldBe(1);
    }

    [Fact]
    public async Task Rollback_WritesNeitherTheBusinessRowNorTheOutboxRow()
    {
        var jobId = await AddJobInTransactionAsync(commit: false);

        (await JobRowsAsync(jobId)).ShouldBe(0);
        (await QueuedOutboxRowsAsync(jobId)).ShouldBe(0);
    }

    [Fact]
    public async Task AnExceptionAfterTheSave_RollsBackBoth()
    {
        var jobId = await AddJobInTransactionAsync(commit: true, throwAfterSave: true);

        (await JobRowsAsync(jobId)).ShouldBe(0);
        (await QueuedOutboxRowsAsync(jobId)).ShouldBe(0);
    }

    [Fact]
    public async Task TheOutboxRow_CarriesTheTenant_TheTriggeringUser_AndIsPending()
    {
        var jobId = await AddJobInTransactionAsync(commit: true);

        var rows = await db.QueryAsync(
            "SELECT tenant_id, triggered_by_user_id, triggered_by_user_name, status, attempt_count FROM core.outbox_message WHERE payload ->> 'jobId' = @id",
            r => (Tenant: r.GetGuid(0), User: r.IsDBNull(1) ? (Guid?)null : r.GetGuid(1), Name: r.IsDBNull(2) ? null : r.GetString(2), Status: r.GetString(3), Attempts: r.GetInt32(4)),
            new NpgsqlParameter("id", jobId.ToString()));

        var row = rows.ShouldHaveSingleItem();
        row.Tenant.ShouldBe(_tenant.Id);
        row.User.ShouldBe(_tenant.AdminUserId);
        row.Name.ShouldBe(_tenant.AdminUserName);
        row.Status.ShouldBe("pending");
        row.Attempts.ShouldBe(0);
    }

    // ---- 2-3. Bộ phát ---------------------------------------------------------------------------

    private async Task<Guid> InsertPendingAsync(string eventType = FakeEvent, string payload = "{\"n\":1}")
    {
        var id = Guid.Empty;
        await _host.AsAsync(_tenant, async sp =>
        {
            var context = sp.GetRequiredService<CoreDbContext>();
            var message = OutboxMessage.Create(_tenant.Id, _host.Clock.GetUtcNow(), eventType, payload, null, _tenant.AdminUserId, _tenant.AdminUserName).Value;
            id = message.Id;
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
        });

        return id;
    }

    private async Task<OutboxState> StateAsync(Guid id)
    {
        var rows = await db.QueryAsync(
            "SELECT status, attempt_count, next_attempt_at, last_error, processed_at FROM core.outbox_message WHERE id = @id",
            r => new OutboxState(
                r.GetString(0), r.GetInt32(1),
                r.IsDBNull(2) ? null : r.GetFieldValue<DateTimeOffset>(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? null : r.GetFieldValue<DateTimeOffset>(4)),
            new NpgsqlParameter("id", id));

        return rows.Single();
    }

    [Fact]
    public async Task ASuccessfulHandler_MarksTheRowDone_AndRunsInTheRowsOwnTenantContext()
    {
        var id = await InsertPendingAsync();
        _handler.Behavior = _ => Task.FromResult(Result.Success());

        var processed = await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        processed.ShouldBe(1);
        var state = await StateAsync(id);
        state.Status.ShouldBe("done");
        state.ProcessedAt.ShouldNotBeNull();
        state.LastError.ShouldBeNull();
        _handler.Calls.ShouldBe(1);
        _handler.SeenTenant.ShouldBe(_tenant.Id, "bộ phát mở lại phạm vi đơn vị của CHÍNH dòng đó");
        _handler.SeenUser.ShouldBe(_tenant.AdminUserId, "và người kích hoạt");
    }

    [Fact]
    public async Task ADoneRow_IsNeverDispatchedAgain()
    {
        await InsertPendingAsync();
        _handler.Behavior = _ => Task.FromResult(Result.Success());

        await Dispatcher().DispatchBatchAsync(CancellationToken.None);
        var second = await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        second.ShouldBe(0);
        _handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task AFailureResult_GoesStraightToDead_WithoutRetry_AndKeepsTheReason()
    {
        var id = await InsertPendingAsync();
        _handler.Behavior = _ => Task.FromResult(Result.Failure(OutboxHandlingErrors.NoHandler.WithParams(("EventType", FakeEvent))));

        await Dispatcher().DispatchBatchAsync(CancellationToken.None);
        _host.Clock.Advance(TimeSpan.FromHours(2));
        var again = await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        var state = await StateAsync(id);
        state.Status.ShouldBe("dead");
        state.Attempts.ShouldBe(1, "lỗi vĩnh viễn: một lần là đủ, thử lại bao nhiêu lần cũng vậy");
        state.NextAttemptAt.ShouldBeNull();
        state.LastError.ShouldNotBeNullOrWhiteSpace();
        again.ShouldBe(0);
        _handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task AnEventTypeWithNoHandler_GoesToDead_LoudlyNotSilently()
    {
        var id = await InsertPendingAsync(eventType: "khong.co.ben.nhan.v1");

        await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        var state = await StateAsync(id);
        state.Status.ShouldBe("dead");
        state.LastError.ShouldNotBeNull().ShouldContain("khong.co.ben.nhan.v1");
    }

    [Fact]
    public async Task ATransientException_StaysPending_WithABackoff_AndIsNotDueImmediately()
    {
        var id = await InsertPendingAsync();
        _handler.Behavior = _ => throw new TimeoutException("hệ ngoài quá tải");
        var before = _host.Clock.GetUtcNow();

        await Dispatcher().DispatchBatchAsync(CancellationToken.None);
        var immediately = await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        var state = await StateAsync(id);
        state.Status.ShouldBe("pending");
        state.Attempts.ShouldBe(1);
        state.NextAttemptAt.ShouldNotBeNull().ShouldBeGreaterThan(before.AddSeconds(4));
        state.LastError.ShouldNotBeNull().ShouldContain(nameof(TimeoutException));
        immediately.ShouldBe(0, "chưa tới hạn thử lại");
    }

    [Fact]
    public async Task ARetryAfterTheBackoff_CanSucceed_AndTheRowEndsDone()
    {
        var id = await InsertPendingAsync();
        _handler.Behavior = _ => throw new TimeoutException("lần đầu hỏng");
        await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        _handler.Behavior = _ => Task.FromResult(Result.Success());
        _host.Clock.Advance(TimeSpan.FromMinutes(20));
        var processed = await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        processed.ShouldBe(1);
        var state = await StateAsync(id);
        state.Status.ShouldBe("done");
        state.LastError.ShouldBeNull();
    }

    [Fact]
    public async Task TransientFailuresUpToTheLimit_EndDead_AfterExactlyMaxAttempts()
    {
        var id = await InsertPendingAsync();
        _handler.Behavior = _ => throw new TimeoutException("luôn hỏng");
        var maxAttempts = _host.Services.GetRequiredService<IOptions<CoreOutboxOptions>>().Value.MaxAttempts;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            (await StateAsync(id)).Status.ShouldBe("pending", $"trước lần thử thứ {attempt}");
            _host.Clock.Advance(TimeSpan.FromHours(1));
            await Dispatcher().DispatchBatchAsync(CancellationToken.None);
        }

        var state = await StateAsync(id);
        state.Status.ShouldBe("dead");
        state.Attempts.ShouldBe(maxAttempts);
        _handler.Calls.ShouldBe(maxAttempts);
    }

    // Lỗi ở pha LƯU — sau bên nhận, ngoài khối bắt quanh bên nhận: khoá ngoại notification_recipient.user_id THẬT từ chối một
    // người nhận không tồn tại (23503). Dòng đó đốt đúng một lần thử rồi chết (mã không tạm thời), và dòng hợp lệ xếp sau nó
    // trong lô vẫn được phát. Bản không cần Docker của cùng kịch bản: OutboxDispatchFailureTests.
    [Fact]
    public async Task ASaveRejectedByTheRealForeignKey_EndsDeadAfterOneAttempt_AndTheRowQueuedBehindIsStillDone()
    {
        var poison = await InsertPendingAsync(eventType: UnknownRecipientHandler.Event);
        _host.Clock.Advance(TimeSpan.FromSeconds(1));
        var valid = await InsertPendingAsync();
        _handler.Behavior = _ => Task.FromResult(Result.Success());

        await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        var dead = await StateAsync(poison);
        dead.Status.ShouldBe("dead");
        dead.Attempts.ShouldBe(1);
        dead.NextAttemptAt.ShouldBeNull();
        dead.LastError.ShouldNotBeNull().ShouldContain(PostgresErrorCodes.ForeignKeyViolation);
        (await StateAsync(valid)).Status.ShouldBe("done", "một dòng hỏng không được chặn dòng xếp sau nó");
    }

    [Fact]
    public async Task TheStoredError_IsRedacted_ATokenInAnExceptionMessageIsNeverPersisted()
    {
        var id = await InsertPendingAsync();
        _handler.Behavior = _ => throw new InvalidOperationException("gọi hệ ngoài thất bại: Authorization: Bearer abcDEF123secretTOKEN, thư của an.nguyen@example.com");

        await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        var error = (await StateAsync(id)).LastError.ShouldNotBeNull();
        error.ShouldNotContain("abcDEF123secretTOKEN");
        error.ShouldNotContain("an.nguyen@example.com");
    }

    [Fact]
    public async Task TwoDispatchersRacingForTheSameRow_OnlyOneHandlesIt()
    {
        var id = await InsertPendingAsync();
        var gate = new TaskCompletionSource();
        _handler.Behavior = async _ =>
        {
            await gate.Task; // giữ khoá dòng cho tới khi bộ phát thứ hai đã thử
            return Result.Success();
        };

        var first = Task.Run(() => Dispatcher().DispatchBatchAsync(CancellationToken.None));
        await _handler.WaitForFirstCallAsync();
        var second = await Dispatcher().DispatchBatchAsync(CancellationToken.None);
        gate.SetResult();
        await first;

        _handler.Calls.ShouldBe(1, "SKIP LOCKED: bộ phát thứ hai bỏ qua dòng đang bị giữ");
        (await StateAsync(id)).Status.ShouldBe("done");
        second.ShouldBe(1, "dòng vẫn nằm trong danh sách tới hạn của bộ phát thứ hai — nó chỉ bỏ qua khi khoá không lấy được");
    }

    // ---- 4. Dead: nhìn thấy được và phát lại được -------------------------------------------------

    private async Task<HealthStatus> ReadinessOfOutboxAsync()
    {
        var check = ActivatorUtilities.CreateInstance<OutboxHealthCheck>(_host.Services);
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        return result.Status;
    }

    [Fact]
    public async Task ADeadRow_MakesTheOutboxHealthCheckDegraded_NotHealthy_NotUnhealthy()
    {
        (await ReadinessOfOutboxAsync()).ShouldBe(HealthStatus.Healthy);

        await InsertPendingAsync(eventType: "khong.co.ben.nhan.v1");
        await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        (await ReadinessOfOutboxAsync()).ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task APendingRowOlderThanTheThreshold_MakesTheHealthCheckDegraded()
    {
        await InsertPendingAsync();
        (await ReadinessOfOutboxAsync()).ShouldBe(HealthStatus.Healthy);

        _host.Clock.Advance(TimeSpan.FromMinutes(_host.Services.GetRequiredService<IOptions<CoreOutboxOptions>>().Value.DegradedPendingAgeMinutes + 1));

        (await ReadinessOfOutboxAsync()).ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task Replay_ResetsADeadRowToPending_AndWritesOneAuditRow()
    {
        var id = await InsertPendingAsync(eventType: "khong.co.ben.nhan.v1");
        await Dispatcher().DispatchBatchAsync(CancellationToken.None);
        (await StateAsync(id)).Status.ShouldBe("dead");

        var result = await _host.AsAsync(_tenant, sp => sp.GetRequiredService<IOutboxReplayService>().ReplayAsync(id, CancellationToken.None));

        result.IsSuccess.ShouldBeTrue();
        var state = await StateAsync(id);
        state.Status.ShouldBe("pending");
        state.Attempts.ShouldBe(0);
        (await db.CountAsync(
            "SELECT count(*) FROM core.audit_log WHERE action_code = @a AND target_id = @id",
            new NpgsqlParameter("a", CoreAndSkill.Core.Application.Audit.AuditActions.OutboxReplay),
            new NpgsqlParameter("id", id.ToString()))).ShouldBe(1);
    }

    [Fact]
    public async Task Replay_OfARowThatIsNotDead_IsRefused_AndWritesNothing()
    {
        var id = await InsertPendingAsync();

        var result = await _host.AsAsync(_tenant, sp => sp.GetRequiredService<IOutboxReplayService>().ReplayAsync(id, CancellationToken.None));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(OutboxErrors.NotDead.Code);
        (await db.CountAsync("SELECT count(*) FROM core.audit_log WHERE target_id = @id", new NpgsqlParameter("id", id.ToString()))).ShouldBe(0);
    }

    [Fact]
    public async Task ASnapshot_CountsPendingAndDeadAcrossTheSystem()
    {
        await InsertPendingAsync();
        await InsertPendingAsync(eventType: "khong.co.ben.nhan.v1");
        await Dispatcher().DispatchBatchAsync(CancellationToken.None);

        var snapshot = await Dispatcher().MeasureAsync(CancellationToken.None);

        snapshot.DeadCount.ShouldBe(1);
    }

    private sealed record OutboxState(string Status, int Attempts, DateTimeOffset? NextAttemptAt, string? LastError, DateTimeOffset? ProcessedAt);

    // Hành vi do test đặt; các bản scoped chỉ chuyển tiếp tới đây và ghi lại ngữ cảnh mà bộ phát đã mở.
    private sealed class ScriptedHandler
    {
        private readonly TaskCompletionSource _firstCall = new();

        public Func<OutboxEnvelope, Task<Result>> Behavior { get; set; } = _ => Task.FromResult(Result.Success());

        public int Calls { get; private set; }

        public Guid? SeenTenant { get; set; }

        public Guid? SeenUser { get; set; }

        public Task WaitForFirstCallAsync() => _firstCall.Task;

        public async Task<Result> InvokeAsync(OutboxEnvelope envelope)
        {
            Calls++;
            _firstCall.TrySetResult();
            return await Behavior(envelope);
        }
    }

    // Tạo thông báo cho một người dùng không tồn tại — phần ghi của nó chỉ hỏng lúc SaveChanges, sau khi bên nhận đã trả thành công.
    private sealed class UnknownRecipientHandler(INotificationRepository notifications) : IOutboxEventHandler
    {
        public const string Event = "test.unknown-recipient.v1";

        public string EventType => Event;

        public async Task<Result> HandleAsync(OutboxEnvelope envelope, CancellationToken ct)
        {
            var notification = Notification.Create("test.thong-bao", null, null).Value;
            var recipient = NotificationRecipient.Create(notification.Id, Guid.NewGuid()).Value;
            await notifications.AddAsync(notification, [recipient], ct);
            return Result.Success();
        }
    }

    private sealed class RecordingHandler(ScriptedHandler script, ITenantContext tenantContext, ICurrentUser currentUser) : IOutboxEventHandler
    {
        public string EventType => FakeEvent;

        public Task<Result> HandleAsync(OutboxEnvelope envelope, CancellationToken ct)
        {
            script.SeenTenant = tenantContext.TenantId;
            script.SeenUser = currentUser.UserId;
            return script.InvokeAsync(envelope);
        }
    }
}
