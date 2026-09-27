using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Identity;

// Thông tin đăng nhập, mật khẩu, hiệu lực phiên — KHÔNG phát phiên (đó là việc của Core.Web,
// docs/adr/0026-ranh-gioi-identity-va-cookie.md). docs/quy-uoc/be-entity-domain.md §7.1.
public interface IIdentityService
{
    // Mật khẩu trước, khoá sau — docs/wiki-core/be/02-identity-auth.md §4.2.
    Task<Result<CredentialCheck>> CheckCredentialsAsync(string userName, string password, CancellationToken ct);

    // Một phép kiểm mật khẩu GIẢ, cùng giá như phép thật — cho nhánh đăng nhập trượt TRƯỚC khi có tài khoản để
    // kiểm (đơn vị không có / ngưng hoạt động). Thiếu nó, thời gian phản hồi lộ ra đơn vị nào có thật
    // (docs/wiki-core/be/09-security-beyond-auth.md bảng "Đường rò", docs/contracts/auth.md §3).
    Task SimulateCredentialCheckAsync(string password, CancellationToken ct);

    // stamp MỚI sau khi đổi — controller cấp lại cookie từ nó. clearMustChangePassword: true ở
    // endpoint bắt buộc đổi (contracts/auth.md §7), false ở tự nguyện (§6).
    Task<Result<CredentialCheck>> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, bool clearMustChangePassword, CancellationToken ct);

    // Gọi ở mọi request; false khi tài khoản không còn HOẶC stamp lệch. KHÔNG biết gì về đơn vị: trạng thái đơn vị là phép
    // kiểm thứ hai, riêng, qua ITenantLookup.FindByIdAsync — be-api-controller.md §7.4 (bảng hai phép kiểm). Chỗ nào mới
    // gọi seam này để quyết một phiên còn dùng được thì phải tự gọi phép 2, nếu không phiên của đơn vị đã ngưng vẫn đi qua.
    Task<bool> IsSessionValidAsync(Guid userId, string securityStamp, CancellationToken ct);
}

// Nguồn DUY NHẤT của SecurityStamp cho LoginOutcome — handler không tra stamp ở đâu khác.
public sealed record CredentialCheck(Guid UserId, string SecurityStamp, bool MustChangePassword);
