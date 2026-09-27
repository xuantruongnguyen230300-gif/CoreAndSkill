using System.Threading.Channels;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Infrastructure.Jobs;

// Phía TIÊU THỤ của BackgroundJobScheduler — rút từng QueuedJob khỏi kênh, mở một DI scope MỚI
// (job nền KHÔNG có HttpContext — docs/quy-uoc/be-architecture.md §1.1), khôi phục phạm vi ngữ
// cảnh đã chụp lúc enqueue rồi chạy. Một job lỗi KHÔNG làm chết hosted service — log Error rồi chờ
// job kế tiếp (docs/wiki-core/be/07-observability.md §6 "Job nền: bắt đầu, kết thúc, số bản ghi xử lý").
//
// Chạy tối đa Core:Jobs:MaxConcurrent việc CÙNG LÚC (docs/wiki-core/be/15-import-export.md §6): nhiều
// người cùng nhập tệp lớn là cách nhanh nhất làm cạn bộ nhớ và kết nối DB. Việc thứ (N+1) trở đi nằm
// trong kênh chờ chỗ — trạng thái của nó trong core.job vẫn là `queued` cho tới khi thật sự được nhặt.
internal sealed class BackgroundJobQueueHostedService(
    Channel<QueuedJob> channel,
    IServiceScopeFactory scopeFactory,
    IExecutionContextScope executionContextScope,
    IOptions<CoreJobOptions> options,
    ILogger<BackgroundJobQueueHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var slots = new SemaphoreSlim(options.Value.MaxConcurrent);
        var inFlight = new HashSet<Task>();

        try
        {
            await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
            {
                await slots.WaitAsync(stoppingToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await RunAsync(job, stoppingToken);
                    }
                    finally
                    {
                        slots.Release();
                    }
                }, CancellationToken.None);

                lock (inFlight)
                    inFlight.Add(task);

                _ = task.ContinueWith(
                    finished =>
                    {
                        lock (inFlight)
                            inFlight.Remove(finished);
                    },
                    TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Dừng tiến trình: không nhặt việc mới, chờ việc đang chạy nhận tín hiệu huỷ.
        }
        finally
        {
            Task[] pending;
            lock (inFlight)
                pending = [.. inFlight];

            try
            {
                await Task.WhenAll(pending);
            }
            catch (OperationCanceledException)
            {
                // Việc đang chạy bị huỷ theo tiến trình — trạng thái `running` của nó do
                // JobRecoveryHostedService xử lý ở lần khởi động sau.
            }
        }
    }

    private async Task RunAsync(QueuedJob job, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        using var ctxScope = job.Scope is { TenantId: { } tenantId }
            ? executionContextScope.Enter(tenantId, job.Scope.UserId, job.Scope.UserName)
            : null;

        logger.LogInformation("Job nền {JobId} bắt đầu.", job.JobId);
        try
        {
            await job.Run(scope.ServiceProvider, stoppingToken);
            logger.LogInformation("Job nền {JobId} kết thúc.", job.JobId);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // Lọc theo TOKEN, không theo kiểu: việc hết thời gian chờ (TaskCanceledException bọc TimeoutException) trong lúc
            // hàng đợi còn chạy là việc HỎNG — không được lọt qua đây im lặng như một lần dừng tiến trình.
            CoreMetrics.BackgroundJobFailed.Add(1);
            logger.LogError(ex, "Job nền {JobId} lỗi — cần người can thiệp.", job.JobId);
        }
    }
}
