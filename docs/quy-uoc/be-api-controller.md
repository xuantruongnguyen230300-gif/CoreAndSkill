---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — Controller, envelope, `Result` → HTTP, bảo mật đường vào

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi code mẫu là khuôn cho `src/BE` sẽ xây ở giai đoạn 2.
>
> Đây là file chủ của mọi mốc `— định nghĩa gốc` bên dưới ([`../OWNERSHIP.md`](../OWNERSHIP.md) §3). File khác
> chỉ được trỏ tới đây.
>
> 📖 Lý do, bẫy, ví dụ mở rộng của từng mục — cùng số §: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md).

---

## 1. Ánh xạ `Result` → HTTP — đúng MỘT chỗ, KHÔNG reflection

### 1.1 Bảng ánh xạ ErrorType → HTTP — định nghĩa gốc

| `ErrorType` | HTTP | Dùng khi |
| --- | ---: | --- |
| `Validation` | 400 | Sai định dạng/ràng buộc tính được từ payload — luôn kèm `fieldErrors` |
| `Unauthorized` | 401 | Chưa đăng nhập, hoặc phiên đã hết hạn/bị thu hồi. **Chỉ hạ tầng được phát** — không handler, không Domain (luật R9, [`../adr/0027-errortype-unauthorized.md`](../adr/0027-errortype-unauthorized.md)) |
| `Forbidden` | 403 | Đã đăng nhập nhưng thiếu quyền |
| `NotFound` | 404 | Resource **chính của route** không tồn tại (kể cả đã soft delete) |
| `Conflict` | 409 | Trùng giá trị unique, hoặc xung đột concurrency |
| `BusinessRule` | 422 | Vi phạm quy tắc nghiệp vụ; tham chiếu **trong payload** không hợp lệ |
| `Unexpected` | 500 | `CORE.SYSTEM.UNEXPECTED` — bug/hạ tầng, không lộ chi tiết ra response. **Chỉ `IExceptionHandler` phát** (§2.4) — không handler, không Domain |

`403` không được gộp về `422`.

### 1.2 Hiện thực — switch expression, không reflection

```csharp
// Core.Web/Http/ResultToHttpMapper.cs
public static class ResultToHttpMapper
{
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation   => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
        ErrorType.NotFound     => StatusCodes.Status404NotFound,
        ErrorType.Conflict     => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Unexpected   => StatusCodes.Status500InternalServerError,
    };
}
```

**Không có nhánh `_`** — thiếu nhánh phải là **lỗi biên dịch**: cảnh báo CS8509 + `TreatWarningsAsErrors` (luật T4).

**Tắt riêng CS8524** — cảnh báo cho giá trị enum **không tên** — bằng `NoWarn` ở `Directory.Build.props`, cùng
chỗ với `TreatWarningsAsErrors`.

Luật `EveryErrorType_MapsTo_AValidHttpStatus` ([`../RULES.md`](../RULES.md) §5) canh: mọi giá trị của
`ErrorType` phải ánh xạ sang một status hợp lệ, tại **đúng một chỗ**.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §1.2

### 1.3 ⚠️ Vì sao CẤM reflection ở cầu nối này

Luật `ResultToHttpMapper_MustNotUse_Reflection` ([`../RULES.md`](../RULES.md) §5) quét mã nguồn của cầu
nối này và cấm `System.Reflection`, `GetMethod`, `Invoke`, `MakeGenericType`, `Activator.CreateInstance`.

Né reflection phía Application: [`be-cqrs-handler.md`](be-cqrs-handler.md) §5.4.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §1.3

---

## 2. Envelope trả về

### 2.1 Hình dạng envelope — định nghĩa gốc

Ba kiểu dưới đây là **nguồn duy nhất** của hình dạng envelope — card trong [`../contracts/`](../contracts/) và
tài liệu FE trỏ về đây, không chép lại khối JSON mẫu ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

`ApiEnvelope` sống ở **`Core.Web`**, không ở `Core.Contracts` ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2).

```csharp
// Core.Web/Http/ApiEnvelope.cs
public sealed record ApiEnvelope<T>(bool Success, T? Data, ApiError? Error, string TraceId);

public sealed record ApiError(
    string Code, string Type, string Message,
    IReadOnlyDictionary<string, string>? MessageParams,
    IReadOnlyDictionary<string, IReadOnlyList<ApiFieldError>>? FieldErrors);

public sealed record ApiFieldError(string Code, IReadOnlyDictionary<string, string>? MessageParams);
```

```csharp
// Core.Web/Http/Envelope.cs — chỗ DUY NHẤT dựng ApiEnvelope<T>
public static class Envelope
{
    public static ApiEnvelope<T> Success<T>(T? data, string traceId) => new(true, data, null, traceId);

    public static ApiEnvelope<object> Failure(Error error, string traceId) => new(false, null, ToApiError(error), traceId);

    // Message qua MessageTemplateRenderer.Render(template, params); Params / FieldErrors rỗng ⇒ null;
    // khoá fieldErrors so sánh Ordinal (§2.3)
    private static ApiError ToApiError(Error error) => ...;
}
```

Thành công:

```json
{ "success": true, "data": { "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234", "userName": "an.nv" },
  "error": null, "traceId": "4bf92f3577b34da6a3ce929d0e0e4736" }
```

Lỗi nghiệp vụ:

```json
{ "success": false, "data": null,
  "error": { "code": "CORE.USER.EMAIL_DUPLICATED", "type": "Conflict", "message": "Email 'an@vd.vn' đã được dùng.",
             "messageParams": { "Email": "an@vd.vn" }, "fieldErrors": null },
  "traceId": "0af7651916cd43dd8448eb211c80319c" }
```

Lỗi validation:

```json
{ "success": false, "data": null,
  "error": { "code": "CORE.VALIDATION.FAILED", "type": "Validation", "message": "Dữ liệu gửi lên không hợp lệ.",
             "messageParams": null,
             "fieldErrors": {
               "Email": [ { "code": "CORE.VALIDATION.FORMAT", "messageParams": null } ],
               "UserName": [ { "code": "CORE.VALIDATION.MAX_LENGTH", "messageParams": { "MaxLength": "64" } } ] } },
  "traceId": "5b8aa5a2d2c872e8321cf37308d69df2" }
```

Mã trong `fieldErrors` lấy từ catalog validation dùng chung — [`be-cqrs-handler.md`](be-cqrs-handler.md) §7.1.

