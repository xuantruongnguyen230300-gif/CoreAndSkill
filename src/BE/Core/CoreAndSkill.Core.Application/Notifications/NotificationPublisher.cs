using CoreAndSkill.Core.Domain.Common;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Application.Notifications;

// Chỗ DUY NHẤT tri thức về thông báo tập trung — bước (4) trong sơ đồ ở docs/wiki-core/be/12-notifications.md
// §1.2: quyết định ai nhận, qua kênh nào. Không có tri thức nào về thông báo nằm ở handler nghiệp vụ.
public interface INotificationPublisher
{
    Task<Result> PublishAsync(NotificationDraft draft, CancellationToken ct);
}

internal sealed class NotificationPublisher(
    IEnumerable<INotificationChannel> channels,
    INotificationPreferences preferences,
    ILogger<NotificationPublisher> logger)
    : INotificationPublisher
{
    private static readonly NotificationChannels[] KnownChannels = [NotificationChannels.InApp, NotificationChannels.Email];

    public async Task<Result> PublishAsync(NotificationDraft draft, CancellationToken ct)
    {
        var recipients = draft.RecipientUserIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (recipients.Count == 0)
        {
            logger.LogInformation("Thông báo {Code} không có người nhận — bỏ qua.", draft.Code);
            return Result.Success();
        }

        foreach (var channelFlag in KnownChannels)
        {
            if (!draft.Channels.HasFlag(channelFlag))
                continue;

            // MỘT lần hỏi cho cả danh sách, mỗi kênh một lần — xem chú thích của INotificationPreferences.
            var enabled = await preferences.FilterEnabledAsync(recipients, draft.Code, channelFlag, ct);

            if (enabled.Count == 0)
                continue;

            var channel = channels.FirstOrDefault(c => c.Channel == channelFlag);
            if (channel is null)
                return Result.Failure(NotificationErrors.ChannelUnavailable.WithParams(("Channel", channelFlag.ToString())));

            var delivered = await channel.DeliverAsync(new NotificationDelivery(draft, enabled), ct);
            if (delivered.IsFailure)
                return delivered;
        }

        return Result.Success();
    }
}
