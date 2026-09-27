using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Notifications;

internal sealed class GetNotificationsQueryHandler(INotificationRepository notifications, ICurrentUser currentUser)
    : IRequestHandler<GetNotificationsQuery, Result<PagedList<NotificationDto>>>
{
    public async Task<Result<PagedList<NotificationDto>>> Handle(GetNotificationsQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("GetNotificationsQuery chạy khi chưa xác thực.");

        return Result.Success(
            await notifications.ListForUserAsync(userId, query.UnreadOnly, query.Page, query.PageSize, ct));
    }
}
