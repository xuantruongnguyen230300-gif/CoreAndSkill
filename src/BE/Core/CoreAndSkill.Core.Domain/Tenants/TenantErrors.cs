using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Tenants;

// Catalog lỗi của invariant Domain — khai TRƯỚC, cạnh code trả ra nó (docs/quy-uoc/be-entity-domain.md §3.2, §3.3).
// Ở Core.Domain vì Tenant gọi nó trong chính invariant của mình; Core.Domain không được tham chiếu
// Core.Application (luật A1). Mã và ErrorType khớp docs/contracts/tenants.md §3.
public static class TenantErrors
{
    public static readonly Error SystemImmutable = new(
        "CORE.TENANT.SYSTEM_IMMUTABLE", "Không được ngưng hoạt động đơn vị hệ thống.", ErrorType.BusinessRule);
}
