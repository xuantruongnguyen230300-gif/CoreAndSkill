using CoreAndSkill.Core.Domain.Menu;
using CoreAndSkill.Core.Domain.Permissions;
using CoreAndSkill.Core.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §6.1.
internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_item", t => t.HasCheckConstraint(
            "ck_menu_item_not_self_parent", "parent_id IS NULL OR parent_id <> id"));
        builder.HasKey(x => x.Id).HasName("pk_menu_item");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LabelKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(50);
        builder.Property(x => x.Route).HasMaxLength(200);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ModuleKey).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_menu_item_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .HasConstraintName("fk_menu_item_parent_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.RequiredPermissionId)
            .HasConstraintName("fk_menu_item_required_permission_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_menu_item_tenant_code_active");

        builder.HasIndex(x => new { x.TenantId, x.ParentId, x.DisplayOrder })
            .HasDatabaseName("ix_menu_item_tenant_parent_order");
    }
}
