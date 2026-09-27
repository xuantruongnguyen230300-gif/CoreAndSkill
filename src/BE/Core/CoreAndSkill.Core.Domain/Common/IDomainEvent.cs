using System.Text.Json.Serialization;

namespace CoreAndSkill.Core.Domain.Common;

// Một chuyện vừa xảy ra mà entity ghi nhận — docs/wiki-core/be/12-notifications.md §1, §2.1.
// Entity KHÔNG biết kênh gửi hay ai nghe: nó chỉ ghi nhận sự kiện; bộ chặn ở tầng dữ liệu chuyển sự
// kiện thành bản ghi outbox trong CÙNG giao dịch với thay đổi (core.outbox_message).
//
// EventType là khoá hợp đồng ổn định, KHÔNG phải tên kiểu CLR: đổi tên/di chuyển lớp không được làm
// hàng chờ đang có mồ côi. Hậu tố ".vN" là số phiên bản hợp đồng (12-notifications.md §2.6) — đổi
// nghĩa một trường thì đặt phiên bản mới, đừng đổi nghĩa trường cũ.
//
// Payload được tuần tự hoá từ CHÍNH đối tượng sự kiện: chỉ chứa định danh và giá trị vô hại, không
// bao giờ mật khẩu, số giấy tờ hay nội dung tệp (12-notifications.md §2.2).
public interface IDomainEvent
{
    [JsonIgnore]
    string EventType { get; }
}

// Entity nào ghi nhận sự kiện thì cài interface này; OutboxInterceptor thu và xoá danh sách ngay
// trước khi lưu.
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
