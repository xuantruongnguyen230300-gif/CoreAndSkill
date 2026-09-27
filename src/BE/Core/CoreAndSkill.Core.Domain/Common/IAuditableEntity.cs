namespace CoreAndSkill.Core.Domain.Common;

// Bốn field vết mà AuditInterceptor điền — docs/quy-uoc/be-entity-domain.md §1, §1.3.
// Interceptor chọn entity theo INTERFACE này (ChangeTracker.Entries<IAuditableEntity>()), không
// theo lớp cơ sở — nên phủ cả hậu duệ BaseEntity lẫn các kiểu không kế thừa nó (Tenant, AppUser…),
// §1.4.
public interface IAuditableEntity
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
    DateTimeOffset? CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}
