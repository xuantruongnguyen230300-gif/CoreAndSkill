using System.Diagnostics;
using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;

// Đường GHI outbox — docs/wiki-core/be/12-notifications.md §2.1: "một bộ chặn ở tầng dữ liệu thu các sự
// kiện mà entity đã ghi nhận, chuyển thành bản ghi Outbox, ngay trước khi lưu". Làm ở tầng này có hai
// cái lợi: handler không phải nhớ ghi outbox, và KHÔNG THỂ QUÊN.
//
// Dòng outbox được thêm vào CHÍNH lượt SaveChanges đang chạy nên nằm trong CÙNG giao dịch với thay đổi
// nghiệp vụ: hoặc cả hai cùng có, hoặc cả hai cùng không. Ghi Outbox ở giao dịch khác thì hai thứ không
// còn cùng số phận — dữ liệu quay lại mà thông báo vẫn đi, hoặc dữ liệu đã lưu mà không ai được báo.
//
// PHẢI đăng ký SAU TenantAssignmentInterceptor: TenantId của dòng outbox lấy từ CHÍNH entity phát sự
// kiện (đã được gán đúng), không đọc lại ambient. Đơn vị và người kích hoạt được CHỤP lên dòng để bộ
// phát mở lại phạm vi ngữ cảnh theo từng dòng (docs/quy-uoc/be-architecture.md §1.1).
public sealed class OutboxInterceptor(TimeProvider timeProvider, ICurrentUser currentUser, ITenantContext tenantContext)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stage(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stage(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // internal: test gọi thẳng để kiểm việc chuyển sự kiện thành dòng outbox mà không cần database.
    internal void Stage(DbContext? context)
    {
        if (context is null)
            return;

        // Vật liệu hoá TRƯỚC khi Add — không sửa tracker trong lúc đang duyệt nó.
        var sources = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (sources.Count == 0)
            return;

        var now = timeProvider.GetUtcNow();
        var traceId = Activity.Current?.TraceId.ToHexString();

        foreach (var source in sources)
        {
            // Đơn vị của dòng outbox = đơn vị của entity phát sự kiện; entity không thuộc đơn vị nào thì
            // lấy từ ngữ cảnh. Không có đơn vị nào ⇒ TỪ CHỐI LƯU (luật M8) — cùng nguyên tắc với
            // TenantAssignmentInterceptor, không bao giờ ghi Guid.Empty.
            var tenantId = (source as ITenantScoped)?.TenantId is { } own && own != Guid.Empty
                ? own
                : tenantContext.TenantId
                    ?? throw new InvalidOperationException(
                        $"Không thể ghi outbox cho sự kiện của '{source.GetType().Name}' vì chưa có đơn vị trong ngữ cảnh hiện tại " +
                        "(luật M8 — docs/wiki-core/be/17-multi-tenant.md §2).");

            foreach (var domainEvent in source.DomainEvents)
            {
                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), OutboxJson.Options);

                var message = OutboxMessage.Create(
                    tenantId,
                    now,
                    domainEvent.EventType,
                    payload,
                    traceId,
                    currentUser.UserId,
                    currentUser.UserName);

                context.Set<OutboxMessage>().Add(message.Value);
            }

            // Sự kiện đã chuyển thành dòng outbox — xoá để lần lưu sau của cùng entity không ghi lại.
            source.ClearDomainEvents();
        }
    }
}
