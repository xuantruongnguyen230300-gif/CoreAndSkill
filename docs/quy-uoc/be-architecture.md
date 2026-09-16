---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quy ước Backend — kiến trúc, layout, composition root

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Toàn bộ file mô tả thứ `src/BE` **phải trở thành** ở
> giai đoạn 2; không đoạn nào được trích dẫn như bằng chứng.
>
> 📖 Lý do và phương án đã loại: [`../kien-truc-core-module.md`](../kien-truc-core-module.md),
> [`../adr/0002-core-5-project.md`](../adr/0002-core-5-project.md). Lý do, bẫy, ví dụ mở rộng của
> **từng mục** — cùng số mục: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md).

---

## 1. Project layout & chiều phụ thuộc

### 1.1 Project nào chứa gì — và các seam của `Core.Application`

> 📖 **Danh sách project Core, thứ mỗi project chứa và CẤM chứa, project nào tham chiếu được project
> nào: đọc [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2.** Bảng đó cố ý không chép
> lại ([`../OWNERSHIP.md`](../OWNERSHIP.md)).

Tên đầy đủ của assembly là `CoreAndSkill.Core.<Tầng>`; host là `CoreAndSkill.Api`; module là
`CoreAndSkill.Modules.<X>.*`.

**Thứ mục này sở hữu là danh sách seam** — interface `Core.Application` khai để tầng ngoài implement:

| Seam | Ai implement | Vai — và chữ ký ở đâu |
| --- | --- | --- |
| `IUnitOfWork` | `Core.Infrastructure` | Một lần ghi chạm nhiều `DbContext` (Core + module) trong **một** transaction; handler không biết `DbContext` — [`be-cqrs-handler.md`](be-cqrs-handler.md) §4 |
| `ICurrentUser` | `Core.Web` (`HttpContextCurrentUser`) | Danh tính từ `HttpContext` — thứ `Core.Application` và `Core.Infrastructure` đều **cấm** chạm. Chữ ký: mục *Danh tính và đơn vị của request* bên dưới |
| `ITenantContext` | `Core.Web` (`HttpContextTenantContext`) | Đơn vị hiện hành của request; mọi `DbContext` nhận nó qua constructor để dựng bộ lọc tenant — [`be-entity-domain.md`](be-entity-domain.md) §5.1, [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) |
| `IPermissionChecker` | `Core.Infrastructure` | Tra ma trận quyền trong DB; test không cần DB |
| `IIdentityService` · `IUserLookupService` · `IUserAdminService` | `Core.Infrastructure` | Giữ `AppUser`/`AppRole` không rời Infrastructure — [`be-entity-domain.md`](be-entity-domain.md) §7 |
| `ISessionPrincipalFactory` — dựng `ClaimsPrincipal` của một phiên từ `LoginOutcome`; tên loại claim khai một lần ở `CoreClaimTypes`; chữ ký: [`be-api-controller.md`](be-api-controller.md) §7.4 | `Core.Infrastructure` | Chỗ **duy nhất** Identity lõi (`Core.Infrastructure`) và cookie scheme (`Core.Web`) gặp nhau — `Core.Web` phát phiên mà không chạm `UserManager` — [`../adr/0026-ranh-gioi-identity-va-cookie.md`](../adr/0026-ranh-gioi-identity-va-cookie.md) |
| `ITenantLookup` | `Core.Infrastructure` | Tra đơn vị theo mã ở bước đăng nhập, **trước** khi mở phạm vi đơn vị — chữ ký: [`be-entity-domain.md`](be-entity-domain.md) §7.1 |
| `ILoginAttemptLimiter` | `Core.Infrastructure` | Hạn mức đăng nhập theo `LoginPartitionKey` kiểm **trong** handler; vượt hạn mức đi ra bằng exception mà `IExceptionHandler` đổi thành 429 — [`be-api-controller.md`](be-api-controller.md) §6.5 |
| `ITenantProvisioningService` | `Core.Infrastructure` | Lệnh bootstrap và endpoint tạo đơn vị gọi **cùng một** hiện thực — [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md). Các thao tác của khu hệ thống tác động lên một đơn vị nghiệp vụ cũng thuộc service này: khôi phục mật khẩu quản trị đơn vị, ngưng và bật lại đơn vị — [`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md); tạo tài khoản quản trị mới cho một đơn vị ([`../contracts/tenants.md`](../contracts/tenants.md) §6); seed lại dữ liệu mặc định cho mọi đơn vị (lệnh `core seed-tenant-defaults`, §3) |
| `ICacheStore` | `Core.Infrastructure` | Đổi bản cài trong bộ nhớ sang Redis là đổi **một dòng đăng ký** — [`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md) |
| `IBackgroundJobScheduler` | `Core.Infrastructure` | Không lộ cơ chế chạy nền ra Application — [`be-cqrs-handler.md`](be-cqrs-handler.md) §10. v1 hiện thực bằng `BackgroundService` của .NET, không thư viện ([`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §7) |
| `IExecutionContextScope` | `Core.Infrastructure` | Danh tính và đơn vị cho mã **không có** `HttpContext` — mục *Danh tính và đơn vị khi không có request* bên dưới |
| `ISettingStore` | `Core.Infrastructure` | Đọc/ghi cấu hình theo đơn vị và cache của nó — [`../wiki-core/be/19-cau-hinh-theo-don-vi.md`](../wiki-core/be/19-cau-hinh-theo-don-vi.md) §4 |
| `ICodeSequence` | `Core.Infrastructure` | Cấp mã nghiệp vụ kế tiếp trong cùng giao dịch — [`../wiki-core/be/20-sinh-ma-nghiep-vu.md`](../wiki-core/be/20-sinh-ma-nghiep-vu.md) §2 |
| `IPermissionCatalogSource` | Mỗi module, và Core cho khoá của chính nó | Core giữ cơ chế phân quyền, module cấp dữ liệu — mục *Danh mục khoá quyền do module cấp* bên dưới |
| `ITenantSeedSource` | Mỗi module có dữ liệu mặc định cho đơn vị mới, và Core cho menu của chính nó | Core giữ cơ chế dựng đơn vị, module cấp vai trò, ánh xạ quyền và menu — mục *Nguồn seed cho đơn vị mới* bên dưới |

#### Danh tính và đơn vị của request — định nghĩa gốc

```csharp
// Core.Application
public interface ICurrentUser
{
    Guid? UserId { get; }       // null ⇒ chưa xác thực
    string? UserName { get; }
}

public interface ITenantContext
{
    Guid? TenantId { get; }     // null ⇒ chưa có đơn vị
}
```

| Luật |
| --- |
| `TenantId` **chỉ** có trên `ITenantContext`, không có trên `ICurrentUser` |
| Hai seam, không gộp: `ICurrentUser` chỉ có giá trị **sau khi** đã xác thực, `ITenantContext` phải có giá trị **trước** bước xác thực |
| Kiểu nullable, không dùng `Guid.Empty` làm "chưa có"; bộ lọc tenant so với `null` thì trả **rỗng** — đóng, không mở ([`be-entity-domain.md`](be-entity-domain.md) §5.1) |

#### Danh tính và đơn vị khi không có request — định nghĩa gốc

Có những đoạn mã phải biết đơn vị và người kích hoạt mà **không có** `HttpContext` để đọc, hoặc có mà
chưa mang đơn vị — allowlist ở bảng dưới. Seam `IExecutionContextScope` mở một **phạm vi ngữ cảnh thực
thi**; hiện thực ở `Core.Infrastructure`, giữ giá trị trong `AsyncLocal`.

```csharp
// Core.Application
public interface IExecutionContextScope
{
    // Bên trong khối using, ICurrentUser / ITenantContext trả giá trị này; Dispose khôi phục phạm vi trước đó.
    IDisposable Enter(Guid tenantId, Guid? userId, string? userName);

    // Phạm vi đang mở, null khi không có — chỗ DUY NHẤT hai lớp ở Core.Web đọc phạm vi (§2.1).
    ExecutionContextSnapshot? Current { get; }
}

public sealed record ExecutionContextSnapshot(Guid? UserId, string? UserName, Guid? TenantId);
```

`ICurrentUser` và `ITenantContext` đọc theo thứ tự: **`Current` của phạm vi đang mở → `HttpContext` → không có gì**.

| Luật |
| --- |
| Chỉ **bộ lọc job nền**, **bộ phát outbox**, **service tạo đơn vị cùng runner lệnh bootstrap** ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)), **bước đăng nhập** (nạp đơn vị trước khi gọi `UserManager`) và **phép kiểm security stamp của cookie** (mở phạm vi bằng `TenantId` lấy từ claim của chính principal đang kiểm — [`../adr/0026-ranh-gioi-identity-va-cookie.md`](../adr/0026-ranh-gioi-identity-va-cookie.md)) được mở phạm vi — luật A12 ở [`../RULES.md`](../RULES.md) §3. Bước đăng nhập là ngoại lệ đã khai của M2 |
| Service tạo đơn vị mở phạm vi của đơn vị **vừa tạo** để dữ liệu seed ghi đúng chỗ, và của đơn vị **đích** khi khôi phục mật khẩu quản trị đơn vị, tạo quản trị mới cho đơn vị ([`../contracts/tenants.md`](../contracts/tenants.md) §6), ngưng, bật lại đơn vị ([`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)), hay seed lại từng đơn vị (`core seed-tenant-defaults`, §3) — các thao tác đó nằm trong service, không phải mục riêng của allowlist |
| `Dispose` khôi phục phạm vi đang mở **trước đó**, không xoá về rỗng |
| Đơn vị và người kích hoạt đi theo việc nền bằng **dữ liệu**, không bằng trí nhớ của chỗ gọi: interceptor ghi outbox lưu `tenant_id`, `triggered_by_user_id`, `triggered_by_user_name` lên chính dòng outbox ([`../database/schema-core.md`](../database/schema-core.md) §8); bộ phát gọi `Enter(tenant, user, name)` **theo từng dòng**; job enqueue từ bên trong phạm vi đó được hiện thực `IBackgroundJobScheduler` chụp lại và khôi phục lúc chạy |
| **Không** chụp vai trò hay quyền. Job cần phân quyền thì đọc quyền **hiện tại** từ DB |
| Bộ phát outbox mở phạm vi bằng giá trị của **chính dòng đang phát**, mỗi dòng một phạm vi |
| Không có **người** là hợp lệ — nhật ký ghi `system`. Không có **đơn vị** thì không hợp lệ: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §2 |

#### Danh mục khoá quyền do module cấp — định nghĩa gốc

Core giữ **cơ chế** phân quyền; tập **khoá** là dữ liệu: mỗi module cấp khoá của mình qua seam
`IPermissionCatalogSource`, Core cấp khoá của chính nó qua cùng seam (danh mục ở
[`../database/schema-core.md`](../database/schema-core.md) §5.2). Mỗi trường của hai record dưới ứng với
đúng một cột của [`../database/schema-core.md`](../database/schema-core.md) §5.1 / §5.2; migration seed đọc
giá trị từ đây, không tự bịa cột nào ([`../database/migration-policy.md`](../database/migration-policy.md) §4.1).

```csharp
// Core.Application
public sealed record PermissionResourceDefinition(
    string Key,             // core.permission_resource.key
    string NameKey,         // khoá dịch — cột name_key
    string? ModuleKey,      // cột module_key; null ⇒ tài nguyên của Core
    int DisplayOrder);      // cột display_order

public sealed record PermissionDefinition(
    string Code,            // core.permission.code — "<ResourceKey>.<Action>", chữ thường
    string ResourceKey,     // Key của một PermissionResourceDefinition trong CÙNG nguồn
    string Action,          // cột action
    string NameKey,         // khoá dịch — cột name_key
    int DisplayOrder);      // cột display_order

public interface IPermissionCatalogSource
{
    IReadOnlyCollection<PermissionResourceDefinition> GetResources();
    IReadOnlyCollection<PermissionDefinition> GetPermissions();
}
```

| Luật |
| --- |
| Mỗi module đăng ký **đúng một** nguồn trong `AddXModule()`; Core gộp mọi nguồn (luật A3) |
| Danh mục đã gộp được kiểm **lúc khởi động** bằng hosted service: `Key` hoặc `Code` trùng giữa các nguồn, `Code` sai khuôn `<tài nguyên>.<hành động>` hoặc không bằng `ResourceKey + "." + Action`, `ResourceKey` không có `PermissionResourceDefinition` nào trong **cùng** nguồn, `NameKey` rỗng ⇒ **tiến trình không khởi động** |
| Tài nguyên và khoá của một module nằm trong **cùng một** nguồn; `ResourceKey` không trỏ sang tài nguyên của nguồn khác |
| **Dòng** vào `core.permission` / `core.permission_resource` bằng **migration idempotent** — `ON CONFLICT … DO NOTHING`, định danh cố định, không xoá tự động ([`../database/migration-policy.md`](../database/migration-policy.md) §4.1); test CI đối chiếu **hai chiều** `Code` và `ResourceKey` của mọi `PermissionDefinition` với `code` và `resource_key` seed trong migration (luật B7). Khoá của Core do migration của Core ghi; khoá của module do migration của **chính module** ghi — ngoại lệ có tên của luật E6 ([`../database/migration-policy.md`](../database/migration-policy.md) §1); test B7 phủ cả khoá của module |
| Tiến trình ứng dụng **không ghi** danh mục — lúc khởi động hay lúc chạy. Tài khoản DB của ứng dụng chỉ `SELECT` trên hai bảng đó (luật M13). Hướng ngược lại là hướng đã khoá `K52` |
| `NameKey` là **khoá dịch**, không phải câu hiển thị — [`be-cqrs-handler.md`](be-cqrs-handler.md) §7.2 |
| **Không** có hiện thực mặc định — thiếu đăng ký thì DI hỏng ngay lúc khởi động |
| Ánh xạ vai trò → quyền **không** đi qua seam này. Giá trị mặc định cho đơn vị mới đi qua `ITenantSeedSource` (mục dưới); Core không có hằng số vai trò nào (luật S1) |

#### Nguồn seed cho đơn vị mới — định nghĩa gốc

Service tạo đơn vị ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md))
dựng đơn vị mới từ dữ liệu **mặc định** mà module và Core cấp qua seam `ITenantSeedSource`:

