namespace CoreAndSkill.Core.Web.Http;

// Tiền tố đường dẫn HTTP — docs/quy-uoc/be-api-controller.md §8.1 "Đường dẫn trong code phải đến từ MỘT hằng số dùng
// chung". [Route] đòi hằng số lúc biên dịch nên mọi giá trị là `const string` ghép chuỗi. Template của [Route] KHÔNG
// có dấu `/` đầu; đường dẫn tuyệt đối (header Location, so khớp HttpRequest.Path) ghép thêm `/` ở chỗ dùng.
//
// public: module khai route của mình dưới ApiRoutes.V1 + "/<module>" — cùng một nguồn với Core. Chỉ Root và V1 ở đây:
// đoạn "/core" là khu RIÊNG của Core (§8.1 "Core không bao giờ đăng ký dưới tiền tố của module" — và ngược lại), nên
// nó nằm ở CoreRoutes (internal), ngoài tầm với của module. ArchTest ApiRoutes_ExposesOnly_RootAndV1_ToModules canh.
public static class ApiRoutes
{
    // Tiền tố gốc — cũng là allowlist của EnvelopeMiddleware (be-api-controller.md §2.4).
    public const string Root = "api";

    public const string V1 = Root + "/v1";
}

// Route của từng controller Core, và đoạn route của những action mà code khác phải so khớp (allowlist của
// PasswordChangeRequiredMiddleware — docs/contracts/auth.md §1.2). Action và allowlist dùng CÙNG hằng số, nên đổi một
// bên là đổi cả hai.
internal static class CoreRoutes
{
    // Khu của Core dưới v1 — module KHÔNG dùng được hằng số này (internal), đúng ý be-api-controller.md §8.1.
    public const string CoreV1 = ApiRoutes.V1 + "/core";

    public const string Antiforgery = CoreV1 + "/antiforgery";
    public const string AntiforgeryToken = "token";

    public const string Auth = CoreV1 + "/auth";
    public const string AuthMe = "me";
    public const string AuthLogout = "logout";
    public const string AuthChangePasswordRequired = "change-password-required";

    public const string ClientErrors = CoreV1 + "/client-errors";
    public const string Diagnostics = CoreV1 + "/diagnostics";
    public const string Files = CoreV1 + "/files";
    public const string Jobs = CoreV1 + "/jobs";
    public const string Meta = CoreV1 + "/meta";
    public const string Notifications = CoreV1 + "/notifications";
    public const string Permissions = CoreV1 + "/permissions";
    public const string Profile = CoreV1 + "/profile";
    public const string Roles = CoreV1 + "/roles";
    public const string SystemTenants = CoreV1 + "/system/tenants";
    public const string Users = CoreV1 + "/users";
}
