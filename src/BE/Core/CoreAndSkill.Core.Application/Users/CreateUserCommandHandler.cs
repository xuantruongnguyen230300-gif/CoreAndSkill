using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Users;

// docs/contracts/users.md §5. Tạo tài khoản và gán vai trò trong MỘT transaction — cả hai bước
// chạy trong handler của MỘT command, TransactionBehavior bọc toàn bộ (be-cqrs-handler.md §5.3).
internal sealed class CreateUserCommandHandler(
    ICurrentUser currentUser,
    IUserLookupService userLookup,
    IUserAdminService userAdmin,
    IRoleQueryService roleQuery,
    IPermissionChecker permissionChecker,
    UserPrivilegeGuard privilegeGuard)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        var callerId = currentUser.UserId
            ?? throw new InvalidOperationException("CreateUserCommand chạy khi chưa xác thực.");

        // roleIds trùng là 400 CORE.USER.DUPLICATE_ROLE_ENTRY, đứng ĐẦU mọi phép kiểm của handler — cùng luật, cùng vị trí với
        // PUT /users/{id}/roles (§7). Không lọc trùng lặng lẽ: payload trùng là lỗi của client, và hai đường mang cùng một
        // danh sách vai trò phải trả cùng một câu trả lời.
        var duplicateCheck = UserRoleAssignmentRules.EnsureNoDuplicateRoleIds(command.RoleIds);
        if (duplicateCheck.IsFailure)
            return Result.Failure<Guid>(duplicateCheck.Error!);

        var roleIds = command.RoleIds;

        // Quyền core.user.role.assign chỉ cần khi roleIds khác rỗng — điều kiện phụ thuộc dữ liệu,
        // [RequirePermission] tĩnh không biểu diễn được (docs/contracts/users.md §5). Tập quyền của người gọi đọc MỘT lần,
        // dùng lại cho luật 1 bên dưới.
        IReadOnlySet<string>? callerPermissions = null;
        if (roleIds.Count > 0)
        {
            callerPermissions = await permissionChecker.GetEffectivePermissionsAsync(callerId, ct);
            if (!callerPermissions.Contains(CorePermissions.UserRoleAssign))
                return Result.Failure<Guid>(CommonErrors.Forbidden);
        }

        if (await userLookup.UserNameExistsAsync(command.UserName, ct))
            return Result.Failure<Guid>(UserErrors.UsernameDuplicated.WithParams(("UserName", command.UserName)));

        if (await userLookup.EmailExistsAsync(command.Email, ct))
            return Result.Failure<Guid>(UserErrors.EmailDuplicated.WithParams(("Email", command.Email)));

        if (callerPermissions is not null)
        {
            // Mọi vai trò tra bằng MỘT câu theo tập; kiểm từng vai trò theo thứ tự payload — vai trò đầu tiên không tồn tại
            // hoặc vượt quyền người gọi quyết định mã lỗi, như khi kiểm lần lượt. Phép kiểm này chạy ngoài khoá; seam kiểm lại
            // dưới khoá dòng trước khi ghi (IUserAdminService.GrantInitialRolesAsync, nợ E17).
            var existing = await roleQuery.FindExistingIdsAsync(roleIds, ct);
            var escalation = await privilegeGuard.LoadRoleEscalationCheckAsync(callerPermissions, roleIds, ct);

            foreach (var roleId in roleIds)
            {
                if (!existing.Contains(roleId))
                    return Result.Failure<Guid>(UserErrors.RoleNotFound.WithParams(("RoleId", roleId)));

                var escalationCheck = escalation.Check(roleId);
                if (escalationCheck.IsFailure)
                    return Result.Failure<Guid>(escalationCheck.Error!);
            }
        }

        var createResult = await userAdmin.CreateAsync(
            new CreateUserInput(command.UserName, command.Email, command.FullName, command.TempPassword), ct);
        if (createResult.IsFailure)
            return createResult;

        if (command.RoleIds.Count > 0)
        {
            // AddToRolesAsync trả IdentityResult — bỏ qua giá trị trả về là thất bại im lặng
            // (docs/contracts/users.md §5 "Ghi chú"); seam trả Result nên không có giá trị nào để bỏ qua. Tài khoản vừa tạo
            // trong transaction này nên không có token nào để so (ADR-0082 chỉ áp cho tài khoản đã có).
            var assignResult = await userAdmin.GrantInitialRolesAsync(createResult.Value, command.RoleIds, ct);
            if (assignResult.IsFailure)
                return Result.Failure<Guid>(assignResult.Error!);
        }

        return createResult;
    }
}
