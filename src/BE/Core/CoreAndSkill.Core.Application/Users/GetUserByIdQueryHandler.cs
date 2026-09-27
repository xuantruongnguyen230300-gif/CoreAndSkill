using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class GetUserByIdQueryHandler(IUserQueryService userQuery)
    : IRequestHandler<GetUserByIdQuery, Result<UserListItemDto>>
{
    public async Task<Result<UserListItemDto>> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await userQuery.FindByIdAsync(query.Id, ct);
        return user is null
            ? Result.Failure<UserListItemDto>(UserErrors.NotFound)
            : Result.Success(user);
    }
}
