using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Roles;

internal sealed class UpdateRoleCommandHandler(IRoleAdminService roleAdmin, IRoleQueryService roleQuery)
    : IRequestHandler<UpdateRoleCommand, Result<RoleSummaryDto>>
{
    public async Task<Result<RoleSummaryDto>> Handle(UpdateRoleCommand command, CancellationToken ct)
    {
        var renamed = await roleAdmin.RenameAsync(command.Id, command.Name, command.Version, ct);
        if (renamed.IsFailure)
            return Result.Failure<RoleSummaryDto>(renamed.Error!);

        // Đọc lại trong cùng transaction của command: version mới nằm trong bản ghi trả về
        // (06-concurrency-control.md §6.3 luật 4) — FE không phải GET lại trước lần ghi kế tiếp.
        var role = await roleQuery.FindByIdAsync(command.Id, ct);
        return role is null
            ? Result.Failure<RoleSummaryDto>(RoleErrors.NotFound)
            : Result.Success(role);
    }
}
