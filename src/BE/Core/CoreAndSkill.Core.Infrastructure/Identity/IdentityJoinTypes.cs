using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Năm bảng join của Identity mang tenant_id (docs/database/schema-core.md §4.3) nên PHẢI là lớp
// con cài ITenantScoped — kiểu mặc định của Identity không có TenantId và không qua vòng lọc
// tenant. docs/quy-uoc/be-entity-domain.md §5.1.
public class AppUserRole : IdentityUserRole<Guid>, ITenantScoped
{
    public Guid TenantId { get; init; }
}

public class AppUserClaim : IdentityUserClaim<Guid>, ITenantScoped
{
    public Guid TenantId { get; init; }
}

public class AppUserLogin : IdentityUserLogin<Guid>, ITenantScoped
{
    public Guid TenantId { get; init; }
}

public class AppRoleClaim : IdentityRoleClaim<Guid>, ITenantScoped
{
    public Guid TenantId { get; init; }
}

public class AppUserToken : IdentityUserToken<Guid>, ITenantScoped
{
    public Guid TenantId { get; init; }
}
