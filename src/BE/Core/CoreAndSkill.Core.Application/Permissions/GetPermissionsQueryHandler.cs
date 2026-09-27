using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Permissions;

internal sealed class GetPermissionsQueryHandler(IPermissionMatrixService matrixService)
    : IRequestHandler<GetPermissionsQuery, Result<IReadOnlyList<PermissionListItemDto>>>
{
    public async Task<Result<IReadOnlyList<PermissionListItemDto>>> Handle(GetPermissionsQuery query, CancellationToken ct)
        => Result.Success(await matrixService.GetCatalogAsync(ct));
}
