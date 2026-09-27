using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §9.7. Tên bảng là `file` (số ít), entity là StoredFile để khỏi trùng
// System.IO.File trong mọi tệp có using System.IO.
internal sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("file");
        builder.HasKey(x => x.Id).HasName("pk_file");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OwnerTable).HasMaxLength(100);
        builder.Property(x => x.OriginalName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Purpose).HasMaxLength(50);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_file_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.OwnerTable, x.OwnerId })
            .HasDatabaseName("ix_file_owner");

        // Tên theo quy ước §2.2/§2.3 (unique lọc xoá mềm ⇒ `ux_` + `tenant` + `_active`), không theo
        // `uq_file_storage_key` mà bảng ở §9.7 từng ghi — `uq_` dành cho ràng buộc KHÔNG lọc.
        builder.HasIndex(x => new { x.TenantId, x.StorageKey })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_file_tenant_storage_key_active");
    }
}
