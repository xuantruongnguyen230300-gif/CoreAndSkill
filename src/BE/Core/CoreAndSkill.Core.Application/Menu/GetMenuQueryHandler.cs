using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Menu;

internal sealed class GetMenuQueryHandler(ICurrentUser currentUser, IMenuQueryService menuQuery)
    : IRequestHandler<GetMenuQuery, Result<IReadOnlyList<MenuItemDto>>>
{
    public async Task<Result<IReadOnlyList<MenuItemDto>>> Handle(GetMenuQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("GetMenuQuery chạy khi chưa xác thực.");

        return Result.Success(await menuQuery.GetVisibleMenuAsync(userId, ct));
    }
}
