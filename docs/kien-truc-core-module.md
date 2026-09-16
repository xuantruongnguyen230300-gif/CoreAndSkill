---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Kiến trúc Core ↔ Module — ranh giới tái sử dụng cho BE và FE

> **File chủ về ranh giới:** cái gì thuộc **Core** (mang đi được sang mọi dự án dựng trên nền tảng này), cái gì thuộc **Module** (nghiệp vụ riêng của một dự án).
>
> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo đang ở giai đoạn 1, chưa có `src/`. Toàn bộ layout dưới đây là thứ `src/` phải trở thành, không phải mô tả hiện trạng.

---

## 1. Mô hình: Modular Monolith

Một solution, một process, N module. Core là **framework nội bộ**; module là nghiệp vụ lắp lên Core qua host (§2.4). Core **không biết module nào tồn tại**.

### Vì sao Modular Monolith, không phải microservices

📖 [`adr/0001-modular-monolith.md`](adr/0001-modular-monolith.md)

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §1

---

## 2. Backend — Năm project Core — định nghĩa gốc

**Nguồn duy nhất** của danh sách project Core và ranh giới từng project; file khác trỏ về đây, không chép lại ([`OWNERSHIP.md`](OWNERSHIP.md) §2).

| Project | Chứa | **Cấm** chứa | Tham chiếu được |
| --- | --- | --- | --- |
| `Core.Domain` | Entity gốc, Value Object, `Result<T>`, `Error`, `ErrorType`, guard clause, catalog lỗi của invariant Domain, luật nghiệp vụ thuần | **Bất kỳ package nào** (zero package reference) — không EF Core, không MediatR, không FluentValidation, không `Microsoft.Extensions.*` | — (chỉ BCL) |
| `Core.Application` | `ICommand<T>`/`IQuery<T>`, handler, validator, pipeline behavior, DTO, catalog lỗi cấp use case, **mọi seam ra ngoài** (danh sách đầy đủ: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §1.1) | EF Core (`DbContext`, `Include`, `AsQueryable`), ASP.NET Core (`HttpContext`, `IActionResult`), `IConfiguration` trực tiếp, mọi kiểu của Identity, mọi project Infrastructure/Web | `Domain` |
| `Core.Infrastructure` | `CoreDbContext`, `IUnitOfWork` điều phối mọi `DbContext` trên một kết nối và một transaction, repository, `IEntityTypeConfiguration<T>`, interceptor, **migration schema `core`**, **Identity lõi** (`AddIdentityCore`, store, `UserManager`, chính sách mật khẩu, kiểm security stamp theo `userId`; `AppUser`/`AppRole`), service tạo đơn vị, cache, gửi mail, lưu file, job nền, implementation của mọi seam khai ở Application | Controller, middleware, cookie scheme, **bất cứ thứ gì phụ thuộc `HttpContext`** | `Domain`, `Application` |
| `Core.Web` | `ApiControllerBase`, `ApiEnvelope` (`Core.Web/Http/ApiEnvelope.cs`), middleware, `IExceptionHandler` của Core, `HttpContextCurrentUser`, `ModelBindingProblemFactory`, ánh xạ `Result` → HTTP, ba attribute phân quyền endpoint cùng filter, **cookie scheme, cookie events, `HttpContext.SignInAsync` / `SignOutAsync`**, **runner lệnh vận hành `RunCoreCommandAsync`** (nhận dòng lệnh của host, chạy lệnh rồi thoát, không mở cổng — vai dòng lệnh duy nhất của project này), controller Core, `AddCore()` / `UseCore()` | Nghiệp vụ của bất kỳ dự án nào; `DbContext` gọi thẳng | `Domain`, `Application`, `Infrastructure` |
| `Core.Contracts` | Integration event, DTO dùng chung **giữa các module** | Kiểu của đường vào HTTP (envelope nằm ở `Core.Web`); bất cứ thứ gì phụ thuộc project Core khác — nó phải tự đứng một mình | — (chỉ BCL) |

Host là `CoreAndSkill.Api`; module là `CoreAndSkill.Modules.<X>.*`. Sơ đồ chiều phụ thuộc và
danh sách ArchTest canh từng dòng: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §1.2–§1.3.

