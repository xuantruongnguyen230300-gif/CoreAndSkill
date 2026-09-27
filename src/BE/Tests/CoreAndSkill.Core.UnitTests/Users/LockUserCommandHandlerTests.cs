using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class LockUserCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserAdminService _userAdmin = Substitute.For<IUserAdminService>();
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly Guid _callerId = Guid.NewGuid();

    public LockUserCommandHandlerTests() => _currentUser.UserId.Returns(_callerId);

    private LockUserCommandHandler CreateHandler()
        => new(_currentUser, _userAdmin, new UserPrivilegeGuard(_permissionChecker, _roleQuery, _userLookup));

    [Fact]
    public async Task Handle_LockingSelf_ReturnsBusinessRuleError()
    {
        var command = new LockUserCommand(_callerId, "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.CannotLockSelf.Code);
        await _userAdmin.DidNotReceiveWithAnyArgs().LockAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_TargetHoldsSystemRole_CallerDoesNot_ReturnsForbidden()
    {
        var targetId = Guid.NewGuid();
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(_callerId, Arg.Any<CancellationToken>()).Returns(false);
        var command = new LockUserCommand(targetId, "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SystemRoleLockForbidden.Code);
        await _userAdmin.DidNotReceiveWithAnyArgs().LockAsync(default, default, default);
    }

    // Kịch bản lỗ luật 4: A (bypass, không giữ vai trò nào) cấp cho B một vai trò {core.user.read, core.user.lock}; B khoá A.
    [Fact]
    public async Task Handle_TargetHasPermissionBypass_CallerHoldsNeitherSystemRoleNorBypass_ReturnsForbidden()
    {
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _userLookup.HasPermissionBypassAsync(_callerId, Arg.Any<CancellationToken>()).Returns(false);
        _roleQuery.UserHoldsAnySystemRoleAsync(_callerId, Arg.Any<CancellationToken>()).Returns(false);
        _userAdmin.LockAsync(targetId, "v1", Arg.Any<CancellationToken>()).Returns(Result.Success());
        var command = new LockUserCommand(targetId, "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SystemRoleLockForbidden.Code);
        await _userAdmin.DidNotReceiveWithAnyArgs().LockAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_BothHavePermissionBypass_ReturnsForbidden()
    {
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _userLookup.HasPermissionBypassAsync(_callerId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _userAdmin.LockAsync(targetId, "v1", Arg.Any<CancellationToken>()).Returns(Result.Success());
        var command = new LockUserCommand(targetId, "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SystemRoleLockForbidden.Code);
        await _userAdmin.DidNotReceiveWithAnyArgs().LockAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_CallerHasPermissionBypass_TargetHoldsSystemRole_CallsLockAsync()
    {
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        _userLookup.HasPermissionBypassAsync(_callerId, Arg.Any<CancellationToken>()).Returns(true);
        _roleQuery.UserHoldsAnySystemRoleAsync(_callerId, Arg.Any<CancellationToken>()).Returns(false);
        _userAdmin.LockAsync(targetId, "v1", Arg.Any<CancellationToken>()).Returns(Result.Success());
        var command = new LockUserCommand(targetId, "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _userAdmin.Received(1).LockAsync(targetId, "v1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_CallsLockAsync()
    {
        var targetId = Guid.NewGuid();
        _roleQuery.UserHoldsAnySystemRoleAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _userAdmin.LockAsync(targetId, "v1", Arg.Any<CancellationToken>()).Returns(Result.Success());
        var command = new LockUserCommand(targetId, "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _userAdmin.Received(1).LockAsync(targetId, "v1", Arg.Any<CancellationToken>());
    }
}
