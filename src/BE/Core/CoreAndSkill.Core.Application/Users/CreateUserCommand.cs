using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Users;

// POST /api/v1/core/users — docs/contracts/users.md §5.
public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string FullName,
    string TempPassword,
    IReadOnlyList<Guid> RoleIds) : ICommand<Guid>;
