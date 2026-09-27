using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Maintenance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Infrastructure.Jobs;

// Job nền ĐẦU TIÊN của Core, chạy trên BackgroundService của .NET — docs/wiki-core/be/trien-khai/04-b3-van-hanh.md
// §1, §3; docs/wiki-core/be/18-trien-khai-va-van-hanh.md §7. KHÔNG đi qua IBackgroundJobScheduler:
// seam đó sinh cho việc CHẠY MỘT LẦN theo yêu cầu (docs/quy-uoc/be-cqrs-handler.md §10.2), còn đây
// là một vòng lặp ĐỊNH KỲ — hai hình dạng khác nhau, không ép chung một seam.
//
// Job nền TOÀN HỆ, không chạy trong ngữ cảnh request — docs/wiki-core/be/17-multi-tenant.md §7:
// KHÔNG mở phạm vi ngữ cảnh nào ở đây; từng IStaleDataCleaner tự quyết cách truy cập dữ liệu của
// nó (kể cả việc có cần bỏ bộ lọc đơn vị hay không) — hosted service này chỉ điều phối vòng lặp
// và ghi log bắt đầu/kết thúc/số bản ghi (docs/wiki-core/be/07-observability.md §6).
//
// Job là Singleton, cleaner của dự án hạ nguồn thì Scoped (cần DbContext) — docs/quy-uoc/be-architecture.md §5.2 "luật
// cứng": không giữ Scoped trong Singleton. Mỗi cleaner chạy trong MỘT phạm vi DI riêng, mở mới ở mỗi lượt quét: một
// cleaner vỡ giữa chừng không để lại DbContext bẩn cho cleaner kế tiếp.
//
// v1 KHÔNG có IStaleDataCleaner nào đăng ký (docs/wiki-core/be/10-data-retention.md §8) — mỗi chu
// kỳ chạy qua một danh sách RỖNG, không xoá gì. Cơ chế đã sẵn sàng, chờ chính sách lưu giữ.
//
// Job KHÔNG truyền mốc thời gian cho cleaner (docs/adr/0060-cleaner-tu-khai-nguong-luu-giu.md): ngưỡng lưu giữ là của
// từng cleaner, khai trong options riêng của nó và tự tính bằng TimeProvider. TimeProvider ở đây chỉ lái PeriodicTimer.
internal sealed class StaleDataCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CoreBackgroundJobOptions> options,
    TimeProvider timeProvider,
    ILogger<StaleDataCleanupHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(options.Value.StaleDataCleanupIntervalHours);
        using var timer = new PeriodicTimer(interval, timeProvider);

        // Chạy một lượt ngay khi khởi động — không đợi hết chu kỳ đầu tiên mới có lần quét nào.
        await RunOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnceAsync(stoppingToken);
    }

    // internal — chỉ để test gọi thẳng một lượt quét mà không phải lái PeriodicTimer thật
    // (docs/quy-uoc/be-architecture.md §9.1, InternalsVisibleTo khai cho IntegrationTests).
    internal async Task RunOnceAsync(CancellationToken ct)
    {
        // Cleaner thứ `index` resolve trong phạm vi RIÊNG của nó. Thứ tự resolve của DI ổn định theo thứ tự đăng ký.
        for (var index = 0; ; index++)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var cleaner = scope.ServiceProvider.GetServices<IStaleDataCleaner>().ElementAtOrDefault(index);
            if (cleaner is null)
                return; // Hết cleaner (hoặc không có cái nào) — không log ồn ào mỗi chu kỳ khi không có việc.

            await RunCleanerAsync(cleaner, ct);
        }
    }

    private async Task RunCleanerAsync(IStaleDataCleaner cleaner, CancellationToken ct)
    {
        logger.LogInformation("Job nền dọn dữ liệu '{CleanerName}' bắt đầu.", cleaner.Name);
        try
        {
            var count = await cleaner.CleanAsync(ct);
            logger.LogInformation(
                "Job nền dọn dữ liệu '{CleanerName}' kết thúc — đã xoá {Count} bản ghi.", cleaner.Name, count);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Lọc theo TOKEN, không theo kiểu: TaskCanceledException vì hết thời gian chờ trong lúc tiến trình còn chạy là lỗi
            // của cleaner này. Để nó thoát ra thì BackgroundService hỏng và host (StopHost mặc định) dừng cả tiến trình.
            CoreMetrics.BackgroundJobFailed.Add(1);
            logger.LogError(ex, "Job nền dọn dữ liệu '{CleanerName}' lỗi — cần người can thiệp.", cleaner.Name);
        }
    }
}