`traceId` theo **W3C Trace Context**: 32 ký tự hex, chính là `Activity.Current.TraceId` của request — lấy từ
header `traceparent` khi phía gọi đã gửi, tự sinh khi không. `TraceIdMiddleware` gán nó vào
`HttpContext.TraceIdentifier` ([`be-architecture.md`](be-architecture.md) §2.1).

### 2.2 Bốn luật về envelope

1. **Mọi response dưới `/api` đều mang envelope này.** Không có ngoại lệ.
2. **`traceId` luôn có mặt**, kể cả khi thành công.
3. **`message` là dev-facing.** Client dựng câu hiển thị từ `code` + `messageParams`; BE không được coi
   `message` là hợp đồng — [`be-cqrs-handler.md`](be-cqrs-handler.md) §8.
4. **`error` là `null` khi thành công, `data` là `null` khi lỗi.** Không bao giờ cả hai cùng có giá trị.
   Luật `HandBuiltErrorEnvelope_AlwaysCarries_ABusinessCode` canh: envelope lỗi dựng tay vẫn phải mang `code`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §2.2

### 2.3 ⚠️ Casing — payload camelCase, khoá của `fieldErrors` PascalCase

Toàn bộ payload serialize theo `JsonNamingPolicy.CamelCase`, nhưng **khoá** của `fieldErrors` giữ
**PascalCase**, khớp đúng tên property C# của DTO (`Email`, `UserName`). Cơ chế: `DictionaryKeyPolicy` để
`null` và **cố ý không set**.

> **Đừng "sửa cho nhất quán"** — FE tra `fieldErrors['Email']` ([`fe-api-client.md`](fe-api-client.md)).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §2.3

### 2.4 Các đường dựng envelope — danh sách đầy đủ

| Đường | Xử lý | Ở đâu |
| --- | --- | --- |
| `ApiControllerBase.HandleResult` | Mọi `Result` từ handler | `Core.Web` |
| `IExceptionHandler` của Core, nối bằng `UseExceptionHandler()` | `DbUpdateConcurrencyException` → 409 `CommonErrors.ConcurrencyConflict` — **đường duy nhất** dịch lỗi đồng thời, handler không bắt (§5.3 của `be-cqrs-handler.md`: `SaveChangesAsync` nằm trong behavior). `LoginAttemptLimitExceededException` → 429 `SecurityErrors.RateLimitExceeded` kèm `Retry-After`, §6.5. Exception còn lại → 500 `CommonErrors.Unexpected`, không lộ chi tiết | `Core.Web` |
| `OnRedirectToLogin` của cookie scheme | 401 `SecurityErrors.NotAuthenticated` — chưa đăng nhập hoặc phiên hết hạn; mọi `ChallengeResult` đi qua đây | `Core.Web`, §7.4 |
| `OnRedirectToAccessDenied` của cookie scheme | 403 `SecurityErrors.Forbidden` — mọi `ForbidResult` đi qua đây | `Core.Web`, §7.4 |
| `AntiforgeryValidationMiddleware` | 403 `SecurityErrors.OriginRejected` — request ghi mang `Origin` ngoài allowlist; 403 `SecurityErrors.CsrfRejected` — thiếu hoặc sai `X-XSRF-TOKEN`. Chặn trước khi tới phân quyền | `Core.Web`, §7.2 |
| `ModelBindingProblemFactory` | Body không parse được, kiểu sai, thiếu tham số bắt buộc — lỗi xảy ra **trước** khi vào action | `Core.Web` |
| `OnRejected` của rate limiter | 429 `SecurityErrors.RateLimitExceeded` — request bị chặn **trước** khi tới action nào, nên không có `Result` để bọc | `Core.Web`, §6 |
| `PasswordChangeRequiredMiddleware` | 403 `SecurityErrors.PasswordChangeRequired` — chặn trước khi vào action, xem [`be-architecture.md`](be-architecture.md) §3.1. Đọc cờ từ claim `CoreClaimTypes.MustChangePassword` (§7.4), **không** truy DB; cờ trong cookie chỉ đổi khi cookie được cấp lại sau đổi mật khẩu | `Core.Web` |
| `EnvelopeMiddleware` | Response do **hạ tầng định tuyến** sinh — 404 `SecurityErrors.RouteNotFound`, 405 `SecurityErrors.RouteMethodNotAllowed`; chúng có thân rỗng nên không đi qua đường nào ở trên | `Core.Web` |

> **Bảng này là danh sách đầy đủ** ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6). Luật
> `HandBuiltErrorEnvelope_AlwaysCarries_ABusinessCode` áp vào từng dòng; mọi `Error` khai ở `SecurityErrors` (§7.4) hoặc `CommonErrors` ([`be-cqrs-handler.md`](be-cqrs-handler.md) §7.1).

Ba ràng buộc của `EnvelopeMiddleware`:

1. **Chỉ bọc tiền tố `/api`** — allowlist, không blocklist.
2. **Không ghi đè response đã có thân.** Phép thử là `HasStarted || ContentLength.HasValue
   || ContentType is { Length: > 0 }` — giữ nguyên 404 mà handler **chủ động** trả (`CORE.USER.NOT_FOUND`).
3. **Không dùng `MapFallback`.**

Hai mã định tuyến `CORE.ROUTE.NOT_FOUND` và `CORE.ROUTE.METHOD_NOT_ALLOWED` cùng mang `type: "NotFound"`; với 405,
status **không** suy từ `type` — như 429 ở §6, hạ tầng đặt status, `EnvelopeMiddleware` chỉ ghi thân, không qua
`ResultToHttpMapper`. Ánh xạ mã → `type` → HTTP: [`../contracts/auth.md`](../contracts/auth.md) §11.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §2.4

---

## 3. `ApiControllerBase`

```csharp
// Core.Web/Controllers/ApiControllerBase.cs
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(Envelope.Success(result.Value, HttpContext.TraceIdentifier)) : Failure(result.Error!);

    protected IActionResult HandleResult(Result result)
        => result.IsSuccess ? Ok(Envelope.Success<object>(null, HttpContext.TraceIdentifier)) : Failure(result.Error!);

    private IActionResult Failure(Error error)
        => StatusCode(ResultToHttpMapper.ToStatusCode(error.Type), Envelope.Failure(error, HttpContext.TraceIdentifier));
}
```

