using CoreAndSkill.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Persistence;

// `is_deleted` của mọi bảng kế thừa BaseEntity mang DEFAULT false — khai MỘT LẦN ở đây, vòng lặp trên model đã
// build như CoreQueryFilters (ADR-0119 quyết định 4, docs/database/schema-core.md §3.2). Câu INSERT viết tay
// của module (seed) khỏi phải nhớ ghi cột này. Gọi ở cuối OnModelCreating của CoreDbContext và DbContext của mọi module.
// EF coi false là giá trị CLR mặc định nên không gửi cột khi false — database điền; kết quả như cũ.
public static class CoreColumnDefaults
{
    public static void ApplyBaseEntityColumnDefaults(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
                continue;
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.IsDeleted))
                .HasDefaultValue(false);
        }
    }
}
