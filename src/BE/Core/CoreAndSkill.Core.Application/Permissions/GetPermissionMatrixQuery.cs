using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Permissions;

// GET /api/v1/core/permissions/matrix — docs/contracts/permissions.md §5.
public sealed record GetPermissionMatrixQuery : IQuery<PermissionMatrixDto>;
