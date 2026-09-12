---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — Controller, envelope, `Result` → HTTP, bảo mật đường vào

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi code mẫu là khuôn cho `src/BE` sẽ xây ở giai đoạn 2.
>
> Đây là file chủ của: ánh xạ `Result` → HTTP, hình dạng envelope, `ApiControllerBase`,
> `RequirePermissionAttribute`, rate limiting, CORS + cookie + antiforgery. File khác chỉ
> được trỏ tới đây.

---

## 1. Ánh xạ `Result` → HTTP — đúng MỘT chỗ, KHÔNG reflection

Đây là mục quan trọng nhất của file, và là chỗ dự án tiền nhiệm đã trả giá đắt nhất.

### 1.1 Bảng ánh xạ ErrorType → HTTP — định nghĩa gốc

| `ErrorType` | HTTP | Dùng khi |
| --- | ---: | --- |
| `Validation` | 400 | Sai định dạng/ràng buộc tính được từ payload — luôn kèm `fieldErrors` |
| `Unauthorized` | 401 | Chưa đăng nhập, hoặc phiên đã hết hạn/bị thu hồi |
| `Forbidden` | 403 | Đã đăng nhập nhưng thiếu quyền |
| `NotFound` | 404 | Resource **chính của route** không tồn tại (kể cả đã soft delete) |
| `Conflict` | 409 | Trùng giá trị unique, hoặc xung đột concurrency |
| `BusinessRule` | 422 | Vi phạm quy tắc nghiệp vụ; tham chiếu **trong payload** không hợp lệ |
| — (exception ngoài dự kiến) | 500 | Bug/hạ tầng — không lộ chi tiết ra response |

`403` không được gộp về `422`: FE cần phân biệt được để điều hướng sang màn xin quyền thay
vì hiện "thao tác sai".

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
        _ => throw new ArgumentOutOfRangeException(
                 nameof(type), type, "ErrorType chưa được ánh xạ sang HTTP status."),
    };
}
```

Nhánh `_` **ném**, không trả 500 mặc định. Thêm một `ErrorType` mới mà quên khai ánh xạ là
một lỗi phải nổ ngay ở test đầu tiên chạm tới nó, không phải một 500 im lặng ở Production.
Kèm theo, compiler cảnh báo thiếu nhánh trên `switch` của enum — và luật T4 (build không
warning) biến cảnh báo đó thành lỗi build.

Luật `EveryErrorType_MapsTo_AValidHttpStatus` ([`../RULES.md`](../RULES.md) §5) canh: mọi
giá trị của `ErrorType` phải ánh xạ sang một status hợp lệ, tại **đúng một chỗ**.

### 1.3 ⚠️ Vì sao CẤM reflection ở cầu nối này

Ở dự án tiền nhiệm, cầu nối từ lỗi nghiệp vụ sang envelope dựng bằng `GetMethod` +
`Invoke`, với một danh sách kiểu **hardcode**. Chuỗi sự kiện:

1. Chữ ký của method đích đổi (thêm một tham số).
2. `GetMethod("...")` không còn khớp và trả **`null`**.
3. Lời gọi `.Invoke(...)` trên `null` ném `NullReferenceException`.
4. Hệ quả: **mọi lỗi nghiệp vụ biến thành `NullReferenceException`** → 500.

Bốn tính chất khiến ca này đắt hơn một bug thường:

- **Không lỗi biên dịch.** Reflection không được compiler kiểm.
- **Hỏng đúng nhánh LỖI.** Đường thành công vẫn chạy, nên smoke test xanh.
- **Không ArchTest nào chạm tới.** Luật kiến trúc lúc đó không có mục nào về reflection.
- **Chỉ lộ ở Production**, khi một người dùng thật làm sai một thao tác thật.

Luật `ResultToHttpMapper_MustNotUse_Reflection` ([`../RULES.md`](../RULES.md) §5) quét mã
nguồn của cầu nối này và cấm `System.Reflection`, `GetMethod`, `Invoke`,
`MakeGenericType`, `Activator.CreateInstance`.

📖 Postmortem đầy đủ: [`../audit/2026-09-05-reflection-envelope.md`](../audit/2026-09-05-reflection-envelope.md).

Cách né reflection ở phía Application (dựng `Result<T>` thất bại trong pipeline behavior
mà không biết `T`) dùng `static abstract` interface member — xem
[`be-cqrs-handler.md`](be-cqrs-handler.md) §5.4.

---

## 2. Envelope trả về

### 2.1 Hình dạng envelope — định nghĩa gốc

Ba kiểu dưới đây là **nguồn duy nhất** của hình dạng envelope trong toàn repo. Card trong
[`../contracts/`](../contracts/) và tài liệu FE trỏ về đây; không file nào chép lại các khối
JSON mẫu ở mục này ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

```csharp
// Core.Contracts/Http/ApiEnvelope.cs
public sealed record ApiEnvelope<T>(
    bool Success,
    T? Data,
    ApiError? Error,
    string TraceId);

public sealed record ApiError(
    string Code,
    string Type,
    string Message,
    IReadOnlyDictionary<string, string>? MessageParams,
    IReadOnlyDictionary<string, IReadOnlyList<ApiFieldError>>? FieldErrors);

public sealed record ApiFieldError(
    string Code,
    IReadOnlyDictionary<string, string>? MessageParams);
