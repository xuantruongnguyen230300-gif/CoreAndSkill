using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Notifications;

// docs/contracts/notifications.md.
public static class NotificationErrors
{
    // Không có thông báo đó, HOẶC không phải của người gọi — gộp hai ca (luật M7).
    public static readonly Error NotFound = new(
        "CORE.NOTIFICATION.NOT_FOUND", "Không tìm thấy thông báo.", ErrorType.NotFound);

    // Ba mã dưới đi vào last_error của dòng outbox `dead` — không ra HTTP.
    public static readonly Error EmailNotConfigured = new(
        "CORE.NOTIFICATION.EMAIL_NOT_CONFIGURED",
        "Thông báo yêu cầu kênh email nhưng dự án chưa đăng ký IEmailSender.",
        ErrorType.BusinessRule);

    // Thiếu mẫu thì thất bại RÕ RÀNG — không bao giờ gửi đi một email có chỗ trống chưa thay (§4.3).
    public static readonly Error TemplateMissing = new(
        "CORE.NOTIFICATION.TEMPLATE_MISSING",
        "Chưa có mẫu email cho thông báo '{Code}' bằng ngôn ngữ '{Language}'.",
        ErrorType.BusinessRule);

    public static readonly Error ChannelUnavailable = new(
        "CORE.NOTIFICATION.CHANNEL_UNAVAILABLE",
        "Không có hiện thực nào cho kênh thông báo '{Channel}'.",
        ErrorType.BusinessRule);

}
