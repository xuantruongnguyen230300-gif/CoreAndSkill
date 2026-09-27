using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class ResetPasswordCommandHandler(
    ICurrentUser currentUser, IUserAdminService userAdmin, UserPrivilegeGuard privilegeGuard)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var callerId = currentUser.UserId
            ?? throw new InvalidOperationException("ResetPasswordCommand chạy khi chưa xác thực.");

        // Luật 5 — không nhắm vào tài khoản "cao hơn" người gọi, không nhắm vào chính mình.
        var privilegeCheck = await privilegeGuard.EnsureCanResetPasswordAsync(callerId, command.UserId, ct);
        if (privilegeCheck.IsFailure)
            return privilegeCheck;

        return await userAdmin.ResetPasswordAsync(command.UserId, command.TempPassword, command.Version, ct);
    }
}
