using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §4.3. PK giữ (user_id, role_id) như Identity mặc định.
internal sealed class AppUserRoleConfiguration : IEntityTypeConfiguration<AppUserRole>
{
    public void Configure(EntityTypeBuilder<AppUserRole> builder)
    {
        builder.ToTable("app_user_role");
        builder.Property(x => x.TenantId).IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_app_user_role_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Đặt tên tường minh — mặc định của Identity/EF sinh tên còn giữ mảnh "AspNetUsers"/"AspNetRoles"
        // dù bảng đã đổi tên (docs/database/schema-core.md §2.2, khuôn fk_<bảng>_<cột>).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_app_user_role_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .HasConstraintName("fk_app_user_role_role_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.RoleId })
            .HasDatabaseName("ix_app_user_role_tenant_role_id");
    }
}
