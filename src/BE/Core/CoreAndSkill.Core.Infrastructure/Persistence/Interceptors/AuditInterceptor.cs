using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;

// Điền bốn cột audit — docs/quy-uoc/be-entity-domain.md §1.3. Chọn entity theo INTERFACE
// (IAuditableEntity), không theo BaseEntity — phủ cả Tenant/AppUser (§1.4). Cả bốn field lấy CHUNG
// một biến `now` trong cùng lượt SaveChanges. `createdBy` là TÊN ĐĂNG NHẬP, không phải id
// (docs/database/schema-core.md §3.2). Không có người (job nền, bootstrap) → ghi SystemActor.UserName —
// docs/quy-uoc/be-architecture.md §1.1. Injects ICurrentUser (seam của Application) — hiện thực
// thật (HttpContextCurrentUser) sống ở Core.Web; Infrastructure chỉ biết interface, không vi phạm
// chiều phụ thuộc.
public sealed class AuditInterceptor(TimeProvider timeProvider, ICurrentUser currentUser)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();
        var actor = currentUser.UserName ?? SystemActor.UserName;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = actor;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actor;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actor;
                    break;
            }
        }
    }
}
