using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Users;

// Hiện thực IUserQueryService — chỉ đọc, chiếu thẳng vào DTO (docs/quy-uoc/be-cqrs-handler.md §11.2).
internal sealed class UserQueryService(CoreDbContext db) : IUserQueryService
{
    // Kích thước lô khi xuất — docs/wiki-core/be/15-import-export.md §5.2: đọc theo lô, không nạp toàn bộ.
    private const int StreamBatchSize = 500;

    public async Task<PagedList<UserListItemDto>> SearchAsync(UserSearchCriteria criteria, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var users = FilterAndOrder(criteria, now);

        var totalCount = await users.CountAsync(ct);

        var pageIds = await users
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var items = await LoadDetailsAsync(pageIds, now, ct);

        // Giữ đúng thứ tự đã sắp — LoadDetailsAsync tra theo tập id, không giữ thứ tự.
        //
        // BỎ QUA id không còn dòng, không tra thẳng theo khoá: ba câu đọc ở trên không nằm trong một ảnh chụp, nên một
        // lần xoá mềm của người quản trị khác xen giữa câu lấy id và câu nạp chi tiết làm id đó biến mất. Tra thẳng khi
        // ấy ném KeyNotFoundException, và người đang xem danh sách nhận 500 vì một thao tác hợp lệ của người khác.
        // TotalCount giữ số của câu đếm — nó là ảnh chụp tại thời điểm đếm, không phải số dòng dựng được. Cùng khuôn với
        // StreamAsync bên dưới.
        var byId = items.ToDictionary(i => i.Id);
        var ordered = new List<UserListItemDto>(pageIds.Count);
        foreach (var id in pageIds)
        {
            if (byId.TryGetValue(id, out var item))
                ordered.Add(item);
        }

        return new PagedList<UserListItemDto>
        {
            Items = ordered,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalCount = totalCount,
        };
    }

    // Đếm và duyệt CÙNG một FilterAndOrder với SearchAsync: xuất dữ liệu theo đúng bộ lọc đang xem
    // (docs/contracts/exports.md §1) — hai bên dựng truy vấn ở MỘT chỗ.
    public Task<int> CountAsync(UserSearchCriteria criteria, CancellationToken ct)
        => FilterAndOrder(criteria, DateTimeOffset.UtcNow).CountAsync(ct);

    // Duyệt theo lô bằng Skip/Take trên CÙNG thứ tự sắp xếp của danh sách (tiêu chí phụ ổn định là Id).
    // Phân trang keyset (be-performance.md §6.3) không dùng được ở đây vì thứ tự do người dùng chọn
    // (5 cột, hai chiều) — với trần Core:Export:MaxRows (mặc định 50.000) thì tối đa 100 lô, chấp nhận
    // được; đo lại nếu trần được nâng cao.
    public async IAsyncEnumerable<UserListItemDto> StreamAsync(
        UserSearchCriteria criteria, int maxRows, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var ordered = FilterAndOrder(criteria, now);

        var yielded = 0;
        while (yielded < maxRows)
        {
            var take = Math.Min(StreamBatchSize, maxRows - yielded);
            var ids = await ordered.Skip(yielded).Take(take).Select(u => u.Id).ToListAsync(ct);
            if (ids.Count == 0)
                yield break;

            var byId = (await LoadDetailsAsync(ids, now, ct)).ToDictionary(i => i.Id);
            foreach (var id in ids)
            {
                if (byId.TryGetValue(id, out var item))
                    yield return item;
            }

            yielded += ids.Count;
            if (ids.Count < take)
                yield break;
        }
    }

    // MỘT chỗ dựng bộ lọc + thứ tự sắp xếp cho danh sách, đếm và xuất. Page/PageSize không đọc ở đây.
    private IQueryable<AppUser> FilterAndOrder(UserSearchCriteria criteria, DateTimeOffset now)
    {
        var users = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var pattern = LikePattern.Contains(criteria.SearchText.Trim());
            users = users.Where(u =>
                EF.Functions.ILike(u.UserName!, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(u.FullName, pattern, LikePattern.EscapeCharacter) ||
                (u.Email != null && EF.Functions.ILike(u.Email, pattern, LikePattern.EscapeCharacter)));
        }

        if (criteria.RoleId is { } roleId)
            users = users.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));

        if (criteria.Status == "active")
            users = users.Where(u => u.LockoutEnd == null || u.LockoutEnd <= now);
        else if (criteria.Status == "locked")
            users = users.Where(u => u.LockoutEnd != null && u.LockoutEnd > now);

        users = (criteria.SortBy, criteria.SortDescending) switch
        {
            ("fullName", false) => users.OrderBy(u => u.FullName).ThenBy(u => u.Id),
            ("fullName", true) => users.OrderByDescending(u => u.FullName).ThenBy(u => u.Id),
            ("email", false) => users.OrderBy(u => u.Email).ThenBy(u => u.Id),
            ("email", true) => users.OrderByDescending(u => u.Email).ThenBy(u => u.Id),
            ("createdAt", false) => users.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id),
            ("createdAt", true) => users.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id),
            (_, false) => users.OrderBy(u => u.NormalizedUserName).ThenBy(u => u.Id),
            (_, true) => users.OrderByDescending(u => u.NormalizedUserName).ThenBy(u => u.Id),
        };

        return users;
    }

    public async Task<UserListItemDto?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        var items = await LoadDetailsAsync([id], DateTimeOffset.UtcNow, ct);
        return items.SingleOrDefault();
    }

    private async Task<List<UserListItemDto>> LoadDetailsAsync(IReadOnlyCollection<Guid> ids, DateTimeOffset now, CancellationToken ct)
    {
        if (ids.Count == 0)
            return [];

        var users = await db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FullName,
                IsLocked = u.LockoutEnd != null && u.LockoutEnd > now,
                u.LockoutEnd,
                u.LockedByAdmin,
                u.MustChangePassword,
                u.CreatedAt,
                Version = u.ConcurrencyStamp,
            })
            .ToListAsync(ct);

        var roleRows = await (
            from ur in db.UserRoles
            where ids.Contains(ur.UserId)
            join r in db.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, RoleId = r.Id, RoleName = r.Name!, r.IsSystem })
            .ToListAsync(ct);

        var rolesByUser = roleRows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<UserRoleSummaryDto>)g
                .Select(r => new UserRoleSummaryDto(r.RoleId, r.RoleName, r.IsSystem))
                .ToList());

        return users.Select(u => new UserListItemDto(
            u.Id,
            u.UserName!,
            u.Email,
            u.FullName,
            rolesByUser.TryGetValue(u.Id, out var roles) ? roles : [],
            u.IsLocked,
            u.LockoutEnd,
            u.LockedByAdmin,
            u.MustChangePassword,
            u.CreatedAt,
            u.Version ?? string.Empty)).ToList();
    }
}
