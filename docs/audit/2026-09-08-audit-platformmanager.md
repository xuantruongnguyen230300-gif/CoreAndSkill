---
kind: tham-chieu
scope: du-an
verified: khong-ap-dung
---

# Audit PlatformManager — giữ gì, bỏ gì, hỏi gì

> 🛑 **MỌI ĐƯỜNG DẪN TRONG FILE NÀY THUỘC REPO `D:\Manager\PlatformManager`, KHÔNG THUỘC REPO NÀY.**
>
> Đây là ảnh chụp một dự án khác tại một thời điểm, không phải luật của CoreAndSkill — vì vậy nó mang `kind: tham-chieu` ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §9). Một tài liệu mô tả dự án khác mà bị đọc như luật sẽ khiến agent báo *"doc yêu cầu X, code không có X"* cho những X chưa bao giờ là luật ở đây.
>
> Cụ thể: `.claude/CLAUDE.md`, `.claude/settings.json`, `scripts/fe-gate.sh` nhắc bên dưới là **của PlatformManager**. Trạng thái của chính repo này khác — ví dụ CoreAndSkill **có** CI ([`../adr/0011-ci-github-actions.md`](../adr/0011-ci-github-actions.md)), trong khi mục S4 bên dưới ghi PlatformManager không có.
>
> File này trước nằm ở `docs/00-overview/`. Nó được chuyển vào khu audit vì nó **là** một lượt audit, và tài liệu nội dung không nên chứa audit ([`README.md`](README.md) §2).

> **Ngày audit:** 2026-09-08 · **Đối tượng:** `D:\Manager\PlatformManager`
> Mọi phát hiện dưới đây đều neo bằng `file:dòng` hoặc lệnh đếm lại được.
> Số liệu gốc: BE 191 file `.cs` / 20.212 dòng · FE 101 file `.ts` / 16.187 dòng · 7 project · 18 file ArchTest.

---

## 0. Kết luận một câu

PlatformManager có **kỷ luật kiểm chứng thuộc hàng hiếm** (ArchTest tự kiểm chính detector của nó, cổng tài liệu đo được tính trung thực) nhưng **ranh giới Core chưa hoàn thành**: phần lớn hạ tầng web nằm trong host, nên Core hôm nay **không tự đứng được** — dự án thứ hai phải copy-paste, không phải lắp vào.

CoreAndSkill nên giữ nguyên tầng kỷ luật, và làm nốt phần ranh giới mà PlatformManager đã chốt nhưng chưa thi công.

---

## 1. Điểm TỐT — bê nguyên, đừng nghĩ lại

