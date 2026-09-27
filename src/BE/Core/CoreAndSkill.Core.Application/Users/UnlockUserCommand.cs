using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Users;

// POST /api/v1/core/users/{id}/unlock — docs/contracts/users.md §8. CỐ Ý không áp luật nào.
public sealed record UnlockUserCommand(Guid UserId, string? Version) : ICommand;
