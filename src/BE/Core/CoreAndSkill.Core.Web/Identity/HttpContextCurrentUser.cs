using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CoreAndSkill.Core.Web.Identity;

// Hiện thực ICurrentUser — CHỖ DUY NHẤT đọc HttpContext để lấy danh tính (docs/quy-uoc/be-architecture.md
// §1.1, §2.1). Đọc theo thứ tự: IExecutionContextScope.Current → HttpContext → không có gì.
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor, IExecutionContextScope executionScope)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            if (executionScope.Current is { } scope)
                return scope.UserId;

            var value = httpContextAccessor.HttpContext?.User.FindFirst(CoreClaimTypes.UserId)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserName
    {
        get
        {
            if (executionScope.Current is { } scope)
                return scope.UserName;

            return httpContextAccessor.HttpContext?.User.FindFirst(CoreClaimTypes.UserName)?.Value;
        }
    }
}
