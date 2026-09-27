using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Users;

// POST /api/v1/core/users/{id}/lock — docs/contracts/users.md §8.
public sealed record LockUserCommand(Guid UserId, string? Version) : ICommand;
