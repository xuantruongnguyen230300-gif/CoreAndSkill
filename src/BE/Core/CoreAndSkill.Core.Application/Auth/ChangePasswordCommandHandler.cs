using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Auth;

internal sealed class ChangePasswordCommandHandler(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IIdentityService identityService,
    IUserLookupService userLookup,
    ITenantLookup tenantLookup,
    SessionDtoFactory sessionDtoFactory)
    : IRequestHandler<ChangePasswordCommand, Result<LoginOutcome>>
{
    public async Task<Result<LoginOutcome>> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("ChangePasswordCommand chạy khi chưa xác thực.");
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("ChangePasswordCommand chạy khi chưa có đơn vị.");

        // clearMustChangePassword: false — đổi TỰ NGUYỆN, không phải đường thoát bắt buộc (§7).
        var checkResult = await identityService.ChangePasswordAsync(
            userId, command.CurrentPassword, command.NewPassword, clearMustChangePassword: false, ct);

        if (checkResult.IsFailure)
            return Result.Failure<LoginOutcome>(checkResult.Error!);

        var check = checkResult.Value;

        var userSummary = await userLookup.FindByIdAsync(userId, ct)
            ?? throw new InvalidOperationException("Phiên hợp lệ nhưng không tìm thấy tài khoản.");
        var tenant = await tenantLookup.FindByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Phiên hợp lệ nhưng không tìm thấy đơn vị.");

        var session = await sessionDtoFactory.BuildAsync(userSummary, tenant, check.MustChangePassword, ct);

        return Result.Success(new LoginOutcome(session, check.SecurityStamp, tenantId));
    }
}