```

```csharp
// Core.Web/Http/Envelope.cs — chỗ DUY NHẤT dựng ApiEnvelope<T>
public static class Envelope
{
    public static ApiEnvelope<T> Success<T>(T? data, string traceId)
        => new(true, data, null, traceId);

    public static ApiEnvelope<object> Failure(Error error, string traceId)
        => new(false, null, ToApiError(error), traceId);

    private static ApiError ToApiError(Error error) => new(
        error.Code,
        error.Type.ToString(),
        MessageTemplateRenderer.Render(error.MessageTemplate, error.Params),
        error.Params.Count == 0 ? null : error.Params,
        error.FieldErrors.Count == 0 ? null : error.FieldErrors.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<ApiFieldError>)kv.Value
                .Select(f => new ApiFieldError(f.Code, f.Params.Count == 0 ? null : f.Params))
                .ToList(),
            StringComparer.Ordinal));
}
```

Thành công:

```json
{
  "success": true,
  "data": { "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234", "userName": "an.nv" },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000001"
}
```

Lỗi nghiệp vụ:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "CORE.USER.EMAIL_DUPLICATED",
    "type": "Conflict",
    "message": "Email 'an@vd.vn' đã được dùng.",
    "messageParams": { "Email": "an@vd.vn" },
    "fieldErrors": null
  },
  "traceId": "0HNO9S8JAP586:00000002"
}
```

