using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Auth;

// docs/contracts/auth.md §6 — tự nguyện đổi mật khẩu, không xoá cờ mustChangePassword.
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<LoginOutcome>;
