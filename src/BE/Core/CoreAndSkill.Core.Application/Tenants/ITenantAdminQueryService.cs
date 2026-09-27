using CoreAndSkill.Core.Application.Common.Paging;

namespace CoreAndSkill.Core.Application.Tenants;

// Seam CHỈ ĐỌC cho danh sách đơn vị của khu quản trị hệ thống — docs/contracts/tenants.md §1.
// Hiện thực ở Core.Infrastructure. Đơn vị hệ thống KHÔNG nằm trong kết quả (§1 "Ghi chú").
public sealed record TenantListItemDto(Guid Id, string Code, string Name, bool IsActive, DateTimeOffset? CreatedAt);

public sealed record TenantSearchCriteria(
    int Page, int PageSize, string SortBy, bool SortDescending, string? SearchText);

public interface ITenantAdminQueryService
{
    Task<PagedList<TenantListItemDto>> SearchAsync(TenantSearchCriteria criteria, CancellationToken ct);

    // Dùng để dựng `data` của POST/PUT (docs/contracts/tenants.md §2) — KHÔNG loại đơn vị hệ thống,
    // khác SearchAsync: người gọi đã biết id cụ thể, không phải đang liệt kê cho người dùng chọn.
    Task<TenantListItemDto?> FindByIdAsync(Guid id, CancellationToken ct);
}