```csharp
// Core.Application
public sealed record SeedRole(string Name, bool IsSystem);
public sealed record SeedRolePermission(string RoleName, string PermissionCode);
public sealed record SeedMenuItem(
    string Code,
    string LabelKey,
    string? Icon,
    string? Route,
    string? ParentCode,                // null ⇒ mục cấp một
    int DisplayOrder,
    string? RequiredPermissionCode,    // khoá trong danh mục đã gộp; null ⇒ không gắn quyền
    string? ModuleKey);

public interface ITenantSeedSource
{
    IReadOnlyCollection<SeedRole> GetRoles();
    IReadOnlyCollection<SeedRolePermission> GetRolePermissions();
    IReadOnlyCollection<SeedMenuItem> GetMenuItems();
}
```

Các mục tham chiếu nhau bằng **mã** (`RoleName`, `ParentCode`, `RequiredPermissionCode`), không bằng id.
Cột đích: [`../database/schema-core.md`](../database/schema-core.md) §4.2 (vai trò), §5.3 (ánh xạ), §6.1 (menu).

| Luật |
| --- |
| Một nguồn trả ba thứ: **vai trò mặc định** (kèm cờ `is_system`), **ánh xạ vai trò → khoá quyền**, **menu** — [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 |
| Nhiều đăng ký được **gộp**; Core tự đăng ký một nguồn cho menu của Core (luật A3) |
| Nguồn của Core **không** khai vai trò nào — vai trò mặc định là của dự án (luật S1) |
| Nguồn đã gộp được kiểm **lúc khởi động**: một khoá quyền trong ánh xạ không có trong danh mục đã gộp ⇒ **tiến trình không khởi động** |
| Không nguồn nào khai vai trò ⇒ đơn vị mới chạy bằng cờ `has_permission_bypass` của tài khoản quản trị đầu tiên — trạng thái hợp lệ, không phải lỗi ([`../contracts/tenants.md`](../contracts/tenants.md) §2) |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §1.1

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
                   Modules.<X>.Infrastructure ────> Core.Infrastructure
                              ▲                     (bộ lọc truy vấn dùng chung)
                   Modules.<X>.Application
                              ▲
                     Modules.<X>.Domain


   Contracts ── không tham chiếu ai, và được cả Core lẫn mọi module tham chiếu
```

**Cạnh `Modules.<X>.Infrastructure → Core.Infrastructure` là cạnh BẮT BUỘC**: `DbContext` của mỗi module
phải áp **cùng** bộ lọc truy vấn (đơn vị, xoá mềm) sống ở `Core.Infrastructure`
([`be-entity-domain.md`](be-entity-domain.md) §5). Cạnh đi **một chiều** — `Core.*` vẫn không được tham chiếu
`Modules.*` (§1.3); Core cần dữ liệu của module thì đi qua interface hẹp ở §1.5.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §1.2

### 1.3 Danh sách cấm — mỗi dòng là một ArchTest

Mỗi dòng có **một** ArchTest canh; tên khai ở cột "Ép bằng gì" của [`../RULES.md`](../RULES.md) §3, không
chép lại ở đây.

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

**Không có "code cũ" nào biện minh cho việc phá luật** — repo này chưa có code cũ.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §1.4

### 1.5 Khi Infrastructure của Core cần dữ liệu của một module

Khai **interface hẹp** ở `Core.Application`, module implement ở `Modules.<X>.Infrastructure`, host đăng ký;
`Core.Infrastructure` chỉ biết interface — inject thẳng repository của module là vi phạm `Core.* → Modules.*`.

```csharp
// Core.Application/Maintenance/IStaleDataCleaner.cs
public interface IStaleDataCleaner
{
    string Name { get; }
    Task<int> CleanAsync(DateTimeOffset olderThan, CancellationToken ct);
}
```

Job nền của Core inject `IEnumerable<IStaleDataCleaner>` và chạy hết.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §1.5

---

## 2. `Core.Web` chứa gì — và vì sao nó KHÔNG được nằm ở host

### 2.1 Danh sách thành phần

| Thành phần | Vai |
| --- | --- |
| `ApiControllerBase` | Lớp cơ sở mang `[Authorize]` (fail-closed) và helper `HandleResult` |
| `ApiEnvelope<T>` + `Envelope` | Hình dạng dây của mọi response dưới `/api` — [`be-api-controller.md`](be-api-controller.md) §2.1 |
| `ResultToHttpMapper` | Ánh xạ `ErrorType` → HTTP status — **đúng một chỗ** trong toàn hệ |
| `IExceptionHandler` của Core | Bắt exception ngoài dự kiến, dịch thành envelope, không lộ stack trace. Đăng ký bằng `AddExceptionHandler<T>()` trong `AddCore`, nối bằng `UseExceptionHandler()` ở §3.1 — không có middleware bắt exception tự viết |
| `EnvelopeMiddleware` | Bọc envelope cho các response do **hạ tầng định tuyến** sinh (404 không khớp route, 405 sai verb) — chúng có thân rỗng nên không đi qua handler nào |
| `TraceIdMiddleware` | Lấy `traceId` theo W3C Trace Context: header `traceparent` → `Activity.Current.TraceId`; gán vào `HttpContext.TraceIdentifier` và vào log scope. Mọi envelope đọc `traceId` từ `HttpContext.TraceIdentifier` |
| Ba attribute phân quyền endpoint và filter của chúng | Luật S11 — [`be-api-controller.md`](be-api-controller.md) §4.2 |
| Cookie scheme, cookie events, `HttpContext.SignInAsync` / `SignOutAsync` | Phát và thu hồi phiên; ghi envelope 401/403 thay cho redirect — [`be-api-controller.md`](be-api-controller.md) §7.4, [`../adr/0026-ranh-gioi-identity-va-cookie.md`](../adr/0026-ranh-gioi-identity-va-cookie.md) |
| Runner lệnh Core (`RunCoreCommandAsync`) | Nhận dòng lệnh của host, chạy lệnh bootstrap qua service tạo đơn vị rồi báo host thoát — §3, [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) |
| Cấu hình rate limit | Theo IP. Hạn mức theo tài khoản đăng nhập **không** nằm ở đây mà trong handler đăng nhập — [`be-api-controller.md`](be-api-controller.md) §6 |
| `HttpContextCurrentUser` · `HttpContextTenantContext` | Implementation của `ICurrentUser` và của `ITenantContext` — hai chỗ **duy nhất** đọc `HttpContext` để lấy danh tính và đơn vị. Cả hai đọc `IExecutionContextScope.Current` **trước**, `HttpContext` **sau** (§1.1) |
| `AntiforgeryValidationMiddleware` | Chống CSRF hai lớp cho mọi method ghi — kiểm `Origin` theo allowlist, rồi kiểm token; tự dựng envelope 403, xem [`be-api-controller.md`](be-api-controller.md) §7.2 |
| `PasswordChangeRequiredMiddleware` | Chặn tài khoản đang ở trạng thái bắt buộc đổi mật khẩu, trừ allowlist ở [`../contracts/auth.md`](../contracts/auth.md) §1.2. Chặn ở BE, không để FE điều hướng |
| `ModelBindingProblemFactory` | Thay `InvalidModelStateResponseFactory` mặc định để lỗi model binding cũng ra đúng envelope |
| Controller của Core | Auth, người dùng, vai trò, permission, menu động — những màn hình mọi sản phẩm đều cần |
| `AddCore()` / `UseCore()` | Hai extension method là **toàn bộ** bề mặt lắp ghép của Core |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §2.1

### 2.2 Vì sao chúng không được ở host

Phép thử một dòng:

> Một dự án mới, chưa viết gì, chỉ tham chiếu `Core.Web` và gọi `AddCore()` + `UseCore()`
> — phải có ngay: đăng nhập, đổi mật khẩu, quản trị người dùng, phân quyền, menu động,
> envelope thống nhất, rate limit, CSRF. Không chép file nào.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §2.2

### 2.3 Ranh giới của `Core.Web`

`Core.Web` **được** biết `Core.Infrastructure`. Nhưng nó **không** được:

- Gọi thẳng `CoreDbContext`. Mọi truy cập dữ liệu đi qua MediatR.
- Chứa nghiệp vụ. Controller chỉ `Send` rồi `HandleResult` — **trừ** action phát hoặc cấp lại cookie phiên ([`be-api-controller.md`](be-api-controller.md) §7.4).
- Biết tên bất kỳ module nào. Luật `CoreSource_MustNotContain_BusinessNameStringLiteral`
  áp cho cả `Core.Web`.

---

## 3. Host mỏng — composition root duy nhất

Project `CoreAndSkill.Api` **chỉ được** chứa: `Program.cs`, `appsettings*.json`, và lời
gọi đăng ký module của dự án.

**CẤM** chứa: middleware, controller, entity, handler, migration, filter, extension method
có logic. Luật `Host_MustNotContain_MiddlewareOrController` bắt điều này.

**`Program.cs` dưới 50 dòng.** Vượt ngưỡng thì chuyển thứ thừa vào `Core.Web`, không nới ngưỡng.

```csharp
// CoreAndSkill.Api/Program.cs — đích đến
using CoreAndSkill.Core.Web;

var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddSkillModule(builder.Configuration);   // mỗi module một dòng — TRƯỚC AddCore (§5.1)
builder.Services.AddCore(builder.Configuration, builder.Environment);

var app = builder.Build();

if (await app.RunCoreCommandAsync(args)) return;

app.UseCore();
// app.UseSkillModule();

app.Run();
```

- **Không có `AddControllers()`, `AddCors()`, `UseAuthentication()`** ở host — chúng nằm trong
  `AddCore()`/`UseCore()`.
- **`AddCore` nhận cả `IHostEnvironment`** — cho Swagger ([`be-api-controller.md`](be-api-controller.md) §8.2)
  và ghi tham số SQL vào log ([`be-performance.md`](be-performance.md) §4.3); kiểm cấu hình lúc khởi động
  **không** thuộc số đó (§4.3).
- **Host không phân tích dòng lệnh.** `RunCoreCommandAsync` nhận lệnh khi tham số đầu tiên là
  `core`: chạy lệnh rồi trả `true` để host thoát — không dựng pipeline HTTP, không mở cổng. Tham số
  đầu không phải `core` ⇒ `false`.
- **Mỗi module đúng một dòng đăng ký.** `AddXxxModule()` tự gộp controller assembly và tự ghi nhận assembly
  handler/validator với Core (§5.1); host không gọi `AddApplicationPart`, không truyền assembly nào cho Core.
  Dòng của module đứng **trước** `AddCore`.

**Lệnh của runner — định nghĩa gốc.** Động từ sau `core` không có trong bảng ⇒ báo lỗi, thoát mã khác 0, không mở cổng.

| Lệnh | Tham số sau động từ | Đầu vào — đọc qua hệ cấu hình | Việc |
| --- | --- | --- | --- |
| `core bootstrap` | Không có | Mã và tên đơn vị hệ thống; mã và tên đơn vị nghiệp vụ đầu tiên; tên đăng nhập và mật khẩu của tài khoản vận hành và của tài khoản quản trị đơn vị | Gọi `ITenantProvisioningService`: đơn vị hệ thống kèm tài khoản vận hành, rồi đơn vị nghiệp vụ đầu tiên kèm tài khoản quản trị |
| `core reset-operator-password` | Không có | Tên đăng nhập của tài khoản vận hành; mật khẩu mới | Đặt lại mật khẩu của một tài khoản mang `is_system_operator` thuộc đơn vị hệ thống |
| `core seed-tenant-defaults` | Không có | Không có | Chạy lại seed `ITenantSeedSource` đã gộp cho **mọi** đơn vị nghiệp vụ, idempotent — vai trò, ánh xạ quyền, menu đã có thì bỏ qua. Dùng khi lắp module mới vào bản cài đã có đơn vị; mỗi đơn vị một transaction |
| `core outbox-replay` | `--id <id>` **hoặc** `--all-dead` | Không có | Đặt lại `status = 'pending'`, `attempt_count = 0` cho một dòng `core.outbox_message` đang `dead` (hoặc mọi dòng `dead`); ghi nhật ký kiểm toán. Ngưỡng và cột: [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2.5 |

Mọi **giá trị** đi qua hệ cấu hình, không qua tham số dòng lệnh; tham số sau động từ chỉ **chọn đối tượng** tác động. Tên khoá cấu hình và cách gọi:
[`../database/script-runbook.md`](../database/script-runbook.md) §3.3, §8, §9, §10; hành vi khi thiếu giá trị, chạy
lại, nhật ký kiểm toán: [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md) §3, §5.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §3

### 3.1 Thứ tự pipeline — định nghĩa gốc

Thứ tự pipeline khai **một lần** trong `UseCore()` — khối dưới đây là **nguồn duy nhất**, file khác trỏ
về đây ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2):

```csharp
// Core.Web/DependencyInjection/CoreApplicationBuilderExtensions.cs
public static WebApplication UseCore(this WebApplication app)
{
    app.UseForwardedHeaders();              // ĐẦU TIÊN — ràng buộc 4 bên dưới
    app.UseMiddleware<TraceIdMiddleware>();
    app.UseExceptionHandler();              // đăng ký IExceptionHandler ở AddCore
    app.UseMiddleware<EnvelopeMiddleware>();
    app.UseCors(CorsPolicyNames.Default);
    app.UseAuthentication();
    app.UseMiddleware<AntiforgeryValidationMiddleware>();    // Origin + token — SAU xác thực, TRƯỚC phân quyền
    app.UseAuthorization();
    app.UseMiddleware<PasswordChangeRequiredMiddleware>();   // SAU UseAuthorization
    app.UseRateLimiter();
    app.MapControllers();
    app.MapHealthChecks("/health/live",  new HealthCheckOptions { Predicate = _ => false })
       .AllowAnonymous()            // allowlist ẩn danh — be-api-controller.md §5
       .DisableRateLimiting();      // miễn rate limit — be-api-controller.md §6.3
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") })
       .AllowAnonymous()
       .DisableRateLimiting();
    return app;
}
```

Bốn chỗ không hoán đổi được:

1. **`PasswordChangeRequiredMiddleware` SAU `UseAuthorization`.** Trả **403** `CORE.AUTH.PASSWORD_CHANGE_REQUIRED`
   bằng envelope dựng **tay** ([`be-api-controller.md`](be-api-controller.md) §2.4); allowlist bốn đường đi
   qua được: [`../contracts/auth.md`](../contracts/auth.md) §1.2.
2. **`AntiforgeryValidationMiddleware` SAU `UseAuthentication`, TRƯỚC `UseAuthorization`**
   ([`be-api-controller.md`](be-api-controller.md) §7.2).
3. **`UseCors` TRƯỚC mọi middleware chặn.**
4. **`UseForwardedHeaders` ĐẦU TIÊN.** `AddCore` **xoá danh sách proxy tin cậy mặc định của framework** rồi
   nạp lại từ cấu hình; danh sách rỗng nghĩa là **không tin proxy nào**.

Luật `EveryMiddlewareClass_IsWiredInto_Pipeline` canh chiều ngược lại: middleware khai mà **không** ai nối
vào pipeline.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §3.1

---

## 4. Cấu hình fail-fast — thiếu cấu hình thì không khởi động

### 4.1 Luật

Mọi cấu hình đọc từ `appsettings`/biến môi trường đi qua `IOptions<T>` với
`ValidateDataAnnotations()` + `ValidateOnStart()`. **Không** project nào ngoài composition
root của Core được inject `IConfiguration` trực tiếp.

Chuỗi kết nối của ứng dụng nằm ở khoá **`ConnectionStrings:Core`** — `AddCore` đọc **một lần**, thiếu hoặc rỗng
⇒ không khởi động; tài khoản trong chuỗi: [`../database/script-runbook.md`](../database/script-runbook.md) §3.3.

```csharp
// Core.Application/Configuration/CoreAuthOptions.cs — POCO thuần, không package hạ tầng
public sealed class CoreAuthOptions
{
    public const string SectionName = "Core:Auth";

    [Required(AllowEmptyStrings = false)]
    public string CookieName { get; init; } = default!;               // cookie phiên — be-api-controller.md §7.4

    [Required(AllowEmptyStrings = false)]
    public string AntiforgeryCookieName { get; init; } = default!;    // cookie nội bộ của antiforgery — be-api-controller.md §7.5; hai tên không được trùng

    [Range(1, 60 * 24)]
    public int SessionMinutes { get; init; }

    [Range(1, 24 * 7)]
    public int SessionAbsoluteHours { get; init; } = 12;   // khoá Core:Auth:SessionAbsoluteHours — cách kiểm: be-api-controller.md §7.4

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

**Không** đọc thẳng `IConfiguration` rồi kết thúc bằng `?? []` hay một mặc định rỗng: cấu hình
**thiếu** phải làm app dừng. `[Required]` + `[MinLength(1)]` trên `AllowedOrigins` là bắt buộc.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §4.2

### 4.3 `ValidateOnStart` ở MỌI môi trường

`ValidateOnStart()` gắn **không điều kiện**, như nhau ở mọi môi trường. Máy dev khởi động được vì **có đủ
giá trị** — không bí mật ở `appsettings.Development.json`, bí mật ở user-secrets
([`repo-artifact.md`](repo-artifact.md) §6.3); thiếu một khoá thì `dotnet run` dừng và nêu tên khoá.

Luật `EveryRequiredOptions_HasA_ValidateOnStart_CodePath` canh: mỗi `Options` bắt buộc có `ValidateOnStart`.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §4.3

### 4.4 Ngoại lệ có tên duy nhất — nhóm khoá `Core:Bootstrap:*`

> **Nhóm cấu hình của lệnh bootstrap KHÔNG gắn `ValidateOnStart`.** Tiến trình API phục vụ thật
> **không đọc** nhóm này; nó chỉ có nghĩa khi người vận hành chạy lệnh bootstrap
> ([`../database/script-runbook.md`](../database/script-runbook.md) §3.3 bước 4, bước 5).

Ngoại lệ chỉ chuyển mốc kiểm, không bỏ fail-fast:

| | Nhóm cấu hình thường | `Core:Bootstrap:*` |
| --- | --- | --- |
| Kiểm lúc nào | Khởi động tiến trình, mọi lần | **Đầu lệnh bootstrap**, trước dòng ghi đầu tiên |
| Thiếu giá trị thì | Tiến trình không khởi động, nêu tên khoá | Lệnh **dừng, không ghi dòng nào**, nêu tên khoá |
| Ai chịu hậu quả | Mọi người dùng | Người vận hành đang đứng tại chỗ chạy lệnh |

Phép kiểm chạy **trước** thao tác ghi đầu tiên — không có trạng thái nửa vời. Ngoại lệ này khai ở đây và
**chỉ** ở đây; thêm một nhóm khoá thứ hai là một quyết định, không phải một lần sửa cấu hình.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §4.4

---

## 5. Dependency Injection

### 5.1 Đăng ký theo nhóm, không liệt kê tay ở host

`AddCore()` gọi các nhóm con, mỗi nhóm sống cùng project mà nó đăng ký:

```csharp
public static IServiceCollection AddCore(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    services.AddCoreOptions(configuration);                 // Core.Web: IOptions + ValidateOnStart (§4)
    services.AddCoreApplication();                          // Core.Application: gom assembly module đã ghi nhận; MediatR, validator, behavior
    services.AddCorePersistence(configuration);             // Core.Infrastructure: kết nối chung, DbContext, UnitOfWork, interceptor
    services.AddCoreIdentity(configuration);                // Core.Infrastructure: Identity lõi, chính sách mật khẩu, security stamp, permission
    services.AddCoreWeb(environment);                       // Core.Web: controller, CORS, rate limit, antiforgery, cookie scheme, IExceptionHandler
    return services;
}
```

**Assembly của module — ghi nhận ở module, gom ở Core**
([`../adr/0025-luu-du-lieu-module-mot-transaction.md`](../adr/0025-luu-du-lieu-module-mot-transaction.md)):

| Bước | Ở đâu | Làm gì |
| --- | --- | --- |
| Ghi nhận | `AddXModule()` của module | Gọi **một** hàm mở rộng của `Core.Application` (tên chốt khi thi công), truyền assembly chứa handler và validator của module. Hàm chỉ **ghi nhận** assembly vào một danh sách giữ trong `IServiceCollection` — không gọi thư viện mediator, không quét validator |
| Gom | `AddCoreApplication()`, bên trong `AddCore` | Đọc danh sách, rồi quét Core cùng mọi assembly đã ghi nhận trong **một** lời đăng ký mediator — behavior đăng ký đúng một lần (luật A10). Mã đăng ký: [`be-cqrs-handler.md`](be-cqrs-handler.md) §5.1 |

**Vì vậy dòng của module ở `Program.cs` đứng TRƯỚC `AddCore`** (§3). Bước gom **niêm** danh sách:
hàm ghi nhận gọi sau khi đã niêm ném `InvalidOperationException` nêu tên assembly ghi nhận muộn.

**Project nào sở hữu kiểu thì project đó sở hữu lời đăng ký.** Giai đoạn đầu đăng ký **tay** trong
từng `AddCoreXxx()`; chỉ chuyển sang quét theo convention khi liệt kê tay thành gánh nặng.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §5.1

### 5.2 Vòng đời DI — định nghĩa gốc

Bảng dưới đây là **nguồn duy nhất** của vòng đời từng seam Core ([`../OWNERSHIP.md`](../OWNERSHIP.md) §2).

| Vòng đời | Dùng cho | Seam / kiểu của Core |
| --- | --- | --- |
| `Singleton` | Không giữ state theo request, thread-safe, khởi tạo đắt | `ResultToHttpMapper` (static), bảng ánh xạ, `IOptions<T>`, `ICacheStore`, `IExecutionContextScope` (giá trị nằm trong `AsyncLocal`, không ở instance), `IPermissionCatalogSource`, `ITenantSeedSource`, `IExceptionHandler` của Core, `ILoginAttemptLimiter` (bộ đếm nằm trong bộ nhớ tiến trình — [`be-api-controller.md`](be-api-controller.md) §6.3 ràng buộc 4) |
| `Scoped` | Bám theo một request / một đơn vị công việc | `DbConnection` dùng chung, mọi `DbContext` (Core và module), mọi repository, `IUnitOfWork`, `ICurrentUser`, **`ITenantContext`**, `IPermissionChecker`, `IIdentityService`, `IUserLookupService`, `IUserAdminService`, `ITenantLookup`, `ISessionPrincipalFactory`, `ITenantProvisioningService` |
| `Transient` | Rẻ, không state, mỗi lần dùng một bản mới | Validator, pipeline behavior, `IBackgroundJobScheduler` |

> **`ITenantContext` bắt buộc là `Scoped`.**

**Luật cứng: không bao giờ inject một `Scoped` vào một `Singleton`.** .NET chỉ bắt được ca này khi
`ValidateScopes` bật (mặc định **tắt** ở Production) — đừng trông vào nó. Job nền và hosted service phải tự
tạo scope bằng `IServiceScopeFactory` cho mỗi lượt chạy.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §5.2

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
│   └── UserErrors.cs
├── Permissions/
├── Menu/
│   └── IMenuItemRepository.cs
└── Common/
    ├── Behaviors/
    ├── Interfaces/
    └── Paging/
```

Một use case = một Command/Query + một Handler + (nếu cần) một Validator, đặt cạnh nhau.

### 6.2 So sánh hai cách

Chọn chia theo nghiệp vụ. `Common/` vẫn chia theo kỹ thuật (`Behaviors/`, `Interfaces/`, `Paging/`); một
file chỉ vào `Common/` khi **từ hai slice trở lên** dùng nó.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §6.2

---

## 7. Thêm một tính năng mới — checklist

Áp cho cả Core lẫn module. Bước nào không áp dụng thì bỏ qua, nhưng đừng đảo thứ tự.

1. **Quyết định phía nào sở hữu.** Từ hai module trở lên cần → Core; một module cần → ở module đó
   ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4).
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
   Kế thừa `ApiControllerBase`; mỗi action không `[AllowAnonymous]` khai **đúng một** mức phân
   quyền — `[RequirePermission]`, `[RequireSystemOperator]` hoặc `[AuthenticatedOnly]` (luật S11,
   [`../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md));
   route tường minh — [`be-api-controller.md`](be-api-controller.md).
