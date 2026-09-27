using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Configurations;

// Kho khoá bảo vệ dữ liệu — docs/database/schema-core.md §9.3, docs/adr/0014-mot-instance-key-ring-postgres.md §2.
// DataProtectionKey là kiểu của Microsoft.AspNetCore.DataProtection.EntityFrameworkCore — chỉ đổi
// tên bảng sang số ít, mọi thứ khác giữ mặc định của package.
internal sealed class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        builder.ToTable("data_protection_key");
    }
}
