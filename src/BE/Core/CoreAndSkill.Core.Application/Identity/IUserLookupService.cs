namespace CoreAndSkill.Core.Application.Identity;

// Chỉ đọc — docs/quy-uoc/be-entity-domain.md §7.1.
public interface IUserLookupService
{
    Task<UserSummaryDto?> FindByIdAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<UserSummaryDto>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<bool> UserNameExistsAsync(string userName, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct);

    // Cờ has_permission_bypass của MỘT tài khoản — docs/contracts/users.md §2 Luật 4 và Luật 5. Tách khỏi
    // UserSummaryDto để không phải sửa mọi nơi dựng DTO đó (SessionDtoFactory…) chỉ vì một luật
    // riêng của tính năng users.
    Task<bool> HasPermissionBypassAsync(Guid userId, CancellationToken ct);
}

// Đủ trường để handler `me` dựng SessionDto mà không chạm AppUser — be-api-controller.md §7.4.
public sealed record UserSummaryDto(
    Guid Id,
    string UserName,
    string FullName,
    string? Email,
    bool IsLocked,
    bool IsSystemOperator,
    bool MustChangePassword,
    string? PreferredLanguage);
