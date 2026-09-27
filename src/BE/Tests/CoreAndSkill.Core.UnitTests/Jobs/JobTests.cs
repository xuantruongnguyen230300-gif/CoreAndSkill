using CoreAndSkill.Core.Domain.Jobs;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Jobs;

public class JobTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 3, 0, 0, TimeSpan.Zero);

    private static Job NewRunningJob()
    {
        var job = Job.Create("banhang.don-hang.import", Guid.NewGuid(), "_tmp/abc").Value;

        // Bước queued -> running không đi qua entity (IJobRepository.TryClaimAsync là một câu UPDATE có
        // điều kiện) — test dựng trạng thái `running` bằng đường mà chính EF dùng khi nạp một dòng.
        typeof(Job).GetProperty(nameof(Job.Status))!.SetValue(job, JobStatus.Running);
        job.ClearDomainEvents();
        return job;
    }

    [Fact]
    public void Create_StartsQueued_WithZeroProgress_AndRecordsJobQueuedEvent()
    {
        var creator = Guid.NewGuid();

        var job = Job.Create("  banhang.don-hang.import  ", creator, "_tmp/abc").Value;

        job.Status.ShouldBe(JobStatus.Queued);
        job.Progress.ShouldBe((short)0);
        job.Type.ShouldBe("banhang.don-hang.import");
        job.CreatedByUserId.ShouldBe(creator);
        job.Id.ShouldNotBe(Guid.Empty);

        var domainEvent = job.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<JobQueuedEvent>();
        domainEvent.JobId.ShouldBe(job.Id);
        domainEvent.JobType.ShouldBe("banhang.don-hang.import");
        domainEvent.Input.ShouldBe("_tmp/abc");
    }

    [Fact]
    public void Complete_FromRunning_SetsSucceeded_Progress100_AndRecordsFinishedEvent()
    {
        var job = NewRunningJob();
        var fileId = Guid.NewGuid();

        var result = job.Complete(Now, "{\"totalRows\":3}", fileId);

        result.IsSuccess.ShouldBeTrue();
        job.Status.ShouldBe(JobStatus.Succeeded);
        job.Progress.ShouldBe((short)100);
        job.FinishedAt.ShouldBe(Now);
        job.ResultJson.ShouldBe("{\"totalRows\":3}");
        job.ResultFileId.ShouldBe(fileId);

        var finished = job.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<JobFinishedEvent>();
        finished.Status.ShouldBe(JobStatus.Succeeded);
        finished.CreatedByUserId.ShouldBe(job.CreatedByUserId);
    }

    [Fact]
    public void Fail_FromRunning_SetsFailed_AndKeepsErrorJson()
    {
        var job = NewRunningJob();

        var result = job.Fail(Now, "{\"code\":\"X\"}");

        result.IsSuccess.ShouldBeTrue();
        job.Status.ShouldBe(JobStatus.Failed);
        job.ErrorJson.ShouldBe("{\"code\":\"X\"}");
        job.FinishedAt.ShouldBe(Now);
        job.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<JobFinishedEvent>().Status.ShouldBe(JobStatus.Failed);
    }

    [Fact]
    public void Fail_FromQueued_IsAllowed_BecauseRestartLosesTheInMemoryQueue()
    {
        var job = Job.Create("t", Guid.NewGuid(), null).Value;

        job.Fail(Now, "{}").IsSuccess.ShouldBeTrue();

        job.Status.ShouldBe(JobStatus.Failed);
    }

    [Fact]
    public void Complete_FromQueued_IsRejected_AndRecordsNoEvent()
    {
        var job = Job.Create("t", Guid.NewGuid(), null).Value;
        job.ClearDomainEvents();

        var result = job.Complete(Now, null, null);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(JobStateErrors.InvalidTransition.Code);
        job.Status.ShouldBe(JobStatus.Queued);
        job.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Fail_FromSucceeded_IsRejected()
    {
        var job = NewRunningJob();
        job.Complete(Now, null, null);

        var result = job.Fail(Now, "{}");

        result.IsFailure.ShouldBeTrue();
        job.Status.ShouldBe(JobStatus.Succeeded);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheCollection()
    {
        var job = Job.Create("t", Guid.NewGuid(), null).Value;

        job.ClearDomainEvents();

        job.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void EventTypes_CarryAVersionSuffix_SoAReceiverCanRefuseAnUnknownVersion()
    {
        JobQueuedEvent.TypeKey.ShouldEndWith(".v1");
        JobFinishedEvent.TypeKey.ShouldEndWith(".v1");
        ((Core.Domain.Common.IDomainEvent)new JobQueuedEvent(Guid.NewGuid(), "t", null)).EventType.ShouldBe(JobQueuedEvent.TypeKey);
        ((Core.Domain.Common.IDomainEvent)new JobFinishedEvent(Guid.NewGuid(), "t", "succeeded", Guid.NewGuid())).EventType.ShouldBe(JobFinishedEvent.TypeKey);
    }
}
