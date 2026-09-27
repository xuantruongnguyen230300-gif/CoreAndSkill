namespace CoreAndSkill.Core.Domain.Common;

// Lớp cơ sở của mọi entity nghiệp vụ — docs/quy-uoc/be-entity-domain.md §1.
// Id là init-only, sinh bằng EntityId.New() (UUID v7) — không ai gán lại từ ngoài (§1.1).
// Năm field dưới mang public setter CÓ CHỦ ĐÍCH, để AuditInterceptor ghi được từ ngoài entity —
// ĐÚNG năm field này, không hơn (§1.2). Field nghiệp vụ của lớp con luôn `private set` (§2).
public abstract class BaseEntity : IAuditableEntity
{
    public Guid Id { get; init; } = EntityId.New();

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
