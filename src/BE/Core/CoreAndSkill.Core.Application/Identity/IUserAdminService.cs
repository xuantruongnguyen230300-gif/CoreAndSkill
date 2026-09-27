using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Identity;

// Hành động quản trị người dùng — docs/quy-uoc/be-entity-domain.md §7.1, docs/contracts/users.md.
// Giữ AppUser/UserManager không rời Core.Infrastructure. Handler kiểm luật nghiệp vụ (trùng tên,
// leo thang đặc quyền…) TRƯỚC khi gọi seam này — seam chỉ làm đúng thao tác ghi đã được duyệt.
public sealed record CreateUserInput(string UserName, string Email, string FullName, string TempPassword);

public sealed record UpdateUserInput(string Email, string FullName, string? Version);

public interface IUserAdminService
{
    Task<Result<Guid>> CreateAsync(CreateUserInput input, CancellationToken ct);

    Task<Result> UpdateAsync(Guid userId, UpdateUserInput input, CancellationToken ct);

    // version: token nhận từ GET gần nhất của bản ghi ĐÍCH — 06-concurrency-control.md §6.3. Áp cho
    // CẢ hai thao tác — docs/contracts/users.md §8.
    Task<Result> LockAsync(Guid userId, string? version, CancellationToken ct);

    Task<Result> UnlockAsync(Guid userId, string? version, CancellationToken ct);

    Task<Result> ResetPasswordAsync(Guid userId, string tempPassword, string? version, CancellationToken ct);

    // Ngữ nghĩa THAY THẾ toàn bộ — docs/contracts/users.md §7. Luật nghiệp vụ (escalation, tự gỡ vai
    // trò hệ thống của chính mình) đã được duyệt Ở HANDLER trước khi gọi tới đây.
    //
    // version: token của TÀI KHOẢN đích, cùng token với UpdateAsync/LockAsync/ResetPasswordAsync —
    // docs/adr/0082-gan-vai-tro-dung-token-cua-tai-khoan.md. So và đổi TRƯỚC mọi thay đổi vai trò, kể cả
    // khi tập đích trùng tập hiện có; lệch hoặc null ⇒ CORE.CONCURRENCY.CONFLICT, không ghi gì.
    //
    // Vai trò được THÊM được kiểm lại dưới khoá dòng, trong cùng transaction, trước khi ghi: vai trò đã bị xoá (kể cả bị
    // xoá SAU phép kiểm của handler) ⇒ CORE.USER.ROLE_NOT_FOUND — nợ E17 ở docs/DEBT.md.
    Task<Result> AssignRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, string? version, CancellationToken ct);

    // Vai trò ban đầu của tài khoản VỪA tạo bằng CreateAsync trong cùng đơn vị công việc — docs/contracts/users.md §5.
    // Không token: chưa ai ngoài transaction này nhìn thấy tài khoản, nên không có bản chụp nào để lệch. Chỉ nhận
    // tài khoản do CreateAsync của cùng instance (Scoped) tạo — gọi cho một tài khoản đã có, kể cả một tài khoản vừa được
    // đọc trong phạm vi này, là lỗi lập trình (ném), không phải đường vòng qua phép so token của AssignRolesAsync.
    // Vai trò kiểm lại dưới khoá dòng như AssignRolesAsync — vai trò đã bị xoá ⇒ CORE.USER.ROLE_NOT_FOUND.
    Task<Result> GrantInitialRolesAsync(Guid newUserId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct);
}
