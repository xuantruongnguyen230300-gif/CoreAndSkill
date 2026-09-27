using CoreAndSkill.Core.Application.Common.Paging;

namespace CoreAndSkill.Core.Application.Users;

// Seam CHỈ ĐỌC cho danh sách/chi tiết người dùng — docs/contracts/users.md §3, §4. Tách khỏi
// IUserLookupService (Identity/) vì đó phục vụ luồng xác thực (UserSummaryDto), còn đây phục vụ
// màn quản trị (phân trang, roles đầy đủ, version). Hiện thực ở Core.Infrastructure.
public sealed record UserRoleSummaryDto(Guid Id, string Name, bool IsSystem);

public sealed record UserListItemDto(
    Guid Id,
    string UserName,
    string? Email,
    string FullName,
    IReadOnlyList<UserRoleSummaryDto> Roles,
    bool IsLocked,
    DateTimeOffset? LockoutEnd,
    bool LockedByAdmin,
    bool MustChangePassword,
    DateTimeOffset? CreatedAt,
    string Version);

public sealed record UserSearchCriteria(
    int Page,
    int PageSize,
    string SortBy,
    bool SortDescending,
    string? SearchText,
    Guid? RoleId,
    string? Status);

public interface IUserQueryService
{
    Task<PagedList<UserListItemDto>> SearchAsync(UserSearchCriteria criteria, CancellationToken ct);

    Task<UserListItemDto?> FindByIdAsync(Guid id, CancellationToken ct);

    // Ba thao tác dưới đây dùng CÙNG MỘT bộ lọc với SearchAsync (Page/PageSize bị bỏ qua). Xuất dữ liệu
    // (docs/contracts/exports.md §1) và màn danh sách dựng truy vấn ở MỘT chỗ — nếu không, hai bên sẽ
    // lệch và tệp xuất không khớp thứ người dùng đang nhìn.
    Task<int> CountAsync(UserSearchCriteria criteria, CancellationToken ct);

    // Đọc theo LÔ, giữ thứ tự sắp xếp của danh sách, tối đa maxRows dòng — không nạp toàn bộ kết quả.
    IAsyncEnumerable<UserListItemDto> StreamAsync(UserSearchCriteria criteria, int maxRows, CancellationToken ct);
}
