using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreAndSkill.Core.Infrastructure.Diagnostics;

// Readiness — docs/wiki-core/be/07-observability.md §8: "Readiness thất bại khi còn migration chưa
// áp" — mặt còn lại của luật E8. DatabaseSchemaStartupCheck (Core.Web, chạy lúc khởi động) chặn
// TOÀN BỘ tiến trình khi migration thiếu, và cũng khi không kết nối được database để kiểm (sau thời
// gian chờ Core:SchemaCheck:*). Nhưng đó là MỘT lần, lúc khởi động: sau đó database vẫn có thể mất
// kết nối, hoặc bị khôi phục về một bản thiếu migration — readiness phải tự kiểm lại ở MỖI lần gọi.
internal sealed class DatabaseHealthCheck(IOptions<CoreConnectionOptions> options, ISchemaVerifier schemaVerifier) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(options.Value.Core);
            await connection.OpenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Không kết nối được PostgreSQL.", ex);
        }

        IReadOnlyList<SchemaDriftReport> reports;
        try
        {
            reports = await schemaVerifier.InspectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Kết nối vừa mở được ở trên nhưng InspectAsync lại lỗi (vd. mất kết nối giữa chừng,
            // hoặc bảng __ef_migrations_history chưa tồn tại vì chưa chạy script lần nào) — vẫn là
            // KHÔNG SẴN SÀNG, không phải 500: readiness không lộ chi tiết nội bộ (§8 "hai nguyên tắc").
            return HealthCheckResult.Unhealthy("Không kiểm được trạng thái migration.", ex);
        }

        var missing = reports.Where(r => r.MissingInDatabase.Count > 0).ToList();
        if (missing.Count > 0)
        {
            var summary = string.Join("; ", missing.Select(r => $"{r.ContextName}: {r.MissingInDatabase.Count} migration"));
            return HealthCheckResult.Unhealthy($"Còn migration chưa áp — {summary}.");
        }

        return HealthCheckResult.Healthy();
    }
}
