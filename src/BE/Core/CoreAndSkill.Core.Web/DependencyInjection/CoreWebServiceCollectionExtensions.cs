using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Web.Controllers;
using CoreAndSkill.Core.Web.ExceptionHandling;
using CoreAndSkill.Core.Web.Filters;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Identity;
using CoreAndSkill.Core.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Web.DependencyInjection;

public static class CoreWebServiceCollectionExtensions
{
    // Số lần / cửa sổ mặc định đã CHỐT — docs/quy-uoc/be-api-controller.md §6.1, §6.2. Đọc ở đây,
    // dùng lại ở UseCoreAsync (bảng §8.1 be-api-controller.md — chuỗi/con số dùng chung khai một
    // chỗ, không gõ tay ở hai nơi).
    private const int LoginPermitLimit = 5;
    private static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(1);
    private const int GlobalPermitLimit = 200;
    private static readonly TimeSpan GlobalWindow = TimeSpan.FromMinutes(1);
    private const int GlobalSegmentsPerWindow = 6;
    // Hàng rào 4 — docs/quy-uoc/be-api-controller.md §6.1: chặn một vòng lặp vẽ lại hỏng ở FE sinh
    // hàng nghìn báo cáo mỗi phút, không phải chặn người dùng thật (vài lỗi rời rạc mỗi phiên).
    private const int ClientErrorsPermitLimit = 20;
    private static readonly TimeSpan ClientErrorsWindow = TimeSpan.FromMinutes(1);

