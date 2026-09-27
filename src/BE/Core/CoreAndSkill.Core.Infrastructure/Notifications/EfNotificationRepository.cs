using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Notifications;

// Hiện thực INotificationRepository. Mọi truy vấn đọc đều lọc theo userId của NGƯỜI GỌI ở đây —
// không truyền bộ lọc người dùng qua tham số HTTP — và đi qua bộ lọc đơn vị + xoá mềm toàn cục.
internal sealed class EfNotificationRepository(CoreDbContext db, ICurrentUser currentUser) : INotificationRepository
{
    public async Task AddAsync(Notification notification, IReadOnlyCollection<NotificationRecipient> recipients, CancellationToken ct)
    {
        await db.Notifications.AddAsync(notification, ct);
        await db.NotificationRecipients.AddRangeAsync(recipients, ct);
    }

    public async Task<PagedList<NotificationDto>> ListForUserAsync(
        Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct)
    {
        var recipients = db.NotificationRecipients.Where(r => r.UserId == userId);
        if (unreadOnly)
            recipients = recipients.Where(r => r.ReadAt == null);

        var query =
            from r in recipients
            join n in db.Notifications on r.NotificationId equals n.Id
            orderby r.CreatedAt descending, r.Id
            select new { n.Id, n.Code, n.Params, n.CreatedAt, r.ReadAt };

        var total = await query.CountAsync(ct);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedList<NotificationDto>
        {
            Items = [.. rows.Select(r => new NotificationDto(r.Id, r.Code, ParseParams(r.Params), r.CreatedAt, r.ReadAt))],
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    // Đếm trên chỉ mục một phần ix_notification_recipient_unread (tenant_id, user_id, created_at DESC
    // WHERE read_at IS NULL AND is_deleted = false) — không nạp danh sách rồi đếm. Endpoint này bị gọi
    // lặp theo chu kỳ nên phải rẻ (contracts/notifications.md §2).
    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct)
        => db.NotificationRecipients.CountAsync(r => r.UserId == userId && r.ReadAt == null, ct);

    public Task<NotificationRecipient?> FindRecipientAsync(Guid notificationId, Guid userId, CancellationToken ct)
        => db.NotificationRecipients.FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.UserId == userId, ct);

    // ExecuteUpdate không đi qua ChangeTracker nên AuditInterceptor không chạy — tự điền hai cột vết.
    public Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        var actor = currentUser.UserName ?? SystemActor.UserName;

        return db.NotificationRecipients
            .Where(r => r.UserId == userId && r.ReadAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.ReadAt, (DateTimeOffset?)now)
                .SetProperty(r => r.UpdatedAt, (DateTimeOffset?)now)
                .SetProperty(r => r.UpdatedBy, actor), ct);
    }

    private static IReadOnlyDictionary<string, string> ParseParams(string? json)
        => string.IsNullOrEmpty(json)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
}
