using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Tenants;

// POST /api/v1/core/system/tenants — docs/contracts/tenants.md §2.
public sealed record CreateTenantCommand(
    string Code,
    string Name,
    string AdminUserName,
    string AdminEmail,
    string AdminFullName,
    string AdminTempPassword) : ICommand<TenantListItemDto>;