| # | Điểm | Bằng chứng | Vì sao hiếm |
|---|---|---|---|
| G1 | **Meta-test cho ArchTest** — mỗi detector có test kiểm chính nó | `BannedDependencyTests.Detectors_Catch_RealViolations_ButIgnore_Comments`, `ErrorCatalogTests.DuplicateDetection_Catches_ACodeDeclaredTwice`, `MigrationsLocationTests.Detector_Catches_RealMigrationShapes_ButIgnores_CommentsAndConfigCalls` | Cổng hỏng âm thầm là cách hỏng tệ nhất. Hầu như không đội nào test cái test |
| G2 | **Cổng đo tính trung thực của tài liệu** | `check-docs.sh` mục 7: trích dẫn `file:dòng` phải nằm trong file. Bắt được 2 lời nói dối ngay ngày thêm (2026-08-23) | Tài liệu bịa bằng chứng thường bịa luôn số dòng — mà số dòng thì máy đếm được |
| G3 | **3 khoá `kind` / `scope` / `verified`** ở frontmatter mọi file `doc/` | 79 file `scope: core`, 61 `du-an`; 126 `luat`, 9 `tham-chieu`, 5 `lich-su` | Biến 3 câu hỏi phải-đọc-mới-biết thành 3 câu máy đọc được. Đây là bản kê khai di trú, có sẵn |
| G4 | **`verified: chua-doi-chieu` là giá trị trung thực** | `CLAUDE.md` §9 của PlatformManager | Chống đúng cám dỗ đóng dấu ngày cho file chưa ai kiểm |
| G5 | **Phép thử ranh giới `.claude` ↔ `doc`** — *".claude không được chứa câu nào có thể trở thành SAI khi code đổi"* | `CLAUDE.md` §2 của PlatformManager, cưỡng chế bằng cổng tài liệu của repo đó | Phép thử trả lời được bằng có/không, không phải bằng cảm tính |
| G6 | **`core-reviewer` không có quyền `Edit`** + context riêng + đọc theo bảng định tuyến | file agent `core-reviewer` của PlatformManager, frontmatter `tools:` | Người kiểm không sửa thứ mình kiểm. Corpus 780 KB → 264 KB nhờ trỏ đường thay vì chép |
| G7 | **Comment ghi lại *đã trả giá gì*** | `ExceptionHandlingBehavior.cs` — chép lại nguyên sự cố 2026-09-05; `Program.cs:431` — lỗi antiforgery do core-reviewer tìm ra | Bài học không bị xoá khi người viết rời đi |
| G8 | **PrimeNG đã bọc đúng** | `grep -rn "from 'primeng/" src/FE/src/app` → **7 kết quả**, đều ở `core/i18n`, `app.config.ts`, `shared/components/data-grid`. **Không có** page nghiệp vụ nào import trực tiếp | Đây là câu hỏi tôi để mở ở báo cáo trước — nay đã có đáp án: **đã bọc tốt** |
| G9 | **G12: template `.html` không được chứa chữ tiếng Việt** | cổng FE của PlatformManager, section G12, dò dấu thanh | Ép i18n từ gốc thay vì "để sau" |
| G10 | **`permissions.deny` chặn thẳng lệnh git ghi** + `dotnet ef database drop` + `npm publish` | `settings.json` của PlatformManager | Tầng duy nhất agent không vượt được |
| G11 | **IntegrationTests chạy Postgres thật** | `PostgresFixture.cs` (243 dòng), 470 dòng test riêng cho `ResourcePermissionEndpoint` | Không mock DB cho thứ mà lỗi nằm ở DB |

---

## 2. Điểm KHÔNG TỐT — Backend

### B1 🔴 Không có project `Core.Web` — hạ tầng web nằm trong host

**Đây là vấn đề nghiêm trọng nhất.**

`PlatformManager.Api` (host lẽ ra phải mỏng) đang giữ:

```
Common/  ApiControllerBase.cs · ApiStatusCodeEnvelopeMiddleware.cs · GlobalExceptionHandler.cs
         HttpContextCurrentUser.cs · LoginUserNameRateLimitMiddleware.cs
         ModelBindingProblemFactory.cs · OriginValidationMiddleware.cs
         TraceIdLogEnrichmentMiddleware.cs · CorsPolicyOptions.cs · SeedCommand.cs
Controllers/  AuthController.cs · UsersController.cs · PermissionsController.cs · MetaController.cs
```

Toàn bộ 14 file này là **mối quan tâm của Core** (auth, user, permission, menu, envelope, CORS, rate limit) nhưng nằm ngoài Core.

**Hệ quả đo được:** dự án thứ hai muốn dùng Core phải copy-paste **629 dòng `Program.cs` + 10 file `Common/` + 4 controller**. Đó không phải tái sử dụng, đó là nhân bản.

Chính họ đã biết: `doc/kien-truc-core-module.md` ghi đích đến là tách thêm `Core.Common` + `Core.Persistence` + `Core.Api` → 6 project, trạng thái **🚧 ĐÃ CHỐT — ĐANG THI CÔNG**. Việc chưa làm.

→ **Câu hỏi Q-BE1**

### B2 🟠 `Program.cs` 629 dòng

Host "mỏng" mà dài hơn mọi file Core. Không tách thành extension method theo nhóm quan tâm (`AddCoreAuth()`, `AddCoreRateLimit()`, `UseCorePipeline()`).

Đi kèm B1: sau khi tách `Core.Web`, `Program.cs` của dự án mới nên còn **dưới 50 dòng**.

### B3 🔴 Chỉ có 2 pipeline behavior — thiếu Transaction

`Core.Application/DependencyInjection.cs` đăng ký đúng 2: `ExceptionHandlingBehavior`, `ValidationBehavior`.