    // Core.Web: controller, envelope JSON, IExceptionHandler, cookie scheme, CORS, rate limit,
    // antiforgery — docs/quy-uoc/be-architecture.md §5.1.
    public static IServiceCollection AddCoreWeb(this IServiceCollection services, IHostEnvironment environment)
    {
        // Swagger theo môi trường — be-api-controller.md §8.2 — vào ở pha có Swagger.

        // TimeProvider.System không tự đăng ký vào DI — cần khai tường minh cho mọi nơi cần đồng hồ
        // hệ thống (vd. DiagnosticsController, AuthController), để test thay được bằng FakeTimeProvider.
        services.TryAddSingleton(TimeProvider.System);

        services.AddControllers(options =>
        {
            // Filter TOÀN CỤC — tự soi EndpointMetadata, chỉ can thiệp khi action mang attribute
            // tương ứng (docs/quy-uoc/be-api-controller.md §4.2).
            options.Filters.Add<RequirePermissionFilter>();
            options.Filters.Add<RequireSystemOperatorFilter>();

            // Endpoint thử của B0 chỉ có ngoài Production — docs/contracts/diagnostics.md.
            if (environment.IsProduction())
                options.Conventions.Add(new DiagnosticsOutsideProductionConvention());

            // Nullable bật toàn solution -> mặc định MVC tự gắn [Required] cho mọi `string` không-null của body,
            // chặn trường thiếu/null TRƯỚC validator và ra nhầm FORMAT. "Thiếu" là việc của validator (REQUIRED) —
            // be-api-controller.md §2.4 chỉ giao cho model binding phần body không parse được, sai kiểu.
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        }).ConfigureApiBehaviorOptions(options =>
        {
            // Lỗi model binding ra envelope như mọi lỗi khác — be-api-controller.md §2.4.
            options.InvalidModelStateResponseFactory = ModelBindingProblemFactory.Create;
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            // Khoá fieldErrors giữ PascalCase, khớp tên property C# — be-api-controller.md §2.3.
            options.JsonSerializerOptions.DictionaryKeyPolicy = null;
        });

        services.AddAuthorization();

        // AllowedOrigins đọc TRỄ qua IOptions<CoreAuthOptions> — CÙNG khuôn với cấu hình cookie bên
        // dưới, không BuildServiceProvider() giữa chừng (be-api-controller.md §7.3).
        //
        // WithExposedHeaders: header nào FE phải ĐỌC ĐƯỢC từ script. Retry-After của 429 (§6.5); Content-Disposition để FE
        // lấy tên tệp khi tải bản xuất dạng blob qua HttpClient (ADR-0062 quyết định 4) — không khai thì trình duyệt giấu.
        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CoreAuthOptions>>((corsOptions, authOptions) =>
                corsOptions.AddPolicy(CorsPolicyNames.Default, policy => policy
                    .WithOrigins(authOptions.Value.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials() // bắt buộc để cookie đi qua
                    .WithExposedHeaders("Retry-After", "Content-Disposition")));

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        // Tên cookie phụ thuộc CoreAuthOptions — không đặt được trực tiếp trong AddAntiforgery ở
        // trên (chưa sẵn sàng lúc đó); cùng khuôn IOptions.Configure<TDep> dùng xuyên suốt file này.
        services.AddOptions<AntiforgeryOptions>()
            .Configure<IOptions<CoreAuthOptions>>(
                (antiforgeryOptions, authOptions) => antiforgeryOptions.Cookie.Name = authOptions.Value.AntiforgeryCookieName);

        // Header chuyển tiếp — be-architecture.md §3.1 ràng buộc 4, be-api-controller.md §6.3 ràng buộc 1. XOÁ
        // danh sách tin cậy mặc định của framework (loopback) rồi nạp đúng thứ đã khai ở Core:Network:*.
        //
        // ⚠️ Rỗng = không tin nguồn nào phải được ép BẰNG TAY: ForwardedHeadersMiddleware chỉ đối chiếu nguồn khi
        // ít nhất một trong hai danh sách có mục — cả hai rỗng mà vẫn bật cờ thì nó tin MỌI nguồn, ai cũng tự đặt
        // được IP và scheme (ForwardedHeadersTrustTests.NothingDeclared_*). Nên chưa khai proxy nào thì tắt hẳn.
        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<CoreNetworkOptions>>((forwarded, network) =>
            {
                forwarded.KnownProxies.Clear();
                forwarded.KnownIPNetworks.Clear();

                foreach (var proxy in network.Value.KnownProxies)
                    forwarded.KnownProxies.Add(IPAddress.Parse(proxy));

                foreach (var cidr in network.Value.KnownNetworks)
                    forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));

                forwarded.ForwardedHeaders = forwarded.KnownProxies.Count + forwarded.KnownIPNetworks.Count > 0
                    ? ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    : ForwardedHeaders.None;
            });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // OnRejected dựng envelope TAY — status KHÔNG suy từ ErrorType.BusinessRule (422), mà
            // đặt trực tiếp — be-api-controller.md §2.4, §6.1 hàng "OnRejected của rate limiter".
            options.OnRejected = async (context, ct) =>
            {
                // Số giây lấy từ CHÍNH bộ đếm đã chặn, không giả định — docs/contracts/auth.md §10. Hai policy cửa sổ cố
                // định (login, client-errors) gắn metadata RetryAfter vào lease bị từ chối; SlidingWindowRateLimiter thì
                // KHÔNG gắn (TryGetMetadata trả false) — nên lease thiếu metadata chỉ có thể là của GlobalLimiter, limiter
                // cửa sổ trượt duy nhất khai ở đây, và số giây là cửa sổ của chính nó.
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var fromLease)
                    ? fromLease
                    : GlobalWindow;

