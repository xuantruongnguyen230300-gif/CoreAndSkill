using CoreAndSkill.Core.Application.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Web.Security;

// Quy tắc origin chung — docs/quy-uoc/be-api-controller.md §7.3 (mốc "Quy tắc origin chung — định nghĩa gốc"), ADR-0129.
// CORS (SetIsOriginAllowed) và lớp 1 của AntiforgeryValidationMiddleware gọi CÙNG hàm IsAllowed này — luật S30. Không chỗ
// nào khác đọc Core:Auth:AllowedOrigins để quyết một origin.
internal sealed class OriginAllowlist(IOptions<CoreAuthOptions> authOptions, IHostEnvironment environment)
{
    private const string DevelopmentHost = "localhost";

    // Danh sách đọc TRỄ qua IOptions (đã qua ValidateOnStart) — không BuildServiceProvider() giữa chừng.
    public bool IsAllowed(string? origin)
        => IsAllowed(origin, authOptions.Value.AllowedOrigins, environment.IsDevelopment());

    // Lõi thuần — test không cần host. Rỗng hay null trả false.
    //   (a) mọi môi trường: khớp một mục allowedOrigins, không phân biệt hoa thường;
    //   (b) CHỈ khi isDevelopment: đúng dạng header Origin, scheme https, host đúng chữ "localhost", cổng bất kỳ.
    public static bool IsAllowed(string? origin, IEnumerable<string>? allowedOrigins, bool isDevelopment)
    {
        if (string.IsNullOrEmpty(origin))
            return false;

        if (allowedOrigins is not null)
        {
            foreach (var allowed in allowedOrigins)
            {
                if (string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return isDevelopment && IsDevelopmentLocalhostOrigin(origin);
    }

    // Header Origin của trình duyệt luôn là scheme://host[:cổng] — không path, không '/' cuối, không cổng mặc định. Chuỗi
    // khai khác dạng đó (so với chính dạng chuẩn hoá của Uri) thì không bao giờ khớp một Origin thật.
    internal static bool IsWellFormedOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.UserInfo.Length > 0)
        {
            return false;
        }

        return string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
    }

    // So Host với chữ "localhost", KHÔNG dùng Uri.IsLoopback: thuộc tính đó trả true cho cả "loopback", dải 127.0.0.0/8 và
    // [::1] — những origin khác site với API ở localhost nên không đăng nhập được (ADR-0129 quyết định 3).
    private static bool IsDevelopmentLocalhostOrigin(string origin)
        => IsWellFormedOrigin(origin)
           && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
           && uri.Scheme == Uri.UriSchemeHttps
           && string.Equals(uri.Host, DevelopmentHost, StringComparison.OrdinalIgnoreCase);
}
