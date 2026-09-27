using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Thông báo — docs/wiki-core/be/12-notifications.md §4.2. Khoá `Core:Notification:*`.
public sealed class CoreNotificationOptions
{
    public const string SectionName = "Core:Notification";

    // Ngôn ngữ dùng khi HỒ SƠ NGƯỜI NHẬN chưa đặt ngôn ngữ ưa dùng (app_user.preferred_language rỗng).
    // Ngôn ngữ luôn lấy từ người NHẬN, không từ người gây ra sự kiện hay từ request.
    [Required, RegularExpression("^[a-z]{2}(-[A-Z]{2})?$")]
    public string DefaultLanguage { get; init; } = "vi";
}
