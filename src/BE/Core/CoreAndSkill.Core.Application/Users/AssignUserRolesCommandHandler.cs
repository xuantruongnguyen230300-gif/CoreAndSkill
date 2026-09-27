using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class AssignUserRolesCommandHandler(
    ICurrentUser currentUser,
    IUserQueryService userQuery,
    IRoleQueryService roleQuery,
    IUserAdminService userAdmin,
    UserPrivilegeGuard privilegeGuard)
    : IRequestHandler<AssignUserRolesCommand, Result>
{
    public async Task<Result> Handle(AssignUserRolesCommand command, CancellationToken ct)
    {
        var callerId = currentUser.UserId
            ?? throw new InvalidOperationException("AssignUserRolesCommand chạy khi chưa xác thực.");

        // Trùng lặp phần tử là 400 — chặn TRƯỚC khi chạm dữ liệu (docs/contracts/users.md §7).
        var duplicateCheck = UserRoleAssignmentRules.EnsureNoDuplicateRoleIds(command.RoleIds);
        if (duplicateCheck.IsFailure)
            return duplicateCheck;

        var target = await userQuery.FindByIdAsync(command.UserId, ct);
        if (target is null)
            return Result.Failure(UserErrors.NotFound);

        // Token lệch đi TRƯỚC mọi luật bên dưới (docs/contracts/users.md §7, ADR-0082). Các luật chạy trên tập vai trò HIỆN
        // TẠI; người gọi cầm token cũ thì tập đó khác tập họ đã nhìn, và "removed" có thể chứa một vai trò người khác vừa gán
        // mà họ chưa từng thấy — trả 403/422 cho vai trò đó làm nhánh 409 "tải lại" của FE không bao giờ chạy. null không bao
        // giờ khớp; so ordinal, cùng phép so IdentityConcurrency.VersionMatches.
        //
        // Đây KHÔNG thay phép so-và-đổi ở seam: lần đọc này và câu UPDATE của seam không nguyên tử, nên một lượt ghi chen giữa
        // chỉ lộ ra ở seam — handler vẫn giao command.Version xuống nguyên vẹn.
        if (command.Version is null || !string.Equals(command.Version, target.Version, StringComparison.Ordinal))
            return Result.Failure(CommonErrors.ConcurrencyConflict);

        var currentRoleIds = target.Roles.Select(r => r.Id).ToHashSet();
        var newRoleIds = command.RoleIds.ToHashSet();

        var added = newRoleIds.Except(currentRoleIds).ToList();
        var removed = target.Roles.Where(r => !newRoleIds.Contains(r.Id)).ToList();

        // Không đổi vai trò nào vẫn đi xuống seam: phép so-và-đổi chốt cuối nằm ở đó (docs/contracts/users.md §7 ràng buộc 3).
        if (added.Count == 0 && removed.Count == 0)
            return await userAdmin.AssignRolesAsync(command.UserId, command.RoleIds, command.Version, ct);

        // Mọi vai trò bị chạm tra bằng MỘT câu theo tập (tồn tại; tập quyền), tập quyền người gọi MỘT lần — số truy vấn
        // không tăng theo số vai trò. Kiểm từng vai trò theo đúng thứ tự cũ: thêm trước, gỡ sau.
        //
        // Phép kiểm tồn tại ở đây chạy NGOÀI khoá — nó quyết thứ tự mã lỗi với luật 1, không chốt gì. Vai trò bị xoá sau câu
        // này bị seam bắt lại dưới khoá dòng, cùng mã ROLE_NOT_FOUND (IUserAdminService.AssignRolesAsync, nợ E17).
        var existing = added.Count == 0 ? new HashSet<Guid>() : await roleQuery.FindExistingIdsAsync(added, ct);
        var escalation = await privilegeGuard.LoadRoleEscalationCheckAsync(
            callerId, [.. added, .. removed.Select(r => r.Id)], ct);

        foreach (var roleId in added)
        {
            if (!existing.Contains(roleId))
                return Result.Failure(UserErrors.RoleNotFound.WithParams(("RoleId", roleId)));

            // Luật 1 — áp cho CẢ vai trò được thêm và vai trò bị gỡ (docs/contracts/users.md §2).
            var addCheck = escalation.Check(roleId);
            if (addCheck.IsFailure)
                return addCheck;
        }

        foreach (var role in removed)
        {
            var removeCheck = escalation.Check(role.Id);
            if (removeCheck.IsFailure)
                return removeCheck;

            // Luật 2 — không tự gỡ vai trò hệ thống của chính mình.
            var selfSystemCheck = UserPrivilegeGuard.EnsureNotRemovingOwnSystemRole(callerId, command.UserId, role.IsSystem);
            if (selfSystemCheck.IsFailure)
                return selfSystemCheck;
        }

        return await userAdmin.AssignRolesAsync(command.UserId, command.RoleIds, command.Version, ct);
    }
}
