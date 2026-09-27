using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Tenants;

internal sealed class RecoveryResetTenantAdminPasswordCommandHandler(ITenantProvisioningService provisioning)
    : IRequestHandler<RecoveryResetTenantAdminPasswordCommand, Result>
{
    public Task<Result> Handle(RecoveryResetTenantAdminPasswordCommand command, CancellationToken ct)
        => provisioning.RecoveryResetAdminPasswordAsync(command.TenantId, command.UserName, command.TempPassword, ct);
}
