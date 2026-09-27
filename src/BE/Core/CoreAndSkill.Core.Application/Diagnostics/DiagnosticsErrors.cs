using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Diagnostics;

public static class DiagnosticsErrors
{
    public static readonly Error ProbeFailureRequested = new(
        "CORE.DIAGNOSTICS.PROBE_FAILURE_REQUESTED",
        "Nhánh lỗi được yêu cầu qua tham số outcome=failure.",
        ErrorType.BusinessRule);
}
