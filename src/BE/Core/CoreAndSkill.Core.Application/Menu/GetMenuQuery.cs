using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Menu;

// GET /api/v1/core/meta/menu — docs/contracts/meta-menu.md §1.
public sealed record GetMenuQuery : IQuery<IReadOnlyList<MenuItemDto>>;
