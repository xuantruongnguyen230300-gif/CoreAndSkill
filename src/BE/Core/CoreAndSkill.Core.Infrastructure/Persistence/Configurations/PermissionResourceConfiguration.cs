using CoreAndSkill.Core.Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §5.1. KHÔNG mang tenant_id — miễn trừ đã khai ở §1.3.
internal sealed class PermissionResourceConfiguration : IEntityTypeConfiguration<PermissionResource>
{
    public void Configure(EntityTypeBuilder<PermissionResource> builder)
    {
        builder.ToTable("permission_resource");
        builder.HasKey(x => x.Id).HasName("pk_permission_resource");

        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ModuleKey).HasMaxLength(100);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        // Unique ĐẦY ĐỦ, KHÔNG lọc — Postgres không nhận index một phần làm đích của foreign key
        // (Permission.ResourceKey trỏ vào đây) — ngoại lệ có chủ đích của §3.3. Khai bằng
        // HasAlternateKey (không phải HasIndex().IsUnique()): Key là PRINCIPAL KEY của quan hệ với
        // Permission.ResourceKey — EF tự quy nó thành một alternate key bất kể khai bằng cách nào;
        // dùng thẳng HasAlternateKey để kiểm soát TÊN ràng buộc sinh ra khớp quy ước schema-core.md §5.2.
        builder.HasAlternateKey(x => x.Key).HasName("uq_permission_resource_key");
        builder.HasIndex(x => x.ModuleKey).HasDatabaseName("ix_permission_resource_module_key");
    }
}
