using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Ánh xạ mã Identity thô → FieldError, theo MÃ chứ không theo endpoint — docs/contracts/auth.md §6 "Ghi chú". Chỗ DUY
// NHẤT dịch IdentityError trước khi rời ranh giới Identity (docs/wiki-core/be/02-identity-auth.md §2.3): đổi mật khẩu,
// tạo/đặt lại mật khẩu người dùng, tạo quản trị đơn vị đều đi qua đây, nên cùng một lỗi ra cùng mã VÀ cùng tham số.
//
// Mã lấy từ catalog (AuthErrors, UserErrors, CommonErrors) — không literal (luật R2). Tham số chính sách mật khẩu lấy từ
// CHÍNH chính sách mà UserManager đã áp (UserManager.Options.Password), không từ một bản đọc cấu hình thứ hai. Tham số
// trùng tên đăng nhập / trùng email lấy từ CHÍNH tài khoản mà UserManager vừa từ chối — giá trị bộ kiểm Identity đã so,
// khoá theo card (docs/contracts/users.md §5: UserName, Email), cùng khoá với phép kiểm trước của handler.
// IdentityError không mang giá trị đó ở dạng máy đọc được (chỉ có câu Description), nên chỗ gọi PHẢI đưa tài khoản vào.
//
// Hàm chỉ nói lỗi THUỘC ô nào theo nghĩa (tên đăng nhập, email, mật khẩu hiện tại, mật khẩu mới); tên khoá fieldErrors
// cụ thể (NewPassword, TempPassword, AdminTempPassword…) là việc của chỗ gọi, theo card của endpoint đó.
internal static class IdentityErrorMapper
{
    internal enum Subject
    {
        UserName,
        Email,
        CurrentPassword,
        NewPassword,
        Unattributed,
    }

    // rejectedUser: tài khoản vừa đưa vào UserManager. null CHỈ khi chỗ gọi không bao giờ lộ lỗi tên đăng nhập/email
    // (fieldFor trả null cho hai Subject đó) — nếu không, câu hiển thị ra "{{UserName}}" nguyên chữ.
    public static (Subject Subject, FieldError Error) Map(IdentityError error, AppUser? rejectedUser, PasswordOptions policy) =>
        error.Code switch
        {
            "DuplicateUserName" => (Subject.UserName,
                ToFieldError(UserErrors.UsernameDuplicated.WithParams(("UserName", rejectedUser?.UserName)))),
            "InvalidUserName" => (Subject.UserName, ToFieldError(CommonErrors.Format)),
            "DuplicateEmail" => (Subject.Email,
                ToFieldError(UserErrors.EmailDuplicated.WithParams(("Email", rejectedUser?.Email)))),
            "InvalidEmail" => (Subject.Email, ToFieldError(CommonErrors.Format)),
            "PasswordMismatch" => (Subject.CurrentPassword, ToFieldError(AuthErrors.PasswordMismatch)),
            "PasswordTooShort" => (Subject.NewPassword,
                ToFieldError(AuthErrors.PasswordTooShort.WithParams(("MinLength", policy.RequiredLength)))),
            "PasswordRequiresDigit" => (Subject.NewPassword, ToFieldError(AuthErrors.PasswordRequiresDigit)),
            "PasswordRequiresLower" => (Subject.NewPassword, ToFieldError(AuthErrors.PasswordRequiresLower)),
            "PasswordRequiresUpper" => (Subject.NewPassword, ToFieldError(AuthErrors.PasswordRequiresUpper)),
            "PasswordRequiresNonAlphanumeric" => (Subject.NewPassword, ToFieldError(AuthErrors.PasswordRequiresNonAlphanumeric)),
            "PasswordRequiresUniqueChars" => (Subject.NewPassword,
                ToFieldError(AuthErrors.PasswordRequiresUniqueChars.WithParams(("MinUniqueChars", policy.RequiredUniqueChars)))),
            _ => (Subject.Unattributed, ToFieldError(AuthErrors.PasswordPolicyViolation)),
        };

    // Gom theo khoá fieldErrors mà chỗ gọi chọn cho từng Subject; fieldFor trả null ⇒ lỗi đó KHÔNG lộ ra (chống dò tên
    // đăng nhập — docs/contracts/tenants.md §4, §6).
    public static IReadOnlyDictionary<string, IReadOnlyList<FieldError>> ToFieldErrors(
        IEnumerable<IdentityError> errors, AppUser? rejectedUser, PasswordOptions policy, Func<Subject, string?> fieldFor)
    {
        var byField = new Dictionary<string, List<FieldError>>(StringComparer.Ordinal);

        foreach (var error in errors)
        {
            var (subject, mapped) = Map(error, rejectedUser, policy);
            if (fieldFor(subject) is not { } field)
                continue;

            var list = byField.TryGetValue(field, out var existing) ? existing : byField[field] = [];
            list.Add(mapped);
        }

        return byField.ToDictionary(kv => kv.Key, IReadOnlyList<FieldError> (kv) => kv.Value, StringComparer.Ordinal);
    }

    private static FieldError ToFieldError(Error error) => new(error.Code, error.Params);
}
