using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Auth;

internal sealed class ChangePasswordRequiredCommandHandler(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IIdentityService identityService,
    IUserLookupService userLookup,
    ITenantLookup tenantLookup,
    SessionDtoFactory sessionDtoFactory)
    : IRequestHandler<ChangePasswordRequiredCommand, Result<LoginOutcome>>
{
    public async Task<Result<LoginOutcome>> Handle(ChangePasswordRequiredCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("ChangePasswordRequiredCommand chạy khi chưa xác thực.");
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("ChangePasswordRequiredCommand chạy khi chưa có đơn vị.");

        var userSummary = await userLookup.FindByIdAsync(userId, ct)
            ?? throw new InvalidOperationException("Phiên hợp lệ nhưng không tìm thấy tài khoản.");

        if (!userSummary.MustChangePassword)
            return Result.Failure<LoginOutcome>(AuthErrors.PasswordChangeNotRequired);

        // clearMustChangePassword: true — đây LÀ đường thoát bắt buộc (§7), khác §6.
        var checkResult = await identityService.ChangePasswordAsync(
            userId, command.CurrentPassword, command.NewPassword, clearMustChangePassword: true, ct);

        if (checkResult.IsFailure)
            return Result.Failure<LoginOutcome>(checkResult.Error!);

        var check = checkResult.Value;

        var tenant = await tenantLookup.FindByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Phiên hợp lệ nhưng không tìm thấy đơn vị.");

        var session = await sessionDtoFactory.BuildAsync(userSummary, tenant, check.MustChangePassword, ct);

        return Result.Success(new LoginOutcome(session, check.SecurityStamp, tenantId));
    }
}
