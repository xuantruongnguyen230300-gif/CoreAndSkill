namespace CoreAndSkill.Core.Application.Menu;

// docs/contracts/meta-menu.md §1. Danh sách PHẲNG — không lồng cây; FE dựng cây bằng ParentId.
public sealed record MenuItemDto(
    Guid Id, Guid? ParentId, string Code, string LabelKey, string? Icon, string? Route, int DisplayOrder);

public interface IMenuQueryService
{
    // Trả cây menu mà NGƯỜI DÙNG userId được thấy — thứ tự quyết định:
    // docs/database/schema-core.md §6.3, docs/contracts/meta-menu.md §1.4, §1.5.
    Task<IReadOnlyList<MenuItemDto>> GetVisibleMenuAsync(Guid userId, CancellationToken ct);
}
