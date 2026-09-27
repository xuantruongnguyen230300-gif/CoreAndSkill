using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Roles;

// DELETE /api/v1/core/roles/{id} — docs/contracts/roles.md §4.
public sealed record DeleteRoleCommand(Guid Id) : ICommand;
