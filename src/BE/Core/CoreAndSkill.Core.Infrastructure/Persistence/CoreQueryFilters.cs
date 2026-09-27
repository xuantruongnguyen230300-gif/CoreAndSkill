using System.Linq.Expressions;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Persistence;

// Nguồn DUY NHẤT của chữ ký ITenantFilteredContext và hai bộ lọc toàn cục — định nghĩa gốc,
// docs/quy-uoc/be-entity-domain.md §5.1. Dùng chung cho CoreDbContext và DbContext của MỌI module.
// Cả hai filter khai MỘT LẦN — vòng lặp trên model đã build, gọi ở CUỐI OnModelCreating, SAU
// ApplyConfigurationsFromAssembly. KHÔNG .Where(x => !x.IsDeleted) ở từng query, KHÔNG khai lẻ ở
// từng IEntityTypeConfiguration<T>.
public interface ITenantFilteredContext
{
    // Đọc MỖI LẦN truy vấn chạy, không phải một lần lúc dựng model.
    Guid? CurrentTenantId { get; }
}

public static class CoreQueryFilters
{
    public const string SoftDeleteKey = "SoftDelete";
    public const string TenantKey = "Tenant";

    public static void ApplyCoreQueryFilters<TContext>(this ModelBuilder modelBuilder, TContext context)
        where TContext : DbContext, ITenantFilteredContext
    {
        ApplySoftDeleteQueryFilters(modelBuilder);
        ApplyTenantQueryFilters(modelBuilder, context);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
                continue;
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(
                Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(SoftDeleteKey, Expression.Lambda(body, parameter));
        }
    }

    private static void ApplyTenantQueryFilters<TContext>(ModelBuilder modelBuilder, TContext context)
        where TContext : DbContext, ITenantFilteredContext
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
                continue;
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
                continue;

            var e = Expression.Parameter(entityType.ClrType, "e");
            // (Guid?)e.TenantId == context.CurrentTenantId
            // Vế phải trỏ vào INSTANCE DbContext, KHÔNG chụp một giá trị Guid lúc dựng model —
            // bẫy model cache, docs/wiki-core/be/17-multi-tenant.md §4.
            var body = Expression.Equal(
                Expression.Convert(Expression.Property(e, nameof(ITenantScoped.TenantId)), typeof(Guid?)),
                Expression.Property(
                    Expression.Constant(context, typeof(TContext)),
                    nameof(ITenantFilteredContext.CurrentTenantId)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(TenantKey, Expression.Lambda(body, e));
        }
    }
}
