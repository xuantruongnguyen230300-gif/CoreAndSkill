---
kind: lich-su
scope: du-an
verified: chua-doi-chieu
---

# Lộ trình & nguồn tài liệu cho CoreAndSkill

> # 🗄️ TÀI LIỆU LỊCH SỬ
>
> **File này mô tả trạng thái QUÁ KHỨ. Đường dẫn, con số và nhận định trong đây cố ý không còn đúng — đừng đọc nó để biết hiện trạng, và đừng trích dẫn nó làm căn cứ.**
>
> | Trong file này | Thực tế hiện nay |
> | --- | --- |
> | Trạng thái các hạng mục | `docs/RULES.md` §10 cho nợ, `docs/adr/` cho quyết định |
> | Nhận định về CI | CoreAndSkill **có** CI — xem `docs/adr/0011-ci-github-actions.md` |
> | Mô hình phục vụ FE↔API | **Hai origin khác nhau** — [`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md) |
> | Lộ trình E0–E6 ở §3, kể cả ghi chú cuối §6 rằng phần còn hiệu lực là *E4 trở đi* | **Không còn hiệu lực** — lộ trình thi công: [`../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md) · [`../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) |
>
> Nguồn sống: [`../README.md`](../README.md) · [`../RULES.md`](../RULES.md) · [`../adr/README.md`](../adr/README.md)


> **Trạng thái:** Draft v1 · **Ngày:** 2026-09-08
> Tài liệu này **thay thế §6 (Lộ trình) và §10 (Câu hỏi còn mở)** của [`development-plan.md`](development-plan.md),
> sau khi khảo sát repo `D:\Manager\PlatformManager` ngày 2026-09-08.

---

## 0. Phát hiện quyết định — CoreAndSkill không phải dự án xây mới

Kế hoạch bản đầu giả định repo trống, phải xây Core từ số 0: **14.5 tuần-người**.

Giả định đó **sai**. `PlatformManager` đã có một Core chạy thật, đã qua nhiều vòng review, và — quan trọng nhất — **đã tự đánh dấu sẵn phần nào thuộc Core**.

Khoá `scope:` trong frontmatter của mọi file `doc/` được sinh ra để trả lời đúng một câu hỏi:

> *"File này có đi theo khi tách CoreBase sang dự án 2 không?"*

Đếm ngày 2026-09-08 (`find doc spec -name '*.md'` + đọc frontmatter):

| `scope` | Số file | Ý nghĩa |
|---|---|---|
| `core` | **79** | Đi theo CoreAndSkill |
| `du-an` | 61 | Ở lại PlatformManager |

| `kind` | Số file | Ý nghĩa |
|---|---|---|
| `luat` | 126 | Code phải tuân |
| `tham-chieu` | 9 | Tham khảo hình dạng, **không** đối chiếu |
| `lich-su` | 5 | Đã chết, giữ để tra cứu |

**70 file vừa `scope: core` vừa `kind: luat`** — đó chính là bản kê khai (manifest) di trú, đã có sẵn, không phải đoán.

**Hệ quả:** công việc của CoreAndSkill là **trích xuất + làm sạch + bổ khuyết**, không phải xây mới. Ước lượng sửa lại: **~8 tuần-người** (chi tiết §3).

> Đây là lý do phải đọc repo cũ trước khi lập kế hoạch. Bản kế hoạch 14.5 tuần đã định làm lại từ đầu khoảng 60% thứ đang chạy tốt.

---

## 1. Đính chính thông số kỹ thuật

Thông số chốt ở vòng hỏi đầu **lệch với thực tế** đang chạy trong PlatformManager. Lấy theo thực tế:

| Hạng mục | Đã nói ở vòng đầu | Thực tế PlatformManager | Chốt cho CoreAndSkill | Bằng chứng |
|---|---|---|---|---|
| .NET | 9 | **net10.0** | **.NET 10** (LTS, 11/2025) | `src/BE/Directory.Build.props` |
| Angular | 18+ | **20.3** | **Angular 20.3** | `src/FE/package.json` |
| Thư viện UI | chưa chốt | **PrimeNG 20.2** + `@primeng/themes` + `primelocale` | **PrimeNG** | `src/FE/package.json` |
| i18n | chưa chốt | **@ngx-translate 18** đang chạy | **@ngx-translate** | `src/FE/package.json`, `public/i18n/` |
| Auth | tôi đề xuất JWT | **Cookie session + Antiforgery 2 lớp**, ghi rõ *"đã CHỐT — KHÔNG JWT"* | **giữ Cookie** (§4.5) | `src/BE/PlatformManager.Api/Program.cs:382` |
| DB | PostgreSQL | PostgreSQL (Npgsql) | PostgreSQL | `Core.Infrastructure/DependencyInjection.cs` |
| Redis / MQ | có trong kế hoạch | **chưa có** | §4.2 — hoãn | không tìm thấy trong `src/BE` |
| Test kiến trúc | đề xuất thêm | **đã có** `PlatformManager.ArchTests` | copy nguyên | `src/BE/Tests/PlatformManager.ArchTests/` |
| CI | đề xuất | **cố ý không có** | **phải có** (§4.7) | `.claude/CLAUDE.md` §8 |

> **.NET 9 đã hết vòng đời hỗ trợ** (STS, 18 tháng kể từ 11/2024). .NET 10 là bản LTS hiện hành. Không có lý do chọn 9.

---

## 2. Nguồn cho từng khu — lấy ở đâu, làm gì với nó

### 2.1 Backend Core

| Thành phần | Nguồn trong PlatformManager | Xử lý |
|---|---|---|
| `Core.Domain` | `src/BE/Core/PlatformManager.Core.Domain/` (`Common/`, `Entities/`) | Copy → đổi namespace → **gỡ mọi tên sản phẩm** |
| `Core.Application` | `.Core.Application/` — `Auth/`, `Users/`, `Permissions/`, `Menu/`, `Notifications/`, `Bootstrap/`, `Common/` | Copy. Đây đã bao gồm **authen + user + message** như yêu cầu |
| `Core.Infrastructure` | `.Core.Infrastructure/` — `Identity/`, `Permissions/`, `Notifications/`, `BackgroundJobs/`, `Persistence/` | Copy |
| Host mỏng | `src/BE/PlatformManager.Api/Program.cs` | Copy, tách phần cấu hình thành extension method |
| **ArchTests** | `src/BE/Tests/PlatformManager.ArchTests/` — `CoreModuleBoundaryTests`, `CoreMustNotKnowBusinessNameTests`, `BannedDependencyTests`, `EntityEncapsulationTests`, `ErrorCatalogTests`, `EnvelopeBusinessCodeTests`, `InterceptorWiringTests`, … | **Copy trước tiên.** Đây là hàng rào, phải có trước dòng code đầu |
| Build config | `src/BE/Directory.Build.props` | Copy + **bổ sung**: hiện chỉ có `WarningsAsErrors=Nullable`, cần mở rộng |
| Central Package Management | **KHÔNG CÓ** | **Viết mới** `Directory.Packages.props` |

**Không mang sang:** `src/BE/Modules/DtiWeekly/` — module nghiệp vụ, đã bị gỡ khỏi solution ngày 2026-08-29.

### 2.2 Frontend Core

| Thành phần | Nguồn | Xử lý |
|---|---|---|
| Cấu hình nền | `src/FE/package.json`, `angular.json`, `eslint.config.js`, `tsconfig*.json`, `.editorconfig` | Copy. `package.json` có phần `"//dependencies"` giải thích vì sao giữ `@angular/animations` — **giữ nguyên chú thích đó** |
| Core FE (interceptor, guard, envelope, auth) | `src/FE/src/app/` phần core | Copy, tách thành lib `@app/core` |
| Design token | `doc/Design/Frontend/PlatformManager/DESIGN.md`, `Icons.md`, `COMPONENTS.md` | Copy, đổi tên project |
| **28 spec component** | `doc/Design/Frontend/PlatformManager/Components/*.md` | Phân loại: component Core (`Button`, `Input`, `DataTable`, `Dialog`, `Toast`, `Sidebar`, `Topbar`, …) giữ; component nghiệp vụ (`KpiTile`, `TrendChart`, `DeltaIndicator`, `HistoryRow`, `ProgressBar`) để lại |
| Gate FE | `scripts/fe-gate.sh` (152 dòng) + `doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md` | Copy nguyên |
| Lộ trình FE F0→F3 | `doc/huong_dan/wiki-core/fe/trien-khai/00..05` | Copy — đây là lộ trình đã chạy thật một lần |

### 2.3 Database

| Thành phần | Nguồn | Xử lý |
|---|---|---|
| Schema Core | `doc/cau-truc-database.md` (`scope: core`) | Copy |
| Script khởi tạo | `doc/db-khoi-tao.sql` | Copy, lọc bảng nghiệp vụ |
| Seed quyền | `scripts/seed-role-permissions.sql` | Copy |
| Chính sách migration | `doc/huong_dan/wiki-core/be/13-core-data-migration.md` | Copy |
| Schema nghiệp vụ | `doc/cau-truc-database-business.md` (`scope: du-an`) | **Không mang** |

> Lưu ý một quyết định đã chốt ở PlatformManager: **không auto-migrate**, chỉ sinh file script để người vận hành tự chạy trên Postgres. Giữ nguyên chính sách này.

### 2.4 Tài liệu (`docs/`)

**Nguồn chính: 70 file `scope: core` + `kind: luat`.** Nhóm theo khu:

| Nhóm | Số file | Đường dẫn nguồn |
|---|---|---|
| Quy ước thi hành | 12 | `doc/huong_dan/quy-uoc/` — `be-{architecture,api-controller,cqrs-handler,entity-domain,performance}.md`, `fe-{architecture,api-client,routing-guard,ui-conventions}.md`, `repo-artifact.md`, `tieu-chi-review.md`, `README.md` |
| Wiki Core BE | 17 | `doc/huong_dan/wiki-core/be/01..16` + `tra-cuu-file-class.md` |
| Wiki Core FE | 18 | `doc/huong_dan/wiki-core/fe/01..17` + `README` |
| Lộ trình FE | 6 | `doc/huong_dan/wiki-core/fe/trien-khai/00..05` |
| Hợp đồng API Core | 4 | `doc/contracts/{auth,users,permissions,meta-menu}.md` |
| Template Design | 10 | `doc/Design/Templates/*.md` |
| Kiến trúc & mục lục | 3 | `doc/kien-truc-core-module.md`, `doc/README.md`, `doc/cau-truc-database.md` |

**Nguồn tham chiếu (giữ, không đối chiếu code):** `doc/tham-khao-ngoai/vnr-successor/00..07` — lộ trình xây dựng của một dự án khác, `kind: tham-chieu`. Đã có tiền lệ agent nhầm đây là luật và báo finding sai (2026-09-01).

**Không mang sang:** `doc/contracts/{danh-muc-dti,dashboard}.md`, `doc/cau-truc-database-business.md`, toàn bộ `spec/`, `doc/Design/.../Screens/` phần nghiệp vụ, `doc/ke-hoach-xay-lai-corebase.md` (`kind: lich-su`).

### 2.5 `.claude` — agent, skill, gate

| Thành phần | Nguồn | Xử lý |
|---|---|---|
| 4 agent | `.claude/agents/{backend-expert,frontend-expert,core-reviewer,design-expert}.md` | Copy, gỡ tên sản phẩm khỏi mô tả và bảng định tuyến |
| 9 skill Design | `.claude/skills/design-*` | Copy nguyên — thuần Core |
| 3 skill expert | `.claude/skills/{backend-expert,frontend-expert,core-reviewer}` | Copy |
| Điều phối | `.claude/skills/feature-kickoff` | Copy — điểm vào duy nhất cho 1 feature |
| **Gate tài liệu** | `.claude/check-docs.sh` (529 dòng, 12 mục) | Copy, sửa mục §1 (bảng git) và §7 (`doc/Prototype/` — đặc thù repo cũ) |
| Luật repo | `.claude/CLAUDE.md` (9 mục) | Copy — **đây là tài sản giá trị nhất**, xem §5 |
| Quyền harness | `.claude/settings.json` (`permissions.deny` chặn lệnh git ghi, `dotnet ef database update/drop`, `npm publish`, `docker push`) | Copy |

**Chưa có, phải viết mới:** `/core-new-module`, `/core-new-usecase`, `/core-new-entity`, `/fe-new-feature`, `/arch-check`, `/doc-sync`.

---

## 3. Lộ trình sửa lại — trích xuất, không xây mới

| Phase | Nội dung | Effort | DoD |
|---|---|---|---|
| **E0** | **Di trú tài liệu.** Copy 70 file `scope: core`; đổi mọi tham chiếu `PlatformManager` → tên trung tính; port `check-docs.sh` + `CLAUDE.md` | 1 tuần | `bash .claude/check-docs.sh` **PASS** trên repo mới. Số file `verified: chua-doi-chieu` được ghi lại làm mốc |
| **E1** | **Di trú BE Core.** ArchTests **trước**, rồi `Core.{Domain,Application,Infrastructure}`, host mỏng. Đổi namespace. Viết `Directory.Packages.props` | 1.5 tuần | `dotnet test` xanh, ArchTests xanh và **đỏ thật** khi cố tình vi phạm |
| **E2** | **Di trú FE Core.** Workspace Angular 20 + PrimeNG 20, `@app/core`, `@app/ui`, token, i18n. Port `fe-gate.sh` | 1.5 tuần | `bash scripts/fe-gate.sh` + `ng lint` + `ng build` + `ng test` đều xanh |
| **E3** | **Bổ khuyết hạ tầng.** CI (chạy cả 3 cổng), `Directory.Packages.props`, Redis cache, Outbox + hosted service | 1.5 tuần | PR không qua cổng thì không merge được. Cổng chạy trên máy CI, không phải trên máy người |
| **E4** | **Dựng 1 module nghiệp vụ mẫu.** PlatformManager hiện **không còn module nào** — phải có khuôn thật trước khi viết skill scaffold | 1.5 tuần | Module mẫu build pass, test pass, không vi phạm ArchTest R1/R2/R3 |
| **E5** | **Viết skill scaffold** dựa trên khuôn E4: `/core-new-module`, `/core-new-usecase`, `/core-new-entity`, `/fe-new-feature`, `/arch-check`, `/doc-sync` | 1 tuần | Chạy `/core-new-module X` → build & test pass **không sửa tay dòng nào** |
| **E6** | **Kiểm chứng.** Dựng một dự án mới từ khung, đo thời gian | 1 tuần | Dev mới: clone → chạy được < 15 phút; use case mới < 30 phút |

**Tổng: ~9 tuần-người.** Với 2 người tách BE/FE từ E1: **~6 tuần.**

### 3.1 Đường găng

```
E0 ──┬── E1 ──┬── E3 ── E4 ── E5 ── E6
     └── E2 ──┘
```

E0 chặn tất cả: agent và gate đều đọc `docs/`, chưa có docs thì chưa có gì kiểm được.

### 3.2 Vì sao E4 (module mẫu) phải trước E5 (skill scaffold)

Đây là điểm gây khó hiểu ở bản kế hoạch trước, nên nói thẳng bằng bằng chứng có thật:

**Skill `/core-new-module` về bản chất là một cái máy photocopy có sửa tên.** Nó cần một bản gốc.

Ngày 2026-08-29, module nghiệp vụ duy nhất của PlatformManager (`Modules.DtiWeekly.*`) **đã bị gỡ khỏi solution**. Nghĩa là hôm nay repo đó **không còn khuôn module nào**. Nếu viết skill scaffold bây giờ, người viết phải tự tưởng tượng ra:

- Module cần đúng mấy project, tên gì
- Đăng ký vào Host ở đâu, thứ tự nào
- `DbContext` của module kế thừa gì, schema khai ở đâu
- Endpoint nối vào envelope `IApiResult<T>` ra sao
- ArchTest nào sẽ bắt lỗi nếu làm sai

Năm câu hỏi đó chỉ có đáp án **sau khi một module thật chạy được**. Viết trước là viết theo trí nhớ về một thứ chưa tồn tại — và sản phẩm sẽ là skill sinh ra code không build được, mà không có "đáp án đúng" nào để đối chiếu khi đi sửa.

Có tiền lệ đúng khuôn này trong repo: `backend-expert.md` từng chứa một cây thư mục 10 project **sai**, và `.claude/CLAUDE.md` §8 ghi nhận nó *"lọt qua mọi luật cho tới khi có người đọc"* — vì sơ đồ trong khối ``` không bị gate nào kiểm.

Sau E4, skill chỉ còn việc: *"sao chép cấu trúc module mẫu, đổi tên, bỏ phần nghiệp vụ"* — và có ngay cách nghiệm thu: chạy skill → `dotnet test` → so với module mẫu.

**Ngoại lệ quan trọng:** luật này chỉ áp cho skill **sinh code**. Skill **đọc và kiểm** — `/arch-check`, `/doc-sync`, `/review-pr` — không cần khuôn, viết được từ E0. Bản kế hoạch trước gộp chung cả 7 skill vào một phase là sai; nên tách.

---

## 4. Các quyết định cần chốt — phân tích được/mất

### 4.1 Thư viện UI: PrimeNG hay Angular Material

**Đã quyết trên thực tế.** PrimeNG 20.2 + `@primeng/themes` + `primelocale` đang chạy, và `doc/Design/` có 28 spec component viết theo nó.

| | Giữ PrimeNG | Đổi sang Material |
|---|---|---|
| Chi phí | 0 | ~3–4 tuần: viết lại 28 spec, dựng lại token, chụp lại toàn bộ ảnh màn hình |
| Được thêm | — | Gần như không: cả hai đều đủ dùng cho app quản trị |
| Rủi ro | Breaking change giữa các major PrimeNG khá nặng (đặc biệt phần theming) | Material ổn định hơn nhưng ít component sẵn hơn — bảng, tree, filter phải tự dựng |

**Khuyến nghị: PrimeNG.** Không cần bàn thêm.

**Nhưng có một việc phải làm kèm:** rủi ro breaking change của PrimeNG là thật, nên **component nghiệp vụ không được import `primeng/*` trực tiếp** — phải đi qua `@app/ui`. Cần kiểm hiện trạng bằng lệnh trước khi di trú:

```bash
grep -rn "from 'primeng/" src/FE/src/app --include="*.ts" | grep -v "/ui/"
```

Kết quả rỗng → đã bọc đúng. Không rỗng → thêm việc bọc vào E2, và thêm một mục vào `fe-gate.sh` để nó không tái diễn.

### 4.2 Message broker: RabbitMQ hay Kafka

**Câu hỏi đặt sai.** Câu đúng là: *có cần broker ở v1 không?*

PlatformManager chạy được đến giờ **không có broker nào** — dùng `BackgroundJobs` trong `Core.Infrastructure`. Đó là bằng chứng thực nghiệm rằng nhu cầu chưa tới.

Chi phí thật của việc thêm một broker không phải là code, mà là **vận hành**: thêm một dịch vụ phải cài, giám sát, sao lưu, xử lý poison message, dead-letter queue, và một loại lỗi mới (message trùng, message lệch thứ tự) mà đội chưa từng gặp.

**Khuyến nghị: không thêm broker ở v1.**

Thay vào đó: định nghĩa `IMessageBus` trong `Core.Application` và implement `OutboxMessageBus` — ghi vào bảng outbox cùng transaction, một hosted service đọc và phát. Đúng ngữ nghĩa "không mất tin", chạy trên Postgres đã có.

Khi thật sự có consumer nằm ngoài process, thay implementation — **call site không đổi một dòng**.

Nếu buộc phải chọn ngay: **RabbitMQ**. Kafka chỉ đáng khi cần replay log lịch sử, throughput trên ~10k msg/s, hoặc nhiều consumer group độc lập đọc cùng luồng. Hệ tầm trung không chạm ngưỡng nào trong ba.

### 4.3 Multi-tenant

**Đây là câu duy nhất tôi không tự trả lời được** — nó phụ thuộc kế hoạch kinh doanh, không phụ thuộc code.

| | Làm ở E1 | Thêm sau |
|---|---|---|
| Chi phí | 3–5 ngày: cột `TenantId`, EF global query filter, `ICurrentUser.TenantId`, ArchTest ép mọi entity khai `ITenantScoped` | 3–4 tuần |
| Rủi ro | Phức tạp thừa nếu không bao giờ dùng | **Rò dữ liệu giữa tenant** — chỉ cần sót một query không lọc |

**Cách rẻ nhất, nên làm dù trả lời gì:** khai sẵn `ICurrentUser.TenantId` (v1 luôn `null`) và interface rỗng `ITenantScoped`. Chi phí ~2 giờ, và biến việc "thêm sau" từ *sửa 200 file* thành *thêm một query filter*.

**Chỉ implement đầy đủ nếu câu trả lời là "chắc chắn có trong 12 tháng".**

### 4.4 Đa ngôn ngữ

**Đã quyết.** `@ngx-translate 18` đang chạy, `public/i18n/` có sẵn, và có hai tài liệu chủ: `wiki-core/fe/08-i18n.md`, `wiki-core/be/16-i18n-va-ma-loi.md`. Mã lỗi BE đã tách khỏi thông điệp hiển thị — đó là phần khó, đã làm xong.

Không cần bàn.

### 4.5 Auth: Cookie session hay JWT ← *quyết định mới, quan trọng nhất*

Bản kế hoạch trước đề xuất **JWT + refresh rotation**. Đó là **mâu thuẫn với quyết định đã chốt** của PlatformManager: `Program.cs:382` ghi rõ *"Cookie session (đã CHỐT — KHÔNG JWT)"*.

| | Cookie session (hiện tại) | JWT |
|---|---|---|
| Chống XSS trộm token | **Tốt hơn** — `HttpOnly`, JS không đọc được | Kém hơn nếu lưu `localStorage` |
| Thu hồi tức thì | **Có sẵn** — server giữ session | Phải tự dựng blacklist Redis |
| Refresh rotation | Không phải viết | Phải tự viết, dễ sai |
| CSRF | **Phải xử lý** — đã làm: antiforgery 2 lớp, 2 cookie riêng | Không cần |
| Cross-origin | `SameSite=None` + `Secure` bắt buộc (đã cấu hình) | Đơn giản hơn |
| Client di động / bên thứ ba | **Kém** | **Tốt** |
| Scale nhiều instance | Cần shared session store (Redis) | Stateless |

Repo đã trả giá học phí cho lựa chọn cookie: `Program.cs:431` ghi lại một lỗi cấu hình antiforgery do `core-reviewer` phát hiện ngày 2026-08-24 (đặt trùng tên cookie), và `:421` ghi lại một chú thích **sai** về lớp phòng thủ, sửa 2026-08-31.

**Khuyến nghị: giữ Cookie session.** Lý do: code đã chạy, đã qua review thật, phần khó nhất (CSRF cho cross-origin SPA) đã xong. Đổi sang JWT là vứt bỏ toàn bộ vốn đó để đổi lấy một khả năng chưa ai cần.

**Đổi ý khi và chỉ khi:** trong 12 tháng có ứng dụng di động native, hoặc có bên thứ ba gọi API. Nếu vậy, nói ngay ở E1 — thêm JWT song song với cookie sau này tốn gấp 3 lần.

### 4.6 Quan hệ giữa CoreAndSkill và PlatformManager — ĐÃ CHỐT: cắt đứt

> **Quyết định (2026-09-08): phương án A — cắt đứt, chỉ tham khảo.**
>
> Lý do của người dùng: PlatformManager là **một bản Core cũ chứa nhiều lỗi**. Nó là nguồn để học phần tốt và học cả những chỗ đã trả giá, không phải nơi Core này quay về hợp nhất.

Sau khi trích xuất, hai repo cùng chứa một bản Core. Bugfix ở bên này có về bên kia không? Ba phương án đã cân nhắc:

| Phương án | Cách làm | Được | Mất |
|---|---|---|---|
| **A. Cắt đứt** ← **đã chọn** | PlatformManager giữ bản của nó, CoreAndSkill đi tiếp độc lập | Không ràng buộc; không phải kéo theo các quyết định cũ mà chính CoreAndSkill đã cố ý lật | Hai bản phân kỳ; lỗi phát hiện bên kia không tự về đây |
| B. PlatformManager tái nhập | Sau khi Core mới xong, PlatformManager thay Core của nó | Một nguồn duy nhất; được kiểm chứng bằng dự án thật | Tốn 1–2 tuần chuyển đổi, rủi ro hồi quy — và CoreAndSkill đã lật sáu quyết định của nó (Core.Web, Result thuần, bỏ role cứng, Transaction behavior, Core sở hữu migration, multi-tenant), nên đây không phải nâng cấp mà là viết lại |
| C. Đóng gói package | Core publish thành package, cả hai cùng dùng | Nâng cấp tập trung | Cần private feed và kỷ luật semver — **đã bị loại ở vòng chốt đầu** |

**Hệ quả phải chấp nhận, nói thẳng:**

1. **Mọi lỗi Core phát hiện ở PlatformManager sẽ không tự về đây**, và ngược lại. Không có cơ chế đồng bộ nào — có nghĩa là không ai phải nhớ đồng bộ, nhưng cũng có nghĩa là không ai được lợi từ việc bên kia sửa.
2. **CoreAndSkill mất phép thử "dự án thật" rẻ nhất.** §6.2 của bản kế hoạch trước vẫn đúng: *bộ khung chưa từng chạy dự án thật là bộ khung chưa được kiểm chứng*. Cắt đứt nghĩa là phép thử đó phải đến từ dự án **mới** đầu tiên dựng trên Core này — và tới lúc đó, mọi thiết kế trong `docs/` vẫn ở trạng thái chưa ai xác nhận bằng code chạy.
3. Vì vậy: **dự án thật đầu tiên nên bắt đầu sớm**, đừng đợi tài liệu hoàn hảo. Nó là thứ duy nhất biến `verified: chua-doi-chieu` thành một ngày đối chiếu thật.

**PlatformManager từ nay được đối xử thế nào:** là **nguồn tham khảo lịch sử**. Được trích dẫn để kể lại một bài học đã trả giá (xem [`../audit/`](../audit/)), **không** được dùng làm căn cứ cho một quyết định mới, và **không** được đối chiếu code của Core này với nó.

### 4.7 CI ← *khuyến nghị đổi so với PlatformManager*

`.claude/CLAUDE.md` §8 ghi: *"Repo không có CI (`.github/` không tồn tại, có chủ đích) — không còn máy nào chạy hộ."* Và ngay sau đó ghi nhận hậu quả: ngày 2026-08-23 phát hiện `scripts/fe-gate.sh` **không tồn tại**, nên 3 trong 9 cổng FE đã không chạy suốt một thời gian dài, trong khi tài liệu vẫn ghi bình thường.

Với một repo cá nhân, không CI là lựa chọn chấp nhận được. Với một **bộ khung nhiều dự án cùng dùng**, nó là lỗ hổng lớn nhất: cổng chạy bằng tay là cổng có thể bị bỏ, và bỏ cổng **không làm hỏng ngay — nó làm hỏng im lặng**.

**Khuyến nghị: CoreAndSkill phải có CI** chạy đủ 3 cổng (`check-docs.sh`, `fe-gate.sh` + bộ lệnh `ng`, `dotnet test`) trên mỗi PR. Đưa vào E3.

---

## 5. Cơ chế ép tuân thủ — trả lời "làm sao agent theo docs 100%"

### 5.1 Bốn tầng, xếp theo độ tin cậy giảm dần

| Tầng | Cơ chế | Bắt được gì | Có thể bị bỏ qua không |
|---|---|---|---|
| **1. Harness chặn** | `settings.json` → `permissions.deny` | Hành động cấm tuyệt đối (lệnh git ghi, `dotnet ef database drop`, `npm publish`) | **Không** — agent không gọi được |
| **2. Cổng tất định** | `check-docs.sh` (12 mục), `fe-gate.sh`, ArchTests, analyzer | Vi phạm **kiểm được bằng máy**: layering, đường dẫn chết, trích dẫn `file:dòng` sai, thiếu nhãn ngày | **Không**, *nếu* CI chạy. Có, nếu chạy tay |
| **3. Agent kiểm agent** | `core-reviewer` — subagent riêng, **không có quyền `Edit`**, đối chiếu `src/` với `doc/` | Vi phạm **cần đọc hiểu**: văn xuôi tả thứ không tồn tại, sơ đồ chép sai, ngày đúng nhưng nội dung sai | **Có** — phụ thuộc agent nhớ gọi |
| **4. Prompt** | `CLAUDE.md`, bảng định tuyến trong agent | Định hướng chung | **Có** |

Đây chính là câu trả lời cho *"có agent kiểm tra sau khi agent trước thực thi không"* — **có, và bạn đã xây rồi**: `core-reviewer`. Ba đặc điểm khiến nó hoạt động thật, đáng giữ nguyên:

1. **Không có quyền `Edit`.** Người kiểm không được sửa thứ mình kiểm.
2. **Context riêng.** Nó tự đọc code, không nhận tóm tắt từ agent vừa viết — nếu nhận tóm tắt, nó chỉ xác nhận lại thiên kiến của agent kia.
3. **Đọc theo bảng định tuyến, không đọc cả wiki.** Corpus đầy đủ từng lên **780 KB** và giết 3 lượt review liên tiếp; trỏ đường thay vì chép giữ ở **~264 KB**.

### 5.2 Ba chỗ nên cải tiến

**(a) `core-reviewer` được gọi "PROACTIVELY" — tức phụ thuộc việc LLM nhớ gọi.** Đó lại là niềm tin, đúng thứ tầng 3 sinh ra để thay thế.

Cách chắc hơn: **Stop hook** trong `settings.json` tự chạy cổng khi agent kết thúc lượt. Hook do harness chạy, không do model quyết định — nó không "quên".

**(b) Chuyển rule từ ArchTest sang Roslyn Analyzer** cho những luật hay bị vi phạm nhất. ArchTest bắt lỗi khi chạy `dotnet test` — tức sau khi đã viết xong. Analyzer bắt **lúc gõ**, gạch đỏ ngay trong IDE. Vòng phản hồi ngắn hơn hàng chục lần, và áp cho cả người lẫn agent.

**(c) Theo dõi một chỉ số duy nhất: tỷ lệ rule có cổng.**

> Mỗi rule trong `docs/` phải trả lời được: *"rule này được ép bằng cái gì?"*
> Trả lời "bằng niềm tin" → nó là gợi ý, không phải rule.

Khi thêm một rule mới mà không thêm được cổng cho nó, hãy ghi nhận điều đó thay vì lờ đi — đó là nợ, và nó cần nhìn thấy được.

### 5.3 Điều phải chấp nhận: PASS không có nghĩa là ĐÚNG

`.claude/CLAUDE.md` đã ghi rất rõ ba loại lỗi cổng **không bao giờ** bắt được:

1. **Văn xuôi tả thứ không tồn tại.** Gỡ một trích dẫn chết làm cổng xanh, nhưng đoạn văn bên cạnh vẫn có thể đang tả một màn hình chưa ai xây.
2. **Sơ đồ / cây thư mục chép sai.** Khối ``` trần không bị luật nào chặn — cây 10 project sai trong `backend-expert.md` lọt qua mọi cổng cho tới khi có người đọc.
3. **Ngày đúng nhưng nội dung sai.** `✅ Xong (2026-08-18)` qua được kiểm nhãn ngày kể cả khi việc đó chưa làm.

**Cổng là lưới chặn hồi quy, không phải chứng nhận chất lượng.** Mục tiêu thực tế không phải "100%" — mà là: *mọi vi phạm kiểm được bằng máy thì phải bị máy chặn, và phần còn lại phải có người hoặc agent độc lập đọc.*

---

## 6. Trạng thái các câu hỏi mở — cập nhật 2026-09-08

Toàn bộ câu hỏi chặn ở §4 **đã được trả lời**. Giữ mục này để truy nguyên, không phải để làm tiếp.

| Câu | Đáp án | Ghi ở đâu |
|---|---|---|
| §4.1 Thư viện UI | PrimeNG — dự án tiền nhiệm đã bọc đúng, đo được rất ít chỗ import trực tiếp | [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) |
| §4.2 Message broker | Không thêm broker ở v1 — dùng Outbox trên PostgreSQL | [`../wiki-core/be/05-cross-module-consistency.md`](../wiki-core/be/05-cross-module-consistency.md) |
| §4.3 Multi-tenant | **Có, làm ngay.** Nhiều cơ quan chung một bản cài; cột phân biệt; cách ly tuyệt đối | [`../adr/0013-multi-tenant.md`](../adr/0013-multi-tenant.md) |
| §4.4 Đa ngôn ngữ | Có, đã có hạ tầng | [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) |
| §4.5 Auth | **Cookie**, FE và API ở **hai origin khác nhau** | [`../adr/0004-giu-aspnet-identity.md`](../adr/0004-giu-aspnet-identity.md) + [`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md) |
| §4.6 Quan hệ hai repo | **Cắt đứt** — chỉ tham khảo | §4.6 ngay trên |
| §4.7 CI | Có | [`../adr/0011-ci-github-actions.md`](../adr/0011-ci-github-actions.md) |
| Số instance / Redis | Một instance; bộ khoá cookie trong PostgreSQL; chưa dùng Redis | [`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md) |
| Hook cho `core-reviewer` | **Đã thi công** — hook `Stop` chặn lượt, không còn phụ thuộc agent nhớ gọi | [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8 |

> Lộ trình E0–E6 ở §3 được viết cho phương án **trích xuất từ dự án tiền nhiệm**. Sau khi chốt cắt đứt (§4.6) và sau khi giai đoạn 1 đã dựng xong `docs/` + `.claude/`, phần còn hiệu lực của nó là **E4 trở đi** — dựng `src/` theo tài liệu. Ba bước đầu đã bị thay bằng việc viết mới, không phải di trú.
