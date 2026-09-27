using System.Diagnostics;
using CoreAndSkill.Core.Application.Audit;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Audit;
using CoreAndSkill.Core.Infrastructure.Persistence;

namespace CoreAndSkill.Core.Infrastructure.Audit;

// Ghi nhật ký kiểm toán TƯỜNG MINH — docs/wiki-core/be/10-data-retention.md §5. Bổ sung cho
// AuditLogInterceptor (chỉ thấy thay đổi dữ liệu trong ChangeTracker): xuất là ĐỌC hàng loạt, phát lại
// outbox là thao tác trên dòng của đơn vị KHÁC — không cái nào để lại thay đổi nào cho interceptor thấy.
//
// Chỉ THÊM dòng vào ngữ cảnh; giao dịch bao quanh quyết định commit. Người làm lấy từ ICurrentUser —
// không có (lệnh dòng lệnh, việc nền) thì ghi `system`, đúng khuôn của AuditInterceptor. Dấu xuyên đơn vị (luật M14)
// đọc từ CrossTenantActorScope như AuditLogInterceptor — cùng một nguồn cho hai đường ghi.
internal sealed class EfAuditTrail(
    CoreDbContext db,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IClientAddressAccessor clientAddress,
    TimeProvider timeProvider,
    CrossTenantActorScope crossTenantActor)
    : IAuditTrail
{
    public void Record(AuditEntry entry)
    {
        // Không tự bịa đơn vị (luật M8): entry không nói thì lấy từ ngữ cảnh, ngữ cảnh không có thì lỗi
        // lập trình — ném, không ghi Guid.Empty.
        var tenantId = entry.TenantId ?? tenantContext.TenantId
            ?? throw new InvalidOperationException(
                $"Không ghi được nhật ký '{entry.ActionCode}': không có đơn vị trong AuditEntry lẫn ngữ cảnh hiện tại.");

        var record = AuditLog.Record(
            tenantId,
            timeProvider.GetUtcNow(),
            currentUser.UserId,
            currentUser.UserName ?? SystemActor.UserName,
            entry.ActionCode,
            entry.TargetType,
            entry.TargetId,
            entry.TargetDisplay,
            actorTenantId: crossTenantActor.ActorTenantId is { } actor && actor != tenantId ? actor : null,
            afterValue: entry.AfterValueJson,
            ipAddress: clientAddress.RemoteIpAddress,
            traceId: Activity.Current?.TraceId.ToHexString());

        db.AuditLogs.Add(record.Value);
    }
}
