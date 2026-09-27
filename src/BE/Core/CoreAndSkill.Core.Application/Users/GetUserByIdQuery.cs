using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Users;

// GET /api/v1/core/users/{id} — docs/contracts/users.md §4.
public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserListItemDto>;
