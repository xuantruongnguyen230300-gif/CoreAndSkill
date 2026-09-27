using CoreAndSkill.Core.Domain.Menu;
using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §6.2.
internal sealed class MenuItemRoleConfiguration : IEntityTypeConfiguration<MenuItemRole>
{
    public void Configure(EntityTypeBuilder<MenuItemRole> builder)
    {
        builder.ToTable("menu_item_role");
        builder.HasKey(x => x.Id).HasName("pk_menu_item_role");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_menu_item_role_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(x => x.MenuItemId)
            .HasConstraintName("fk_menu_item_role_menu_item_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .HasConstraintName("fk_menu_item_role_role_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.MenuItemId, x.RoleId })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_menu_item_role_tenant_item_role_active");

        builder.HasIndex(x => new { x.TenantId, x.RoleId })
            .HasDatabaseName("ix_menu_item_role_tenant_role_id");
    }
}