Từ phía module có thêm đúng một cạnh bắt buộc: `Modules.<X>.Infrastructure` tham chiếu
`Core.Infrastructure`; chiều ngược lại (`Core.*` → `Modules.*`) vẫn bị cấm.

Thư mục trên đĩa: project Core ở `src/BE/Core/CoreAndSkill.Core.<Tầng>/`, host ở
`src/BE/CoreAndSkill.Api/`, test ở `src/BE/Tests/` ([`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §9.1).

ADR của hai ranh giới trong bảng: Identity lõi ↔ cookie — [`adr/0026-ranh-gioi-identity-va-cookie.md`](adr/0026-ranh-gioi-identity-va-cookie.md); service tạo đơn vị và runner lệnh — [`adr/0023-dich-vu-tao-don-vi-dung-chung.md`](adr/0023-dich-vu-tao-don-vi-dung-chung.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §2

### 2.1 Vì sao tách 5 project chứ không gộp 1

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §2.1

### 2.2 Vì sao có `Core.Web` — bệnh nặng nhất cần trị

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §2.2

### 2.3 Vì sao KHÔNG có `Core.Common` và `Core.Persistence`

Đã loại — 📖 [`adr/0002-core-5-project.md`](adr/0002-core-5-project.md)

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §2.3

### 2.4 Host — composition root duy nhất

Project host (`Api`) **chỉ được** chứa:

- `Program.cs` — gọi `AddCore()`, nhường dòng lệnh cho runner lệnh Core (`RunCoreCommandAsync`), gọi `UseCore()`, đăng ký module. Mẫu: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §3
- `appsettings*.json`
- Đăng ký module của dự án

**Cấm** chứa: middleware, controller, entity, handler, migration. Luật A7 ở [`RULES.md`](RULES.md) ép điều này.

Ngưỡng tự kiểm: **`Program.cs` dưới 50 dòng.**

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §2.4

---

## 3. Ba luật ranh giới — ép bằng test, không bằng niềm tin

| # | Luật | Ép bằng |
| --- | --- | --- |
| **R1** | Core **không bao giờ** tham chiếu Module | Không có ProjectReference + ArchTest A3 |
| **R2** | Module A **không bao giờ** tham chiếu trực tiếp Module B — chỉ qua `Core.Contracts` | ArchTest A4 |
| **R3** | Tầng trong không biết tầng ngoài: Domain ⊄ Application ⊄ Infrastructure/Web | Đồ thị ProjectReference + ArchTest A1, A2 |

**R1 có một dạng vi phạm tinh vi hơn ProjectReference:** Core biết *tên* của nghiệp vụ. Một chuỗi `"DonHang"` nằm trong `Core.Application` là vi phạm R1 kể cả khi không có tham chiếu assembly nào. Luật A5 bắt dạng này; detector của nó **ưu tiên phân tích AST** — nếu quét văn bản thì phải có test chứng minh nó bỏ qua comment và định danh (luật T1).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §3

---

## 4. Tiêu chí: cái gì thuộc Core, cái gì thuộc Module

### 4.1 Phép thử

> **Một thứ thuộc Core khi nó có ý nghĩa với MỌI sản phẩm dựng trên nền tảng này.**

| Ví dụ | Thuộc | Vì sao |
| --- | --- | --- |
| Đăng nhập, phiên, đổi mật khẩu | Core | Mọi sản phẩm đều cần |
| Người dùng, vai trò, quyền | Core | Mọi sản phẩm đều cần |
| Menu động theo quyền | Core | Mọi sản phẩm đều cần |
| Thông báo (in-app, email) | Core | Cơ chế chung, nội dung do module cung cấp |
| Lưu file, import/export | Core | Cơ chế chung |
| Quản lý đơn hàng | Module | Chỉ có nghĩa với sản phẩm bán hàng |
| Báo cáo tuần theo tiêu chí X | Module | Nghiệp vụ riêng |

### 4.2 Ngưỡng định lượng — chống Core phình to

> **Code chỉ được vào Core khi từ HAI module trở lên cần nó.**

Một module cần thì để ở module đó. Module thứ hai cần thì mới nâng lên Core — và lúc nâng, phải gỡ sạch mọi dấu vết của module đầu tiên khỏi thiết kế.

**Cơ chế chống:** mọi thay đổi chạm Core — tập đường dẫn ở §10 — phải qua agent `architect` và phải có ADR. Xem [`.claude/agents/architect.md`](../.claude/agents/architect.md).

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §4.2

### 4.3 Khi nào tách một module mới

Tách khi thoả **ít nhất hai** trong ba:

1. Có ranh giới nghiệp vụ rõ — người dùng nói về nó như một thứ riêng
2. Có tập bảng riêng, gần như không join sang bảng của module khác
3. Có thể thay đổi độc lập — sửa nó không buộc phải sửa module khác

Không thoả thì để chung.

---

## 5. Ranh giới dữ liệu

| Luật | Chi tiết |
| --- | --- |
| Mỗi module một **schema PostgreSQL riêng** | Core dùng schema `core`; module dùng schema tên module |
| Mỗi module một `DbContext` riêng | **Không** kế thừa `IdentityDbContext` của Core. Bộ lọc tenant và xoá mềm dùng chung qua extension của Core — [`quy-uoc/be-entity-domain.md`](quy-uoc/be-entity-domain.md) §5.1 |
| Một lần ghi là **một** transaction trên mọi `DbContext` | `IUnitOfWork` điều phối mọi `DbContext` đã đăng ký trên **một** `DbConnection` và **một** transaction — [`quy-uoc/be-cqrs-handler.md`](quy-uoc/be-cqrs-handler.md) §4 · [`adr/0025-luu-du-lieu-module-mot-transaction.md`](adr/0025-luu-du-lieu-module-mot-transaction.md) |
| **Cấm foreign key vật lý xuyên schema** | Tham chiếu bằng ID (ví dụ một cột `Guid` giữ id người tạo), không bằng navigation property |
| Migration độc lập theo module | Core sở hữu migration schema `core`; module sở hữu migration của mình |

📖 Chi tiết: [`database/migration-policy.md`](database/migration-policy.md)

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §5

---

## 6. Frontend — bốn tầng

> 📖 Sơ đồ bốn tầng FE và chiều phụ thuộc: [`quy-uoc/fe-architecture.md`](quy-uoc/fe-architecture.md) §1 — file chủ.

**Chiều phụ thuộc chỉ đi xuống.** `core/` là tầng đáy — nó không được import bất cứ thứ gì từ ba tầng trên.

### 6.1 Ranh giới FE được ép bằng gì

FE **giữ cấu trúc thư mục** trong một app Angular duy nhất; ranh giới do ESLint canh. Luật F3 ở [`RULES.md`](RULES.md) cấm `eslint-disable` cho đúng danh sách rule ranh giới, và cổng FE kiểm điều đó.

📖 [`adr/0007-fe-giu-cau-truc-thu-muc.md`](adr/0007-fe-giu-cau-truc-thu-muc.md)

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §6.1

### 6.2 Ngoại lệ có chủ đích: `Design/` phủ cả Core lẫn nghiệp vụ

[`Design/`](Design/) là nguồn giao diện duy nhất và chứa **cả** màn hình Core lẫn màn hình nghiệp vụ, **cả** component dùng chung lẫn component riêng sản phẩm. Vì vậy **không** áp luật "gỡ nghiệp vụ khỏi Core" cho khu Design: một spec component được phép trích dẫn màn hình nghiệp vụ làm nơi nó xuất hiện.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §6.2

---

## 7. Một module gồm những gì — hợp đồng lắp ghép

Để lắp được vào Host, một module phải cung cấp:

| Thành phần | Vai |
| --- | --- |
| `Modules.<X>.Domain` | Entity, VO, luật nghiệp vụ |
| `Modules.<X>.Application` | Command/Query, handler, validator, DTO |
| `Modules.<X>.Infrastructure` | `DbContext` riêng, EF configuration, **migration schema của mình** |
| `Modules.<X>.Endpoints` | Controller kế thừa `ApiControllerBase` của Core |
| Một nguồn danh mục khoá quyền | Khoá của module qua `IPermissionCatalogSource`, Core kiểm lúc khởi động — [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §1.1 |
| Nguồn seed cho đơn vị mới — khi module có vai trò, ánh xạ quyền hay menu mặc định | `ITenantSeedSource`; Core gộp mọi nguồn và kiểm lúc khởi động — [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §1.1 |
| Một extension `AddXModule()` | Điểm đăng ký duy nhất — Host chỉ gọi đúng dòng này |

**Module KHÔNG được đăng ký lại pipeline behavior** — behavior sống ở `Core.Application`; luật A10 bắt điều này. Handler và validator của module đi vào **cùng một** lời quét của Core — module không tự gọi `AddMediatR` hay tự quét validator; thứ tự đăng ký ở [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §5.1.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §7

---

## 8. Số liệu — đếm bằng lệnh, đừng chép

Theo [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §6, **không chép số lượng project vào tài liệu**. Khi `src/` tồn tại (giai đoạn 2), đếm bằng:

```bash
find src/BE -iname '*.csproj' | wc -l          # project trên đĩa
grep -c '<Project' src/BE/*.slnx                # project thật sự trong solution
```

Hai con số này **có thể khác nhau**; luật A6 ở [`RULES.md`](RULES.md) bắt trường hợp đó.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §8

---

## 9. Bảng chuyển trạng thái

Khi `src/` bắt đầu được xây ở giai đoạn 2, thay bảng này bằng bảng *"có thật hôm nay → sẽ thành"* theo [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4.

| Hôm nay (giai đoạn 1) | Sẽ thành (giai đoạn 2) |
| --- | --- |
| Chưa có `src/` | `src/BE` các project Core ở §2 + host + tests; `src/FE` một app Angular bốn tầng |
| Chưa có cổng BE/FE | `dotnet test` (gồm ArchTests) + `fe-gate.sh` + bộ lệnh `ng` |
| Mọi luật `📐` ở [`RULES.md`](RULES.md) | Chuyển dần sang `✅` khi cổng tương ứng được viết |

---

## 10. Đường dẫn chạm Core — định nghĩa gốc

Khối máy đọc dưới đây là nguồn **duy nhất** của câu hỏi *"thay đổi này có chạm Core không"*. Hook,
agent và skill đọc khối này hoặc trỏ về đây; không nơi nào giữ danh sách thứ hai ([`OWNERSHIP.md`](OWNERSHIP.md) §5).

Luật định dạng: khối nằm giữa hai dòng chú thích HTML đánh dấu mở và đóng, bên trong là **một** khối rào không gắn ngôn ngữ; mỗi dòng một đường dẫn tính từ gốc repo — thư mục kết thúc bằng `/`, tệp ghi đúng tên; dòng trống và dòng bắt đầu bằng `#` bị bỏ qua; không liệt kê tệp bị `.gitignore` loại.

<!-- core-paths:begin -->
```
# Backend — project Core, host, ArchTests
src/BE/Core/
src/BE/CoreAndSkill.Api/
src/BE/Tests/CoreAndSkill.ArchTests/

# Frontend — ba tầng dùng chung và hàng rào ranh giới
src/FE/src/app/core/
src/FE/src/app/shared/
src/FE/src/app/platform/
src/FE/eslint.config.js
src/FE/eslint.boundaries.cjs

# Tài liệu mà code Core phải tuân
docs/quy-uoc/
docs/RULES.md
docs/OWNERSHIP.md
docs/kien-truc-core-module.md
```
<!-- core-paths:end -->

Nguồn của từng nhóm — và là chỗ phải sửa khối này **cùng lượt** khi layout đổi:

| Nhóm | Đọc từ |
| --- | --- |
| Project Core, host | §2 |
| ArchTests | [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §9.1 |
| `core/`, `shared/`, `platform/`, `eslint.config.js` | [`quy-uoc/fe-architecture.md`](quy-uoc/fe-architecture.md) §1–§2, §4 |
| `eslint.boundaries.cjs` | Tệp khai ranh giới mà `eslint.config.js` nạp vào — [`wiki-core/fe/trien-khai/05-gate.md`](wiki-core/fe/trien-khai/05-gate.md) |

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §10
