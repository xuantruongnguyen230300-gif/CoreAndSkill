using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Tenants;

// docs/quy-uoc/be-entity-domain.md §7.1. core.tenant KHÔNG mang tenant_id nên không có bộ lọc nào
// cần bỏ ở đây — tra được TRƯỚC khi mở phạm vi đơn vị.
internal sealed class TenantLookupService(CoreDbContext db) : ITenantLookup
{
    public Task<TenantSummary?> FindByCodeAsync(string code, CancellationToken ct)
    {
        var normalized = code.Trim().ToUpperInvariant();

        return db.Tenants
            .Where(t => t.Code == normalized)
            .Select(t => new TenantSummary(t.Id, t.Code, t.Name, t.IsActive))
            .SingleOrDefaultAsync(ct);
    }

    public Task<TenantSummary?> FindByIdAsync(Guid id, CancellationToken ct)
        => db.Tenants
            .Where(t => t.Id == id)
            .Select(t => new TenantSummary(t.Id, t.Code, t.Name, t.IsActive))
            .SingleOrDefaultAsync(ct);
}
