using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Khoá tài khoản sau N lần sai liên tiếp — định nghĩa gốc của giá trị mặc định,
// docs/wiki-core/be/02-identity-auth.md §4.2. Nạp vào LockoutOptions.MaxFailedAccessAttempts /
// LockoutOptions.DefaultLockoutTimeSpan của Identity — tên khoá KHÔNG khớp tên property gốc, ánh
// xạ thủ công ở AddCoreIdentity (Core.Infrastructure).
public sealed class CoreIdentityLockoutOptions
{
    public const string SectionName = "Core:Identity:Lockout";

    [Range(1, 100)]
    public int MaxFailedAttempts { get; init; } = 5;

    [Range(1, 24 * 60)]
    public int DurationMinutes { get; init; } = 15;
}
