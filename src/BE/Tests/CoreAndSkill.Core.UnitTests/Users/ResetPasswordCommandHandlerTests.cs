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

public class ResetPasswordCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserAdminService _userAdmin = Substitute.For<IUserAdminService>();
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly Guid _callerId = Guid.NewGuid();

    public ResetPasswordCommandHandlerTests() => _currentUser.UserId.Returns(_callerId);

    private ResetPasswordCommandHandler CreateHandler()
        => new(_currentUser, _userAdmin, new UserPrivilegeGuard(_permissionChecker, _roleQuery, _userLookup));

    [Fact]
    public async Task Handle_ResettingOwnPassword_ReturnsBusinessRuleError()
    {
        var command = new ResetPasswordCommand(_callerId, "Temp@123", "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.CannotResetOwnPassword.Code);
        await _userAdmin.DidNotReceiveWithAnyArgs().ResetPasswordAsync(default, default!, default, default);
    }

    [Fact]
    public async Task Handle_TargetHasPermissionBypass_ReturnsForbidden()
    {
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(true);
        var command = new ResetPasswordCommand(targetId, "Temp@123", "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.ResetPasswordTargetForbidden.Code);
    }

    [Fact]
    public async Task Handle_Success_CallsResetPasswordAsync()
    {
        var targetId = Guid.NewGuid();
        _userLookup.HasPermissionBypassAsync(targetId, Arg.Any<CancellationToken>()).Returns(false);
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.reset-password" });
        _permissionChecker.GetEffectivePermissionsAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        _userAdmin.ResetPasswordAsync(targetId, "Temp@123", "v1", Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var command = new ResetPasswordCommand(targetId, "Temp@123", "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _userAdmin.Received(1).ResetPasswordAsync(targetId, "Temp@123", "v1", Arg.Any<CancellationToken>());
    }
}
