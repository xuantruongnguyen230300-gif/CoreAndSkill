using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Permissions;

// GET /api/v1/core/permissions/matrix/by-resource — docs/contracts/permissions.md §7.
public sealed record GetPermissionMatrixByResourceQuery : IQuery<PermissionMatrixByResourceDto>;
