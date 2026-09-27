using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Profile;

internal sealed class RenouncePermissionBypassCommandHandler(ICurrentUser currentUser, IUserProfileService profileService)
    : IRequestHandler<RenouncePermissionBypassCommand, Result>
{
    public async Task<Result> Handle(RenouncePermissionBypassCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("RenouncePermissionBypassCommand chạy khi chưa xác thực.");

        return await profileService.RenouncePermissionBypassAsync(userId, ct);
    }
}
