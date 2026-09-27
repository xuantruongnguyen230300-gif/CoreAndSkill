using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Infrastructure.Outbox;

// Readiness trả `Degraded` khi Outbox có dòng `dead` HOẶC dòng `pending` cũ nhất quá ngưỡng —
// docs/wiki-core/be/12-notifications.md §2.5, docs/wiki-core/be/07-observability.md §8.
//
// Degraded, không Unhealthy: outbox kẹt KHÔNG làm request phục vụ sai — chỉ là thông báo không tới. Nhưng
// nó phải HIỆN RA: "một bản ghi kẹt mà không ai biết là một thông báo không bao giờ tới, và người dùng chỉ
// phát hiện khi hậu quả đã xảy ra" (§2.5 yêu cầu 1). Health check là endpoint công khai nên KHÔNG trả chi
// tiết nội bộ — chỉ trạng thái và một câu chung; chi tiết ở log và chỉ số.
internal sealed class OutboxHealthCheck(
    IServiceScopeFactory scopeFactory,
    IOptions<CoreOutboxOptions> options,
    TimeProvider timeProvider)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        OutboxSnapshot snapshot;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            snapshot = await OutboxSnapshot.MeasureAsync(db, timeProvider.GetUtcNow(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Không đo được outbox: DB không với tới, hoặc bảng chưa có (script chưa áp). Readiness đã
            // có DatabaseHealthCheck cho hai ca đó; ở đây báo đỏ chung, không lộ thông điệp lỗi.
            return HealthCheckResult.Unhealthy("Không kiểm được trạng thái outbox.", ex);
        }

        return Evaluate(snapshot, TimeSpan.FromMinutes(options.Value.DegradedPendingAgeMinutes));
    }

    // Quyết định thuần, tách khỏi việc đo để kiểm được mà không cần database (đo bằng SQL là phần RequiresDocker).
    internal static HealthCheckResult Evaluate(OutboxSnapshot snapshot, TimeSpan degradedPendingAge)
    {
        var stalled = snapshot.OldestPendingAge > degradedPendingAge;

        if (snapshot.DeadCount > 0 || stalled)
            return HealthCheckResult.Degraded("Outbox có bản ghi cần người xem.");

        return HealthCheckResult.Healthy();
    }
}