| Trách nhiệm | Ghi chú |
| --- | --- |
| Mang `[Authorize]` — **fail-closed** | Endpoint công khai phải khai `[AllowAnonymous]` tường minh. Quên khai = bị chặn, không phải lộ ra. Mọi action khác khai **đúng một** mức phân quyền — §4.2 |
| Cung cấp `HandleResult` | Chỗ **duy nhất** controller đặt status code |
| Gắn `traceId` | Lấy từ `HttpContext.TraceIdentifier` — `TraceIdMiddleware` đã gán giá trị W3C `Activity.Current.TraceId` vào đó (§2.1). Không tự sinh |

**Không** khai `[ApiController]` và `[Route]` ở lớp cơ sở: chúng khai ở **từng** controller, route viết
**tường minh** (`[Route("api/v1/core/users")]`), không dùng token `[controller]`. Tiền tố bắt buộc gồm cả số
phiên bản — §8.1.

Luật `EveryController_Inherits_ApiControllerBase` ([`../RULES.md`](../RULES.md) §6) canh.

### 3.1 Controller mẫu

```csharp
// Core.Web/Controllers/UsersController.cs
[ApiController]
[Route("api/v1/core/users")]
public sealed class UsersController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(CorePermissions.UserRead)]
    public async Task<IActionResult> GetList([FromQuery] GetUsersListQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission(CorePermissions.UserRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new GetUserByIdQuery(id), ct));

    [HttpPost]
    [RequirePermission(CorePermissions.UserWrite)]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken ct)
        => HandleResult(await mediator.Send(command, ct));
}
```

Ba luật cho mọi controller:

- **Không logic nghiệp vụ.** Chỉ `Send` rồi `HandleResult` — **trừ** action phát hoặc cấp lại cookie phiên (§7.4).
- **Không tự `try/catch`**, không `return StatusCode(500, ...)`.
- **Không inject repository hay `DbContext`.** Chỉ `ISender` — ngoại lệ có tên: action phát hoặc cấp lại
  cookie phiên inject thêm `ISessionPrincipalFactory` và `TimeProvider` (§7.4).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §3.1

---

## 4. Phân quyền — permission, KHÔNG có hằng số role

### 4.1 Luật gốc

> **Trong `Core.*` không tồn tại một hằng số role nào.** Role là **dữ liệu** trong DB.
> Phân quyền kiểm bằng **permission**, không bằng tên role.

Luật `Core_MustNotDeclare_RoleConstants` và `Authorization_MustCheck_Permission_NotRoleName`
([`../RULES.md`](../RULES.md) §6) canh hai vế.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §4.1

### 4.2 Ba mức phân quyền của endpoint — mỗi action đúng MỘT mức

`[Authorize]` ở lớp cơ sở chỉ đòi **đã đăng nhập**, nên mỗi action không `[AllowAnonymous]` khai **đúng một**
trong ba mức (luật S11 ở [`../RULES.md`](../RULES.md),
[ADR-0024](../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md)):

