using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Roles;

internal sealed class GetRoleByIdQueryHandler(IRoleQueryService roleQuery)
    : IRequestHandler<GetRoleByIdQuery, Result<RoleSummaryDto>>
{
    public async Task<Result<RoleSummaryDto>> Handle(GetRoleByIdQuery query, CancellationToken ct)
    {
        var role = await roleQuery.FindByIdAsync(query.Id, ct);
        return role is null
            ? Result.Failure<RoleSummaryDto>(RoleErrors.NotFound)
            : Result.Success(role);
    }
}
