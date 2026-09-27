using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Users;

// POST /api/v1/core/users/{id}/reset-password — docs/contracts/users.md §9.
public sealed record ResetPasswordCommand(Guid UserId, string TempPassword, string? Version) : ICommand;
