using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Notifications;

internal sealed class GetUnreadNotificationCountQueryHandler(INotificationRepository notifications, ICurrentUser currentUser)
    : IRequestHandler<GetUnreadNotificationCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(GetUnreadNotificationCountQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("GetUnreadNotificationCountQuery chạy khi chưa xác thực.");

        return Result.Success(await notifications.CountUnreadAsync(userId, ct));
    }
}
