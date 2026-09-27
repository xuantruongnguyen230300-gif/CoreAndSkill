using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Tenants;

internal sealed class CreateTenantAdminCommandHandler(ITenantProvisioningService provisioning)
    : IRequestHandler<CreateTenantAdminCommand, Result>
{
    public async Task<Result> Handle(CreateTenantAdminCommand command, CancellationToken ct)
    {
        var result = await provisioning.CreateAdditionalAdminAsync(
            command.TenantId, command.UserName, command.Email, command.FullName, command.TempPassword, ct);

        // docs/contracts/tenants.md §6 — response không trả id tài khoản vừa tạo.
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error!);
    }
}
