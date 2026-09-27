using CoreAndSkill.Core.Application.Common.Interfaces;

namespace CoreAndSkill.Core.Infrastructure.Execution;

// Hiện thực IExecutionContextScope — giữ giá trị trong AsyncLocal, Singleton (giá trị KHÔNG nằm ở
// instance) — docs/quy-uoc/be-architecture.md §1.1, §5.2. Chỉ nơi gọi thuộc allowlist của luật A12
// được gọi Enter (ArchTest ở B2+, khi có project module để soát).
public sealed class ExecutionContextScope : IExecutionContextScope
{
    private static readonly AsyncLocal<ExecutionContextSnapshot?> Ambient = new();

    public ExecutionContextSnapshot? Current => Ambient.Value;

    public IDisposable Enter(Guid tenantId, Guid? userId, string? userName)
    {
        var previous = Ambient.Value;
        Ambient.Value = new ExecutionContextSnapshot(userId, userName, tenantId);

        return new ScopeHandle(previous);
    }

    private sealed class ScopeHandle(ExecutionContextSnapshot? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            Ambient.Value = previous; // khôi phục phạm vi TRƯỚC ĐÓ, không xoá về rỗng.
            _disposed = true;
        }
    }
}
