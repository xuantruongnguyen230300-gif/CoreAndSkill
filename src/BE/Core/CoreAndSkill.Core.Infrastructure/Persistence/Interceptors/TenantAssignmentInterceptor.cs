using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;

// Luật M2 + M8 — docs/wiki-core/be/17-multi-tenant.md §2, §5. TenantId gán MỘT LẦN lúc Added, từ
// ITenantContext (nguồn: claim của phiếu xác thực, hoặc IExecutionContextScope khi không có
// request). Chưa có đơn vị để gán → TỪ CHỐI LƯU (không bao giờ gán Guid.Empty). TenantId bị sửa
// lúc Modified → lỗi lập trình, ném ngoại lệ (không phải lỗi nghiệp vụ).
public sealed class TenantAssignmentInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
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

        foreach (var entry in context.ChangeTracker.Entries<ITenantScoped>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    {
                        var tenantId = tenantContext.TenantId
                            ?? throw new InvalidOperationException(
                                $"Không thể lưu '{entry.Entity.GetType().Name}' vì chưa có đơn vị trong ngữ cảnh hiện tại " +
                                "(luật M8 — docs/wiki-core/be/17-multi-tenant.md §2). Mở phạm vi ngữ cảnh thực thi trước khi ghi.");

                        entry.Property(nameof(ITenantScoped.TenantId)).CurrentValue = tenantId;
                        break;
                    }

                case EntityState.Modified:
                    {
                        // Bất biến là GIÁ TRỊ, không phải cờ: DbContext.Update(entity) của Identity (UserStore.UpdateAsync)
                        // đánh dấu MỌI cột là Modified dù giá trị giữ nguyên. Giá trị bằng nhau thì hạ cờ để câu UPDATE
                        // không ghi cột tenant_id.
                        var property = entry.Property(nameof(ITenantScoped.TenantId));
                        if (!property.IsModified)
                            break;

                        var comparer = property.Metadata.GetValueComparer();
                        var unchanged = comparer is null
                            ? Equals(property.OriginalValue, property.CurrentValue)
                            : comparer.Equals(property.OriginalValue, property.CurrentValue);
                        if (!unchanged)
                            throw new InvalidOperationException(
                                $"'{entry.Entity.GetType().Name}.TenantId' không bao giờ đổi sau khi tạo — lỗi lập trình.");

                        property.IsModified = false;
                        break;
                    }
            }
        }
    }
}
