using System.Linq.Expressions;

namespace CoreAndSkill.Core.Application.Common.Interfaces;

// Seam job nền — docs/quy-uoc/be-cqrs-handler.md §10.2, docs/quy-uoc/be-architecture.md §1.1. Chỉ
// dùng System.Linq.Expressions của BCL — không lộ kiểu nào của cơ chế chạy nền cụ thể ra
// Core.Application. v1 hiện thực bằng BackgroundService của .NET (docs/wiki-core/be/18-trien-khai-va-van-hanh.md
// §7) — hiện thực ở Core.Infrastructure.
public interface IBackgroundJobScheduler
{
    Task<string> EnqueueAsync<TJob>(
        Expression<Func<TJob, CancellationToken, Task>> methodCall,
        CancellationToken ct = default);

    Task<string> ScheduleAsync<TJob>(
        Expression<Func<TJob, CancellationToken, Task>> methodCall,
        TimeSpan delay,
        CancellationToken ct = default);
}
