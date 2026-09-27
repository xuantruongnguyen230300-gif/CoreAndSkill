using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Permissions;

internal sealed class GetPermissionMatrixQueryHandler(IPermissionMatrixService matrixService)
    : IRequestHandler<GetPermissionMatrixQuery, Result<PermissionMatrixDto>>
{
    public async Task<Result<PermissionMatrixDto>> Handle(GetPermissionMatrixQuery query, CancellationToken ct)
        => Result.Success(await matrixService.GetMatrixAsync(ct));
}
