using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Vai trò — DỮ LIỆU, không phải hằng số trong code (luật S1). docs/database/schema-core.md §4.2.
//
// Description và IsSystem mang `private set` (docs/quy-uoc/be-entity-domain.md §1.4): EF vật liệu hoá qua setter riêng;
// RoleStore của Identity không ghi hai field này. IsSystem chỉ đặt lúc tạo (seed đơn vị mới — ITenantSeedSource). Chưa
// đường nào ghi Description, nên không có method đổi nó.
public class AppRole : IdentityRole<Guid>, ITenantScoped, IAuditableEntity
{
    public Guid TenantId { get; init; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    public AppRole()
    {
        Id = EntityId.New();
    }

    public static AppRole NewRole(string name, bool isSystem = false) => new() { Name = name, IsSystem = isSystem };
}
