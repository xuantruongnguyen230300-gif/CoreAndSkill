using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Notifications;

// Ai nhận một thông báo và đã đọc chưa — docs/database/schema-core.md §7.2. UserId là id trần tới
// app_user (Identity không rời Core.Infrastructure — luật S5).
public sealed class NotificationRecipient : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; init; }

    public Guid NotificationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private NotificationRecipient()
    {
        // EF Core cần ctor không tham số.
    }

    public static Result<NotificationRecipient> Create(Guid notificationId, Guid userId)
        => Result.Success(new NotificationRecipient
        {
            NotificationId = notificationId,
            UserId = userId,
        });

    // Đánh dấu lại một thông báo đã đọc là hợp lệ và KHÔNG đổi mốc đọc đầu tiên.
    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;
}
