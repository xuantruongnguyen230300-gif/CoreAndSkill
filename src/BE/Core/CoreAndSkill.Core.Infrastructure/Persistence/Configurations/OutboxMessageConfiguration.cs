using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §8. KHÔNG mang khối audit, KHÔNG kế thừa BaseEntity (nên nằm ngoài
// bộ lọc xoá mềm E3) — dòng đã phát được dọn theo chính sách lưu giữ, không xoá mềm.
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_message", t => t.HasCheckConstraint(
            "ck_outbox_message_status", "status IN ('pending','dead','done')"));
        builder.HasKey(x => x.Id).HasName("pk_outbox_message");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TraceId).HasMaxLength(64);
        builder.Property(x => x.TriggeredByUserName).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue(OutboxStatus.Pending);
        builder.Property(x => x.AttemptCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LastError).HasColumnType("text");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_outbox_message_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Ngoại lệ có chủ đích của quy tắc "tenant_id đứng đầu index" (schema-core.md §3.7, §8): bộ
        // phát quét TOÀN HỆ, không theo đơn vị nào.
        builder.HasIndex(x => x.NextAttemptAt)
            .HasFilter("status = 'pending'")
            .HasDatabaseName("ix_outbox_message_pending");

        // Readiness hỏi "có dòng dead nào không" ở MỖI lần gọi — chỉ mục một phần chỉ chứa dòng dead
        // (hiếm), nên câu hỏi đó không quét cả bảng.
        builder.HasIndex(x => x.OccurredAt)
            .HasFilter("status = 'dead'")
            .HasDatabaseName("ix_outbox_message_dead");
    }
}
