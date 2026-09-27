---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng của `be-api-controller.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`docs/quy-uoc/be-api-controller.md`](../../../quy-uoc/be-api-controller.md); luật ở đó. Số mục dưới đây trùng số mục của file luật — mục không có gì dời thì ghi "—".

---

## 1. Ánh xạ `Result` → HTTP — đúng MỘT chỗ, KHÔNG reflection

Đây là mục quan trọng nhất của file luật, và là chỗ dự án tiền nhiệm đã trả giá đắt nhất.

### 1.1 Bảng ánh xạ ErrorType → HTTP

`403` gộp về `422` thì FE hiện "thao tác sai" cho một ca thật ra là thiếu quyền — FE cần phân biệt được
để điều hướng sang màn xin quyền.

### 1.2 Hiện thực — switch expression, không reflection

Lỗi biên dịch là thứ ta muốn — không phải một test đỏ, càng không phải một 500 im lặng ở Production.
Thêm một `ErrorType` mới mà quên khai ánh xạ: switch expression thiếu một giá trị có tên sinh cảnh báo
CS8509, và `TreatWarningsAsErrors` biến nó thành lỗi build — một nhánh `_` sẽ nuốt đúng cảnh báo đó.

CS8524 là cảnh báo cho số nguyên bất kỳ ép sang `ErrorType` — thứ không nhánh nào phủ được; không tắt
thì switch đã phủ đủ mọi giá trị có tên vẫn không biên dịch. Giá trị không tên lọt tới `ResultToHttpMapper`
lúc chạy ném `SwitchExpressionException`.

Khối đầy đủ — chuyển từ file luật §1.2 ngày 2026-09-23; bảng ánh xạ vẫn là định nghĩa gốc ở §1.1 của file luật, khối này chỉ là hiện thực của nó:

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

### 1.3 ⚠️ Vì sao CẤM reflection ở cầu nối này

Cầu nối dựng bằng `GetMethod` + `Invoke` hỏng đúng nhánh **lỗi**, không lỗi biên dịch: chữ ký method
đích đổi ⇒ `GetMethod` trả `null` ⇒ **mọi lỗi nghiệp vụ thành `NullReferenceException`** → 500. Cách né
reflection ở phía Application (dựng `Result<T>` thất bại trong pipeline behavior mà không biết `T`) dùng
`static abstract` interface member — [`be-cqrs-handler.md`](../../../quy-uoc/be-cqrs-handler.md) §5.4.

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

📖 Postmortem đầy đủ: [`../../../audit/2026-09-05-reflection-envelope.md`](../../../audit/2026-09-05-reflection-envelope.md).

---

## 2. Envelope trả về

### 2.1 Hình dạng envelope

`ApiEnvelope` là hình dạng dây của đường vào HTTP nên sống ở `Core.Web`; `Core.Contracts` giữ đúng vai
sự kiện và DTO **Core phát ra** cho module, chỉ BCL. `traceId` gán vào `HttpContext.TraceIdentifier`
nên cùng một giá trị nằm trong envelope, trong log scope, và trong mọi span tracing phía sau.

Ví dụ mở rộng — lỗi nghiệp vụ và lỗi validation, chuyển từ file luật §2.1 ngày 2026-09-27 (ví dụ
"thành công" vẫn ở đó):

```json
{ "success": false, "data": null,
  "error": { "code": "CORE.USER.EMAIL_DUPLICATED", "type": "Conflict", "message": "Email 'an@vd.vn' đã được dùng.",
             "messageParams": { "Email": "an@vd.vn" }, "fieldErrors": null },
  "traceId": "0af7651916cd43dd8448eb211c80319c" }
```

```json
{ "success": false, "data": null,
  "error": { "code": "CORE.VALIDATION.FAILED", "type": "Validation", "message": "Dữ liệu gửi lên không hợp lệ.",
             "messageParams": null,
             "fieldErrors": {
               "Email": [ { "code": "CORE.VALIDATION.FORMAT", "messageParams": null } ],
               "UserName": [ { "code": "CORE.VALIDATION.MAX_LENGTH", "messageParams": { "MaxLength": "64" } } ] } },
  "traceId": "5b8aa5a2d2c872e8321cf37308d69df2" }
```

Thân helper private của `Envelope` — chỗ duy nhất `Error` biến thành `ApiError`:

```csharp
// Core.Web/Http/Envelope.cs
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
```

### 2.2 Bốn luật về envelope

- Một ngoại lệ "endpoint này đơn giản nên trả object trần" buộc FE viết hai nhánh bóc, và nhánh
  thứ hai là nơi bug sống.
- `traceId` là thứ duy nhất nối một màn hình người dùng đang nhìn với một dòng log ở server.

### 2.3 ⚠️ Casing — payload camelCase, khoá của `fieldErrors` PascalCase

Đặt `DictionaryKeyPolicy = CamelCase` làm gãy toàn bộ việc bind lỗi vào field trên form phía FE — và
gãy **im lặng**, không test nào bắt, vì cả hai bên vẫn là JSON hợp lệ. Cách viết đúng ở đây trông như
một lỗi — vì thế đây là một trong số ít chỗ được phép giữ comment trong code
([`README.md`](../../../quy-uoc/README.md) §5.4).

### 2.4 Các đường dựng envelope — danh sách đầy đủ

Một đường không có tên trong bảng là một đường không ai nghĩ tới việc kiểm; số dòng của bảng cố ý không
viết ra thành chữ — đếm bằng lệnh ([`.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §6). Mọi đường ở đây dựng
envelope **bằng tay** hoặc bọc lại envelope, nên luật `HandBuiltErrorEnvelope_AlwaysCarries_ABusinessCode`
áp thẳng vào từng dòng; ba ràng buộc của `EnvelopeMiddleware` hỏng im lặng nếu bỏ.