Thiếu: **Transaction**, Logging, Performance, Caching, Authorization.

`IUnitOfWork` ghi rõ *"Handler own SaveChanges, đúng một lần, ở cuối"*. Nghĩa là **không có transaction bao ngoài**. Một command chạm 2 aggregate, nếu phần 2 lỗi sau khi phần 1 đã `SaveChanges` → **ghi dở dang, không rollback**.

Ngoại lệ `DiscardTrackedChanges()` cho thấy họ đã gặp đúng lớp vấn đề này ở luồng import theo dòng, và vá bằng cách xử lý thủ công thay vì transaction.

→ **Câu hỏi Q-BE2**

### B4 🔴 Reflection trong đường xử lý lỗi

`ExceptionHandlingBehavior.BuildErrorResponse` dựng envelope bằng `Type.GetMethod(...)` + `MethodInfo.Invoke(...)`, với **danh sách kiểu tham số hardcode**.

Đã nổ thật, theo chính comment trong file:

> *"Cái bẫy đã nổ THẬT một lần (2026-09-05). Khi đó GetMethod trả null, `method!` biến MỌI DomainException thành NullReferenceException — hỏng ở đúng nhánh lỗi, không có lỗi biên dịch, không ArchTest nào chạm tới, nên chỉ lộ ra ở Production."*

Họ vá bằng `?? throw` + 1 unit test. **Nguyên nhân gốc vẫn còn**: mỗi lần đổi chữ ký `ApiResult<T>.BusinessError` là một lần phải nhớ sửa danh sách kiểu ở file khác, và compiler không nhắc.

Giải pháp không cần reflection: cho `ICommand<T>`/`IQuery<T>` ràng buộc `TResponse : IApiResult<T>`, hoặc dùng một interface factory phi generic. Compiler bắt được thay vì runtime.

→ **Câu hỏi Q-BE3**

### B5 🟠 Hai cơ chế lỗi song song

Domain ném `DomainException` / `ConflictException`; Application trả `IApiResult<T>`. `ExceptionHandlingBehavior` làm cầu nối — và cầu nối đó chính là B4.

Không sai tuyệt đối (dùng exception để thoát sớm khỏi entity method là hợp lý), nhưng nó là **nguồn sinh ra** sự phức tạp ở B4.

→ gộp vào **Q-BE3**

### B6 🔴 `AppUser` / `AppRole` nằm ở Infrastructure, không ở Domain

```
Core.Infrastructure/Identity/AppUser.cs:11   class AppUser : IdentityUser<Guid>
Core.Infrastructure/Identity/AppRole.cs:6    class AppRole : IdentityRole<Guid>
```

Và `BaseEntity` ghi rõ: *"KHÔNG dùng cho AppUser/AppRole — đó là entity của ASP.NET Core Identity, tự quản lý vòng đời riêng."*

**Hệ quả:** "người dùng" — khái niệm trung tâm nhất của Core — không thuộc Domain, mà thuộc Infrastructure và bị khoá vào ASP.NET Core Identity.

- Mọi dự án dùng Core **buộc phải** dùng ASP.NET Core Identity.
- Đổi sang SSO / LDAP / identity provider ngoài → phải sửa Core.
- Domain không thể có luật nghiệp vụ nào về user mà không đi qua Infrastructure.

→ **Câu hỏi Q-BE4**

### B7 🔴 Hardcode 3 role vào Core

`Core.Application/Common/Roles.cs`:

```csharp
public const string SuperAdmin = "SuperAdmin";
public const string Admin = "Admin";
public const string User = "User";
```

Đây là **nghiệp vụ nằm trong Core**. Mọi dự án dùng Core buộc phải có đúng 3 role này, đúng tên này.

Mâu thuẫn với chính thiết kế của họ: hệ đã có **permission-based** (`RequirePermissionAttribute`, `IPermissionChecker`, ma trận quyền theo resource). Có ma trận quyền rồi thì role cứng là thừa và là thứ trói.

Thêm một lỗi phụ: comment của file trỏ về `doc/ke-hoach-xay-lai-corebase.md` — file mang `kind: lich-su`, tức **code đang trỏ vào tài liệu đã chết**. Đúng loại lỗi `check-docs.sh` mục 10 sinh ra để bắt, nhưng mục đó chỉ kiểm đường dẫn có tồn tại, không kiểm file đó còn sống không.