Lỗi validation:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "CORE.VALIDATION.FAILED",
    "type": "Validation",
    "message": "Dữ liệu gửi lên không hợp lệ.",
    "messageParams": null,
    "fieldErrors": {
      "Email": [ { "code": "CORE.USER.EMAIL_MALFORMED", "messageParams": null } ],
      "UserName": [ { "code": "CORE.USER.USERNAME_TOO_LONG", "messageParams": { "MaxLength": "64" } } ]
    }
  },
  "traceId": "0HNO9S8JAP586:00000003"
}
```

### 2.2 Bốn luật về envelope

1. **Mọi response dưới `/api` đều mang envelope này.** Không có ngoại lệ "endpoint này
   đơn giản nên trả object trần" — một ngoại lệ buộc FE viết hai nhánh bóc, và nhánh thứ
   hai là nơi bug sống.
2. **`traceId` luôn có mặt**, kể cả khi thành công. Nó là thứ duy nhất nối một màn hình
   người dùng đang nhìn với một dòng log ở server.
3. **`message` là dev-facing.** Client dựng câu hiển thị từ `code` + `messageParams`. BE
   không được coi `message` là hợp đồng —
   [`be-cqrs-handler.md`](be-cqrs-handler.md) §8.
4. **`error` là `null` khi thành công, `data` là `null` khi lỗi.** Không bao giờ cả hai
   cùng có giá trị. Luật `HandBuiltErrorEnvelope_AlwaysCarries_ABusinessCode` canh vế
   quan trọng hơn: envelope lỗi dựng tay (ở middleware, ở model binding) vẫn phải mang
   `code`.

### 2.3 ⚠️ Casing — payload camelCase, khoá của `fieldErrors` PascalCase

Toàn bộ payload serialize theo `JsonNamingPolicy.CamelCase`. Nhưng **khoá** của
`fieldErrors` giữ **PascalCase**, khớp đúng tên property C# của DTO (`Email`, `UserName`).

Cơ chế: `DictionaryKeyPolicy` để `null` và **cố ý không set**.

> **Đừng "sửa cho nhất quán".** Đặt `DictionaryKeyPolicy = CamelCase` sẽ làm gãy toàn bộ
> việc bind lỗi vào field trên form phía FE — và gãy **im lặng**, không test nào bắt, vì
> cả hai bên vẫn là JSON hợp lệ. FE tra `fieldErrors['Email']`; xem
> [`fe-api-client.md`](fe-api-client.md).

Đây là một trong số rất ít chỗ được phép giữ comment trong code, theo ngoại lệ ở
[`README.md`](README.md) §5.4: cách viết đúng ở đây trông như một lỗi.

### 2.4 Các đường dựng envelope — danh sách đầy đủ

| Đường | Xử lý | Ở đâu |
| --- | --- | --- |
| `ApiControllerBase.HandleResult` | Mọi `Result` từ handler | `Core.Web` |
| `ExceptionHandlingMiddleware` | Exception ngoài dự kiến, và exception của hạ tầng có nghĩa xác định (antiforgery, concurrency) | `Core.Web` |
| `ModelBindingProblemFactory` | Body không parse được, kiểu sai, thiếu tham số bắt buộc — lỗi xảy ra **trước** khi vào action | `Core.Web` |
| `OnRejected` của rate limiter | 429 `CORE.RATE_LIMIT.EXCEEDED` — request bị chặn **trước** khi tới action nào, nên không có `Result` để bọc | `Core.Web`, §6 |
| `PasswordChangeRequiredMiddleware` | 403 `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` — chặn trước khi vào action, xem [`be-architecture.md`](be-architecture.md) §3.1 | `Core.Web` |
| `EnvelopeMiddleware` | Response do **hạ tầng định tuyến** sinh (404 không khớp route, 405 sai verb) — chúng có thân rỗng nên không đi qua đường nào ở trên | `Core.Web` |

> **Bảng này là danh sách đầy đủ; số dòng của nó cố ý không viết ra thành chữ.** Mọi đường
> ở đây dựng envelope **bằng tay** hoặc bọc lại envelope, nên luật
> `HandBuiltErrorEnvelope_AlwaysCarries_ABusinessCode` áp thẳng vào từng dòng — một đường
> không có tên trong bảng là một đường không ai nghĩ tới việc kiểm.
>
> Đếm bằng lệnh, đừng chép số ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6).

Ba ràng buộc của `EnvelopeMiddleware`, cả ba hỏng im lặng nếu bỏ:

1. **Chỉ bọc tiền tố `/api`** — allowlist, không blocklist. Trả JSON cho một file tĩnh
   404 biến một file thiếu thành một trang hỏng khó đoán.
2. **Không ghi đè response đã có thân.** Phép thử là `HasStarted || ContentLength.HasValue
   || ContentType is { Length: > 0 }`. Đây là thứ giữ nguyên 404 mà handler **chủ động**
   trả (`CORE.USER.NOT_FOUND`); ghi đè nó là thay một mã nghiệp vụ đúng bằng một mã định
   tuyến sai — và cả hai đều là HTTP 404 nên không ai thấy.
3. **Không dùng `MapFallback`.** Nó đăng ký route bắt-tất-cả cho mọi verb, nên trở thành
   ứng viên hợp lệ của request sai verb và **nuốt mất chính mã 405** — sai verb biến thành
   404.

Mã của hai ca định tuyến cố ý khác miền với mã nghiệp vụ: `CORE.ROUTE.NOT_FOUND` và
`CORE.ROUTE.METHOD_NOT_ALLOWED`. Hai ca cùng HTTP 404 thì `code` là thứ **duy nhất** FE
phân biệt được "gọi sai URL" (bug của FE) với "bản ghi không tồn tại" (dữ liệu).

---

## 3. `ApiControllerBase`

```csharp
// Core.Web/Controllers/ApiControllerBase.cs
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
        => result.IsSuccess
            ? Ok(Envelope.Success(result.Value, HttpContext.TraceIdentifier))
            : Failure(result.Error!);

    protected IActionResult HandleResult(Result result)
        => result.IsSuccess
            ? Ok(Envelope.Success<object>(null, HttpContext.TraceIdentifier))
            : Failure(result.Error!);

    private IActionResult Failure(Error error)
        => StatusCode(
            ResultToHttpMapper.ToStatusCode(error.Type),
            Envelope.Failure(error, HttpContext.TraceIdentifier));
}
```

| Trách nhiệm | Ghi chú |
| --- | --- |
| Mang `[Authorize]` — **fail-closed** | Endpoint công khai phải khai `[AllowAnonymous]` tường minh. Quên khai = bị chặn, không phải lộ ra |
| Cung cấp `HandleResult` | Chỗ **duy nhất** controller đặt status code |
| Gắn `traceId` | Lấy từ `HttpContext.TraceIdentifier`, không tự sinh |

**Không** khai `[ApiController]` và `[Route]` ở lớp cơ sở. Chúng khai ở **từng** controller
cụ thể, và route viết **tường minh** (`[Route("api/v1/core/users")]`), không dùng token
`[controller]` — để đổi tên class không làm đổi URL công khai. Tiền tố bắt buộc gồm cả số
phiên bản — §8.1.

Luật `EveryController_Inherits_ApiControllerBase` ([`../RULES.md`](../RULES.md) §6) canh:
một controller kế thừa thẳng `ControllerBase` là một controller không có `[Authorize]` mặc
định và không đi qua `ResultToHttpMapper`.

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

- **Không logic nghiệp vụ.** Chỉ `Send` rồi `HandleResult`. Một `if` trong controller là
  một luật nghiệp vụ nằm ngoài tầm test của tầng Application.
- **Không tự `try/catch`**, không `return StatusCode(500, ...)`. Cả hai tạo nguồn sự thật
  thứ hai cho ánh xạ `Result` → HTTP.
- **Không inject repository hay `DbContext`.** Chỉ `ISender`.

---

## 4. Phân quyền — permission, KHÔNG có hằng số role

### 4.1 Luật gốc

> **Trong `Core.*` không tồn tại một hằng số role nào.** Role là **dữ liệu** trong DB.
> Phân quyền kiểm bằng **permission**, không bằng tên role.

Luật `Core_MustNotDeclare_RoleConstants` và `Authorization_MustCheck_Permission_NotRoleName`
([`../RULES.md`](../RULES.md) §6) canh hai vế.

**Vì sao:** dự án tiền nhiệm hardcode ba tên role trong Core. Hệ quả:

- Dự án thứ hai có mô hình vai trò khác (theo phòng ban, theo cấp) phải **sửa Core** hoặc
  phải tạo role giả mang đúng tên Core mong đợi.
- Thêm một vai trò mới là một lần **deploy**, không phải một thao tác quản trị.
- Câu hỏi *"ai làm được việc X"* không trả lời được bằng truy vấn — phải đọc code.
- Một điều kiện `if (role == "Admin")` rải rác nhiều chỗ, và không chỗ nào biết chỗ nào.

### 4.2 Hiện thực

```csharp
// Core.Application/Permissions/RequirePermissionAttribute.cs — metadata thuần
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}

