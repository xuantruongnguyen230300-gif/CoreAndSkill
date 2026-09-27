namespace CoreAndSkill.Core.Domain.Outbox;

// Đúng tập giá trị của cột core.outbox_message.status — docs/database/schema-core.md §8. `status` là
// nguồn duy nhất của trạng thái dòng.
public static class OutboxStatus
{
    // Chờ phát; next_attempt_at chỉ là lịch của lần thử kế.
    public const string Pending = "pending";

    // Chạm ngưỡng thử lại (hoặc lỗi không thử lại được) — bộ phát KHÔNG tự chạm nữa; cần người xem.
    public const string Dead = "dead";

    // Đã phát; processed_at ghi thời điểm.
    public const string Done = "done";
}
