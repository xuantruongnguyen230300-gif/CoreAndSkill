using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Auth;

// GET /api/v1/core/auth/me — docs/contracts/auth.md §5. Không tham số: định danh lấy từ
// ICurrentUser (phiên đang gọi).
public sealed record GetCurrentSessionQuery : IQuery<SessionDto>;
