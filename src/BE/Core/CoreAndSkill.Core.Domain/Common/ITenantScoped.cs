namespace CoreAndSkill.Core.Domain.Common;

// Đánh dấu "bản ghi này thuộc về một đơn vị" — TRỤC ĐỘC LẬP với BaseEntity (soft-delete/audit).
// docs/wiki-core/be/17-multi-tenant.md §2. get-only: TenantId do interceptor gán lúc Added, không
// phải dữ liệu người dùng nhập (luật M2) — docs/RULES.md §9.
public interface ITenantScoped
{
    Guid TenantId { get; }
}
