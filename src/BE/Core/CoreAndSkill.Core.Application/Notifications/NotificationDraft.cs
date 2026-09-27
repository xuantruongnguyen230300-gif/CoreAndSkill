namespace CoreAndSkill.Core.Application.Notifications;

// Hai kênh ở v1 — docs/wiki-core/be/12-notifications.md §3.1. Kênh thứ ba mới đáng dựng cơ chế định
// tuyến (§3.2): hai kênh thì một interface + một bộ chọn đơn giản là đủ.
[Flags]
public enum NotificationChannels
{
    None = 0,
    InApp = 1,
    Email = 2,
}

// Một thông báo cần gửi — do BÊN NGHE integration event dựng (JobFinishedOutboxHandler, hoặc handler
// của module), KHÔNG do handler nghiệp vụ: nghiệp vụ chỉ phát sự kiện, không biết ai nhận hay qua kênh
// nào (12-notifications.md §1.1).
//
// Code + Params, KHÔNG có câu đã ghép: khoá dịch và tham số theo TÊN (luật R7), ví dụ
// ("core.user.locked", {"UserName": "an.nv"}). Câu chữ ghép lúc hiển thị, theo ngôn ngữ của người XEM.
public sealed record NotificationDraft(
    string Code,
    IReadOnlyDictionary<string, string> Params,
    IReadOnlyCollection<Guid> RecipientUserIds,
    string? ModuleKey = null,
    NotificationChannels Channels = NotificationChannels.InApp);
