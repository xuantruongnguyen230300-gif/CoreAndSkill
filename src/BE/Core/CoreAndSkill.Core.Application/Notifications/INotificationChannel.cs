using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Notifications;

// Danh sách người nhận SAU KHI đã lọc theo sở thích — kênh không tự hỏi lại.
public sealed record NotificationDelivery(NotificationDraft Draft, IReadOnlyCollection<Guid> RecipientUserIds);

// Một cài đặt cho MỖI kênh — docs/wiki-core/be/12-notifications.md §3.2. Kênh trả Failure khi lỗi
// KHÔNG thử lại được (thiếu mẫu, chưa cấu hình); lỗi tạm thời (mạng) là exception để outbox thử lại.
public interface INotificationChannel
{
    NotificationChannels Channel { get; }

    Task<Result> DeliverAsync(NotificationDelivery delivery, CancellationToken ct);
}

// "Những ai trong danh sách này muốn nhận thông báo loại đó qua kênh đó" — §3.3. Ngay ở v1 cần MỘT chỗ
// trả lời câu này: không có thì cách duy nhất để tắt bớt thông báo là sửa code.
//
// Hỏi theo TẬP, không theo từng người, và đó là một quyết định về hình dạng seam chứ không phải về hiệu
// năng của bản mặc định: bản của Core chạy trong bộ nhớ nên hỏi kiểu nào cũng như nhau, nhưng dự án cài
// bản đọc database thì một câu hỏi mỗi người là một truy vấn — 200 người nhận trên 2 kênh thành 400 truy
// vấn tuần tự, và chỗ gọi nằm trong Core nên dự án KHÔNG sửa được. Hình dạng sai ở đây là hình dạng sai
// vĩnh viễn với mọi dự án hạ nguồn.
//
// Hợp đồng của hiện thực: trả về tập con của userIds — giữ nguyên thứ tự đầu vào, không thêm ai không
// được hỏi. Hiện thực mặc định của Core trả nguyên tập (mặc định là bật). Schema chưa có bảng sở thích
// (schema-core.md không khai) nên Core chưa lưu gì; dự án có bảng ánh xạ người dùng x loại x kênh thì
// đăng ký hiện thực của mình đè lên — không phải sửa bên gửi.
public interface INotificationPreferences
{
    Task<IReadOnlyCollection<Guid>> FilterEnabledAsync(
        IReadOnlyCollection<Guid> userIds, string code, NotificationChannels channel, CancellationToken ct);
}

internal sealed class AlwaysEnabledNotificationPreferences : INotificationPreferences
{
    public Task<IReadOnlyCollection<Guid>> FilterEnabledAsync(
        IReadOnlyCollection<Guid> userIds, string code, NotificationChannels channel, CancellationToken ct)
        => Task.FromResult(userIds);
}
