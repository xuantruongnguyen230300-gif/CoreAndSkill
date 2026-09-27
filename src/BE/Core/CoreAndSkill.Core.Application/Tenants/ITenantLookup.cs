namespace CoreAndSkill.Core.Application.Tenants;

// Tra đơn vị theo mã, TRƯỚC khi mở phạm vi đơn vị — docs/quy-uoc/be-entity-domain.md §7.1.
public interface ITenantLookup
{
    Task<TenantSummary?> FindByCodeAsync(string code, CancellationToken ct);

    // Dùng khi đã có TenantId từ ITenantContext (vd. dựng lại tenantCode/tenantName cho `me`) —
    // KHÔNG dùng để suy TenantId từ input client (luật M2 vẫn áp: TenantId luôn đến từ claim).
    Task<TenantSummary?> FindByIdAsync(Guid id, CancellationToken ct);
}

public sealed record TenantSummary(Guid Id, string Code, string Name, bool IsActive);
