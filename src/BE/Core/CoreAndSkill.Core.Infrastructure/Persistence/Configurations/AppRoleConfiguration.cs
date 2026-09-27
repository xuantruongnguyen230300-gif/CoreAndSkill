using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §4.0, §4.2.
internal sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("app_role");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsSystem).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_app_role_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        var normalizedName = builder.Metadata.FindProperty(nameof(AppRole.NormalizedName));
        if (normalizedName is not null)
        {
            var index = builder.Metadata.FindIndex(normalizedName);
            if (index is not null)
                builder.Metadata.RemoveIndex(index);
        }

        builder.HasIndex(x => new { x.TenantId, x.NormalizedName })
            .IsUnique()
            .HasDatabaseName("RoleNameIndex");
    }
}
