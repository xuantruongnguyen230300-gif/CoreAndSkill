using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Permissions;

// Trục HÀNG của ma trận phân quyền — docs/database/schema-core.md §5.1.
// KHÔNG mang tenant_id — miễn trừ đã khai ở schema-core.md §1.3 (danh mục dùng chung toàn hệ).
// Nội dung được nạp bằng migration idempotent ở B2 (docs/wiki-core/be/trien-khai/03-b2-phan-quyen-va-bien.md
// §2 bước 1); B1 chỉ dựng bảng.
public sealed class PermissionResource : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public string NameKey { get; private set; } = string.Empty;
    public string? ModuleKey { get; private set; }
    public int DisplayOrder { get; private set; }

    private PermissionResource()
    {
        // EF Core cần ctor không tham số.
    }
}
