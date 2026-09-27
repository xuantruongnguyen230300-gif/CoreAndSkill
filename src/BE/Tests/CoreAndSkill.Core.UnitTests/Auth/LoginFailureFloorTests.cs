using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.UnitTests.Support;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

// Luật S19 — docs/RULES.md §6, docs/adr/0058-dang-nhap-truot-cho-du-mot-san-thoi-gian.md, docs/contracts/auth.md §3: mọi
// phản hồi trượt của đăng nhập — trừ 429 — đi ra không sớm hơn một sàn chung, tính từ lúc handler bắt đầu; nhánh thành
// công không chờ. Đồng hồ lái tay: mỗi seam "làm việc" đẩy đồng hồ đi một khoảng khác nhau (mô phỏng nhánh nhanh/chậm),
// rồi test đọc CHÍNH XÁC mốc handler trả lời — không đo thời gian thật (chập chờn), không ngủ.
public class LoginFailureFloorTests
{
    private const int FloorMs = 500;

    private static readonly DateTimeOffset Start = new(2026, 9, 22, 8, 0, 0, TimeSpan.Zero);

    private readonly ManualTimeProvider _clock = new(Start);
    private readonly ILoginAttemptLimiter _loginAttemptLimiter = Substitute.For<ILoginAttemptLimiter>();
    private readonly ITenantLookup _tenantLookup = Substitute.For<ITenantLookup>();
    private readonly IExecutionContextScope _executionContextScope = Substitute.For<IExecutionContextScope>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IUserLookupService _userLookup = Substitute.For<IUserLookupService>();
    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();

    private readonly TenantSummary _activeTenant = new(Guid.NewGuid(), "SO-GD", "Sở GD", IsActive: true);

    private LoginCommandHandler CreateHandler(int floorMs = FloorMs)
    {
        var authOptions = Options.Create(new CoreAuthOptions
        {
            CookieName = "c",
            AntiforgeryCookieName = "a",
            SessionMinutes = 30,
            AllowedOrigins = ["https://x"],
        });
        _executionContextScope.Enter(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string?>()).Returns(Substitute.For<IDisposable>());

        return new LoginCommandHandler(
            _loginAttemptLimiter, _tenantLookup, _executionContextScope, _identityService, _userLookup,
            new SessionDtoFactory(_userLookup, _permissionChecker, authOptions),
            Options.Create(new CoreIdentityLoginOptions { FailureFloorMs = floorMs }),
            _clock);
    }

    // Từng nhánh trượt tiêu một lượng thời gian KHÁC NHAU trước khi tới chỗ trả lời — đúng chênh lệch mà ADR-0058 mô tả.
    public enum Branch
    {
        TenantMissing,
        TenantInactive,
        UserNameUnknown,
        WrongPassword,
        LockedOut,
        UserSummaryMissing,
    }

    private void Arrange(Branch branch)
    {
        switch (branch)
        {
            case Branch.TenantMissing:
                _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>()).Returns(_ => Work(TimeSpan.FromMilliseconds(3), (TenantSummary?)null));
                _identityService.SimulateCredentialCheckAsync("pw", Arg.Any<CancellationToken>()).Returns(_ => Work(TimeSpan.FromMilliseconds(90)));
                break;

            case Branch.TenantInactive:
                _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>())
                    .Returns(_ => Work(TimeSpan.FromMilliseconds(3), (TenantSummary?)new TenantSummary(Guid.NewGuid(), "SO-GD", "Sở GD", IsActive: false)));
                _identityService.SimulateCredentialCheckAsync("pw", Arg.Any<CancellationToken>()).Returns(_ => Work(TimeSpan.FromMilliseconds(90)));
                break;

            case Branch.UserNameUnknown:
                ArrangeActiveTenant();
                _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
                    .Returns(_ => Work(TimeSpan.FromMilliseconds(95), Result.Failure<CredentialCheck>(AuthErrors.InvalidCredentials)));
                break;

            case Branch.WrongPassword:
                ArrangeActiveTenant();
                // Băm thật + một câu UPDATE có commit — nhánh chậm nhất.
                _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
                    .Returns(_ => Work(TimeSpan.FromMilliseconds(140), Result.Failure<CredentialCheck>(AuthErrors.InvalidCredentials)));
                break;

            case Branch.LockedOut:
                ArrangeActiveTenant();
                _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
                    .Returns(_ => Work(TimeSpan.FromMilliseconds(95), Result.Failure<CredentialCheck>(AuthErrors.LockedOut)));
                break;

