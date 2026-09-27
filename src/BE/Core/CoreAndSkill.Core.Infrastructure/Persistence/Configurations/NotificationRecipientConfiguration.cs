using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §7.2.
internal sealed class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("notification_recipient");
        builder.HasKey(x => x.Id).HasName("pk_notification_recipient");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_notification_recipient_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Notification>()
            .WithMany()
            .HasForeignKey(x => x.NotificationId)
            .HasConstraintName("fk_notification_recipient_notification_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_notification_recipient_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        // Tên rút gọn `notif_user` có chủ đích (trần 63 byte, schema-core.md §2.2): phần bị rút gọn
        // là tên cột, không bao giờ là phần `tenant`.
        builder.HasIndex(x => new { x.TenantId, x.NotificationId, x.UserId })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_notification_recipient_tenant_notif_user_active");

        // Truy vấn nóng: chỉ báo "chưa đọc" trên thanh trên cùng, gọi lặp theo chu kỳ.
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.CreatedAt })
            .IsDescending(false, false, true)
            .HasFilter("read_at IS NULL AND is_deleted = false")
            .HasDatabaseName("ix_notification_recipient_unread");
    }
}
