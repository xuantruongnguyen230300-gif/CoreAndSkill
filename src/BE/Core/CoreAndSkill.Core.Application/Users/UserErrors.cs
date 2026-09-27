using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Users;

// Catalog lỗi — docs/contracts/users.md.
public static class UserErrors
{
    public static readonly Error NotFound = new(
        "CORE.USER.NOT_FOUND", "Không tìm thấy người dùng.", ErrorType.NotFound);

    public static readonly Error UsernameDuplicated = new(
        "CORE.USER.USERNAME_DUPLICATED", "Tên đăng nhập '{UserName}' đã tồn tại.", ErrorType.Conflict);

    public static readonly Error EmailDuplicated = new(
        "CORE.USER.EMAIL_DUPLICATED", "Email '{Email}' đã được dùng.", ErrorType.Conflict);

    // CHỈ xuất hiện ở fieldErrors[<ô tên đăng nhập>][].code, dưới mã gốc CORE.VALIDATION.FAILED — tên đăng nhập trùng danh
    // tính hệ thống (SystemActor). Không messageParams.
    public static readonly Error UsernameReserved = new(
        "CORE.USER.USERNAME_RESERVED", "Tên đăng nhập này được hệ thống giữ riêng. Chọn tên khác.", ErrorType.Validation);

    public static readonly Error RoleNotFound = new(
        "CORE.USER.ROLE_NOT_FOUND", "Vai trò '{RoleId}' không tồn tại.", ErrorType.BusinessRule);

    // Luật 1 §2 — cả hai chiều gán và gỡ.
    public static readonly Error RoleEscalationForbidden = new(
        "CORE.USER.ROLE_ESCALATION_FORBIDDEN",
        "Không thao tác trên vai trò cấp quyền mà bạn không có.", ErrorType.Forbidden);

    // Luật 2 §2.
    public static readonly Error SelfSystemRoleRemovalForbidden = new(
        "CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN",
        "Không tự gỡ vai trò hệ thống của chính mình.", ErrorType.BusinessRule);

    // Luật 3 §2 — 422, không 403: người gọi CÓ quyền core.user.lock.
    public static readonly Error CannotLockSelf = new(
        "CORE.USER.CANNOT_LOCK_SELF", "Không tự khoá tài khoản của chính mình.", ErrorType.BusinessRule);

    // Luật 4 §2 — mọi vế dùng CHUNG một mã, và câu KHÔNG nêu lý do (ADR-0085): người mang vai trò hệ thống khoá tài khoản
    // mang cờ bypass cũng nhận mã này, nên một câu nêu điều kiện "vai trò hệ thống" sẽ nói sai về chính người đọc nó.
    // Cùng khuôn ResetPasswordTargetForbidden.
    public static readonly Error SystemRoleLockForbidden = new(
        "CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN",
        "Không đủ quyền khoá tài khoản này.", ErrorType.Forbidden);

    public static readonly Error DuplicateRoleEntry = new(
        "CORE.USER.DUPLICATE_ROLE_ENTRY", "Danh sách vai trò có mục trùng lặp.", ErrorType.Validation);

    // Luật 5 §2 — ba vế dùng CHUNG một mã (cố ý — card không cho biết đích mang cờ bypass hay cờ vận hành).
    public static readonly Error ResetPasswordTargetForbidden = new(
        "CORE.USER.RESET_PASSWORD_TARGET_FORBIDDEN",
        "Không đủ quyền đặt lại mật khẩu cho tài khoản này.", ErrorType.Forbidden);

    public static readonly Error CannotResetOwnPassword = new(
        "CORE.USER.CANNOT_RESET_OWN_PASSWORD",
        "Dùng chức năng tự đổi mật khẩu thay vì đặt lại cho chính mình.", ErrorType.BusinessRule);

    // Identity từ chối (mật khẩu tạm không đạt chính sách…) — lý do ở fieldErrors.
    public static readonly Error CreateFailed = new(
        "CORE.USER.CREATE_FAILED", "Tạo người dùng thất bại.", ErrorType.BusinessRule);

    public static readonly Error UpdateFailed = new(
        "CORE.USER.UPDATE_FAILED", "Cập nhật người dùng thất bại.", ErrorType.BusinessRule);

    public static readonly Error LockFailed = new(
        "CORE.USER.LOCK_FAILED", "Thao tác khoá/mở khoá thất bại.", ErrorType.BusinessRule);

    public static readonly Error ResetPasswordFailed = new(
        "CORE.USER.RESET_PASSWORD_FAILED", "Đặt lại mật khẩu thất bại.", ErrorType.BusinessRule);
}
