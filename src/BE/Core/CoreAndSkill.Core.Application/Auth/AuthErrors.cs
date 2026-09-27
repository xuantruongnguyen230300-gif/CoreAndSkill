using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Auth;

// Catalog lỗi của đăng nhập/đổi mật khẩu — docs/contracts/auth.md §3, §6, §7, §11.
public static class AuthErrors
{
    // Bốn ca gộp một mã, cố ý — docs/contracts/auth.md §3 "Ghi chú".
    public static readonly Error InvalidCredentials = new(
        "CORE.AUTH.INVALID_CREDENTIALS", "Sai thông tin đăng nhập.", ErrorType.BusinessRule);

    public static readonly Error LockedOut = new(
        "CORE.AUTH.LOCKED_OUT", "Tài khoản đang bị khoá.", ErrorType.BusinessRule);

    public static readonly Error ChangePasswordFailed = new(
        "CORE.AUTH.CHANGE_PASSWORD_FAILED", "Đổi mật khẩu thất bại.", ErrorType.BusinessRule);

    public static readonly Error PasswordChangeNotRequired = new(
        "CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED", "Tài khoản không ở trạng thái bắt buộc đổi mật khẩu.", ErrorType.BusinessRule);

    // Chỉ xuất hiện ở fieldErrors["NewPassword"][].code — docs/contracts/auth.md §6.
    public static readonly Error NewPasswordSameAsCurrent = new(
        "CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT", "Mật khẩu mới không được trùng mật khẩu hiện tại.", ErrorType.Validation);

    // Ánh xạ mã Identity thô → mã catalog, theo MÃ chứ không theo endpoint — docs/contracts/auth.md §6
    // "Ghi chú". Infrastructure dịch IdentityError.Code sang các mã dưới trước khi rời ranh giới
    // Identity (docs/wiki-core/be/02-identity-auth.md §2.3).
    public static readonly Error PasswordMismatch = new(
        "CORE.AUTH.PASSWORD_MISMATCH", "Mật khẩu hiện tại không đúng.", ErrorType.Validation);

    public static readonly Error PasswordTooShort = new(
        "CORE.AUTH.PASSWORD_TOO_SHORT", "Mật khẩu phải có ít nhất {MinLength} ký tự.", ErrorType.Validation);

    public static readonly Error PasswordRequiresDigit = new(
        "CORE.AUTH.PASSWORD_REQUIRES_DIGIT", "Mật khẩu phải có ít nhất một chữ số.", ErrorType.Validation);

    public static readonly Error PasswordRequiresLower = new(
        "CORE.AUTH.PASSWORD_REQUIRES_LOWER", "Mật khẩu phải có ít nhất một chữ thường.", ErrorType.Validation);

    public static readonly Error PasswordRequiresUpper = new(
        "CORE.AUTH.PASSWORD_REQUIRES_UPPER", "Mật khẩu phải có ít nhất một chữ hoa.", ErrorType.Validation);

    public static readonly Error PasswordRequiresNonAlphanumeric = new(
        "CORE.AUTH.PASSWORD_REQUIRES_NON_ALPHANUMERIC", "Mật khẩu phải có ít nhất một ký tự đặc biệt.", ErrorType.Validation);

    public static readonly Error PasswordRequiresUniqueChars = new(
        "CORE.AUTH.PASSWORD_REQUIRES_UNIQUE_CHARS", "Mật khẩu phải có ít nhất {MinUniqueChars} ký tự khác nhau.", ErrorType.Validation);

    // Dự phòng — mã Identity không rơi vào nhóm trên.
    public static readonly Error PasswordPolicyViolation = new(
        "CORE.AUTH.PASSWORD_POLICY_VIOLATION", "Mật khẩu không đạt chính sách.", ErrorType.Validation);
}
