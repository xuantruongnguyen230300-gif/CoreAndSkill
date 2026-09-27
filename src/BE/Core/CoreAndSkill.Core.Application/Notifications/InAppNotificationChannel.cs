using System.Text.Json;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Notifications;

namespace CoreAndSkill.Core.Application.Notifications;

// Kênh trong ứng dụng — rẻ nhất, không phụ thuộc hệ ngoài (docs/wiki-core/be/12-notifications.md §3.1,
// §5): hỏng ở đâu thấy ngay ở đó, nên dựng TRƯỚC kênh email.
//
// Lưu KHOÁ + THAM SỐ. Ngôn ngữ của người nhận không có vai trò gì ở đây: câu chữ ghép lúc hiển thị
// theo người XEM, không phải lúc lưu — đó chính là cách thông báo "hiện đúng ngôn ngữ của người nhận".
internal sealed class InAppNotificationChannel(INotificationRepository notifications) : INotificationChannel
{
    public NotificationChannels Channel => NotificationChannels.InApp;

    public async Task<Result> DeliverAsync(NotificationDelivery delivery, CancellationToken ct)
    {
        var draft = delivery.Draft;
        var paramsJson = draft.Params.Count == 0 ? null : JsonSerializer.Serialize(draft.Params);

        var notification = Notification.Create(draft.Code, paramsJson, draft.ModuleKey);
        if (notification.IsFailure)
            return Result.Failure(notification.Error!);

        var recipients = new List<NotificationRecipient>(delivery.RecipientUserIds.Count);
        foreach (var userId in delivery.RecipientUserIds)
        {
            var recipient = NotificationRecipient.Create(notification.Value.Id, userId);
            if (recipient.IsFailure)
                return Result.Failure(recipient.Error!);

            recipients.Add(recipient.Value);
        }

        await notifications.AddAsync(notification.Value, recipients, ct);
        return Result.Success();
    }
}
