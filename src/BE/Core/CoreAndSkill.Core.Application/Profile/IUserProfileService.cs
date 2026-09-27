using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Profile;

// Seam của hồ sơ cá nhân — tách khỏi IUserLookupService (Identity/) vì đây là các trường chỉ chính
// chủ đọc/sửa (PhoneNumber, HasPermissionBypass, token đồng thời), không phải DTO dùng cho luồng
// xác thực. Hiện thực ở Core.Infrastructure. docs/contracts/profile.md.
public sealed record ProfileDto(
    string UserName,
    string? Email,
    string FullName,
    string? PhoneNumber,
    string? PreferredLanguage,
    bool HasPermissionBypass,
    string Version);

// Version nullable — "thiếu" là một trạng thái đầu vào hợp lệ, không phải lỗi biên dịch: card
// docs/contracts/profile.md §2 đòi "thiếu hoặc lệch ⇒ 409, vì null không bao giờ khớp".
public sealed record UpdateProfileInput(string FullName, string? PhoneNumber, string? PreferredLanguage, string? Version);

public interface IUserProfileService
{
    Task<ProfileDto?> GetAsync(Guid userId, CancellationToken ct);

    Task<Result<ProfileDto>> UpdateAsync(Guid userId, UpdateProfileInput input, CancellationToken ct);

    Task<Result> RenouncePermissionBypassAsync(Guid userId, CancellationToken ct);
}
