using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Users;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

// docs/contracts/users.md §2 — năm luật bảo vệ tài khoản quản trị.
public class UserPrivilegeGuardTests
{
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();

    private UserPrivilegeGuard CreateGuard() => new(_permissionChecker, _roleQuery, _userLookup);

    // Luật 1
    [Fact]
    public async Task RoleEscalationCheck_CallerHasAllRolePermissions_Succeeds()
    {
        var callerId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _permissionChecker.GetEffectivePermissionsAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read", "core.user.write" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]> { [roleId] = ["core.user.read"] });

        var check = await CreateGuard().LoadRoleEscalationCheckAsync(callerId, [roleId], CancellationToken.None);

        check.Check(roleId).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task RoleEscalationCheck_CallerMissingRolePermission_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _permissionChecker.GetEffectivePermissionsAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read" });
        _permissionChecker.StubRolePermissions(
            new Dictionary<Guid, string[]> { [roleId] = ["core.user.read", "core.permission.write"] });

        var check = await CreateGuard().LoadRoleEscalationCheckAsync(callerId, [roleId], CancellationToken.None);

        var result = check.Check(roleId);
        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.RoleEscalationForbidden.Code);
    }

    // Đóng, không mở: một vai trò không nằm trong dữ liệu đã nạp (chỗ gọi quên đưa nó vào) bị từ chối, không được cho qua.
    [Fact]
    public async Task RoleEscalationCheck_RoleNotLoaded_IsRejected()
    {
        var callerId = Guid.NewGuid();
        _permissionChecker.GetEffectivePermissionsAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]>());

        var check = await CreateGuard().LoadRoleEscalationCheckAsync(callerId, [Guid.NewGuid()], CancellationToken.None);

        check.Check(Guid.NewGuid()).IsFailure.ShouldBeTrue();
    }

    // Luật 2
    [Fact]
    public void EnsureNotRemovingOwnSystemRole_SelfAndSystemRole_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();

        var result = UserPrivilegeGuard.EnsureNotRemovingOwnSystemRole(userId, userId, roleIsSystem: true);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SelfSystemRoleRemovalForbidden.Code);
    }

    [Fact]
    public void EnsureNotRemovingOwnSystemRole_SelfButNotSystemRole_Succeeds()
    {
        var userId = Guid.NewGuid();

        var result = UserPrivilegeGuard.EnsureNotRemovingOwnSystemRole(userId, userId, roleIsSystem: false);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void EnsureNotRemovingOwnSystemRole_DifferentUser_Succeeds()
    {
        var result = UserPrivilegeGuard.EnsureNotRemovingOwnSystemRole(Guid.NewGuid(), Guid.NewGuid(), roleIsSystem: true);

        result.IsSuccess.ShouldBeTrue();
    }

    // Luật 3
    [Fact]
    public void EnsureNotLockingSelf_Self_ReturnsBusinessRuleError()
    {
        var userId = Guid.NewGuid();

        var result = UserPrivilegeGuard.EnsureNotLockingSelf(userId, userId);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.CannotLockSelf.Code);
    }

    [Fact]
    public void EnsureNotLockingSelf_DifferentUser_Succeeds()
    {
        var result = UserPrivilegeGuard.EnsureNotLockingSelf(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.ShouldBeTrue();
    }

    // Luật 4
    [Fact]
    public async Task EnsureCanLockAsync_TargetNotSystemRoleHolder_Succeeds()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateGuard().EnsureCanLockAsync(callerId, targetId, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _roleQuery.DidNotReceive().UserHoldsAnySystemRoleAsync(callerId, Arg.Any<CancellationToken>());
        await _userLookup.DidNotReceive().HasPermissionBypassAsync(callerId, Arg.Any<CancellationToken>());
    }

    // Quản trị đơn vị mặc định mang cờ bypass và KHÔNG giữ vai trò nào — thiếu vế "đích mang cờ bypass" thì vế vai trò hệ
    // thống cho mọi người gọi qua. Kịch bản: A (bypass) tạo vai trò {core.user.read, core.user.lock}, gán cho B; B đọc
    // version của A rồi khoá A. B không mang vai trò hệ thống, không mang cờ.
    [Fact]
    public async Task EnsureCanLockAsync_TargetHasPermissionBypass_CallerHoldsNeitherSystemRoleNorBypass_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _userLookup.HasPermissionBypassAsync(callerId, Arg.Any<CancellationToken>()).Returns(false);
        _roleQuery.UserHoldsAnySystemRoleAsync(callerId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateGuard().EnsureCanLockAsync(callerId, targetId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SystemRoleLockForbidden.Code);
    }

    // Đích mang cờ bypass bị chặn BẤT KỂ người gọi — kể cả người gọi giữ vai trò hệ thống.
    [Fact]
    public async Task EnsureCanLockAsync_TargetHasPermissionBypass_EvenIfCallerHoldsSystemRole_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _roleQuery.UserHoldsAnySystemRoleAsync(callerId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateGuard().EnsureCanLockAsync(callerId, targetId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SystemRoleLockForbidden.Code);
    }

    // Hai người cùng mang cờ: vế người gọi không cứu được, vì đích mang cờ luôn bị chặn.
    [Fact]
    public async Task EnsureCanLockAsync_BothHavePermissionBypass_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _userLookup.HasPermissionBypassAsync(callerId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _roleQuery.UserHoldsAnySystemRoleAsync(callerId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateGuard().EnsureCanLockAsync(callerId, targetId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SystemRoleLockForbidden.Code);
    }

    // Người gọi mang cờ bypass được tính như mang vai trò hệ thống: quản trị đơn vị (không giữ vai trò nào) khoá được tài
    // khoản giữ vai trò is_system.
    [Fact]
    public async Task EnsureCanLockAsync_CallerHasPermissionBypass_TargetHoldsSystemRole_Succeeds()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _userLookup.HasPermissionBypassAsync(callerId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(callerId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateGuard().EnsureCanLockAsync(callerId, targetId, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task EnsureCanLockAsync_TargetIsSystemRoleHolder_CallerIsNot_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(callerId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateGuard().EnsureCanLockAsync(callerId, targetId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SystemRoleLockForbidden.Code);
    }

    [Fact]
    public async Task EnsureCanLockAsync_BothHoldSystemRole_Succeeds()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(callerId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateGuard().EnsureCanLockAsync(callerId, targetId, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    // Luật 5
    [Fact]
    public async Task EnsureCanResetPasswordAsync_Self_ReturnsBusinessRuleError()
    {
        var userId = Guid.NewGuid();

        var result = await CreateGuard().EnsureCanResetPasswordAsync(userId, userId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.CannotResetOwnPassword.Code);
    }

    [Fact]
    public async Task EnsureCanResetPasswordAsync_TargetHasPermissionBypass_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateGuard().EnsureCanResetPasswordAsync(callerId, targetId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.ResetPasswordTargetForbidden.Code);
    }

    [Fact]
    public async Task EnsureCanResetPasswordAsync_TargetHasMorePermissionsThanCaller_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _permissionChecker.GetEffectivePermissionsAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read" });
        _permissionChecker.GetEffectivePermissionsAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read", "core.permission.write" });

        var result = await CreateGuard().EnsureCanResetPasswordAsync(callerId, targetId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.ResetPasswordTargetForbidden.Code);
    }

    [Fact]
    public async Task EnsureCanResetPasswordAsync_TargetIsSubsetOfCaller_Succeeds()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _permissionChecker.GetEffectivePermissionsAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read", "core.user.reset-password" });
        _permissionChecker.GetEffectivePermissionsAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read" });

        var result = await CreateGuard().EnsureCanResetPasswordAsync(callerId, targetId, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    // Tài khoản vận hành hệ thống — docs/contracts/tenants.md §4 "Ghi chú": khôi phục nó KHÔNG đi qua HTTP.
    // Tập quyền của nó RỖNG (cờ vận hành không phải một quyền trong ma trận), nên vế "tập quyền đích là tập con"
    // của luật 5 cho MỌI người gọi qua — kể cả một tài khoản mang cờ bypass lọt vào đơn vị hệ thống.
    [Fact]
    public async Task EnsureCanResetPasswordAsync_TargetIsSystemOperator_WithEmptyPermissions_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        ArrangeSystemOperator(targetId);
        _permissionChecker.GetEffectivePermissionsAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read", "core.user.reset-password", "core.permission.write" });
        _permissionChecker.GetEffectivePermissionsAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        var result = await CreateGuard().EnsureCanResetPasswordAsync(callerId, targetId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.ResetPasswordTargetForbidden.Code);
    }

    // Cùng lý do cho khoá: tài khoản vận hành không giữ vai trò is_system nào, nên luật 4 nguyên bản cho qua.
    [Fact]
    public async Task EnsureCanLockAsync_TargetIsSystemOperator_EvenIfCallerHoldsSystemRole_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        ArrangeSystemOperator(targetId);
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _roleQuery.UserHoldsAnySystemRoleAsync(callerId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateGuard().EnsureCanLockAsync(callerId, targetId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SystemRoleLockForbidden.Code);
    }

    private void ArrangeSystemOperator(Guid userId)
        => _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserSummaryDto(userId, "superadmin", "Vận hành", null, IsLocked: false,
                IsSystemOperator: true, MustChangePassword: false, PreferredLanguage: null));
}
