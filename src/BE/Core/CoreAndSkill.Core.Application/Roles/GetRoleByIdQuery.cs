using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Roles;

// GET /api/v1/core/roles/{id} — docs/contracts/roles.md §5.
public sealed record GetRoleByIdQuery(Guid Id) : IQuery<RoleSummaryDto>;
