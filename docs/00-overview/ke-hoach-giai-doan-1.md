---
kind: lich-su
scope: du-an
verified: chua-doi-chieu
---

# Kế hoạch giai đoạn 1 — chỉ `.claude/` + `docs/`

> # 🗄️ TÀI LIỆU LỊCH SỬ
>
> **File này mô tả trạng thái QUÁ KHỨ. Đường dẫn, con số và nhận định trong đây cố ý không còn đúng — đừng đọc nó để biết hiện trạng, và đừng trích dẫn nó làm căn cứ.**
>
> | Trong file này | Thực tế hiện nay |
> | --- | --- |
> | Cây thư mục và số file từng khu | Đếm bằng lệnh — cây trong file này là cây của ngày lập kế hoạch |
> | Danh sách skill dự kiến | `ls .claude/skills` là danh sách thật |
> | Mô hình phục vụ FE↔API | **Hai origin khác nhau** — [`../adr/0015-fe-va-api-khac-nguon.md`](../adr/0015-fe-va-api-khac-nguon.md) |
>
> Nguồn sống: [`../README.md`](../README.md) · [`../RULES.md`](../RULES.md) · [`../adr/README.md`](../adr/README.md)


> **Ngày:** 2026-09-08 · **Phạm vi:** KHÔNG viết `src/`. Chỉ dựng tầng tri thức và tầng agent.
> `src/` sẽ được phát triển ở giai đoạn 2, **bám theo `docs/`**.
> Đọc kèm: [`../audit/2026-09-08-audit-platformmanager.md`](../audit/2026-09-08-audit-platformmanager.md) (điểm tốt/xấu), [`lo-trinh-va-nguon.md`](lo-trinh-va-nguon.md) (nguồn di trú).

---

## 0. Mười hai quyết định đã chốt

Đây là đầu vào bắt buộc cho mọi tài liệu viết ở giai đoạn 1.

| # | Chủ đề | Quyết định | Khác PlatformManager? |
|---|---|---|---|
| 1 | Phạm vi giai đoạn 1 | Chỉ `.claude/` + `docs/`; `src/` làm sau, theo doc | — |
| 2 | Layout Core | **5 project**: `Domain` · `Application` · `Infrastructure` · **`Web`** · `Contracts` | ✅ Khác — nay có 3, hạ tầng web nằm ở host |
| 3 | Identity | **Giữ ASP.NET Core Identity** (`AppUser : IdentityUser<Guid>`) | Giữ nguyên |
| 4 | Cơ chế lỗi | **Result thuần** — Domain KHÔNG ném exception nghiệp vụ | ✅ Khác — nay là lai exception + envelope, cầu nối bằng reflection |
| 5 | Phân quyền | **Bỏ hardcode role**, chỉ dùng permission. Role là dữ liệu trong DB | ✅ Khác — nay hardcode 3 role trong `Core.Application` |
| 6 | Pipeline behavior | **Validation + Transaction** (không Logging/Performance/Caching ở v1) | ✅ Khác — nay thiếu Transaction |
| 7 | Frontend | **Giữ cấu trúc thư mục** `core/` `shared/` `platform/`, siết ESLint | Giữ nguyên |
| 8 | Agent | **8 agent**: 4 port + 4 mới (BA, Test, Tech-writer, Architect) | ✅ Khác — nay có 4 |
| 9 | CI | **GitHub Actions** chạy đủ 3 cổng | ✅ Khác — nay cố ý không có CI |
| 10 | Migration | **Core sở hữu migration của bảng Core** | ✅ Khác — nay host sở hữu, có ArchTest ép giữ |
| 11 | Áp schema | Chạy tay, **nhưng phải có đường dẫn cố định + thứ tự chạy ghi rõ** để dev khác chạy được | ✅ Khác — nay chỉ nói "tự chạy tay" |
| 12 | Comment trong code | **Tối thiểu.** Kết quả audit ghi vào `docs/audit/`, KHÔNG ghi vào file nội dung | ✅ Khác — nay comment tới >60% file |

> **Quyết định 12 có hệ quả trực tiếp lên giai đoạn 1:** nó tạo ra một khu mới `docs/audit/` mà PlatformManager không có, và nó là lý do các tài liệu viết ở đây phải giữ được lý do thiết kế — vì code sẽ không còn giữ nữa.

