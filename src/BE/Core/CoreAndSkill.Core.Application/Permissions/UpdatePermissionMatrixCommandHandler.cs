using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Permissions;

internal sealed class UpdatePermissionMatrixCommandHandler(IPermissionMatrixService matrixService)
    : IRequestHandler<UpdatePermissionMatrixCommand, Result<UpdatePermissionMatrixResultDto>>
{
    public async Task<Result<UpdatePermissionMatrixResultDto>> Handle(UpdatePermissionMatrixCommand command, CancellationToken ct)
    {
        // ValidationBehavior đã chặn Entries null, phần tử null và RoleIds null trước khi tới đây.
        var entries = command.Entries!;

        // Trùng lặp permissionId — chặn TRƯỚC khi ghi, tính được từ payload (docs/contracts/permissions.md §6).
        // messageParams nêu id lặp ĐẦU TIÊN theo thứ tự payload.
        var seen = new HashSet<Guid>();
        foreach (var entry in entries)
        {
            if (!seen.Add(entry.PermissionId))
                return Result.Failure<UpdatePermissionMatrixResultDto>(PermissionErrors.DuplicateEntryOf(entry.PermissionId));
        }

        var byPermission = entries.ToDictionary(
            e => e.PermissionId, IReadOnlyList<Guid> (e) => e.RoleIds);

        var result = await matrixService.ReplaceMatrixAsync(command.Version, byPermission, ct);

        return result.IsSuccess
            ? Result.Success(new UpdatePermissionMatrixResultDto(result.Value))
            : Result.Failure<UpdatePermissionMatrixResultDto>(result.Error!);
    }
}
