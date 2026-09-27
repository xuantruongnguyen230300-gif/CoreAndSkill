using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Application.Notifications;

// Kênh email — docs/wiki-core/be/12-notifications.md §3, §4. Core giữ CƠ CHẾ: tra người nhận, chọn
// ngôn ngữ, gọi bộ kết xuất, gọi bộ gửi. Nội dung mẫu và dịch vụ gửi thuộc dự án.
//
// NGÔN NGỮ LẤY TỪ HỒ SƠ NGƯỜI NHẬN (app_user.preferred_language), có giá trị mặc định của hệ thống khi
// thiếu — KHÔNG lấy từ request và KHÔNG từ người gây ra sự kiện (§4.2): thông báo gửi cho người khác,
// và thông báo phát sinh từ job nền thì không có request nào để lấy.
internal sealed class EmailNotificationChannel(
    IUserLookupService users,
    INotificationTemplateRenderer renderer,
    IEnumerable<IEmailSender> senders,
    IOptions<CoreNotificationOptions> options,
    ILogger<EmailNotificationChannel> logger)
    : INotificationChannel
{
    public NotificationChannels Channel => NotificationChannels.Email;

    public async Task<Result> DeliverAsync(NotificationDelivery delivery, CancellationToken ct)
    {
        var sender = senders.FirstOrDefault();
        if (sender is null)
            return Result.Failure(NotificationErrors.EmailNotConfigured);

        var draft = delivery.Draft;
        var recipients = await users.FindByIdsAsync(delivery.RecipientUserIds.ToList(), ct);

        foreach (var recipient in recipients)
        {
            // Người nhận không có địa chỉ: không thử lại được và không phải lỗi hệ thống — ghi nhận rồi
            // đi tiếp. Chỉ định danh, không kèm địa chỉ (07-observability.md §5).
            if (string.IsNullOrWhiteSpace(recipient.Email))
            {
                logger.LogInformation(
                    "Người nhận {UserId} không có địa chỉ email — bỏ qua kênh email cho thông báo {Code}.",
                    recipient.Id, draft.Code);
                continue;
            }

            var language = string.IsNullOrWhiteSpace(recipient.PreferredLanguage)
                ? options.Value.DefaultLanguage
                : recipient.PreferredLanguage;

            var rendered = await renderer.RenderAsync(draft.Code, language, draft.Params, ct);
            if (rendered.IsFailure)
                return Result.Failure(rendered.Error!);

            var sent = await sender.SendAsync(
                new EmailMessage(recipient.Email, recipient.FullName, rendered.Value), ct);
            if (sent.IsFailure)
                return sent;
        }

        return Result.Success();
    }
}
