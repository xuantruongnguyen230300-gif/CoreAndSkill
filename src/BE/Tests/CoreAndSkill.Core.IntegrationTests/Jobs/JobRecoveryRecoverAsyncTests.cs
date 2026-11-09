using System.Data;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Jobs;

// Nợ B32 — JobRecoveryHostedService.RecoverAsync: một việc hỏng không kéo việc khác, và log nói đúng điều đã xảy ra.
// KHÔNG cần Docker: tập ứng viên do CoreDbContext thật đọc qua một bảng trả sẵn (SqlRecorder), phần còn lại của từng việc
// (kho việc, đơn vị giao dịch) là bản giả đi thẳng. Phần câu quét ứng viên chạy trên PostgreSQL thật KHÔNG thuộc nhóm này:
// JobRecoveryCandidateParityDatabaseTests (RequiresDocker).
//
// Điểm mù: ExecuteInTransactionAsync của bản giả trả đúng `Value` của TransactionOutcome như bản thật, nhưng không có
// giao dịch thật — nên nhóm này không nói được gì về commit/rollback, chỉ về luồng điều khiển và log của vòng lặp.
public sealed class JobRecoveryRecoverAsyncTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 3, 0, 0, TimeSpan.Zero);

    private readonly Dictionary<Guid, Job> _jobs = [];
    private readonly HashSet<Guid> _throwing = [];
    private readonly List<RecoveryCandidate> _candidates = [];
    private readonly CollectingLogger _logger = new();

    // Việc `queued` đúng như một ứng viên bình thường: Fail chấp nhận.
    private Job Queued()
    {
        var job = Job.Create("test.job", Guid.NewGuid(), null).Value;
        _jobs[job.Id] = job;
        _candidates.Add(new RecoveryCandidate(job.Id, Guid.NewGuid(), job.CreatedByUserId));
        return job;
    }

    // Việc đã `succeeded` (Fail từ chối). Không có đường công khai tới trạng thái đó từ Job.Create — cùng cách JobTests dựng.
    private Job AlreadySucceeded()
    {
        var job = Queued();
        typeof(Job).GetProperty(nameof(Job.Status))!.SetValue(job, JobStatus.Succeeded);
        return job;
    }

    // Ứng viên có trong tập quét nhưng không còn trong kho khi nạp lại (đã bị xoá giữa hai bước).
    private Guid Vanished()
    {
        var job = Queued();
        _jobs.Remove(job.Id);
        return job.Id;
    }

    private Guid Throwing()
    {
        var job = Queued();
        _throwing.Add(job.Id);
        return job.Id;
    }

    private async Task<Exception?> RecoverAsync()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("created_by_user_id", typeof(Guid));
        foreach (var c in _candidates)
            table.Rows.Add(c.Id, c.TenantId, c.CreatedByUserId);

        var recorder = new SqlRecorder(readers: [table]);

        var repository = Substitute.For<IJobRepository>();
        repository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var id = call.Arg<Guid>();
            return _throwing.Contains(id)
                ? throw new InvalidOperationException("kho việc hỏng cho " + id)
                : Task.FromResult(_jobs.GetValueOrDefault(id));
        });

        var contextScope = Substitute.For<IExecutionContextScope>();
        contextScope.Enter(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string?>()).Returns(_ => Substitute.For<IDisposable>());

        var services = new ServiceCollection();
        services.AddScoped(_ => OfflineCoreDbContext.Create(null, recorder.Interceptors));
        services.AddScoped(_ => repository);
        services.AddScoped<IUnitOfWork, DirectUnitOfWork>();
        using var provider = services.BuildServiceProvider();

        var service = new JobRecoveryHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(), contextScope, new FixedClock(Now), _logger);

        // Việc RecoverAsync có ném ra ngoài hay không khi một việc hỏng là điều spec B32 CHƯA nói (chỉ nói việc sau vẫn được
        // đánh dấu) — nên ca nào cũng chỉ khẳng định kết quả, không khẳng định ném hay không.
        return await Record.ExceptionAsync(() => service.RecoverAsync(CancellationToken.None));
    }

    private IEnumerable<string> MarkedLines(Guid id)
        => _logger.Entries
            .Where(e => e.Message.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase)
                && e.Message.Contains("đã đánh dấu failed", StringComparison.Ordinal))
            .Select(e => e.Message);

    [Fact]
    public async Task WhenTheSecondCandidateThrows_TheThirdIsStillMarkedFailed()
    {
        var first = Queued();
        Throwing();
        var third = Queued();

        await RecoverAsync();

        first.Status.ShouldBe(JobStatus.Failed);
        third.Status.ShouldBe(JobStatus.Failed, "một việc hỏng không được kéo theo việc đứng sau nó");
    }

    // Đối chứng (T6): đường thành công CÓ log "đã đánh dấu failed" — không có nó thì hai ca "không có dòng" bên dưới xanh rỗng.
    [Fact]
    public async Task Control_AMarkedJob_IsLoggedAsMarkedFailed()
    {
        var job = Queued();

        await RecoverAsync();

        job.Status.ShouldBe(JobStatus.Failed);
        MarkedLines(job.Id).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AJobThatCannotBeFound_IsNotLoggedAsMarkedFailed()
    {
        var missing = Vanished();
        var marked = Queued();

        await RecoverAsync();

        MarkedLines(missing).ShouldBeEmpty("không tìm thấy việc thì không có gì được đánh dấu");
        MarkedLines(marked.Id).ShouldHaveSingleItem("việc đánh dấu thật vẫn phải có dòng của nó");
    }

    [Fact]
    public async Task AJobWhoseFailIsRejected_IsNotLoggedAsMarkedFailed_AndKeepsItsStatus()
    {
        var rejected = AlreadySucceeded();

        await RecoverAsync();

        rejected.Status.ShouldBe(JobStatus.Succeeded);
        MarkedLines(rejected.Id).ShouldBeEmpty("Job.Fail từ chối thì việc không được đánh dấu — log không được nói khác");
    }

    [Fact]
    public async Task TheTotalLine_CountsJobsReallyMarked_NotCandidates()
    {
        Queued();
        Vanished();
        AlreadySucceeded();
        Queued();

        await RecoverAsync();

        var total = _logger.Entries.Single(e => e.Message.StartsWith("Đã đánh dấu", StringComparison.Ordinal)).Message;
        total.ShouldContain("2", Case.Sensitive, "hai việc đánh dấu thật");
        total.ShouldNotContain("4", Case.Sensitive, "bốn là số ứng viên, không phải số việc đã đánh dấu");
    }

    private sealed class DirectUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<TransactionOutcome<T>>> operation, CancellationToken ct = default)
            => (await operation(ct)).Value;
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class CollectingLogger : ILogger<JobRecoveryHostedService>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
