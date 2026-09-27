using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

internal sealed class AppRoleClaimConfiguration : IEntityTypeConfiguration<AppRoleClaim>
{
    public void Configure(EntityTypeBuilder<AppRoleClaim> builder)
    {
        builder.ToTable("app_role_claim");
        builder.Property(x => x.TenantId).IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_app_role_claim_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .HasConstraintName("fk_app_role_claim_role_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.RoleId })
            .HasDatabaseName("ix_app_role_claim_tenant_role_id");
    }
}