// Core.Application/Permissions/IPermissionChecker.cs — seam, test không cần DB
public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken ct);
}
```

```csharp
// Core.Web/Filters/RequirePermissionFilter.cs
internal sealed class RequirePermissionFilter(
    IPermissionChecker checker,
    ICurrentUser currentUser) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var required = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .Select(a => a.Key)
            .ToArray();

        if (required.Length == 0)
            return;   // không khai attribute = giữ nguyên [Authorize], không chặn thêm

        if (currentUser.UserId is not { } userId)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var ct = context.HttpContext.RequestAborted;

        foreach (var key in required)
        {
            if (!await checker.HasPermissionAsync(userId, key, ct))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
```

### 4.3 Ma trận quyền theo resource

Permission key có khuôn `<tài nguyên>.<hành động>`, khai bằng hằng số **chuỗi** — không
phải enum, để module thêm key của mình mà không sửa Core:

> 📖 **Danh mục khoá của Core — tập đầy đủ kèm ý nghĩa từng khoá: đọc
> [`../database/schema-core.md`](../database/schema-core.md) §5.2.** Nơi đó khai cột `code`,
> và cột đó chính là dữ liệu seed — nên nó là file chủ. Mục này chỉ nêu **cách khai** hằng số,
> với vài dòng trích làm ví dụ; chép cả danh sách sang đây là dựng nguồn thứ hai
> ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

```csharp
// Core.Application/Permissions/CorePermissions.cs — TRÍCH, không phải danh mục đầy đủ
public static class CorePermissions
{
    public const string UserRead  = "core.user.read";
    public const string UserWrite = "core.user.write";
    // … các khoá còn lại khai đúng theo schema-core.md §5.2
}
```

Mỗi hằng số ở đây phải khớp **đúng chuỗi** một dòng `core.permission.code`. Lệch một ký tự
nghĩa là `[RequirePermission(...)]` hỏi một khoá không có trong dữ liệu, và
`IPermissionChecker` trả `false` cho **mọi** người — kể cả tài khoản đủ quyền, vì deny-by-default.

Ma trận sống trong DB, không trong code:

| Bảng | Nội dung |
| --- | --- |
| `core.app_role` | Vai trò — **dữ liệu**, tạo/sửa/xoá được từ màn quản trị |
| `core.permission` | Danh mục permission key, seed từ hằng số của Core và của từng module |
| `core.role_permission` | Ma trận: vai trò nào có permission nào |

Tên bảng số **ít**, theo quy ước đặt tên bắt buộc ở
[`../database/schema-core.md`](../database/schema-core.md) §2.1.

`IPermissionChecker` tra qua ba bảng đó theo vai trò của người dùng. Thêm một vai trò mới
là một thao tác quản trị; đổi quyền của một vai trò cũng vậy.

**Ngoại lệ break-glass** (một vai trò đi qua mọi kiểm tra) là một quyết định **của dự án**,
không của Core: nếu cần, dự án tự seed một vai trò có **tất cả** permission. Core không
biết vai trò đó tồn tại, và không có nhánh `if` nào cho nó.

### 4.4 Điều gì `RequirePermission` KHÔNG làm

Nó kiểm quyền ở mức **endpoint**, không ở mức **bản ghi**. Câu hỏi *"người này có được sửa
đúng bản ghi này không"* thuộc về handler và trả `ErrorType.Forbidden`.

Đây là ranh giới có chủ đích: nhét kiểm theo bản ghi vào filter buộc filter phải đọc dữ
liệu nghiệp vụ, và filter sống ở `Core.Web` — nơi không được biết nghiệp vụ nào tồn tại.

---

## 5. `[AllowAnonymous]` — allowlist khai tường minh

`ApiControllerBase` mang `[Authorize]`, nên mặc định là **chặn**. Endpoint công khai phải
khai `[AllowAnonymous]`, và mỗi lời khai phải nằm trong một allowlist khai ở một chỗ:

```csharp
// Core.Web/Security/AnonymousEndpointAllowlist.cs
public static class AnonymousEndpointAllowlist
{
    public static readonly IReadOnlySet<string> Endpoints = new HashSet<string>(StringComparer.Ordinal)
    {
        "POST /api/v1/core/auth/login",
        "POST /api/v1/core/auth/forgot-password",
        "POST /api/v1/core/auth/reset-password",
        "GET /api/v1/core/antiforgery/token",
        "GET /health/live",
        "GET /health/ready",
    };
}
```

Luật `EveryAllowAnonymous_IsOn_TheAllowlist` ([`../RULES.md`](../RULES.md) §6) đối chiếu
mọi `[AllowAnonymous]` trong solution với danh sách này.

**Vì sao phải khai tường minh:** một endpoint công khai là một quyết định bảo mật. Cách
duy nhất để nó được rà lại là bắt nó xuất hiện ở một chỗ mà người review đọc được **toàn
bộ** trong một lần nhìn. Rải `[AllowAnonymous]` khắp nơi thì câu hỏi *"những gì đang mở ra
Internet?"* không trả lời được nếu không đọc hết mọi controller.

Thêm một dòng vào allowlist là một thay đổi phải giải trình trong PR. Đó là mục đích.

---

## 6. Rate limiting

### 6.1 Ba hàng rào

| # | Hàng rào | Phân vùng theo | Kiểu cửa sổ | Chặn được gì |
| --- | --- | --- | --- | --- |
| 1 | Policy `login` | **IP** | `FixedWindow` | Brute-force từ một nguồn |
| 2 | Giới hạn nền, mọi request | **IP** | `SlidingWindow` | Vòng lặp retry hỏng ở FE, tab treo gọi API liên tục |
| 3 | Nhánh login | **tên đăng nhập** | `SlidingWindow` | Brute-force **phân tán** nhiều IP cùng nhắm một tài khoản |
| 4 | Policy `client-errors` | **IP** | `FixedWindow` | Một lỗi trong vòng lặp vẽ lại ở FE sinh hàng nghìn báo cáo mỗi phút — [`../contracts/client-errors.md`](../contracts/client-errors.md) |

Hàng rào 3 chặn đúng kịch bản hai hàng rào kia bỏ lọt: phân vùng theo IP không thấy gì bất
thường khi hàng nghìn IP mỗi IP chỉ thử vài lần.

### 6.2 ⚠️ Chọn đúng overload — bẫy đã dính thật

```csharp
// ❌ SAI — tạo MỘT limiter dùng chung cho cả policy, KHÔNG phân vùng gì cả
options.AddFixedWindowLimiter("login", o => { o.PermitLimit = 5; o.Window = TimeSpan.FromMinutes(1); });
```

Với overload trên, hạn mức là **5 lượt đăng nhập mỗi phút cho TOÀN HỆ THỐNG**, không phải
mỗi IP. Hệ quả **ngược hẳn mục tiêu**:

- Một kẻ tấn công đốt hết quota là **khoá đăng nhập của mọi người** — biện pháp chống
  brute-force biến thành lỗ hổng DoS.
- Brute-force phân tán qua nhiều IP thì **không** bị chặn riêng chút nào.

Ở dự án tiền nhiệm, cách viết sai này còn đi kèm **một câu giải thích sai** trong tài liệu
("mặc định của limiter là phân vùng theo remote IP"), và code chép y theo nên mang nguyên
lỗi. Rule sai không nằm yên — nó sinh ra code sai.

```csharp
// ✅ ĐÚNG — AddPolicy + RateLimitPartition
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
        }));

    var byIp = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
            }));

    var byUserName = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        ctx.Request.Path.StartsWithSegments("/api/v1/core/auth/login")
            ? RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: LoginPartitionKey.Resolve(ctx),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    SegmentsPerWindow = 5,
                })
            : RateLimitPartition.GetNoLimiter<string>("skip"));

    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(byIp, byUserName);
});
```

```csharp
[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting("login")]
public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    => HandleResult(await mediator.Send(command, ct));
```

> 🚨 **Chuỗi đường dẫn trong `StartsWithSegments` phải là route THẬT, kèm số phiên bản.**
> Viết thiếu một đoạn (`/api/core/auth/login` thay cho `/api/v1/core/auth/login`) thì điều kiện
> **không bao giờ đúng**, nên nhánh luôn rơi vào
> `GetNoLimiter` — hàng rào chống dò mật khẩu phân tán chạy ở **mọi** request và **không chặn
> gì cả**. Không lỗi biên dịch, không lỗi lúc chạy, không test nào xanh-đỏ vì nó. Đây là dạng
> hỏng im lặng đắt nhất trong file này, và nó chỉ cần một chuỗi lệch để xảy ra.
>
> Vì vậy: đường dẫn dùng để phân nhánh **không** được gõ tay ở hai chỗ. Khai một hằng số dùng
> chung cho cả `[Route]` của controller lẫn điều kiện ở đây, để hai bên không thể lệch nhau.

### 6.3 Bốn ràng buộc

1. **Phân vùng không đọc header nào do client gửi.** Cho client tự chọn phân vùng là tự vô
   hiệu hoá rate limit. Chỉ đọc kết nối TCP thật.

   > ⚠️ **Bật `UseForwardedHeaders` mà không khai proxy tin cậy thì TỆ HƠN là không bật.**
   > Không bật: mọi request mang IP của proxy, nên cả hệ dùng chung một phân vùng — giới hạn
   > quá chặt, nhưng **không ai vượt được**. Bật mà không khai: bất kỳ ai cũng đặt được header
   > chuyển tiếp tuỳ ý và **tự chọn phân vùng của mình**, tức rate limit không còn giới hạn gì.
   >
   > Hai trạng thái đó trông giống hệt nhau ở log và ở test — chỉ khác nhau khi có người thật
   > sự tấn công. Vì vậy: khai danh sách proxy tin cậy **tường minh** — vị trí bước này trong
   > pipeline và quy tắc cấu hình ở [`be-architecture.md`](be-architecture.md) §3.1, ràng buộc 4.
2. **Hai thuật toán khác nhau, có lý do.** `login` dùng cửa sổ **cố định** vì với
   brute-force, khoá cứng tới hết cửa sổ là điều ta muốn. Giới hạn nền dùng cửa sổ **trượt**
   vì cửa sổ cố định cho phép dồn tới **2×** hạn mức khi burst vắt qua ranh giới phút, và
   phạt tới gần một phút người vô tình chạm ngưỡng. Đừng đổi cho "nhất quán".
3. **Không rate-limit health check.** Orchestrator gọi `/health/live` và `/health/ready`
   liên tục; chặn chúng gây báo động giả. Endpoint phát token antiforgery cũng miễn — nó là bước bắt buộc trước mọi
   request ghi.
4. **Đếm trong bộ nhớ của từng instance là hạn mức nhân lên.** Chạy N instance thì ngưỡng
   thực tế là N lần ngưỡng đã khai. Ở v1 chạy một instance nên chấp nhận được — nhưng đây
   là **điều kiện bắt buộc phải giải trước khi mở instance thứ hai**, không phải chi tiết
   tinh chỉnh sau.
5. **Thông điệp từ chối không tiết lộ ngưỡng.** Trả mã lỗi chung, không kèm con số. Nêu
   ngưỡng ra là chỉ cho kẻ tấn công cách điều chỉnh tốc độ xuống vừa dưới nó.
6. **429 phải ra đúng envelope**, mang `code: "CORE.RATE_LIMIT.EXCEEDED"`. FE cần phân
   biệt "thử lại sau" với "thao tác sai".

### 6.4 Con số là ước lượng, không phải số đo

Các hạn mức trên là mặc định hợp lý cho quy mô vài chục người dùng đồng thời, mỗi người
một IP. Sau khi chạy thật, đếm số lần 429 trong log:

- Có 429 **không** đi kèm sự cố FE → hạn mức chặt hơn hành vi thật, nới lên.
- Không có 429 nào trong nhiều tuần → hạn mức đang không làm gì cả. **Vẫn giữ** — nó là
  lưới chặn tai nạn, không phải công cụ đo tải.

Rate limit theo IP **không phải** phòng thủ DDoS: tấn công thật đến từ hàng nghìn IP, mỗi
IP dưới ngưỡng. Việc đó thuộc tầng mạng, không đặt kỳ vọng vào app.

---

## 7. CORS + Cookie `SameSite=Lax` + antiforgery hai lớp

### 7.1 Chuỗi ràng buộc — vì sao vẫn cần antiforgery khi đã có `SameSite`

Đọc theo thứ tự, mỗi bước kéo theo bước sau:

1. FE và API ở **hai origin khác nhau nhưng cùng tên miền gốc** (dev: hai cổng của `localhost`;
   prod: hai subdomain) — [`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md).
