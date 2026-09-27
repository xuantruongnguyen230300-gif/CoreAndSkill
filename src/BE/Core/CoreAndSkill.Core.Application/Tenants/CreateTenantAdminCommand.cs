using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Tenants;

// POST /api/v1/core/system/tenants/{id}/admins — docs/contracts/tenants.md §6.
public sealed record CreateTenantAdminCommand(
    Guid TenantId, string UserName, string Email, string FullName, string TempPassword) : ICommand;
