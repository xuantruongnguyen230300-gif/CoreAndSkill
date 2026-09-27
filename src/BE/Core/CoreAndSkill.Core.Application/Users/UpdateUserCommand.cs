using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Users;

// PUT /api/v1/core/users/{id} — docs/contracts/users.md §6. KHÔNG đụng vai trò — §7 riêng.
public sealed record UpdateUserCommand(Guid Id, string Email, string FullName, string? Version) : ICommand;
