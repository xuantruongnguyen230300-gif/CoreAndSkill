using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Permissions;

// Danh mục quyền — một dòng là một hành động kiểm được. docs/database/schema-core.md §5.2.
// KHÔNG mang tenant_id — miễn trừ đã khai ở schema-core.md §1.3. Nội dung được nạp bằng migration
// idempotent ở B2; B1 chỉ dựng bảng (docs/wiki-core/be/trien-khai/03-b2-phan-quyen-va-bien.md §2 bước 1).
public sealed class Permission : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string ResourceKey { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string NameKey { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }
    public int DisplayOrder { get; private set; }

    private Permission()
    {
        // EF Core cần ctor không tham số.
    }
}