| Attribute | Dùng cho | Kiểm gì |
| --- | --- | --- |
| `[RequirePermission("<code>")]` | Endpoint nghiệp vụ của đơn vị | Khoá quyền qua `IPermissionChecker` — ma trận vai trò, hoặc cờ `has_permission_bypass` (§4.3) |
| `[RequireSystemOperator]` | Khu quản trị hệ thống — [`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md) | Cờ `is_system_operator` của tài khoản. **Không** đi qua ma trận quyền |
| `[AuthenticatedOnly("<lý do>")]` | Endpoint **của bản thân**: hồ sơ, đổi mật khẩu, `me`, đăng xuất | Chỉ cần đã đăng nhập. Chuỗi lý do bắt buộc — người review đọc được ngay tại chỗ vì sao endpoint này không đòi quyền |

S11 bắt cả ca không khai mức nào lẫn ca khai hai mức — bằng ArchTest, lúc build.

**S11 đếm theo mức, không theo số attribute** (cùng ADR):

| Ca | S11 tính | Filter làm gì |
| --- | --- | --- |
| Nhiều `[RequirePermission]` trên cùng một action | **Một** mức | Đòi **tất cả** khoá — vòng lặp trong `RequirePermissionFilter` dưới đây |
| Mức khai ở cấp controller | Áp cho **mọi** action của controller; action không tự khai gì vẫn đạt | `EndpointMetadata` của action đã gồm attribute cấp controller |
| `[RequirePermission]` ở cả cấp controller lẫn cấp action | Cùng một mức — khoá hai cấp cộng dồn | Đòi tất cả khoá của cả hai cấp |
| Action khai mức **khác** mức cấp controller | **Vi phạm** | — Controller trộn nhiều mức thì khai mức ở từng action, không khai ở cấp controller |
| Action mang `[AllowAnonymous]` trong controller đã khai mức | **`[AllowAnonymous]` thắng** — S11 bỏ qua action này, chỉ sau khi kiểm nó có tên trong allowlist ẩn danh (§5) | Không kiểm gì — thấy `IAllowAnonymous` trong `EndpointMetadata` thì trả về ngay |

```csharp
// Core.Web/Permissions/RequirePermissionAttribute.cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}

// Core.Web/Permissions/RequireSystemOperatorAttribute.cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireSystemOperatorAttribute : Attribute
{
}

// Core.Web/Permissions/AuthenticatedOnlyAttribute.cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuthenticatedOnlyAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}

// Core.Application/Permissions/IPermissionChecker.cs — seam
public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken ct);

    // Tập quyền hiệu lực (§4.3)
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct);
}
```

`RequirePermissionFilter` (`Core.Web/Filters/`, `internal sealed`, `IAsyncAuthorizationFilter`, nhận
`IPermissionChecker` + `ICurrentUser`) — bốn bước trong `OnAuthorizationAsync`, theo thứ tự:

1. `EndpointMetadata` có `IAllowAnonymous` ⇒ trả về ngay (bảng trên).
2. Gom `Key` của mọi `RequirePermissionAttribute` trong `EndpointMetadata`; rỗng ⇒ trả về (action mang mức khác).
3. `currentUser.UserId` rỗng ⇒ `ChallengeResult`.
4. Từng khoá: `HasPermissionAsync(userId, key, ct)` sai ⇒ `ForbidResult`.

Hai kết quả của filter **không tự dựng envelope** — `ChallengeResult` và `ForbidResult` đi qua hai cookie
event ở §7.4 (bảng §2.4). **Không** dùng `UnauthorizedResult`.

Filter của `[RequireSystemOperator]` cùng khuôn, đọc cờ từ claim `CoreClaimTypes.IsSystemOperator` (§7.4) — **không**
truy DB. `[AuthenticatedOnly]` không cần filter: `[Authorize]` của lớp cơ sở đã đòi đăng nhập.

> 📖 Lý do, bẫy, ví dụ mở rộng (gồm khối `RequirePermissionFilter` đầy đủ): [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §4.2

### 4.3 Ma trận quyền theo resource

Permission key có khuôn `<tài nguyên>.<hành động>`, khai bằng hằng số **chuỗi** — không phải enum:

> 📖 **Danh mục khoá của Core — tập đầy đủ kèm ý nghĩa từng khoá: đọc
> [`../database/schema-core.md`](../database/schema-core.md) §5.2** (file chủ — [`../OWNERSHIP.md`](../OWNERSHIP.md) §2).
> Mục này chỉ nêu **cách khai** hằng số.

```csharp
// Core.Application/Permissions/CorePermissions.cs — TRÍCH, không phải danh mục đầy đủ
public static class CorePermissions
{
    public const string UserRead  = "core.user.read";
    public const string UserWrite = "core.user.write";
    // …
}
```

Mỗi hằng số ở đây phải khớp **đúng chuỗi** một dòng `core.permission.code`.

Ma trận sống trong DB, không trong code:

| Bảng | Nội dung |
| --- | --- |
| `core.app_role` | Vai trò — **dữ liệu**, tạo/sửa/xoá được từ màn quản trị |
| `core.permission` | Danh mục permission key — ghi bằng migration idempotent, đối chiếu hai chiều với hằng số; tiến trình ứng dụng chỉ đọc ([`be-architecture.md`](be-architecture.md) §1.1) |
| `core.role_permission` | Ma trận: vai trò nào có permission nào |

Tên bảng số **ít** ([`../database/schema-core.md`](../database/schema-core.md) §2.1). `IPermissionChecker` tra qua
ba bảng đó theo vai trò của người dùng.

**Cờ `has_permission_bypass` là nhánh DUY NHẤT bỏ qua ma trận**
([ADR-0021](../adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md)):
`IPermissionChecker` đọc cờ ở đúng một chỗ trong hiện thực: tập quyền hiệu lực của tài khoản mang cờ là
toàn bộ danh mục hiện có, `HasPermissionAsync` trả lời từ chính tập đó. Cờ **không** gỡ bộ lọc đơn vị, và
Core vẫn không có vai trò "toàn quyền" nào (luật S1).

Phản hồi phiên lấy `permissions` từ `GetEffectivePermissionsAsync` — cùng tập filter kiểm, tính lúc trả lời;
handler phiên **không** đọc cờ ([`../contracts/auth.md`](../contracts/auth.md)). FE **không** có nhánh riêng
cho cờ: kiểm khoá như với mọi tài khoản.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §4.3

### 4.4 Điều gì `RequirePermission` KHÔNG làm

Nó kiểm quyền ở mức **endpoint**, không ở mức **bản ghi**. Kiểm theo bản ghi thuộc về handler và trả
`ErrorType.Forbidden`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §4.4

---

## 5. `[AllowAnonymous]` — allowlist khai tường minh

`ApiControllerBase` mang `[Authorize]` (§3), nên mỗi `[AllowAnonymous]` phải nằm trong một allowlist khai ở
một chỗ:

```csharp
// Core.Web/Security/AnonymousEndpointAllowlist.cs
public static class AnonymousEndpointAllowlist
{
    public static readonly IReadOnlySet<string> Endpoints = new HashSet<string>(StringComparer.Ordinal)
    {
        "POST /api/v1/core/auth/login",
        "GET /api/v1/core/antiforgery/token",
        "POST /api/v1/core/client-errors",
        "GET /health/live",
        "GET /health/ready",
    };
}
```

Luật `EveryAllowAnonymous_IsOn_TheAllowlist` ([`../RULES.md`](../RULES.md) §6) đối chiếu mọi
`[AllowAnonymous]` trong solution với danh sách này. Thêm một dòng vào allowlist là một thay đổi phải
giải trình trong PR.

**`[AllowAnonymous]` trên một action thắng mức khai ở cấp controller** (bảng §4.2) và phải có tên trong danh
sách trên; ngoài danh sách thì đỏ ở cả S4 lẫn S11.

**Không có endpoint quên mật khẩu hay đặt lại mật khẩu bằng token ở v1** — quản trị đặt lại hộ
([ADR-0029](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)).
Thêm lại một trong hai là đảo một ADR, không phải thêm một dòng allowlist.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §5

---

## 6. Rate limiting

### 6.1 Các hàng rào

| # | Hàng rào | Phân vùng theo | Kiểu cửa sổ | Chặn được gì |
| --- | --- | --- | --- | --- |
| 1 | Policy `login` | **IP** | `FixedWindow` | Brute-force từ một nguồn |
| 2 | Giới hạn nền, mọi request | **IP** | `SlidingWindow` | Vòng lặp retry hỏng ở FE, tab treo gọi API liên tục |
| 3 | Trong handler đăng nhập — **không** ở `UseRateLimiter` | **`LoginPartitionKey`** (§6.5) | Cửa sổ trượt | Brute-force **phân tán** nhiều IP cùng nhắm một tài khoản |
| 4 | Policy `client-errors` | **IP** | `FixedWindow` | Một lỗi trong vòng lặp vẽ lại ở FE sinh hàng nghìn báo cáo mỗi phút — [`../contracts/client-errors.md`](../contracts/client-errors.md) |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §6.1

### 6.2 ⚠️ Chọn đúng overload — bẫy đã dính thật

**Không dùng overload `AddFixedWindowLimiter("login", …)`** — dùng `AddPolicy` + `RateLimitPartition`:

```csharp
// ✅ ĐÚNG
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
                { PermitLimit = 200, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6 }));
});
```

Hàng rào 3 không có trong khối trên — §6.5. Action đăng nhập gắn policy bằng `[EnableRateLimiting("login")]`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §6.2

### 6.3 Ràng buộc

1. **Phân vùng không đọc header nào do client gửi** — chỉ kết nối TCP thật. Proxy tin cậy khai **tường
   minh**; vị trí và quy tắc cấu hình: [`be-architecture.md`](be-architecture.md) §3.1, ràng buộc 4.
2. **Hai kiểu cửa sổ ở §6.1 là cố ý.** Đừng đổi cho "nhất quán".
3. **Không rate-limit health check** (`/health/live`, `/health/ready`). Endpoint phát token antiforgery
   cũng miễn.
4. **Đếm trong bộ nhớ của từng instance là hạn mức nhân lên.** v1 chạy một instance nên chấp nhận được;
   đây là **điều kiện bắt buộc phải giải trước khi mở instance thứ hai**.
5. **Thông điệp từ chối không tiết lộ ngưỡng.** Trả mã lỗi chung, không kèm con số.
6. **429 phải ra đúng envelope**, mang `SecurityErrors.RateLimitExceeded` (§7.4).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §6.3

### 6.4 Con số là ước lượng, không phải số đo

Hạn mức trên là mặc định cho vài chục người dùng đồng thời, mỗi người một IP. Sau khi chạy thật, đếm số
lần 429 trong log: có 429 **không** kèm sự cố FE → nới lên; không có 429 nào trong nhiều tuần → **vẫn giữ**.
Rate limit theo IP **không phải** phòng thủ DDoS — việc đó thuộc tầng mạng.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §6.4

### 6.5 `LoginPartitionKey` — định nghĩa gốc

Hàng rào 3 (§6.1) kiểm **trong handler đăng nhập**, theo khoá `LoginPartitionKey`, **trước** khi kiểm mật
khẩu; thứ tự pipeline ở [`be-architecture.md`](be-architecture.md) §3.1 không đổi.

Khoá gồm **hai** phần, cả hai đã chuẩn hoá:

| Phần | Chuẩn hoá theo |
| --- | --- |
| Mã đơn vị | Đúng quy tắc tra đơn vị lúc đăng nhập — cột `code` ở [`../database/schema-core.md`](../database/schema-core.md) §1.3 |
| Tên đăng nhập | Đúng bộ chuẩn hoá Identity dùng cho `normalized_user_name` (`ILookupNormalizer`) |

Hai phần ghép sao cho hai cặp khác nhau **không bao giờ** cho cùng một khoá. Card đăng nhập trỏ về
đây, không khai lại ([`../contracts/auth.md`](../contracts/auth.md) §10).

Hạn mức mặc định: **10** lần trong cửa sổ trượt **5 phút** cho mỗi khoá — ước lượng theo §6.4; bộ đếm
trong bộ nhớ tiến trình nên ràng buộc 4 ở §6.3 áp nguyên.

**Vượt hạn mức — đường ra.** Handler đăng nhập gọi seam `ILoginAttemptLimiter`
([`be-architecture.md`](be-architecture.md) §1.1); `IExceptionHandler` của Core
bắt `LoginAttemptLimitExceededException` và trả **429 `CORE.RATE_LIMIT.EXCEEDED` kèm `Retry-After`** — cùng một
`Error` (`SecurityErrors.RateLimitExceeded`, §7.4) với hai hàng rào theo IP ở `OnRejected` (§2.4); status 429 đặt
trực tiếp, không qua `ResultToHttpMapper` ([`../contracts/auth.md`](../contracts/auth.md) §10).

```csharp
// Core.Application/Identity/ILoginAttemptLimiter.cs — seam; hiện thực ở Core.Infrastructure
public interface ILoginAttemptLimiter
{
    ValueTask EnsureAttemptAllowedAsync(string tenantCode, string userName, CancellationToken ct);
}

