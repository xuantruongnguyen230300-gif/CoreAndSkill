using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Outbox;

public static class OutboxErrors
{
    public static readonly Error NotFound = new(
        "CORE.OUTBOX.NOT_FOUND",
        "Không có bản ghi outbox nào mang định danh đó.",
        ErrorType.NotFound);

    public static readonly Error NotDead = new(
        "CORE.OUTBOX.NOT_DEAD",
        "Bản ghi outbox không ở trạng thái 'dead' nên không phát lại được.",
        ErrorType.BusinessRule);
}
