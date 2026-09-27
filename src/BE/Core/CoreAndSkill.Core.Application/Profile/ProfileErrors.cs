using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Profile;

// docs/contracts/profile.md §3.
public static class ProfileErrors
{
    public static readonly Error PermissionBypassNotHeld = new(
        "CORE.PROFILE.PERMISSION_BYPASS_NOT_HELD", "Tài khoản không mang cờ bỏ qua kiểm quyền.", ErrorType.BusinessRule);

    public static readonly Error NoOtherPermissionAdmin = new(
        "CORE.PROFILE.NO_OTHER_PERMISSION_ADMIN",
        "Đơn vị chưa có tài khoản nào khác giữ quyền phân quyền qua vai trò.", ErrorType.BusinessRule);
}