2. `SameSite` xét theo **site** — scheme cộng tên miền gốc — **không** theo origin. Hai subdomain
   cùng scheme là **một** site.
3. Nên cookie phiên dùng **`SameSite=Lax`**: nó được gửi cho mọi request cùng site, kể cả
   `fetch` từ subdomain kia. **Không** cần `SameSite=None`.
4. `Lax` là lớp chống CSRF **đầu tiên**: request do một site khác phát sinh không mang cookie phiên.
5. Nhưng một subdomain **khác** của cùng tên miền gốc vẫn là cùng site với trình duyệt — `Lax`
   không chặn nó. Nên vẫn cần hai lớp ở §7.2.

> 🛑 **Điều kiện của cả chuỗi: cùng tên miền gốc VÀ cùng scheme.**
>
> | Lệch | Hậu quả |
> | --- | --- |
> | Khác scheme — FE `http`, API `https` | **Khác site.** Cookie `Lax` không được gửi ⇒ đăng nhập xong vẫn 401. Đó là lý do mọi môi trường, kể cả máy dev, chạy HTTPS |
> | Khác tên miền gốc | Cookie thành **bên thứ ba**: phải `SameSite=None`, và trình duyệt có thể chặn hẳn. Đổi điều kiện này là một **ADR mới**, không phải một dòng cấu hình |

