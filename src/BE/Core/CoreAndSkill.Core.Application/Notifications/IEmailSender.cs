using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Notifications;

// Email đã kết xuất, sẵn sàng gửi. Có CẢ bản văn bản thuần lẫn HTML — một số trình đọc thư chỉ hiển
// thị văn bản thuần (docs/wiki-core/be/12-notifications.md §4.3).
public sealed record RenderedEmail(string Subject, string TextBody, string HtmlBody);

public sealed record EmailMessage(string ToAddress, string ToDisplayName, RenderedEmail Content);

// Interface KẾT XUẤT MẪU — Core KHÔNG đi kèm mẫu thư nào (§4.1): làm vậy là đưa câu chữ vào Core, và
// Core sẽ không mang đi được sang dự án có giọng văn hoặc ngôn ngữ khác. Nội dung mẫu và bản dịch
// thuộc DỰ ÁN; Core giữ cơ chế (interface này, chọn ngôn ngữ theo người nhận, gửi).
//
// Hợp đồng cho hiện thực: tham số theo TÊN (luật R7); THOÁT KÝ TỰ khi kết xuất HTML — tên người dùng
// chứa ký tự đặc biệt không được phá email và không được thành đường chèn script; thiếu mẫu thì trả
// Failure, KHÔNG gửi email có chỗ trống chưa thay.
public interface INotificationTemplateRenderer
{
    Task<Result<RenderedEmail>> RenderAsync(
        string code, string language, IReadOnlyDictionary<string, string> parameters, CancellationToken ct);
}

// Seam gửi thư. Core KHÔNG chọn nhà cung cấp hay thư viện gửi thư: "cấu hình dịch vụ gửi ở dự án"
// (12-notifications.md §6). KHÔNG có hiện thực mặc định — thiếu đăng ký mà có thông báo yêu cầu kênh
// email thì thất bại RÕ RÀNG (NotificationErrors.EmailNotConfigured, dòng outbox `dead`), không im lặng
// bỏ qua.
//
// Failure = lỗi KHÔNG thử lại (địa chỉ không tồn tại, bị từ chối vĩnh viễn). Lỗi tạm thời (mạng,
// dịch vụ quá tải) là EXCEPTION để outbox thử lại có lùi dần (§2.4).
public interface IEmailSender
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken ct);
}

// Hiện thực mặc định của bộ kết xuất: LUÔN thiếu mẫu. Đúng theo §4.3 — Core không có mẫu nào, và một
// dự án chưa đăng ký bộ kết xuất của mình sẽ thấy lỗi rõ ràng thay vì email rỗng.
internal sealed class MissingTemplateRenderer : INotificationTemplateRenderer
{
    public Task<Result<RenderedEmail>> RenderAsync(
        string code, string language, IReadOnlyDictionary<string, string> parameters, CancellationToken ct)
        => Task.FromResult(Result.Failure<RenderedEmail>(
            NotificationErrors.TemplateMissing.WithParams(("Code", code), ("Language", language))));
}
