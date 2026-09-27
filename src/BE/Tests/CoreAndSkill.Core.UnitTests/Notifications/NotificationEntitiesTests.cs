using CoreAndSkill.Core.Domain.Notifications;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Notifications;

public class NotificationEntitiesTests
{
    [Fact]
    public void Notification_Create_StoresCodeAndParams_NotAComposedSentence()
    {
        var notification = Notification.Create("  core.user.locked  ", "{\"UserName\":\"an.nv\"}", "core").Value;

        notification.Code.ShouldBe("core.user.locked");
        notification.Params.ShouldBe("{\"UserName\":\"an.nv\"}");
        notification.ModuleKey.ShouldBe("core");
        notification.Severity.ShouldBe("info");
        notification.LinkRoute.ShouldBeNull();
    }

    [Fact]
    public void Notification_HasNoPropertyThatCouldHoldAComposedSentence()
    {
        // 12-notifications.md §5: "lưu tham số, không lưu câu đã ghép". Nếu ai thêm cột `message`/`text`/
        // `body`/`title` vào entity, test này đỏ — đó là dấu hiệu đang lưu câu chữ thay vì khoá.
        var stringProperties = typeof(Notification)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name)
            .ToList();

        stringProperties.ShouldNotContain("Message");
        stringProperties.ShouldNotContain("Text");
        stringProperties.ShouldNotContain("Body");
        stringProperties.ShouldNotContain("Title");
        stringProperties.ShouldNotContain("Content");
        stringProperties.ShouldBe(["Code", "Params", "Severity", "LinkRoute", "ModuleKey"], ignoreOrder: true);
    }

    [Fact]
    public void Recipient_Create_StartsUnread()
    {
        var notificationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var recipient = NotificationRecipient.Create(notificationId, userId).Value;

        recipient.NotificationId.ShouldBe(notificationId);
        recipient.UserId.ShouldBe(userId);
        recipient.ReadAt.ShouldBeNull();
    }

    [Fact]
    public void Recipient_MarkRead_KeepsTheFirstReadTimestamp()
    {
        var recipient = NotificationRecipient.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var first = new DateTimeOffset(2026, 9, 21, 1, 0, 0, TimeSpan.Zero);

        recipient.MarkRead(first);
        recipient.MarkRead(first.AddHours(5));

        recipient.ReadAt.ShouldBe(first);
    }
}
