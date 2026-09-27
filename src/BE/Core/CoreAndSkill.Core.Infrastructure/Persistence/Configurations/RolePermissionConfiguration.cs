using CoreAndSkill.Core.Domain.Permissions;
using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §5.3. PK ĐƠN trên Id (không ghép) — bẫy soft-delete §3.3: khoá
// chính không bao giờ là khoá nghiệp vụ.
internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permission");
        builder.HasKey(x => x.Id).HasName("pk_role_permission");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_role_permission_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .HasConstraintName("fk_role_permission_role_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .HasConstraintName("fk_role_permission_permission_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.RoleId, x.PermissionId })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_role_permission_tenant_role_perm_active");

        // Bắt buộc — phục vụ truy vấn kiểm quyền trên mọi request có [RequirePermission].
        builder.HasIndex(x => new { x.TenantId, x.PermissionId, x.RoleId })
            .HasDatabaseName("ix_role_permission_tenant_perm_role");
    }
}
