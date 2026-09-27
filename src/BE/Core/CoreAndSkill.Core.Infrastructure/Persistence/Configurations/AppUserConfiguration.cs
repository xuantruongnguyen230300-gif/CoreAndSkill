using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// docs/database/schema-core.md §4.0, §4.1. Đổi tên bảng AspNetUsers → app_user; hai index tự đặt
// của Identity giữ TÊN nhưng khai lại ĐỊNH NGHĨA để gồm tenant_id (§4.0 cảnh báo "giữ tên không có
// nghĩa là giữ định nghĩa").
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_user");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PreferredLanguage).HasMaxLength(10);
        // Nới kiểu so với cột gốc của Identity — trần 20 ký tự là luật nghiệp vụ (contracts/profile.md §2).
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.MustChangePassword).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.IsSystemOperator).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasPermissionBypass).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.LockedByAdmin).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("fk_app_user_tenant_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Gỡ hai index đơn cột mà IdentityUserContext<>.OnModelCreating đã tạo, thay bằng bản gồm
        // tenant_id — GIỮ TÊN, ĐỔI ĐỊNH NGHĨA (§4.0). Không gỡ thì tên trùng với index mới → lỗi
        // lúc build model.
        RemoveSingleColumnIndex(builder, nameof(AppUser.NormalizedUserName));
        RemoveSingleColumnIndex(builder, nameof(AppUser.NormalizedEmail));

        // Tên hai index là một phần của hợp đồng lỗi: AppUserStore dịch vi phạm 23505 theo đúng tên này (ADR-0089) —
        // khai ở AppUserUniqueIndexes, không gõ lại.
        builder.HasIndex(x => new { x.TenantId, x.NormalizedUserName })
            .IsUnique()
            .HasDatabaseName(AppUserUniqueIndexes.UserName);

        // Unique chỉ đúng khi tuỳ chọn "email phải duy nhất" của Identity đang bật
        // (RequireUniqueEmail = true, đặt ở AddCoreIdentity) — docs/database/schema-core.md §4.1.
        builder.HasIndex(x => new { x.TenantId, x.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName(AppUserUniqueIndexes.Email);

        // Luật M11 — hai cờ đặc quyền loại trừ nhau, ép bằng ràng buộc kiểm tra ở database.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_app_user_privilege_flags_exclusive",
            "NOT (has_permission_bypass AND is_system_operator)"));
    }

    private static void RemoveSingleColumnIndex(EntityTypeBuilder<AppUser> builder, string propertyName)
    {
        var property = builder.Metadata.FindProperty(propertyName);
        if (property is null)
            return;

        var index = builder.Metadata.FindIndex(property);
        if (index is not null)
            builder.Metadata.RemoveIndex(index);
    }
}
