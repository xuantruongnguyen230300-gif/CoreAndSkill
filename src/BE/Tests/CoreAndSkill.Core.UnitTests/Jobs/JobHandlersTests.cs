using System.Linq.Expressions;
using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.UnitTests.Support;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Jobs;

public class JobHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 3, 0, 0, TimeSpan.Zero);

    private static OutboxEnvelope Envelope(string eventType, object payload)
        => new(Guid.NewGuid(), Guid.NewGuid(), eventType, JsonSerializer.Serialize(payload, OutboxJson.Options), Now, "trace", null, null);

    private static OutboxEnvelope RawEnvelope(string eventType, string payload)
        => new(Guid.NewGuid(), Guid.NewGuid(), eventType, payload, Now, null, null, null);

    // ---- JobQueuedOutboxHandler ---------------------------------------------------------------

    [Fact]
    public async Task Queued_EnqueuesTheRunner_ThroughTheSeam_WithJobIdAndInput()
    {
        var scheduler = Substitute.For<IBackgroundJobScheduler>();
        var handler = new JobQueuedOutboxHandler(scheduler);
        var jobId = Guid.NewGuid();
        var envelope = Envelope(JobQueuedEvent.TypeKey, new JobQueuedEvent(jobId, "x.import", "_tmp/abc"));

        var result = await handler.HandleAsync(envelope, default);

        result.IsSuccess.ShouldBeTrue();
        handler.EventType.ShouldBe(JobQueuedEvent.TypeKey);

        var call = scheduler.ReceivedCalls().Single();
        call.GetMethodInfo().Name.ShouldBe(nameof(IBackgroundJobScheduler.EnqueueAsync));

        // Biểu thức truyền qua seam PHẢI gọi JobRunner.RunAsync với đúng jobId và input của dòng outbox.
        var expression = (Expression<Func<JobRunner, CancellationToken, Task>>)call.GetArguments()[0]!;
        var body = expression.Body.ShouldBeAssignableTo<MethodCallExpression>()!;
        body.Method.Name.ShouldBe(nameof(JobRunner.RunAsync));
        Expression.Lambda<Func<Guid>>(body.Arguments[0]).Compile()().ShouldBe(jobId);
        Expression.Lambda<Func<string?>>(body.Arguments[1]).Compile()().ShouldBe("_tmp/abc");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task Queued_AnInvalidPayload_IsAPermanentFailure_AndNeverTouchesTheScheduler(string payload)
    {
        var scheduler = Substitute.For<IBackgroundJobScheduler>();

        var result = await new JobQueuedOutboxHandler(scheduler).HandleAsync(RawEnvelope(JobQueuedEvent.TypeKey, payload), default);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(OutboxHandlingErrors.PayloadInvalid.Code);
        scheduler.ReceivedCalls().ShouldBeEmpty();
    }

    // ---- JobFinishedOutboxHandler -------------------------------------------------------------

    [Theory]
    [InlineData(JobStatus.Succeeded, JobNotificationCodes.Succeeded)]
    [InlineData(JobStatus.Failed, JobNotificationCodes.Failed)]
    public async Task Finished_NotifiesTheInitiator_WithAKeyAndNamedParams_NotASentence(string status, string expectedCode)
    {
        var publisher = Substitute.For<INotificationPublisher>();
        publisher.PublishAsync(default!, default).ReturnsForAnyArgs(Result.Success());
        var initiator = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var envelope = Envelope(JobFinishedEvent.TypeKey, new JobFinishedEvent(jobId, "x.import", status, initiator));

        var result = await new JobFinishedOutboxHandler(publisher).HandleAsync(envelope, default);

        result.IsSuccess.ShouldBeTrue();
        var draft = (NotificationDraft)publisher.ReceivedCalls().Single().GetArguments()[0]!;
        draft.Code.ShouldBe(expectedCode);
        draft.RecipientUserIds.ShouldBe([initiator]);
        draft.Params["JobId"].ShouldBe(jobId.ToString());
        draft.Params["JobType"].ShouldBe("x.import");
        draft.Channels.ShouldBe(NotificationChannels.InApp, "Core không tự bật kênh email — đó là quyết định của dự án");
    }

    [Fact]
    public async Task Finished_APublisherFailure_IsPassedBackSoTheOutboxRowIsNotMarkedDone()
    {
        var publisher = Substitute.For<INotificationPublisher>();
        publisher.PublishAsync(default!, default).ReturnsForAnyArgs(Result.Failure(NotificationErrors.EmailNotConfigured));
        var envelope = Envelope(JobFinishedEvent.TypeKey, new JobFinishedEvent(Guid.NewGuid(), "t", JobStatus.Succeeded, Guid.NewGuid()));

        var result = await new JobFinishedOutboxHandler(publisher).HandleAsync(envelope, default);

        result.Error!.Code.ShouldBe(NotificationErrors.EmailNotConfigured.Code);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("garbage")]
    public async Task Finished_AnInvalidPayload_IsAPermanentFailure(string payload)
    {
        var publisher = Substitute.For<INotificationPublisher>();

        var result = await new JobFinishedOutboxHandler(publisher).HandleAsync(RawEnvelope(JobFinishedEvent.TypeKey, payload), default);

        result.Error!.Code.ShouldBe(OutboxHandlingErrors.PayloadInvalid.Code);
        publisher.ReceivedCalls().ShouldBeEmpty();
    }

    // ---- GET /jobs/{id} -----------------------------------------------------------------------

    private static (GetJobQueryHandler Handler, InMemoryJobRepository Jobs, Guid Caller) NewQueryHandler()
    {
        var jobs = new InMemoryJobRepository();
        var caller = Guid.NewGuid();
        return (new GetJobQueryHandler(jobs, new FakeCurrentUser(caller, "an")), jobs, caller);
    }

    [Fact]
    public async Task GetJob_TheInitiatorSeesStatusResultAndError_AsRawJson()
    {
        var (handler, jobs, caller) = NewQueryHandler();
        var job = Job.Create("x.import", caller, null).Value;
        typeof(Job).GetProperty(nameof(Job.Status))!.SetValue(job, JobStatus.Running);
        job.Complete(Now, "{\"totalRows\":12,\"succeeded\":11}", Guid.NewGuid());
        await jobs.AddAsync(job, default);

        var result = await handler.Handle(new GetJobQuery(job.Id), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(job.Id);
        result.Value.Kind.ShouldBe("x.import");
        result.Value.Status.ShouldBe("succeeded");
        result.Value.Progress.ShouldBe(100);
        result.Value.FinishedAt.ShouldBe(Now);
        result.Value.Result!.Value.GetProperty("totalRows").GetInt32().ShouldBe(12);
        result.Value.ResultFileId.ShouldBe(job.ResultFileId);
        result.Value.Error.ShouldBeNull();
    }

    [Fact]
    public async Task GetJob_ForAFailedJob_ReturnsTheErrorObject_AndNoResult()
    {
        var (handler, jobs, caller) = NewQueryHandler();
        var job = Job.Create("x.import", caller, null).Value;
        job.Fail(Now, "{\"code\":\"CORE.JOB.UNEXPECTED\"}");
        await jobs.AddAsync(job, default);

        var result = await handler.Handle(new GetJobQuery(job.Id), default);

        result.Value.Status.ShouldBe("failed");
        result.Value.Result.ShouldBeNull();
        result.Value.Error!.Value.GetProperty("code").GetString().ShouldBe("CORE.JOB.UNEXPECTED");
    }

    [Fact]
    public async Task GetJob_AnotherUsersJob_IsNotFound_IndistinguishableFromAMissingJob()
    {
        var (handler, jobs, _) = NewQueryHandler();
        var othersJob = Job.Create("x.import", Guid.NewGuid(), null).Value;
        await jobs.AddAsync(othersJob, default);

        var forbidden = await handler.Handle(new GetJobQuery(othersJob.Id), default);
        var missing = await handler.Handle(new GetJobQuery(Guid.NewGuid()), default);

        forbidden.Error!.Code.ShouldBe(JobErrors.NotFound.Code);
        missing.Error!.Code.ShouldBe(JobErrors.NotFound.Code);
        forbidden.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetJob_WithoutASession_IsNotFound()
    {
        var jobs = new InMemoryJobRepository();
        var job = Job.Create("t", Guid.NewGuid(), null).Value;
        await jobs.AddAsync(job, default);

        var result = await new GetJobQueryHandler(jobs, new FakeCurrentUser()).Handle(new GetJobQuery(job.Id), default);

        result.Error!.Code.ShouldBe(JobErrors.NotFound.Code);
    }

    // ---- Quyền của tệp kết quả (owner_table = core.job) -----------------------------------------------

    [Fact]
    public async Task ResultFileOwnerChecker_OnlyTheInitiatorCanReadOrRemove()
    {
        var jobs = new InMemoryJobRepository();
        var initiator = Guid.NewGuid();
        var job = Job.Create("t", initiator, null).Value;
        await jobs.AddAsync(job, default);

        var mine = new JobFileOwnerAccessChecker(jobs, new FakeCurrentUser(initiator, "an"));
        var theirs = new JobFileOwnerAccessChecker(jobs, new FakeCurrentUser(Guid.NewGuid(), "binh"));
        var anonymous = new JobFileOwnerAccessChecker(jobs, new FakeCurrentUser());

        mine.OwnerTable.ShouldBe("core.job");
        (await mine.CanReadAsync(job.Id, default)).ShouldBeTrue();
        (await mine.CanWriteAsync(job.Id, default)).ShouldBeTrue();
        (await theirs.CanReadAsync(job.Id, default)).ShouldBeFalse();
        (await theirs.CanWriteAsync(job.Id, default)).ShouldBeFalse();
        (await anonymous.CanReadAsync(job.Id, default)).ShouldBeFalse();
        (await mine.CanReadAsync(Guid.NewGuid(), default)).ShouldBeFalse("việc không tồn tại thì không ai đọc được tệp gắn vào nó");
    }

    [Fact]
    public void ResultFileOwnerTable_MatchesThePurposeConstants()
        => CoreFilePurposes.JobOwnerTable.ShouldBe("core.job");
}