            case Branch.UserSummaryMissing:
                ArrangeActiveTenant();
                var userId = Guid.NewGuid();
                _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
                    .Returns(_ => Work(TimeSpan.FromMilliseconds(95), Result.Success(new CredentialCheck(userId, "stamp", false))));
                _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(_ => Work(TimeSpan.FromMilliseconds(2), (UserSummaryDto?)null));
                break;
        }
    }

    private void ArrangeActiveTenant()
        => _tenantLookup.FindByCodeAsync("SO-GD", Arg.Any<CancellationToken>()).Returns(_ => Work(TimeSpan.FromMilliseconds(3), (TenantSummary?)_activeTenant));

    // Một seam "làm việc" = đồng hồ đi tới.
    private Task<T> Work<T>(TimeSpan took, T value)
    {
        _clock.Advance(took);
        return Task.FromResult(value);
    }

    private Task Work(TimeSpan took)
    {
        _clock.Advance(took);
        return Task.CompletedTask;
    }

    public static TheoryData<Branch> FailureBranches => new(Enum.GetValues<Branch>());

    [Theory]
    [MemberData(nameof(FailureBranches))]
    public async Task Login_EveryFailureBranch_WaitsForTheSameFloor(Branch branch)
    {
        Arrange(branch);
        var handler = CreateHandler();

        var pending = handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        // Mọi truy vấn đã xong (đồng hồ đã đi), nhưng handler CHƯA trả lời: nó đang giữ phản hồi lại cho tới mốc sàn.
        pending.IsCompleted.ShouldBeFalse($"nhánh {branch} trả lời trước mốc sàn");
        var elapsedByWork = _clock.GetUtcNow() - Start;
        elapsedByWork.ShouldBeGreaterThan(TimeSpan.Zero, "chống test rỗng: nhánh phải có tiêu thời gian");
        _clock.RequestedDelays.ShouldHaveSingleItem().ShouldBe(TimeSpan.FromMilliseconds(FloorMs) - elapsedByWork);

        // Một tick trước mốc: vẫn chưa trả lời. Đúng mốc: trả lời, và là phản hồi TRƯỢT — sàn không đổi kết quả.
        _clock.Advance(TimeSpan.FromMilliseconds(FloorMs) - elapsedByWork - TimeSpan.FromTicks(1));
        pending.IsCompleted.ShouldBeFalse();
        _clock.Advance(TimeSpan.FromTicks(1));

        var result = await pending;
        result.IsFailure.ShouldBeTrue();
        (_clock.GetUtcNow() - Start).ShouldBe(TimeSpan.FromMilliseconds(FloorMs));
    }

    // Nhánh chậm hơn sàn: không chờ thêm — sàn là mốc tối thiểu, không phải khoảng cộng thêm (nếu cộng thêm thì chênh lệch
    // giữa các nhánh còn nguyên, chỉ dịch đi 500 ms).
    [Fact]
    public async Task Login_FailureBranchSlowerThanTheFloor_DoesNotWaitAnyLonger()
    {
        ArrangeActiveTenant();
        _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
            .Returns(_ => Work(TimeSpan.FromMilliseconds(FloorMs + 200), Result.Failure<CredentialCheck>(AuthErrors.InvalidCredentials)));
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        result.Error!.Code.ShouldBe(AuthErrors.InvalidCredentials.Code);
        _clock.RequestedDelays.ShouldBeEmpty();
    }

    // Nhánh thành công không chờ (ADR-0058 quyết định 1): người đăng nhập đúng không phải trả giá cho kẻ dò.
    [Fact]
    public async Task Login_Success_DoesNotWait()
    {
        ArrangeActiveTenant();
        var userId = Guid.NewGuid();
        _identityService.CheckCredentialsAsync("an.nv", "pw", Arg.Any<CancellationToken>())
            .Returns(_ => Work(TimeSpan.FromMilliseconds(95), Result.Success(new CredentialCheck(userId, "stamp-1", false))));
        _userLookup.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserSummaryDto(userId, "an.nv", "Nguyễn Văn An", "an@vd.vn", false, false, false, "vi"));
        _userLookup.GetRoleNamesAsync(userId, Arg.Any<CancellationToken>()).Returns([]);
        _permissionChecker.GetEffectivePermissionsAsync(userId, Arg.Any<CancellationToken>()).Returns(new HashSet<string>());
        var handler = CreateHandler();

        var pending = handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None);

        pending.IsCompleted.ShouldBeTrue("nhánh thành công không được giữ lại");
        (await pending).IsSuccess.ShouldBeTrue();
        _clock.RequestedDelays.ShouldBeEmpty();
    }

    // 429 là ngoại lệ có tên của luật: hạn mức chặn TRƯỚC mọi truy vấn, và exception đi thẳng ra IExceptionHandler — không
    // có gì để san, và giữ lại một request bị từ chối chỉ tốn chỗ trong hàng rào.
    [Fact]
    public async Task Login_RateLimited_ThrowsImmediately_WithoutWaiting()
    {
        _loginAttemptLimiter.EnsureAttemptAllowedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new LoginAttemptLimitExceededException(TimeSpan.FromSeconds(30)));
        var handler = CreateHandler();

        await Should.ThrowAsync<LoginAttemptLimitExceededException>(
            () => handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), CancellationToken.None));

        _clock.RequestedDelays.ShouldBeEmpty();
    }

    // Huỷ request trong lúc đang giữ phản hồi: thoát ngay bằng OperationCanceledException, không đợi hết sàn.
    [Fact]
    public async Task Login_CancelledWhileHolding_StopsWaiting()
    {
        Arrange(Branch.TenantMissing);
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();

        var pending = handler.Handle(new LoginCommand("SO-GD", "an.nv", "pw"), cts.Token);
        pending.IsCompleted.ShouldBeFalse();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => pending);
    }
}
