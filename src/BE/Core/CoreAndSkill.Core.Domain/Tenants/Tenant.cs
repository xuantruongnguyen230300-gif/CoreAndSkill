using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Tenants;

// Một dòng là một đơn vị — docs/database/schema-core.md §1.3.
// KHÔNG kế thừa BaseEntity, có chủ đích: tenant không bao giờ bị xoá nên không có IsDeleted; nó
// implement trực tiếp IAuditableEntity để AuditInterceptor vẫn điền bốn cột audit
// (docs/quy-uoc/be-entity-domain.md §1.4). KHÔNG khai ITenantScoped — nó là GỐC, không tự trỏ vào
// mình (docs/wiki-core/be/17-multi-tenant.md §2).
public sealed class Tenant : IAuditableEntity
{
    public Guid Id { get; init; } = EntityId.New();

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }

    // Bốn field của IAuditableEntity — public setter có chủ đích, §1.4.
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Tenant()
    {
        // EF Core cần ctor không tham số.
    }

    // Mã chuẩn hoá về chữ HOA trước khi lưu — docs/database/schema-core.md §1.3.
    public static Result<Tenant> Create(string code, string name, bool isSystem = false)
        => Result.Success(new Tenant
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsActive = true,
            IsSystem = isSystem,
        });

    public Result Deactivate()
    {
        if (IsSystem)
            return Result.Failure(TenantErrors.SystemImmutable);

        IsActive = false;
        return Result.Success();
    }

    public Result Activate()
    {
        IsActive = true;
        return Result.Success();
    }
}
