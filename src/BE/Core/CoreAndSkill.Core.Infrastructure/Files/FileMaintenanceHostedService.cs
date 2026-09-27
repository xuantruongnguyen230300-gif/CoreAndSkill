using System.Linq.Expressions;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Common.Logging;
using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Infrastructure.Files;

// Job định kỳ bảo trì kho tệp — docs/wiki-core/be/14-file-storage.md §5.2, §6. Là BackgroundService
// ĐỨNG RIÊNG, không đi qua IBackgroundJobScheduler: đây là việc LẶP THEO CHU KỲ (ADR-0047).
//
// Bốn việc, mỗi việc bọc riêng để một việc hỏng không kéo việc khác (2b: gỡ tệp tải lên không bao giờ được gắn,
// Core:File:UnattachedRetentionHours — §3.1):
//   1. Dọn tệp TẠM quá hạn. Phải là job định kỳ chứ KHÔNG chỉ khối dọn trong code — tệp tạm còn lại luôn
//      đến từ ca tiến trình CHẾT, tức đúng ca khối dọn dẹp không chạy (§6).
//   2. Gỡ bản ghi tệp KẾT QUẢ của việc nền quá hạn (Core:File:ResultFileRetentionDays) — tệp tạm có
//      hạn dùng.
//   3. Đối soát hai chiều đĩa <-> DB (§5.2):
//        - tệp trên đĩa KHÔNG có bản ghi, cũ hơn khoảng an toàn ⇒ XOÁ. Khoảng an toàn là bắt buộc: nếu
//          không job sẽ xoá đúng tệp vừa tải lên trong lúc giao dịch chưa kịp commit.
//        - tệp của bản ghi đã gỡ (xoá mềm) quá khoảng an toàn ⇒ xoá tệp vật lý, GIỮ bản ghi làm lịch sử.
//        - bản ghi KHÔNG có tệp ⇒ BÁO CÁO, KHÔNG tự xoá: đó là dấu hiệu có gì đó sai, cần người xem.
//
// Job toàn hệ, không chạy trong ngữ cảnh một đơn vị: bỏ bộ lọc ĐƠN VỊ có tên (luật M5/B6) — không bao
// giờ gọi IgnoreQueryFilters không tham số. Mỗi phép bỏ nêu đúng filter nó bỏ.
internal sealed class FileMaintenanceHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CoreFileOptions> options,
    TimeProvider timeProvider,
    ILogger<FileMaintenanceHostedService> logger)
    : BackgroundService
{
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.Value.MaintenanceIntervalHours), timeProvider);

        await RunOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnceAsync(stoppingToken);
    }

    // internal — test gọi thẳng một lượt mà không lái PeriodicTimer thật.
    internal async Task RunOnceAsync(CancellationToken ct)
    {
        await GuardedAsync("dọn tệp tạm", PurgeTempAsync, ct);
        await GuardedAsync("gỡ tệp kết quả quá hạn", ExpireResultFilesAsync, ct);
        await GuardedAsync("gỡ tệp không bao giờ được gắn", ExpireUnattachedFilesAsync, ct);
        await GuardedAsync("đối soát đĩa và DB", ReconcileAsync, ct);
    }

    private async Task GuardedAsync(string name, Func<IServiceScope, CancellationToken, Task> step, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await step(scope, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Lọc theo TOKEN, không theo kiểu: chỉ token của chính lượt này đã huỷ mới là "đang tắt". Hết thời gian chờ đi ra
            // thành TaskCanceledException trong lúc tiến trình còn chạy là bước hỏng như mọi bước hỏng khác.
            // Không nuốt: log Error + chỉ số, việc chạy lại ở chu kỳ sau.
            CoreMetrics.BackgroundJobFailed.Add(1);
            logger.LogError("Bảo trì kho tệp — bước '{Step}' lỗi: {Failure} — cần người can thiệp.", name, FailureText.Describe(ex));
        }
    }

    private async Task PurgeTempAsync(IServiceScope scope, CancellationToken ct)
    {
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var cutoff = timeProvider.GetUtcNow().AddHours(-options.Value.TempRetentionHours);

        var deleted = await storage.PurgeTempAsync(cutoff, ct);
        if (deleted > 0)
            logger.LogInformation("Đã dọn {Count} tệp tạm quá hạn.", deleted);
    }

    private async Task ExpireResultFilesAsync(IServiceScope scope, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var now = timeProvider.GetUtcNow();
        var cutoff = now.AddDays(-options.Value.ResultFileRetentionDays);

        var expired = await db.Files
            .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
            .Where(f => f.Purpose == CoreFilePurposes.JobResult && f.CreatedAt < cutoff)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.IsDeleted, true)
                .SetProperty(f => f.UpdatedAt, now)
                .SetProperty(f => f.UpdatedBy, SystemActor.UserName), ct);

        if (expired > 0)
            logger.LogInformation("Đã gỡ {Count} tệp kết quả của việc nền quá hạn — tệp vật lý dọn ở bước đối soát.", expired);
    }

    // Tệp tải lên rồi bỏ form: owner_table NULL mãi, đối soát coi là "còn sống" nên không bao giờ dọn (§3.1).
    // Gỡ mềm bản ghi; tệp vật lý đi theo đường "bản ghi đã gỡ quá khoảng an toàn" ở bước đối soát.
    private async Task ExpireUnattachedFilesAsync(IServiceScope scope, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var now = timeProvider.GetUtcNow();

        var expired = await db.Files
            .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
            .Where(IsUnattachedBefore(now.AddHours(-options.Value.UnattachedRetentionHours)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.IsDeleted, true)
                .SetProperty(f => f.UpdatedAt, now)
                .SetProperty(f => f.UpdatedBy, SystemActor.UserName), ct);

        if (expired > 0)
        {
            CoreMetrics.FileUnattachedExpired.Add(expired);
            logger.LogInformation("Đã gỡ {Count} tệp tải lên nhưng không bao giờ được gắn — tệp vật lý dọn ở bước đối soát.", expired);
        }
    }

    // internal — phép chọn kiểm được trên bộ nhớ, không cần DB. !IsDeleted tường minh (bộ lọc xoá mềm cũng loại, nhưng
    // phép chọn không được dựa vào nó): gỡ lại một bản ghi đã gỡ sẽ đẩy UpdatedAt lên mãi, tệp vật lý không bao giờ bị dọn.
    internal static Expression<Func<StoredFile, bool>> IsUnattachedBefore(DateTimeOffset cutoff)
        => f => f.OwnerTable == null && !f.IsDeleted && f.CreatedAt < cutoff;

    private async Task ReconcileAsync(IServiceScope scope, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var graceCutoff = timeProvider.GetUtcNow().AddHours(-options.Value.OrphanGraceHours);

        var orphansDeleted = await DeleteOrphansAsync(db, storage, graceCutoff, ct);
        var missing = await CountMissingContentAsync(db, storage, ct);

        if (orphansDeleted > 0)
        {
            CoreMetrics.FileOrphansDeleted.Add(orphansDeleted);
            logger.LogInformation("Đối soát kho tệp: đã xoá {Count} tệp không còn bản ghi (mồ côi hoặc đã gỡ).", orphansDeleted);
        }

        if (missing.Count > 0)
        {
            CoreMetrics.FileContentMissing.Add(missing.Count);

            // Chỉ định danh bản ghi (tối đa 10), không kèm khoá kho lưu hay tên tệp.
            logger.LogWarning(
                "Đối soát kho tệp: {Count} bản ghi tệp KHÔNG còn nội dung trong kho lưu (ví dụ {SampleIds}) — " +
                "không tự xoá, cần người xem (phục hồi DB và kho tệp về hai mốc khác nhau? tệp bị xoá tay?).",
                missing.Count, string.Join(", ", missing.Sample));
        }
    }

    // Chiều "tệp trên đĩa, không có bản ghi" + "bản ghi đã gỡ quá khoảng an toàn".
    private async Task<int> DeleteOrphansAsync(CoreDbContext db, IFileStorage storage, DateTimeOffset graceCutoff, CancellationToken ct)
    {
        var deleted = 0;
        var batch = new List<StoredObject>(BatchSize);

        await foreach (var stored in storage.ListAsync(ct))
        {
            batch.Add(stored);
            if (batch.Count < BatchSize)
                continue;

            deleted += await ProcessBatchAsync(db, storage, batch, graceCutoff, ct);
            batch.Clear();
        }

        if (batch.Count > 0)
            deleted += await ProcessBatchAsync(db, storage, batch, graceCutoff, ct);

        return deleted;
    }

    private static async Task<int> ProcessBatchAsync(
        CoreDbContext db, IFileStorage storage, List<StoredObject> batch, DateTimeOffset graceCutoff, CancellationToken ct)
    {
        var keys = batch.Select(b => b.Key).ToList();

        // Bỏ CẢ HAI filter, nêu tên từng cái: đây là job toàn hệ và cần thấy cả bản ghi đã gỡ.
        var rows = await db.Files
            .IgnoreQueryFilters([CoreQueryFilters.TenantKey, CoreQueryFilters.SoftDeleteKey])
            .Where(f => keys.Contains(f.StorageKey))
            .Select(f => new { f.StorageKey, f.IsDeleted, f.UpdatedAt })
            .ToListAsync(ct);

        // Một khoá có thể có nhiều dòng (đã gỡ rồi tái dùng khoá — không xảy ra vì khoá sinh mới mỗi
        // lần, nhưng đừng để một dòng sống bị che bởi dòng đã gỡ): còn MỘT dòng sống là còn tham chiếu.
        var live = rows.Where(r => !r.IsDeleted).Select(r => r.StorageKey).ToHashSet(StringComparer.Ordinal);
        var removedBefore = rows
            .Where(r => r.IsDeleted && r.UpdatedAt < graceCutoff)
            .Select(r => r.StorageKey)
            .ToHashSet(StringComparer.Ordinal);
        var known = rows.Select(r => r.StorageKey).ToHashSet(StringComparer.Ordinal);

        var deleted = 0;
        foreach (var stored in batch)
        {
            if (live.Contains(stored.Key))
                continue;

            var isOrphan = !known.Contains(stored.Key) && stored.LastWriteUtc < graceCutoff;
            var isRemoved = removedBefore.Contains(stored.Key);

            if (!isOrphan && !isRemoved)
                continue;

            await storage.DeleteAsync(stored.Key, ct);
            deleted++;
        }

        return deleted;
    }

    // Chiều "bản ghi, không có tệp": duyệt keyset theo Id, chỉ ĐẾM và lấy mẫu.
    private async Task<(int Count, IReadOnlyList<Guid> Sample)> CountMissingContentAsync(
        CoreDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var count = 0;
        var sample = new List<Guid>();
        var afterId = Guid.Empty;

        while (true)
        {
            var afterCopy = afterId;
            var rows = await db.Files
                .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
                .Where(f => f.Id > afterCopy)
                .OrderBy(f => f.Id)
                .Take(BatchSize)
                .Select(f => new { f.Id, f.StorageKey })
                .ToListAsync(ct);

            if (rows.Count == 0)
                break;

            foreach (var row in rows)
            {
                if (await storage.ExistsAsync(row.StorageKey, ct))
                    continue;

                count++;
                if (sample.Count < 10)
                    sample.Add(row.Id);
            }

            afterId = rows[^1].Id;
        }

        return (count, sample);
    }
}

