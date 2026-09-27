namespace CoreAndSkill.Core.Application.Common.Interfaces;

// Đơn vị hiện hành của request — định nghĩa gốc, docs/quy-uoc/be-architecture.md §1.1.
// PHẢI có giá trị TRƯỚC bước xác thực (bước đăng nhập nạp tenant trước khi gọi UserManager —
// docs/wiki-core/be/17-multi-tenant.md §11.1). Mọi DbContext nhận nó qua constructor để dựng bộ
// lọc tenant — docs/quy-uoc/be-entity-domain.md §5.1. Đăng ký Scoped — KHÔNG BAO GIỜ Singleton.
public interface ITenantContext
{
    Guid? TenantId { get; }
}
