---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Kiến trúc Core ↔ Module — ranh giới tái sử dụng cho BE và FE

> **File chủ về ranh giới:** cái gì thuộc **Core** (mang đi được sang mọi dự án dựng trên nền tảng này), cái gì thuộc **Module** (nghiệp vụ riêng của một dự án).
>
> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** `src/BE` và `src/FE` đã có trên đĩa, nhưng **chưa ai đối chiếu từng mục của file này với source** — frontmatter vẫn `verified: chua-doi-chieu`. Đọc layout dưới đây là *thứ `src/` phải trở thành*, không phải xác nhận `src/` đã khớp. Cái gì đã có thật tới đâu: §9.

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
| `Core.Web` | `ApiControllerBase`, `ApiEnvelope` (`Core.Web/Http/ApiEnvelope.cs`), middleware, `IExceptionHandler` của Core, `HttpContextCurrentUser`, `ModelBindingProblemFactory`, ánh xạ `Result` → HTTP, ba attribute phân quyền endpoint cùng filter, **cookie scheme, cookie events, `HttpContext.SignInAsync` / `SignOutAsync`**, **runner lệnh vận hành `RunCoreCommandAsync`** (nhận dòng lệnh của host, chạy lệnh rồi thoát, không mở cổng — vai dòng lệnh duy nhất của project này), controller Core, `AddCore()` / `UseCoreAsync()` | Nghiệp vụ của bất kỳ dự án nào; `DbContext` gọi thẳng | `Domain`, `Application`, `Infrastructure` |
| `Core.Contracts` | Integration event, DTO mà **Core phát ra** cho module. Hợp đồng giữa hai module **không** ở đây — §7 | Kiểu của đường vào HTTP (envelope nằm ở `Core.Web`); bất cứ thứ gì phụ thuộc project Core khác — nó phải tự đứng một mình | — (chỉ BCL) |

Host là `CoreAndSkill.Api`; module là `CoreAndSkill.Modules.<X>.*`. Sơ đồ chiều phụ thuộc và
danh sách ArchTest canh từng dòng: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §1.2–§1.3.

Từ phía module có thêm đúng một cạnh bắt buộc: `Modules.<X>.Infrastructure` tham chiếu
`Core.Infrastructure`; chiều ngược lại (`Core.*` → `Modules.*`) vẫn bị cấm.

