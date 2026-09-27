using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Domain.Notifications;

namespace CoreAndSkill.Core.Application.Notifications;

// Đúng năm field của docs/contracts/notifications.md §1 — không severity, không linkRoute (chưa dùng ở v1).
// `Id` là id của THÔNG BÁO (nội dung dùng chung); người nhận được xác định bởi (thông báo, người gọi).
public sealed record NotificationDto(
    Guid Id,
    string Code,
    IReadOnlyDictionary<string, string> Params,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? ReadAt);

// AddAsync / FindRecipientAsync KHÔNG SaveChanges. Hai thao tác đếm/đánh dấu hàng loạt chạy NGAY bằng
// một câu lệnh — endpoint đếm bị gọi lặp theo chu kỳ nên phải rẻ: đếm trên chỉ mục, không nạp danh sách.
public interface INotificationRepository
{
    Task AddAsync(Notification notification, IReadOnlyCollection<NotificationRecipient> recipients, CancellationToken ct);

    Task<PagedList<NotificationDto>> ListForUserAsync(Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct);

    // Có theo dõi. null khi không có, HOẶC người nhận đó không phải userId — chỗ gọi không phân biệt được.
    Task<NotificationRecipient?> FindRecipientAsync(Guid notificationId, Guid userId, CancellationToken ct);

    Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset now, CancellationToken ct);
}