9. **Hợp đồng API** → thêm card vào [`../contracts/`](../contracts/) **trước khi** FE bắt đầu.
10. **Test** — theo thứ tự ba tầng ở §9.1: unit Domain, unit Application, integration.
11. **Chạy cổng.** `dotnet test` (gồm ArchTests) phải xanh trước khi báo xong.

---

## 8. SOLID & OOP — áp vào đúng ngữ cảnh Core này

**S — Single Responsibility.** Một Command/Query + một Handler = đúng một use case; một class mang hai
lý do để thay đổi thì tách.

**O — Open/Closed.** Thêm mã lỗi, Command, module mới **không được** đòi sửa `Core.Web`,
`ApiControllerBase`, hay `ResultToHttpMapper`. Sửa `Core.*` để phục vụ riêng một module là vi phạm OCP.

**L — Liskov Substitution.** Mọi implementation dùng được ở **mọi** nơi interface được yêu cầu; không
`NotImplementedException`. `AppUser` **cố ý không** kế thừa `BaseEntity` — nó là `IdentityUser<Guid>`
implement `IAuditableEntity` ([`be-entity-domain.md`](be-entity-domain.md) §1.4).

**I — Interface Segregation.** Ba interface Identity riêng: `IIdentityService` (kiểm thông tin
đăng nhập, đổi mật khẩu), `IUserLookupService` (tra cứu, chỉ đọc), `IUserAdminService` (khoá, gán
vai trò). Ngưỡng: interface vượt 6–8 method là dấu hiệu nên tách — không phải luật cứng; **không**
tách vụn thành interface một method.

