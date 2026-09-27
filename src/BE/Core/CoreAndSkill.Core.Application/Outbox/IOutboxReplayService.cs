using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Outbox;

// `core outbox-replay` — docs/database/script-runbook.md §10, docs/quy-uoc/be-architecture.md §3.
// Đặt dòng `dead` về `pending` (attempt_count = 0) để bộ phát nhặt ở nhịp quét kế — KHÔNG gọi bên nhận
// ngay trong lệnh, cùng đường phát với mọi dòng khác. Mỗi dòng phát lại một dòng nhật ký kiểm toán.
public interface IOutboxReplayService
{
    // Một dòng. Không có dòng đó, hoặc dòng không ở `dead` ⇒ thất bại và KHÔNG ghi gì (phát lại một
    // dòng pending/done là phát trùng).
    Task<Result> ReplayAsync(Guid id, CancellationToken ct);

    // Mọi dòng đang `dead`; trả số dòng đã phát lại.
    Task<Result<int>> ReplayAllDeadAsync(CancellationToken ct);
}
