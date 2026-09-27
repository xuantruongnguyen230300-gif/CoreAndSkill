using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Outbox;

// Một integration event chờ phát — docs/database/schema-core.md §8, docs/wiki-core/be/12-notifications.md §2.
// Ghi trong CÙNG giao dịch với thay đổi nghiệp vụ (OutboxInterceptor): hoặc cả hai cùng có, hoặc cả
// hai cùng không — đó là toàn bộ lý do Outbox tồn tại.
//
// KHÔNG kế thừa BaseEntity: không khối audit, không xoá mềm (hàng đợi vận hành; dòng đã phát được dọn
// theo chính sách lưu giữ). Mang tenant_id + người kích hoạt để bộ phát mở lại phạm vi ngữ cảnh thực
// thi THEO TỪNG DÒNG (docs/quy-uoc/be-architecture.md §1.1).
public sealed class OutboxMessage : ITenantScoped
{
    public Guid Id { get; init; } = EntityId.New();

    public Guid TenantId { get; init; }

    public DateTimeOffset OccurredAt { get; private set; }

    // Khoá hợp đồng ổn định kèm phiên bản — IDomainEvent.EventType.
    public string EventType { get; private set; } = string.Empty;

    // jsonb, TỰ CHỨA: bên xử lý không phải đọc lại DB để hiểu sự kiện. Không dữ liệu nhạy cảm.
    public string Payload { get; private set; } = "{}";

    // Correlation id của request đã sinh ra sự kiện — nối việc nền với request.
    public string? TraceId { get; private set; }

    public Guid? TriggeredByUserId { get; private set; }
    public string? TriggeredByUserName { get; private set; }

    public string Status { get; private set; } = OutboxStatus.Pending;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }

    // Lỗi cuối, ĐÃ qua bộ lọc trường nhạy cảm và cắt ngắn.
    public string? LastError { get; private set; }

    private OutboxMessage()
    {
        // EF Core cần ctor không tham số.
    }

    public static Result<OutboxMessage> Create(
        Guid tenantId,
        DateTimeOffset occurredAt,
        string eventType,
        string payload,
        string? traceId,
        Guid? triggeredByUserId,
        string? triggeredByUserName)
        => Result.Success(new OutboxMessage
        {
            TenantId = tenantId,
            OccurredAt = occurredAt,
            EventType = eventType,
            Payload = payload,
            TraceId = traceId,
            TriggeredByUserId = triggeredByUserId,
            TriggeredByUserName = triggeredByUserName,
            // Nhặt được ngay ở nhịp quét kế — index một phần theo next_attempt_at.
            NextAttemptAt = occurredAt,
        });

    public void MarkDone(DateTimeOffset now)
    {
        Status = OutboxStatus.Done;
        ProcessedAt = now;
        NextAttemptAt = null;
        LastError = null;
    }

    // retryable = false (dữ liệu sai hình dạng, phiên bản lạ, không có bên nhận): thử lại bao nhiêu
    // lần cũng vậy, chuyển thẳng sang `dead`. retryable = true: thử lại tới maxAttempts rồi mới `dead`.
    public void RecordFailure(
        DateTimeOffset now, string error, bool retryable, int maxAttempts, TimeSpan retryDelay)
    {
        AttemptCount++;
        LastError = error;

        if (!retryable || AttemptCount >= maxAttempts)
        {
            Status = OutboxStatus.Dead;
            NextAttemptAt = null; // chỉ là lịch của lần thử kế; dòng chết không có lần kế.
            return;
        }

        Status = OutboxStatus.Pending;
        NextAttemptAt = now + retryDelay;
    }

    // `core outbox-replay` — chỉ dòng `dead`: phát lại một dòng `pending` hay `done` là phát trùng.
    public Result Replay(DateTimeOffset now)
    {
        if (Status != OutboxStatus.Dead)
            return Result.Failure(OutboxErrors.NotDead);

        Status = OutboxStatus.Pending;
        AttemptCount = 0;
        NextAttemptAt = now;
        return Result.Success();
    }
}
