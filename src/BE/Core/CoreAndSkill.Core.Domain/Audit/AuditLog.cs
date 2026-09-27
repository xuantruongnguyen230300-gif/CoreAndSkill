using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Audit;

// Một dòng là một hành động đã xảy ra — docs/database/schema-core.md §9.4, docs/wiki-core/be/10-data-retention.md §5.
// KHÔNG kế thừa BaseEntity (không có năm cột audit, không xoá mềm — bảng CHỈ ghi thêm, bất biến ép
// bằng quyền DB, không bằng quy ước — luật M13). KHÔNG khai ITenantScoped: TenantId của MỖI dòng do
// người GHI quyết định tường minh (đơn vị nào, không phải "đơn vị của người gọi hiện tại") — thao
// tác xuyên đơn vị ghi HAI dòng, mỗi dòng một TenantId khác nhau trong cùng một request
// (docs/adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md §4). Nếu implement ITenantScoped,
// TenantAssignmentInterceptor sẽ ép TenantId theo NGỮ CẢNH THỰC THI đang mở tại thời điểm
// SaveChanges — đúng cho phần lớn dòng tự động (§ Interceptor), nhưng sai cho dòng thứ hai của thao
// tác xuyên đơn vị nếu ghi qua đường khác with cùng SaveChanges call. Vì vậy factory Record() bên
// dưới nhận TenantId làm tham số bắt buộc, không suy ngầm.
public sealed class AuditLog
{
    public Guid Id { get; init; } = EntityId.New();

    public Guid TenantId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    public Guid? ActorUserId { get; private set; }
    public Guid? ActorTenantId { get; private set; } // khác null ⇒ dấu hiệu THAO TÁC XUYÊN ĐƠN VỊ
    public string ActorDisplay { get; private set; } = string.Empty;

    public string ActionCode { get; private set; } = string.Empty; // khuôn "<tài nguyên>.<hành động>"
    public string TargetType { get; private set; } = string.Empty;
    public string TargetId { get; private set; } = string.Empty; // text — module có thể không dùng UUID
    public string? TargetDisplay { get; private set; }

    public string? BeforeValue { get; private set; } // jsonb — chỉ với thay đổi quan trọng
    public string? AfterValue { get; private set; }

    public System.Net.IPAddress? IpAddress { get; private set; }
    public string? TraceId { get; private set; }

    private AuditLog()
    {
        // EF Core cần ctor không tham số.
    }

    // occurredAt truyền vào từ TimeProvider của chỗ gọi — entity không tự đọc đồng hồ hệ thống
    // (cùng nguyên tắc "test thay được đồng hồ" áp cho AuditInterceptor).
    //
    // Trả Result<AuditLog> dù hôm nay KHÔNG có nhánh hỏng nào — luật factory ở
    // docs/quy-uoc/be-entity-domain.md §3.2 vô điều kiện, và Tenant.Create/RolePermission.Create cũng
    // không có nhánh hỏng mà vẫn trả Result. Chữ ký giữ nguyên để ngày thêm một bất biến (khuôn
    // "<tài nguyên>.<hành động>" của actionCode chẳng hạn) không phải đổi KIỂU TRẢ VỀ.
    //
    // NHƯNG KHÔNG PHẢI "chỉ sửa thân hàm": mọi chỗ gọi hôm nay đọc thẳng `.Value` mà KHÔNG kiểm
    // IsSuccess, và Result<T>.Value NÉM khi thất bại. Ngày thêm nhánh hỏng đầu tiên, bốn chỗ gọi
    // dưới đây đổi hành vi từ "trả lỗi" sang "ném", và hai chỗ đầu ném GIỮA SavingChanges — nơi một
    // ngoại lệ làm hỏng cả giao dịch chứ không chỉ dòng nhật ký:
    //   • Core.Infrastructure/Persistence/Interceptors/AuditLogInterceptor.cs — WriteRecord, một
    //     hàm trả void, không có kênh nào để mang lỗi ra;
    //   • Core.Infrastructure/Tenants/TenantProvisioningService.cs — bốn lời gọi, trong đó ba lời
    //     gọi của thao tác xuyên đơn vị nằm trong cùng một SaveChanges.
    // Nên việc của ngày đó là: thêm bất biến VÀ quyết định mỗi chỗ gọi làm gì khi nó vỡ. Câu trước
    // đây ở đây nói ngược lại ("chỉ phải sửa thân hàm"), và đó đúng là cách một thay đổi tưởng là
    // cục bộ đi ra tới giao dịch.
    public static Result<AuditLog> Record(
        Guid tenantId,
        DateTimeOffset occurredAt,
        Guid? actorUserId,
        string actorDisplay,
        string actionCode,
        string targetType,
        string targetId,
        string? targetDisplay = null,
        Guid? actorTenantId = null,
        string? beforeValue = null,
        string? afterValue = null,
        System.Net.IPAddress? ipAddress = null,
        string? traceId = null)
        => Result.Success(new AuditLog
        {
            TenantId = tenantId,
            OccurredAt = occurredAt,
            ActorUserId = actorUserId,
            ActorTenantId = actorTenantId,
            ActorDisplay = actorDisplay,
            ActionCode = actionCode,
            TargetType = targetType,
            TargetId = targetId,
            TargetDisplay = targetDisplay,
            BeforeValue = beforeValue,
            AfterValue = afterValue,
            IpAddress = ipAddress,
            TraceId = traceId,
        });
}
