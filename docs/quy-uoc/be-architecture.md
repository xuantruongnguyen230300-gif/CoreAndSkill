---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — kiến trúc, layout, composition root

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Toàn bộ file mô tả thứ `src/BE` **phải trở thành** ở
> giai đoạn 2. Repo chưa có `src/`, nên không đoạn nào ở đây là mô tả hiện trạng và không
> đoạn nào được trích dẫn như bằng chứng.
>
> 📖 Lý do và các phương án đã loại nằm ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md)
> và [`../adr/0002-core-5-project.md`](../adr/0002-core-5-project.md). File này chỉ nêu
> **quy tắc thi hành**, không lặp lại phần lý luận.

---

## 1. Project layout & chiều phụ thuộc

Đây là mục quan trọng nhất của file. Mọi câu hỏi *"file này bỏ vào đâu"* trả lời bằng bảng
dưới đây, và chỉ bằng bảng dưới đây.

### 1.1 Project nào chứa gì — và các seam của `Core.Application`

> 📖 **Danh sách project Core, thứ mỗi project chứa, thứ mỗi project CẤM chứa, và
> project nào tham chiếu được project nào: đọc
> [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2.**
>
> Bảng đó **cố ý không chép lại vào đây.** Hai bản sẽ lệch — khuôn
> [`../OWNERSHIP.md`](../OWNERSHIP.md) mô tả.

Tên đầy đủ của assembly là `CoreAndSkill.Core.<Tầng>`; host là `CoreAndSkill.Api`; module là
`CoreAndSkill.Modules.<X>.*`.

**Thứ mục này sở hữu là danh sách seam** — interface `Core.Application` khai để tầng ngoài
implement. Đây là bề mặt DIP của Core, và nó phải đọc được trong một lần nhìn:

| Seam | Ai implement | Vì sao nó là interface chứ không phải lớp cụ thể |
| --- | --- | --- |
| `IUnitOfWork` | `Core.Infrastructure` | Handler không được biết `DbContext` tồn tại — [`be-cqrs-handler.md`](be-cqrs-handler.md) §4 |
| `ICurrentUser` | `Core.Web` (`HttpContextCurrentUser`) | Danh tính đến từ `HttpContext`, mà `Core.Application` và `Core.Infrastructure` đều **cấm** chạm `HttpContext` |
| `ITenantContext` | `Core.Web` | Đơn vị hiện hành của request. `CoreDbContext` nhận nó qua constructor để dựng bộ lọc tenant — [`be-entity-domain.md`](be-entity-domain.md) §5.1, [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) |
| `IPermissionChecker` | `Core.Infrastructure` | Tra ma trận quyền trong DB; test không cần DB |
| `IIdentityService` · `IUserLookupService` · `IUserAdminService` | `Core.Infrastructure` | Giữ `AppUser`/`AppRole` không rời Infrastructure — [`be-entity-domain.md`](be-entity-domain.md) §7 |
| `ICacheStore` | `Core.Infrastructure` | Đổi bản cài trong bộ nhớ sang Redis là đổi **một dòng đăng ký**, không sửa chỗ gọi — [`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md) |
| `IBackgroundJobScheduler` | `Core.Infrastructure` | Không lộ kiểu của thư viện job nền ra Application — [`be-cqrs-handler.md`](be-cqrs-handler.md) §10 |
| `IExecutionContextScope` | `Core.Infrastructure` | Đặt danh tính và đơn vị cho mã **không có** `HttpContext` để đọc — mục *Danh tính và đơn vị khi không có request* bên dưới |
| `ISettingStore` | `Core.Infrastructure` | Đọc/ghi cấu hình theo đơn vị và cache của nó — [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md) §4 |
| `ICodeSequence` | `Core.Infrastructure` | Cấp mã nghiệp vụ kế tiếp trong cùng giao dịch — [`../wiki-core/be/20-sinh-ma-nghiep-vu.md`](../wiki-core/be/20-sinh-ma-nghiep-vu.md) §2 |
| `IPermissionCatalogSource` | Mỗi module, và Core cho khoá của chính nó | Core giữ cơ chế phân quyền, module cấp dữ liệu — mục *Danh mục khoá quyền do module cấp* bên dưới |

`ITenantContext` và `ICurrentUser` là **hai** seam chứ không phải một: `ICurrentUser` chỉ có
giá trị **sau khi** đã xác thực, còn `ITenantContext` phải có giá trị **trước** bước xác thực —
luồng đăng nhập tra đơn vị theo mã đơn vị rồi nạp vào ngữ cảnh, và chỉ sau đó mới gọi
`UserManager`. Gộp hai seam làm bước đó không viết được.

#### Danh tính và đơn vị khi không có request — định nghĩa gốc

Có những đoạn mã phải biết đơn vị và người kích hoạt mà **không có** `HttpContext` để đọc,
hoặc có mà chưa mang đơn vị: job nền, bộ phát outbox, lệnh seed, và bước đăng nhập (trước khi
có phiếu thì chưa có claim đơn vị nào).

Cơ chế: seam `IExecutionContextScope` mở một **phạm vi ngữ cảnh thực thi**. Hiện thực ở
`Core.Infrastructure`, giữ giá trị trong `AsyncLocal` — không chạm `HttpContext` nên được phép
ở tầng đó.

```csharp
// Core.Application
public interface IExecutionContextScope
{
    // Mọi ICurrentUser / ITenantContext đọc bên trong khối using trả giá trị này.
    IDisposable Enter(Guid tenantId, Guid? userId, string? userName);
}
```

`ICurrentUser` và `ITenantContext` đọc theo thứ tự: **phạm vi đang mở → `HttpContext` → không có gì**.

| Luật | Vì sao |
| --- | --- |
| Chỉ **bộ lọc job nền, bộ phát outbox, lệnh seed** và **bước đăng nhập** được mở phạm vi — luật ở [`../RULES.md`](../RULES.md) §3 | Mở phạm vi ở chỗ khác là tự chọn đơn vị — đúng lỗ mà luật M2 chặn ở đường vào HTTP. Bước đăng nhập là ngoại lệ đã khai của M2 |
| Job nền **chụp** đơn vị và người kích hoạt lúc enqueue, khôi phục lúc chạy — cơ chế nằm trong hiện thực của `IBackgroundJobScheduler`, **không** ở chỗ gọi | Nằm ở chỗ gọi thì mỗi nơi enqueue phải nhớ truyền, và nơi nào quên sẽ lặng lẽ ghi dữ liệu không danh tính |
| **Không** chụp vai trò hay quyền | Job chạy sau vài phút mà quyết định theo ảnh chụp thì quyền vừa bị thu hồi vẫn còn hiệu lực. Job cần phân quyền thì đọc quyền **hiện tại** từ DB |
| Bộ phát outbox mở phạm vi bằng `TenantId` của **chính dòng outbox** | Dòng biết nó thuộc đơn vị nào; tiến trình phát thì không |
| Không có **người** là hợp lệ — nhật ký ghi `system`. Không có **đơn vị** thì không hợp lệ: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §2 | Job hệ thống không do ai kích hoạt; nhưng dữ liệu đơn vị mà không có đơn vị là dữ liệu vô hình với mọi người |

#### Danh mục khoá quyền do module cấp — định nghĩa gốc

Core giữ **cơ chế** phân quyền — kiểm deny-by-default, ma trận, seed idempotent. Tập **khoá**
là dữ liệu: mỗi module cấp khoá của mình qua seam `IPermissionCatalogSource`, và Core cấp khoá
của chính nó qua cùng seam (danh mục ở [`../database/schema-core.md`](../database/schema-core.md) §5.2).

```csharp
// Core.Application
public sealed record PermissionDefinition(string Code, string LabelKey);

public interface IPermissionCatalogSource
{
    IReadOnlyCollection<PermissionDefinition> GetPermissions();
}
```

| Luật | Vì sao |
| --- | --- |
| Mỗi module đăng ký **đúng một** nguồn trong `AddXModule()`; Core gộp mọi nguồn | Core không được biết module nào tồn tại — luật A3 |
| Danh mục đã gộp được kiểm **lúc khởi động** bằng hosted service: khoá trùng giữa các nguồn, khoá sai khuôn `<tài nguyên>.<hành động>`, nhãn rỗng ⇒ **tiến trình không khởi động** | Kiểm lúc dùng thì host khai sai vẫn chạy bình thường, rồi trả 500 đúng lúc ai đó mở màn phân quyền. Cùng cơ chế với kiểm cấu hình lúc khởi động (§4) nên hai loại cấu hình sai hỏng cùng một kiểu |
| `LabelKey` là **khoá dịch**, không phải câu hiển thị | BE sở hữu mã, không sở hữu câu chữ — [`be-cqrs-handler.md`](be-cqrs-handler.md) §7.2 |
| **Không** có hiện thực mặc định | Thiếu đăng ký thì DI hỏng ngay lúc khởi động — hỏng ồn ào thay vì một màn phân quyền trống |
| Ánh xạ vai trò → quyền **không** đi qua seam này | Đó là dữ liệu của dự án, seed riêng — [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4. Core không có hằng số vai trò nào (luật S1) |

### 1.2 Sơ đồ chiều phụ thuộc

```
                        Domain
                          ▲
                          │
                     Application ─────────────┐
                       ▲     ▲                │
                       │     │                │ (khai interface)
              Infrastructure  │                │
                       ▲      │                ▼
                       │      │        (Infrastructure implement)
                       └──── Web
                              ▲
                              │
                        Api (host)
                              ▲
                              │
                   Modules.<X>.Endpoints
                              ▲
                   Modules.<X>.Infrastructure
                              ▲
                   Modules.<X>.Application
                              ▲
                     Modules.<X>.Domain


   Contracts ── không tham chiếu ai, và được cả Core lẫn mọi module tham chiếu
```

Đọc ngược mũi tên: `Application` biết `Domain`; `Domain` **không** biết ai. Đây là DIP viết
thành đồ thị tham chiếu — không phải quy ước bằng lời.

### 1.3 Danh sách cấm — mỗi dòng là một ArchTest

Mỗi dòng dưới đây có **một** ArchTest canh. Tên của chúng khai ở cột "Ép bằng gì" của
[`../RULES.md`](../RULES.md) §3 — không chép lại ở đây.

| Cấm |
| --- |
| `Core.Domain` có bất kỳ `PackageReference` nào |
| `Core.Application` chạm EF Core / ASP.NET Core / bất kỳ Infrastructure nào |
| `Core.*` tham chiếu bất kỳ assembly `Modules.*` nào |
| `Modules.A` tham chiếu `Modules.B` |
| Source của `Core.*` chứa chuỗi literal đặt tên tầng nghiệp vụ |
| Project trên đĩa không được khai trong solution |
| Host chứa middleware hoặc controller |

### 1.4 Vì sao giữ luật này từ slice đầu tiên

Chi phí giữ layer sạch từ commit đầu tiên gần như bằng **0**: nó chỉ là việc đặt file vào
đúng thư mục. Chi phí gỡ rối sau khi `Core.Domain` đã dính EF Core thì rất cao và thường
phải viết lại — vì lúc đó entity đã mang navigation property, lazy loading, và các thuộc
tính chỉ có nghĩa khi có `DbContext`.

**Không có "code cũ" nào biện minh cho việc phá luật**, vì ở repo này chưa có code cũ.

### 1.5 Khi Infrastructure của Core cần dữ liệu của một module

Tình huống: một job nền dùng chung ở `Core.Infrastructure` cần dọn dữ liệu cũ của một
module cụ thể. Inject thẳng repository của module đó là vi phạm `Core.* → Modules.*`.

Cách đúng: khai **interface hẹp** ở `Core.Application`, module tự implement ở
`Modules.<X>.Infrastructure`, host đăng ký. `Core.Infrastructure` chỉ biết interface.

```csharp
// Core.Application/Maintenance/IStaleDataCleaner.cs
public interface IStaleDataCleaner
{
    string Name { get; }
    Task<int> CleanAsync(DateTimeOffset olderThan, CancellationToken ct);
}
```

Job nền của Core inject `IEnumerable<IStaleDataCleaner>` và chạy hết. Core không biết
module nào tồn tại; module không biết job nào gọi mình. Thêm module thứ hai không sửa
một dòng nào của Core.

---

## 2. `Core.Web` chứa gì — và vì sao nó KHÔNG được nằm ở host

Đây là mục mới so với mọi tài liệu tiền nhiệm, và là lý do tồn tại của cả layout.

### 2.1 Danh sách thành phần

| Thành phần | Vai |
| --- | --- |
| `ApiControllerBase` | Lớp cơ sở mang `[Authorize]` (fail-closed) và helper `HandleResult` |
| `ResultToHttpMapper` | Ánh xạ `ErrorType` → HTTP status — **đúng một chỗ** trong toàn hệ |
| `ExceptionHandlingMiddleware` | Bắt exception ngoài dự kiến, dịch thành envelope, không lộ stack trace |
| `EnvelopeMiddleware` | Bọc envelope cho các response do **hạ tầng định tuyến** sinh (404 không khớp route, 405 sai verb) — chúng có thân rỗng nên không đi qua handler nào |
| `TraceIdMiddleware` | Gắn `traceId` vào `HttpContext` và vào mọi envelope |
| `OriginValidationMiddleware` | Chặn request ghi có `Origin` ngoài allowlist — lớp CSRF thứ nhất, xem [`be-api-controller.md`](be-api-controller.md) |
| Cấu hình rate limit | Theo IP **và** theo tên đăng nhập; chi tiết ở [`be-api-controller.md`](be-api-controller.md) |
| `HttpContextCurrentUser` | Implementation của `ICurrentUser` **và** `ITenantContext` — chỗ **duy nhất** đọc `HttpContext` để lấy danh tính và đơn vị |
| `AntiforgeryValidationMiddleware` | Kiểm token CSRF cho mọi method ghi — lớp CSRF thứ hai, xem [`be-api-controller.md`](be-api-controller.md) §7.2 |
| `PasswordChangeRequiredMiddleware` | Chặn tài khoản đang ở trạng thái bắt buộc đổi mật khẩu, trừ allowlist ở [`../contracts/auth.md`](../contracts/auth.md) §1.2. Chặn ở đây chứ không để FE điều hướng: FE điều hướng là trải nghiệm, một request gọi thẳng bằng `curl` đi qua được |
| `ModelBindingProblemFactory` | Thay `InvalidModelStateResponseFactory` mặc định để lỗi model binding cũng ra đúng envelope |
| Controller của Core | Auth, người dùng, vai trò, permission, menu động — những màn hình mọi sản phẩm đều cần |
| `AddCore()` / `UseCore()` | Hai extension method là **toàn bộ** bề mặt lắp ghép của Core |

### 2.2 Vì sao chúng không được ở host

Ở dự án tiền nhiệm, toàn bộ danh sách trên nằm trong project host. Hệ quả đo được: **dự
án thứ hai muốn dùng lại Core phải copy-paste hơn 600 dòng `Program.cs` cộng 14 file.**

Đó không phải tái sử dụng — đó là nhân bản. Ba hậu quả kéo theo, và cả ba đều không tự
lộ ra:

1. **Mọi bugfix hạ tầng phải sửa ở từng dự án.** Bẫy antiforgery hai cookie trùng tên
   (xem [`be-api-controller.md`](be-api-controller.md)) sửa ở dự án A không chạm dự án B.
2. **Hai bản sao trôi khỏi nhau ngay lập tức.** Dự án B sửa một dòng cho hợp cảnh của
   mình; từ đó không còn "Core" nữa, chỉ còn hai codebase giống nhau một phần.
3. **Không có chỗ nào để viết ArchTest.** Luật *"mọi controller kế thừa `ApiControllerBase`"*
   cần một `ApiControllerBase` **được ship**, không phải một class mỗi dự án tự chép.

`Core.Web` tồn tại để **Core tự đứng được**. Phép thử một dòng:

> Một dự án mới, chưa viết gì, chỉ tham chiếu `Core.Web` và gọi `AddCore()` + `UseCore()`
> — phải có ngay: đăng nhập, đổi mật khẩu, quản trị người dùng, phân quyền, menu động,
> envelope thống nhất, rate limit, CSRF. Không chép file nào.

Thiếu bất kỳ mục nào trong danh sách đó nghĩa là mục đó đang nằm sai chỗ.

### 2.3 Ranh giới của `Core.Web`

`Core.Web` **được** biết `Core.Infrastructure` — nó là nơi nối dây. Nhưng nó **không**
được:

- Gọi thẳng `CoreDbContext`. Mọi truy cập dữ liệu đi qua MediatR.
- Chứa nghiệp vụ. Controller chỉ `Send` rồi `HandleResult`.
- Biết tên bất kỳ module nào. Luật `CoreSource_MustNotContain_BusinessNameStringLiteral`
  áp cho cả `Core.Web`.

---

## 3. Host mỏng — composition root duy nhất

Project `CoreAndSkill.Api` **chỉ được** chứa: `Program.cs`, `appsettings*.json`, và lời
gọi đăng ký module của dự án.

**CẤM** chứa: middleware, controller, entity, handler, migration, filter, extension method
có logic. Luật `Host_MustNotContain_MiddlewareOrController` bắt điều này.

Ngưỡng tự kiểm: **`Program.cs` dưới 50 dòng.** Vượt ngưỡng nghĩa là có thứ lẽ ra thuộc
`Core.Web` đang nằm sai chỗ — sửa bằng cách chuyển nó vào `Core.Web`, không phải bằng
cách nới ngưỡng.

```csharp
// CoreAndSkill.Api/Program.cs — đích đến
using CoreAndSkill.Core.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCore(builder.Configuration, builder.Environment);
// builder.Services.AddSkillModule(builder.Configuration);   // mỗi module một dòng

var app = builder.Build();

app.UseCore();
// app.UseSkillModule();

app.Run();
```

Ba điều đáng chú ý trong đoạn trên:

- **Không có `AddControllers()`, `AddCors()`, `UseAuthentication()`** — chúng nằm bên
  trong `AddCore()`/`UseCore()`. Kéo chúng ra host là cho phép mỗi dự án tự đặt lại thứ
  tự middleware, mà thứ tự middleware là thứ hỏng **im lặng**: đặt `UseCors` sau
  `UseAuthorization` thì preflight vẫn qua nhưng request thật bị chặn ở nơi khó đoán.
- **`AddCore` nhận cả `IHostEnvironment`.** Một số luật chỉ bật ở Production (fail-fast
  cấu hình, `Secure` cookie bắt buộc); truyền environment vào là cách duy nhất để Core
  quyết định mà không cần đọc biến toàn cục.
- **Mỗi module đúng một dòng đăng ký.** Module tự gộp controller assembly của mình bên
  trong `AddXxxModule()`. Host không gọi `AddApplicationPart` cho ai cả.

### 3.1 Thứ tự pipeline — định nghĩa gốc

Thứ tự pipeline là một quyết định kiến trúc, không phải chi tiết cấu hình. Nó khai **một
lần** trong `UseCore()`, và khối dưới đây là **nguồn duy nhất** của thứ tự đó — file khác trỏ
về đây, không chép lại ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2):

```csharp
// Core.Web/DependencyInjection/CoreApplicationBuilderExtensions.cs
public static WebApplication UseCore(this WebApplication app)
{
    app.UseForwardedHeaders();              // ĐẦU TIÊN — ràng buộc 4 bên dưới
    app.UseMiddleware<TraceIdMiddleware>();
    app.UseExceptionHandler();              // đăng ký IExceptionHandler ở AddCore
    app.UseMiddleware<EnvelopeMiddleware>();
    app.UseCors(CorsPolicyNames.Default);
    app.UseMiddleware<OriginValidationMiddleware>();
    app.UseAuthentication();
    app.UseMiddleware<AntiforgeryValidationMiddleware>();
    app.UseAuthorization();
    app.UseMiddleware<PasswordChangeRequiredMiddleware>();   // SAU UseAuthorization
    app.UseRateLimiter();
    app.MapControllers();
    app.MapHealthChecks("/health/live",  new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
    return app;
}
```

Những chỗ trong thứ tự trên không hoán đổi được:

1. **`PasswordChangeRequiredMiddleware` nằm SAU `UseAuthorization`.** Nó cần biết người gọi là
   ai, và nó trả **403** chứ không 401 — đặt trước `UseAuthentication` thì nó không có danh
   tính để kiểm và sẽ chặn cả người chưa đăng nhập. Nó dựng envelope **bằng tay**
   (`CORE.AUTH.PASSWORD_CHANGE_REQUIRED`) và là một trong các đường dựng envelope ở
   [`be-api-controller.md`](be-api-controller.md) §2.4. Allowlist bốn đường đi qua được:
   [`../contracts/auth.md`](../contracts/auth.md) §1.2.
2. **`AntiforgeryValidationMiddleware` nằm SAU `UseAuthentication`.** `IAntiforgery` gắn
   request-token với danh tính tại thời điểm phát hành, nên nó cần danh tính đã dựng xong.
3. **`UseCors` nằm TRƯỚC mọi middleware chặn.** Đặt sau thì preflight vẫn qua nhưng request
   thật bị chặn ở một chỗ khó đoán.
4. **`UseForwardedHeaders` đứng ĐẦU TIÊN.** Mọi bước sau nó đọc IP và scheme của request —
   rate limit phân vùng theo IP, cookie `Secure` cần biết request là HTTPS, log ghi IP. Đặt
   muộn thì các bước đứng trước nó thấy IP và scheme **của proxy**.

   `AddCore` **xoá danh sách proxy tin cậy mặc định của framework** rồi nạp lại từ cấu hình;
   danh sách rỗng nghĩa là **không tin proxy nào** — trạng thái đúng khi app chạy không có
   proxy. Hai cách hỏng, cả hai đều im lặng:

   | Hỏng | Hậu quả |
   | --- | --- |
   | Có proxy mà thiếu cấu hình | App tưởng mọi request là HTTP ⇒ không phát cookie mang `Secure` ⇒ endpoint phát token antiforgery lỗi, **không request ghi nào đi qua** — trong khi `/health` vẫn xanh nên triển khai vẫn báo thành công |
   | Tin mọi nguồn gửi header chuyển tiếp | Ai cũng giả được IP để tự chọn phân vùng rate limit — [`be-api-controller.md`](be-api-controller.md) §6.3 |

**Hai endpoint health, không phải một.** `/health/live` không chạm DB (orchestrator dùng để
quyết định restart); `/health/ready` kiểm DB và migration (load balancer dùng để quyết định
đưa vào luồng). Gộp làm một thì một sự cố DB tạm thời khiến orchestrator **restart** app — làm
mọi thứ tệ hơn, vì restart không sửa được DB. Cả hai phải có tên trong allowlist ẩn danh ở
[`be-api-controller.md`](be-api-controller.md) §5.

Luật `EveryMiddlewareClass_IsWiredInto_Pipeline` canh chiều ngược lại: một middleware
được khai mà **không** ai nối vào pipeline là một lớp bảo vệ tồn tại trên giấy. Đây là
dạng lỗi tệ nhất — nó tạo cảm giác được bảo vệ. `PasswordChangeRequiredMiddleware` từng
**thiếu hẳn** khỏi khối trên trong khi hợp đồng auth đã mô tả nó như một hàng rào có thật;
đó chính là ca luật này sinh ra để bắt.

---

## 4. Cấu hình fail-fast — thiếu cấu hình thì không khởi động

### 4.1 Luật

Mọi cấu hình đọc từ `appsettings`/biến môi trường đi qua `IOptions<T>` với
`ValidateDataAnnotations()` + `ValidateOnStart()`. **Không** project nào ngoài composition
root của Core được inject `IConfiguration` trực tiếp.

```csharp
// Core.Application/Configuration/CoreAuthOptions.cs — POCO thuần, không package hạ tầng
public sealed class CoreAuthOptions
{
    public const string SectionName = "Core:Auth";

    [Required(AllowEmptyStrings = false)]
    public string CookieName { get; init; } = default!;

    [Range(1, 60 * 24)]
    public int SessionMinutes { get; init; }

    [Required, MinLength(1)]
    public string[] AllowedOrigins { get; init; } = [];
}
```

```csharp
// Core.Web/DependencyInjection/CoreServiceCollectionExtensions.cs
services.AddOptions<CoreAuthOptions>()
    .Bind(configuration.GetSection(CoreAuthOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 4.2 Vì sao `?? []` là một cái bẫy, không phải một mặc định lịch sự

Ở dự án tiền nhiệm, allowlist CORS từng đọc thẳng `IConfiguration` và kết thúc bằng
`?? []`. Một cấu hình **thiếu** biến thành một allowlist **rỗng**, mà allowlist rỗng chặn
**mọi** origin. Kết quả: app khởi động thành công, `/health/ready` xanh, deploy báo thành công,
và FE không gọi được một API nào.

> **Một app khởi động rồi chặn sạch mọi request còn tệ hơn một app dừng lại và nói thẳng
> nó thiếu gì.**

Vì vậy `[Required]` + `[MinLength(1)]` trên `AllowedOrigins` không phải trang trí — nó là
thứ biến một sự cố ở Production thành một dòng lỗi lúc `dotnet run`.

### 4.3 Ranh giới: chỉ fail-fast ở Production

`ValidateOnStart()` gắn có điều kiện. Ở Development, một máy chưa cấu hình xong vẫn phải
boot được — nếu không thì người mới clone repo về không chạy nổi lần đầu.

```csharp
var optionsBuilder = services.AddOptions<CoreAuthOptions>()
    .Bind(configuration.GetSection(CoreAuthOptions.SectionName))
    .ValidateDataAnnotations();

if (environment.IsProduction())
    optionsBuilder.ValidateOnStart();
```

Đánh đổi được chấp nhận: ở Development, cấu hình thiếu lộ ra ở lời gọi API đầu tiên chứ
không lúc khởi động. Chấp nhận được vì Development có người ngồi trước màn hình.

Luật `EveryRequiredOptions_HasA_ValidateOnStart_CodePath` canh: mỗi `Options` bắt buộc
phải có **một đường** dẫn tới `ValidateOnStart`, kể cả khi đường đó nằm sau một `if`.

---

## 5. Dependency Injection

### 5.1 Đăng ký theo nhóm, không liệt kê tay ở host

`AddCore()` không phải một hàm dài. Nó gọi các nhóm con, mỗi nhóm sống cùng project mà nó
đăng ký:

```csharp
public static IServiceCollection AddCore(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    services.AddCoreOptions(configuration, environment);   // Core.Web
    services.AddCoreApplication();                          // Core.Application: MediatR, validator, behavior
    services.AddCorePersistence(configuration);             // Core.Infrastructure: DbContext, repository, interceptor
    services.AddCoreIdentity(configuration);                // Core.Infrastructure: Identity, cookie, permission
    services.AddCoreWeb(environment);                        // Core.Web: controller, CORS, rate limit, antiforgery
    return services;
}
```

Nguyên tắc: **project nào sở hữu kiểu thì project đó sở hữu lời đăng ký.** Đặt lời đăng ký
repository của Infrastructure vào `Core.Web` sẽ khiến `Core.Web` phải `using` mọi
namespace nội bộ của Infrastructure — và ranh giới nào cũng vỡ theo đúng cách đó.

Ở giai đoạn đầu, đăng ký **tay** trong từng `AddCoreXxx()`. Chỉ chuyển sang quét theo
convention (marker interface, assembly scan) khi việc liệt kê tay đã thật sự trở thành
gánh nặng. Quét sớm đổi một danh sách đọc được lấy một phép màu khó debug.

### 5.2 Vòng đời DI — định nghĩa gốc

Bảng dưới đây là **nguồn duy nhất** của vòng đời từng seam Core. File khác trỏ về đây
([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

| Vòng đời | Dùng cho | Seam / kiểu của Core |
| --- | --- | --- |
| `Singleton` | Không giữ state theo request, thread-safe, khởi tạo đắt | `ResultToHttpMapper` (static), bảng ánh xạ, `IOptions<T>`, `ICacheStore`, `IExecutionContextScope` (giá trị nằm trong `AsyncLocal`, không ở instance), `IPermissionCatalogSource` |
| `Scoped` | Bám theo một request / một đơn vị công việc | `CoreDbContext`, mọi repository, `IUnitOfWork`, `ICurrentUser`, **`ITenantContext`**, `IPermissionChecker`, `IIdentityService`, `IUserLookupService`, `IUserAdminService` |
| `Transient` | Rẻ, không state, mỗi lần dùng một bản mới | Validator, pipeline behavior, `IBackgroundJobScheduler` |

> **`ITenantContext` bắt buộc là `Scoped`, và đây không phải lựa chọn phong cách.** Nó mang
> đơn vị của **request hiện tại**. Đăng ký `Singleton` thì đơn vị của request đầu tiên được
> dùng lại cho mọi request sau — tức **đơn vị A đọc dữ liệu đơn vị B**, không lỗi, không ngoại
> lệ, chỉ là truy vấn trả về nhiều hơn đáng ra được thấy. `CoreDbContext` cũng `Scoped` nên hai
> vòng đời khớp nhau; nhét `ITenantContext` vào một singleton là ca bị luật cứng dưới đây cấm.

**Luật cứng: không bao giờ inject một `Scoped` vào một `Singleton`.** `CoreDbContext` là
`Scoped`; nhét nó vào một singleton nghĩa là một `DbContext` sống suốt vòng đời app, dùng
chung giữa các request, và `ChangeTracker` của nó phình vô hạn. .NET bắt được ca này lúc
khởi động **chỉ khi** `ValidateScopes` bật (mặc định bật ở Development, **tắt** ở
Production) — nên đừng trông vào nó ở Production.

Job nền và hosted service không có scope của request: chúng phải tự tạo scope bằng
`IServiceScopeFactory` cho mỗi lượt chạy.

---

## 6. Vertical slice trong `Application`

### 6.1 Quy ước

Thư mục trong `*.Application` chia theo **nghiệp vụ**, không theo loại kỹ thuật:

```
Core.Application/
├── Users/
│   ├── CreateUserCommand.cs
│   ├── CreateUserCommandHandler.cs
│   ├── CreateUserCommandValidator.cs
│   ├── UpdateUserCommand.cs
│   ├── GetUserByIdQuery.cs
│   ├── GetUsersListQuery.cs
│   ├── UserDto.cs
│   ├── IUserRepository.cs
│   └── UserErrors.cs
├── Permissions/
├── Menu/
└── Common/
    ├── Behaviors/
    ├── Interfaces/
    └── Paging/
```

Một use case = một Command/Query + một Handler + (nếu cần) một Validator, đặt cạnh nhau.

### 6.2 So sánh hai cách

| | Theo loại kỹ thuật (`Commands/`, `Queries/`, `Validators/`) | Theo nghiệp vụ (chọn cách này) |
| --- | --- | --- |
| Sửa một feature | Nhảy qua 4–5 thư mục để ráp lại bức tranh | Mọi file liên quan nằm cạnh nhau |
| Xoá một feature | Phải đi tìm mảnh vụn ở từng thư mục, luôn sót | Xoá một thư mục |
| Tách thành module riêng | Phải gỡ từng file khỏi từng thư mục dùng chung | Di chuyển một thư mục |
| Xung đột merge | Cao — hai người thêm hai feature cùng chạm `Commands/` | Thấp — hai thư mục khác nhau |
| Đọc lần đầu | Thấy ngay "hệ thống có những loại kỹ thuật nào" | Thấy ngay "hệ thống làm được những việc gì" |

Cột phải thắng ở mọi hàng trừ hàng cuối, và hàng cuối là câu hỏi người ta chỉ hỏi một
lần trong đời một dự án.

**Đánh đổi thật:** thư mục `Common/` vẫn tồn tại và vẫn chia theo kỹ thuật
(`Behaviors/`, `Interfaces/`, `Paging/`). Đó là chấp nhận được vì nó chứa **hạ tầng của
tầng Application**, không phải use case. Ranh giới để không biến `Common/` thành ngăn kéo
rác: một file chỉ vào `Common/` khi **từ hai slice trở lên** dùng nó.

---

## 7. Thêm một tính năng mới — checklist

Áp cho cả Core lẫn module. Bước nào không áp dụng thì bỏ qua, nhưng đừng đảo thứ tự — thứ
tự này chọn để lỗi lộ ra sớm nhất có thể.

1. **Quyết định phía nào sở hữu.** Từ hai module trở lên cần → Core. Một module cần → để
   ở module đó. Phép thử đầy đủ: [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.
2. **Entity** → `<Phía>.Domain/Entities/`. Kế thừa `BaseEntity`, field nghiệp vụ
   `private set`, dựng bằng factory trả `Result<T>` — [`be-entity-domain.md`](be-entity-domain.md).
3. **Catalog lỗi** → `<Phía>.Application/<Feature>/<Feature>Errors.cs`. Khai mã **trước**
   khi viết handler; mã không được dựng từ chuỗi literal ngoài catalog —
   [`be-cqrs-handler.md`](be-cqrs-handler.md).
4. **Command/Query + Handler + Validator + DTO** → cùng một thư mục
   `<Phía>.Application/<Feature>/`. Handler trả `Result<T>`, **không** tự mở transaction,
   **không** tự gọi `SaveChangesAsync`.
5. **Interface repository** → khai ở `<Phía>.Application/<Feature>/`, implement ở
   `<Phía>.Infrastructure/Persistence/Repositories/`.
6. **EF configuration** → `<Phía>.Infrastructure/Persistence/Configurations/`. Entity của
   Core vào schema `core`; entity của module vào schema của module. **Không FK vật lý
   xuyên schema** — [`../database/migration-policy.md`](../database/migration-policy.md).
7. **Migration** → project sở hữu schema đó tự sinh và tự giữ. Core sở hữu migration của
   schema `core` — [`../adr/0008-core-so-huu-migration.md`](../adr/0008-core-so-huu-migration.md).
   Áp schema chạy tay theo [`../database/script-runbook.md`](../database/script-runbook.md).
8. **Controller** → `Core.Web/Controllers/` (Core) hoặc `Modules.<X>.Endpoints/` (module).
   Kế thừa `ApiControllerBase`, khai `[RequirePermission]`, route tường minh —
   [`be-api-controller.md`](be-api-controller.md).
9. **Hợp đồng API** → thêm card vào [`../contracts/`](../contracts/) **trước khi** FE bắt
   đầu. Contract là thứ hai phía cùng nhìn, không phải tài liệu viết sau.
10. **Test** — theo thứ tự:
    - unit test cho factory/mutation method của entity (không cần DB),
    - unit test cho handler với repository giả lập,
    - integration test cho endpoint qua `WebApplicationFactory` + PostgreSQL thật.
11. **Chạy cổng.** `dotnet test` (gồm ArchTests) phải xanh trước khi báo xong.

---

## 8. SOLID & OOP — áp vào đúng ngữ cảnh Core này

Không lý thuyết suông. Mỗi nguyên tắc kèm một chỗ cụ thể trong Core mà nó quyết định.

**S — Single Responsibility.** Một Command/Query + một Handler = đúng một use case. Phép
thử: nếu **sửa một luật nghiệp vụ** buộc phải sửa class X, **và** đổi công nghệ lưu trữ
cũng buộc phải sửa chính class X, thì X đang mang hai lý do để thay đổi — tách. Đây là lý
do repository nằm ở Infrastructure còn luật nằm ở Domain/Handler.

**O — Open/Closed.** Thêm một mã lỗi mới, một Command mới, một module mới **không được**
đòi sửa `Core.Web`, `ApiControllerBase`, hay `ResultToHttpMapper`. Cụ thể: thêm một
`ErrorDescriptor` chỉ là thêm một dòng vào catalog của feature đó — vì `ErrorType` đã phủ
hết các nhánh HTTP và ánh xạ đã khai đủ. Nếu thấy mình đang sửa `Core.*` để phục vụ riêng
một module, dừng lại: đó là vi phạm OCP thật, không phải việc nhỏ.

**L — Liskov Substitution.** Mọi implementation của một interface phải dùng được ở **mọi**
nơi interface đó được yêu cầu. Không `NotImplementedException` cho method nào. Ứng dụng
cụ thể: `AppUser` **cố ý không** kế thừa `BaseEntity` — nó là `IdentityUser<Guid>` và có
vòng đời khác. Ép nó kế thừa rồi bỏ qua một phần hành vi là vi phạm LSP; cho nó mang
trường vết tương đương là cách đúng.

**I — Interface Segregation.** Đây là lý do Core khai **ba** interface Identity riêng thay
vì một `IIdentityService` khổng lồ: `IIdentityService` (đăng nhập, đổi mật khẩu),
`IUserLookupService` (tra cứu, chỉ đọc), `IUserAdminService` (khoá, gán vai trò). Một
handler chỉ tra cứu người dùng không phải phụ thuộc vào bề mặt cho phép khoá tài khoản.
Ngưỡng phán đoán: interface vượt quá 6–8 method là dấu hiệu nên tách — không phải luật
cứng, và **không** tách vụn thành interface một method chỉ để cho đẹp.

**D — Dependency Inversion.** Toàn bộ §1 chính là DIP viết thành đồ thị tham chiếu:
Application khai interface, Infrastructure implement, và compiler không cho đi ngược. Không
có gì thêm phải làm ngoài giữ kỷ luật đó.

**Encapsulation.** Field nghiệp vụ `private set`; mutate qua method mang tên nghiệp vụ
(`user.Deactivate()`, không `user.IsActive = false`). Ngoại lệ **có chủ đích và có giới
hạn**: các field audit của `BaseEntity` có public setter để interceptor ghi từ ngoài — lý
do và ranh giới của ngoại lệ đó ở [`be-entity-domain.md`](be-entity-domain.md).

**Abstraction.** Trong `Domain` và `Application` **không bao giờ** `new` một class của
Infrastructure. Nếu thấy mình cần, nghĩa là thiếu một interface.

**Inheritance.** Chỉ dùng cho `BaseEntity` và `ApiControllerBase`. Không xây hierarchy
nghiệp vụ nhiều tầng. Ưu tiên composition (Value Object, service riêng).

**Polymorphism.** Chỉ khai interface khi thật sự có từ hai implementation trở lên, hiện
tại hoặc cận kề. Một `switch` ba nhánh đọc được rõ hơn ba class rải ba file.

---

## 9. Kết cấu thư mục test và ArchTest nào canh luật nào

### 9.1 Layout

```
src/BE/Tests/
├── CoreAndSkill.ArchTests/            ← luật kiến trúc; chạy trong mọi `dotnet test`
├── CoreAndSkill.Core.UnitTests/       ← Domain + Application; không DB, không HTTP
└── CoreAndSkill.Core.IntegrationTests/ ← endpoint thật + PostgreSQL thật (Testcontainers)
```

| Tầng test | Test gì | Không test gì |
| --- | --- | --- |
| Unit — Domain | Factory method trả `Result` đúng lỗi, mutation method giữ invariant, Value Object từ chối giá trị sai | Bất cứ thứ gì cần DB |
| Unit — Application | Handler với repository giả lập: nhánh thành công, từng nhánh lỗi, đúng `ErrorType` | EF Core thật, ánh xạ HTTP |
| Integration | Endpoint qua `WebApplicationFactory`, migration áp được, query filter soft delete có hiệu lực, envelope đúng hình dạng, rate limit chặn thật | — |
| Arch | Ranh giới tầng, quy ước đặt tên, mọi thứ "khai rồi có được nối vào không" | Hành vi |

**`UseInMemoryDatabase` bị CẤM.** Provider đó không có transaction, không có ràng buộc,
không có index lọc — nên nó không chứng minh được gì về hành vi thật, mà lại xanh. Luật T2
ở [`../RULES.md`](../RULES.md) yêu cầu PostgreSQL thật qua Testcontainers.

### 9.2 ArchTest nào canh luật nào

> 📖 **Danh mục tên ArchTest là cột "Ép bằng gì" của [`../RULES.md`](../RULES.md) — đó là nguồn duy nhất.**
> Giải thích từng test canh điều gì, và test nào là integration chứ không phải ArchTest:
> [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md) §2.2.

Dùng **đúng** tên ở đó khi nhắc tới chúng ở bất kỳ đâu — tên là thứ người sau `grep` để tìm,
nên một tên chép lệch là một tên không tìm thấy.

### 9.3 Mọi detector phải có test đối chứng

Luật T1 ở [`../RULES.md`](../RULES.md): mỗi ArchTest ở trên phải đi kèm một test
`Detector_*` chứng minh nó **bắt được** vi phạm — bằng cách cho nó một mẫu vi phạm cố ý và
kiểm rằng nó đỏ.

Không có test đối chứng thì một detector hỏng sẽ **xanh vĩnh viễn**, và một cổng xanh giả
tệ hơn không có cổng: nó tạo cảm giác được bảo vệ. Ở dự án tiền nhiệm, một script cổng
**không tồn tại trên đĩa** khiến ba mục cổng không chạy suốt thời gian dài trong khi tài
liệu vẫn ghi bình thường — xem [`../audit/2026-08-23-cong-khong-ton-tai.md`](../audit/2026-08-23-cong-khong-ton-tai.md).

### 9.4 Bẫy khi thi công detector

Khi viết detector cho `CoreSource_MustNotContain_BusinessNameStringLiteral`, **ưu tiên
phân tích AST, không quét văn bản nguồn**. Ở dự án tiền nhiệm, detector cho luật tương
đương quét văn bản, nên một chuỗi nội suy hợp lệ vẫn bị bắt và người viết phải tách biến
chỉ để làm chuỗi "sạch" — đuôi vẫy chó.

Nếu buộc phải quét văn bản, detector phải có test chứng minh nó bỏ qua comment và định
danh.

---

## 10. Đếm bằng lệnh, đừng chép số

Khi `src/` tồn tại ở giai đoạn 2, các câu hỏi *"có bao nhiêu project"*, *"có bao nhiêu
behavior"*, *"còn bao nhiêu chỗ chưa có `ValidateOnStart`"* trả lời bằng lệnh, không bằng
bảng chép tay trong tài liệu ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6):

```bash
find src/BE -iname '*.csproj' -not -path '*/obj/*' -not -path '*/bin/*' | sort
grep -rn 'IPipelineBehavior<' src/BE --include='*.cs' | grep -v /obj/
grep -rn 'ValidateOnStart' src/BE --include='*.cs' | grep -v /obj/
```

Hai con số "project trên đĩa" và "project trong solution" **có thể khác nhau** — một
project trên đĩa mà không nằm trong solution thì không được build, không được test, và
không ai biết. `EveryProjectOnDisk_IsDeclared_InSolution` bắt đúng trường hợp đó.
