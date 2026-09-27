using CoreAndSkill.Core.Domain.Outbox;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Outbox;

public class OutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 3, 0, 0, TimeSpan.Zero);

    private static OutboxMessage NewMessage()
        => OutboxMessage.Create(Guid.NewGuid(), Now, "core.job.queued.v1", "{}", "trace", Guid.NewGuid(), "an").Value;

    [Fact]
    public void Create_StartsPending_AndIsDueImmediately()
    {
        var tenant = Guid.NewGuid();
        var user = Guid.NewGuid();

        var message = OutboxMessage.Create(tenant, Now, "core.job.queued.v1", "{\"a\":1}", "trace-1", user, "an").Value;

        message.Status.ShouldBe(OutboxStatus.Pending);
        message.AttemptCount.ShouldBe(0);
        message.NextAttemptAt.ShouldBe(Now);
        message.TenantId.ShouldBe(tenant);
        message.TriggeredByUserId.ShouldBe(user);
        message.TriggeredByUserName.ShouldBe("an");
        message.TraceId.ShouldBe("trace-1");
        message.Payload.ShouldBe("{\"a\":1}");
        message.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public void MarkDone_SetsDone_ProcessedAt_ClearsScheduleAndError()
    {
        var message = NewMessage();
        message.RecordFailure(Now, "boom", retryable: true, maxAttempts: 5, TimeSpan.FromSeconds(5));

        message.MarkDone(Now.AddMinutes(1));

        message.Status.ShouldBe(OutboxStatus.Done);
        message.ProcessedAt.ShouldBe(Now.AddMinutes(1));
        message.NextAttemptAt.ShouldBeNull();
        message.LastError.ShouldBeNull();
    }

    [Fact]
    public void RecordFailure_Retryable_StaysPending_AndSchedulesTheNextAttempt()
    {
        var message = NewMessage();

        message.RecordFailure(Now, "timeout", retryable: true, maxAttempts: 5, TimeSpan.FromSeconds(30));

        message.Status.ShouldBe(OutboxStatus.Pending);
        message.AttemptCount.ShouldBe(1);
        message.NextAttemptAt.ShouldBe(Now.AddSeconds(30));
        message.LastError.ShouldBe("timeout");
    }

    [Fact]
    public void RecordFailure_OnTheFifthAttempt_MovesToDead_AndDropsTheSchedule()
    {
        var message = NewMessage();

        for (var attempt = 1; attempt <= 5; attempt++)
            message.RecordFailure(Now, $"lỗi {attempt}", retryable: true, maxAttempts: 5, TimeSpan.FromSeconds(5));

        message.Status.ShouldBe(OutboxStatus.Dead);
        message.AttemptCount.ShouldBe(5);
        message.NextAttemptAt.ShouldBeNull();
        message.LastError.ShouldBe("lỗi 5");
    }

    [Fact]
    public void RecordFailure_NotRetryable_MovesToDeadOnTheFirstAttempt()
    {
        var message = NewMessage();

        message.RecordFailure(Now, "hợp đồng không khớp", retryable: false, maxAttempts: 5, TimeSpan.FromSeconds(5));

        message.Status.ShouldBe(OutboxStatus.Dead);
        message.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public void Replay_FromDead_ResetsToPendingWithZeroAttempts()
    {
        var message = NewMessage();
        message.RecordFailure(Now, "x", retryable: false, maxAttempts: 5, TimeSpan.Zero);

        var result = message.Replay(Now.AddHours(1));

        result.IsSuccess.ShouldBeTrue();
        message.Status.ShouldBe(OutboxStatus.Pending);
        message.AttemptCount.ShouldBe(0);
        message.NextAttemptAt.ShouldBe(Now.AddHours(1));
    }

    [Theory]
    [InlineData(OutboxStatus.Pending)]
    [InlineData(OutboxStatus.Done)]
    public void Replay_FromAnythingButDead_IsRejected_BecauseItWouldDoubleSend(string status)
    {
        var message = NewMessage();
        if (status == OutboxStatus.Done)
            message.MarkDone(Now);

        var result = message.Replay(Now);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(OutboxErrors.NotDead.Code);
        message.Status.ShouldBe(status);
    }
}
