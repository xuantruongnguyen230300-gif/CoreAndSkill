using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Permissions;

internal sealed class GetPermissionMatrixByResourceQueryHandler(IPermissionMatrixService matrixService)
    : IRequestHandler<GetPermissionMatrixByResourceQuery, Result<PermissionMatrixByResourceDto>>
{
    public async Task<Result<PermissionMatrixByResourceDto>> Handle(GetPermissionMatrixByResourceQuery query, CancellationToken ct)
        => Result.Success(await matrixService.GetMatrixByResourceAsync(ct));
}
