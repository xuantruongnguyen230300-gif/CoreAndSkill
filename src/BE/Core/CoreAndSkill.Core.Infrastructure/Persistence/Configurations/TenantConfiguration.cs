using CoreAndSkill.Core.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §1.3. Tenant KHÔNG khai ITenantScoped (là GỐC) — vòng lặp lọc
// tenant ở CoreQueryFilters bỏ qua nó tự động. Vẫn kế thừa lọc SoftDelete? KHÔNG — Tenant không kế
// thừa BaseEntity nên vòng lặp SoftDelete cũng bỏ qua nó (đúng — tenant không bao giờ bị xoá).
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenant");
        builder.HasKey(x => x.Id).HasName("pk_tenant");

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.IsSystem).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        // Unique constraint ĐẦY ĐỦ, không lọc — docs/database/schema-core.md §1.3.
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_tenant_code");

        // Unique index MỘT PHẦN — nhiều nhất một đơn vị hệ thống (luật M10).
        builder.HasIndex(x => x.IsSystem)
            .IsUnique()
            .HasFilter("is_system")
            .HasDatabaseName("ux_tenant_is_system");
    }
}
