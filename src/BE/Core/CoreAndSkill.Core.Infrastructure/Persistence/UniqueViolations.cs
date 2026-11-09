using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAndSkill.Core.Infrastructure.Persistence;

// Nhận ra vi phạm khoá duy nhất của PostgreSQL (23505) trên MỘT ràng buộc đã biết, bằng TÊN RÀNG BUỘC — không bằng câu
// thông báo, không gộp mọi DbUpdateException. Ràng buộc khác, mã lỗi khác, hay lỗi không phải của PostgreSQL thì trả false
// để người gọi để ngoại lệ đi tiếp (500). Cùng cách đọc với AppUserUniqueIndexes (ADR-0089). Tên so Ordinal: PostgreSQL
// giữ nguyên hoa thường của tên có nháy kép.
internal static class UniqueViolations
{
    // Tên khai ở TenantConfiguration / AppRoleConfiguration; test đối chiếu từng tên với mô hình EF.
    public const string TenantCode = "uq_tenant_code";
    public const string NormalizedNameIndex = "RoleNameIndex";

    public static bool IsOn(DbUpdateException exception, string constraintName)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: { } name }
           && string.Equals(name, constraintName, StringComparison.Ordinal);
}
