using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CoreAndSkill.Core.Web.Identity;

// Hiện thực ITenantContext — CHỖ DUY NHẤT đọc HttpContext để lấy đơn vị (docs/quy-uoc/be-architecture.md
// §1.1, §2.1). Đăng ký Scoped — KHÔNG BAO GIỜ Singleton (bẫy model cache, docs/wiki-core/be/17-multi-tenant.md §4).
internal sealed class HttpContextTenantContext(IHttpContextAccessor httpContextAccessor, IExecutionContextScope executionScope)
    : ITenantContext
{
    public Guid? TenantId
    {
        get
        {
            if (executionScope.Current is { } scope)
                return scope.TenantId;

            var value = httpContextAccessor.HttpContext?.User.FindFirst(CoreClaimTypes.TenantId)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