// Core.Application/Identity/LoginAttemptLimitExceededException.cs
public sealed class LoginAttemptLimitExceededException(TimeSpan retryAfter) : Exception
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
```

| Quyết định |
| --- |
| Đường ra là exception, không phải `Result` — `ResultToHttpMapper` không đưa `ErrorType` nào về 429 (§1.1), và `Result` không mang header |
| **Hiện thực** của seam ném, handler không ném (luật R1). Kiểu exception khai ở `Core.Application` để `Core.Web` bắt được mà không biết hiện thực |
| Seam nhận mã đơn vị và tên đăng nhập **thô**; khoá dựng trong hiện thực, ở `Core.Infrastructure` |
| `Retry-After` là số nguyên giây, làm tròn lên từ `RetryAfter` — lấy từ cửa sổ của bộ đếm, không hardcode |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §6.5

---

## 7. CORS + Cookie `SameSite=Lax` + antiforgery hai lớp

### 7.1 Chuỗi ràng buộc — vì sao vẫn cần antiforgery khi đã có `SameSite`

FE và API ở **hai origin khác nhau nhưng cùng tên miền gốc**
([ADR-0015](../adr/0015-fe-va-api-khac-nguon.md)). Cookie phiên dùng
**`SameSite=Lax`**, **không** cần `SameSite=None`; vẫn cần hai lớp ở §7.2.

> 🛑 **Điều kiện của cả chuỗi: cùng tên miền gốc VÀ cùng scheme.**
>
> | Lệch | Hậu quả |
> | --- | --- |
> | Khác scheme — FE `http`, API `https` | **Khác site.** Cookie `Lax` không được gửi ⇒ đăng nhập xong vẫn 401. Đó là lý do mọi môi trường, kể cả máy dev, chạy HTTPS |
> | Khác tên miền gốc | Cookie thành **bên thứ ba**: phải `SameSite=None`, và trình duyệt có thể chặn hẳn. Đổi điều kiện này là một **ADR mới**, không phải một dòng cấu hình |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §7.1

### 7.2 Hai lớp

Cả hai lớp nằm trong **một** middleware, `AntiforgeryValidationMiddleware` (§2.4; vị trí:
[`be-architecture.md`](be-architecture.md) §3.1; `Core.Web/Security/`, `internal sealed`, nhận `IAntiforgery` +
`IOptions<CoreAuthOptions>`).

| Lớp | Cơ chế | Từ chối bằng | Chặn gì |
| --- | --- | --- | --- |
| 1 | Request ghi có header `Origin` không nằm trong allowlist — cùng allowlist với CORS (§7.3) | 403 `CORE.AUTH.ORIGIN_REJECTED` | Request từ site lạ do trình duyệt phát sinh, vì trình duyệt luôn gắn `Origin` cho request cross-origin và **script không sửa được** header đó |
| 2 | Token antiforgery — mọi `POST`/`PUT`/`PATCH`/`DELETE` phải mang header `X-XSRF-TOKEN` khớp | 403 `CORE.AUTH.CSRF_REJECTED` | Ca lớp 1 bỏ lọt (request không có `Origin`, hoặc allowlist bị cấu hình rộng) |

Hai lớp độc lập: request với `Origin` lạ nhưng token hợp lệ vẫn phải bị **403 `CORE.AUTH.ORIGIN_REJECTED`**.
Cách FE xử lý từng mã: [`../contracts/auth.md`](../contracts/auth.md) §11.

**Phiên hết hạn ra 401, không ra 403** — bước 2 dưới đây cho đi tiếp để `UseAuthorization` trả 401 qua
`OnRedirectToLogin`.

Thứ tự trong `InvokeAsync`:

1. `GET` / `HEAD` / `OPTIONS` / `TRACE` ⇒ `next` (chỉ kiểm method ghi — bẫy thứ ba ở §7.5).
2. Endpoint có `IAuthorizeData`, không `IAllowAnonymous`, và `User` chưa xác thực ⇒ `next` (401 ở `UseAuthorization`).
3. Header `Origin` có mặt và không nằm trong `AllowedOrigins` (so `OrdinalIgnoreCase`) ⇒
   `SecurityEnvelopeWriter.WriteEnvelopeAsync(http, SecurityErrors.OriginRejected)` — lớp 1.
4. `!antiforgery.IsRequestValidAsync(http)` ⇒ `WriteEnvelopeAsync(http, SecurityErrors.CsrfRejected)` — lớp 2.
5. `next`.

`WriteEnvelopeAsync` (§7.4) dùng chung cho hai cookie event và middleware này — status lấy từ `ResultToHttpMapper`,
không gõ tay.

**Endpoint nào cần antiforgery:**

| Loại | Cần? |
| --- | --- |
| Mọi endpoint ghi (`POST`, `PUT`, `PATCH`, `DELETE`) | ✅ |
| Đăng nhập | ✅ — chống ép người dùng đăng nhập vào tài khoản của kẻ tấn công |
| Đăng xuất | ✅ — bị ép đăng xuất là phiền, tuy nhẹ |
| Endpoint đọc (`GET`) | ❌ — nhưng khi đó `GET` **không được** gây thay đổi trạng thái. Một `GET` có tác dụng phụ là lỗ hổng, không phải một lựa chọn thiết kế |

> 📖 Lý do, bẫy, ví dụ mở rộng (gồm khối `AntiforgeryValidationMiddleware` đầy đủ): [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §7.2

### 7.3 CORS

```csharp
services.AddCors(options => options.AddPolicy(CorsPolicyNames.Default, policy =>
{
    policy.WithOrigins(authOptions.AllowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()                         // bắt buộc để cookie đi qua
          .WithExposedHeaders("Retry-After");
}));
```

- Allowlist đọc qua `IOptions<CoreAuthOptions>` — fail-fast lúc khởi động ([`be-architecture.md`](be-architecture.md) §4).
- Header phản hồi mà FE cần đọc phải nằm trong `WithExposedHeaders` — `Retry-After` của 429 (§6.5) là một.
- **Không bao giờ** `AllowAnyOrigin()` một khi đã có auth thật.

> 🛑 **Không bao giờ dùng CORS thay cho phân quyền.** Không "sửa" lỗi CORS bằng cách phản hồi lại đúng
> `Origin` mà client gửi lên.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §7.3

### 7.4 Cookie phiên

```csharp
// Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs — cookie scheme sống ở Core.Web; khối đầy đủ: ly-do §7.4
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = authOptions.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(authOptions.SessionMinutes);
        // Events: bảng §2.4 và đoạn "Phép kiểm phiên ở mọi request" dưới
    });
