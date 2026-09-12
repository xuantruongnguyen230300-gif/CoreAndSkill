---
kind: lich-su
scope: du-an
verified: chua-doi-chieu
---

# Kế hoạch chi tiết — `docs/` và `.claude/`

> # 🗄️ TÀI LIỆU LỊCH SỬ
>
> **File này mô tả trạng thái QUÁ KHỨ. Đường dẫn, con số và nhận định trong đây cố ý không còn đúng — đừng đọc nó để biết hiện trạng, và đừng trích dẫn nó làm căn cứ.**
>
> | Trong file này | Thực tế hiện nay |
> | --- | --- |
> | Danh sách file và số lượng từng khu | Đếm bằng lệnh; con số trong file này là con số của ngày lập kế hoạch |
> | Mô tả agent, skill, cổng | `.claude/README.md` và `bash .claude/check-docs.sh` |
> | Mô hình phục vụ FE↔API | **Hai origin khác nhau** — [`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md) |
>
> Nguồn sống: [`../README.md`](../README.md) · [`../RULES.md`](../RULES.md) · [`../adr/README.md`](../adr/README.md)


> **Ngày:** 2026-09-08 · **Phạm vi:** giai đoạn 1, chỉ `docs/` + `.claude/` + `.claude/check-docs.sh`. Không viết `src/`.
> Chi tiết hoá [`ke-hoach-giai-doan-1.md`](ke-hoach-giai-doan-1.md). Quyết định gốc: 12 mục ở §0 của file đó.

> ## ⚠️ KẾ HOẠCH NÀY ĐÃ THỰC THI XONG — 2026-09-08
>
> Đây là bản **kế hoạch** mô tả *sẽ viết gì vào đâu*. Việc đó đã làm xong: `docs/` và `.claude/` đã dựng. **Đừng đọc file này để biết hiện trạng** — nó mô tả ý định lúc lập kế hoạch, và vài chỗ đã bị chính việc thi công lật lại.
>
> | Chỗ trong file này | Trạng thái |
> | --- | --- |
> | §3–§7 mô tả *"sẽ viết mục nào vào file nào"* | 🗄️ **Đã thực thi.** Nội dung thật nằm ở chính các file đó, không ở đây |
> | §3.4 dòng *"CORS + `SameSite=None` + antiforgery 2 lớp — ✅ Giữ"* | ✅ **ĐÚNG.** Đã chốt (2026-09-10) FE và API ở **hai origin khác nhau**; xem `docs/quy-uoc/be-api-controller.md` §7 và ADR-0015 |
> | Mọi bảng liệt kê *"file X chứa những mục nào"* | 🗄️ Không phải định nghĩa, không thuộc sổ chủ quyền — nhưng cũng **không phải nguồn để tra cứu**. Tra ở chính file đó |
> | §11 Câu hỏi còn mở | Đã đóng hết, xem bảng ở đó |
>
> Giữ file lại vì nó ghi *vì sao* mỗi khu được thiết kế như vậy — thứ không đọc được từ kết quả. Nhưng mọi câu ở đây nói về **cái gì đang có** đều phải kiểm lại ở file thật.

---

## 0. Hai luật chi phối mọi thứ dưới đây

Port nguyên văn từ PlatformManager — đây là phần đã trả giá để có.

### Luật 1 — `.claude/` giữ **quy trình**, `docs/` giữ **tri thức**

Phép thử, trả lời được bằng có/không:

> **`.claude/` không được chứa câu nào có thể trở thành SAI khi code thay đổi.**

| Câu | Code đổi thì có sai không | Thuộc |
|---|---|---|
| "Xong việc chạm Core thì gọi `core-reviewer`" | Không | `.claude` ✓ |
| "Sửa envelope thì đọc `docs/quy-uoc/be-api-controller.md` trước" | Không (chỉ sai nếu **doc** đổi chỗ) | `.claude` ✓ |
| "Handler trả `Result<T>`, lỗi khai qua `ErrorDescriptor`" | **Có** | `docs` |
| "`Core/` có 5 project" | **Có** | `docs` |

**Hệ quả cứng:** file trong `.claude/` **không được chứa code block ngôn ngữ lập trình** (`csharp`, `typescript`, `scss`, `sql`). Chỉ được `bash`/`sh` (lệnh chạy), `markdown` (mẫu báo cáo), `text` (khối `$ARGUMENTS`), và khối không gắn ngôn ngữ (sơ đồ cây).

### Luật 2 — chiều cập nhật một chiều

| Việc vừa xảy ra | Sửa ở | `.claude/` có đổi không |
|---|---|---|
| Chốt quyết định kiến trúc | `docs/` | **KHÔNG** |
| Bổ sung quy ước, code mẫu | `docs/` | **KHÔNG** |
| File `docs/` đổi tên / đổi chỗ / bị xoá | `docs/` | **CÓ — chỉ sửa đường dẫn** |
| Thêm file `docs/` làm file chủ của chủ đề mới | `docs/` | **CÓ — thêm MỘT dòng đường dẫn** vào đúng agent cần nó |
| Thêm/bỏ agent hoặc skill | `.claude/` | CÓ |

**Không ô nào cho phép chép nội dung từ `docs/` sang `.claude/`.**

Dạng trỏ đường chuẩn — một dòng, không tóm tắt kèm:

```markdown
> 📖 Envelope & error → HTTP: đọc `docs/quy-uoc/be-api-controller.md`
```

> **Vì sao nghiêm khắc thế:** PlatformManager đã trả giá ba lần — (1) `src/BE/CLAUDE.md` khẳng định 5 project `Business.*` đã tồn tại trong khi chưa có project nào; (2) một recipe `RowVersion` sai provider tồn tại song song ở hai chỗ, sửa một nơi không chạm nơi kia; (3) agent đọc bản sao xong đã có câu trả lời tự tin nên **không bao giờ mở `docs/`** — lỗi loại này không tự lộ ra.

---

## 1. Bản đồ `docs/`

```
docs/
├─ README.md                    mục lục + bảng trạng thái cấp khu
├─ RULES.md                     rule MUST/MUST NOT + cột "ép bằng gì"
├─ kien-truc-core-module.md     ranh giới Core ↔ Module (file chủ)
│
├─ quy-uoc/                     QUY ƯỚC THI CÔNG — có code mẫu, ~400-500 dòng/file
│  ├─ README.md
│  ├─ be-architecture.md · be-entity-domain.md · be-cqrs-handler.md
│  ├─ be-api-controller.md · be-performance.md
│  ├─ fe-architecture.md · fe-api-client.md · fe-ui-conventions.md · fe-routing-guard.md
│  └─ repo-artifact.md · tieu-chi-review.md
│
├─ wiki-core/                   KIẾN THỨC NỀN — "một core tốt gồm gì và vì sao"
│  ├─ README.md
│  ├─ be/    01..16 + tra-cuu-file-class.md
│  └─ fe/    01..17 + trien-khai/00..05
│
├─ contracts/                   HỢP ĐỒNG API Core — auth · users · permissions · meta-menu
├─ database/                    schema-core.md · migration-policy.md · script-runbook.md 🆕
├─ Design/                      NGUỒN GIAO DIỆN DUY NHẤT — Templates/ · Components/ · DESIGN.md
├─ adr/          🆕             ADR đánh số cho mọi quyết định kiến trúc
└─ audit/        🆕             kết quả audit + postmortem sự cố
```

**Không chép số lượng file vào bất kỳ tài liệu nào** (luật §6 của PlatformManager — bảng đếm tay luôn mục ruỗng). Đếm bằng lệnh:

```bash
find docs -name '*.md' | wc -l
grep -rLE '^kind:' docs --include='*.md'    # file thiếu frontmatter
```

### Ba khoá frontmatter — bắt buộc mọi file

| Khoá | Giá trị | Trả lời |
|---|---|---|
| `kind` | `luat` · `tham-chieu` · `quyet-dinh` · `lich-su` | Code có phải tuân file này không |
| `scope` | `core` · `du-an` | File có đi theo khi tách sang dự án khác không |
| `verified` | `YYYY-MM-DD` · `chua-doi-chieu` · `khong-ap-dung` | Lần cuối đối chiếu source là bao giờ |

Giai đoạn 1 chưa có `src/` → gần như mọi file mang `verified: chua-doi-chieu`. **Đó là giá trị trung thực, không phải nợ.** `khong-ap-dung` chỉ được dùng khi file đã mang `kind: tham-chieu` hoặc `kind: lich-su` — cổng kiểm điều kiện đó, không cho tự khai.

---

## 2. Phần CHUNG — 3 file gốc

### 2.1 `docs/README.md` — mục lục cấp cao nhất

`kind: luat` · `scope: core`

| Mục | Nội dung |
|---|---|
| Tra theo chủ đề | Bảng *"đang làm gì → đọc file nào"*. Đây là đường dự phòng khi bảng định tuyến của agent không có chủ đề |
| Các khu — mỗi khu một vai | `quy-uoc/` = đang thực thi · `wiki-core/` = kiến thức nền · `Design/` = giao diện · `contracts/`+`database/` = hợp đồng · `adr/` = quyết định · `audit/` = bài học |
| Bảng trạng thái | Nhãn **ở cấp khu**, không ở cấp file |
| Cổng trước khi coi là xong | `bash .claude/check-docs.sh` + cảnh báo *PASS ≠ ĐÚNG* |

> **Bài học phải áp dụng ngay:** PlatformManager từng gắn nhãn trạng thái vào **từng dòng file** trong mục lục. Đối chiếu 2026-09-06: **5 trong 7 nhãn đã sai**. Trạng thái của một file phải đọc ở **đầu chính file đó** + khoá `verified`. Mục lục chỉ giữ nhãn cấp khu.

**Khoảng cách giữa `quy-uoc/` và `wiki-core/` là cố ý** — nó cho `core-reviewer` phân biệt *"lệch vì đã cố ý đơn giản hoá"* (không phải finding) với *"lệch vì thiếu sót thật"* (là finding). Đừng gộp hai khu.

### 2.2 `docs/RULES.md` — rule máy kiểm được

`kind: luat` · `scope: core`

Bảng một dòng một rule, **bốn cột**:

| Rule | Mức | Ép bằng gì | Trạng thái |
|---|---|---|---|
| `Core.Domain` có 0 package reference | MUST | ArchTest `Core_Domain_Assembly_MustHave_ZeroPackageReference` | 📐 chờ giai đoạn 2 |
| `Core.*` không tham chiếu `Modules.*` | MUST | ArchTest `Core_MustNotReference_AnyModulesAssembly` | 📐 chờ giai đoạn 2 |
| `.claude/` không chứa code block ngôn ngữ lập trình | MUST | `check-docs.sh` §2 | ✅ ngay giai đoạn 1 |
| Mọi file `docs/` khai đủ 3 khoá frontmatter | MUST | `check-docs.sh` §12 | ✅ ngay giai đoạn 1 |
| Chú thích không trỏ vào file `kind: lich-su` | MUST | `check-docs.sh` §13 🆕 | ✅ ngay giai đoạn 1 |

**Cột "Ép bằng gì" là lý do file này tồn tại.** Rule trả lời "bằng niềm tin" thì không phải rule — nó là gợi ý, và phải nằm ở danh sách nợ cuối file, nhìn thấy được.

### 2.3 `docs/kien-truc-core-module.md` — file chủ về ranh giới

`kind: luat` · `scope: core` · **Nguồn: port + sửa nặng** từ `doc/kien-truc-core-module.md` (94 KB)

| Mục | Sửa gì so với nguồn |
|---|---|
| Layout Core | **3 → 5 project**: `Domain` · `Application` · `Infrastructure` · `Web` 🆕 · `Contracts` 🆕. Bỏ hẳn phương án 6 project (`Common` là ngăn kéo rác; `Persistence` tách khỏi `Infrastructure` không đáng) |
| Bảng *"có thật hôm nay → sẽ thành"* | Giai đoạn 1: cột trái là **rỗng** (chưa có src). Ghi rõ toàn bộ là `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` |
| Tiêu chí Core vs Module | Giữ. Bổ sung ngưỡng định lượng: **code chỉ vào Core khi ≥ 2 module cần** |
| Ranh giới DB | Mỗi module 1 schema; **cấm FK vật lý xuyên schema** |
| Host mỏng | **Dưới 50 dòng** sau khi có `Core.Web` (nay là 629 dòng) |
| Nguyên tắc đếm | Giữ nguyên: *"số project đếm bằng lệnh, đừng chép vào tài liệu"* |

---

## 3. Phần BACKEND

### 3.1 `docs/quy-uoc/be-architecture.md`

`kind: luat` · **Nguồn:** port `quy-uoc/be-architecture.md` (438 dòng), sửa nặng mục layout

| Mục | Nội dung | Sửa gì |
|---|---|---|
| Project layout & dependency direction | Bảng 5 project: chứa gì / cấm chứa gì / tham chiếu được gì | 🔴 **Viết lại** — thêm `Core.Web`, `Core.Contracts` |
| Host mỏng — composition root duy nhất | `Program.cs` chỉ `AddCore()` + `UseCore()` + đăng ký module | 🔴 **Viết lại** — nay 14 file hạ tầng nằm ở host |
| Cấu hình — fail-fast validation | `IOptions<T>` + `ValidateOnStart`, thiếu cấu hình thì **không khởi động** | ✅ Giữ (đã tốt) |
| Dependency Injection | Quy ước đăng ký, vòng đời, `AddCoreXxx()` theo nhóm | ✅ Giữ |
| Vertical slice trong Application | Thư mục theo nghiệp vụ, không theo loại kỹ thuật | ✅ Giữ |
| Thêm tính năng mới — checklist | Các bước từ entity tới endpoint | 🟠 Sửa theo Result thuần |
| SOLID & OOP áp dụng cụ thể | ✅ Giữ | |
| Testing | Kim tự tháp + ArchTest | 🟠 Thêm luật cho `Core.Web` |

### 3.2 `docs/quy-uoc/be-entity-domain.md`

`kind: luat` · **Nguồn:** port, sửa mục exception

| Mục | Sửa gì |
|---|---|
| `BaseEntity` — `Id` là `init` (UUID v7), 5 field audit `public set` có chủ đích | ✅ Giữ nguyên — thiết kế này tốt và có lý do rõ |
| Soft delete — một vòng lặp trong `OnModelCreating`, không khai lẻ từng configuration | ✅ Giữ |
| Encapsulation — field nghiệp vụ `private set`, mutate qua method có tên nghiệp vụ | ✅ Giữ |
| Value Object | ✅ Giữ |
| `RowVersion` / concurrency | ⚠️ **Kiểm lại provider** — PlatformManager từng có recipe dùng `IsRowVersion()` của SQL Server trong dự án chạy Npgsql |
| **Factory & mutation method** | 🔴 **Viết lại**: trả `Result<T>`, **KHÔNG ném `DomainException`** |
| ~~`DomainException` / `ConflictException`~~ | 🔴 **Xoá hẳn mục này** |
| Ranh giới Identity | 🆕 **Viết mới**: `AppUser`/`AppRole` chỉ sống trong `Core.Infrastructure`; `Application` chỉ thấy `IIdentityService`. Ghi rõ đây là **đánh đổi có ý thức** |

### 3.3 `docs/quy-uoc/be-cqrs-handler.md`

`kind: luat` · **Nguồn:** port `be-cqrs-handler.md` (511 dòng) — file bị sửa nhiều nhất

| Mục | Sửa gì |
|---|---|
| Command / Query qua MediatR | ✅ Giữ |
| Handler — 4 bước, handler own `SaveChanges` | 🔴 **Viết lại**: `TransactionBehavior` bọc command; handler không tự quản transaction |
| Validator (FluentValidation) | ✅ Giữ, kể cả cảnh báo *"nhiều validator cho MỘT request phải có `ValidationContext` riêng"* |
| `ErrorDescriptor` — thay magic string | ✅ Giữ — thiết kế catalog tập trung này tốt |
| ~~`BaseResponse` với `Ok`/`Fail`~~ | 🔴 **Thay bằng `Result<T>`** — bỏ `IApiResult` khỏi tầng Application |
| **`Result<T>` — hợp đồng đầy đủ** | 🆕 **Viết mới**: `Error(Code, MessageTemplate, ErrorType)`; `ErrorType` ∈ `Validation`/`NotFound`/`Conflict`/`Forbidden`/`BusinessRule`; cách truyền Result ngược lên |
| **i18n cho thông điệp lỗi** | ✅ **Giữ nguyên thiết kế** — tham số truyền theo **TÊN** chứ không theo thứ tự, vì trật tự từ mỗi ngôn ngữ một khác. Nhưng bỏ phụ thuộc `FluentValidation.Internal.MessageFormatter` (namespace `Internal` — vỡ được ở bất kỳ bản minor nào) |
| Grid / danh sách — envelope nhất quán | ✅ Giữ |
| Audit log cho hành động nhạy cảm | 🟠 Nguồn đang `📐 ĐÍCH ĐẾN` — giữ nhãn đó |
| Command chạy lâu → job nền | ✅ Giữ |

### 3.4 `docs/quy-uoc/be-api-controller.md`

`kind: luat` · **Nguồn:** port, đổi chủ thể từ host sang `Core.Web`

| Mục | Sửa gì |
|---|---|
| **`Core.Web` chứa gì** | 🆕 **Viết mới** — đây là mục quan trọng nhất của cả giai đoạn 1. Liệt kê: `ApiControllerBase`, 6 middleware, `HttpContextCurrentUser`, `ModelBindingProblemFactory`, controller Core, extension `AddCore()`/`UseCore()` |
| **Ánh xạ `Result` → HTTP** | 🆕 **Viết mới**, thay `ExceptionHandlingBehavior`. `ErrorType` → status code, ở **đúng một chỗ**, **KHÔNG reflection** |
| Envelope trả về | ✅ Giữ hình dạng; bỏ phần dựng từ exception |
| Rate limiting | ✅ Giữ — kèm cảnh báo overload đúng (nguồn từng có mẫu sai overload và `Program.cs` chép y theo) |
| CORS + `SameSite=None` + antiforgery 2 lớp | ✅ Giữ — phần khó nhất đã làm đúng, kể cả bài học đặt trùng tên cookie (2026-08-24) |
| Phân quyền | 🔴 **Sửa**: `RequirePermissionAttribute` + `IPermissionChecker` giữ; **bỏ mọi hằng số role** |
| `AllowAnonymous` allowlist | ✅ Giữ |

### 3.5 `docs/quy-uoc/be-performance.md`

`kind: luat` · **Nguồn:** port gần như nguyên — repository, query, index, N+1, cache. Bỏ phần `CachingBehavior` (chưa làm ở v1), ghi rõ **vì sao chưa** để người sau không tưởng là bỏ sót.

### 3.6 `docs/wiki-core/be/` — kiến thức nền BE

Port toàn bộ. Bốn file phải sửa nặng:

| File | Sửa gì |
|---|---|
| `02-identity-auth.md` | 🔴 **Bỏ role hardcode.** Chỉ permission. Thêm mục *"dự án mới seed role + permission mặc định thế nào"* và *"tài khoản bootstrap lấy toàn quyền bằng cách nào khi không có role cứng"* |
| `04-testing-strategy.md` | 🟠 Thêm ArchTest cho `Core.Web`; **bỏ** ArchTest ép migration ở host; giữ nguyên **meta-test** (mỗi detector có test kiểm chính nó — đây là điểm hay nhất, phải giữ) |
| `13-core-data-migration.md` | 🔴 **Đảo**: Core sở hữu migration bảng Core. Ghi rõ đánh đổi: `Core.Infrastructure` nay biết provider là PostgreSQL |
| `16-i18n-va-ma-loi.md` | 🟠 Error catalog theo `Result`, bỏ ánh xạ từ loại exception. Giữ nguyên nguyên tắc tham số theo tên |

Còn lại port gần nguyên: `01-core-components`, `03-metadata-driven-design`, `05-cross-module-consistency`, `06-concurrency-control`, `07-observability`, `08-adr-practice`, `09-security-beyond-auth`, `10-data-retention`, `11-performance-caching`, `12-notifications`, `14-file-storage`, `15-import-export`, `tra-cuu-file-class`.

> `tra-cuu-file-class.md` là bảng tra "class này nằm file nào". Giai đoạn 1 chưa có src → để **rỗng có chủ đích**, `verified: khong-ap-dung`, kèm dòng nói rõ nó được điền ở giai đoạn 2.

---

## 4. Phần FRONTEND

### 4.1 `docs/quy-uoc/fe-architecture.md`

`kind: luat` · **Nguồn:** port, sửa mục ranh giới

| Mục | Sửa gì |
|---|---|
| Bốn tầng `core/` → `shared/` → `platform/` → `modules/` và chiều phụ thuộc | ✅ Giữ |
| Cấu trúc một feature | ✅ Giữ |
| **Ranh giới ép bằng ESLint** | 🔴 **Viết lại** vì đã chọn "giữ thư mục, siết ESLint": (1) hai zone `coreLayerZones` + `moduleBoundaryZones`; (2) **cấm `eslint-disable` cho rule ranh giới**; (3) quy trình *"thêm module mới phải thêm tên vào `BUSINESS_MODULES`"*; (4) cổng kiểm mảng đó khớp thư mục `modules/` thật |
| **Ngưỡng kích thước file** | 🆕 **Viết mới** — nguồn có `styles.scss` **1015 dòng** và `quan-tri-nguoi-dung.page.ts` **560 dòng**. Đặt ngưỡng + cách tách |

> **Lý do mục ranh giới phải viết kỹ:** không chọn Angular workspace nghĩa là không có compiler ép. ESLint là hàng rào duy nhất, mà ESLint bỏ qua được bằng một dòng comment. Nếu không cấm `eslint-disable`, luật chỉ là gợi ý.

### 4.2 `docs/quy-uoc/fe-ui-conventions.md`

| Mục | Sửa gì |
|---|---|
| Control flow Angular 20 (`@if`/`@for` có `track`), standalone, signals | ✅ Giữ |
| **Bọc PrimeNG** | ✅ **Nâng thành luật có cổng.** Đo được ở nguồn: chỉ 7 import `primeng/` toàn FE, đều hợp lệ. Ghi rõ danh sách chỗ được phép import trực tiếp, mọi chỗ khác đi qua `shared/` |
| Style theo token — không hex, không `rgb()` literal | ✅ Giữ (G1 + G11) |
| Form typed reactive | ✅ Giữ |
| **Tách `styles.scss`** | 🆕 Viết mới — token màu / typography / spacing / override PrimeNG |

### 4.3 `docs/quy-uoc/fe-api-client.md` · `fe-routing-guard.md`

Port gần nguyên. Sửa:
- `fe-api-client.md`: envelope FE khớp `Result` → HTTP mới; ranh giới DTO/model + mapper giữ nguyên (G6 đã canh).
- `fe-routing-guard.md`: **bỏ guard theo role**, chỉ guard theo permission (hệ quả quyết định 5).

### 4.4 `docs/wiki-core/fe/` + `trien-khai/`

Port toàn bộ. Sửa nhẹ:

| File | Sửa gì |
|---|---|
| `07-auth-identity.md` | Bỏ role, chỉ permission |
| `trien-khai/05-gate.md` | 🔴 **Sửa nặng** — xem §4.5 |
| Còn lại | Port gần nguyên |

### 4.5 Cổng FE — phải khai đúng thứ thật sự chạy

Nguồn hôm nay: `fe-gate.sh` chạy **5 mục** (G1, G3, G6, G11, G12); G2/G7/G8/G9 thuộc `ng lint`/`ng build`; G10 để trống có chủ đích; **G4 và G5 chưa bao giờ tồn tại**.

Doc mới phải:

1. Ghi rõ **G4, G5 là `📐 ĐÍCH ĐẾN`**, không ghi lẫn vào nhóm đang chạy.
2. Ghi rõ **G8 hôm nay là no-op** vì `BUSINESS_MODULES` rỗng — và điều kiện để nó thật sự chạy.
3. Cảnh báo nguyên văn: **`fe-gate.sh` KHÔNG phải toàn bộ cổng FE.** Chạy mỗi script rồi tuyên bố cổng xanh là cách hỏng **đã xảy ra thật**.
4. Đếm mục bằng lệnh, không chép số: `grep -c '^section ' scripts/fe-gate.sh`.

### 4.6 `docs/Design/`

Port `Templates/` (10 file) + `DESIGN.md` + `COMPONENTS.md` + `Icons.md` + `Design/CLAUDE.md`.

**Phân loại lại `Components/`** — nguồn có 28 spec, `shared/components/` mới hiện thực 8:

| Nhóm | Xử lý |
|---|---|
| Core (Button, Input, DataTable, Dialog, Toast, Sidebar, Topbar, Toolbar, ConfirmDialog, Card, Badge, Avatar, Check, FormRow, IconButton, NoticeBanner, SegmentedControl, LanguageSwitcher, AuthCard, AuthField, Footer, Table) | Mang sang |
| Nghiệp vụ (KpiTile, TrendChart, DeltaIndicator, HistoryRow, ProgressBar) | **Không mang** |

Giữ nguyên ngoại lệ đã chốt: `Design/` phủ cả màn Core lẫn màn nghiệp vụ — chia đôi khu này sẽ phá đúng thứ nó sinh ra để làm.

---

## 5. Phần DATABASE

### 5.1 `docs/database/schema-core.md`

`kind: luat` · `scope: core` · **Nguồn:** `doc/cau-truc-database.md` (schema `core`). **Không** mang `cau-truc-database-business.md` (`scope: du-an`) và `cau-truc-database.sql` (`kind: lich-su`).

Bảng/cột/index của schema `core`: user, role, permission, role_permission, menu, menu_role, refresh/session, notification.

### 5.2 `docs/database/migration-policy.md`

`kind: luat` · **Sửa nặng** so với nguồn

| Mục | Nội dung |
|---|---|
| **Ai sở hữu migration** | 🔴 **Đảo:** Core sở hữu migration bảng Core; module sở hữu migration schema mình |
| Đánh đổi | Ghi thẳng: `Core.Infrastructure` nay **biết provider là PostgreSQL**. Chấp nhận vì đã chốt Postgres |
| Schema riêng theo module | Core dùng schema `core`; **cấm FK vật lý xuyên schema**, tham chiếu bằng ID |
| Soft delete | Query filter tự động cho mọi `BaseEntity` |
| ArchTest liên quan | Mỗi migration nằm đúng project của schema nó (**thay** luật cũ ép migration ở host) |

### 5.3 `docs/database/script-runbook.md` 🆕 — hệ quả quyết định 11

Quyết định: giữ chạy tay, **nhưng phải có đường dẫn cụ thể để dev khác chạy được**. File này trả lời đủ 5 câu:

| # | Câu | Phải ghi rõ |
|---|---|---|
| 1 | Script nằm ở đâu | Một đường dẫn cố định — `database/scripts/` |
| 2 | Đặt tên thế nào | Có thứ tự: `0001__core-init.sql`, `0002__core-add-audit.sql` |
| 3 | Chạy theo thứ tự nào, biết mình đang ở đâu | Bảng lịch sử áp dụng trong DB |
| 4 | Sinh script bằng lệnh gì | `dotnet ef migrations script --idempotent --from X --to Y` |
| 5 | **Làm sao biết DB đã lệch model** | App khởi động gọi `GetPendingMigrations()`; có migration chưa áp thì **từ chối khởi động**, in đúng tên migration thiếu + đường dẫn script cần chạy |

> Câu 5 là câu quan trọng nhất và là thứ nguồn không có. Chạy tay mà không có cơ chế phát hiện lệch thì lỗi chỉ lộ ra lúc chạy — im lặng chạy tiếp với DB lệch là cách hỏng tệ nhất.

---

## 6. Phần ADR và AUDIT — hai khu mới

### 6.1 `docs/adr/`

Nguồn có `wiki-core/be/08-adr-practice.md` (luật viết ADR) nhưng **không có chỗ chứa ADR nào**. Giai đoạn 1 mở khu này và ghi ngay 12 ADR cho 12 quyết định đã chốt:

| ADR | Nội dung |
|---|---|
| 0001 | Modular Monolith — Core + Modules trong một solution |
| 0002 | Core gồm 5 project, có `Core.Web` |
| 0003 | Result thuần — Domain không ném exception nghiệp vụ |
| 0004 | Giữ ASP.NET Core Identity, khoanh vùng trong Infrastructure |
| 0005 | Permission-based, bỏ role hardcode |
| 0006 | Pipeline behavior: Validation + Transaction |
| 0007 | FE giữ cấu trúc thư mục, siết ESLint (không dùng workspace/Nx) |
| 0008 | Core sở hữu migration bảng Core |
| 0009 | Áp schema chạy tay + cơ chế phát hiện lệch |
| 0010 | Comment tối thiểu, lịch sử sự cố ở `docs/audit/` |
| 0011 | CI GitHub Actions chạy đủ cổng |
| 0012 | Giai đoạn 1 chỉ `docs/` + `.claude/` |

Mỗi ADR: Bối cảnh · Quyết định · Phương án đã cân nhắc và **vì sao loại** · Hệ quả · Trạng thái.

### 6.2 `docs/audit/` — hệ quả quyết định 12

Vì code sẽ không còn giữ lịch sử sự cố, khu này giữ thay.

```
docs/audit/README.md
docs/audit/YYYY-MM-DD-<slug>.md
```

Mỗi mục 4 phần: **Hiện tượng · Nguyên nhân gốc · Cách vá · Cổng nào lẽ ra phải bắt** (phần 4 là phần có giá trị nhất — nó sinh ra mục cổng mới).

Chép sang ngay 2 bài học có thật làm mẫu:

1. **2026-09-05 — reflection ở đường xử lý lỗi.** `GetMethod` trả null → mọi `DomainException` thành `NullReferenceException`, hỏng đúng nhánh lỗi, không lỗi biên dịch, không ArchTest nào chạm tới, **chỉ lộ ra ở Production**.
2. **2026-08-23 — cổng không tồn tại.** `scripts/fe-gate.sh` không có trên đĩa nên 3 cổng FE không chạy suốt thời gian dài, trong khi tài liệu vẫn ghi bình thường.

Code chỉ được để lại **một dòng** trỏ tới file audit, không kể lại nội dung.

---

## 7. Phần AGENT — 8 agent, mỗi agent chỉ trỏ đường

### 7.1 Khung chung mọi agent

Mọi file agent theo đúng dàn bài của PlatformManager — **không mục nào chứa tri thức**:

| Mục | Chứa gì |
|---|---|
| `# Vai trò` | Làm gì, không làm gì |
| `# STEP -1 — Resolve root` | Marker bất biến để tìm gốc (`docs/README.md`, …) |
| `# 📖 Tri thức kỹ thuật — KHÔNG nằm ở file này` | **Bảng định tuyến**: *đang làm gì → đọc file nào*. Một dòng, không tóm tắt kèm |
| `# 🤝 Bàn giao` | Giao việc cho agent nào, dạng gì |
| `# 🛑 Dừng lại và hỏi khi` | Ranh giới không được tự quyết |
| `# 🔧 Lệnh & công cụ` | Lệnh được phép chạy |

### 7.2 Bốn agent port

| Agent | Bảng định tuyến trỏ vào | Sửa gì khi port |
|---|---|---|
| `backend-expert` | `quy-uoc/be-*.md` (5 file) · `wiki-core/be/14`,`15` · `repo-artifact.md` · `kien-truc-core-module.md` · `wiki-core/be/01` §Áp dụng | Thêm dòng cho `Core.Web`; **xoá** dòng về `DomainException`; gỡ tên sản phẩm |
| `frontend-expert` | `quy-uoc/fe-*.md` (4 file) · `wiki-core/fe/*` · `Design/` | Thêm dòng về luật cấm `eslint-disable`; gỡ tên sản phẩm |
| `core-reviewer` | `docs/README.md` + `wiki-core/README.md` (2 mục lục bắt buộc) + bảng định tuyến theo chủ đề review | Giữ nguyên 3 đặc điểm cốt lõi (xem dưới); thêm dòng trỏ `docs/audit/` |
| `design-expert` | `Design/CLAUDE.md` · `Design/Templates/` | Gần như port thẳng |

**Ba đặc điểm của `core-reviewer` phải giữ nguyên** — đây là thứ khiến "agent kiểm agent" hoạt động thật:

1. **Không có quyền `Edit`** (khai ở `tools:` frontmatter) — người kiểm không sửa thứ mình kiểm.
2. **Context riêng, tự đọc code**, không nhận tóm tắt từ agent vừa viết — nhận tóm tắt thì chỉ xác nhận lại thiên kiến của agent kia.
3. **Một lượt = một phạm vi** (BE **hoặc** FE, không bao giờ cả hai) và **đọc theo bảng định tuyến, không đọc cả wiki**. Lý do đã trả giá: một lượt BE-only từng tiêu 405K token; corpus đầy đủ 780 KB giết 3 lượt review liên tiếp; trỏ đường giữ ở ~264 KB.

### 7.3 Bốn agent mới

| Agent | Vai trò | Bảng định tuyến trỏ vào | Ranh giới "dừng lại và hỏi" |
|---|---|---|---|
| `ba-analyst` | Yêu cầu thô → US + AC + BR | `Design/Templates/` · `contracts/` · `kien-truc-core-module.md` (phân loại Core vs nghiệp vụ) | **Không tự bịa nghiệp vụ.** Không rõ thì hỏi. Không tự quyết một feature là Core hay Module |
| `test-engineer` | Sinh unit + integration test, tìm ca biên | `wiki-core/be/04-testing-strategy.md` · `wiki-core/fe/06-testing-strategy.md` · `quy-uoc/tieu-chi-review.md` | Không sửa code sản phẩm để test dễ pass |
| `tech-writer` | TechDoc / UserGuide / UnitTest doc | `Design/Templates/` · `contracts/` · `docs/README.md` | Không khẳng định thứ chưa đối chiếu source |
| `architect` | ADR + phản biện thiết kế | `adr/` · `wiki-core/be/08-adr-practice.md` · `kien-truc-core-module.md` · `RULES.md` | **Được quyền nói không.** Mọi thay đổi chạm `Core/` phải qua đây và phải có ADR |

### 7.4 `.claude/CLAUDE.md` — luật repo

Port 9 mục của nguồn, sửa:

- §1 bảng cấm git — giữ nguyên, **cưỡng chế bằng `settings.json` → `permissions.deny`**, không bằng câu văn
- §2 ranh giới ba khu — đổi `doc/` → `docs/`
- §7 — bỏ mục về `doc/Prototype/` (đặc thù repo cũ)
- §8 — cập nhật: giai đoạn 1 chỉ có **một** cổng (`check-docs.sh`); hai cổng còn lại thêm ở giai đoạn 2
- §9 — giữ nguyên ba khoá frontmatter

---

## 8. Phần SKILL

### 8.1 Port được ngay

| Nhóm | Skill | Sửa gì |
|---|---|---|
| Design | 9 skill `design-*` | Gỡ tên sản phẩm; đổi đường dẫn `doc/` → `docs/` |
| Wrapper | `backend-expert`, `frontend-expert`, `core-reviewer` | Đổi đường dẫn |
| Điều phối | `feature-kickoff` | Thêm nhánh gọi `ba-analyst` (bước 3 gate spec) và `test-engineer` (sau bước 5) |

### 8.2 Viết mới ở giai đoạn 1 — chỉ đọc và kiểm

| Skill | Làm gì | Trỏ vào |
|---|---|---|
| `/doc-sync` | Dò lệch trong `docs/`: thiếu frontmatter · file chủ trùng chủ đề · bảng định tuyến trỏ file chết · **chú thích trỏ vào `kind: lich-su`** | `docs/README.md` · `RULES.md` |
| `/adr-new` | Tạo ADR đánh số tiếp theo theo mẫu | `wiki-core/be/08-adr-practice.md` |
| `/ba-story` | Chạy `ba-analyst` để sinh US/AC/BR | `Design/Templates/` |
| `/audit-log` | Ghi một mục `docs/audit/` từ một sự cố vừa xảy ra | `docs/audit/README.md` |
| `/review-doc` | Review một thay đổi tài liệu theo `tieu-chi-review.md` | `quy-uoc/tieu-chi-review.md` |

`/arch-check` và `/review-pr` **hoãn sang giai đoạn 2** — chúng cần `src/` để kiểm.

### 8.3 Hoãn sang giai đoạn 2 — nhóm sinh code

`/core-new-module` · `/core-new-usecase` · `/core-new-entity` · `/fe-new-feature`

Lý do đã nêu ở [`lo-trinh-va-nguon.md`](lo-trinh-va-nguon.md) §3.2: skill scaffold là máy photocopy, cần bản gốc. Giai đoạn 1 chưa có `src/`.

---

## 9. `.claude/check-docs.sh` — cổng duy nhất của giai đoạn 1

Port 12 mục của nguồn (529 dòng), sửa 2, thêm 2.

| # | Mục | Trạng thái |
|---|---|---|
| 1 | Bảng cấm git trong `CLAUDE.md` §1 khớp `permissions.deny` | ✅ Port |
| 2 | `.claude/**/*.md` không có code block ngôn ngữ lập trình | ✅ Port |
| 3 | Mọi link markdown resolve được | ✅ Port |
| 4 | Đường dẫn `docs/...` được trích dẫn đều tồn tại | 🟠 Sửa: `doc/` → `docs/` |
| 5 | Trích dẫn không neo vào file bị `.gitignore` loại trừ | ✅ Port |
| 6 | Dòng chứa `ĐÃ CÓ`/`✅ Xong`/`FIXED`/`Đã bật` đều kèm ngày | ✅ Port |
| 7 | **Trích dẫn `file:dòng` phải nằm trong file** | ✅ Port — mục giá trị nhất, đo gián tiếp tính trung thực |
| 8 | Bảng định tuyến trỏ đúng chủ đề (định danh trong `` ` `` phải có trong file đích) | ✅ Port |
| 9 | Tên file `.md` trong code span ở `.claude/` phải resolve được | ✅ Port |
| 10 | Chú thích trong `src/` trỏ `docs/` phải tồn tại | 🟠 Giai đoạn 1 chưa có `src/` → no-op **có khai báo** |
| 11 | ~~Không còn tham chiếu `doc/Prototype/`~~ | 🔴 **Bỏ** — đặc thù repo cũ |
| 12 | Mọi file `docs/` khai đủ `kind`/`scope`/`verified` | ✅ Port |
| 13 | 🆕 **Chú thích/trích dẫn không trỏ vào file `kind: lich-su`** | Bịt lỗ hổng tìm ra ở `Roles.cs` — đường dẫn tồn tại nên cổng xanh, mà file đã chết |
| 14 | 🆕 **Mọi rule trong `RULES.md` khai cột "ép bằng gì"** | Rule chưa có cổng phải nằm ở danh sách nợ, nhìn thấy được |

**Bắt buộc kèm theo:** mọi lệnh của cổng phải nằm trong `permissions.allow` của `settings.json`. Một cổng bị prompt chặn là cổng trên thực tế không ai chạy.

**Và:** giữ nguyên mục cảnh báo ở cuối — **PASS không có nghĩa là tài liệu ĐÚNG.** Ba loại lỗi cổng không bao giờ bắt được: văn xuôi tả thứ không tồn tại; sơ đồ/cây thư mục chép sai (khối ``` trần không bị luật nào chặn); ngày đúng nhưng nội dung sai.

---

## 10. Lộ trình giai đoạn 1

| Bước | Nội dung | Effort | Nghiệm thu |
|---|---|---|---|
| **D0** | `docs/README.md` + `RULES.md` + khung thư mục + quy ước frontmatter | 3 ngày | Khung đủ chỗ cho mọi file sắp port |
| **D1** | Port khu `wiki-core/` (be + fe + trien-khai) — phần ít sửa nhất, làm trước để có nền | 1 tuần | Mọi file có frontmatter; `check-docs.sh` mục 12 xanh |
| **D2** | Port + sửa khu `quy-uoc/`, trong đó 6 file sửa nặng (§3, §4) | 1.5 tuần | Không còn mâu thuẫn với 12 quyết định |
| **D3** | `kien-truc-core-module.md` + `database/` (3 file, có `script-runbook.md` mới) + `contracts/` | 1 tuần | 5 câu của runbook đều có đáp án |
| **D4** | `Design/` — port Templates + phân loại lại 28 component spec | 3 ngày | Chỉ còn component Core |
| **D5** | `.claude/check-docs.sh` (14 mục) + `settings.json` + CI | 3 ngày | Cổng đỏ thật khi cố tình vi phạm |
| **D6** | 4 agent port + 4 agent mới + `.claude/CLAUDE.md` | 1 tuần | Mọi dòng định tuyến trỏ file có thật (mục 8 của cổng xanh) |
| **D7** | Skill: port 13 + viết 5 mới | 1 tuần | `/doc-sync` chạy được và tìm ra ít nhất 1 vấn đề thật |
| **D8** | `adr/` 12 ADR + `audit/` 2 postmortem mẫu | 3 ngày | 12 quyết định đều truy nguyên được |

**Tổng: khoảng 6.5 tuần-người.**

Đường găng: `D0 → D1 → D2 → D6 → D7`. D3, D4, D5, D8 chen vào được sau D0.

> **Thứ tự D1 trước D2 là có chủ đích:** `wiki-core/` là kiến thức nền ít phụ thuộc quyết định mới nhất, port được gần nguyên. Làm nó trước cho ta một nền để `quy-uoc/` trỏ vào, và cho cổng có thứ thật để kiểm ngay từ tuần đầu.

---

## 11. Câu hỏi còn mở — ĐÃ ĐÓNG HẾT (2026-09-08)

Năm câu này đều đã có đáp án. **Đừng đọc mục này như việc phải làm** — bảng đầy đủ kèm nơi ghi đáp án ở [`ke-hoach-giai-doan-1.md`](ke-hoach-giai-doan-1.md) §9.

Tóm tắt: multi-tenant **làm ngay** ([`../adr/0013-multi-tenant.md`](../adr/0013-multi-tenant.md)); quan hệ với dự án tiền nhiệm là **cắt đứt**; **chưa dùng Redis**, một instance, bộ khoá cookie trong PostgreSQL ([`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md)); auth dùng **cookie** hai origin khác nhau ([`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md)); hook `Stop` **đã thi công** ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8).

> Hai quyết định trong số đó **lật lại** thứ chính tài liệu này từng viết: multi-tenant chuyển từ "ngoài phạm vi v1" thành "làm ngay", và cơ chế gọi `core-reviewer` chuyển từ niềm tin sang cưỡng chế bằng hook. Chỗ nào trong file này còn nói theo bản cũ thì bản mới thắng.
