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
}
