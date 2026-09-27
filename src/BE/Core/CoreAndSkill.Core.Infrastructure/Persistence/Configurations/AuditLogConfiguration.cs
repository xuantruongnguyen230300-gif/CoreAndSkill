using CoreAndSkill.Core.Domain.Audit;
using CoreAndSkill.Core.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §9.4. Bảng CHỈ ghi thêm — không FK tới app_user (actor có thể bị
// đổi tên/xoá mềm, dòng vẫn phải còn nghĩa nhờ actor_display), không cột audit/xoá mềm (khác mọi
// entity kế thừa BaseEntity). Quyền DB (INSERT, SELECT — không UPDATE/DELETE) ép ở
// docs/database/script-runbook.md §3.6, KHÔNG ở tầng này.
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log");
        builder.HasKey(x => x.Id).HasName("pk_audit_log");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.ActorDisplay).IsRequired();
        builder.Property(x => x.ActionCode).IsRequired();
        builder.Property(x => x.TargetType).IsRequired();
        builder.Property(x => x.TargetId).IsRequired();

        builder.Property(x => x.BeforeValue).HasColumnType("jsonb");
        builder.Property(x => x.AfterValue).HasColumnType("jsonb");
        builder.Property(x => x.IpAddress).HasColumnType("inet");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_audit_log_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.ActorTenantId)
            .HasConstraintName("fk_audit_log_actor_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        // docs/database/schema-core.md §9.4: (tenant_id, occurred_at DESC) — cột đầu ASC (mặc
        // định), cột thứ hai DESC để khớp chiều đọc "mới nhất trước" của trang audit log.
        builder.HasIndex(x => new { x.TenantId, x.OccurredAt })
            .HasDatabaseName("ix_audit_log_tenant_occurred")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.TenantId, x.TargetType, x.TargetId })
            .HasDatabaseName("ix_audit_log_tenant_target");
    }
}