                // MỘT chuỗi cho cả header lẫn messageParams — hai nơi không thể lệch nhau (be-api-controller.md §6.3
                // ràng buộc 6: màn tắt toast chỉ có envelope trong tay, không có header).
                var retryAfterSeconds = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds;
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    Envelope.Failure(
                        SecurityErrors.RateLimitExceeded.WithParams(("RetryAfterSeconds", retryAfterSeconds)),
                        context.HttpContext.TraceIdentifier),
                    ct);
            };

            // Hàng rào 1 — theo IP, riêng đăng nhập — be-api-controller.md §6.1, §6.2.
            options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LoginPermitLimit,
                    Window = LoginWindow,
                }));

            // Hàng rào 4 — riêng docs/contracts/client-errors.md, theo IP.
            options.AddPolicy("client-errors", ctx => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = ClientErrorsPermitLimit,
                    Window = ClientErrorsWindow,
                }));

            // Hàng rào 2 — giới hạn NỀN cho MỌI request, kể cả endpoint không khai policy nào.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = GlobalPermitLimit,
                        Window = GlobalWindow,
                        SegmentsPerWindow = GlobalSegmentsPerWindow,
                    }));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<ITenantContext, HttpContextTenantContext>();
        services.AddScoped<IClientAddressAccessor, HttpContextClientAddressAccessor>();
        // AddProblemDetails() bắt buộc đi kèm — middleware của UseExceptionHandler() đòi có nó
        // (hoặc ExceptionHandlingPath) lúc khởi động, dù CoreExceptionHandler luôn xử lý (trả true)
        // nên nhánh ProblemDetails không bao giờ thật sự chạy tới.
        services.AddExceptionHandler<CoreExceptionHandler>();
        services.AddProblemDetails();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return SecurityEnvelopeWriter.WriteEnvelopeAsync(context.HttpContext, SecurityErrors.NotAuthenticated);
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return SecurityEnvelopeWriter.WriteEnvelopeAsync(context.HttpContext, SecurityErrors.Forbidden);
                };

                // Phép kiểm phiên ở MỌI request — docs/quy-uoc/be-api-controller.md §7.4. Mở phạm vi
                // ngữ cảnh thực thi bằng TenantId từ claim của CHÍNH principal đang kiểm (allowlist
                // luật A12) rồi gọi IIdentityService + ITenantLookup trong CÙNG một scope.
                options.Events.OnValidatePrincipal = ValidatePrincipalAsync;
            });

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<CoreAuthOptions>>((cookieOptions, authOptions) =>
            {
                cookieOptions.Cookie.Name = authOptions.Value.CookieName;
                cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(authOptions.Value.SessionMinutes);
            });

        return services;
    }

    private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var userIdValue = principal?.FindFirst(CoreClaimTypes.UserId)?.Value;
        var tenantIdValue = principal?.FindFirst(CoreClaimTypes.TenantId)?.Value;
        var stamp = principal?.FindFirst(CoreClaimTypes.SecurityStamp)?.Value;
        var issuedAtValue = principal?.FindFirst(CoreClaimTypes.IssuedAt)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(tenantIdValue, out var tenantId) ||
            stamp is null ||
            !DateTimeOffset.TryParse(issuedAtValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var issuedAt))
        {
            context.RejectPrincipal();
            return;
        }

        var services = context.HttpContext.RequestServices;
        var ct = context.HttpContext.RequestAborted;

        // Trần TUYỆT ĐỐI của phiên — docs/quy-uoc/be-api-controller.md §7.4. Quá trần thì từ chối
        // DÙ phiên đang được gia hạn đều, không đợi kiểm stamp/tenant.
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var authOptions = services.GetRequiredService<IOptions<CoreAuthOptions>>().Value;
        if (timeProvider.GetUtcNow() - issuedAt > TimeSpan.FromHours(authOptions.SessionAbsoluteHours))
        {
            context.RejectPrincipal();
            return;
        }

        var scopeFactory = services.GetRequiredService<IExecutionContextScope>();
        using var scope = scopeFactory.Enter(tenantId, userId, principal!.FindFirst(CoreClaimTypes.UserName)?.Value);

        var identityService = services.GetRequiredService<IIdentityService>();
        var sessionValid = await identityService.IsSessionValidAsync(userId, stamp, ct);

        if (!sessionValid)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var tenantLookup = services.GetRequiredService<ITenantLookup>();
        var tenant = await tenantLookup.FindByIdAsync(tenantId, ct);

        if (tenant is null || !tenant.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        // Gia hạn ở MỌI request mang phiên hợp lệ — không dựa vào SlidingExpiration.
        context.ShouldRenew = true;
    }
}
