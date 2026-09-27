using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Khoá cấu hình của lệnh bootstrap — định nghĩa gốc, docs/database/script-runbook.md §3.3 bước 4.
// NGOẠI LỆ có tên duy nhất (docs/quy-uoc/be-architecture.md §4.4): KHÔNG gắn ValidateOnStart — tiến
// trình API phục vụ thật không đọc nhóm này. Kiểm ở ĐẦU lệnh bootstrap, trước dòng ghi đầu tiên;
// thiếu một giá trị thì lệnh dừng, không ghi dòng nào.
public sealed class CoreBootstrapOptions
{
    public const string SectionName = "Core:Bootstrap";

    [Required(AllowEmptyStrings = false)]
    public string SystemTenantCode { get; init; } = default!;

    [Required(AllowEmptyStrings = false)]
    public string SystemTenantName { get; init; } = default!;

    [Required(AllowEmptyStrings = false)]
    public string FirstTenantCode { get; init; } = default!;

    [Required(AllowEmptyStrings = false)]
    public string FirstTenantName { get; init; } = default!;

    // Hai tên đăng nhập dưới đây là tài khoản lệnh TẠO — không được là danh tính hệ thống (SystemActor).
    [Required(AllowEmptyStrings = false)]
    [NotReservedUserName]
    public string OperatorUserName { get; init; } = default!;

    [Required(AllowEmptyStrings = false)]
    public string OperatorPassword { get; init; } = default!;

    // Email của hai tài khoản lệnh TẠO. Bắt buộc vì Identity của Core bật RequireUniqueEmail (AddCoreIdentity): bộ kiểm
    // người dùng từ chối email rỗng, nên thiếu khoá này thì lệnh chỉ hỏng muộn hơn, ở lúc tạo tài khoản. [EmailAddress] là
    // CHÍNH phép kiểm hình dạng mà bộ kiểm người dùng của Identity chạy — giá trị qua được đây thì không bị trả InvalidEmail.
    // 256 = độ rộng cột core.app_user.email.
    [Required(AllowEmptyStrings = false)]
    [EmailAddress(ErrorMessage = EmailFormatMessage)]
    [MaxLength(EmailMaxLength, ErrorMessage = EmailLengthMessage)]
    public string OperatorEmail { get; init; } = default!;

    [Required(AllowEmptyStrings = false)]
    [NotReservedUserName]
    public string AdminUserName { get; init; } = default!;

    [Required(AllowEmptyStrings = false)]
    public string AdminPassword { get; init; } = default!;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress(ErrorMessage = EmailFormatMessage)]
    [MaxLength(EmailMaxLength, ErrorMessage = EmailLengthMessage)]
    public string AdminEmail { get; init; } = default!;

    private const int EmailMaxLength = 256;
    private const string EmailFormatMessage = "{0}: không phải một địa chỉ email hợp lệ.";
    private const string EmailLengthMessage = "{0}: dài quá {1} ký tự.";
}