### 7.2 Hai lớp

| Lớp | Cơ chế | Chặn gì |
| --- | --- | --- |
| 1 | `OriginValidationMiddleware` — request ghi có header `Origin` ngoài allowlist bị 403 | Request từ site lạ do trình duyệt phát sinh, vì trình duyệt luôn gắn `Origin` cho request cross-origin và **script không sửa được** header đó |
| 2 | Token antiforgery — mọi `POST`/`PUT`/`PATCH`/`DELETE` phải mang header `X-XSRF-TOKEN` khớp | Ca lớp 1 bỏ lọt (request không có `Origin`, hoặc allowlist bị cấu hình rộng) |

Hai lớp độc lập: một request với `Origin` lạ nhưng token hợp lệ vẫn phải bị **403**. Đó
chính là phép thử chứng minh lớp 1 đang chạy.

**Endpoint nào cần antiforgery:**

| Loại | Cần? |
| --- | --- |
| Mọi endpoint ghi (`POST`, `PUT`, `PATCH`, `DELETE`) | ✅ |
| Đăng nhập | ✅ — chống ép người dùng đăng nhập vào tài khoản của kẻ tấn công |
| Đăng xuất | ✅ — bị ép đăng xuất là phiền, tuy nhẹ |
| Endpoint đọc (`GET`) | ❌ — nhưng khi đó `GET` **không được** gây thay đổi trạng thái. Một `GET` có tác dụng phụ là lỗ hổng, không phải một lựa chọn thiết kế |

### 7.3 CORS

```csharp
services.AddCors(options => options.AddPolicy(CorsPolicyNames.Default, policy =>
{
    policy.WithOrigins(authOptions.AllowedOrigins)   // từ IOptions, KHÔNG hardcode
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();                        // bắt buộc để cookie đi qua
}));
```

