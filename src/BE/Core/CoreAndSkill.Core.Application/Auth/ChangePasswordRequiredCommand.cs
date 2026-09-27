using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Auth;

// docs/contracts/auth.md §7 — đường thoát duy nhất khỏi trạng thái bắt buộc đổi mật khẩu.
public sealed record ChangePasswordRequiredCommand(string CurrentPassword, string NewPassword) : ICommand<LoginOutcome>;
