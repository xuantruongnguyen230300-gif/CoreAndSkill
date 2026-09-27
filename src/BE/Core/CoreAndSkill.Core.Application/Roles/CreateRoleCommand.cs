using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Roles;

// POST /api/v1/core/roles — docs/contracts/roles.md §2.
public sealed record CreateRoleCommand(string Name) : ICommand<Guid>;
