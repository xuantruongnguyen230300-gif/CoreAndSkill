using CoreAndSkill.Core.Application.Audit;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Outbox;

// `core outbox-replay` — docs/database/script-runbook.md §10. CHỈ chạm dòng `dead`; đặt về `pending` với
// attempt_count = 0 để bộ phát nhặt ở nhịp quét kế (không gọi bên nhận ngay). MỖI dòng phát lại một dòng
// nhật ký kiểm toán, ghi cùng giao dịch — phát lại là thao tác vận hành có hậu quả nghiệp vụ
// (10-data-retention.md §5.4), nên "đã phát lại" và "đã ghi vết" cùng có hoặc cùng không.
//
// Chạy ngoài request (lệnh dòng lệnh): không có đơn vị nào trong ngữ cảnh, nên bỏ filter đơn vị CÓ TÊN
// khi tìm dòng và ghi dòng nhật ký với đơn vị CỦA DÒNG ĐÓ (AuditEntry.TenantId) — không suy từ ambient.
internal sealed class OutboxReplayService(
    CoreDbContext db,
    IUnitOfWork unitOfWork,
    IAuditTrail auditTrail,
    TimeProvider timeProvider)
    : IOutboxReplayService
{
    public Task<Result> ReplayAsync(Guid id, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            var message = await db.OutboxMessages
                .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
                .FirstOrDefaultAsync(o => o.Id == id, innerCt);

            if (message is null)
                return new TransactionOutcome<Result>(Result.Failure(OutboxErrors.NotFound), ShouldCommit: false);

            var replayed = message.Replay(timeProvider.GetUtcNow());
            if (replayed.IsFailure)
                return new TransactionOutcome<Result>(replayed, ShouldCommit: false);

            RecordAudit(message);
            return new TransactionOutcome<Result>(Result.Success(), ShouldCommit: true);
        }, ct);

    public Task<Result<int>> ReplayAllDeadAsync(CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            var dead = await db.OutboxMessages
                .IgnoreQueryFilters([CoreQueryFilters.TenantKey])
                .Where(o => o.Status == OutboxStatus.Dead)
                .ToListAsync(innerCt);

            var now = timeProvider.GetUtcNow();
            foreach (var message in dead)
            {
                var replayed = message.Replay(now);
                if (replayed.IsFailure)
                    return new TransactionOutcome<Result<int>>(Result.Failure<int>(replayed.Error!), ShouldCommit: false);

                RecordAudit(message);
            }

            return new TransactionOutcome<Result<int>>(Result.Success(dead.Count), ShouldCommit: true);
        }, ct);

    private void RecordAudit(OutboxMessage message)
        => auditTrail.Record(new AuditEntry(
            AuditActions.OutboxReplay,
            TargetType: "core.outbox_message",
            TargetId: message.Id.ToString(),
            TargetDisplay: message.EventType,
            TenantId: message.TenantId));
}
