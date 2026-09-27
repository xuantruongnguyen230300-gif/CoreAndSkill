using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Application.Auth;

// Gộp lại đúng MỘT chỗ dựng SessionDto — login, me, và hai endpoint đổi mật khẩu đều đi qua đây
// (docs/quy-uoc/be-api-controller.md §7.4: "SessionDto ghép từ các seam, không chạm AppUser").
// Không phải một seam (không interface) — chỉ là composition nội bộ Application, không hiện thực
// ở tầng khác.
internal sealed class SessionDtoFactory(
    IUserLookupService userLookup,
    IPermissionChecker permissionChecker,
    IOptions<CoreAuthOptions> authOptions)
{
    public async Task<SessionDto> BuildAsync(
        UserSummaryDto userSummary, TenantSummary tenant, bool mustChangePassword, CancellationToken ct)
    {
        var roles = await userLookup.GetRoleNamesAsync(userSummary.Id, ct);
        var permissions = await permissionChecker.GetEffectivePermissionsAsync(userSummary.Id, ct);

        return new SessionDto(
            userSummary.Id,
            userSummary.UserName,
            userSummary.Email,
            userSummary.FullName,
            roles,
            permissions.ToList(),
            mustChangePassword,
            userSummary.IsSystemOperator,
            authOptions.Value.SessionMinutes,
            userSummary.PreferredLanguage,
            tenant.Code,
            tenant.Name);
    }
}
