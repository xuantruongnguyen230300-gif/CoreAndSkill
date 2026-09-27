using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Tenants;

// POST /api/v1/core/system/tenants/{id}/recovery-reset-password — docs/contracts/tenants.md §4.
public sealed record RecoveryResetTenantAdminPasswordCommand(
    Guid TenantId, string UserName, string TempPassword) : ICommand;
