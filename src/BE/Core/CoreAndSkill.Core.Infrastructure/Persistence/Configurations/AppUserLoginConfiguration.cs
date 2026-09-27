using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §4.3 — PK ĐỔI so với mặc định của Identity: (tenant_id,
// login_provider, provider_key) thay vì (login_provider, provider_key). Chưa dùng ở v1.
internal sealed class AppUserLoginConfiguration : IEntityTypeConfiguration<AppUserLogin>
{
    public void Configure(EntityTypeBuilder<AppUserLogin> builder)
    {
        builder.ToTable("app_user_login");
        builder.Property(x => x.TenantId).IsRequired();

        builder.HasKey(x => new { x.TenantId, x.LoginProvider, x.ProviderKey })
            .HasName("pk_app_user_login");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_app_user_login_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_app_user_login_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasDatabaseName("ix_app_user_login_tenant_user_id");
    }
}
