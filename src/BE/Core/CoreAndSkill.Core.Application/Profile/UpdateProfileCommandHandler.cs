using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Profile;

internal sealed class UpdateProfileCommandHandler(ICurrentUser currentUser, IUserProfileService profileService)
    : IRequestHandler<UpdateProfileCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(UpdateProfileCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("UpdateProfileCommand chạy khi chưa xác thực.");

        var input = new UpdateProfileInput(command.FullName, command.PhoneNumber, command.PreferredLanguage, command.Version);

        return await profileService.UpdateAsync(userId, input, ct);
    }
}
