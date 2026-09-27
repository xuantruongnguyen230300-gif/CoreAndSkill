using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class LockUserCommandHandler(
    ICurrentUser currentUser, IUserAdminService userAdmin, UserPrivilegeGuard privilegeGuard)
    : IRequestHandler<LockUserCommand, Result>
{
    public async Task<Result> Handle(LockUserCommand command, CancellationToken ct)
    {
        var callerId = currentUser.UserId
            ?? throw new InvalidOperationException("LockUserCommand chạy khi chưa xác thực.");

        // Luật 3 — không tự khoá chính mình. Kiểm TRƯỚC NotFound: id là chính mình thì chắc chắn
        // tồn tại, không cần đọc DB để biết luật nào áp trước.
        var selfCheck = UserPrivilegeGuard.EnsureNotLockingSelf(callerId, command.UserId);
        if (selfCheck.IsFailure)
            return selfCheck;

        // Luật 4 — chỉ người mang vai trò hệ thống (hoặc cờ bypass) mới khoá được tài khoản mang vai trò hệ thống; đích mang
        // cờ bypass hoặc là tài khoản vận hành thì luôn chặn.
        var systemRoleCheck = await privilegeGuard.EnsureCanLockAsync(callerId, command.UserId, ct);
        if (systemRoleCheck.IsFailure)
            return systemRoleCheck;

        return await userAdmin.LockAsync(command.UserId, command.Version, ct);
    }
}
