using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Tài khoản đăng nhập — docs/database/schema-core.md §4.1. AppUser/AppRole chỉ sống ở
// Core.Infrastructure — luật S5, docs/quy-uoc/be-entity-domain.md §7.1. KHÔNG kế thừa BaseEntity —
// Identity tự quản vòng đời bằng field riêng (lockout_end thay cho is_active); implement trực tiếp
// IAuditableEntity để AuditInterceptor vẫn điền bốn cột audit (§1.4).
//
// Field "Ta thêm" mang `private set` và đổi qua method tên nghiệp vụ (be-entity-domain.md §1.4 bảng luật, §2). EF Core
// vật liệu hoá qua chính setter riêng đó; UserStore của Identity không ghi field nào ở đây — không field nào cần setter
// công khai. Field kế thừa từ IdentityUser<Guid> nằm ngoài ranh giới.
public class AppUser : IdentityUser<Guid>, ITenantScoped, IAuditableEntity
{
    public Guid TenantId { get; init; }

    // Bốn field của IAuditableEntity — public setter có chủ đích, §1.4.
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // "Ta thêm" — docs/database/schema-core.md §4.1.
    public string FullName { get; private set; } = string.Empty;
    public bool MustChangePassword { get; private set; } = true;
    public string? PreferredLanguage { get; private set; }

    // Cờ vào khu quản trị hệ thống — docs/adr/0017-khu-quan-tri-he-thong.md. Không đi qua ma trận
    // quyền, và luật M9 cấm gán vai trò nghiệp vụ cho tài khoản mang cờ này.
    public bool IsSystemOperator { get; private set; }

    // Cờ của tài khoản quản trị đầu tiên của MỘT đơn vị: bỏ qua kiểm permission, KHÔNG bỏ qua bộ
    // lọc đơn vị — docs/adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md. Hai cờ này LOẠI TRỪ nhau
    // (luật M11, ràng buộc kiểm tra ở database).
    public bool HasPermissionBypass { get; private set; }

    // true khi khoá do quản trị đặt tay; khoá tự động sau nhiều lần sai không đặt cờ này.
    public bool LockedByAdmin { get; private set; }

    // Ctor không tham số còn công khai: EF vật liệu hoá bằng nó, và tài khoản "thăm dò" dùng để chạy PasswordValidators /
    // băm giả cũng dựng bằng nó (IdentityService, TenantProvisioningService). Tài khoản THẬT dựng bằng NewAccount.
    public AppUser()
    {
        Id = EntityId.New();
    }

    // Chỗ DUY NHẤT gán hai cờ đặc quyền — luật M12: cờ chỉ sinh CÙNG LÚC với tài khoản mang nó, không bật trên tài khoản đã
    // có. Mọi tài khoản mới đều phải đổi mật khẩu ở lần đăng nhập đầu.
    public static AppUser NewAccount(
        string userName, string? email, string fullName, bool hasPermissionBypass = false, bool isSystemOperator = false)
        => new()
        {
            UserName = userName,
            Email = email,
            FullName = fullName,
            MustChangePassword = true,
            HasPermissionBypass = hasPermissionBypass,
            IsSystemOperator = isSystemOperator,
        };

    public void ChangeFullName(string fullName) => FullName = fullName;

    public void UpdateProfile(string fullName, string? preferredLanguage)
    {
        FullName = fullName;
        PreferredLanguage = preferredLanguage;
    }

    public void RequirePasswordChange() => MustChangePassword = true;

    public void ClearPasswordChangeRequirement() => MustChangePassword = false;

    // Chiều duy nhất cờ bypass được đổi sau khi tạo: TỪ BỎ — docs/contracts/profile.md §3. Không có method bật lại.
    public void RenouncePermissionBypass() => HasPermissionBypass = false;

    public void MarkLockedByAdmin() => LockedByAdmin = true;

    public void ClearAdminLock() => LockedByAdmin = false;
}
