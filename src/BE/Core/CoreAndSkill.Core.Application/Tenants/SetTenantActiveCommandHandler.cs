using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Tenants;

internal sealed class SetTenantActiveCommandHandler(ITenantProvisioningService provisioning)
    : IRequestHandler<SetTenantActiveCommand, Result>
{
    public Task<Result> Handle(SetTenantActiveCommand command, CancellationToken ct)
        => provisioning.SetTenantActiveAsync(command.TenantId, command.IsActive!.Value, ct); // ValidationBehavior đã chặn null
}
