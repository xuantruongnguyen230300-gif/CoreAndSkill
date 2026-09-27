using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Outbox;

// Lỗi KHÔNG THỬ LẠI của bên nhận outbox — không đi ra HTTP, chỉ vào cột last_error của dòng `dead`.
public static class OutboxHandlingErrors
{
    public static readonly Error PayloadInvalid = new(
        "CORE.OUTBOX.PAYLOAD_INVALID",
        "Nội dung sự kiện '{EventType}' không đúng hình dạng hợp đồng.",
        ErrorType.BusinessRule);

    // Không bên nhận nào cho EventType này — gồm cả phiên bản bên nhận không hiểu: từ chối rõ ràng
    // thay vì cố xử lý (12-notifications.md §2.6).
    public static readonly Error NoHandler = new(
        "CORE.OUTBOX.NO_HANDLER",
        "Không có bên nhận nào cho sự kiện '{EventType}'.",
        ErrorType.BusinessRule);
}