Thư mục trên đĩa: project Core ở `src/BE/Core/CoreAndSkill.Core.<Tầng>/`, test của Core ở `src/BE/Tests/`
([`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §9.1), host ở `src/BE/CoreAndSkill.Api/`, module ở
`src/BE/Modules/<X>/CoreAndSkill.Modules.<X>.<Tầng>/` kèm test ở `src/BE/Modules/<X>/Tests/`. Vùng sở hữu tệp:
[`quy-uoc/repo-artifact.md`](quy-uoc/repo-artifact.md) §15.5.

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

Host (`Api`) thuộc **vùng dự án** ([`adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md`](adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md)). Nó được chứa gì, cấm chứa gì, và ngưỡng của `Program.cs`: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §3. Luật A7 ở [`RULES.md`](RULES.md) ép điều này.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §2.4

---

## 3. Ba luật ranh giới — ép bằng test, không bằng niềm tin

| # | Luật | Ép bằng |
| --- | --- | --- |
| **R1** | Core **không bao giờ** tham chiếu Module | Không có ProjectReference + ArchTest A3 |
| **R2** | Module A **không bao giờ** tham chiếu project của Module B, trừ `Modules.<B>.Contracts` (§7); qua đó chỉ **đọc**, ghi sang B chỉ bằng sự kiện | ArchTest A4 📐 **chưa thi công** — trạng thái và tên ArchTest thật: [`RULES.md`](RULES.md) §3; vế ghi: nợ A21 |
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
| `Modules.<X>.Contracts` — **chỉ khi** module khác cần | Sự kiện, DTO X công bố; chỉ BCL; project duy nhất của X mà module khác được tham chiếu. Đọc đồng bộ qua interface ở đây; ghi chỉ qua sự kiện outbox — [`adr/0103-giua-module-doc-dong-bo-qua-contracts-ghi-qua-su-kien.md`](adr/0103-giua-module-doc-dong-bo-qua-contracts-ghi-qua-su-kien.md) |
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

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (lật 2026-09-16). Tám điều kiện chuyển giai đoạn ở
> [`adr/0030-dieu-kien-chuyen-giai-doan-2.md`](adr/0030-dieu-kien-chuyen-giai-doan-2.md) đã đạt —
> điều kiện 3 hoãn hợp lệ theo
> [`adr/0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md`](adr/0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md).
> `src/` **đã** tồn tại trên đĩa (đối chiếu 2026-09-19) nhưng **chưa vào git**; bảng dưới là
> *"có thật hôm nay → sẽ thành"* theo [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4, không phải
> xác nhận từng mục của file này đã khớp source — việc đó làm riêng từng mục.

| Có thật hôm nay | Sẽ thành |
| --- | --- |
| `src/BE` có trên đĩa: project Core ở §2, host `CoreAndSkill.Api`, các project Tests — đều khai ở `src/BE/CoreAndSkill.slnx`. `src/FE` có `core/`, `platform/`, `shared/` | `src/FE` thêm `modules/` khi module đầu tiên về |
| `src/` chưa vào git, nên hai job cổng BE/FE của CI **chưa chạy lần nào** | Vào git ở lần commit đầu → job tự bật theo điều kiện có `src/BE` / `src/FE` |
| Cổng BE chạy được tại chỗ: `dotnet test` trên `src/BE/Tests/CoreAndSkill.ArchTests` xanh (2026-09-19). `scripts/fe-gate.sh` tồn tại — lượt này **không chạy** nó, nên không khai kết quả | `dotnet test` toàn solution + `fe-gate.sh` + bộ lệnh `ng` chạy trong CI mỗi PR |
| Mọi luật `📐` ở [`RULES.md`](RULES.md) | Chuyển dần sang `✅` khi cổng tương ứng được viết — từng luật, không hàng loạt |
| Lớp 3 chặn Core (hook `Stop`) chưa thử bằng payload thật | Chứng minh sống ở PR đầu tiên chạm một đường dẫn `core-paths` ([`adr/0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md`](adr/0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md)) |

---

## 10. Đường dẫn chạm Core — định nghĩa gốc

Khối máy đọc dưới đây là nguồn **duy nhất** của câu hỏi *"thay đổi này có chạm Core không"*. Hook,
agent và skill đọc khối này hoặc trỏ về đây; không nơi nào giữ danh sách thứ hai ([`OWNERSHIP.md`](OWNERSHIP.md) §5).

Khối **không** trả lời *"dự án được sửa tệp nào"* — câu đó ở [`quy-uoc/repo-artifact.md`](quy-uoc/repo-artifact.md) §15.5. Đường dẫn trong khối thuộc vùng Core; chiều ngược lại không đúng.

Luật định dạng: khối nằm giữa hai dòng chú thích HTML đánh dấu mở và đóng, bên trong là **một** khối rào không gắn ngôn ngữ; mỗi dòng một đường dẫn tính từ gốc repo — thư mục kết thúc bằng `/`, tệp ghi đúng tên; dòng trống và dòng bắt đầu bằng `#` bị bỏ qua; không liệt kê tệp bị `.gitignore` loại.

<!-- core-paths:begin -->
```
# Backend — project Core, ArchTests. Host cố ý nằm ngoài khối — ADR-0100
src/BE/Core/
src/BE/Tests/CoreAndSkill.ArchTests/

# Ngưỡng của cổng test BE — nới một ngưỡng là tắt một luật (T3, T4)
src/BE/Tests/CoreAndSkill.Core.UnitTests/CoreAndSkill.Core.UnitTests.csproj
src/BE/Directory.Build.props

# Frontend — ba tầng dùng chung và hàng rào ranh giới
src/FE/src/app/core/
src/FE/src/app/shared/
src/FE/src/app/platform/
src/FE/eslint.config.js
src/FE/eslint.boundaries.cjs
src/FE/public/i18n/
src/FE/src/index.html

# Cổng FE và canary của chính nó — nới một mẫu dò là tắt một luật Core
scripts/fe-gate.sh
scripts/tests/

# Script DDL của schema core — đổi tệp ở đây là đổi tập khoá quyền của Core
database/scripts/core/

# Tệp chứa chính khối này — thu hẹp khối là tắt hàng rào, nên sửa tệp này cũng đòi review
docs/kien-truc-core-module.md
```
<!-- core-paths:end -->

Nguồn của từng nhóm — và là chỗ phải sửa khối này **cùng lượt** khi layout đổi:

| Nhóm | Đọc từ |
| --- | --- |
| Project Core | §2 |
| ArchTests | [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §9.1 |
| `CoreAndSkill.Core.UnitTests.csproj`, `Directory.Build.props` | Ngưỡng của T3, T4 — [`RULES.md`](RULES.md) §8 |
| `core/`, `shared/`, `platform/`, `eslint.config.js` | [`quy-uoc/fe-architecture.md`](quy-uoc/fe-architecture.md) §1–§2, §4 |
| `eslint.boundaries.cjs` | Tệp khai ranh giới mà `eslint.config.js` nạp vào — [`wiki-core/fe/trien-khai/05-gate.md`](wiki-core/fe/trien-khai/05-gate.md) |
| `database/scripts/core/` | [`database/script-runbook.md`](database/script-runbook.md) §1–§2 — khuôn tên và thứ tự script của schema `core` |
| `src/FE/public/i18n/` | Bảng dịch **tầng Core**; dự án ghi đè ở `public/i18n-app/` — [`quy-uoc/fe-architecture.md`](quy-uoc/fe-architecture.md) §1.1 |
| `src/FE/src/index.html` | Trang chủ của bundle: script áp theme trước khung hình đầu — [`quy-uoc/fe-ui-conventions.md`](quy-uoc/fe-ui-conventions.md) §3.5 |
| `scripts/fe-gate.sh`, `scripts/tests/` | Cổng script FE và canary của nó — [`wiki-core/fe/trien-khai/05-gate.md`](wiki-core/fe/trien-khai/05-gate.md) §3.1, §5 |

Khối là **định nghĩa**, không phải bản kê những đường sửa đang thật sự bị canh. Hook `PostToolUse` là bên **tiêu thụ**: nó có thể phủ hẹp hơn khối, và nơi chứng minh nó phủ tới đâu là bộ test hook [`run-tests.sh`](../.claude/hooks/tests/run-tests.sh) — không phải file này. Cổng [`check-docs.sh`](../.claude/check-docs.sh) §26 chỉ canh việc khối còn **đọc được**. Nên đừng đọc sự có mặt của một đường dẫn ở đây như lời khai rằng mọi công cụ sửa nó đều đang bị canh: muốn biết thì chạy bộ test hook.

> 📖 Lý do, bẫy, ví dụ mở rộng: [`kien-truc-core-module.md`](wiki-core/be/ly-do/kien-truc-core-module.md) §10
