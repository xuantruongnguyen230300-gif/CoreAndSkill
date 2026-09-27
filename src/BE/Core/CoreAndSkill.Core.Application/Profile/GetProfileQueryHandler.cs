using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Profile;

internal sealed class GetProfileQueryHandler(ICurrentUser currentUser, IUserProfileService profileService)
    : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetProfileQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("GetProfileQuery chạy khi chưa xác thực.");

        var profile = await profileService.GetAsync(userId, ct)
            ?? throw new InvalidOperationException("Phiên hợp lệ nhưng không tìm thấy tài khoản.");

        return Result.Success(profile);
    }
}