- Allowlist đọc từ cấu hình qua `IOptions<CoreAuthOptions>` — fail-fast ở Production
  ([`be-architecture.md`](be-architecture.md) §4).
- **Không bao giờ** `AllowAnyOrigin()` một khi đã có auth thật. `AllowAnyOrigin()` và
  `AllowCredentials()` còn loại trừ nhau về mặt kỹ thuật, nên ai viết cả hai sẽ gặp lỗi
  lúc chạy — nhưng đừng trông vào lỗi đó, hãy đừng viết.

> 🛑 **Điều CORS KHÔNG làm.** CORS là cơ chế của **trình duyệt**. Nó không bảo vệ được
> trước một client không phải trình duyệt — `curl` bỏ qua nó hoàn toàn. **Không bao giờ
> dùng CORS thay cho phân quyền.** Ai đó "sửa" lỗi CORS bằng cách phản hồi lại đúng
> `Origin` mà client gửi lên là đã tự tay cho phép mọi trang web.

### 7.4 Cookie phiên

```csharp
services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = authOptions.CookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(authOptions.SessionMinutes);
    options.SlidingExpiration = true;

    // API không redirect sang trang login — trả 401/403 để FE tự điều hướng
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
```

**`SessionMinutes` — chốt 30 phút cho v1 (2026-09-10).** Đây là thời gian **không thao tác**:
`SlidingExpiration = true` nên mỗi request đẩy mốc hết hạn đi tiếp. Ba mươi phút là mức của phần
mềm nội bộ và dịch vụ công, và nằm trong khoảng OWASP khuyến nghị cho ứng dụng thường. Thấp hơn
mười lăm phút thì hộp thoại cảnh báo sắp hết phiên bật liên tục — và đó là lúc người dùng bắt đầu
đòi tắt hẳn nó đi.

