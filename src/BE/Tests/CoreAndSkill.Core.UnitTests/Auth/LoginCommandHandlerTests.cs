using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private readonly ILoginAttemptLimiter _loginAttemptLimiter = Substitute.For<ILoginAttemptLimiter>();
    private readonly ITenantLookup _tenantLookup = Substitute.For<ITenantLookup>();
    private readonly IExecutionContextScope _executionContextScope = Substitute.For<IExecutionContextScope>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();

    private LoginCommandHandler CreateHandler(int sessionMinutes = 30)
    {
        var authOptions = Options.Create(new CoreAuthOptions
        {
            CookieName = "c",
            AntiforgeryCookieName = "a",
            SessionMinutes = sessionMinutes,
            AllowedOrigins = ["https://x"],
        });
        var factory = new SessionDtoFactory(_userLookup, _permissionChecker, authOptions);

        _executionContextScope.Enter(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string?>())
            .Returns(Substitute.For<IDisposable>());

        // Sàn thời gian (S19) kiểm riêng ở LoginFailureFloorTests bằng đồng hồ lái tay; ở đây đồng hồ thật với sàn 1 ms —
        // đủ nhỏ để không kéo dài test, đủ để nhánh trượt vẫn đi qua đúng đường chờ.
        return new LoginCommandHandler(
            _loginAttemptLimiter, _tenantLookup, _executionContextScope, _identityService, _userLookup, factory,
            Options.Create(new CoreIdentityLoginOptions { FailureFloorMs = 1 }), TimeProvider.System);
    }

    [Fact]
    public async Task Handle_ChecksLoginAttemptLimit_BeforeLookingUpTenant()
    {
        _tenantLookup.FindByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TenantSummary?)null);
        var handler = CreateHandler();

        await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        await _loginAttemptLimiter.Received(1).EnsureAttemptAllowedAsync("SO-GD", "an.nv", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LoginAttemptLimitExceeded_PropagatesException()
    {
        _loginAttemptLimiter.EnsureAttemptAllowedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new LoginAttemptLimitExceededException(TimeSpan.FromSeconds(30)));
        var handler = CreateHandler();

        await Should.ThrowAsync<LoginAttemptLimitExceededException>(
            () => handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None));

        await _tenantLookup.DidNotReceiveWithAnyArgs().FindByCodeAsync(default!, default);
    }

    [Fact]
    public async Task Handle_TenantNotFound_ReturnsInvalidCredentials()
    {
        _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>()).Returns((TenantSummary?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(AuthErrors.InvalidCredentials.Code);
        await _identityService.DidNotReceiveWithAnyArgs().CheckCredentialsAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_TenantInactive_ReturnsInvalidCredentials()
    {
        var tenantId = Guid.NewGuid();
        _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(tenantId, "SO-GD", "Sở GD", IsActive: false));
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(AuthErrors.InvalidCredentials.Code);
    }

    // F1 — chống dò bằng thời gian (docs/wiki-core/be/09-security-beyond-auth.md bảng "Đường rò", docs/contracts/auth.md
    // §3): nhánh trượt vì đơn vị không có / ngưng hoạt động PHẢI trả giá một phép băm như nhánh có đơn vị. Đếm lời gọi
    // thay vì đo thời gian thật — đo thời gian chập chờn.
    public static TheoryData<bool?> TenantMissingOrInactive => new() { null, false };

    [Theory]
    [MemberData(nameof(TenantMissingOrInactive))]
    public async Task Handle_TenantMissingOrInactive_RunsExactlyOneSimulatedPasswordCheck(bool? tenantActive)
    {
        _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>())
            .Returns(tenantActive is { } active ? new TenantSummary(Guid.NewGuid(), "SO-GD", "Sở GD", active) : null);
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        result.Error!.Code.ShouldBe(AuthErrors.InvalidCredentials.Code);
        await _identityService.Received(1).SimulateCredentialCheckAsync("pw", Arg.Any<CancellationToken>());
        await _identityService.DidNotReceiveWithAnyArgs().CheckCredentialsAsync(default!, default!, default);
    }

    // Đối chứng: có đơn vị thì phép kiểm THẬT gánh phép băm — không thêm phép giả (hai phép băm là lệch theo chiều ngược).
    [Fact]
    public async Task Handle_TenantActive_RunsTheRealCheck_AndNoSimulatedOne()
    {
        _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(Guid.NewGuid(), "SO-GD", "Sở GD", IsActive: true));
        _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CredentialCheck>(AuthErrors.InvalidCredentials));
        var handler = CreateHandler();

        await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        await _identityService.Received(1).CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>());
        await _identityService.DidNotReceiveWithAnyArgs().SimulateCredentialCheckAsync(default!, default);
    }

    [Fact]
    public async Task Handle_TenantFound_OpensExecutionScopeBeforeCheckingCredentials()
    {
        var tenantId = Guid.NewGuid();
        _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(tenantId, "SO-GD", "Sở GD", IsActive: true));

        var userId = Guid.NewGuid();
        _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CredentialCheck>(AuthErrors.InvalidCredentials));

        var handler = CreateHandler();

        await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        _executionContextScope.Received(1).Enter(tenantId, null, null);
    }

    [Fact]
    public async Task Handle_CredentialCheckFails_PropagatesError()
    {
        _tenantLookup.FindByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(Guid.NewGuid(), "SO-GD", "Sở GD", true));
        _identityService.CheckCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CredentialCheck>(AuthErrors.LockedOut));

        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(AuthErrors.LockedOut.Code);
    }

    [Fact]
    public async Task Handle_Success_BuildsSessionAndOutcome()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(tenantId, "SO-GD", "Sở Giáo dục", true));
        _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CredentialCheck(userId, "stamp-1", false)));
        _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserSummaryDto(userId, "an.nv", "Nguyễn Văn An", "an@vd.vn", false, false, false, "vi"));
        _userLookup.GetRoleNamesAsync(userId, Arg.Any<CancellationToken>()).Returns(["Quản trị"]);
        _permissionChecker.GetEffectivePermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.user.read" });

        var handler = CreateHandler(sessionMinutes: 45);

        var result = await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.SecurityStamp.ShouldBe("stamp-1");
        result.Value.TenantId.ShouldBe(tenantId);
        result.Value.Session.Id.ShouldBe(userId);
        result.Value.Session.UserName.ShouldBe("an.nv");
        result.Value.Session.TenantCode.ShouldBe("SO-GD");
        result.Value.Session.TenantName.ShouldBe("Sở Giáo dục");
        result.Value.Session.SessionMinutes.ShouldBe(45);
        result.Value.Session.Roles.ShouldContain("Quản trị");
        result.Value.Session.Permissions.ShouldContain("core.user.read");
        result.Value.Session.MustChangePassword.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_UserSummaryMissingAfterCredentialCheck_ReturnsInvalidCredentials()
    {
        var userId = Guid.NewGuid();
        _tenantLookup.FindByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantSummary(Guid.NewGuid(), "SO-GD", "Sở GD", true));
        _identityService.CheckCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CredentialCheck(userId, "stamp", false)));
        _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserSummaryDto?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(AuthErrors.InvalidCredentials.Code);
    }
}