**D — Dependency Inversion.** Toàn bộ §1 chính là DIP viết thành đồ thị tham chiếu.

**Encapsulation.** Field nghiệp vụ `private set`; mutate qua method mang tên nghiệp vụ
(`user.Deactivate()`, không `user.IsActive = false`). Ngoại lệ có giới hạn: field audit của
`BaseEntity` và của kiểu mang `IAuditableEntity` có public setter — [`be-entity-domain.md`](be-entity-domain.md) §1.

**Abstraction.** Trong `Domain` và `Application` **không bao giờ** `new` một class của
Infrastructure — cần thì khai interface.

**Inheritance.** Chỉ cho `BaseEntity` và `ApiControllerBase`. Không hierarchy nghiệp vụ nhiều tầng;
ưu tiên composition.

**Polymorphism.** Chỉ khai interface khi thật sự có từ hai implementation trở lên, hiện tại hoặc
cận kề.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §8

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
| Integration | Endpoint qua `WebApplicationFactory`, migration áp được, bộ lọc tenant và xoá mềm có hiệu lực, transaction commit/rollback trọn vẹn, envelope đúng hình dạng, rate limit chặn thật | — |
| Arch | Ranh giới tầng, quy ước đặt tên, mọi thứ "khai rồi có được nối vào không" | Hành vi |

