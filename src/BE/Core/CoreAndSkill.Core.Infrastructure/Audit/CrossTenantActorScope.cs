namespace CoreAndSkill.Core.Infrastructure.Audit;

// Luật M14 — docs/database/schema-core.md §9.4 điểm 4, docs/contracts/tenants.md §5: dòng nhật ký ghi ở một đơn vị mà
// NGƯỜI THỰC HIỆN thuộc đơn vị khác phải mang actor_tenant_id — kể cả dòng AuditLogInterceptor tự sinh (core.user.*,
// core.role.*) và dòng IAuditTrail ghi tường minh. Đơn vị của người thực hiện là đơn vị theo CLAIM của phiếu, không phải
// phạm vi đơn vị đang mở: khi service tạo đơn vị mở phạm vi của đơn vị đích, ITenantContext đã trả đơn vị đích, và mọi
// đường ghi bên trong không còn thấy người gọi đến từ đâu.
//
// Chỗ DUY NHẤT biết cả hai đơn vị là nơi mở phạm vi (TenantProvisioningService.EnterTenantScope): nó đọc ITenantContext
// TRƯỚC khi Enter — lúc đó giá trị là claim của phiếu — rồi đánh dấu vào đây khi hai đơn vị khác nhau. Cùng khuôn với
// PasswordRehashScope: Scoped, cùng phạm vi DI với DbContext và các interceptor của request; lồng được (đếm độ sâu), và
// Dispose khôi phục giá trị trước đó.
//
// Không mở rộng ICurrentUser/ITenantContext/ExecutionContextSnapshot cho việc này — chữ ký của chúng là định nghĩa gốc
// ở docs/quy-uoc/be-architecture.md §1.1 ("TenantId CHỈ có trên ITenantContext"), đổi là quyết định kiến trúc.
public sealed class CrossTenantActorScope
{
    private Guid? _actorTenantId;

    // Đơn vị của người thực hiện khi nó KHÁC đơn vị đang mở; null ở mọi lúc khác.
    public Guid? ActorTenantId => _actorTenantId;

    // actorTenantId: đơn vị của người gọi theo claim, đọc TRƯỚC khi mở phạm vi đơn vị đích.
    internal IDisposable Begin(Guid actorTenantId)
    {
        var previous = _actorTenantId;
        _actorTenantId = actorTenantId;
        return new Exit(this, previous);
    }

    private sealed class Exit(CrossTenantActorScope owner, Guid? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            owner._actorTenantId = previous;
        }
    }
}
