using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §7.1. code + params, KHÔNG có cột nào chứa câu đã ghép.
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notification", t => t.HasCheckConstraint(
            "ck_notification_severity", "severity IN ('info','success','warning','error')"));
        builder.HasKey(x => x.Id).HasName("pk_notification");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Params).HasColumnType("jsonb");
        builder.Property(x => x.Severity).HasMaxLength(20).IsRequired().HasDefaultValue(Notification.DefaultSeverity);
        builder.Property(x => x.LinkRoute).HasMaxLength(200);
        builder.Property(x => x.ModuleKey).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_notification_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_notification_tenant_created_at");
    }
}