Vì sao ba ràng buộc của `EnvelopeMiddleware`:

1. **Chỉ bọc `/api`** — trả JSON cho một file tĩnh 404 biến một file thiếu thành một trang hỏng
   khó đoán.
2. **Không ghi đè response đã có thân** — ghi đè là thay một mã nghiệp vụ đúng (`CORE.USER.NOT_FOUND`)
   bằng một mã định tuyến sai, và cả hai đều là HTTP 404 nên không ai thấy.
3. **Không `MapFallback`** — nó đăng ký route bắt-tất-cả cho mọi verb, nên trở thành ứng viên hợp lệ
   của request sai verb và **nuốt mất chính mã 405** — sai verb biến thành 404.

Hai ca định tuyến cùng HTTP 404 với ca nghiệp vụ thì `code` là thứ **duy nhất** FE phân biệt được
"gọi sai URL" (bug của FE) với "bản ghi không tồn tại" (dữ liệu). 405 mang `type: "NotFound"` vì với
người gọi, cặp verb + đường dẫn đó không trỏ tới tài nguyên nào — cùng nhóm với route không khớp, và
cùng là bug của FE.

**Vì sao nhánh client huỷ chỉ bắt ca bị bọc.** Khi exception đứng đầu là `OperationCanceledException` hay
`IOException` và `RequestAborted` đã huỷ, `ExceptionHandlerMiddleware` của framework đã trả 499 và ghi log
mức `Debug` trước khi gọi `IExceptionHandler` nào. Chuỗi thông điệp của nhánh đó (*"The request was aborted by the
client"*) có trong `Microsoft.AspNetCore.Diagnostics.dll` của shared framework bản 10 đang cài, đo ngày 2026-09-22.
Còn lại là ca một tầng giữa **bọc** lần huỷ vào exception khác. Ca đó rơi xuống nhánh 500 và ghi một dòng
`Error` cho một việc không phải lỗi, nên cảnh báo vận hành kêu mỗi lần người dùng đóng tab giữa chừng. Không
ghi envelope vì client đã đi.

---

## 3. `ApiControllerBase`

Route không dùng token `[controller]` để đổi tên class không làm đổi URL công khai. Một controller kế
thừa thẳng `ControllerBase` là một controller không có `[Authorize]` mặc định và không đi qua
`ResultToHttpMapper` — đó là thứ `EveryController_Inherits_ApiControllerBase` canh.

Khối đầy đủ — chuyển từ file luật §3 ngày 2026-09-23; bảng trách nhiệm và luật S3 vẫn ở đó. **Năm** lối dưới **bốn** tên: hai overload `HandleResult`, `HandleCreated`, `HandleExport` ([ADR-0064](../../../adr/0064-nhanh-tra-tep-di-qua-apicontrollerbase.md)), `HandleFile` ([ADR-0068](../../../adr/0068-nhanh-tai-tep-di-qua-handlefile.md)) — và **một** `Failure` dùng chung cho nhánh lỗi của cả bốn:

```csharp
// Core.Web/Controllers/ApiControllerBase.cs
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(Envelope.Success(result.Value, HttpContext.TraceIdentifier)) : Failure(result.Error!);

    protected IActionResult HandleResult(Result result)
        => result.IsSuccess ? Ok(Envelope.Success<object>(null, HttpContext.TraceIdentifier)) : Failure(result.Error!);

    protected IActionResult HandleCreated<T>(Result<T> result, string location)
        => result.IsSuccess ? Created(location, Envelope.Success(result.Value, HttpContext.TraceIdentifier)) : Failure(result.Error!);

    protected IActionResult HandleExport(Result<ExportFile> result)
        => result.IsSuccess ? new ExportFileResult(result.Value) : Failure(result.Error!);

    protected IActionResult HandleFile(Result<FileDownload> result)
        => result.IsSuccess ? FileDownloadResponse.For(result.Value) : Failure(result.Error!);

    private IActionResult Failure(Error error)
        => StatusCode(ResultToHttpMapper.ToStatusCode(error.Type), Envelope.Failure(error, HttpContext.TraceIdentifier));
}
```

Hai lối tệp tách nhau bằng **kiểu tham số**, không bằng kỷ luật của người viết: `ExportFile` mang một delegate ghi, `FileDownload` mang một luồng **đã mở**. Gọi nhầm lối là lỗi biên dịch — và nhờ vậy dấu hiệu cú pháp `HandleExport` mà cổng S20 dò vẫn chỉ trỏ vào endpoint **xuất**, không trỏ nhầm sang một lần tải tệp thường (ADR-0068 quyết định 2).

Cả hai đi tới **một** lớp kết quả, `ExportFileResult` — chỗ duy nhất trong repo đặt ba header bắt buộc của một phản hồi tệp (`Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, `Cache-Control: no-store`). Lớp đó giữ `internal`, và tên nó là **hằng số cú pháp** của cổng S20 nên không đổi dù nay nó phục vụ cả hai nhánh. `FileDownloadResponse.For` chỉ là lối bọc: nó gói luồng đã mở vào một hàm ghi có `await using`, để việc **đóng luồng** — kể cả khi client ngắt giữa chừng — thuộc về đúng một chỗ thay vì một câu dặn trong chú thích của record.

### 3.1 Controller mẫu

Một `if` trong controller là một luật nghiệp vụ nằm ngoài tầm test của tầng Application. `try/catch` hay
`StatusCode(500, ...)` trong controller đều tạo nguồn sự thật thứ hai cho ánh xạ `Result` → HTTP.

Khối mẫu đầy đủ — chuyển từ file luật §3.1 ngày 2026-09-22 (`UsersController` thật còn có action `Export`, §7.2 ngoại lệ có tên):

```csharp
// Core.Web/Controllers/UsersController.cs
[ApiController]
[Route(CoreRoutes.Users)] // hằng số §8.1 — không gõ tay "api/v1/core/users"
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

---

## 4. Phân quyền — permission, KHÔNG có hằng số role

### 4.1 Luật gốc

**Vì sao:** dự án tiền nhiệm hardcode ba tên role trong Core. Hệ quả:

- Dự án thứ hai có mô hình vai trò khác (theo phòng ban, theo cấp) phải **sửa Core** hoặc
  phải tạo role giả mang đúng tên Core mong đợi.
- Thêm một vai trò mới là một lần **deploy**, không phải một thao tác quản trị.
- Câu hỏi *"ai làm được việc X"* không trả lời được bằng truy vấn — phải đọc code.
- Một điều kiện `if (role == "Admin")` rải rác nhiều chỗ, và không chỗ nào biết chỗ nào.

### 4.2 Ba mức phân quyền của endpoint — mỗi action đúng MỘT mức

Một action chỉ dựa vào `[Authorize]` của lớp cơ sở thì mở cho **mọi** tài khoản của **mọi** đơn vị — kể
cả tài khoản vận hành. Không khai mức nào là để một endpoint mở cho mọi tài khoản mà không ai quyết định
điều đó; khai hai mức là để người đọc phải đoán mức nào thắng. S11 bắt bằng ArchTest lúc build, không
phải lúc có người gọi thử.

Hai cookie event ghi `CORE.AUTH.NOT_AUTHENTICATED` (401) cho `ChallengeResult` và `CORE.AUTH.FORBIDDEN`
(403) cho `ForbidResult` — mỗi mã một đường dựng. Filter của `[RequireSystemOperator]` đi cùng ba bước:
action mang `[AllowAnonymous]` ⇒ trả về ngay, chưa đăng nhập ⇒ `ChallengeResult`, không mang cờ ⇒
`ForbidResult`. `[AuthenticatedOnly]` tồn tại để S11 đếm được và để lý do nằm ngay cạnh endpoint.

`GetEffectivePermissionsAsync` trả tập quyền hiệu lực: khoá gộp từ vai trò; tài khoản mang
`has_permission_bypass` nhận toàn bộ danh mục quyền hiện có. `HasPermissionAsync` trả lời từ chính tập này.
`IPermissionChecker` là seam nên test filter không cần DB. Ba attribute là metadata thuần — không chứa logic;
filter đọc chúng từ `EndpointMetadata`.

`UnauthorizedResult` bị cấm vì nó ra 401 không qua cookie scheme nên không mang envelope.

Khối `RequirePermissionFilter` đầy đủ — bốn bước ở file luật, viết ra:

```csharp
// Core.Web/Filters/RequirePermissionFilter.cs
internal sealed class RequirePermissionFilter(
    IPermissionChecker checker,
    ICurrentUser currentUser) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any())
            return;   // [AllowAnonymous] thắng — bảng "S11 đếm theo mức"

        var required = metadata
            .OfType<RequirePermissionAttribute>()
            .Select(a => a.Key)
            .ToArray();

        if (required.Length == 0)
            return;   // action mang mức khác (S11)

        if (currentUser.UserId is not { } userId)
        {
            context.Result = new ChallengeResult();
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

Khối khai ba attribute — chuyển từ file luật §4.2 ngày 2026-09-23; hình dạng và ràng buộc `AllowMultiple` là luật, ở đó:

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
```

Chữ ký seam `IPermissionChecker` — chuyển từ file luật §4.2 ngày 2026-09-23; ba vai của ba phương thức vẫn khai ở đó:

```csharp
// Core.Application/Permissions/IPermissionChecker.cs — seam
public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken ct);

    // Tập quyền hiệu lực (§4.3)
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct);

    // Tập quyền từng vai trò cấp, một truy vấn theo tập, không qua cờ bypass — luật chống leo thang, contracts/users.md §2
    Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetPermissionsForRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken ct);
}
```

### 4.3 Ma trận quyền theo resource

Khoá là hằng số **chuỗi**, không phải enum, để module thêm khoá của mình mà không sửa Core.

Chép cả danh mục khoá sang file luật là dựng nguồn thứ hai — cột `code` ở `schema-core.md` chính là dữ
liệu seed, nên đó là file chủ. Một hằng số lệch một ký tự nghĩa là `[RequirePermission(...)]` hỏi một khoá
không có trong dữ liệu: `IPermissionChecker` trả `false` cho **mọi** người, kể cả tài khoản đủ quyền, vì
deny-by-default.

Thêm một vai trò mới là một thao tác quản trị; đổi quyền của một vai trò cũng vậy. Phản hồi phiên tính
`permissions` lúc trả lời chứ không từ ảnh chụp lúc tạo tài khoản, và FE kiểm khoá như với mọi tài khoản
nên khoá của module lắp sau vẫn hiện đúng.

### 4.4 Điều gì `RequirePermission` KHÔNG làm

Đây là ranh giới có chủ đích: câu hỏi *"người này có được sửa đúng bản ghi này không"* nhét vào filter
buộc filter phải đọc dữ liệu nghiệp vụ, và filter sống ở `Core.Web` — nơi không được biết nghiệp vụ nào
tồn tại. Envelope lỗi dựng tay ở middleware và ở model binding là hai chỗ dễ quên `code` nhất.

---

## 5. `[AllowAnonymous]` — allowlist khai tường minh

**Vì sao phải khai tường minh:** một endpoint công khai là một quyết định bảo mật. Cách
duy nhất để nó được rà lại là bắt nó xuất hiện ở một chỗ mà người review đọc được **toàn
bộ** trong một lần nhìn. Rải `[AllowAnonymous]` khắp nơi thì câu hỏi *"những gì đang mở ra
Internet?"* không trả lời được nếu không đọc hết mọi controller. Việc thêm một dòng allowlist phải
giải trình trong PR — đó là mục đích.

Mặc định của `ApiControllerBase` là **chặn**; endpoint công khai phải khai `[AllowAnonymous]` tường minh.
Action mang `[AllowAnonymous]` mở cho người chưa đăng nhập — nên nó phải có tên trong danh sách như mọi
endpoint ẩn danh khác. Detector của S11 kiểm allowlist trước khi bỏ qua action ẩn danh để một action ẩn
danh ngoài danh sách không lọt qua khe giữa hai luật S4 và S11.

Quên mật khẩu tự phục vụ nằm ngoài v1 — quản trị đặt lại hộ.

Allowlist này cùng khuôn với hai danh sách khai tường minh khác — một danh sách ở một chỗ, ArchTest
đối chiếu mọi chỗ dùng với nó: miễn trừ tenant của luật M4
([`../../../database/schema-core.md`](../../../database/schema-core.md) §1.3) và bỏ bộ lọc của luật M5
([`../17-multi-tenant.md`](../17-multi-tenant.md) §7.1).

---

Khuôn khai allowlist ẩn danh — chuyển từ file luật §5 ngày 2026-09-23. **Bốn dòng dưới đây là ví dụ về hình dạng, không phải danh sách sống**: danh sách thật đọc từ chính tệp đó, và ArchTest `EveryAllowAnonymous_IsOn_TheAllowlist` cũng đọc code chứ không đọc tài liệu:

```csharp
// Core.Web/Security/AnonymousEndpointAllowlist.cs
public static class AnonymousEndpointAllowlist
{
    public static readonly IReadOnlySet<string> Endpoints = new HashSet<string>(StringComparer.Ordinal)
    {
        "GET /api/v1/core/diagnostics/probe",   // endpoint thử của B0
        "POST /api/v1/core/auth/login",
        // … dòng khác: đọc trong tệp
    };
}
```

## 6. Rate limiting

### 6.1 Các hàng rào

Hàng rào 3 chặn đúng kịch bản hai hàng rào theo IP bỏ lọt: phân vùng theo IP không thấy gì bất
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

Dạng đúng — khối đầy đủ, chuyển từ file luật §6.2 ngày 2026-09-22; con số là mặc định đã chốt ở đó:

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

Hàng rào 3 không có trong khối trên — nó nằm trong handler đăng nhập, file luật §6.5.

Action đăng nhập gắn policy theo tên. Đây là action của Core có việc giữa `Send` và `HandleResult` —
phát cookie phiên là việc của `Core.Web`; kiểu trả về của handler ở
[`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §7.4, thân action ở §7.4 của file này:

```csharp
[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting("login")]
public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
{
    ...   // thân action: §7.4 của file này
}
```

### 6.3 Ràng buộc

1. Cho client tự chọn phân vùng là tự vô hiệu hoá rate limit.

   > ⚠️ **Bật `UseForwardedHeaders` mà không khai proxy tin cậy thì TỆ HƠN là không bật.**
   > Không bật: mọi request mang IP của proxy, nên cả hệ dùng chung một phân vùng — giới hạn
   > quá chặt, nhưng **không ai vượt được**. Bật mà không khai: bất kỳ ai cũng đặt được header
   > chuyển tiếp tuỳ ý và **tự chọn phân vùng của mình**, tức rate limit không còn giới hạn gì.
   >
   > Hai trạng thái đó trông giống hệt nhau ở log và ở test — chỉ khác nhau khi có người thật
   > sự tấn công.
2. `login` dùng cửa sổ **cố định** vì với brute-force, khoá cứng tới hết cửa sổ là điều ta muốn.
   Giới hạn nền dùng cửa sổ **trượt** vì cửa sổ cố định cho phép dồn tới **2×** hạn mức khi burst
   vắt qua ranh giới phút, và phạt tới gần một phút người vô tình chạm ngưỡng.
3. Chặn health check gây báo động giả ở orchestrator. Endpoint phát token antiforgery là bước bắt buộc
   trước mọi request ghi, nên cũng miễn.
4. Chạy N instance thì ngưỡng thực tế là N lần ngưỡng đã khai — hạn mức nhân theo số instance không phải
   chi tiết tinh chỉnh sau.
5. Nêu ngưỡng ra là chỉ cho kẻ tấn công cách điều chỉnh tốc độ xuống vừa dưới nó.
6. FE cần phân biệt "thử lại sau" với "thao tác sai".

### 6.4 Con số là ước lượng, không phải số đo

Cách đọc log sau khi chạy thật: có 429 **không** kèm sự cố FE → nới lên; không có 429 nào trong nhiều
tuần → **vẫn giữ**. Có 429 không đi kèm sự cố FE nghĩa là hạn mức chặt hơn hành vi thật. Không có 429 nào nghĩa là hạn
mức đang không làm gì cả — vẫn giữ, vì nó không phải công cụ đo tải. Tấn công DDoS thật đến từ hàng
nghìn IP, mỗi IP dưới ngưỡng; không đặt kỳ vọng vào app.

### 6.5 `LoginPartitionKey`

**Vì sao không ở `UseRateLimiter`.** Mã đơn vị và tên đăng nhập nằm trong **thân** request. Hàm chọn
phân vùng của rate limiter là hàm **đồng bộ** nhận `HttpContext` và chạy trước model binding — muốn
đọc hai giá trị đó ở đấy thì phải tự đọc và đệm thân request, tức dựng một bộ phân tích thân thứ hai
cạnh model binding. Handler đăng nhập đã có sẵn cả hai giá trị, đã qua validator.

Vì sao khoá cần hai phần, cả hai đã chuẩn hoá:

| Phần | Vì sao cần |
| --- | --- |
| Mã đơn vị | Tên đăng nhập chỉ duy nhất **trong một đơn vị**. Phân vùng theo tên trần thì `admin` của mọi đơn vị dùng chung một hạn mức: dò `admin` ở đơn vị A khoá đăng nhập của `admin` ở **mọi** đơn vị — hàng rào chống dò mật khẩu biến thành công cụ từ chối dịch vụ xuyên đơn vị |
| Tên đăng nhập | Không chuẩn hoá thì `Admin`, `ADMIN`, `admin` là ba phân vùng — kẻ dò nhân hạn mức lên chỉ bằng cách đổi hoa thường |

Vì sao từng quyết định ở đường ra:

| Quyết định (tóm tắt) | Vì sao |
| --- | --- |
| Exception, không `Result` | `IExceptionHandler` đặt được cả status lẫn `Retry-After` mà không thêm một `ErrorType` chỉ để phục vụ một ca hạ tầng |
| Hiện thực ném, handler không ném | Luật R1 — Domain và Application không ném exception |
| Seam nhận giá trị thô | Chuẩn hoá tên đăng nhập dùng `ILookupNormalizer` của Identity — kiểu mà `Core.Application` không được thấy ([`../../../kien-truc-core-module.md`](../../../kien-truc-core-module.md) §2) |
| `Retry-After` từ cửa sổ bộ đếm | Cùng nguyên tắc card đăng nhập đặt cho hai hàng rào theo IP |

---

## 7. CORS + Cookie `SameSite=Lax` + antiforgery hai lớp

### 7.1 Chuỗi ràng buộc — vì sao vẫn cần antiforgery khi đã có `SameSite`

Đọc theo thứ tự, mỗi bước kéo theo bước sau:

1. FE và API ở **hai origin khác nhau nhưng cùng tên miền gốc** (dev: hai cổng của `localhost`;
   prod: hai subdomain) — [`../../../adr/0015-fe-va-api-khac-nguon.md`](../../../adr/0015-fe-va-api-khac-nguon.md).
2. `SameSite` xét theo **site** — scheme cộng tên miền gốc — **không** theo origin. Hai subdomain
   cùng scheme là **một** site.
3. Nên cookie phiên dùng **`SameSite=Lax`**: nó được gửi cho mọi request cùng site, kể cả
   `fetch` từ subdomain kia. **Không** cần `SameSite=None`.
4. `Lax` là lớp chống CSRF **đầu tiên**: request do một site khác phát sinh không mang cookie phiên.
5. Nhưng một subdomain **khác** của cùng tên miền gốc vẫn là cùng site với trình duyệt — `Lax`
   không chặn nó. Nên vẫn cần hai lớp.

Điều kiện của cả chuỗi — cùng tên miền gốc **và** cùng scheme — lệch thì:

| Lệch | Hậu quả |
| --- | --- |
| Khác scheme — FE `http`, API `https` | **Khác site.** Cookie `Lax` không được gửi ⇒ đăng nhập xong vẫn 401. Đó là lý do mọi môi trường, kể cả máy dev, chạy HTTPS |
| Khác tên miền gốc | Cookie thành **bên thứ ba**: phải `SameSite=None`, và trình duyệt có thể chặn hẳn. Đổi điều kiện này là một **ADR mới**, không phải một dòng cấu hình |

### 7.2 Hai lớp

**Vì sao đăng nhập và đăng xuất cũng cần token:** đăng nhập thiếu token là ép được người dùng đăng nhập vào tài khoản của kẻ tấn công (login CSRF) — mọi thứ họ làm sau đó nằm trong tài khoản người khác; đăng xuất bị ép là phiền, tuy nhẹ.

Hai lớp mang hai mã vì FE xử lý khác nhau: lấy token mới rồi gửi lại chữa được `CSRF_REJECTED`,
không chữa được `ORIGIN_REJECTED`. Ca *`Origin` lạ nhưng token hợp lệ vẫn 403 `ORIGIN_REJECTED`* là phép thử
chứng minh lớp 1 đang chạy.

Khối `AntiforgeryValidationMiddleware` đầy đủ — năm bước ở file luật, viết ra:

```csharp
// Core.Web/Security/AntiforgeryValidationMiddleware.cs
internal sealed class AntiforgeryValidationMiddleware(
    RequestDelegate next,
    IAntiforgery antiforgery,
    IOptions<CoreAuthOptions> authOptions)
{
    public async Task InvokeAsync(HttpContext http)
    {
        var endpoint = http.GetEndpoint();

        // Chỉ method ghi (bẫy thứ ba ở §7.5) — TRỪ action tự khai nó có tác dụng phụ; đọc METADATA, không đọc đường dẫn
        if (IsSafeMethod(http.Request.Method) &&
            endpoint?.Metadata.GetMetadata<RequireAntiforgeryAttribute>() is null)
        {
            await next(http);
            return;
        }

        var requiresAuthentication =
            endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null &&
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;

        if (requiresAuthentication && http.User.Identity?.IsAuthenticated != true)
        {
            await next(http);   // hết phiên ⇒ 401 ở UseAuthorization
            return;
        }

        var origin = http.Request.Headers.Origin.ToString();
        if (origin.Length > 0 &&
            !authOptions.Value.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            await SecurityEnvelopeWriter.WriteEnvelopeAsync(http, SecurityErrors.OriginRejected);   // lớp 1
            return;
        }

        // Lớp 2 — ValidateRequestAsync trong try/catch, KHÔNG IsRequestValidAsync (bẫy ngay dưới)
        if (!await HasValidTokenAsync(http))
        {
            await SecurityEnvelopeWriter.WriteEnvelopeAsync(http, SecurityErrors.CsrfRejected);
            return;
        }

        await next(http);
    }

    private async Task<bool> HasValidTokenAsync(HttpContext http)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(http);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsHead(method) ||
        HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);
}
```

**Hai bẫy của nhánh ngoại lệ có tên** — `GET` mang dấu `[RequireAntiforgery]` (endpoint xuất, [ADR-0062](../../../adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md)):

- **Bước 1 phải hỏi metadata của endpoint trước khi nhường cho method an toàn.** Nhường theo method rồi mới đọc endpoint là mọi `GET` đi qua, kể cả `GET` mang dấu.
- **Nhánh kiểm cho `GET` mang dấu không được gọi `IsRequestValidAsync`.** Hợp đồng của hàm đó nói thẳng: request dùng method an toàn thì **không kiểm token**, trả `true`. Gọi nó cho một `GET` là một cổng luôn xanh — không lỗi, không log, và test gọi export thiếu token vẫn 200. Đường đúng là `ValidateRequestAsync`, bắt `AntiforgeryValidationException` rồi ghi envelope `CsrfRejected`. Đây là đúng loại "cổng xanh rỗng" mà [`../../../audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md`](../../../audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md) đã trả giá — nên test S20 phải có ca *thiếu token → 403* chứ không chỉ ca *có token → 200*.

Vì sao phiên hết hạn phải ra 401 chứ không 403: token FE đang giữ gắn với danh tính lúc phát. Khi
phiên đã hết — cookie quá hạn, hoặc phép kiểm security stamp vừa từ chối principal — request tới
middleware này không còn danh tính, và kiểm token sẽ ra `CSRF_REJECTED` cho một ca thật ra là hết phiên.
Bỏ qua ở đây không mở lỗ nào: request đó không tới được action.

**Vì sao request ghi thiếu token tới đường dẫn không tồn tại ra 403 chứ không 404.** Bước 2 chỉ nhường khi đọc
được metadata của một endpoint; không có endpoint thì middleware đi tiếp bước 3–4 như với mọi request ghi.
Hệ quả có lợi: người ngoài không có token không phân biệt được đường dẫn có thật với đường dẫn bịa bằng
request ghi, vì cả hai cùng ra 403. Cái giá chỉ rơi vào người gỡ lỗi bằng `curl` không kèm token — họ thấy
`CSRF_REJECTED` cho một lỗi gõ sai đường dẫn. FE luôn gửi token ở request ghi, nên đi qua bước 4 và nhận
404 `CORE.ROUTE.NOT_FOUND` như thường.

### 7.3 CORS

Khối policy đầy đủ ([`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §7.3):

```csharp
services.AddCors(options => options.AddPolicy(CorsPolicyNames.Default, policy =>
{
    policy.WithOrigins(authOptions.AllowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()                         // bắt buộc để cookie đi qua
          .WithExposedHeaders("Retry-After", "Content-Disposition");
}));
```

- FE và API khác nguồn ([`../../../adr/0015-fe-va-api-khac-nguon.md`](../../../adr/0015-fe-va-api-khac-nguon.md)),
  nên header không nằm trong `WithExposedHeaders` vẫn tới trình duyệt mà mã FE đọc ra `null` — `Retry-After`
  của 429 và `Content-Disposition` của endpoint trả tệp là hai header FE cần đọc. Lỗi này **không có thông
  báo**: FE nhận đúng tệp, chỉ không biết tên. `WithOrigins` lấy từ `IOptions` để không hardcode.
- CORS là cơ chế của **trình duyệt**: nó không bảo vệ được trước một client không phải trình duyệt.
- `AllowAnyOrigin()` cùng `AllowCredentials()` gây lỗi lúc chạy — nhưng đó không phải hàng rào để
  trông vào.
- Ai đó "sửa" lỗi CORS bằng cách phản hồi lại đúng `Origin` mà client gửi lên là đã tự tay cho phép mọi
  trang web.

### 7.4 Cookie phiên

Status của hai cookie event lấy từ `ResultToHttpMapper`, không gõ tay — ánh xạ `ErrorType` → HTTP vẫn
ở đúng một chỗ. Nhờ seam `ISessionPrincipalFactory`, `Core.Web` phát phiên mà không chạm `UserManager`
và `Core.Infrastructure` không biết cookie tồn tại.

Khối cookie scheme đầy đủ — file luật chỉ giữ phần tuỳ chọn:

```csharp
// Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = authOptions.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(authOptions.SessionMinutes);

        // Không redirect sang trang login — ghi envelope
        options.Events.OnRedirectToLogin        = ctx => SecurityEnvelopeWriter.WriteEnvelopeAsync(ctx.HttpContext, SecurityErrors.NotAuthenticated);
        options.Events.OnRedirectToAccessDenied = ctx => SecurityEnvelopeWriter.WriteEnvelopeAsync(ctx.HttpContext, SecurityErrors.Forbidden);

        // Kiểm phiên ở MỌI request; còn hiệu lực thì gia hạn
        options.Events.OnValidatePrincipal = async ctx =>
        {
            if (!await IsSessionStillValidAsync(ctx))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            ctx.ShouldRenew = true;
        };
    });

static Task<bool> IsSessionStillValidAsync(CookieValidatePrincipalContext ctx)
{
    // Đọc CoreClaimTypes.TenantId / UserId / UserName / SecurityStamp / IssuedAt từ ctx.Principal.
    // IssuedAt + authOptions.SessionAbsoluteHours đã qua ⇒ false. Còn lại: mở IExecutionContextScope.Enter(tenantId, userId, userName)
    // rồi IIdentityService.IsSessionValidAsync(userId, securityStamp) — chỉ kiểm stamp — false ⇒ false;
    // sau đó ITenantLookup.FindByIdAsync(tenantId): null hoặc !IsActive ⇒ false. Hai seam, hai lần đọc —
    // quy-uoc/be-api-controller.md §7.4.
    ...
}
```

`timeProvider` mà action đăng nhập truyền `now` là `TimeProvider` của BCL. Handler của `me` lấy
`MustChangePassword` từ `UserSummaryDto` vì không có `CredentialCheck` trong tay.

`OnValidatePrincipal` chạy **trước** khi `HttpContext.User` được gán, nên `ITenantContext` chưa có giá trị;
mở phạm vi ngữ cảnh thực thi bằng `TenantId` từ claim của chính principal đang kiểm là một mục của
allowlist A12, không phải một lần bỏ bộ lọc. Phiên hết hiệu lực bị từ chối ngay tại request đó, và
request bị từ chối phiên thì không gia hạn.

**Thu hồi phiên tức thì** — không chờ cookie hết hạn, không chờ chu kỳ nào. Hiện thực phải gọi
`UserManager.UpdateSecurityStampAsync` tường minh vì không được dựa vào việc một API khác của Identity
có tự đổi stamp hay không — thiếu bước này thì phiên của người vừa bị khoá **vẫn sống**.

**Vì sao không dựa vào `SlidingExpiration`:** nó chỉ cấp lại cookie khi request tới lúc đã trôi quá
**nửa** `ExpireTimeSpan`, nên phiên thật có thể hết sớm hơn mốc `sessionMinutes` hứa với client tới gần
nửa hạn — bất kỳ bộ đếm nào dựng trên con số đó (FE v1 chưa có) sẽ báo khi phiên đã chết.

| Cái giá | Chấp nhận vì |
| --- | --- |
| **Mọi** response của request mang phiên hợp lệ đều kèm `Set-Cookie` | Mốc hết hạn ở server trùng mốc `sessionMinutes` mà DTO phiên hứa với client ([`../../../contracts/auth.md`](../../../contracts/auth.md) §3). Lệch tới nửa hạn thì người dùng mất dữ liệu đang nhập đúng lúc bấm Lưu |
| Request mà handler trả lỗi nghiệp vụ vẫn gia hạn | Người dùng vẫn đang thao tác, dù thao tác đó bị từ chối |

**Vì sao 30 phút, không "ghi nhớ đăng nhập":** một phiên dài hạn tuỳ chọn là một cookie sống nhiều
ngày trên máy dùng chung ở cơ quan — đúng chỗ giới hạn này tồn tại để chặn; `IsPersistent = false` làm
cookie thành cookie phiên của trình duyệt. Ba mươi phút là mức của
phần mềm nội bộ và dịch vụ công, và nằm trong khoảng OWASP khuyến nghị cho ứng dụng thường. Thấp hơn
mười lăm phút thì hộp thoại cảnh báo sắp hết phiên bật liên tục — và đó là lúc người dùng bắt đầu
đòi tắt hẳn nó đi. Đổi giá trị này không phải một lần chỉnh cho tiện.

**Vì sao hai `Events` redirect không phải tuỳ chọn:** mặc định của cookie authentication là
**redirect 302** sang trang đăng nhập. Với một SPA, redirect đó biến một lỗi 401 rõ ràng thành một
response HTML 200 mà FE parse thành rác.

**Vì sao handler đăng nhập trả `LoginOutcome` thay vì `ClaimsPrincipal`, và controller không gọi lại `me`:**
handler đã tra đủ mọi thứ `SessionDto` cần; gọi thêm query của `me` là lần đọc thứ hai cho cùng dữ liệu, và là
chỗ thứ hai có thể trả khác đi. Principal dựng từ `LoginOutcome` thì factory không truy vấn, nên không phụ thuộc
phạm vi đơn vị của handler — controller gọi được sau khi phạm vi đã đóng.

**Vì sao `SecurityStamp` của `LoginOutcome` lấy từ `CredentialCheck`:** stamp là giá trị nội bộ của Identity;
seam trả nó ngay trong kết quả kiểm mật khẩu thì handler không cần một thao tác tra cứu thứ hai, và không DTO
tra cứu nào phải mang stamp ra ngoài — đúng lý do ADR-0026 loại phương án D.

**Vì sao `issuedAt` là tham số của factory, không phải factory tự lấy giờ:** cấp lại cookie sau đổi mật khẩu
phải giữ trần tuyệt đối của phiên cũ; factory tự lấy giờ thì mỗi lần đổi mật khẩu là một lần reset trần —
đổi mật khẩu định kỳ thành cách kéo dài phiên vô hạn. Đưa giờ từ ngoài vào cũng làm factory test được bằng giá trị.

**Vì sao lớp mã ở `Core.Web/Security/` tên `SecurityErrors`, không phải `AuthErrors`:** `Core.Application/Auth/`
đã có `AuthErrors` cho mã `CORE.AUTH.*` mà handler phát; hai lớp trùng tên ở hai project là hai `using` và một
lần chọn nhầm im lặng. Tên khác nhau nói luôn ai phát: `SecurityErrors` là hạ tầng HTTP (origin, CSRF, chưa
đăng nhập, cấm), `AuthErrors` là nghiệp vụ đăng nhập.

**Vì sao permission không vào cookie, còn `MustChangePassword` và `IsSystemOperator` thì có:** quyền phải có
hiệu lực ở request kế tiếp khi vai trò đổi ([`../02-identity-auth.md`](../02-identity-auth.md) §5) — chụp vào
cookie là quyền cũ sống tới khi đăng nhập lại; cờ buộc đổi mật khẩu chỉ đổi ở hai đường mà cả hai đều phát lại
cookie hoặc đổi security stamp (đổi mật khẩu, đặt lại hộ), nên claim không bao giờ cũ hơn DB; cờ vận hành chỉ
đặt lúc tạo tài khoản ([`../../../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../../../adr/0023-dich-vu-tao-don-vi-dung-chung.md))
nên cũng vậy.

Khối `AuthController` mẫu — cấp cookie sau đăng nhập và sau đổi mật khẩu ([`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §7.4):

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


**Vì sao phép kiểm phiên gộp `is_active` của đơn vị:** đơn vị ngưng hoạt động phải đá phiên đang mở ở request kế
tiếp — cùng yêu cầu "thu hồi tức thì" với khoá tài khoản; gộp vào lần đọc đã có thì không thêm chi phí
([`../02-identity-auth.md`](../02-identity-auth.md) §6.3).

Helper ghi envelope từ hạ tầng — chuyển từ file luật §7.4 ngày 2026-09-23; catalog mã vẫn là định nghĩa gốc ở đó:

```csharp
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

### 7.5 ⚠️ Antiforgery — hai nửa, hai đường

**Vì sao tên cookie khai một chỗ:** trùng tên thì trình duyệt **ghi đè**, vì nó không có khái niệm
"hai cookie khác mục đích cùng tên". Hậu quả không nhất quán nên rất khó chẩn đoán: lúc thì request
ghi bị từ chối dù vừa tải trang, lúc thì người dùng bị đăng xuất giữa chừng — tuỳ phản hồi nào tới
trước. Một dòng test chặn đúng lớp lỗi này.

**Vì sao không có cookie thứ hai chứa request-token:** mô hình double-submit cổ điển đặt request-token
vào một cookie cho JS đọc. Ở đây FE và API khác origin, nên muốn JS đọc được thì cookie đó phải
mở cho **mọi** subdomain của tên miền gốc. Trả trong body hẹp hơn: chỉ origin được CORS cho phép
đọc được nó.

**Bẫy thứ hai — token gắn với danh tính lúc phát hành:** token phát cho người ẩn danh không hợp lệ cho phiên vừa đăng nhập, và token của phiên cũ không hợp lệ sau đăng xuất; FE không gọi lại thì request ghi đầu tiên sau mỗi lần đổi danh tính bị 403 `CSRF_REJECTED` — lỗi "đăng nhập xong bấm gì cũng hỏng một lần". **Bẫy thứ ba — chỉ áp cho method ghi:** áp cho `GET` thì mọi tải trang đầu đều cần token trước khi có token.

Endpoint phát token — request-token đi trong **body**, không set cookie nào cho JS:

```csharp
[HttpGet("token")]
[AllowAnonymous]
[DisableRateLimiting]
public IActionResult GetToken([FromServices] IAntiforgery antiforgery)
{
    var tokens = antiforgery.GetAndStoreTokens(HttpContext);   // set cookie-token nội bộ
    return Ok(Envelope.Success(new { token = tokens.RequestToken }, HttpContext.TraceIdentifier));
}
```

**Bẫy thứ hai:** `IAntiforgery` gắn request-token với danh tính lúc phát hành, nên token phát lúc **chưa
đăng nhập** không dùng được cho request ghi **sau khi** đăng nhập. Token FE đang giữ không tự đổi theo sự kiện
đăng nhập; chỉ có token mới khi FE gọi lại endpoint phát token — không chỉ một lần lúc khởi động app.

**Bẫy thứ ba:** áp validate antiforgery cho `GET` sẽ chặn cả lần tải trang đầu tiên, khi cookie còn
chưa tồn tại.

---

## 8. Versioning, Swagger, health check

### 8.1 Tiền tố đường dẫn

Phiên bản nằm trong đường dẫn vì đó là thứ đọc được trong log, trong bookmark, và trong một
câu `curl` dán vào issue. Core sở hữu `/api/vN/core/…`, module sở hữu `/api/vN/<module>/…` — không tranh
chấp không gian tên. Chuỗi đường dẫn viết ở hai chỗ là chuỗi sẽ lệch — lệch ở điều kiện rate limit là cả
hàng rào ngừng chặn.

**Vì sao `ApiRoutes.Root` và `ApiRoutes.V1` là `public`.** Module khai route dưới `ApiRoutes.V1 + "/<module>"`,
và `EnvelopeMiddleware` chỉ bọc response dưới tiền tố `ApiRoutes.Root`. Nếu module tự gõ tiền tố thì một lần
gõ lệch là endpoint của module ra thân trần, không envelope, mà không lỗi nào báo. Cái giá là hai hằng số đó
thành hợp đồng với module: đổi tên chúng là thay đổi phá vỡ. Tiền tố khu `core` thì không đưa ra ngoài. Module
không bao giờ đăng ký dưới `/core`, nên một hằng số `public` cho tiền tố đó chỉ mở sẵn đúng đường sai.

Dòng "đổi ngữ nghĩa mà giữ nguyên hình dạng" là dòng đáng sợ nhất của bảng phát hành: hình dạng
không đổi nên không cổng nào bắt được, mà client vẫn hiểu sai. Ở dự án tiền nhiệm, việc *"bỏ một
phần tử khỏi payload ghi đè ma trận"* đổi nghĩa từ "thu hồi sạch" thành "lỗi 400" mà không đổi một
byte nào của schema.

### 8.2 Swagger / OpenAPI

Thiếu `[ProducesResponseType]` cho nhánh lỗi thì tài liệu sinh ra nói dối về hình dạng envelope. Hợp đồng
nằm ở `contracts/` vì nơi đó ghi cả luật nghiệp vụ và mã lỗi có thể trả về — thứ OpenAPI không diễn đạt được.

### 8.3 Health check

Gộp hai endpoint làm một thì một sự cố DB tạm thời sẽ khiến orchestrator **restart** app — làm mọi
thứ tệ hơn, vì restart không sửa được DB. Chúng do orchestrator và load balancer gọi, không phải client
nghiệp vụ, nên không đi theo vòng đời phiên bản của API. Chi tiết nội bộ không được lộ: tên máy chủ, chuỗi
kết nối, phiên bản package. §8.1 của file luật khai đúng ngoại lệ này và không khai ngoại lệ nào khác.

Một app chạy trên schema cũ hơn model là một app ghi sai dữ liệu, không phải một app hơi lỗi thời.
