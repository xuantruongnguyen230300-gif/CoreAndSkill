using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Tenants;

// docs/contracts/tenants.md §2. Gọi service tạo đơn vị DÙNG CHUNG (ADR-0023) — CÙNG hiện thực với
// core bootstrap, khác ở FailIfExists: true (endpoint từ chối trùng mã, bootstrap thì bỏ qua).
internal sealed class CreateTenantCommandHandler(ITenantProvisioningService provisioning, ITenantAdminQueryService tenantQuery)
    : IRequestHandler<CreateTenantCommand, Result<TenantListItemDto>>
{
    public async Task<Result<TenantListItemDto>> Handle(CreateTenantCommand command, CancellationToken ct)
    {
        var result = await provisioning.CreateTenantAsync(
            new CreateTenantInput(
                command.Code,
                command.Name,
                IsSystem: false,
                command.AdminUserName,
                command.AdminTempPassword,
                AdminHasPermissionBypass: true,
                AdminIsSystemOperator: false,
                FailIfExists: true,
                AdminEmail: command.AdminEmail,
                AdminFullName: command.AdminFullName),
            ct);

        if (result.IsFailure)
            return Result.Failure<TenantListItemDto>(result.Error!);

        var dto = await tenantQuery.FindByIdAsync(result.Value.TenantId, ct);
        return Result.Success(dto!); // vừa tạo/tìm thấy ở CreateTenantAsync — chắc chắn tồn tại
    }
}
