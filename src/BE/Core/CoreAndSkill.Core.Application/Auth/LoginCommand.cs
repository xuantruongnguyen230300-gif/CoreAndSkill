using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Auth;

// docs/contracts/auth.md §3. Cài INoTransaction — bộ đếm sai mật khẩu và mốc khoá của Identity
// phải được lưu dù đăng nhập thất bại (docs/adr/0033-luong-dang-nhap-outcome-va-claim.md,
// docs/quy-uoc/be-cqrs-handler.md §5.3).
public sealed record LoginCommand(string TenantCode, string UserName, string Password)
    : ICommand<LoginOutcome>, INoTransaction;
