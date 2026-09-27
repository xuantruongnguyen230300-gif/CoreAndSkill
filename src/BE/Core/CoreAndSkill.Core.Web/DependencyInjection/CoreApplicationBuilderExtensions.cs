using CoreAndSkill.Core.Web.Security;
using CoreAndSkill.Core.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAndSkill.Core.Web.DependencyInjection;

public static class CoreApplicationBuilderExtensions
{
    // Thứ tự pipeline đầy đủ — định nghĩa gốc ở docs/quy-uoc/be-architecture.md §3.1. BỐN chỗ
    // không hoán đổi được, xem chú thích tại chỗ. KHÔNG đổi thứ tự khi thêm middleware mới.
    public static async Task<WebApplication> UseCoreAsync(this WebApplication app)
    {
        // Luật E8 — docs/database/script-runbook.md §5.1, §5.2. Chạy TRƯỚC mọi middleware khác:
        // DB thiếu migration thì không phục vụ request nào, kể cả health check.
        await app.VerifyDatabaseSchemaAsync();

        // 1/4 — ĐẦU TIÊN. Proxy tin cậy nạp từ Core:Network:* ở AddCoreWeb (ForwardedHeadersOptions) — danh sách
        // rỗng ⇒ không tin proxy nào (ràng buộc 4 của be-architecture.md §3.1).
        app.UseForwardedHeaders();

        app.UseMiddleware<TraceIdMiddleware>();
        app.UseExceptionHandler();
        app.UseMiddleware<EnvelopeMiddleware>();

        // 3/4 — UseCors TRƯỚC mọi middleware chặn.
        app.UseCors(CorsPolicyNames.Default);

        app.UseAuthentication();

        // 2/4 — SAU UseAuthentication, TRƯỚC UseAuthorization (be-api-controller.md §7.2).
        app.UseMiddleware<AntiforgeryValidationMiddleware>();

        app.UseAuthorization();
        app.UseMiddleware<PasswordChangeRequiredMiddleware>(); // SAU UseAuthorization
        app.UseRateLimiter();
        app.MapControllers();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous()
            .DisableRateLimiting();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") })
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }

    // docs/database/script-runbook.md §5.1–§5.3. Toàn bộ phép kiểm — kể cả thử lại khi chưa kết nối được database, và dừng
    // tiến trình khi hết hạn — ở DatabaseSchemaStartupCheck.
    private static async Task VerifyDatabaseSchemaAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var check = ActivatorUtilities.CreateInstance<DatabaseSchemaStartupCheck>(scope.ServiceProvider);
        await check.RunAsync(app.Lifetime.ApplicationStopping);
    }
}
