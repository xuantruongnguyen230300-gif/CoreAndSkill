using System.Text.Json;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.UnitTests.ClientErrors;
using CoreAndSkill.Core.UnitTests.Support;
using CoreAndSkill.Tests.Shared;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Jobs;

// docs/quy-uoc/be-cqrs-handler.md §10.3, docs/contracts/jobs.md. Ba tính chất của JobRunner: idempotent, KHÔNG
// nuốt lỗi, và phát MỘT sự kiện kết thúc trong cùng giao dịch với trạng thái cuối.
public class JobRunnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 3, 0, 0, TimeSpan.Zero);

    private readonly InMemoryJobRepository _jobs = new();
    private readonly PassThroughUnitOfWork _uow = new();
    private readonly RecordingLogger<JobRunner> _logger = new();

    private sealed class ScriptedExecutor(string handledType, Func<JobExecutionContext, Task<JobOutcome>> run) : IJobExecutor
    {
        public List<JobExecutionContext> Contexts { get; } = [];

        public bool Handles(string jobType) => jobType == handledType;

        public Task<JobOutcome> ExecuteAsync(JobExecutionContext context, CancellationToken ct)
        {
            Contexts.Add(context);
            return run(context);
        }
    }

    private JobRunner Runner(params IJobExecutor[] executors)
        => new(_jobs, _uow, executors, new FixedTimeProvider(Now), _logger);

    private async Task<Job> QueuedJobAsync(string type = "x.import")
    {
        var job = Job.Create(type, Guid.NewGuid(), "_tmp/in").Value;
        job.ClearDomainEvents();
        await _jobs.AddAsync(job, default);
        return job;
    }

    [Fact]
    public async Task Run_Success_CompletesTheJob_WithResultAndFile_AndRecordsFinishedEvent()
    {
        var job = await QueuedJobAsync();
        var fileId = Guid.NewGuid();
        var executor = new ScriptedExecutor("x.import", _ => Task.FromResult(JobOutcome.Success("{\"totalRows\":3}", fileId)));

        await Runner(executor).RunAsync(job.Id, "_tmp/in", default);

        job.Status.ShouldBe(JobStatus.Succeeded);
        job.ResultJson.ShouldBe("{\"totalRows\":3}");
        job.ResultFileId.ShouldBe(fileId);
        job.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<JobFinishedEvent>();
        _uow.Commits.ShouldBe([true]);
    }

    [Fact]
    public async Task Run_PassesTheInputReference_ThatTravelledWithTheOutboxRow()
    {
        var job = await QueuedJobAsync();
        var executor = new ScriptedExecutor("x.import", _ => Task.FromResult(JobOutcome.Success(null)));

        await Runner(executor).RunAsync(job.Id, "_tmp/in", default);

        var context = executor.Contexts.ShouldHaveSingleItem();
        context.JobId.ShouldBe(job.Id);
        context.JobType.ShouldBe("x.import");
        context.Input.ShouldBe("_tmp/in");
    }

    [Fact]
    public async Task Run_ProgressReportedByTheExecutor_IsWrittenThroughTheRepository()
    {
        var job = await QueuedJobAsync();
        var executor = new ScriptedExecutor("x.import", async context =>
        {
            await context.ReportProgressAsync(40);
            return JobOutcome.Success(null);
        });

        await Runner(executor).RunAsync(job.Id, null, default);

        _jobs.ProgressUpdates.ShouldBe([40]);
    }

    [Fact]
    public async Task Run_ExecutorReturnsFailure_MarksFailed_WithTheErrorAsJson()
    {
        var job = await QueuedJobAsync();
        var executor = new ScriptedExecutor("x.import", _ => Task.FromResult(JobOutcome.Failure(
            new Error("CORE.IMPORT.SOURCE_UNREADABLE", "Không đọc được {What}.", ErrorType.BusinessRule).WithParams(("What", "tệp")))));

        await Runner(executor).RunAsync(job.Id, null, default);

        job.Status.ShouldBe(JobStatus.Failed);
        using var error = JsonDocument.Parse(job.ErrorJson!);
        error.RootElement.GetProperty("code").GetString().ShouldBe("CORE.IMPORT.SOURCE_UNREADABLE");
        error.RootElement.GetProperty("message").GetString().ShouldBe("Không đọc được tệp.");
        error.RootElement.GetProperty("messageParams").GetProperty("What").GetString().ShouldBe("tệp");
    }

    [Fact]
    public async Task Run_TheSameJobTwice_RunsTheExecutorOnce_BecauseOutboxDeliversAtLeastOnce()
    {
        var job = await QueuedJobAsync();
        var executor = new ScriptedExecutor("x.import", _ => Task.FromResult(JobOutcome.Success(null)));
        var runner = Runner(executor);

        await runner.RunAsync(job.Id, "_tmp/in", default);
        await runner.RunAsync(job.Id, "_tmp/in", default);

        executor.Contexts.Count.ShouldBe(1);
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Run_AnUnknownJobId_DoesNothing()
    {
        var executor = new ScriptedExecutor("x.import", _ => Task.FromResult(JobOutcome.Success(null)));

        await Runner(executor).RunAsync(Guid.NewGuid(), null, default);

        executor.Contexts.ShouldBeEmpty();
        _uow.Commits.ShouldBeEmpty();
    }

    [Fact]
    public async Task Run_NoExecutorForTheType_FailsTheJob_AndCountsIt()
    {
        var job = await QueuedJobAsync("loai.la");

        var failures = MeasureJobFailures(() => Runner().RunAsync(job.Id, null, default).GetAwaiter().GetResult());

        job.Status.ShouldBe(JobStatus.Failed);
        job.ErrorJson!.ShouldContain(JobErrors.NoExecutor.Code);
        failures.ShouldBe(1, "đúng MỘT lần — không đếm trùng, không bỏ sót");
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Run_AnUnexpectedException_IsNotSwallowed_LoggedRedacted_CountedAndTheJobFails()
    {
        var job = await QueuedJobAsync();
        var executor = new ScriptedExecutor("x.import", _ => throw new InvalidOperationException("password: Abc@12345 rejected"));

        var failures = MeasureJobFailures(() => Runner(executor).RunAsync(job.Id, null, default).GetAwaiter().GetResult());

        job.Status.ShouldBe(JobStatus.Failed);
        job.ErrorJson!.ShouldContain(JobErrors.Unexpected.Code);
        job.ErrorJson!.ShouldNotContain(CommonErrors.Unexpected.Code, Case.Sensitive,
            "việc nền hỏng mang mã của khu việc nền, không mang mã 500 của request — ADR-0077, luật R9");
        failures.ShouldBe(1, "đúng MỘT lần — không đếm trùng, không bỏ sót");

        var error = _logger.Entries.Single(e => e.Level == LogLevel.Error);
        error.Message.ShouldNotContain("Abc@12345", Case.Sensitive, "thông điệp của thư viện ngoài có thể mang giá trị đầu vào (07 §5)");
        error.Message.ShouldContain("InvalidOperationException");
    }

    // "Đang tắt" nghĩa là token của CHÍNH lượt chạy đã huỷ — không phải "ngoại lệ có kiểu OperationCanceledException".
    [Fact]
    public async Task Run_Cancellation_PropagatesAndLeavesTheJobRunning_ForTheRecoveryServiceToMark()
    {
        var job = await QueuedJobAsync();
        using var stopping = new CancellationTokenSource();
        var executor = new ScriptedExecutor("x.import", _ =>
        {
            stopping.Cancel(); // tiến trình dừng giữa lúc việc đang chạy
            throw new OperationCanceledException(stopping.Token);
        });

        await Should.ThrowAsync<OperationCanceledException>(() => Runner(executor).RunAsync(job.Id, null, stopping.Token));

        job.Status.ShouldBe(JobStatus.Running);
    }

    // Hết thời gian chờ của HttpClient đi ra thành TaskCanceledException (bọc TimeoutException) trong khi token của lượt
    // chạy CHƯA huỷ. Đó là lỗi của executor, không phải tín hiệu dừng: việc kết thúc `failed` với CORE.JOB.UNEXPECTED
    // theo đúng đường lỗi không lường trước, không treo ở `running` chờ lần khởi động sau.
    [Fact]
    public async Task Run_ATimeoutSurfacingAsTaskCanceled_WhileTheTokenIsNotCancelled_FailsTheJob_InsteadOfLeavingItRunning()
    {
        var job = await QueuedJobAsync();
        var executor = new ScriptedExecutor("x.import", _ => throw new TaskCanceledException("", new TimeoutException()));

        var failures = MeasureJobFailures(() => Runner(executor).RunAsync(job.Id, null, CancellationToken.None).GetAwaiter().GetResult());

        job.Status.ShouldBe(JobStatus.Failed);
        job.ErrorJson!.ShouldContain(JobErrors.Unexpected.Code);
        failures.ShouldBe(1, "đúng MỘT lần — không đếm trùng, không bỏ sót");
        _uow.Commits.ShouldBe([true]);
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains(nameof(TaskCanceledException)));
    }

    [Fact]
    public async Task Run_WhenTheFinalStateCannotBeRecorded_LogsAnErrorInsteadOfStayingSilent()
    {
        var job = await QueuedJobAsync();
        // Executor tự đổi trạng thái sang `succeeded` ngoài luồng: Complete từ trạng thái đó bị từ chối.
        var executor = new ScriptedExecutor("x.import", _ =>
        {
            typeof(Job).GetProperty(nameof(Job.Status))!.SetValue(job, JobStatus.Succeeded);
            return Task.FromResult(JobOutcome.Success(null));
        });

        await Runner(executor).RunAsync(job.Id, null, default);

        _uow.Commits.ShouldBe([false]);
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains("KHÔNG ghi được trạng thái cuối"));
    }

    [Fact]
    public async Task Run_WhenTheJobRowVanishesBetweenClaimAndFinish_LogsAnError()
    {
        var job = await QueuedJobAsync();
        var executor = new ScriptedExecutor("x.import", _ =>
        {
            _jobs.Jobs.Remove(job.Id);
            return Task.FromResult(JobOutcome.Success(null));
        });

        await Runner(executor).RunAsync(job.Id, null, default);

        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Error);
    }

    // Đếm chỉ số core.job.failed phát ra trong lúc chạy `act` — chỉ số phải tăng, không chỉ dòng log.
    // Qua `MeterProbe` chứ không qua `MeterListener` trần: listener trần đếm luôn phép đo của mọi test chạy
    // song song trên cùng `core.job.failed`, nên con số nó trả về không dùng để khẳng định ĐÚNG MỘT lần được.
    private static long MeasureJobFailures(Action act)
    {
        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);

        act();

        return probe.Count("core.job.failed");
    }
}
