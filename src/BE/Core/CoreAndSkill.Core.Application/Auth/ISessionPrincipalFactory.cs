using System.Security.Claims;

namespace CoreAndSkill.Core.Application.Auth;

// Seam dựng principal và kiểu của phiên — định nghĩa gốc, docs/quy-uoc/be-api-controller.md §7.4.
// Factory KHÔNG truy vấn (docs/adr/0033-luong-dang-nhap-outcome-va-claim.md) — chỉ dựng
// ClaimsPrincipal thuần từ LoginOutcome đã có sẵn. Hiện thực ở Core.Infrastructure — chỗ DUY NHẤT
// Identity lõi và cookie scheme gặp nhau (docs/adr/0026-ranh-gioi-identity-va-cookie.md).
public interface ISessionPrincipalFactory
{
    ClaimsPrincipal Create(LoginOutcome outcome, DateTimeOffset issuedAt);
}

// Tên loại claim khai MỘT LẦN ở đây — mọi nơi khác trỏ về, không viết tay chuỗi claim thứ hai.
public static class CoreClaimTypes
{
    public const string UserId = "core.user_id";
    public const string UserName = "core.user_name";
    public const string TenantId = "core.tenant_id";
    public const string SecurityStamp = "core.security_stamp";
    public const string IssuedAt = "core.issued_at"; // UTC
    public const string MustChangePassword = "core.must_change_password";
    public const string IsSystemOperator = "core.is_system_operator";
}

// TenantId ở đây là dữ liệu NỘI BỘ để factory dựng claim — KHÔNG lộ ra response (SessionDto không
// có field này; contracts/auth.md chỉ khai tenantCode/tenantName để hiển thị, luật M2).
public sealed record LoginOutcome(SessionDto Session, string SecurityStamp, Guid TenantId);

// Ý nghĩa từng trường: docs/contracts/auth.md §3, §5.
public sealed record SessionDto(
    Guid Id,
    string UserName,
    string? Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool MustChangePassword,
    bool IsSystemOperator,
    int SessionMinutes,
    string? PreferredLanguage,
    string TenantCode,
    string TenantName);
