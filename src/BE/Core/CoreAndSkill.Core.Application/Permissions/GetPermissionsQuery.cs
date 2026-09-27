using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Permissions;

// GET /api/v1/core/permissions — docs/contracts/permissions.md §4. KHÔNG phân trang (§4 "Ghi chú").
public sealed record GetPermissionsQuery : IQuery<IReadOnlyList<PermissionListItemDto>>;