---

## 1. Phần DOC — nền của mọi phần khác

### 1.1 Cấu trúc `docs/`

Kế thừa cấu trúc đã chứng minh của PlatformManager (`quy-uoc/` + `wiki-core/`), thêm 2 khu mới:

```
docs/
├─ README.md                 ← MỤC LỤC + BẢNG TRẠNG THÁI (✅ sống / 🚧 đang thi công / ⚠️ đã lệch / 🗄️ lịch sử)
├─ RULES.md                  ← rule dạng MUST/MUST NOT, mỗi rule kèm cột "ép bằng gì"
├─ kien-truc-core-module.md  ← ranh giới Core ↔ Module (file chủ)
│
├─ quy-uoc/                  ← QUY ƯỚC THI CÔNG (12 file, port + sửa)
│  ├─ be-architecture.md · be-api-controller.md · be-cqrs-handler.md
│  ├─ be-entity-domain.md · be-performance.md
│  ├─ fe-architecture.md · fe-api-client.md · fe-routing-guard.md · fe-ui-conventions.md
│  ├─ repo-artifact.md · tieu-chi-review.md · README.md
│
├─ wiki-core/                ← TRI THỨC NỀN
│  ├─ be/  kiến thức nền backend
│  ├─ fe/  kiến thức nền frontend
│  └─ fe/trien-khai/ 00..05                    (6 file)
│
├─ contracts/                ← HỢP ĐỒNG API Core: auth · users · permissions · meta-menu
├─ database/                 ← schema Core · chính sách migration · thứ tự chạy script
├─ Design/                   ← nguồn giao diện DUY NHẤT: Templates/ · Components/ · Tokens
├─ adr/                      ← 🆕 ADR đánh số. `wiki-core/be/08-adr-practice.md` có luật, chưa có chỗ chứa
└─ audit/                    ← 🆕 kết quả audit + postmortem sự cố (quyết định 12)
```

### 1.2 Ba khoá frontmatter — giữ nguyên, bắt buộc mọi file

| Khoá | Giá trị | Trả lời |
|---|---|---|
| `kind` | `luat` · `tham-chieu` · `quyet-dinh` · `lich-su` | Code có phải tuân file này không? |
| `scope` | `core` · `du-an` | File có đi theo khi tách sang dự án khác không? |
| `verified` | `YYYY-MM-DD` · `chua-doi-chieu` · `khong-ap-dung` | Lần cuối đối chiếu với source là bao giờ? |

Giai đoạn 1 chưa có `src/` nên **phần lớn file sẽ mang `verified: chua-doi-chieu`** — đó là giá trị trung thực, không phải nợ. Con số này chỉ giảm được ở giai đoạn 2.

### 1.3 Khu mới `docs/audit/` — hệ quả của quyết định 12

Vì code sẽ không còn giữ lịch sử sự cố, khu này phải giữ thay. Mỗi mục:

```
docs/audit/YYYY-MM-DD-<slug>.md   — hiện tượng · nguyên nhân gốc · cách vá · cổng nào lẽ ra phải bắt
```

Code chỉ được để lại **một dòng** trỏ tới nó, không kể lại nội dung.

Hai bài học của PlatformManager nên chép sang ngay làm mẫu (chúng là bằng chứng thật, không phải giả định):

- Sự cố reflection ở `ExceptionHandlingBehavior` (2026-09-05) — hỏng đúng nhánh lỗi, chỉ lộ ở Production.
- Sự cố `fe-gate.sh` không tồn tại (2026-08-23) — 3 cổng FE không chạy suốt thời gian dài, tài liệu vẫn ghi bình thường.

### 1.4 Tài liệu phải viết mới / sửa nặng

Vì 6 quyết định lệch khỏi PlatformManager, các file sau **không port thẳng được**:

