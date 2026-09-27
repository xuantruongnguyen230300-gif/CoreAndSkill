namespace CoreAndSkill.Core.Web.Security;

// Allowlist tường minh của MỌI [AllowAnonymous] trong solution — luật S4
// (docs/RULES.md S4, docs/quy-uoc/be-api-controller.md §5). Chữ ký chép nguyên từ file đó —
// không viết lại. Thêm một dòng vào đây là một thay đổi phải giải trình trong PR. ArchTest
// EveryAllowAnonymous_IsOn_TheAllowlist (CoreAndSkill.ArchTests) đối chiếu mọi [AllowAnonymous]
// trong solution với danh sách này.
public static class AnonymousEndpointAllowlist
{
    public static readonly IReadOnlySet<string> Endpoints = new HashSet<string>(StringComparer.Ordinal)
    {
        // Endpoint thử của B0 — chứng minh cầu nối Result → HTTP trước khi CQRS/Identity (B1)
        // tồn tại. docs/wiki-core/be/trien-khai/01-b0-nen-mong.md §2 bước 4,
        // docs/contracts/diagnostics.md. Chỉ có NGOÀI Production — ở Production tuyến không được đăng ký
        // (DiagnosticsOutsideProductionConvention); dòng này vẫn cần vì S4 quét attribute, không quét môi trường.
        "GET /api/v1/core/diagnostics/probe",

        // docs/contracts/auth.md §3 — chưa đăng nhập thì chưa có gì để đòi quyền.
        "POST /api/v1/core/auth/login",

        // docs/contracts/auth.md §2 — cần để gửi được request ghi đầu tiên; không rate-limit
        // (docs/quy-uoc/be-api-controller.md §6.3).
        "GET /api/v1/core/antiforgery/token",

        // docs/contracts/client-errors.md §1 — lỗi ở màn đăng nhập cũng phải báo được. Rate limit
        // RIÊNG (policy "client-errors", KHÔNG miễn) — CoreWebServiceCollectionExtensions.cs.
        "POST /api/v1/core/client-errors",
    };
}
