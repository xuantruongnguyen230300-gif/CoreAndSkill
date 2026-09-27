using CoreAndSkill.Core.Application.Common.Logging;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Infrastructure.Outbox;

// Tiến trình phát Outbox — một BackgroundService ĐỊNH KỲ đứng riêng, KHÔNG đi qua IBackgroundJobScheduler
// (docs/adr/0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md: việc lặp theo chu kỳ là loại thứ hai; việc
// chạy-một-lần theo jobId mới đi qua seam). Chính nó lại là bên gọi seam đó khi nhận JobQueuedEvent.
//
// Mỗi nhịp: (1) phát tới khi không còn dòng nào tới hạn, (2) đo trạng thái để cập nhật chỉ số cảnh báo
// sớm, (3) thi thoảng dọn dòng `done` quá hạn. Nhịp hỏng KHÔNG làm chết dịch vụ — nhưng cũng KHÔNG im
// lặng: DB không với tới được thì bản ghi nằm nguyên trong bảng chờ, còn dịch vụ để lại dấu vết (log
// Error có tiết chế + chỉ số) và readiness/DB health check báo đỏ.
internal sealed class OutboxDispatcherHostedService(
    OutboxDispatcher dispatcher,
    IServiceScopeFactory scopeFactory,
    IOptions<CoreOutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcherHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    private int _consecutiveFailures;
    private DateTimeOffset _lastCleanup = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollSeconds), timeProvider);

        await RunOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnceAsync(stoppingToken);
    }

    // internal — test gọi thẳng một nhịp mà không lái PeriodicTimer thật.
    internal async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            // Phát tới khi cạn — một nhịp không được bỏ lại dòng đã tới hạn chỉ vì lô đầy.
            while (await dispatcher.DispatchBatchAsync(ct) > 0)
            {
            }

            await dispatcher.MeasureAsync(ct);
            await CleanupDoneAsync(ct);

            _consecutiveFailures = 0;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Lọc theo TOKEN, không theo kiểu: chỉ token của chính vòng này đã huỷ mới là "đang tắt". TaskCanceledException vì
            // hết thời gian chờ mà thoát khỏi đây thì BackgroundService hỏng và host (StopHost mặc định) dừng cả tiến trình.
            _consecutiveFailures++;
            CoreMetrics.BackgroundJobFailed.Add(1);

            // Không log mỗi 5 giây khi DB mất kết nối: dòng đầu, rồi thưa dần. Chỉ số vẫn đếm mọi lần.
            if (_consecutiveFailures == 1 || _consecutiveFailures % 12 == 0)
            {
                logger.LogError(
                    "Nhịp phát outbox lỗi (lần liên tiếp thứ {Failures}): {Failure} — outbox nằm nguyên trong bảng chờ, không mất dữ liệu.",
                    _consecutiveFailures, FailureText.Describe(ex));
            }
        }
    }

    // Dòng đã phát được xoá sau Core:Outbox:DoneRetentionDays — không dọn thì bảng phình và tiến trình
    // chậm dần (12-notifications.md §2.3). Bỏ filter đơn vị có tên: dọn toàn hệ.
    private async Task CleanupDoneAsync(CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        if (now - _lastCleanup < CleanupInterval)
            return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var cutoff = now.AddDays(-options.Value.DoneRetentionDays);

        var deleted = await db.OutboxMessages
            .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
            .Where(o => o.Status == OutboxStatus.Done && o.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        _lastCleanup = now;

        if (deleted > 0)
            logger.LogInformation("Đã dọn {Count} dòng outbox đã phát quá {Days} ngày.", deleted, options.Value.DoneRetentionDays);
    }
}
