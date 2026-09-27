using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.ClientErrors;

// POST /api/v1/core/client-errors — docs/contracts/client-errors.md §1. Không mã lỗi RIÊNG — chỉ
// dùng mã dùng chung (CORE.VALIDATION.FAILED, CSRF, Origin, rate limit).
public sealed record ReportClientErrorCommand(
    string Kind,
    string Message,
    string? Stack,
    string DuongDan,
    string? TraceId,
    string PhienBanApp) : ICommand;
