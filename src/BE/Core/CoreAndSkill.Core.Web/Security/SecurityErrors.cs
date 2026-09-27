using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Web.Security;

// Catalog DUY NHẤT của mã mà hạ tầng HTTP phát — docs/quy-uoc/be-api-controller.md §7.4.
internal static class SecurityErrors
{
    public static readonly Error RouteNotFound = new(
        "CORE.ROUTE.NOT_FOUND", "Không có đường dẫn này.", ErrorType.NotFound);

    public static readonly Error RouteMethodNotAllowed = new(
        "CORE.ROUTE.METHOD_NOT_ALLOWED", "Phương thức không được hỗ trợ ở đường dẫn này.", ErrorType.NotFound);

    public static readonly Error NotAuthenticated = new(
        "CORE.AUTH.NOT_AUTHENTICATED", "Chưa đăng nhập hoặc phiên đã hết hạn.", ErrorType.Unauthorized);

    // Handler cũng phát mã này — khai MỘT lần ở CommonErrors, ở đây trỏ tới (be-api-controller.md §7.4, luật R3).
    public static readonly Error Forbidden = CommonErrors.Forbidden;

    public static readonly Error PasswordChangeRequired = new(
        "CORE.AUTH.PASSWORD_CHANGE_REQUIRED", "Phải đổi mật khẩu trước khi tiếp tục.", ErrorType.Forbidden);

    // B2 — docs/quy-uoc/be-api-controller.md §7.2.
    public static readonly Error OriginRejected = new(
        "CORE.AUTH.ORIGIN_REJECTED", "Nguồn gọi không nằm trong danh sách được phép.", ErrorType.Forbidden);

    public static readonly Error CsrfRejected = new(
        "CORE.AUTH.CSRF_REJECTED", "Thiếu hoặc sai token chống giả mạo.", ErrorType.Forbidden);

    // Status KHÔNG suy từ Type — đặt trực tiếp ở nơi phát (429), không qua ResultToHttpMapper —
    // docs/quy-uoc/be-api-controller.md §2.4, §6.5.
    public static readonly Error RateLimitExceeded = new(
        "CORE.RATE_LIMIT.EXCEEDED", "Quá nhiều yêu cầu, thử lại sau.", ErrorType.BusinessRule);
}
