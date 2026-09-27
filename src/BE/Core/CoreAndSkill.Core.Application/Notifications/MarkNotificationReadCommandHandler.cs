using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Notifications;

internal sealed class MarkNotificationReadCommandHandler(
    INotificationRepository notifications, ICurrentUser currentUser, TimeProvider timeProvider)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("MarkNotificationReadCommand chạy khi chưa xác thực.");

        // Thông báo của người khác cho cùng kết quả với "không có" — 404, không phải 403 (luật M7).
        var recipient = await notifications.FindRecipientAsync(command.Id, userId, ct);
        if (recipient is null)
            return Result.Failure(NotificationErrors.NotFound);

        recipient.MarkRead(timeProvider.GetUtcNow());
        return Result.Success();
    }
}
