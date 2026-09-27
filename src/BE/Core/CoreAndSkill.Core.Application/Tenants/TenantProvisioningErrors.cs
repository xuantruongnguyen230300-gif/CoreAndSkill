using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Tenants;

// Lỗi của ITenantProvisioningService — dùng cho lệnh vận hành (bootstrap) VÀ card
// docs/contracts/tenants.md (B3+). Mã khớp ĐÚNG card — đổi mã ở đây là breaking change trên dây.
public static class TenantProvisioningErrors
{
    public static readonly Error NotFound = new(
        "CORE.TENANT.NOT_FOUND", "Không tìm thấy đơn vị.", ErrorType.NotFound);

    public static readonly Error CodeDuplicate = new(
        "CORE.TENANT.CODE_DUPLICATE", "Mã đơn vị '{Code}' đã tồn tại.", ErrorType.Conflict);

    // docs/contracts/tenants.md §2, §6 — CÙNG một mã cho cả hai endpoint: Identity từ chối tạo tài
    // khoản quản trị (đầu tiên hoặc bổ sung). §6 gộp MỌI ca thành một mã (không phân biệt lý do liên
    // quan userName) — cố ý, chống dò tên đăng nhập trong đơn vị.
    public static readonly Error AdminCreateFailed = new(
        "CORE.TENANT.ADMIN_CREATE_FAILED", "Không tạo được tài khoản quản trị cho đơn vị.", ErrorType.BusinessRule);

    public static readonly Error SeedFailed = new(
        "CORE.TENANT.SEED_FAILED", "Không seed được dữ liệu mặc định cho đơn vị.", ErrorType.BusinessRule);

    public static readonly Error RecoveryTargetNotEligible = new(
        "CORE.TENANT.RECOVERY_TARGET_NOT_ELIGIBLE",
        "Không có tài khoản đủ điều kiện khôi phục mang tên đăng nhập đã cho trong đơn vị này.",
        ErrorType.BusinessRule);

    public static readonly Error RecoveryResetFailed = new(
        "CORE.TENANT.RECOVERY_RESET_FAILED", "Không đặt lại được mật khẩu của tài khoản đích.", ErrorType.BusinessRule);

    public static readonly Error OperatorNotFound = new(
        "CORE.TENANT.OPERATOR_NOT_FOUND",
        "Không tìm thấy tài khoản vận hành mang tên đăng nhập đã cho, thuộc đơn vị hệ thống.", ErrorType.BusinessRule);

    public static readonly Error OperatorPasswordResetFailed = new(
        "CORE.TENANT.OPERATOR_PASSWORD_RESET_FAILED",
        "Không đặt lại được mật khẩu tài khoản vận hành.", ErrorType.BusinessRule);
}
