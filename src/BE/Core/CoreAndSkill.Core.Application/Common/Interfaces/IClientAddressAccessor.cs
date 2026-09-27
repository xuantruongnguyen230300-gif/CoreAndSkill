using System.Net;

namespace CoreAndSkill.Core.Application.Common.Interfaces;

// Địa chỉ IP của request hiện tại, SAU bước chuyển tiếp của proxy (docs/quy-uoc/be-architecture.md
// §3.1) — dùng để điền cột ip_address của nhật ký kiểm toán (docs/database/schema-core.md §9.4).
// Cùng lý do với ICurrentUser/ITenantContext: chỉ Core.Web được đọc HttpContext (luật M2 áp dụng
// tương tự) — hiện thực HttpContextClientAddressAccessor sống ở Core.Web. Không có request (job
// nền, lệnh bootstrap) ⇒ null.
public interface IClientAddressAccessor
{
    IPAddress? RemoteIpAddress { get; }

    // Header User-Agent thô — dùng cho báo lỗi runtime của trình duyệt (docs/contracts/client-errors.md
    // §1: "server tự đọc User-Agent và IP từ header — không nhận chúng từ thân request").
    string? UserAgent { get; }
}
