using CoreAndSkill.Core.Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §5.2. KHÔNG mang tenant_id — miễn trừ đã khai ở §1.3. Nội dung
// (dòng dữ liệu) được nạp bằng migration ở B2 — B1 chỉ dựng bảng rỗng.
internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permission");
        builder.HasKey(x => x.Id).HasName("pk_permission");

        builder.Property(x => x.Code).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ResourceKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NameKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsSystem).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_permission_code_active");

        builder.HasIndex(x => new { x.ResourceKey, x.DisplayOrder })
            .HasDatabaseName("ix_permission_resource_key_display_order");

        // FK trỏ vào permission_resource.key (không phải id) — bắt buộc unique ĐẦY ĐỦ ở đích.
        builder.HasOne<PermissionResource>()
            .WithMany()
            .HasPrincipalKey(x => x.Key)
            .HasForeignKey(x => x.ResourceKey)
            .HasConstraintName("fk_permission_resource_key")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
