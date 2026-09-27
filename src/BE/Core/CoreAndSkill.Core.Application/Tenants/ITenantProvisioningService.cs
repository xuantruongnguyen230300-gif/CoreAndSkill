namespace CoreAndSkill.Core.Application.Tenants;

using CoreAndSkill.Core.Domain.Common;

// Service tạo đơn vị DÙNG CHUNG — docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md.
// Lệnh bootstrap (Core.Web runner) và endpoint tạo đơn vị của khu quản trị hệ thống (B3+) gọi
// CÙNG một hiện thực. Mỗi lời gọi chạy trong MỘT transaction: đơn vị, tài khoản quản trị đầu tiên
// và seed (vai trò/ánh xạ quyền/menu qua ITenantSeedSource) cùng commit, hoặc không dòng nào được
// ghi. Chạy lại với cùng CreateTenantInput không nhân đôi — nhận ra đơn vị đã có theo IsSystem
// (nhiều nhất một đơn vị hệ thống) hoặc theo Code, và tài khoản đã có theo tên đăng nhập.
public sealed record CreateTenantInput(
    string Code,
    string Name,
    bool IsSystem,
    string AdminUserName,
    string AdminPassword,
    bool AdminHasPermissionBypass,
    bool AdminIsSystemOperator,
    // Bắt buộc, không mặc định: Identity của Core bật RequireUniqueEmail (AddCoreIdentity), nên bộ kiểm người dùng từ chối
    // tài khoản không email — lời gọi thiếu email luôn thất bại với CORE.TENANT.ADMIN_CREATE_FAILED. Endpoint tạo đơn vị
    // (docs/contracts/tenants.md §2) truyền adminEmail của request; core bootstrap truyền Core:Bootstrap:OperatorEmail /
    // Core:Bootstrap:AdminEmail (CoreBootstrapOptions).
    string AdminEmail,
    // false (mặc định) = idempotent kiểu bootstrap: đơn vị đã có thì BỎ QUA, không lỗi.
    // true = kiểu endpoint POST /system/tenants (docs/contracts/tenants.md §2): đơn vị đã có (Code
    // trùng) là một CONFLICT tường minh — CORE.TENANT.CODE_DUPLICATE — KHÔNG âm thầm coi là thành công.
    bool FailIfExists = false,
    // null (mặc định, dùng bởi core bootstrap): FullName = AdminUserName. Endpoint tạo đơn vị (§2) truyền họ tên của request.
    string? AdminFullName = null);

public sealed record TenantProvisioningResult(Guid TenantId, Guid AdminUserId, bool TenantWasCreated, bool AdminWasCreated);

public interface ITenantProvisioningService
{
    Task<Result<TenantProvisioningResult>> CreateTenantAsync(CreateTenantInput input, CancellationToken ct);

    // Đường DUY NHẤT đặt lại mật khẩu tài khoản vận hành — docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md §5.
    // Chỉ nhận tài khoản mang is_system_operator, thuộc đơn vị hệ thống; không thấy → dừng, không ghi gì.
    Task<Result> ResetOperatorPasswordAsync(string operatorUserName, string newPassword, CancellationToken ct);

    // Ngưng / bật lại một đơn vị nghiệp vụ — docs/contracts/tenants.md §3,
    // docs/adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md §5. Thao tác XUYÊN ĐƠN VỊ: ghi
    // hai dòng nhật ký (§4 của ADR đó).
    Task<Result> SetTenantActiveAsync(Guid tenantId, bool isActive, CancellationToken ct);

    // Khôi phục "cửa quản trị" của một đơn vị — docs/contracts/tenants.md §4, ADR-0029 §3. Chỉ nhận
    // đích thuộc đơn vị tenantId, mang has_permission_bypass HOẶC giữ một vai trò is_system của
    // CHÍNH đơn vị đó.
    Task<Result> RecoveryResetAdminPasswordAsync(
        Guid tenantId, string userName, string tempPassword, CancellationToken ct);

    // Lối cuối khi đơn vị không còn đích đủ điều kiện cho RecoveryResetAdminPasswordAsync —
    // docs/contracts/tenants.md §6, ADR-0029 §3 phương án D. CHỈ tạo mới — không sửa tài khoản đã có
    // (luật M12).
    Task<Result<Guid>> CreateAdditionalAdminAsync(
        Guid tenantId, string userName, string email, string fullName, string tempPassword, CancellationToken ct);
}
