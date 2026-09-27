using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Auth;

// [AuthenticatedOnly] ở Core.Web đã đảm bảo ICurrentUser.UserId và ITenantContext.TenantId có giá
// trị trước khi tới đây. UserSummary/tenant không tìm thấy là dữ liệu không nhất quán (phiên hợp
// lệ nhưng bản ghi biến mất) — lỗi NGOÀI DỰ KIẾN (docs/quy-uoc/be-entity-domain.md §3.4), ném thay
// vì trả Result.
internal sealed class GetCurrentSessionQueryHandler(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    ITenantLookup tenantLookup,
    IUserLookupService userLookup,
    SessionDtoFactory sessionDtoFactory)
    : IRequestHandler<GetCurrentSessionQuery, Result<SessionDto>>
{
    public async Task<Result<SessionDto>> Handle(GetCurrentSessionQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("GetCurrentSessionQuery chạy khi chưa xác thực.");
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("GetCurrentSessionQuery chạy khi chưa có đơn vị.");

        var userSummary = await userLookup.FindByIdAsync(userId, ct)
            ?? throw new InvalidOperationException("Phiên hợp lệ nhưng không tìm thấy tài khoản.");

        var tenant = await tenantLookup.FindByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Phiên hợp lệ nhưng không tìm thấy đơn vị.");

        var session = await sessionDtoFactory.BuildAsync(userSummary, tenant, userSummary.MustChangePassword, ct);

        return Result.Success(session);
    }
}
