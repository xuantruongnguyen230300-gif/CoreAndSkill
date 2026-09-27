using CoreAndSkill.Core.Application.Common;
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

public class AssignUserRolesCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserQueryService _userQuery = Substitute.For<IUserQueryService>();
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();
    private readonly IUserAdminService _userAdmin = Substitute.For<IUserAdminService>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly Guid _callerId = Guid.NewGuid();

    public AssignUserRolesCommandHandlerTests() => _currentUser.UserId.Returns(_callerId);

    private AssignUserRolesCommandHandler CreateHandler()
        => new(_currentUser, _userQuery, _roleQuery, _userAdmin,
            new UserPrivilegeGuard(_permissionChecker, _roleQuery, _userLookup));

    [Fact]
    public async Task Handle_DuplicateRoleIds_ReturnsValidationError()
    {
        var roleId = Guid.NewGuid();
        var command = new AssignUserRolesCommand(Guid.NewGuid(), [roleId, roleId], "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.DuplicateRoleEntry.Code);
    }

    [Fact]
    public async Task Handle_TargetNotFound_ReturnsNotFound()
    {
        var targetId = Guid.NewGuid();
        _userQuery.FindByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns((UserListItemDto?)null);
        var command = new AssignUserRolesCommand(targetId, [], "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task Handle_AddingRoleCallerCannotGrant_ReturnsRoleEscalationForbidden()
    {
        var targetId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _userQuery.FindByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new UserListItemDto(targetId, "an.nv", "an@vd.vn", "An", [], false, null, false, false, null, "v1"));
        _roleQuery.StubRolesExist(id => id == roleId);
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]> { [roleId] = ["core.permission.write"] });
        var command = new AssignUserRolesCommand(targetId, [roleId], "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.RoleEscalationForbidden.Code);
    }

    [Fact]
    public async Task Handle_RemovingOwnSystemRole_ReturnsSelfSystemRoleRemovalForbidden()
    {
        var currentRoleId = Guid.NewGuid();
        _userQuery.FindByIdAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new UserListItemDto(
                _callerId, "an.nv", "an@vd.vn", "An",
                [new UserRoleSummaryDto(currentRoleId, "Quản trị hệ thống", true)],
                false, null, false, false, null, "v1"));
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.permission.write" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]> { [currentRoleId] = ["core.permission.write"] });
        var command = new AssignUserRolesCommand(_callerId, [], "v1"); // gỡ sạch — kể cả vai trò hệ thống của chính mình

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.SelfSystemRoleRemovalForbidden.Code);
    }

    [Fact]
    public async Task Handle_Success_CallsAssignRoles()
    {
        var targetId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _userQuery.FindByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new UserListItemDto(targetId, "an.nv", "an@vd.vn", "An", [], false, null, false, false, null, "v1"));
        _roleQuery.StubRolesExist(id => id == roleId);
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]> { [roleId] = ["core.user.read"] });
        _userAdmin.AssignRolesAsync(targetId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var command = new AssignUserRolesCommand(targetId, [roleId], "v1");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        // Token của TÀI KHOẢN đích đi nguyên xuống seam — phép so nằm ở đó (docs/adr/0082-gan-vai-tro-dung-token-cua-tai-khoan.md).
        await _userAdmin.Received(1).AssignRolesAsync(targetId, Arg.Any<IReadOnlyCollection<Guid>>(), "v1", Arg.Any<CancellationToken>());
    }

    // Thay 3 vai trò đang có bằng 4 vai trò mới: 7 vai trò bị chạm (luật 1 áp cho cả thêm lẫn gỡ). Số lần hỏi seam không
    // được tăng theo con số đó — tập quyền người gọi MỘT lần, tập quyền các vai trò MỘT câu, sự tồn tại các vai trò MỘT câu.
    [Fact]
    public async Task Handle_ManyRolesTouched_AsksEachSeamAConstantNumberOfTimes()
    {
        var targetId = Guid.NewGuid();
        var current = Enumerable.Range(0, 3).Select(i => new UserRoleSummaryDto(Guid.NewGuid(), $"Cũ {i}", false)).ToList();
        var added = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        _userQuery.FindByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new UserListItemDto(targetId, "an.nv", "an@vd.vn", "An", current, false, null, false, false, null, "v1"));
        _roleQuery.StubRolesExist(_ => true);
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read" });
        _permissionChecker.StubRolePermissions(
            current.Select(r => r.Id).Concat(added).ToDictionary(id => id, _ => new[] { "core.user.read" }));
        _userAdmin.AssignRolesAsync(targetId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateHandler().Handle(new AssignUserRolesCommand(targetId, added, "v1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        SeamCallCounter.CallerPermissionLookups(_permissionChecker).ShouldBe(1);
        _permissionChecker.ReceivedCalls().Count().ShouldBeLessThanOrEqualTo(2);
        _roleQuery.ReceivedCalls().Count().ShouldBe(1);
    }

    // docs/contracts/users.md §7 ràng buộc 3: PUT không đổi vai trò nào VẪN so version. Handler không được tự trả 200 ở nhánh
    // "không có gì đổi" — token khớp lúc đọc thì nó vẫn phải giao version xuống seam, vì phép so-và-đổi ở đó là chốt cuối:
    // một lượt ghi khác chen giữa lần đọc và câu UPDATE chỉ lộ ra ở seam, và lỗi đồng thời seam trả về đi thẳng ra.
    [Fact]
    public async Task Handle_NoRoleChange_StillHandsTheVersionToTheSeam_AndPropagatesItsConflict()
    {
        var targetId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _userQuery.FindByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new UserListItemDto(
                targetId, "an.nv", "an@vd.vn", "An", [new UserRoleSummaryDto(roleId, "Kế toán", false)],
                false, null, false, false, null, "v1"));
        _userAdmin.AssignRolesAsync(targetId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await CreateHandler().Handle(new AssignUserRolesCommand(targetId, [roleId], "v1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        await _userAdmin.Received(1).AssignRolesAsync(targetId, Arg.Any<IReadOnlyCollection<Guid>>(), "v1", Arg.Any<CancellationToken>());
    }

    // docs/contracts/users.md §7 mục "Ghi chú — đồng thời", ADR-0082. B vừa gán Rhigh cho X (token v1 → v2); A vẫn cầm v1 và
    // PUT {R1, R3}. Luật 1 chạy trên trạng thái HIỆN TẠI thì thấy A "gỡ" Rhigh — một vai trò A chưa từng thấy — và trả 403;
    // nhánh 409 "tải lại" của FE không bao giờ chạy. Token lệch phải thắng trước mọi luật §2, và không gì xuống tới seam.
    //
    // Hàng "v2" là cặp đỏ: cùng dữ liệu, token khớp ⇒ luật 1 THẬT SỰ bắn — nên 409 ở các hàng còn lại không phải vì luật im.
    // "V2" khác "v2" đúng một chữ hoa: phép so là ordinal. null không bao giờ khớp.
    [Theory]
    [InlineData("v2", "CORE.USER.ROLE_ESCALATION_FORBIDDEN")]
    [InlineData("v1", "CORE.CONCURRENCY.CONFLICT")]
    [InlineData("V2", "CORE.CONCURRENCY.CONFLICT")]
    [InlineData(null, "CORE.CONCURRENCY.CONFLICT")]
    public async Task Handle_StaleVersion_RemovingARoleCallerCannotGrant_ReturnsConflict_NotEscalation(
        string? clientVersion, string expectedCode)
    {
        UserErrors.RoleEscalationForbidden.Code.ShouldBe("CORE.USER.ROLE_ESCALATION_FORBIDDEN");
        CommonErrors.ConcurrencyConflict.Code.ShouldBe("CORE.CONCURRENCY.CONFLICT");

        var targetId = Guid.NewGuid();
        var r1 = new UserRoleSummaryDto(Guid.NewGuid(), "Kế toán", false);
        var r3 = new UserRoleSummaryDto(Guid.NewGuid(), "Thủ quỹ", false);
        var rHigh = new UserRoleSummaryDto(Guid.NewGuid(), "Quản lý quyền", false);
        _userQuery.FindByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new UserListItemDto(targetId, "an.nv", "an@vd.vn", "An", [r1, r3, rHigh], false, null, false, false, null, "v2"));
        _roleQuery.StubRolesExist(_ => true);
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]>
        {
            [r1.Id] = ["core.user.read"],
            [r3.Id] = ["core.user.read"],
            [rHigh.Id] = ["core.permission.write"],
        });

        var result = await CreateHandler().Handle(
            new AssignUserRolesCommand(targetId, [r1.Id, r3.Id], clientVersion), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(expectedCode);
        await _userAdmin.DidNotReceiveWithAnyArgs().AssignRolesAsync(default, default!, default, default);
    }

    // Cùng khuôn với ca trên cho luật 2: X tự gỡ vai trò hệ thống của chính mình bằng một token cũ. Luật 2 chạy trên trạng
    // thái hiện tại thì trả 422 SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN cho một tập vai trò người gọi chưa từng thấy. Hàng "v2" là
    // cặp đỏ: token khớp ⇒ luật 2 thật sự bắn.
    [Theory]
    [InlineData("v2", "CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN")]
    [InlineData("v1", "CORE.CONCURRENCY.CONFLICT")]
    [InlineData(null, "CORE.CONCURRENCY.CONFLICT")]
    public async Task Handle_StaleVersion_RemovingOwnSystemRole_ReturnsConflict_NotSelfSystemRoleRemoval(
        string? clientVersion, string expectedCode)
    {
        UserErrors.SelfSystemRoleRemovalForbidden.Code.ShouldBe("CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN");
        CommonErrors.ConcurrencyConflict.Code.ShouldBe("CORE.CONCURRENCY.CONFLICT");

        var systemRole = new UserRoleSummaryDto(Guid.NewGuid(), "Quản trị hệ thống", true);
        _userQuery.FindByIdAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new UserListItemDto(_callerId, "an.nv", "an@vd.vn", "An", [systemRole], false, null, false, false, null, "v2"));
        _permissionChecker.GetEffectivePermissionsAsync(_callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.permission.write" });
        _permissionChecker.StubRolePermissions(new Dictionary<Guid, string[]> { [systemRole.Id] = ["core.permission.write"] });

        var result = await CreateHandler().Handle(new AssignUserRolesCommand(_callerId, [], clientVersion), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(expectedCode);
        await _userAdmin.DidNotReceiveWithAnyArgs().AssignRolesAsync(default, default!, default, default);
    }
}