```

**Catalog mã của hạ tầng HTTP — định nghĩa gốc.** Mọi mã ở bảng §2.4 khai ở đây.

```csharp
// Core.Web/Security/SecurityErrors.cs — catalog DUY NHẤT của mã mà hạ tầng HTTP phát (nơi phát: bảng §2.4); không trùng AuthErrors (mã CORE.AUTH.* của handler)
internal static class SecurityErrors
{
    public static readonly Error NotAuthenticated = new("CORE.AUTH.NOT_AUTHENTICATED", "Chưa đăng nhập hoặc phiên đã hết hạn.", ErrorType.Unauthorized);

    public static readonly Error Forbidden = new("CORE.AUTH.FORBIDDEN", "Không có quyền thực hiện thao tác này.", ErrorType.Forbidden);

    public static readonly Error OriginRejected = new("CORE.AUTH.ORIGIN_REJECTED", "Nguồn gọi không nằm trong danh sách được phép.", ErrorType.Forbidden);

    public static readonly Error CsrfRejected = new("CORE.AUTH.CSRF_REJECTED", "Thiếu hoặc sai token chống giả mạo.", ErrorType.Forbidden);

    public static readonly Error PasswordChangeRequired = new("CORE.AUTH.PASSWORD_CHANGE_REQUIRED", "Phải đổi mật khẩu trước khi tiếp tục.", ErrorType.Forbidden);

    // Ba mã dưới: status KHÔNG suy từ Type — đặt trực tiếp ở nơi phát (§2.4, §6.5), không qua WriteEnvelopeAsync
    public static readonly Error RateLimitExceeded = new("CORE.RATE_LIMIT.EXCEEDED", "Quá nhiều yêu cầu, thử lại sau.", ErrorType.BusinessRule);

    public static readonly Error RouteNotFound = new("CORE.ROUTE.NOT_FOUND", "Không có đường dẫn này.", ErrorType.NotFound);

    public static readonly Error RouteMethodNotAllowed = new("CORE.ROUTE.METHOD_NOT_ALLOWED", "Phương thức không được hỗ trợ ở đường dẫn này.", ErrorType.NotFound);
}

// Core.Web/Security/SecurityEnvelopeWriter.cs — helper DUY NHẤT ghi envelope lỗi từ hạ tầng
internal static class SecurityEnvelopeWriter
{
    public static Task WriteEnvelopeAsync(HttpContext http, Error error)
    {
        http.Response.StatusCode = ResultToHttpMapper.ToStatusCode(error.Type);
        return http.Response.WriteAsJsonAsync(Envelope.Failure(error, http.TraceIdentifier));
    }
}
```

**Seam dựng principal và kiểu của phiên — định nghĩa gốc.** File khác trỏ về đây, không chép chữ ký.

```csharp
// Core.Application/Auth/ — hiện thực factory ở Core.Infrastructure
public interface ISessionPrincipalFactory
{
    // Không truy vấn gì; issuedAt: khối AuthController dưới
    ClaimsPrincipal Create(LoginOutcome outcome, DateTimeOffset issuedAt);
}

public static class CoreClaimTypes
{
    public const string UserId             = "core.user_id";
    public const string UserName           = "core.user_name";
    public const string TenantId           = "core.tenant_id";
    public const string SecurityStamp      = "core.security_stamp";
    public const string IssuedAt           = "core.issued_at";             // UTC
    public const string MustChangePassword = "core.must_change_password";
    public const string IsSystemOperator   = "core.is_system_operator";
}