→ **Câu hỏi Q-BE5**

### B8 🟠 Migration nằm trong host

`PlatformManager.Api/Persistence/Migrations/` — và có ArchTest **ép giữ nguyên**: `MigrationsLocationTests.Host_MustOwn_TheMigrationsAndSnapshot`, `CoreProjects_MustNotContain_EfMigrationSourceFiles`.

Có lý do: migration phụ thuộc provider, mà Core không nên biết provider. Nhưng hệ quả là **dự án mới không thừa kế được migration cho bảng Core** — phải tự sinh lại từ đầu.

Họ đã bù bằng `Core_MustStillShip_TheBaselineSqlArtifact` (Core vẫn ship file SQL baseline). Cần xem cách này có đủ không.

→ **Câu hỏi Q-DB1**

### B9 🟡 Thiếu Central Package Management, `Directory.Build.props` quá mỏng

- **Không có `Directory.Packages.props`** → 7 `.csproj` tự khai version, dễ lệch.
- `Directory.Build.props` chỉ có `<WarningsAsErrors>Nullable</WarningsAsErrors>` — **không phải** `TreatWarningsAsErrors` đầy đủ. Warning khác vẫn lọt.

Sửa rẻ, làm ngay được.

### B10 🟡 ArchTest quét văn bản nguồn, làm méo code

`CoreMustNotKnowBusinessNameTests.CoreSource_MustNotContain_BusinessNameStringLiteral` quét **text**, không quét AST.

Bằng chứng nó làm méo code — comment thật trong `ExceptionHandlingBehavior.cs`:

> *"Luật `Core_MustNotKnowBusinessName` quét văn bản nguồn, nên `$"…{nameof(X.BusinessError)}…"` vẫn bị bắt dù đó không phải literal theo nghĩa của C#. Đặt ở đây thì tên vẫn tự cập nhật khi đổi tên phương thức, mà chuỗi thì sạch."*

Tức là **code phải viết vòng để né detector**. Đuôi vẫy chó. Họ đã bù bằng meta-test (`Detector_Ignores_Comments_And_Identifiers`) — tốt, nhưng gốc vẫn là quét text.

### B11 🟡 Mật độ comment rất cao

`ExceptionHandlingBehavior.cs`: ước lượng trên 60% là comment, nhiều đoạn kể lại lịch sử sự cố.

Đánh đổi thật: giá trị lưu giữ bài học **rất cao** (G7), nhưng chi phí đọc cũng cao — cho cả người lẫn agent (đây chính là lớp vấn đề đã giết 3 lượt `core-reviewer`). Và không cổng nào kiểm comment, nên chúng tự trôi.

→ **Câu hỏi Q-BE6**

---

## 3. Điểm KHÔNG TỐT — Frontend

### F1 🔴 Không phải Angular workspace nhiều library

`src/FE/src/app/{core,shared,platform}` chỉ là **thư mục**, không phải Angular project/library.

Ranh giới hiện chỉ do ESLint `import/no-restricted-paths` (`eslint.config.js`, 135 dòng) — **không có ranh giới cưỡng chế bằng compiler**.

**Hệ quả:**
- Không thể đóng gói `@app/core` / `@app/ui` độc lập.
- Dự án mới copy thư mục, y hệt vấn đề B1 bên BE.
- ESLint bỏ qua được bằng một dòng `// eslint-disable`; ProjectReference thì không.

→ **Câu hỏi Q-FE1**

### F2 🔴 G8 (chặn import chéo module) hôm nay là no-op

`doc/.../05-gate.md` dòng 25 ghi rõ:

> *"**Hôm nay là no-op có chủ đích (đối chiếu 2026-09-06)**: `BUSINESS_MODULES` trong `src/FE/eslint.config.js` rỗng nên cả block rule bị bỏ hẳn."*

Trung thực, có lý do (chưa có module nghiệp vụ nào). Nhưng nghĩa là: **ranh giới module ở FE chưa từng được kiểm chứng.** Ngày có module thứ hai mới biết rule có chạy không.