Giá trị này là **cấu hình**, dự án đổi được; nhưng đổi nó là một quyết định về bảo mật, không phải
một lần chỉnh cho tiện. FE dùng chính con số này để hẹn giờ cảnh báo —
[`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) §7.5.

> 🚧 **Chưa có: mức trần tuyệt đối.** Gia hạn trượt nghĩa là người làm việc liên tục **không bao
> giờ** bị đăng xuất. Nhiều hệ thống cơ quan thêm một mức trần — ví dụ tám giờ — để mỗi ngày làm
> việc phải đăng nhập lại một lần. ASP.NET Core không có sẵn: phải ghi thời điểm phát phiên vào
> claim rồi kiểm ở `OnValidatePrincipal`. Làm khi có một yêu cầu bảo mật cụ thể đòi nó, không làm
> vì nghe hợp lý.

Hai `Events` ở cuối không phải tuỳ chọn: mặc định của cookie authentication là **redirect
302** sang trang đăng nhập. Với một SPA, redirect đó biến một lỗi 401 rõ ràng thành một
response HTML 200 mà FE parse thành rác.

### 7.5 ⚠️ Antiforgery — hai nửa, hai đường

> **Luật nền cho cả mục này: tên mọi cookie do hệ thống đặt khai ở ĐÚNG MỘT chỗ trong cấu
> hình, dùng chung tiền tố và phân biệt bằng hậu tố vai trò — kèm một test khẳng định
> không có hai tên trùng nhau.**
>
> Trùng tên thì trình duyệt **ghi đè**, vì nó không có khái niệm "hai cookie khác mục đích
> cùng tên". Hậu quả không nhất quán nên rất khó chẩn đoán: lúc thì request ghi bị từ chối
> dù vừa tải trang, lúc thì người dùng bị đăng xuất giữa chừng — tuỳ phản hồi nào tới
> trước. Một dòng test chặn đúng lớp lỗi này.

Antiforgery có **hai nửa**, đi **hai đường khác nhau**:

| | Là gì | Đi đường nào | JS đọc được? |
| --- | --- | --- | --- |
| **cookie-token** | Bí mật server tự quản | Cookie **nội bộ** của `AddAntiforgery`, trình duyệt tự gửi | **Không** — `HttpOnly = true` |
| **request-token** | Giá trị client gửi lại | **Thân phản hồi** của endpoint phát token → FE giữ trong bộ nhớ → header `X-XSRF-TOKEN` | Có — nó nằm trong body |

**Không có cookie thứ hai chứa request-token.** Mô hình double-submit cổ điển đặt request-token
vào một cookie cho JS đọc. Ở đây FE và API khác origin, nên muốn JS đọc được thì cookie đó phải
mở cho **mọi** subdomain của tên miền gốc. Trả trong body hẹp hơn: chỉ origin được CORS cho phép
đọc được nó.

```csharp
services.AddAntiforgery(options =>
{
    options.Cookie.Name = authOptions.AntiforgeryCookieName;   // khai cùng chỗ với mọi tên cookie — luật nền ở trên
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.HeaderName = "X-XSRF-TOKEN";
});
```

```csharp
// Endpoint phát token — request-token đi trong BODY, không set cookie nào cho JS
[HttpGet("token")]
[AllowAnonymous]
[DisableRateLimiting]
public IActionResult GetToken([FromServices] IAntiforgery antiforgery)
{
    var tokens = antiforgery.GetAndStoreTokens(HttpContext);   // set cookie-token nội bộ
    return Ok(Envelope.Success(new { token = tokens.RequestToken }, HttpContext.TraceIdentifier));
}
```

**Bẫy thứ hai, cùng chủ đề:** token phát lúc **chưa đăng nhập** không dùng lại được cho
request ghi **sau khi** đã đăng nhập — `IAntiforgery` gắn request-token với danh tính tại
thời điểm phát hành. Token FE đang giữ không tự đổi theo sự kiện đăng nhập; chỉ có token mới
khi FE gọi lại endpoint trên.

Hệ quả bắt buộc cho FE: **gọi lại endpoint phát token ngay sau khi đăng nhập thành công và sau
khi đăng xuất**, không chỉ một lần lúc khởi động app. Hợp đồng đầy đủ: [`../contracts/auth.md`](../contracts/auth.md).

**Bẫy thứ ba:** validate antiforgery **chỉ áp cho method ghi**. Áp cho `GET` sẽ chặn cả
lần tải trang đầu tiên, khi cookie còn chưa tồn tại.

---

## 8. Versioning, Swagger, health check

### 8.1 Tiền tố đường dẫn — định nghĩa gốc

**Mọi endpoint HTTP của repo này nằm dưới `/api/v<N>/<khu>/…`, không có ngoại lệ.** Core sở
hữu `/api/vN/core/…`; module sở hữu `/api/vN/<module>/…`. Không tranh chấp không gian tên.

| Thành phần | Giá trị | Ghi chú |
| --- | --- | --- |
| Tiền tố gốc | `/api` | Cũng là allowlist của `EnvelopeMiddleware` — §2.4 |
| Phiên bản | `/v1` ở v1 | Trong **đường dẫn**, không ở header, không ở query string |
| Khu | `/core` hoặc `/<module>` | Core không bao giờ đăng ký dưới tiền tố của module |
| Ví dụ đầy đủ | `/api/v1/core/users` | |

Phiên bản nằm trong đường dẫn vì đó là thứ đọc được trong log, trong bookmark, và trong một
câu `curl` dán vào issue.

> 🚨 **Chuỗi đường dẫn viết ở hai chỗ là chuỗi sẽ lệch** — lệch ở điều kiện rate limit (§6.2)
> là cả hàng rào ngừng chặn. Đường dẫn xuất hiện trong code phải đến từ **một** hằng số dùng chung.

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

Dòng cuối là dòng đáng sợ nhất: hình dạng không đổi nên không cổng nào bắt được, mà client
vẫn hiểu sai. Ở dự án tiền nhiệm, việc *"bỏ một phần tử khỏi payload ghi đè ma trận"* đổi
nghĩa từ "thu hồi sạch" thành "lỗi 400" mà không đổi một byte nào của schema.

**Chỉ nuôi một major version tại một thời điểm.** Khi thật sự cần `v2`, `v1` sống song song
đúng một chu kỳ phát hành rồi gỡ; version cũ khai `[Obsolete]` và có **ngày gỡ** ghi trong
contract card ở [`../contracts/`](../contracts/).

### 8.2 Swagger / OpenAPI

- Bật ở Development; ở Production **chỉ** bật sau xác thực, hoặc tắt hẳn.
- Mọi action khai `[ProducesResponseType]` cho status thành công và cho ít nhất một nhánh
  lỗi — nếu không, tài liệu sinh ra nói dối về hình dạng envelope.
- Kiểu trả về trong tài liệu là `ApiEnvelope<T>`, không phải `T`.
- OpenAPI là tài liệu **sinh ra**, không phải hợp đồng. Hợp đồng của một endpoint nằm ở
  [`../contracts/`](../contracts/) — nơi ghi cả luật nghiệp vụ và mã lỗi có thể trả về, thứ
  OpenAPI không diễn đạt được.

### 8.3 Health check

| Endpoint | Kiểm gì | Ai gọi |
| --- | --- | --- |
| `/health/live` | Process còn sống — **không** chạm DB | Orchestrator, để quyết định restart |
| `/health/ready` | DB kết nối được, migration đã áp đủ | Load balancer, để quyết định đưa vào luồng |

Tách hai endpoint là bắt buộc: gộp làm một thì một sự cố DB tạm thời sẽ khiến orchestrator
**restart** app — làm mọi thứ tệ hơn, vì restart không sửa được DB.

Cả hai `[AllowAnonymous]` — và vì thế **cả hai phải có tên trong allowlist ở §5**, không phải
một dòng `GET /health` gộp chung. Cả hai miễn rate limit, và cả hai **không** trả chi tiết nội
bộ (tên máy chủ, chuỗi kết nối, phiên bản package) ra ngoài.

Hai endpoint này là ngoại lệ **duy nhất** nằm ngoài tiền tố `/api/v1/…` ở §8.1: chúng do
orchestrator và load balancer gọi, không phải client nghiệp vụ, nên chúng không đi theo vòng
đời phiên bản của API và cũng không mang envelope.

`/health/ready` phải đỏ khi còn migration chưa áp — luật `Startup_Fails_When_PendingMigrationsExist`
([`../RULES.md`](../RULES.md) §4) đi cùng cặp với nó. Một app chạy trên schema cũ hơn model
là một app ghi sai dữ liệu, không phải một app hơi lỗi thời.