public sealed record LoginOutcome(SessionDto Session, string SecurityStamp);

// Ý nghĩa từng trường: contracts/auth.md §3, §5
public sealed record SessionDto(
    Guid Id, string UserName, string? Email, string FullName,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions,
    bool MustChangePassword, bool IsSystemOperator, int SessionMinutes,
    string? PreferredLanguage, string TenantCode, string TenantName);
```

```csharp
// Core.Web/Controllers/AuthController.cs
[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting("login")]
public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
{
    var result = await mediator.Send(command, ct);
    if (result.IsFailure)
        return HandleResult(result);

    await SignInAsync(result.Value, issuedAt: timeProvider.GetUtcNow());
    return HandleResult(Result.Success(result.Value.Session));
}

[HttpPost("change-password")]
[AuthenticatedOnly("Đổi mật khẩu của chính mình")]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
{
    var result = await mediator.Send(command, ct);
    if (result.IsFailure)
        return HandleResult(result);

    var issuedAt = DateTimeOffset.Parse(User.FindFirstValue(CoreClaimTypes.IssuedAt)!, CultureInfo.InvariantCulture);
    await SignInAsync(result.Value, issuedAt);
    return HandleResult(Result.Success());
}

private Task SignInAsync(LoginOutcome outcome, DateTimeOffset issuedAt) => HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    principalFactory.Create(outcome, issuedAt),
    new AuthenticationProperties { IsPersistent = false });
```

**Ranh giới Identity ↔ cookie** ([ADR-0026](../adr/0026-ranh-gioi-identity-va-cookie.md)):

| Ở `Core.Web` | Ở `Core.Infrastructure` |
| --- | --- |
| Cookie scheme, cookie events, `HttpContext.SignInAsync` / `SignOutAsync`. **Không** giữ `AppUser`, `UserManager`, `SignInManager<AppUser>` | `AddIdentityCore` (không phải `AddIdentity`), store, `UserManager`, chính sách mật khẩu, khoá tài khoản, kiểm security stamp theo `userId`, hiện thực `ISessionPrincipalFactory` |

**Đăng nhập — handler trả gì, controller làm gì.** `LoginCommandHandler` trả `Result<LoginOutcome>`
(`LoginCommand` cài `INoTransaction` — [`be-cqrs-handler.md`](be-cqrs-handler.md) §5.3). Thứ tự trong handler:
`ILoginAttemptLimiter` (§6.5) → `ITenantLookup.FindByCodeAsync` → mở phạm vi đơn vị →
`IIdentityService.CheckCredentialsAsync` trả `CredentialCheck` ([`be-entity-domain.md`](be-entity-domain.md) §7.1)
→ dựng `SessionDto` → `LoginOutcome(session, check.SecurityStamp)`. `SessionDto` ghép từ các seam, không chạm
`AppUser`: `IUserLookupService.FindByIdAsync` (`UserSummaryDto`: danh tính, `IsSystemOperator`, `PreferredLanguage`),
`IUserLookupService.GetRoleNamesAsync` (`Roles`), `IPermissionChecker.GetEffectivePermissionsAsync` (`Permissions`),
`TenantSummary` đã tra ở bước trước (`TenantCode`, `TenantName`); `MustChangePassword` từ `CredentialCheck` — cùng lần
đọc với stamp; `SessionMinutes` từ `CoreAuthOptions`. Handler của `me` ghép cùng cách, chỉ khác `MustChangePassword`
lấy từ `UserSummaryDto`. Controller gọi `principalFactory.Create(outcome, now)` rồi `HttpContext.SignInAsync`, trả
`outcome.Session` trong envelope — **không** gọi lại query của `me` trong cùng request.

`ClaimsPrincipal` của phiên dựng **chỉ** qua `ISessionPrincipalFactory.Create`; tên loại claim khai
**một lần** ở `CoreClaimTypes`. **Permission không vào cookie** — kiểm mỗi request qua `IPermissionChecker` (§4.3).

**Phép kiểm phiên ở mọi request** (`OnValidatePrincipal`): mở phạm vi ngữ cảnh thực thi bằng `TenantId` từ
**claim của chính principal đang kiểm** (mục allowlist luật A12 ở [`be-architecture.md`](be-architecture.md) §1.1),
rồi gọi `IIdentityService.IsSessionValidAsync(userId, securityStamp)`. Phép kiểm trả `false` khi security stamp lệch
**hoặc** đơn vị của claim có `is_active = false` — cùng một lần đọc. `false` ⇒ `RejectPrincipal` + `SignOutAsync`;
`true` ⇒ `ShouldRenew = true`.

**Đổi mật khẩu thành công** (cả hai endpoint ở [`../contracts/auth.md`](../contracts/auth.md) §6, §7): handler gọi
`IIdentityService.ChangePasswordAsync` ([`be-entity-domain.md`](be-entity-domain.md) §7.1) — endpoint §7 truyền
`clearMustChangePassword: true`, §6 truyền `false` — rồi trả `Result<LoginOutcome>` mang stamp mới và
`MustChangePassword` từ `CredentialCheck` của lời gọi đó; controller cấp lại cookie với `issuedAt` **đọc từ claim
`CoreClaimTypes.IssuedAt` của principal cũ** (khối trên) — trần phiên không reset; thân phản hồi giữ `data: null`
theo card.

**Khoá tài khoản và đặt lại mật khẩu đều đổi security stamp của tài khoản đích**, nên phiên của nó bị từ
chối ở **request kế tiếp** — kể cả quản trị đặt lại hộ và tài khoản vận hành khôi phục mật khẩu quản trị đơn vị
([ADR-0029](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)).
Hiện thực ở `Core.Infrastructure` gọi `UserManager.UpdateSecurityStampAsync` **tường minh** trong chính
thao tác đó. **Mở khoá không đổi stamp.** Hợp đồng: [`../contracts/users.md`](../contracts/users.md) §8, §9.

**Gia hạn ở mọi request mang phiên hợp lệ.** Mốc hết hạn tính từ request gần nhất qua được phép kiểm phiên.
Cơ chế: `ShouldRenew = true` sau khi phép kiểm qua. **Không** dựa vào `SlidingExpiration`. Request mà handler trả
lỗi nghiệp vụ **vẫn gia hạn**.

**`SessionMinutes` — chốt 30 phút cho MỌI phiên ở v1 (2026-09-10).** Không có tuỳ chọn *ghi nhớ đăng nhập*:
phiên phát với `IsPersistent = false`. Ba mươi phút là thời gian **không thao tác**; giá trị là **cấu hình**, nhưng
đổi nó là một quyết định về bảo mật. FE v1 **không** hẹn giờ cảnh báo theo con số này
([`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) §10); trường vẫn nằm trong DTO.

