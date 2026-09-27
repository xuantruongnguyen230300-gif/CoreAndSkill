using System.Text.Json;
using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Outbox;

// Một dòng outbox đã sẵn sàng cho bên nhận — docs/wiki-core/be/12-notifications.md §2.2. TỰ CHỨA: bên
// xử lý không phải đọc lại DB để hiểu sự kiện.
public sealed record OutboxEnvelope(
    Guid Id,
    Guid TenantId,
    string EventType,
    string PayloadJson,
    DateTimeOffset OccurredAt,
    string? TraceId,
    Guid? TriggeredByUserId,
    string? TriggeredByUserName)
{
    // null khi payload không phải JSON hợp lệ của kiểu đó — bên nhận trả OutboxErrors.PayloadInvalid.
    public T? ReadPayload<T>() where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(PayloadJson, OutboxJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

// Một bộ tuần tự hoá cho CẢ HAI chiều (ghi payload ở interceptor, đọc ở bên nhận) — lệch cấu hình giữa
// hai đầu là đúng loại lỗi chỉ lộ ra khi sự kiện đầu tiên đi qua.
public static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

// Bên nhận một loại sự kiện. Nhiều bên nhận cho cùng một EventType là hợp lệ (mỗi bên làm việc của
// mình: dựng thông báo, đẩy việc vào hàng đợi…).
//
// KẾT QUẢ quyết định số phận dòng outbox:
//   - Success                → dòng chuyển `done` (sau khi MỌI bên nhận của loại đó thành công).
//   - Failure                → KHÔNG THỬ LẠI, chuyển thẳng `dead`: dữ liệu sai hình dạng, phiên bản lạ,
//                              đích không tồn tại — thử lại bao nhiêu lần cũng vậy (§2.4).
//   - Exception              → lỗi tạm thời (mạng, hệ ngoài): thử lại có lùi dần, tới ngưỡng thì `dead`.
// Phần ghi DB của bên nhận cùng nằm trong MỘT giao dịch với việc đánh dấu `done`.
public interface IOutboxEventHandler
{
    // Khoá hợp đồng kèm phiên bản — IDomainEvent.EventType.
    string EventType { get; }

    Task<Result> HandleAsync(OutboxEnvelope envelope, CancellationToken ct);
}
