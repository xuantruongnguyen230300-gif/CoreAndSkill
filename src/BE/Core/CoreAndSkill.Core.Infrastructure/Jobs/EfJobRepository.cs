using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Jobs;

internal sealed class EfJobRepository(CoreDbContext db, ICurrentUser currentUser) : IJobRepository
{
    public async Task AddAsync(Job job, CancellationToken ct)
        => await db.Jobs.AddAsync(job, ct);

    public Task<Job?> FindByIdAsync(Guid id, CancellationToken ct)
        => db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<Job?> FindForReadAsync(Guid id, CancellationToken ct)
        => db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);

    // MỘT câu UPDATE có điều kiện: hai bộ chạy cùng nhặt một việc thì chỉ một bên thấy "1 dòng bị đổi".
    // ExecuteUpdate không đi qua ChangeTracker nên AuditInterceptor không chạy — tự điền hai cột vết.
    public async Task<bool> TryClaimAsync(Guid id, DateTimeOffset now, CancellationToken ct)
    {
        var actor = currentUser.UserName ?? SystemActor.UserName;

        var changed = await db.Jobs
            .Where(j => j.Id == id && j.Status == JobStatus.Queued)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, JobStatus.Running)
                .SetProperty(j => j.StartedAt, (DateTimeOffset?)now)
                .SetProperty(j => j.UpdatedAt, (DateTimeOffset?)now)
                .SetProperty(j => j.UpdatedBy, actor), ct);

        return changed == 1;
    }

    public async Task UpdateProgressAsync(Guid id, int progress, CancellationToken ct)
    {
        var clamped = (short)Math.Clamp(progress, 0, 100);

        // Chỉ đổi khi còn `running`: việc đã kết thúc không bị kéo lùi tiến độ bởi một lời gọi muộn.
        await db.Jobs
            .Where(j => j.Id == id && j.Status == JobStatus.Running)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Progress, clamped), ct);
    }
}
