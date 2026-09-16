---
kind: lich-su
scope: du-an
verified: chua-doi-chieu
---

# Kế hoạch phát triển repo CoreAndSkill

> # 🗄️ TÀI LIỆU LỊCH SỬ
>
> **File này mô tả trạng thái QUÁ KHỨ. Đường dẫn, con số và nhận định trong đây cố ý không còn đúng — đừng đọc nó để biết hiện trạng, và đừng trích dẫn nó làm căn cứ.**
>
> | Trong file này | Thực tế hiện nay |
> | --- | --- |
> | Bốn file `00-overview/` là kế hoạch **đang thực hiện** | Kế hoạch đã thực thi xong; hiện trạng đọc ở `docs/README.md` |
> | Bảy luật nêu ở đây là dự kiến | Luật thật ở `docs/RULES.md`, kèm cột *Ép bằng gì* |
> | Mô hình phục vụ FE↔API | **Hai origin khác nhau** — [`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md) |
> | Bảng *MỘT PHẦN ĐÃ BỊ THAY THẾ* ngay dưới: các mục ghi *Còn đúng — dùng tiếp*, và §6 Lộ trình trỏ sang `lo-trinh-va-nguon.md` §3 | **Không còn hiệu lực** — lộ trình thi công: [`../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md) · [`../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) |
>
> Nguồn sống: [`../README.md`](../README.md) · [`../RULES.md`](../RULES.md) · [`../adr/README.md`](../adr/README.md)


> **Trạng thái:** Draft v1 · **Ngày:** 2026-09-08 · **Chủ sở hữu:** Trường Nguyễn Xuân
> Tài liệu này là điểm khởi đầu. Mọi quyết định kiến trúc phát sinh sau đó phải được ghi thành ADR trong `docs/01-architecture/adr/`.

> ## ⚠️ MỘT PHẦN ĐÃ BỊ THAY THẾ — 2026-09-08
>
> Viết **trước khi** khảo sát `D:\Manager\PlatformManager`, nên giả định repo trống và phải xây Core từ số 0. Giả định đó sai: PlatformManager đã có Core chạy thật và đã đánh dấu sẵn 79 file `scope: core` để tách ra.
>
> | Mục trong file này | Trạng thái |
> | --- | --- |
> | §1 Mục tiêu, §2 Kiến trúc, §3 Đặc tả BE, §4 Đặc tả FE, §5 `.claude`, §7 Chất lượng, §8 Rủi ro | **Còn đúng** — dùng tiếp |
> | §6 Lộ trình (14.5 tuần, P0→P9) | **THAY THẾ** bởi [`lo-trinh-va-nguon.md`](lo-trinh-va-nguon.md) §3 — trích xuất, ~9 tuần, E0→E6 |
> | §10 Câu hỏi còn mở | **THAY THẾ** bởi [`lo-trinh-va-nguon.md`](lo-trinh-va-nguon.md) §4 — 4/7 câu đã có đáp án |
> | §3.3 "JWT access + refresh rotation" | **SAI** — PlatformManager đã chốt Cookie session, KHÔNG JWT (`Program.cs:382`). Xem [`lo-trinh-va-nguon.md`](lo-trinh-va-nguon.md) §4.5 |
> | §3.1 "Result Pattern" + §3.2 "ProblemDetails RFC 7807" | **ĐÃ LỖI THỜI** — hình dạng envelope thật đã chốt khác (một khối `error` mang `code`/`type`/`message`/`messageParams`/`fieldErrors`, kèm `traceId`). File chủ là [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md); hợp đồng `Result<T>` ở [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) |
> | §2.2 "Cấu trúc thư mục đích" | **ĐÃ LỖI THỜI** — cây thư mục ở đó (`backend/src/Core`, `docs/01-architecture/`, `docs/02-backend/`…) là bản phác đầu, không phải cấu trúc đã dựng. Cấu trúc thật: [`../README.md`](../README.md) |
> | Mọi chỗ ghi ".NET 9" | **SAI** — thực tế là .NET 10 (`src/BE/Directory.Build.props`) |
> | Mọi chỗ ghi "Angular 18+", "PrimeNG hoặc Material" | **SAI** — thực tế Angular 20.3 + PrimeNG 20.2 |
> | §5.3 gộp 7 skill vào một phase | **Thiếu chính xác** — skill *sinh code* phải sau module mẫu; skill *đọc/kiểm* làm được ngay. Xem [`lo-trinh-va-nguon.md`](lo-trinh-va-nguon.md) §3.2 |
> | §1.3 "Không làm multi-tenant ở v1" và §3.6 "chừa đường, không implement" | 🔄 **ĐÃ LẬT 2026-09-08** — multi-tenant nay **nằm trong phạm vi v1**. Nhiều cơ quan dùng chung một bản cài, nên điều kiện kích hoạt đã thoả ngay từ đầu. Xem [`../adr/0013-multi-tenant.md`](../adr/0013-multi-tenant.md) · [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) · [`../RULES.md`](../RULES.md) §9 |
> | §3.6 "`ICurrentUser.TenantId` v1 luôn trả null", "`ITenantScoped` interface rỗng" | **SAI** — cả hai nay mang giá trị thật và được bảy luật M1–M7 canh |
> | §4.2 "`AuthInterceptor` — gắn token, tự refresh khi 401, xếp hàng request trong lúc refresh" | **SAI** — đó là mô hình JWT. Đã chốt **phiên bằng cookie**, không có token để gắn cũng không có refresh để xếp hàng. Xem [`../adr/0004-giu-aspnet-identity.md`](../adr/0004-giu-aspnet-identity.md) và [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §2 |
> | §4.2 "`BaseApiService<TDto, TId>`" | **SAI** — base class cho service đã bị loại, không hoãn. Thay bằng hàm thuần nhận mapper làm tham số bắt buộc. Xem [`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §9 |

---

## 1. Mục tiêu & phạm vi

### 1.1 Repo này là gì

`CoreAndSkill` là **bộ khung khởi tạo dự án (starter framework)** cho các hệ thống nghiệp vụ tầm trung, gồm 3 tài sản gắn chặt với nhau:

| Tài sản | Vai trò | Thư mục |
|---|---|---|
| **Source Core** | Hạ tầng kỹ thuật dùng lại: auth, user, message, kiến trúc sạch, cross-cutting concerns | `backend/src/Core`, `frontend/projects/core` + `frontend/projects/ui` |
| **Modules** | Nghiệp vụ, được lắp lên Core. Nơi 90% code dự án thực tế được viết | `backend/src/Modules`, `frontend/projects/features` |
| **.claude** | Agent + Skill để BA hoá yêu cầu, code, review, test theo đúng chuẩn | `.claude/` |
| **docs** | **Nguồn sự thật duy nhất (SSOT)**. Code và agent đều phải phục tùng | `docs/` |

### 1.2 Tiêu chí thành công (đo được)

Repo được coi là "xong v1.0" khi một dev mới có thể:

1. Clone repo, chạy script setup → API + FE chạy được trong **< 15 phút**, không cần hỏi ai.
2. Chạy skill `/core-new-module BanHang` → sinh ra module 4 tầng đầy đủ, build pass, test pass, có sẵn 1 CRUD mẫu.
3. Viết 1 use case mới (command + validator + handler + endpoint + test) trong **< 30 phút**.
4. Mở PR → CI tự chặn nếu vi phạm layering, thiếu test, hoặc lệch chuẩn code.
5. Không cần đọc code Core để dùng Core — chỉ đọc `docs/`.

### 1.3 Ngoài phạm vi (nói rõ để tránh phình)

- Không làm microservices. Không service mesh, không API gateway riêng.
- Không làm CQRS với read-model tách DB (chỉ tách Command/Query ở tầng application).
- Không làm Event Sourcing.
- Không làm multi-tenant ở v1 — nhưng **thiết kế không được chặn đường** (xem §3.6).
- Không làm micro-frontend / Module Federation ở v1.

**Lý do:** hệ thống tầm trung (10–50 bảng, vài chục đến vài nghìn user) không trả nổi chi phí vận hành của những thứ trên. Đây là lỗi phổ biến nhất khi làm "core dùng chung".

---

## 2. Quyết định kiến trúc nền tảng

### 2.1 Modular Monolith — 1 solution, 1 process, N module

Đã chốt: **không dùng NuGet package, không dùng submodule.** Core và Modules nằm chung 1 solution nhưng tách thư mục, tách project, tách schema DB.

```
                    ┌─────────────────────────┐
                    │        Host (API)       │  ← composition root duy nhất
                    └────────────┬────────────┘
                                 │ đăng ký module
          ┌──────────────┬───────┴───────┬──────────────┐
          ▼              ▼               ▼              ▼
   ┌────────────┐ ┌────────────┐  ┌────────────┐ ┌────────────┐
   │  Identity  │ │Notification│  │  BanHang   │ │  KhoHang   │   ← Modules
   └─────┬──────┘ └─────┬──────┘  └─────┬──────┘ └─────┬──────┘
         └──────────────┴───────┬───────┴──────────────┘
                                ▼
                    ┌─────────────────────────┐
                    │          CORE           │  ← không biết module nào tồn tại
                    │ Domain·App·Infra·Web    │
                    └─────────────────────────┘
```

**Ba luật bất di bất dịch** (sẽ được ép bằng test, không phải bằng niềm tin):

| # | Luật | Cách ép |
|---|---|---|
| R1 | Core **không bao giờ** tham chiếu Module | `ArchitectureTests` + không có ProjectReference |
| R2 | Module A **không bao giờ** tham chiếu trực tiếp Module B | Chỉ được đi qua `Core.Contracts` (integration event / interface công khai) |
| R3 | Tầng trong không biết tầng ngoài | Domain ⊄ Application ⊄ Infrastructure/Endpoints |

> **Vì sao modular monolith:** với hệ tầm trung, nó cho bạn *ranh giới* của microservices (dễ tách sau) mà không phải trả *chi phí vận hành* của microservices (distributed transaction, tracing phân tán, N pipeline deploy). Nếu 2 năm nữa module `BanHang` cần scale riêng, việc tách nó ra thành service độc lập là **cơ học** vì ranh giới đã sẵn.

### 2.2 Cấu trúc thư mục đích

```
CoreAndSkill/
├─ CLAUDE.md                        ← trỏ agent về docs/, liệt kê rule cứng
├─ docs/                            ← SSOT
│  ├─ 00-overview/                  development-plan.md, glossary.md
│  ├─ 01-architecture/              layering.md, module-contract.md, adr/
│  ├─ 02-backend/                   coding-standard.md, result-pattern.md,
│  │                                error-catalog.md, auth-spec.md, di-convention.md
│  ├─ 03-frontend/                  angular-standard.md, ui-kit.md, state.md, form.md
│  ├─ 04-database/                  naming.md, migration-policy.md, indexing.md
│  ├─ 05-api/                       rest-convention.md, versioning.md,
│  │                                pagination.md, error-envelope.md
│  ├─ 06-process/                   git-flow.md, pr-checklist.md,
│  │                                definition-of-done.md, story-point.md
│  ├─ 07-testing/                   test-strategy.md, naming.md, coverage-gate.md
│  ├─ 08-templates/                 us-template.md, adr-template.md, techdoc-template.md
│  └─ RULES.md                      ← bản rút gọn, mọi rule dạng MUST / MUST NOT
│
├─ .claude/
│  ├─ agents/                       architect, ba, backend-dev, frontend-dev,
│  │                                reviewer, tester, tech-writer
│  ├─ skills/                       core-new-module, core-new-usecase, core-new-entity,
│  │                                fe-new-feature, arch-check, review-pr, doc-sync
│  ├─ commands/
│  ├─ hooks/                        format + build sau khi sửa file
│  └─ settings.json
│
├─ backend/
│  ├─ CoreAndSkill.sln
│  ├─ Directory.Build.props         ← LangVersion, Nullable, TreatWarningsAsErrors
│  ├─ Directory.Packages.props      ← Central Package Management (khoá version 1 chỗ)
│  ├─ .editorconfig                 ← analyzer rules, severity = error
│  ├─ src/
│  │  ├─ Core/
│  │  │  ├─ Core.Domain/            BaseEntity, AggregateRoot, ValueObject,
│  │  │  │                          DomainEvent, Result<T>, Error, IAuditable, ISoftDelete
│  │  │  ├─ Core.Application/       ICommand/IQuery, pipeline behaviors (Validation,
│  │  │  │                          Logging, Transaction, Caching, Performance),
│  │  │  │                          ICurrentUser, IUnitOfWork, PagedResult<T>, IDateTime
│  │  │  ├─ Core.Infrastructure/    CoreDbContext base, Repository<T>, Outbox,
│  │  │  │                          RedisCacheService, IMessageBus, FileStorage,
│  │  │  │                          EmailSender, PasswordHasher, JwtTokenService
│  │  │  ├─ Core.Web/               ExceptionHandlingMiddleware → ProblemDetails,
│  │  │  │                          ApiVersioning, CORS, RateLimit, Swagger,
│  │  │  │                          HealthChecks, PermissionAuthorizationHandler
│  │  │  └─ Core.Contracts/         Integration events, shared DTO/enum liên module
│  │  ├─ Modules/
│  │  │  ├─ Identity/               ← MODULE MẪU CHUẨN (dev copy theo)
│  │  │  │  ├─ Identity.Domain/
│  │  │  │  ├─ Identity.Application/
│  │  │  │  ├─ Identity.Infrastructure/
│  │  │  │  └─ Identity.Endpoints/
│  │  │  ├─ Notification/           ← module thứ 2, chứng minh Core không bị lock vào Identity
│  │  │  └─ _Template/              ← khung rỗng cho skill scaffold
│  │  └─ Host/
│  │     └─ Api/                    Program.cs, appsettings, module registration
│  └─ tests/
│     ├─ Core.UnitTests/
│     ├─ Identity.UnitTests/
│     ├─ IntegrationTests/          Testcontainers: Postgres + Redis + RabbitMQ
│     └─ ArchitectureTests/         ← NetArchTest: ép R1, R2, R3
│
├─ frontend/
│  ├─ angular.json                  Angular workspace nhiều project
│  ├─ projects/
│  │  ├─ core/                      lib @app/core: interceptor (auth, error, loading),
│  │  │                             AuthService, PermissionDirective, guard,
│  │  │                             BaseApiService, ApiResult model, i18n
│  │  ├─ ui/                        lib @app/ui: DataTable, FormField, Modal, Upload,
│  │  │                             ConfirmDialog, Toast, PageHeader, EmptyState
│  │  ├─ shell/                     app: layout, sidebar, routing gốc
│  │  └─ features/
│  │     └─ identity/               lib feature: user, role, permission, profile
│  └─ eslint.config.js              ← ép boundary: features không import lẫn nhau
│
├─ database/                        seed script, ERD
├─ scripts/                         setup.ps1, seed.ps1, reset-db.ps1
└─ .github/workflows/               (hoặc azure-pipelines.yml)
```

### 2.3 Vì sao Core tách 5 project chứ không phải 1

Cám dỗ lớn là gom hết vào `Core.dll` cho gọn. Đừng. Tách 5 project là cách **duy nhất** compiler ép được luật layering — `Core.Domain` không tham chiếu `Core.Infrastructure` thì code trong Domain **không thể** gọi `DbContext`, dù dev có muốn. Đây là hàng rào chi phí bằng 0 và không bao giờ hỏng.

### 2.4 Ranh giới DB giữa các module

- Mỗi module **1 schema Postgres riêng**: `identity.users`, `banhang.orders`.
- Mỗi module có `DbContext` riêng, kế thừa `CoreDbContext`.
- **Cấm FK vật lý xuyên schema.** Module tham chiếu nhau bằng ID (`Guid CreatedByUserId`), không bằng navigation property.
- Migration độc lập theo module → deploy từng module không đụng nhau.

> Đây là điều kiện tiên quyết để sau này tách microservice. Một FK xuyên schema là một sợi dây trói vĩnh viễn.

---

## 3. Đặc tả các thành phần Core (backend)

### 3.1 Result Pattern thay vì Exception cho lỗi nghiệp vụ

```csharp
// Core.Domain
public sealed record Error(string Code, string Message, ErrorType Type);
public class Result<T> { bool IsSuccess; T Value; Error Error; }
```

- **Exception** chỉ dành cho lỗi *ngoài dự kiến* (mất DB, bug).
- **Result** dành cho lỗi *dự kiến được* (email trùng, số dư không đủ).
- Mọi `Error.Code` phải có trong `docs/02-backend/error-catalog.md` — đây là hợp đồng với FE và với đội test.

**Lý do:** exception cho luồng nghiệp vụ làm chậm (stack unwinding), làm mất tính tường minh của chữ ký hàm, và khiến FE không biết trước tập lỗi có thể xảy ra.

### 3.2 Error envelope thống nhất — RFC 7807 ProblemDetails

Mọi lỗi trả về FE có đúng 1 hình dạng:

```json
{
  "type": "https://docs.internal/errors/IDENTITY.EMAIL_DUPLICATED",
  "title": "Email đã tồn tại",
  "status": 409,
  "detail": "Email đã được đăng ký trong hệ thống.",
  "traceId": "00-4bf92f...-01",
  "errors": { "email": ["Email đã tồn tại"] }
}
```

FE có **một** interceptor duy nhất xử lý toàn bộ lỗi. Không có `try/catch` rải rác trong component.

### 3.3 Authentication & Authorization

| Hạng mục | Quyết định |
|---|---|
| Token | JWT access (15 phút) + refresh token (7 ngày), **rotation** mỗi lần refresh |
| Lưu refresh token | Bảng `identity.refresh_tokens` (lưu hash), revoke được theo thiết bị |
| Logout tức thì | Redis blacklist theo `jti`, TTL = thời gian sống còn lại của access token |
| Phân quyền | **Permission-based**, KHÔNG role-based cứng. Role chỉ là túi chứa permission |
| Kiểm tra | `[HasPermission("banhang.order.approve")]` → `PermissionAuthorizationHandler` |
| Mật khẩu | ASP.NET Core Identity hasher (PBKDF2) hoặc Argon2id |
| Chuẩn bị sẵn | Interface `IExternalAuthProvider` cho SSO/OAuth sau này (không implement v1) |

> **Vì sao permission-based:** gần như 100% dự án doanh nghiệp sẽ gặp yêu cầu "ông A giống ông B nhưng không được duyệt đơn". Role-based cứng chết ở yêu cầu đó, và refactor auth giữa dự án là việc rất đau.

### 3.4 Message / Notification

Tách rõ 3 khái niệm hay bị lẫn:

1. **Domain Event** — trong process, cùng transaction. `OrderApproved` → cập nhật tồn kho.
2. **Integration Event** — qua message bus, khác transaction. Dùng **Outbox pattern** để không mất tin.
3. **Notification** — thông báo tới người dùng (in-app, email, push). Là một *consumer* của integration event, không phải cơ chế.

`Core.Infrastructure` cung cấp `IMessageBus` + `OutboxProcessor` (background service). Module `Notification` là consumer mẫu.

### 3.5 Cross-cutting qua Pipeline Behavior

Thứ tự bắt buộc trong `Core.Application`:

```
Request → Logging → Validation → Authorization → Caching(query) → Transaction(command) → Handler
```

Dev viết handler chỉ có logic nghiệp vụ thuần. Validation, transaction, log, cache là **miễn phí** vì đã nằm ở behavior.

### 3.6 Chừa đường cho multi-tenant (không implement)

Chỉ cần 2 việc rẻ tiền ở v1:

- `ICurrentUser` có sẵn thuộc tính `TenantId` (v1 luôn trả `null`).
- Base entity có `ITenantScoped` (interface rỗng, chưa dùng).

Khi cần multi-tenant, chỉ phải thêm 1 EF global query filter — không phải sửa 200 file.

---

## 4. Đặc tả Core Frontend (Angular)

### 4.1 Nguyên tắc

| Vấn đề | Quyết định |
|---|---|
| Kiến trúc | **Standalone components** + Angular Signals. Không dùng NgModule cho code mới |
| State | Signal-based service store cho state nội bộ. **Không NgRx ở v1** — chỉ thêm khi có state phức tạp thật |
| Server state | Service + signal, cache theo key. Không tự viết framework cache |
| Thư viện UI | 1 lựa chọn duy nhất (PrimeNG hoặc Angular Material) — bọc lại trong `@app/ui`, **component nghiệp vụ không import trực tiếp thư viện gốc** |
| Form | Typed Reactive Forms + factory sinh form từ metadata |
| i18n | Chuẩn bị từ ngày đầu. Thêm sau rất tốn |
| Ranh giới | ESLint boundary rule: `features/*` không import chéo nhau, chỉ import `core` và `ui` |

> **Vì sao bọc thư viện UI:** khi thư viện lên major version phá vỡ API, bạn sửa 1 chỗ (`@app/ui`) thay vì 300 chỗ. Chi phí bọc ban đầu khoảng 2 ngày, tiết kiệm hàng tuần về sau.

### 4.2 `@app/core` bắt buộc có

- `AuthInterceptor` — gắn token, tự refresh khi 401, xếp hàng request trong lúc refresh.
- `ErrorInterceptor` — đọc ProblemDetails, map sang toast / form error theo `error-catalog.md`.
- `LoadingInterceptor` — đếm request đang chạy → signal global.
- `authGuard`, `permissionGuard` + directive `*appHasPermission`.
- CRUD + phân trang dùng chung. **Hình dạng đã thay đổi so với dòng gốc ở đây:** không phải một base class để kế thừa, mà là **hàm thuần nhận mapper làm tham số** — xem [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) §5 và [`../wiki-core/fe/01-core-components.md`](../wiki-core/fe/01-core-components.md) §9.

### 4.3 `@app/ui` bắt buộc có

`DataTable` (server-side paging/sort/filter), `FormField`, `Modal`, `ConfirmDialog`, `Toast`, `FileUpload`, `PageHeader`, `EmptyState`, `SkeletonLoader`.

---

## 5. Tầng `.claude` — agent & skill

### 5.1 Sự thật cần chấp nhận trước

> **Không thể ép LLM tuân thủ 100% bằng văn bản.** Prompt là *hướng dẫn*, không phải *ràng buộc*.

Nên chiến lược là **hai lớp**:

| Lớp | Cơ chế | Đảm bảo |
|---|---|---|
| **Lớp mềm** — hướng dẫn | `CLAUDE.md`, skill đọc `docs/` trước khi làm | phần lớn đúng ngay lần đầu |
| **Lớp cứng** — ép buộc | `ArchitectureTests`, analyzer `TreatWarningsAsErrors`, ESLint boundary, CI gate | 100% — vi phạm thì **không build được** |

Mọi rule trong `docs/RULES.md` phải trả lời được câu: *"Rule này được ép bằng cái gì?"* Nếu câu trả lời là "bằng niềm tin", nó không phải rule — nó là gợi ý.

### 5.2 Agents

| Agent | Vai trò | Đọc bắt buộc |
|---|---|---|
| `vnr-architect` | Quyết định kiến trúc, viết ADR, phản biện thiết kế | `01-architecture/` |
| `vnr-ba` | Chuyển yêu cầu thô → US + AC + BR + luồng nghiệp vụ | `08-templates/us-template.md` |
| `vnr-backend-dev` | Code BE theo chuẩn Core, không tự phát minh pattern | `02-backend/`, `05-api/` |
| `vnr-frontend-dev` | Code Angular theo `@app/core` + `@app/ui` | `03-frontend/` |
| `vnr-reviewer` | Review theo checklist, **chỉ báo lỗi có thật, có dòng cụ thể** | `06-process/pr-checklist.md` |
| `vnr-tester` | Sinh unit + integration test, tìm ca biên | `07-testing/` |
| `vnr-tech-writer` | Sinh TechDoc / UserGuide / UnitTest doc (đã có sẵn, tái dùng) | `08-templates/` |

### 5.3 Skills

| Skill | Đầu ra |
|---|---|
| `/core-new-module <Tên>` | 4 project + DbContext + schema + đăng ký Host + 1 CRUD mẫu + test → build & test pass |
| `/core-new-usecase <Module> <Tên>` | Command/Query + Validator + Handler + Endpoint + unit test + mục trong error-catalog |
| `/core-new-entity <Module> <Tên>` | Entity + EF configuration + migration + repository |
| `/fe-new-feature <Tên>` | Angular feature lib + routing + service + trang list/detail dùng `@app/ui` |
| `/arch-check` | Chạy ArchitectureTests + lint boundary, báo cáo vi phạm layering |
| `/review-pr` | Review diff theo `pr-checklist.md`, xuất findings có file:line |
| `/doc-sync` | Dò lệch giữa `docs/` và code (endpoint thiếu doc, error code lạ, entity chưa có trong DB doc) |

### 5.4 Hooks

- `PostToolUse` trên Edit/Write file `.cs` → `dotnet format` + build project tương ứng.
- `PostToolUse` trên `.ts` / `.html` → `eslint --fix` + `prettier`.
- `PreToolUse` cảnh báo khi agent sửa file trong `backend/src/Core/` — Core là hạ tầng ổn định, mọi thay đổi phải có lý do và ADR.

---

## 6. Lộ trình thực thi

**Giả định:** 1 senior toàn thời gian, có thể kèm 1 dev hỗ trợ. Đơn vị là **tuần-người**, giả định 5–6 giờ code thực/ngày, phần còn lại là họp, suy nghĩ, việc phát sinh.

| Phase | Nội dung | Effort | Cột mốc nghiệm thu (DoD) |
|---|---|---|---|
| **P0** | Nền móng repo & docs | 1 tuần | `docs/RULES.md` + `01-architecture/layering.md` + 3 ADR đầu tiên xong. Solution rỗng build pass. `Directory.Build.props` + `.editorconfig` đã bật `TreatWarningsAsErrors` |
| **P1** | Core kernel BE | 2 tuần | `Core.Domain` + `Core.Application` + `Core.Web` xong. Result pattern, ProblemDetails, pipeline behaviors, healthcheck chạy. `ArchitectureTests` xanh và **thực sự đỏ** khi cố tình vi phạm |
| **P2** | Core Infrastructure + Postgres | 1.5 tuần | `CoreDbContext`, Repository, UnitOfWork, audit, soft-delete, migration chạy. IntegrationTests với Testcontainers Postgres xanh |
| **P3** | Module Identity (module mẫu) | 2 tuần | Đăng nhập, refresh rotation, logout, CRUD user/role/permission, seed admin. Đây là **bản mẫu tham chiếu** cho mọi module sau |
| **P4** | Redis + Message bus + Outbox | 1 tuần | Cache behavior chạy, blacklist token chạy, Outbox publish được, module `Notification` consume được 1 event thật |
| **P5** | Core FE Angular | 2 tuần | Workspace + `@app/core` (3 interceptor, guard, directive) + `@app/ui` (9 component). Có trang demo hoặc Storybook cho UI kit |
| **P6** | Feature Identity FE | 1.5 tuần | Login, quên mật khẩu, đổi mật khẩu, quản trị user/role. Chứng minh full-stack thông suốt |
| **P7** | `.claude` agents + skills | 1.5 tuần | 7 agent + 7 skill. Test thực: `/core-new-module` sinh module build pass mà không cần sửa tay |
| **P8** | Chất lượng & CI | 1 tuần | Pipeline: build → test → coverage gate → arch test → lint. PR không pass thì không merge được |
| **P9** | Docker + vận hành | 1 tuần | `docker compose up` chạy toàn hệ. Dockerfile multi-stage. Logging có cấu trúc (Serilog) + OpenTelemetry trace |

**Tổng: khoảng 14.5 tuần-người ≈ 3.5 tháng** cho 1 người. Với 2 người và tách BE/FE song song từ P5: **khoảng 9–10 tuần**.

### 6.1 Đường găng & song song hoá

```
P0 → P1 → P2 → P3 ─┬→ P4 ────────┬→ P8 → P9
                   └→ P5 → P6 ───┘
                        P7 (bắt đầu song song sau P3)
```

- **P7 (.claude) phải đợi P3.** Skill scaffold module cần một module mẫu *đã đúng* để sao chép. Viết skill trước khi có mẫu là viết mù.
- **P5 (FE) có thể bắt đầu song song với P4** nếu có 2 người — chỉ cần API Identity từ P3 đã ổn định.

### 6.2 Mốc "dùng được sớm"

Đừng đợi hết P9. Sau **P3 + P6** (khoảng tuần 10) bộ khung đã đủ dùng cho một dự án thật quy mô nhỏ. Hãy **áp nó vào 1 dự án thật ngay lúc đó** — đây là cách duy nhất phát hiện những chỗ Core thiết kế sai. Bộ khung chưa từng chạy dự án thật là bộ khung chưa được kiểm chứng.

---

## 7. Chất lượng & cổng chặn

### 7.1 Kim tự tháp test

| Loại | Tỷ lệ | Phạm vi | Công cụ |
|---|---|---|---|
| Unit | ~70% | Domain logic, validator, handler (mock repo) | xUnit + FluentAssertions + NSubstitute |
| Integration | ~25% | Endpoint → DB thật, transaction, migration | `WebApplicationFactory` + Testcontainers |
| Architecture | — | Ép R1/R2/R3, quy tắc đặt tên | NetArchTest |
| E2E | ~5% | 3–5 luồng sống còn (login, tạo đơn) | Playwright |

### 7.2 Cổng CI (merge bị chặn nếu đỏ)

1. Build không warning (`TreatWarningsAsErrors=true`).
2. Toàn bộ test xanh.
3. Coverage **Core.Domain + Core.Application ≥ 80%**; module ≥ 60%. Không đặt mục tiêu 100% — nó tạo ra test rác.
4. ArchitectureTests xanh.
5. ESLint boundary xanh.
6. Không có secret trong code (gitleaks).

### 7.3 Git flow

`main` (luôn deploy được) ← `develop` ← `feature/<US-id>-<mô-tả>`.
Squash merge. Commit theo Conventional Commits. PR không quá ~400 dòng thay đổi — PR lớn hơn không ai review nổi thật sự.

---

## 8. Rủi ro & cách chặn

| Rủi ro | Xác suất | Tác động | Cách chặn |
|---|---|---|---|
| **Core phình to** — nhét nghiệp vụ vào Core "cho tiện" | Cao | Cao | Luật: code chỉ vào Core khi **từ 2 module trở lên** cần nó. Hook cảnh báo khi sửa `Core/`. Mỗi PR đụng Core phải có ADR |
| **Over-engineering** — thêm CQRS/ES/microservice "cho sau này" | Cao | Cao | Mục §1.3 là hợp đồng. Muốn phá phải viết ADR nêu rõ chi phí vận hành |
| **Docs lệch code** | Rất cao | Trung bình | Skill `/doc-sync` chạy định kỳ trong CI. Docs nằm trong DoD, không phải việc "làm sau" |
| **Agent tạo code lệch chuẩn** | Cao | Trung bình | Lớp cứng §5.1 — build fail thì code lệch chuẩn không lọt được |
| **Module coupling ngầm** | Trung bình | Cao | ArchitectureTests R2 + cấm FK xuyên schema |
| **Bộ khung không ai dùng** | Trung bình | Rất cao | Áp vào dự án thật ở tuần 10 (§6.2), không đợi hoàn hảo |
| **Angular / thư viện UI breaking change** | Trung bình | Trung bình | Bọc trong `@app/ui`, khoá version, nâng cấp có kế hoạch |

---

## 9. Việc cần làm ngay (tuần đầu tiên)

1. Chốt và ghi ADR-0001 *Modular Monolith*, ADR-0002 *Result Pattern*, ADR-0003 *Permission-based Authorization*.
2. Viết `docs/RULES.md` — mỗi rule kèm cột "ép bằng gì".
3. Viết `docs/01-architecture/layering.md` + `module-contract.md` (module phải cung cấp gì để lắp vào Host).
4. Dựng solution rỗng + `Directory.Build.props` + `Directory.Packages.props` + `.editorconfig`.
5. Viết `ArchitectureTests` **trước** khi viết Core — để hàng rào có sẵn từ dòng code đầu tiên.
6. Viết `CLAUDE.md` ở root trỏ agent về `docs/RULES.md`.

> Điểm 5 quan trọng nhất. Viết test kiến trúc *sau* khi đã có code luôn dẫn tới việc phải nới lỏng luật cho khớp với code sai đã lỡ viết.

---

## 10. Câu hỏi còn mở

Cần trả lời trước khi bắt đầu P1:

1. **Thư viện UI Angular:** PrimeNG hay Angular Material? Ảnh hưởng toàn bộ `@app/ui`.
2. **Message broker:** RabbitMQ hay Kafka? Với hệ tầm trung, RabbitMQ gần như luôn là đáp án đúng.
3. **Có yêu cầu multi-tenant trong 12 tháng tới không?** Nếu "chắc chắn có" thì nên làm ngay ở P2, rẻ hơn nhiều so với thêm sau.
4. **Ngôn ngữ hiển thị:** chỉ tiếng Việt hay đa ngôn ngữ? Ảnh hưởng thiết kế i18n và cả error-catalog.
5. **Đội dùng bộ khung này gồm mấy người, trình độ thế nào?** Quyết định độ "chặt" của lớp cứng và độ chi tiết của docs.