### F3 🟠 G4 và G5 chưa bao giờ bật

| Gate | Nội dung | Kế hoạch | Thực tế |
|---|---|---|---|
| G4 | Component dumb (`components/`) không inject service data | "Sau F4" | **chưa có** |
| G5 | Mọi `services/*.service.ts` có `.spec.ts` cạnh nó | "Sau F2" | **chưa có** |

`fe-gate.sh` chạy thật đúng 5 mục: G1, G3, G6, G11, G12. G2/G7/G8/G9 thuộc `ng lint` / `ng build`. G10 để trống có chủ đích.

### F4 🟡 `styles.scss` 1015 dòng

Một file gánh toàn bộ token + reset + override PrimeNG. Cần tách theo nhóm (token màu / typography / spacing / override).

### F5 🟡 Component quá lớn

`quan-tri-nguoi-dung.page.ts` **560 dòng**, spec đi kèm **807 dòng**. Một trang quản trị user không nên tới 560 dòng — dấu hiệu logic chưa đẩy xuống service hoặc chưa tách component con.

### F6 🟡 `shared/components/` mới có 8 component

`auth-card`, `confirm-dialog`, `data-grid`, `language-switcher`, `sidebar`, `toast`, `toolbar`, `topbar`.

Trong khi `doc/Design/.../Components/` có **28 spec**. Khoảng cách giữa spec và hiện thực là 20 component — cần phân loại cái nào thuộc Core, cái nào nghiệp vụ.

---

## 4. Điểm KHÔNG TỐT — Database

### D1 🟠 Không auto-migrate, chỉ sinh script chạy tay

Quyết định có chủ đích (`ke-hoach-xay-lai-corebase.md`): *"cách áp dụng thay đổi schema DB đổi từ auto-migrate sang chỉ sinh file script (người dùng tự chạy tay trên Postgres)"*.

Được: an toàn cho production, DBA kiểm soát được.
Mất: **không có cơ chế nào đảm bảo script đã chạy khớp với model EF hiện tại.** Lệch giữa DB thật và model là loại lỗi chỉ lộ ra lúc chạy.

→ **Câu hỏi Q-DB2**

### D2 🟠 Migration ở host (trùng B8)

### D3 🟡 Ranh giới schema chưa được kiểm chứng

Có `SchemaBoundaryTests.EveryMappedEntity_LivesInTheSchemaOfItsSide` — nhưng hôm nay **không còn module nghiệp vụ nào**, nên test này chưa từng chạy với 2 schema thật. Cùng lớp vấn đề với F2.

---

## 5. Điểm KHÔNG TỐT — Skill & Agent

### S1 🔴 Thiếu hẳn agent BA và agent Test

Hiện có **4 agent**: `backend-expert`, `frontend-expert`, `core-reviewer`, `design-expert`.

Yêu cầu bạn nêu cho CoreAndSkill gồm *"làm tài liệu như một BA"* và *"test"* — **chưa có gì phục vụ hai việc này**.

→ **Câu hỏi Q-SA1**

### S2 🔴 Không có skill sinh code nào

12 skill = 9 `design-*` + 3 wrapper agent + 1 `feature-kickoff`.

Không có `/new-module`, `/new-usecase`, `/new-entity`, `/new-fe-feature`. Đây đúng là phần phải viết mới — và phải viết **sau** khi có module mẫu (lý do: skill sinh mã chỉ viết được sau khi có một module mẫu để rút khuôn).

### S3 🟠 `core-reviewer` gọi "PROACTIVELY" — phụ thuộc LLM nhớ gọi

Frontmatter ghi *"Dùng PROACTIVELY sau khi backend-expert hoặc frontend-expert vừa hoàn thành công việc chạm tới thành phần core"*.

Đó lại là niềm tin — đúng thứ tầng verifier sinh ra để thay thế. Chuyển sang **Stop hook** thì harness chạy, không phụ thuộc model nhớ.

→ **Câu hỏi Q-SA2**

### S4 🔴 Không có CI

File luật **của PlatformManager** ghi: *"Repo không có CI (`.github/` không tồn tại, có chủ đích) — không còn máy nào chạy hộ."*

