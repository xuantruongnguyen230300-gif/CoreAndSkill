namespace CoreAndSkill.Core.Domain.Common;

// Điểm sinh Id DUY NHẤT của toàn hệ — docs/quy-uoc/be-entity-domain.md §1.1.
// UUID v7 (sắp theo thời gian), không v4 (Guid.NewGuid()). Dùng cho MỌI kiểu cần Id, kể cả kiểu
// không kế thừa BaseEntity (Tenant, AppUser…) — §1.4.
public static class EntityId
{
    public static Guid New() => Guid.CreateVersion7();
}
