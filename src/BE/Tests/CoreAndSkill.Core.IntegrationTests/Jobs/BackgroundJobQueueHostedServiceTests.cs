using System.Threading.Channels;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Infrastructure.Execution;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Jobs;

// Phía tiêu thụ hàng đợi việc nền — không chạm DB, kiểm bằng Channel trong bộ nhớ, không cần Postgres. Đặt ở
// IntegrationTests vì kiểu internal của Core.Infrastructure (docs/quy-uoc/be-architecture.md §9.1).
//
// "Đang tắt" là token dừng của CHÍNH hàng đợi đã huỷ, không phải "ngoại lệ có kiểu OperationCanceledException": một
// việc hết thời gian chờ (TaskCanceledException bọc TimeoutException) trong lúc tiến trình vẫn chạy là một việc HỎNG,
// phải để lại dòng log Error và chỉ số core.job.failed như mọi việc hỏng khác.
public sealed class BackgroundJobQueueHostedServiceTests
{
    private sealed class CollectingLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message)> Entries
        {
            get
            {
                lock (_entries)
                    return [.. _entries];
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_entries)
                _entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private static BackgroundJobQueueHostedService Service(Channel<QueuedJob> channel, ILogger<BackgroundJobQueueHostedService> logger)
    {
        var provider = new ServiceCollection().BuildServiceProvider();

        // Một chỗ chạy: việc thứ hai chỉ bắt đầu SAU KHI việc thứ nhất đã đi hết khối bắt lỗi của nó.
        return new BackgroundJobQueueHostedService(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            new ExecutionContextScope(),
            Options.Create(new CoreJobOptions { MaxConcurrent = 1 }),
            logger);
    }

    [Fact]
    public async Task AJobTimingOutAsTaskCanceled_WhileTheQueueIsNotStopping_IsLoggedAndCounted_AndTheNextJobStillRuns()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var logger = new CollectingLogger<BackgroundJobQueueHostedService>();
        var service = Service(channel, logger);
        var nextRan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);
        await service.StartAsync(CancellationToken.None);

        await channel.Writer.WriteAsync(new QueuedJob("viec-het-gio", async (_, _) =>
        {
            await Task.Yield();
            throw new TaskCanceledException("", new TimeoutException());
        }, null));
        await channel.Writer.WriteAsync(new QueuedJob("viec-ke-tiep", (_, _) =>
        {
            nextRan.SetResult();
            return Task.CompletedTask;
        }, null));

        await nextRan.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await service.StopAsync(CancellationToken.None);

        logger.Entries.ShouldContain(
            e => e.Level == LogLevel.Error && e.Message.Contains("viec-het-gio", StringComparison.Ordinal),
            "việc hết giờ trong lúc tiến trình còn chạy là việc hỏng — không được im lặng");
        probe.Count(CoreMetrics.BackgroundJobFailed.Name).ShouldBe(1);
    }

    // Chiều ngược lại — hàng đợi dừng thật: việc đang chạy nhận tín hiệu huỷ, dừng êm, không bị đếm là việc hỏng.
    [Fact]
    public async Task AJobCancelledBecauseTheQueueIsStopping_IsNotCountedAsAFailure()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var logger = new CollectingLogger<BackgroundJobQueueHostedService>();
        var service = Service(channel, logger);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);
        await service.StartAsync(CancellationToken.None);

        await channel.Writer.WriteAsync(new QueuedJob("viec-dang-chay", async (_, stoppingToken) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }, null));

        await started.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await service.StopAsync(CancellationToken.None);

        probe.Count(CoreMetrics.BackgroundJobFailed.Name).ShouldBe(0);
        logger.Entries.ShouldNotContain(e => e.Level == LogLevel.Error);
    }

    // ---- B33: mã trong dòng log của hàng đợi là mã của HÀNG ĐỢI, không phải JobId của core.job -------------------------------
    //
    // `{JobId}` đã là tên của core.job.id (Guid) ở mọi nơi khác; mã do EnqueueAsync trả về là một chuỗi khác hẳn. Hai mã mang
    // chung một tên trường làm truy vết sai (quyết định 2026-10-04, hướng d). Dòng nối hai mã nằm ở nơi đưa việc vào hàng đợi
    // (JobQueuedOutboxHandler) — ở đây khoá nửa của hàng đợi: tên trường là QueueItemId, và giá trị là ĐÚNG mã EnqueueAsync trả.

    public sealed class ProbeJob
    {
        public Task OkAsync(CancellationToken ct) => Task.CompletedTask;

        public async Task FailAsync(CancellationToken ct)
        {
            await Task.Yield();
            throw new InvalidOperationException("hỏng có chủ đích");
        }
    }

    // Ghi GIÁ TRỊ từng trường của dòng log có cấu trúc (không so chuỗi thông điệp), bỏ khoá `{OriginalFormat}`.
    private sealed class FieldCollectingLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, IReadOnlyDictionary<string, object?> Fields)> _entries = [];

        public IReadOnlyList<(LogLevel Level, IReadOnlyDictionary<string, object?> Fields)> Entries
        {
            get
            {
                lock (_entries)
                    return [.. _entries];
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var pairs = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            var fields = pairs.Where(pair => pair.Key != "{OriginalFormat}").ToDictionary(pair => pair.Key, pair => pair.Value);
            lock (_entries)
                _entries.Add((logLevel, fields));
        }
    }

    [Fact]
    public async Task QueueLines_CarryTheIdReturnedByEnqueue_UnderQueueItemId_AndNoJobIdField()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var executionScope = new ExecutionContextScope();
        var scheduler = new BackgroundJobScheduler(channel, executionScope);
        var logger = new FieldCollectingLogger<BackgroundJobQueueHostedService>();
        var provider = new ServiceCollection().AddSingleton<ProbeJob>().BuildServiceProvider();
        var service = new BackgroundJobQueueHostedService(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            executionScope,
            Options.Create(new CoreJobOptions { MaxConcurrent = 1 }),
            logger);

        string idOk;
        string idFail;
        using (executionScope.Enter(Guid.NewGuid(), null, null))
        {
            idOk = await scheduler.EnqueueAsync<ProbeJob>((job, ct) => job.OkAsync(ct));
            idFail = await scheduler.EnqueueAsync<ProbeJob>((job, ct) => job.FailAsync(ct));
        }

        await service.StartAsync(CancellationToken.None);

        // Bắt đầu + kết thúc của việc xong, bắt đầu + Error của việc hỏng. Chờ đủ bốn dòng TRƯỚC khi dừng: dừng sớm thì bộ lọc
        // `when (!stoppingToken.IsCancellationRequested)` nuốt dòng Error.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (logger.Entries.Count < 4 && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        await service.StopAsync(CancellationToken.None);
        var entries = logger.Entries;

        entries.ShouldNotContain(e => e.Fields.ContainsKey("JobId"), "JobId là tên của core.job.id — dòng của hàng đợi không được dùng lại tên đó");

        var okLines = entries.Where(e => e.Fields.TryGetValue("QueueItemId", out var v) && Equals(v, idOk)).ToList();
        okLines.Count.ShouldBe(2, "bắt đầu và kết thúc của việc chạy xong đều mang QueueItemId = mã EnqueueAsync trả về");
        okLines.ShouldAllBe(e => e.Level == LogLevel.Information);

        var failLines = entries.Where(e => e.Fields.TryGetValue("QueueItemId", out var v) && Equals(v, idFail)).ToList();
        failLines.ShouldContain(e => e.Level == LogLevel.Error, "dòng Error của việc hỏng phải mang QueueItemId = mã EnqueueAsync trả về");
    }
}