> ⚠️ Câu trên là của repo tiền nhiệm. Đừng mở `.claude/CLAUDE.md` của repo **này** để tìm nó — ở đây không có câu đó, và `.github/workflows/docs-gate.yml` thì **có tồn tại**.

Và ngay đó ghi nhận hậu quả đã xảy ra: ngày 2026-08-23 phát hiện `scripts/fe-gate.sh` **không tồn tại**, nên G1/G3/G6 *"khi đó không có gì canh"* suốt một thời gian dài, trong khi tài liệu vẫn ghi bình thường.

Với repo cá nhân: chấp nhận được. Với bộ khung nhiều dự án dùng chung: **đây là lỗ hổng lớn nhất của tầng `.claude`.**

→ **Câu hỏi Q-SA3**

### S5 🟡 `check-docs.sh` không kiểm được "tài liệu còn sống không"

Mục 10 kiểm chú thích trong `src/` trỏ `doc/` **có tồn tại**. Nhưng B7 cho thấy một chú thích trỏ đúng vào file `kind: lich-su` — đường dẫn tồn tại, cổng xanh, mà nội dung thì đã chết.

Sửa rẻ: thêm một mục — *chú thích trong `src/` không được trỏ vào file `kind: lich-su`*.

---

## 6. Bảng tổng hợp theo mức độ

| Mức | BE | FE | DB | Skill/Agent |
|---|---|---|---|---|
| 🔴 Phải sửa | B1 Core.Web · B3 Transaction · B4 Reflection · B6 User ở Infra · B7 Role hardcode | F1 Không phải workspace · F2 G8 no-op | — | S1 Thiếu BA+Test · S2 Không skill sinh code · S4 Không CI |
| 🟠 Nên sửa | B2 Program.cs 629 dòng · B5 Hai cơ chế lỗi · B8 Migration ở host | F3 G4/G5 chưa bật | D1 Không auto-migrate · D2 Migration ở host | S3 PROACTIVELY |
| 🟡 Cân nhắc | B9 Package management · B10 ArchTest quét text · B11 Comment dày | F4 styles.scss · F5 Component lớn · F6 Thiếu component | D3 Schema chưa kiểm chứng | S5 Cổng không bắt doc chết |

---

## 7. Câu hỏi cần bạn quyết

Không có câu nào tôi nên tự quyết — mỗi câu đều có đánh đổi thật.

| Mã | Câu hỏi | Ảnh hưởng |
|---|---|---|
| **Q-BE1** | Tách `Core.Web` (đưa 14 file `Common/` + `Controllers/` ra khỏi host)? | Quyết định Core có tự đứng được không |
| **Q-BE2** | Thêm `TransactionBehavior`? | Rủi ro ghi dở dang |
| **Q-BE3** | Bỏ reflection ở đường lỗi — chuyển sang Result thuần hay ràng buộc generic? | Ổn định nhánh lỗi |
| **Q-BE4** | `User` về Domain (tự quản) hay giữ ASP.NET Identity ở Infrastructure? | Quyết định Core có khoá vào Identity không |
| **Q-BE5** | Bỏ `Roles.cs` hardcode, chỉ dùng permission? | Nghiệp vụ trong Core |
| **Q-BE6** | Chính sách comment: giữ mật độ hiện tại hay tách lịch sử sự cố ra `docs/`? | Chi phí context của agent |
| **Q-FE1** | Chuyển sang Angular workspace nhiều library (`@app/core`, `@app/ui`)? | Quyết định FE Core có tái dùng được không |
| **Q-DB1** | Migration Core: giữ ở host, hay đưa về Infrastructure theo module? | Dự án mới có thừa kế được migration không |
| **Q-DB2** | Giữ "không auto-migrate"? Nếu giữ, thêm cơ chế gì để phát hiện DB lệch model? | An toàn production vs lỗi âm thầm |
| **Q-SA1** | Thêm agent BA + agent Test? | Đáp ứng yêu cầu gốc của bạn |
| **Q-SA2** | Chuyển `core-reviewer` từ "PROACTIVELY" sang Stop hook? | Verifier có chắc chạy không |
| **Q-SA3** | CoreAndSkill có CI không? | Cổng có chắc chạy không |
