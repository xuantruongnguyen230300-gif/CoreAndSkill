using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Notifications;

internal sealed class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notifications, ICurrentUser currentUser, TimeProvider timeProvider)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("MarkAllNotificationsReadCommand chạy khi chưa xác thực.");

        await notifications.MarkAllReadAsync(userId, timeProvider.GetUtcNow(), ct);
        return Result.Success();
    }
}
