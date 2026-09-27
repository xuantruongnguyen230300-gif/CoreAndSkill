using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Jobs;

// Catalog lỗi của invariant Job — cùng project với entity gọi nó (docs/quy-uoc/be-entity-domain.md §3.3).
public static class JobStateErrors
{
    public static readonly Error InvalidTransition = new(
        "CORE.JOB.INVALID_TRANSITION",
        "Việc ở trạng thái '{From}' không chuyển được sang '{To}'.",
        ErrorType.BusinessRule);
}
