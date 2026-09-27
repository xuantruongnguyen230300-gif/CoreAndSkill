using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §9.8. created_by_user_id là id trần — KHÔNG FK tới app_user (handler
// kiểm "việc của chính người gọi" bằng cột này).
internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("job", t =>
        {
            t.HasCheckConstraint("ck_job_status", "status IN ('queued','running','succeeded','failed','cancelled')");
            t.HasCheckConstraint("ck_job_progress", "progress BETWEEN 0 AND 100");
        });
        builder.HasKey(x => x.Id).HasName("pk_job");

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue(JobStatus.Queued);
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.Progress).IsRequired().HasDefaultValue((short)0);

        // Tên thuộc tính C# là ResultJson/ErrorJson để không đè tên kiểu Result/Error trong Domain;
        // cột vật lý vẫn là `result`/`error` như schema.
        builder.Property(x => x.ResultJson).HasColumnName("result").HasColumnType("jsonb");
        builder.Property(x => x.ErrorJson).HasColumnName("error").HasColumnType("jsonb");

        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_job_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(x => x.ResultFileId)
            .HasConstraintName("fk_job_result_file_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.CreatedAt })
            .HasDatabaseName("ix_job_tenant_created_at");
    }
}
