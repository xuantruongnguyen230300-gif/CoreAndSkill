namespace CoreAndSkill.Core.Application.Audit;

// Mã hành động của những dòng nhật ký kiểm toán mà HANDLER/dịch vụ ghi tường minh — khuôn
// "<tài nguyên>.<hành động>" (docs/database/schema-core.md §9.4). Các mã do AuditLogInterceptor tự
// sinh từ thay đổi dữ liệu nằm ở Core.Infrastructure (AuditActionCodes) — hai nguồn ghi, hai danh
// sách, vì hai đường ghi khác nhau.
public static class AuditActions
{
    // Xuất danh sách người dùng — docs/wiki-core/be/10-data-retention.md §5.4.
    public const string UserExport = "core.user.export";

    // `core outbox-replay` — docs/database/script-runbook.md §10.
    public const string OutboxReplay = "core.outbox.replay";
}

// Một dòng nhật ký ghi TƯỜNG MINH. Ai làm (người, địa chỉ, mã lần gọi) do hiện thực điền từ ngữ cảnh —
// chỗ gọi chỉ nói LÀM GÌ, TRÊN ĐỐI TƯỢNG NÀO. Không bao giờ nhét nội dung nhạy cảm vào đây (luật S13):
// giá trị jsonb chỉ chứa thứ đã chọn, không chứa nguyên đối tượng đầu vào.
//
// TenantId: null = đơn vị của ngữ cảnh hiện hành. Lệnh vận hành chạm dòng của đơn vị KHÁC (outbox-replay) phải
// nói tường minh nó thuộc đơn vị nào.
public sealed record AuditEntry(
    string ActionCode,
    string TargetType,
    string TargetId,
    string? TargetDisplay = null,
    string? AfterValueJson = null,
    Guid? TenantId = null);

// Seam ghi nhật ký kiểm toán tường minh — bổ sung cho AuditLogInterceptor, thứ chỉ thấy được thay đổi
// dữ liệu trong ChangeTracker. Xuất dữ liệu là ĐỌC hàng loạt nên không có thay đổi nào để interceptor
// thấy — đó là lý do seam này tồn tại.
//
// KHÔNG lưu: dòng chỉ được thêm vào ngữ cảnh dữ liệu, giao dịch bao quanh (TransactionBehavior hoặc
// IUnitOfWork ở chỗ gọi) quyết định commit — nhật ký và thao tác cùng số phận.
public interface IAuditTrail
{
    void Record(AuditEntry entry);
}