**Seam hạ tầng** — biên duy nhất được giả lập trong unit test (luật T8) — là interface khai ở
`*.Application` và **chỉ** có hiện thực ở `*.Infrastructure`. Bộ lọc tenant, xoá mềm và transaction
**chỉ** được chứng minh bằng integration test.

**`UseInMemoryDatabase` bị CẤM.** Luật T2 ở [`../RULES.md`](../RULES.md) yêu cầu PostgreSQL thật qua
Testcontainers.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §9.1

### 9.2 ArchTest nào canh luật nào

> 📖 **Danh mục tên ArchTest là cột "Ép bằng gì" của [`../RULES.md`](../RULES.md) — đó là nguồn duy nhất.**
> Giải thích từng test canh điều gì, và test nào là integration chứ không phải ArchTest:
> [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md) §2.2.

Dùng **đúng** tên ở đó khi nhắc tới chúng ở bất kỳ đâu.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §9.2

### 9.3 Mọi detector phải có test đối chứng

Luật T1 ở [`../RULES.md`](../RULES.md): mỗi ArchTest phải đi kèm một test `Detector_*` cho nó một mẫu vi
phạm cố ý và kiểm rằng nó đỏ.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §9.3

### 9.4 Bẫy khi thi công detector

Detector cho `CoreSource_MustNotContain_BusinessNameStringLiteral` **ưu tiên phân tích AST, không
quét văn bản nguồn**. Nếu buộc phải quét văn bản, detector phải có test chứng minh nó bỏ qua comment
và định danh.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §9.4

---

## 10. Đếm bằng lệnh, đừng chép số

Khi có `src/`, các câu hỏi đếm — project, behavior, chỗ chưa có `ValidateOnStart` — trả lời bằng lệnh,
không bằng bảng chép tay ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6):

```bash
find src/BE -iname '*.csproj' -not -path '*/obj/*' -not -path '*/bin/*' | sort
grep -rn 'IPipelineBehavior<' src/BE --include='*.cs' | grep -v /obj/
grep -rn 'ValidateOnStart' src/BE --include='*.cs' | grep -v /obj/
```

Hai con số "project trên đĩa" và "project trong solution" **có thể khác nhau**;
`EveryProjectOnDisk_IsDeclared_InSolution` bắt đúng trường hợp đó.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`ly-do/be-architecture.md`](../wiki-core/be/ly-do/be-architecture.md) §10
