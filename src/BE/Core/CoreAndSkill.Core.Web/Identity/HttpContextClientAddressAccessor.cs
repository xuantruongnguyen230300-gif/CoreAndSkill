using CoreAndSkill.Core.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CoreAndSkill.Core.Web.Identity;

// Hiện thực IClientAddressAccessor — CHỖ DUY NHẤT đọc HttpContext để lấy địa chỉ IP của request
// (cùng ranh giới với HttpContextCurrentUser/HttpContextTenantContext, docs/quy-uoc/be-architecture.md
// §1.1, §2.1). RemoteIpAddress đọc SAU UseForwardedHeaders (docs/quy-uoc/be-architecture.md §3.1),
// nên đây đã là IP thật của client, không phải IP của proxy.
internal sealed class HttpContextClientAddressAccessor(IHttpContextAccessor httpContextAccessor)
    : IClientAddressAccessor
{
    public System.Net.IPAddress? RemoteIpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
