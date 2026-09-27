using System.Globalization;
using System.Security.Claims;
using CoreAndSkill.Core.Application.Auth;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Hiện thực ISessionPrincipalFactory — KHÔNG truy vấn (docs/adr/0033-luong-dang-nhap-outcome-va-claim.md).
// Chỗ DUY NHẤT Identity lõi và cookie scheme gặp nhau — docs/adr/0026-ranh-gioi-identity-va-cookie.md.
// KHÔNG dùng CookieAuthenticationDefaults/IdentityConstants ở đây: tên scheme là khái niệm của cookie
// scheme (Core.Web), Infrastructure không được biết nó tồn tại (ranh giới của chính ADR-0026).
internal sealed class SessionPrincipalFactory : ISessionPrincipalFactory
{
    private const string AuthenticationType = "CoreAndSkillSession";

    public ClaimsPrincipal Create(LoginOutcome outcome, DateTimeOffset issuedAt)
    {
        var session = outcome.Session;

        var claims = new List<Claim>
        {
            new(CoreClaimTypes.UserId, session.Id.ToString()),
            new(CoreClaimTypes.UserName, session.UserName),
            new(CoreClaimTypes.SecurityStamp, outcome.SecurityStamp),
            new(CoreClaimTypes.IssuedAt, issuedAt.ToString("O", CultureInfo.InvariantCulture)),
            new(CoreClaimTypes.MustChangePassword, session.MustChangePassword.ToString(CultureInfo.InvariantCulture)),
            new(CoreClaimTypes.IsSystemOperator, session.IsSystemOperator.ToString(CultureInfo.InvariantCulture)),
        };

        // TenantId nạp vào phiếu từ LoginOutcome.TenantId — KHÔNG từ đâu khác (luật M2). SessionDto
        // chỉ mang TenantCode/TenantName để HIỂN THỊ (contracts/auth.md §3); Guid thật nằm ở
        // LoginOutcome, không lộ ra response.
        claims.Add(new Claim(CoreClaimTypes.TenantId, outcome.TenantId.ToString()));

        var identity = new ClaimsIdentity(claims, AuthenticationType);
        return new ClaimsPrincipal(identity);
    }
}
