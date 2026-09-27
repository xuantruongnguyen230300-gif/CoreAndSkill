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

public class CreateUserCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly IUserAdminService _userAdmin = Substitute.For<IUserAdminService>();
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();
    private readonly Guid _callerId = Guid.NewGuid();

    public CreateUserCommandHandlerTests() => _currentUser.UserId.Returns(_callerId);

    private CreateUserCommandHandler CreateHandler()
        => new(_currentUser, _userLookup, _userAdmin, _roleQuery,
            _permissionChecker, new UserPrivilegeGuard(_permissionChecker, _roleQuery, _userLookup));

    // roleIds trùng là 400 DUPLICATE_ROLE_ENTRY — cùng luật với PUT /users/{id}/roles, không lọc trùng lặng lẽ. Đứng ĐẦU: kể
    // cả người gọi thiếu core.user.role.assign cũng nhận 400, không 403; không phép kiểm nào khác chạy, không tài khoản nào
    // được tạo. Người gọi ở đây CÓ đủ quyền và mọi phép kiểm khác đều qua — trên bản lọc trùng lặng lẽ, ca này tạo tài khoản.
    [Fact]
    public async Task Handle_DuplicateRoleIds_ReturnsDuplicateRoleEntry_BeforeAnyOtherCheck_AndCreatesNothing()
    {
        var roleId = Guid.NewGuid();
        _roleQuery.StubRolesExist(id => id == roleId);
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { CorePermissions.UserRoleAssign, "core.user.read" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]> { [roleId] = ["core.user.read"] });
        _userAdmin.CreateAsync(Arg.Any<CreateUserInput>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        _userAdmin.GrantInitialRolesAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var command = new CreateUserCommand("binh.tv", "binh@vd.vn", "Trần Văn Bình", "Temp@123", [roleId, roleId]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.DuplicateRoleEntry.Code);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        await _userAdmin.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _userAdmin.DidNotReceiveWithAnyArgs().GrantInitialRolesAsync(default, default!, default);
        _permissionChecker.ReceivedCalls().ShouldBeEmpty();
        _userLookup.ReceivedCalls().ShouldBeEmpty();
        _roleQuery.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_RoleIdsNonEmpty_CallerMissingRoleAssignPermission_ReturnsForbidden()
    {
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.write" });
        var command = new CreateUserCommand("binh.tv", "binh@vd.vn", "Trần Văn Bình", "Temp@123", [Guid.NewGuid()]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("CORE.AUTH.FORBIDDEN");
        await _userAdmin.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_UserNameAlreadyExists_ReturnsConflict()
    {
        _userLookup.UserNameExistsAsync("binh.tv", Arg.Any<CancellationToken>()).Returns(true);
        var command = new CreateUserCommand("binh.tv", "binh@vd.vn", "Trần Văn Bình", "Temp@123", []);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.UsernameDuplicated.Code);
    }

    [Fact]
    public async Task Handle_RoleEscalation_ReturnsForbidden()
    {
        var roleId = Guid.NewGuid();
        _roleQuery.StubRolesExist(id => id == roleId);
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { CorePermissions.UserRoleAssign, "core.user.read" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]> { [roleId] = ["core.permission.write"] });
        var command = new CreateUserCommand("binh.tv", "binh@vd.vn", "Trần Văn Bình", "Temp@123", [roleId]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.RoleEscalationForbidden.Code);
        await _userAdmin.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Success_CreatesUserAndAssignsRoles()
    {
        var roleId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        _roleQuery.StubRolesExist(id => id == roleId);
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { CorePermissions.UserRoleAssign, "core.user.read" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]> { [roleId] = ["core.user.read"] });
        _userAdmin.CreateAsync(Arg.Any<CreateUserInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(newUserId));
        _userAdmin.GrantInitialRolesAsync(newUserId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var command = new CreateUserCommand("binh.tv", "binh@vd.vn", "Trần Văn Bình", "Temp@123", [roleId]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(newUserId);
        await _userAdmin.Received(1).GrantInitialRolesAsync(newUserId, Arg.Is<IReadOnlyCollection<Guid>>(r => r.Contains(roleId)), Arg.Any<CancellationToken>());
        // Tài khoản vừa tạo không đi qua phép so token của §7 (ADR-0082) — không có bản chụp nào để lệch.
        await _userAdmin.DidNotReceiveWithAnyArgs().AssignRolesAsync(default, default!, default, default);
    }

    // Số lần hỏi seam KHÔNG được tăng theo số vai trò trong payload: tập quyền của người gọi tính MỘT lần (dùng cho cả kiểm
    // core.user.role.assign lẫn luật 1), mọi vai trò tra bằng MỘT câu theo tập. Mỗi lời gọi seam ở đây là ít nhất một
    // câu SQL ở bản thật — số câu SQL đếm ở PermissionSetQueryCountTests (IntegrationTests).
    [Fact]
    public async Task Handle_ManyRoles_AsksEachSeamAConstantNumberOfTimes()
    {
        var roleIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var newUserId = Guid.NewGuid();
        var granted = new HashSet<string> { CorePermissions.UserRoleAssign, "core.user.read" };
        _permissionChecker.HasPermissionAsync(_callerId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => granted.Contains(ci.ArgAt<string>(1)));
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>()).Returns(granted);
        _permissionChecker.StubRolePermissions(roleIds.ToDictionary(id => id, _ => new[] { "core.user.read" }));
        _roleQuery.StubRolesExist(_ => true);
        _userAdmin.CreateAsync(Arg.Any<CreateUserInput>(), Arg.Any<CancellationToken>()).Returns(Result.Success(newUserId));
        _userAdmin.GrantInitialRolesAsync(newUserId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateHandler().Handle(
            new CreateUserCommand("binh.tv", "binh@vd.vn", "Trần Văn Bình", "Temp@123", roleIds), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        SeamCallCounter.CallerPermissionLookups(_permissionChecker).ShouldBe(1);
        _permissionChecker.ReceivedCalls().Count().ShouldBeLessThanOrEqualTo(2);
        _roleQuery.ReceivedCalls().Count().ShouldBe(1);
    }
}