**Trần tuyệt đối của phiên: `Core:Auth:SessionAbsoluteHours`, mặc định 12 giờ** từ lúc đăng nhập — thuộc tính
`CoreAuthOptions.SessionAbsoluteHours` ([`be-architecture.md`](be-architecture.md) §4.1). Quá trần
thì `OnValidatePrincipal` từ chối principal và trả 401 `CORE.AUTH.NOT_AUTHENTICATED` **dù phiên đang được gia
hạn đều**. Cơ chế: claim `CoreClaimTypes.IssuedAt` — tham số `issuedAt` của factory (khối trên); kiểm cùng chỗ với
security stamp. Response đăng nhập không mang giá trị này.

> 📖 Lý do, bẫy, ví dụ mở rộng (gồm khối cookie scheme đầy đủ): [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §7.4

### 7.5 ⚠️ Antiforgery — hai nửa, hai đường

> **Luật nền cho cả mục này: tên mọi cookie do hệ thống đặt khai ở ĐÚNG MỘT chỗ trong cấu
> hình, dùng chung tiền tố và phân biệt bằng hậu tố vai trò — kèm một test khẳng định
> không có hai tên trùng nhau.**

Antiforgery có **hai nửa**, đi **hai đường khác nhau**:

| | Là gì | Đi đường nào | JS đọc được? |
| --- | --- | --- | --- |
| **cookie-token** | Bí mật server tự quản | Cookie **nội bộ** của `AddAntiforgery`, trình duyệt tự gửi | **Không** — `HttpOnly = true` |
| **request-token** | Giá trị client gửi lại | **Thân phản hồi** của endpoint phát token → FE giữ trong bộ nhớ → header `X-XSRF-TOKEN` | Có — nó nằm trong body |

**Không có cookie thứ hai chứa request-token** — không dùng mô hình double-submit cổ điển.

```csharp
services.AddAntiforgery(options =>
{
    options.Cookie.Name = authOptions.AntiforgeryCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.HeaderName = "X-XSRF-TOKEN";
});
```

Endpoint phát token (`[AllowAnonymous]`, `[DisableRateLimiting]`) gọi `GetAndStoreTokens` rồi trả
`RequestToken` trong body envelope — hợp đồng ở [`../contracts/auth.md`](../contracts/auth.md) §2.

FE **gọi lại endpoint phát token ngay sau đăng nhập và sau đăng xuất** — token gắn với danh tính lúc phát ([`../contracts/auth.md`](../contracts/auth.md)). Validate antiforgery **chỉ áp cho method ghi**, không áp cho `GET`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §7.5

---

## 8. Versioning, Swagger, health check

### 8.1 Tiền tố đường dẫn — định nghĩa gốc

**Mọi endpoint HTTP của repo này nằm dưới `/api/v<N>/<khu>/…`**, trừ **đúng hai** endpoint kiểm tra sức
khoẻ ở §8.3 — ngoại lệ có tên, đóng, không có ca thứ ba.

| Thành phần | Giá trị | Ghi chú |
| --- | --- | --- |
| Tiền tố gốc | `/api` | Cũng là allowlist của `EnvelopeMiddleware` — §2.4 |
| Phiên bản | `/v1` ở v1 | Trong **đường dẫn**, không ở header, không ở query string |
| Khu | `/core` hoặc `/<module>` | Core không bao giờ đăng ký dưới tiền tố của module |
| Ví dụ đầy đủ | `/api/v1/core/users` | |

> 🚨 **Đường dẫn trong code phải đến từ MỘT hằng số dùng chung.**

**Quy tắc phát hành** — thay đổi nào buộc tăng version:

| Loại thay đổi | Breaking? | Tăng version? |
| --- | --- | --- |
| Thêm field **tuỳ chọn** vào request, hoặc field mới vào response | Không | Không |
| Thêm endpoint mới | Không | Không |
| Thêm giá trị mới vào một enum trên dây | **Có** — client cũ không biết giá trị đó | Không, nhưng phải báo FE |
| Đổi tên / bỏ field, đổi kiểu, thêm field bắt buộc | **Có** | Có |
| Đổi giá trị `code` của một lỗi | **Có** — nó là khoá dịch của FE | Có |
| Siết miền giá trị (nới `maxLength` thì không, siết thì có) | **Có** | Có |
| Đổi **ngữ nghĩa** mà giữ nguyên hình dạng | **Có, và nguy hiểm nhất** | Có |

**Chỉ nuôi một major version tại một thời điểm.** Khi thật sự cần `v2`, `v1` sống song song
đúng một chu kỳ phát hành rồi gỡ; version cũ khai `[Obsolete]` và có **ngày gỡ** ghi trong
contract card ở [`../contracts/`](../contracts/).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §8.1

### 8.2 Swagger / OpenAPI

- Bật ở Development; ở Production **chỉ** bật sau xác thực, hoặc tắt hẳn.
- Mọi action khai `[ProducesResponseType]` cho status thành công và ít nhất một nhánh lỗi.
- Kiểu trả về trong tài liệu là `ApiEnvelope<T>`, không phải `T`.
- OpenAPI là tài liệu **sinh ra**, không phải hợp đồng — hợp đồng nằm ở [`../contracts/`](../contracts/).

### 8.3 Health check

| Endpoint | Kiểm gì | Ai gọi |
| --- | --- | --- |
| `/health/live` | Process còn sống — **không** chạm DB | Orchestrator, để quyết định restart |
| `/health/ready` | DB kết nối được, migration đã áp đủ. Trả `Degraded` khi Outbox có bản ghi `dead` hoặc bản ghi chưa phát cũ nhất quá 15 phút — [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md) §8 | Load balancer, để quyết định đưa vào luồng |

Cả hai `[AllowAnonymous]`, **có tên riêng trong allowlist §5**, miễn rate limit, không trả chi tiết nội bộ, không versioning, không envelope (ngoại lệ §8.1). `/health/ready` đỏ khi còn migration chưa áp — luật `Startup_Fails_When_PendingMigrationsExist` ([`../RULES.md`](../RULES.md) §4).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §8.3
