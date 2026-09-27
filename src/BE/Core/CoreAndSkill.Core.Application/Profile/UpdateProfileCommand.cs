using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Profile;

// PUT /api/v1/core/profile — docs/contracts/profile.md §2. Thay thế TOÀN PHẦN: trường vắng mặt
// đọc như null. Chuẩn hoá "" → null xảy ra ở Core.Web (UpdateProfileRequest), TRƯỚC khi vào pipeline
// validate — đúng thứ tự "chuẩn hoá trước khi kiểm" mà card yêu cầu.
public sealed record UpdateProfileCommand(
    string FullName,
    string? PhoneNumber,
    string? PreferredLanguage,
    string? Version) : ICommand<ProfileDto>;