| File | Vì sao phải sửa |
|---|---|
| `kien-truc-core-module.md` | Layout đổi 3 → **5 project**, có `Core.Web` |
| `quy-uoc/be-architecture.md` | Thêm tầng `Web`, định nghĩa cái gì thuộc `Web` vs `Api` host |
| `quy-uoc/be-cqrs-handler.md` | **Result thuần** — bỏ toàn bộ phần `DomainException`/`ConflictException` |
| `quy-uoc/be-entity-domain.md` | Entity method trả `Result`, không ném |
| `wiki-core/be/02-identity-auth.md` | **Bỏ role hardcode**, chỉ permission; cách seed role cho dự án mới |
| `wiki-core/be/13-core-data-migration.md` | **Core sở hữu migration**; đường dẫn + thứ tự chạy script (quyết định 11) |
| `wiki-core/be/16-i18n-va-ma-loi.md` | Error catalog theo Result, bỏ ánh xạ từ loại exception |
| `wiki-core/be/04-testing-strategy.md` | Thêm luật ArchTest cho `Core.Web`; bỏ ArchTest ép migration ở host |

---

## 2. Phần BACKEND — doc quy định gì

### 2.1 Năm project và ranh giới

> 📖 **Danh sách project, thứ mỗi project chứa và cấm chứa, chiều tham chiếu: đọc
> [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2.**
>
> Bản trước của mục này giữ một bản sao đầy đủ, và nó **đã lệch**: nó ghi `Core.Infrastructure`
> chứa Redis, trong khi [`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md)
> đã chốt v1 **không** dùng Redis. Đúng khuôn [`../OWNERSHIP.md`](../OWNERSHIP.md) mô tả — một
> kế hoạch chép lại bảng ranh giới thì bảng đó đóng băng ở thời điểm viết kế hoạch, còn ranh
> giới thật thì đi tiếp.

Phần thuộc riêng kế hoạch này: **`Core.Web` và `Core.Contracts` là hai project mới** so với
hình dạng của dự án tiền nhiệm — chúng là công phải làm ở giai đoạn 2, không phải thứ đang có.

**Host (`Api`) sau khi tách phải dưới 50 dòng** — chỉ `AddCore()`, `UseCore()`, đăng ký module.

ArchTest cần khai ở doc (viết code ở giai đoạn 2):
- `Core.Domain` có **0 package reference**
- `Core.Application` KHÔNG tham chiếu EF Core / ASP.NET Core
- `Core.Web` KHÔNG chứa chuỗi tên nghiệp vụ
- `Core.*` KHÔNG tham chiếu assembly `Modules.*`
- `Modules.A` KHÔNG tham chiếu `Modules.B`

### 2.2 Result thuần — quy ước phải ghi rõ

```
Domain:      public Result<Order> Approve()   ← trả về, KHÔNG ném
Application: Task<Result<OrderDto>> Handle()  ← truyền Result lên
Core.Web:    Result → HTTP status + envelope  ← ánh xạ duy nhất ở đây
```

Doc phải quy định:

1. **`Error` là gì** — mã, thông điệp khuôn, loại (`Validation` / `NotFound` / `Conflict` / `Forbidden` / `BusinessRule`).
2. **Ánh xạ loại lỗi → HTTP** — nằm đúng **một chỗ** trong `Core.Web`. Đây là chỗ thay thế `ExceptionHandlingBehavior` cũ, và **không dùng reflection**.
3. **Error catalog tập trung** — giữ luật cũ đã tốt: `ErrorDescriptor` không được dựng từ chuỗi literal ngoài catalog (`ErrorCodeSourceTests`).
4. **Exception còn dùng khi nào** — chỉ cho lỗi ngoài dự kiến (mất DB, bug). Không cho luồng nghiệp vụ.
5. **i18n** — giữ nguyên thiết kế đã tốt của PlatformManager: mã lỗi tách khỏi câu hiển thị, tham số truyền theo **tên** chứ không theo thứ tự.

### 2.3 Pipeline behavior — đúng 2

```
Request → Validation → Transaction(chỉ command) → Handler
```

- `ValidationBehavior` — FluentValidation, trả `Result` lỗi validate, không ném.
- `TransactionBehavior` — bọc **command** trong transaction; query không đi qua. Đây là thứ PlatformManager thiếu và là rủi ro ghi dở dang.

Doc ghi rõ **vì sao chưa có** Logging/Performance/Caching: chưa có vấn đề thật, thêm sau khi đo được. Ghi ra để người sau không tưởng là bỏ sót.

### 2.4 Phân quyền — bỏ role hardcode

- Core chỉ biết **permission** (chuỗi định danh, ví dụ `core.user.write`; danh mục đầy đủ ở [`../database/schema-core.md`](../database/schema-core.md) §5.2).
- **Role là dữ liệu trong DB**, mỗi dự án tự khai. Không có hằng số role nào trong Core.
- Giữ ma trận quyền theo resource + `RequirePermissionAttribute` + `IPermissionChecker` (thiết kế này đã tốt).
- Doc phải mô tả **cơ chế seed**: dự án mới khai role + permission mặc định ở đâu, chạy thế nào.

> Cần chú ý một hệ quả: tài khoản bootstrap ban đầu phải có đường lấy toàn quyền mà không cần role cứng. Doc phải nói rõ cách làm.

### 2.5 Identity — giữ, nhưng khoanh vùng

Giữ ASP.NET Core Identity. Doc phải ghi rõ **ranh giới rò rỉ**:

- `AppUser`/`AppRole` chỉ được dùng trong `Core.Infrastructure`.
- `Core.Application` chỉ thấy `IIdentityService`, `IUserLookupService`, `IUserAdminService` — không thấy `UserManager`.
- Ghi thẳng vào doc rằng đây là **đánh đổi có ý thức**: đổi sang SSO/LDAP sau này sẽ phải sửa `Core.Infrastructure`, không sửa được bằng cấu hình.

---

## 3. Phần FRONTEND — doc quy định gì

### 3.1 Cấu trúc — giữ thư mục, siết ESLint

```
src/app/
├─ core/       tầng đáy: interceptor, auth, guard, http envelope, i18n, theme, toast, menu
├─ shared/     component dùng chung (bọc PrimeNG), directive, model
├─ platform/   màn hình Core: login, đổi mật khẩu, phân quyền, quản trị người dùng
└─ modules/    màn hình nghiệp vụ của từng dự án
```

Luật chiều phụ thuộc: `modules/` → `platform/` → `shared/` → `core/`. Không có chiều ngược.

### 3.2 Việc phải làm vì đã chọn "siết ESLint" thay vì workspace

Vì không có compiler ép ranh giới, ESLint phải gánh — và phải **không bỏ qua được**:

1. Giữ `import/no-restricted-paths` với 2 zone hiện có (`coreLayerZones`, `moduleBoundaryZones`).
2. **Cấm `// eslint-disable` cho đúng các rule ranh giới** — thêm mục vào cổng CI. Nếu không, rule chỉ là gợi ý.
3. **Bịt lỗ G8**: hôm nay `BUSINESS_MODULES` rỗng nên rule bị bỏ hẳn. Doc phải ghi rõ quy trình *"thêm module thứ hai thì phải thêm tên vào mảng này"*, và cổng phải kiểm mảng đó khớp với thư mục `modules/` thật.
4. **Bật G4 + G5** — hai gate lên kế hoạch từ đầu mà chưa bao giờ có:
   - G4: `components/` không được `inject(...Service)` lấy dữ liệu
   - G5: mọi `services/*.service.ts` phải có `.spec.ts` cạnh nó

### 3.3 Giữ nguyên những thứ đã tốt

- **Bọc PrimeNG** — đo được: chỉ 7 import `primeng/` toàn FE, đều ở `core/` hoặc `shared/data-grid`. Doc phải ghi thành luật + cổng để không trôi.
- **G12** — template `.html` không chứa chữ tiếng Việt, mọi câu đến từ `i18n/`.
- **G1 + G11** — không hex, không `rgb()/rgba()` literal trong SCSS; màu phải là token.
- Design token là nguồn, code đuổi theo.

### 3.4 Việc phải sửa

| Vấn đề đo được | Doc phải quy định |
|---|---|
| `styles.scss` **1015 dòng** | Tách theo nhóm: token màu / typography / spacing / override PrimeNG. Đặt ngưỡng dòng tối đa cho một file style |
| `quan-tri-nguoi-dung.page.ts` **560 dòng** | Ngưỡng dòng tối đa cho component; logic đẩy xuống service; tách component con |
| `shared/components/` mới có **8** trong khi Design có **28 spec** | Phân loại 28 spec: cái nào Core, cái nào nghiệp vụ. Chỉ Core mới vào `shared/` |

---

## 4. Phần DATABASE — doc quy định gì

### 4.1 Migration — Core sở hữu phần của Core

Đảo ngược so với PlatformManager (host sở hữu tất cả).

```
Core.Infrastructure/Migrations/       ← bảng Core: user, role, permission, menu
Modules/<X>/Infrastructure/Migrations/ ← bảng của module X
```

Đánh đổi phải ghi rõ vào doc: **`Core.Infrastructure` nay biết provider là PostgreSQL.** Chấp nhận được vì đã chốt Postgres và không có kế hoạch đổi. ArchTest cũ ép migration ở host phải **bỏ**, thay bằng test ép mỗi migration nằm đúng project của schema nó.

### 4.2 Áp schema — chạy tay, nhưng phải chạy được

Quyết định 11 nói rõ: giữ chạy tay, **nhưng phải có đường dẫn cụ thể để dev khác chạy được**. Doc phải trả lời đủ 5 câu:

1. **Script nằm ở đâu** — một đường dẫn cố định, ví dụ `database/scripts/`
2. **Đặt tên thế nào** — có thứ tự, ví dụ `0001__core-init.sql`, `0002__core-add-audit.sql`
3. **Chạy theo thứ tự nào** — và biết mình đang ở đâu (bảng `schema_history` hoặc tương đương)
4. **Sinh script bằng lệnh gì** — `dotnet ef migrations script --idempotent` từ migration nào tới migration nào
5. **Làm sao biết DB đã lệch model** — bắt buộc, vì đây là điểm yếu của cách chạy tay

Với câu 5, khuyến nghị: app khi khởi động gọi `Database.GetPendingMigrations()`, có migration chưa áp thì **từ chối khởi động** kèm thông báo nêu đúng tên migration còn thiếu và đường dẫn script cần chạy. Im lặng chạy tiếp với DB lệch là cách hỏng tệ nhất.

### 4.3 Ranh giới schema

- Mỗi module một schema Postgres riêng; Core dùng schema `core`.
- **Cấm FK vật lý xuyên schema** — tham chiếu bằng ID.
- Giữ `SchemaBoundaryTests` (mỗi entity nằm đúng schema của phía nó).
- Giữ soft-delete query filter tự động cho mọi `BaseEntity` (thiết kế này đã tốt: một vòng lặp trong `OnModelCreating`, không khai lẻ ở từng configuration).

---

## 5. Phần AGENT — 8 agent

### 5.1 Bốn agent port từ PlatformManager

| Agent | Sửa gì khi port |
|---|---|
| `backend-expert` | Bảng định tuyến thêm `Core.Web`; bỏ mọi hướng dẫn về `DomainException` (nay Result thuần); gỡ tên sản phẩm |
| `frontend-expert` | Thêm luật cấm `eslint-disable` cho rule ranh giới; gỡ tên sản phẩm |
| `core-reviewer` | Giữ nguyên 3 đặc điểm cốt lõi: **không có quyền `Edit`**, context riêng, đọc theo bảng định tuyến. Thêm mục review `docs/audit/` |
| `design-expert` | Gần như port thẳng — 9 skill `design-*` đã thuần Core |

### 5.2 Bốn agent mới

| Agent | Vai trò | Đầu vào | Đầu ra | Nguyên tắc |
|---|---|---|---|---|
| `ba-analyst` | Yêu cầu thô → US + AC + BR | Mô tả nghiệp vụ | `spec/<feature>/business-rules.md` | **Không tự bịa nghiệp vụ.** Không rõ thì hỏi, không suy diễn |
| `test-engineer` | Sinh unit + integration test, tìm ca biên | Code + AC | File test | **Tách khỏi dev agent** — người viết code không tự viết toàn bộ test cho mình |
| `tech-writer` | TechDoc / UserGuide / UnitTest doc | US + code | Bộ tài liệu bàn giao | Port từ skill `em-ti-pi` sẵn có ở cấp global |
| `architect` | ADR + phản biện thiết kế | Đề xuất thay đổi | `docs/adr/NNNN-*.md` | Được quyền **nói không**. Mọi PR đụng `Core/` phải qua đây |

### 5.3 Luật chung cho mọi agent — giữ nguyên của PlatformManager

Đây là tài sản giá trị nhất, port nguyên văn:

1. **Không chạy lệnh git ghi** — cưỡng chế bằng `settings.json` → `permissions.deny`, không bằng câu văn.
2. **`.claude/` chỉ chứa quy trình, `docs/` chứa tri thức.** Phép thử: *".claude không được chứa câu nào có thể trở thành SAI khi code thay đổi."* Hệ quả cứng: file `.claude/` không được chứa code block ngôn ngữ lập trình.
3. **Chiều cập nhật một chiều** — nội dung vào `docs/`, chỉ **đường dẫn** vào `.claude/`. Không bao giờ chép nội dung ngược lại.
4. **Đọc theo bảng định tuyến**, không đọc cả wiki. Lý do đã trả giá: corpus 780 KB giết 3 lượt review liên tiếp.
5. **Một lượt = một phạm vi** (BE hoặc FE, không bao giờ cả hai).

---

## 6. Phần SKILL

### 6.1 Phân loại theo thời điểm viết được

| Nhóm | Skill | Viết được ở giai đoạn 1? |
|---|---|---|
| **Design** | 9 skill `design-*` | ✅ Port thẳng |
| **Điều phối** | `feature-kickoff` | ✅ Port, thêm nhánh gọi `ba-analyst` và `test-engineer` |
| **Wrapper agent** | `backend-expert`, `frontend-expert`, `core-reviewer` | ✅ Port |
| **Đọc & kiểm** | `/doc-sync`, `/arch-check`, `/review-pr` | ✅ Viết mới — chỉ đọc, không cần `src/` |
| **Tài liệu** | `/ba-story`, `/tech-doc`, `/adr-new` | ✅ Viết mới |
| **Sinh code** | `/core-new-module`, `/core-new-usecase`, `/core-new-entity`, `/fe-new-feature` | ❌ **Giai đoạn 2** |

### 6.2 Vì sao nhóm "sinh code" phải đợi

Đã giải thích ở [`lo-trinh-va-nguon.md`](lo-trinh-va-nguon.md) §3.2. Tóm tắt: skill scaffold là máy photocopy, nó cần bản gốc. Giai đoạn 1 chưa có `src/` nên chưa có bản gốc nào để sao.

Viết trước sẽ sinh ra skill không ai kiểm chứng được — và đúng loại lỗi này đã xảy ra ở PlatformManager: `backend-expert.md` từng chứa cây thư mục 10 project **sai**, lọt qua mọi cổng cho tới khi có người đọc.

### 6.3 Skill mới `/doc-sync` — quan trọng nhất ở giai đoạn 1

Vì giai đoạn 1 chỉ sinh ra tài liệu, cần một skill kiểm chính tài liệu đó:

- Mọi file có đủ 3 khoá frontmatter chưa
- Có file nào là "file chủ thứ hai" của một chủ đề đã có chủ không (luật một chủ đề một file chủ)
- Bảng định tuyến trong `.claude/` có trỏ đúng file còn sống không
- **Chú thích không được trỏ vào file `kind: lich-su`** — bịt lỗ hổng đã tìm ra ở `Roles.cs`

---

## 7. Phần CỔNG & CI

### 7.1 `check-docs.sh` — port 12 mục, thêm 2

Port nguyên 12 mục hiện có. Sửa mục §1 (bảng git) và §7 (`doc/Prototype/` — đặc thù repo cũ). Thêm:

| Mục mới | Bắt gì |
|---|---|
| 13 | Chú thích trong `src/` (giai đoạn 2) không trỏ vào file `kind: lich-su` |
| 14 | Mọi rule trong `RULES.md` có khai cột "ép bằng gì"; rule chưa có cổng phải nằm trong danh sách nợ nhìn thấy được |

### 7.2 CI — GitHub Actions

Giai đoạn 1 chưa có `src/`, nên pipeline khởi đầu chỉ có 1 job:

```
on: [pull_request]
  → bash .claude/check-docs.sh
```

Giai đoạn 2 thêm `dotnet test` và `fe-gate.sh` + bộ lệnh `ng`.

Điểm phải làm đúng ngay: **mọi lệnh của cổng phải nằm trong `permissions.allow` của `settings.json`.** Một cổng bị prompt chặn là một cổng trên thực tế không ai chạy — PlatformManager đã ghi nhận đúng cơ chế hỏng này.

---

## 8. Lộ trình giai đoạn 1

| Bước | Nội dung | Effort | Nghiệm thu |
|---|---|---|---|
| **D0** | Dựng khung `docs/` + `RULES.md` + `README.md` (mục lục + bảng trạng thái) + 3 khoá frontmatter | 3 ngày | Khung đủ chỗ cho mọi file sắp port |
| **D1** | Port 70 file `scope: core`, đổi tên sản phẩm, gắn lại frontmatter | 1 tuần | Không còn chuỗi `PlatformManager` nào ngoài phần trích dẫn lịch sử |
| **D2** | Viết lại 8 tài liệu lệch quyết định (§1.4) | 1 tuần | Không còn mâu thuẫn giữa doc và 12 quyết định |
| **D3** | Port `check-docs.sh` + thêm 2 mục + dựng CI | 3 ngày | CI xanh trên PR; cổng đỏ thật khi cố tình vi phạm |
| **D4** | Port 4 agent + viết 4 agent mới | 1 tuần | Mỗi agent có bảng định tuyến trỏ file có thật |
| **D5** | Port 12 skill + viết skill đọc/kiểm và skill tài liệu | 1 tuần | `/doc-sync` chạy được và tìm ra ít nhất 1 vấn đề thật |
| **D6** | Lập `docs/audit/` + chép 2 bài học mẫu + ghi ADR cho 12 quyết định | 3 ngày | 12 quyết định đều có ADR đánh số |

**Tổng: khoảng 5 tuần-người.**

Đường găng: `D0 → D1 → D2 → D4 → D5`. D3 và D6 chen vào được bất cứ lúc nào sau D1.

---

## 9. Câu hỏi còn mở — ĐÃ ĐÓNG HẾT (2026-09-08)

Năm câu từng chặn giai đoạn 2 nay đều có đáp án. Giữ mục này để truy nguyên.

| Câu | Đáp án | Ghi ở đâu |
|---|---|---|
| Multi-tenant | **Có, làm ngay** — nhiều cơ quan chung một bản cài, cột phân biệt, cách ly tuyệt đối, một tài khoản một tenant | [`../adr/0013-multi-tenant.md`](../adr/0013-multi-tenant.md) · [`../RULES.md`](../RULES.md) §9 |
| Quan hệ với dự án tiền nhiệm | **Cắt đứt**, chỉ tham khảo — nó là bản Core cũ nhiều lỗi | [`lo-trinh-va-nguon.md`](lo-trinh-va-nguon.md) §4.6 |
| Redis / message broker | **Chưa dùng.** Một instance; bộ khoá bảo vệ dữ liệu để trong PostgreSQL ngay từ đầu | [`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md) |
| Auth cookie hay JWT | **Cookie**, và FE với API ở **hai origin khác nhau** — chấp nhận nhóm vấn đề cookie chéo nguồn, đổi lấy việc không cần reverse proxy ở mọi môi trường | [`../adr/0004-giu-aspnet-identity.md`](../adr/0004-giu-aspnet-identity.md) |
| Hook cho `core-reviewer` | **Đã thi công.** Hook `Stop` chạy cổng và chặn lượt; không còn phụ thuộc agent nhớ gọi | [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8 |

**Câu hỏi mới sinh ra từ các quyết định trên**, chưa chặn việc gì nhưng cần theo dõi:

1. **Một instance là một điểm hỏng duy nhất**, và nay nhiều cơ quan cùng phụ thuộc vào nó. Có cam kết mức độ sẵn sàng nào không? Nếu có thì phải thêm instance, và bộ khoá đã sẵn sàng cho việc đó.
2. **Tenant đầu tiên được tạo bằng cách nào** — có màn quản trị hệ thống riêng, hay chạy lệnh seed? Ảnh hưởng thiết kế màn hình.
3. **Số tenant dự kiến** — vài đơn vị hay vài chục? Con số này quyết định lúc nào mô hình cột phân biệt chạm trần.
