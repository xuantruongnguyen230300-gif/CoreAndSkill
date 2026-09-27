using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using MediatR;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Application.Auth;

// Thứ tự — docs/quy-uoc/be-api-controller.md §7.4, docs/wiki-core/be/17-multi-tenant.md §11.1.
// Hạn mức theo LoginPartitionKey (ILoginAttemptLimiter) — docs/quy-uoc/be-api-controller.md §6.5.
//
// Luật S19 (docs/adr/0058-dang-nhap-truot-cho-du-mot-san-thoi-gian.md, docs/contracts/auth.md §3): MỌI phản hồi trượt
// — trừ 429 — đi ra không sớm hơn Core:Identity:Login:FailureFloorMs tính từ lúc handler bắt đầu. Phép băm giả chỉ san
// phần băm; các nhánh vẫn khác nhau ở phần truy vấn (nhánh sai mật khẩu ghi thêm một câu UPDATE, nhánh đơn vị không có
// thì không tra tài khoản), và chênh lệch đó lộ ra tên đăng nhập / mã đơn vị nào có thật. Sàn bù phần phép băm giả không
// san được, chứ không thay nó. Nhánh thành công không chờ.
internal sealed class LoginCommandHandler(
    ILoginAttemptLimiter loginAttemptLimiter,
    ITenantLookup tenantLookup,
    IExecutionContextScope executionContextScope,
    IIdentityService identityService,
    IUserLookupService userLookup,
    SessionDtoFactory sessionDtoFactory,
    IOptions<CoreIdentityLoginOptions> loginOptions,
    TimeProvider timeProvider)
    : IRequestHandler<LoginCommand, Result<LoginOutcome>>
{
    public async Task<Result<LoginOutcome>> Handle(LoginCommand command, CancellationToken ct)
    {
        // Mốc sàn tính từ ĐÂY — trước cả hạn mức: 429 đi ra bằng exception ở dòng dưới và không chờ (ADR-0058 quyết định 1).
        var startedAt = timeProvider.GetTimestamp();

        // Hàng rào 3 — kiểm TRƯỚC khi tra tenant, không phụ thuộc tenant/user có tồn tại hay không
        // (be-api-controller.md §6.5).
        await loginAttemptLimiter.EnsureAttemptAllowedAsync(command.TenantCode, command.UserName, ct);

        // Bước 1-2: tra tenant. Không tìm thấy, hoặc ngưng hoạt động → trượt cùng mã với sai mật
        // khẩu (docs/contracts/auth.md §3) — VÀ cùng giá một phép băm: thiếu nó, thời gian phản hồi lộ ra
        // đơn vị nào có thật (docs/wiki-core/be/09-security-beyond-auth.md bảng "Đường rò"). Hai ca đi CHUNG
        // một nhánh để không ca nào nhanh hơn ca kia.
        var tenant = await tenantLookup.FindByCodeAsync(command.TenantCode, ct);
        if (tenant is null || !tenant.IsActive)
        {
            await identityService.SimulateCredentialCheckAsync(command.Password, ct);
            return await FailAtTheFloorAsync(startedAt, AuthErrors.InvalidCredentials, ct);
        }

        // Bước 3: nạp TenantId vào ngữ cảnh của request này — PHẢI đứng trước bước 4 (luật A12
        // allowlist "bước đăng nhập").
        using var scope = executionContextScope.Enter(tenant.Id, userId: null, userName: null);

        // Bước 4-5: kiểm mật khẩu rồi khoá tài khoản — thứ tự đầy đủ ở IIdentityService (Infrastructure).
        var credentialCheckResult = await identityService.CheckCredentialsAsync(command.UserName, command.Password, ct);
        if (credentialCheckResult.IsFailure)
            return await FailAtTheFloorAsync(startedAt, credentialCheckResult.Error!, ct);

        var credentialCheck = credentialCheckResult.Value;

        var userSummary = await userLookup.FindByIdAsync(credentialCheck.UserId, ct);
        if (userSummary is null)
            return await FailAtTheFloorAsync(startedAt, AuthErrors.InvalidCredentials, ct);

        var session = await sessionDtoFactory.BuildAsync(userSummary, tenant, credentialCheck.MustChangePassword, ct);

        // Bước 6: phát phiếu mang claim TenantId — controller ở Core.Web gọi
        // ISessionPrincipalFactory rồi SignInAsync (docs/adr/0033-luong-dang-nhap-outcome-va-claim.md).
        return Result.Success(new LoginOutcome(session, credentialCheck.SecurityStamp, tenant.Id));
    }

    // Đường ra DUY NHẤT của mọi nhánh trượt: đếm số liệu, rồi giữ phản hồi lại cho tới mốc sàn. Chờ SAU khi mọi truy vấn
    // đã xong và ngoài transaction (LoginCommand cài INoTransaction) nên không giữ kết nối; chờ bất đồng bộ nên không giữ
    // luồng. Nhánh đã chậm hơn sàn thì không chờ thêm — sàn là mốc tối thiểu, không phải khoảng cộng thêm.
    private async Task<Result<LoginOutcome>> FailAtTheFloorAsync(long startedAt, Error error, CancellationToken ct)
    {
        CoreMetrics.LoginFailed.Add(1);

        var remaining = TimeSpan.FromMilliseconds(loginOptions.Value.FailureFloorMs) - timeProvider.GetElapsedTime(startedAt);
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, timeProvider, ct);

        return Result.Failure<LoginOutcome>(error);
    }
}
