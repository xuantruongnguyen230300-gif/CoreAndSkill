using CoreAndSkill.Core.Application.Common.Cqrs;
using CoreAndSkill.Core.Application.Common.Paging;

namespace CoreAndSkill.Core.Application.Notifications;

// docs/contracts/notifications.md. Bốn use case, MỌI use case đều chỉ chạm thông báo CỦA CHÍNH NGƯỜI
// GỌI — định danh lấy từ phiên (ICurrentUser), không bao giờ từ tham số.

// GET /api/v1/core/notifications — tham số phân trang theo khuôn chung (contracts/README.md §8).
public sealed record GetNotificationsQuery(int Page = 1, int PageSize = 20, bool UnreadOnly = false)
    : IQuery<PagedList<NotificationDto>>;

// GET /api/v1/core/notifications/unread-count
public sealed record GetUnreadNotificationCountQuery : IQuery<int>;

// PUT /api/v1/core/notifications/{id}/read
public sealed record MarkNotificationReadCommand(Guid Id) : ICommand;

// PUT /api/v1/core/notifications/read-all
public sealed record MarkAllNotificationsReadCommand : ICommand;
