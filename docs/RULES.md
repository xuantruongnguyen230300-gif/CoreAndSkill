---
kind: luat
scope: core
verified: chua-doi-chieu
---

# RULES — mọi luật của repo, kèm cột "ép bằng gì"

> **Lý do file này tồn tại nằm ở cột "Ép bằng gì".**
>
> Một luật trả lời được câu *"luật này được ép bằng cái gì?"* bằng một cổng cụ thể thì nó là **luật**. Trả lời "bằng niềm tin" thì nó là **gợi ý** — và phải nằm ở [§10 Danh sách nợ](#10-danh-sách-nợ--luật-chưa-có-cổng), nhìn thấy được, chứ không trộn lẫn vào các bảng trên.
>
> Ba mức: **MUST** (vi phạm là lỗi) · **MUST NOT** (cấm) · **SHOULD** (nên, lệch thì phải nêu lý do trong PR).

**Cột "Trạng thái"** cho biết cổng đã chạy được chưa:
`✅` = đang chạy ở giai đoạn 1 · `✅⌀` = cổng chạy nhưng **tập đầu vào hiện rỗng** · `📐` = chờ giai đoạn 2 (cần `src/`) · `🕳️` = chưa có cổng, xem §10.

> **Vì sao tách `✅⌀` khỏi `✅`.** Một mục cổng xét 0 mục vẫn "chạy", nhưng nó chưa từng chặn được gì và sẽ không chặn được gì cho tới khi có đầu vào. Gộp nó chung ký hiệu với một mục đang xét hàng nghìn dòng làm bảng này trả lời sai đúng câu hỏi nó tồn tại để trả lời: *luật nào đang thật sự được máy ép?* Chính output của cổng đã phân biệt hai ca đó — bảng phải theo kịp, không được trung thực kém hơn script.

---

## 1. Luật tài liệu — hiệu lực NGAY

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| D1 | Mọi file `docs/` và `spec/` khai đủ `kind` / `scope` / `verified` | MUST | `check-docs.sh` §12 | ✅ |
| D2 | `.claude/**/*.md` không chứa code block ngôn ngữ lập trình | MUST NOT | `check-docs.sh` §2 | ✅ |
| D3 | Mọi link markdown nội bộ resolve được | MUST | `check-docs.sh` §3 | ✅ |
| D4 | Mọi đường dẫn `docs/…`, `.claude/…`, `spec/…` được trích dẫn đều tồn tại | MUST | `check-docs.sh` §4 | ✅ |
| D5 | Trích dẫn không neo vào file bị `.gitignore` loại khỏi repo | MUST NOT | `check-docs.sh` §5 | ✅ |
| D6 | Dòng chứa `ĐÃ CÓ` / `✅ Xong` / `FIXED` / `Đã bật` phải kèm ngày đối chiếu | MUST | `check-docs.sh` §6 | ✅ |
| D7 | Trích dẫn dạng `file:dòng` phải nằm trong file | MUST | `check-docs.sh` §7 | ✅⌀ — §7 xét 0 trích dẫn, **không phải vì tài liệu sạch**: mọi trích dẫn `file:dòng` hiện có đều trỏ ra ngoài ba tiền tố `docs/`/`.claude/`/`spec/`, hoặc vào file chưa tồn tại. Đó là sự thật về **phép dò**, không phải về repo |
| D8 | Bảng định tuyến trỏ đúng chủ đề — định danh trong backtick phải có trong file đích | MUST | `check-docs.sh` §8 | ✅ |
| D9 | Tên file `.md` viết trong code span ở `.claude/` phải resolve được | MUST | `check-docs.sh` §9 | ✅ |
| D10 | Trích dẫn không trỏ vào file mang `kind: lich-su` | MUST NOT | `check-docs.sh` §13 | ✅ |
| D11 | Mọi luật ở file này khai cột "Ép bằng gì" | MUST | `check-docs.sh` §14 | ✅ |
| D12 | Bảng cấm git trong `CLAUDE.md` §1 khớp `permissions.deny` của `settings.json` | MUST | `check-docs.sh` §1 | ✅ |
| D13 | **Một nội dung có đúng MỘT nguồn đối chiếu.** Một định nghĩa (catalog, bảng ánh xạ, chữ ký kiểu) chỉ được nằm ở file chủ của nó; file khác chỉ trỏ đường | MUST | `check-docs.sh` §15 đọc bảng chủ quyền ở [`OWNERSHIP.md`](OWNERSHIP.md) §3 | ✅ cho định nghĩa đã đăng ký |
| D17 | Mọi dòng trong bảng chủ quyền phải có chuỗi định danh xuất hiện đúng ở file chủ — không nhiều hơn, không ít hơn | MUST | `check-docs.sh` §15 | ✅ |
| D16 | Không tuyên bố `CÓ THẬT` khi repo chưa có `src/` để đối chiếu | MUST NOT | `check-docs.sh` §11 — dùng **canary** vì 0 dòng khớp là trạng thái hợp lệ | ✅⌀ |
| D22 | **Định nghĩa gắn mốc `— định nghĩa gốc` phải có dòng trong sổ chủ quyền.** Chiều ngược của D13: D13/D17 đi từ sổ ra tài liệu, D22 đi từ tài liệu vào sổ | MUST | `check-docs.sh` §16 | ✅ |
| D23 | **Bảng định tuyến tự khai bằng TIÊU ĐỀ CỘT CUỐI.** Chỉ bảng có tiêu đề cột cuối thuộc tập đã chốt mới được §8 đối chiếu; bảng văn xuôi có kèm liên kết thì không | MUST | `check-docs.sh` §8 | ✅ |
| D24 | **File nội dung không kể lại bản trước của chính nó.** Bản cũ nằm trong lịch sử git; bài học có cơ chế hỏng vào [`audit/`](audit/). Miễn trừ: `audit/`, `adr/` (bối cảnh của một quyết định), file `kind: lich-su` | MUST NOT | `check-docs.sh` §17 — chỉ bắt các cụm đã biết; cách diễn đạt khác dựa vào [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §3 và review | ✅ một phần |
| D28 | **Từ vựng nghiệp vụ không được nằm ở VỊ TRÍ ĐỊNH DANH trong [`Design/Components/`](Design/Components/)** — trong code span hoặc code block, tức làm tên biến thể / tên prop / tên trường. Ví dụ nghiệp vụ trong văn xuôi và trong ô bảng thì **được phép**, kể cả ở mục `Biến thể`/`Trạng thái`/`API dự kiến`: ví dụ là thứ làm spec đọc được | MUST NOT | `check-docs.sh` §18 — quét cả dạng có dấu lẫn dạng ASCII (`kyKeToan`, `hachToan`…); có canary + bộ đếm đầu vào vì điều kiện PASS thuần phủ định | ✅ một phần — đây là **lát cắt** của D27, không phải cả D27 |
| D29 | **Một kiểu công khai chỉ được khai ở MỘT chỗ trong toàn `docs/`.** Áp cho `export interface` / `type` / `enum` / `const`; file khác chỉ được trỏ đường | MUST | `check-docs.sh` §19 — **cố ý bỏ `export class`**: mọi class trong `docs/` là ví dụ minh hoạ, và một ví dụ chạy xuyên suốt nhiều file là ưu điểm chứ không phải bản sao | ✅ |
| D30 | **Giá trị của một token chỉ sống ở [`Design/DESIGN.md`](Design/DESIGN.md).** Cấm gán giá trị literal cho `--token` ở nơi khác; bí danh `var(--x)` và khung minh hoạ không mang giá trị thì được phép | MUST NOT | `check-docs.sh` §20 — canary kép (pattern khớp được dòng gán thật **và** không khớp bí danh). Miễn trừ `adr/`: một ADR ghi lại quyết định tại thời điểm đó, kể cả giá trị | ✅ |
| D32 | **Mỗi hướng đã khoá (`❌ loại, không hoãn`) mang một mã `K##`.** Mã tuần tự, không tái sử dụng, không đánh số lại. Lật một hướng thì viết ADR **trích mã đó**, không xoá dòng. Quy ước đầy đủ: [`wiki-core/README.md`](wiki-core/README.md) §9.1 | MUST | `check-docs.sh` §21 — bắt thiếu mã, trùng mã, và **lỗ trống trong dãy số**: một mã biến mất mà không ADR nào trích dẫn nghĩa là một hướng khoá vừa bị lật trong im lặng | ✅ |
| D33 | **Mỗi luồng trong [`luong/`](luong/) khai đủ sáu mục, trong đó có mục *Quan hệ với đơn vị* trả lời một trong ba: *thuộc đơn vị* · *dùng chung toàn hệ* · *không áp dụng kèm lý do*.** Mục lục khu không được lệch khỏi thư mục | MUST | `check-docs.sh` §22 — kiểm **có mặt**, không kiểm đúng sai. Đó là toàn bộ tham vọng: biến một khoảng im lặng thành một câu tường minh để người đọc soát được | ✅ |

> **Mã `B*` ở [§10](#10-danh-sách-nợ--luật-chưa-có-cổng) là nợ của Backend**, tách khỏi dãy `A*`/`E*`/`R*`/`S*` ở các bảng trên vì chúng chưa có cổng. Trộn chúng vào bảng có cổng là làm bảng trông đầy đủ hơn thực tế.

> **D14 và D15 cố ý KHÔNG nằm ở bảng này** — chúng chưa có cổng nào, nên theo đúng luật của chính file này, chỗ của chúng là [§10 Danh sách nợ](#10-danh-sách-nợ--luật-chưa-có-cổng). Trộn một luật không có cổng vào bảng luật là cách làm bảng trông đầy đủ hơn thực tế.

## 2. Luật ranh giới `.claude` ↔ `docs`

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| C1 | `.claude/` không chứa câu có thể trở thành SAI khi code đổi | MUST NOT | `check-docs.sh` §2 (bắt phần kiểm được: code block) | ✅ một phần |
| C2 | Không chép nội dung từ `docs/` sang `.claude/` — chỉ trỏ đường dẫn | MUST NOT | Review bởi `core-reviewer` | 🕳️ §10 |
| C3 | Dòng trỏ đường trong `.claude/` là một dòng, không kèm tóm tắt | MUST | Review | 🕳️ §10 |
| C4 | Agent không chạy lệnh git ghi | MUST NOT | `settings.json` → `permissions.deny` | ✅ |

## 3. Luật kiến trúc Backend

📖 Chi tiết: [`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) · [`kien-truc-core-module.md`](kien-truc-core-module.md)

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| A1 | `Core.Domain` có **zero package reference** | MUST | ArchTest `Core_Domain_MustHave_ZeroPackageReference` | 📐 |
| A2 | `Core.Application` không tham chiếu EF Core / ASP.NET Core / bất kỳ Infrastructure nào | MUST NOT | ArchTest `Core_Application_MustNotDependOn_Infrastructure` | 📐 |
| A3 | `Core.*` không tham chiếu bất kỳ assembly `Modules.*` nào | MUST NOT | ArchTest `Core_MustNotReference_AnyModulesAssembly` | 📐 |
| A4 | `Modules.A` không tham chiếu `Modules.B` — chỉ đi qua `Core.Contracts` | MUST NOT | ArchTest `Modules_MustNotReference_OtherModules` | 📐 |
| A5 | Source của `Core.*` không chứa chuỗi literal đặt tên tầng nghiệp vụ | MUST NOT | ArchTest `CoreSource_MustNotContain_BusinessNameStringLiteral` | 📐 |
| A6 | Mọi project trên đĩa đều được khai trong solution | MUST | ArchTest `EveryProjectOnDisk_IsDeclared_InSolution` | 📐 |
| A7 | Host (`Api`) chỉ là composition root — không chứa middleware, controller, hay logic | MUST NOT | ArchTest `Host_MustNotContain_MiddlewareOrController` | 📐 |
| A8 | Mọi `Options` bắt buộc có đường `ValidateOnStart` | MUST | ArchTest `EveryRequiredOptions_HasA_ValidateOnStart_CodePath` | 📐 |
| A9 | Mọi middleware khai báo đều được nối vào pipeline | MUST | ArchTest `EveryMiddlewareClass_IsWiredInto_Pipeline` | 📐 |
| A10 | Mọi pipeline behavior được đăng ký đúng **một** lần | MUST | ArchTest `EveryPipelineBehavior_IsRegistered_ExactlyOnce` | 📐 |
| A11 | Mọi `AbstractValidator` được đăng ký ở composition root | MUST | ArchTest `EveryAbstractValidator_IsRegistered` | 📐 |
| A13 | **Handler và endpoint trả DTO, không trả entity.** Phép chiếu viết tay ngay trong câu truy vấn; không dùng thư viện ánh xạ tự động | MUST | ArchTest `PublicApi_MustNotExpose_Entity` — kiểu trả về của handler và controller không nằm trong `Core.Domain`; xem [`quy-uoc/be-cqrs-handler.md`](quy-uoc/be-cqrs-handler.md) §11 | 📐 |
| A12 | Chỉ bộ lọc job nền, bộ phát outbox, lệnh seed và bước đăng nhập được mở phạm vi ngữ cảnh thực thi (`IExecutionContextScope.Enter`) | MUST NOT | ArchTest `ExecutionContextScope_IsEntered_OnlyByAllowedCallers` | 📐 |

## 4. Luật Domain & dữ liệu

📖 Chi tiết: [`quy-uoc/be-entity-domain.md`](quy-uoc/be-entity-domain.md) · [`database/migration-policy.md`](database/migration-policy.md)

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| E1 | Entity nghiệp vụ không có public setter cho field nghiệp vụ (chỉ 5 field audit được phép) | MUST NOT | ArchTest `BaseEntity_Descendants_MustNotHave_PublicSetter` | 📐 |
| E2 | `BaseEntity.Id` là `init`-only | MUST | ArchTest `BaseEntityId_MustBe_InitOnly` | 📐 |
| E3 | Mọi hậu duệ `BaseEntity` được map vào model và có soft-delete query filter | MUST | ArchTest `EveryBaseEntityDescendant_HasNamedSoftDeleteQueryFilter` | 📐 |
| E4 | Mỗi entity nằm đúng schema của phía nó (Core → `core`, module → schema riêng) | MUST | ArchTest `EveryMappedEntity_LivesInTheSchemaOfItsSide` | 📐 |
| E5 | **Không có foreign key vật lý xuyên schema** — tham chiếu bằng ID | MUST NOT | ArchTest `NoForeignKey_CrossesSchemaBoundary` | 📐 |
| E6 | Migration của một schema nằm trong project sở hữu schema đó | MUST | ArchTest `EveryMigration_LivesIn_ItsOwningProject` | 📐 |
| E7 | Mọi interceptor khai báo đều được nối vào `DbContextOptions` | MUST | ArchTest `EveryInterceptor_IsWiredInto_DbContextOptions` | 📐 |
| E8 | App từ chối khởi động khi còn migration chưa áp vào DB | MUST | Integration test `Startup_Fails_When_PendingMigrationsExist` | 📐 |

## 5. Luật lỗi & envelope

📖 Chi tiết: [`quy-uoc/be-cqrs-handler.md`](quy-uoc/be-cqrs-handler.md) · [`quy-uoc/be-api-controller.md`](quy-uoc/be-api-controller.md) · [`wiki-core/be/16-i18n-va-ma-loi.md`](wiki-core/be/16-i18n-va-ma-loi.md)

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| R1 | Domain và Application **không ném exception cho lỗi nghiệp vụ** — trả `Result` | MUST NOT | ArchTest `DomainAndApplication_MustNotThrow_BusinessException` | 📐 |
| R2 | `ErrorDescriptor` không được dựng từ chuỗi literal ngoài catalog | MUST NOT | ArchTest `ErrorDescriptor_IsNever_BuiltFrom_AStringLiteral_OutsideACatalog` | 📐 |
| R3 | Mọi mã lỗi nghiệp vụ khớp khuôn đã khai và **duy nhất** trong toàn hệ | MUST | ArchTest `EveryBusinessCode_Matches_Format` + `_IsUnique` | 📐 |
| R4 | Mọi `ErrorType` ánh xạ sang một HTTP status hợp lệ, tại **đúng một chỗ** | MUST | ArchTest `EveryErrorType_MapsTo_AValidHttpStatus` | 📐 |
| R5 | Ánh xạ `Result` → HTTP **không dùng reflection** | MUST NOT | ArchTest `ResultToHttpMapper_MustNotUse_Reflection` | 📐 |
| R6 | Envelope lỗi dựng tay luôn mang business code | MUST | ArchTest `HandBuiltErrorEnvelope_AlwaysCarries_ABusinessCode` | 📐 |
| R7 | Tham số thông điệp truyền theo **tên**, không theo thứ tự | MUST | ArchTest `MessageParams_AreNamed_NotPositional` | 📐 |
| R8 | Câu hiển thị cho người dùng không hardcode trong BE — chỉ mã + tham số | MUST NOT | ArchTest `NoUserFacingVietnameseString_InBackend` | 📐 |

> **R5 có lý do cụ thể:** ở dự án tiền nhiệm, cầu nối exception→envelope dựng bằng `GetMethod` + `Invoke` với danh sách kiểu hardcode. Khi chữ ký đổi, `GetMethod` trả null và **mọi lỗi nghiệp vụ biến thành `NullReferenceException`** — hỏng đúng nhánh lỗi, không lỗi biên dịch, không test kiến trúc nào chạm tới, chỉ lộ ra ở Production. Xem [`audit/2026-09-05-reflection-envelope.md`](audit/2026-09-05-reflection-envelope.md).

## 6. Luật bảo mật & phân quyền

📖 Chi tiết: [`wiki-core/be/02-identity-auth.md`](wiki-core/be/02-identity-auth.md) · [`wiki-core/be/09-security-beyond-auth.md`](wiki-core/be/09-security-beyond-auth.md)

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| S1 | **Không có hằng số role nào trong Core** — role là dữ liệu trong DB | MUST NOT | ArchTest `Core_MustNotDeclare_RoleConstants` | 📐 |
| S2 | Phân quyền kiểm bằng permission, không bằng tên role | MUST | ArchTest `Authorization_MustCheck_Permission_NotRoleName` | 📐 |
| S3 | Mọi controller kế thừa `ApiControllerBase` | MUST | ArchTest `EveryController_Inherits_ApiControllerBase` | 📐 |
| S4 | Mọi `[AllowAnonymous]` nằm trong allowlist đã khai | MUST | ArchTest `EveryAllowAnonymous_IsOn_TheAllowlist` | 📐 |
| S5 | `AppUser` / `AppRole` chỉ xuất hiện trong `Core.Infrastructure` | MUST | ArchTest `IdentityTypes_MustNotLeak_OutsideInfrastructure` | 📐 |
| S6 | Không có secret trong source hoặc trong bundle FE | MUST NOT | `gitleaks` trong CI | 📐 |
| S9 | Chính sách mật khẩu **giống nhau ở mọi môi trường** — không tệp cấu hình theo môi trường nào khai lại nó | MUST NOT | Test cấu hình `PasswordPolicy_IsDeclared_InExactlyOneFile` — quét tệp cấu hình theo môi trường tìm khoá chính sách mật khẩu | 📐 |
| S10 | **Không mã nào ra quyết định dựa trên chuỗi tên đăng nhập** — tên đăng nhập là dữ liệu, không phải vai trò | MUST NOT | Review — xem [§10](#10-danh-sách-nợ--luật-chưa-có-cổng) | 🕳️ §10 |

## 7. Luật Frontend

📖 Chi tiết: [`quy-uoc/fe-architecture.md`](quy-uoc/fe-architecture.md) · [`quy-uoc/fe-ui-conventions.md`](quy-uoc/fe-ui-conventions.md)

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| F1 | `core/` không import ngược lên `shared/` / `platform/` / `modules/` | MUST NOT | ESLint `import/no-restricted-paths` zone `coreLayerZones` | 📐 |
| F2 | `modules/<A>/` không import nội bộ `modules/<B>/` | MUST NOT | ESLint zone `moduleBoundaryZones` | 📐 |
| F3 | **`eslint-disable` bị cấm cho các rule ranh giới** | MUST NOT | `fe-gate.sh` — quét comment disable trên đúng danh sách rule | 📐 |
| F4 | `BUSINESS_MODULES` trong cấu hình ESLint khớp thư mục `modules/` thật | MUST | `fe-gate.sh` | 📐 |
| F5 | Component nghiệp vụ không import trực tiếp `primeng/*` — đi qua `shared/` | MUST NOT | `fe-gate.sh` — allowlist đường dẫn được phép | 📐 |
| F6 | Không hex color literal trong SCSS của component | MUST NOT | `fe-gate.sh` section tương ứng | 📐 |
| F7 | Không `rgb()` / `rgba()` literal trong SCSS — trừ dạng `rgb(var(--x) / a)` | MUST NOT | `fe-gate.sh` section tương ứng | 📐 |
| F8 | Template `.html` không chứa chữ tiếng Việt — mọi câu đến từ i18n | MUST NOT | `fe-gate.sh` section tương ứng | 📐 |
| F9 | Không còn cú pháp Angular cũ (`*ngIf`, `*ngFor`, `@Input()`, `NgModule`) | MUST NOT | `fe-gate.sh` section tương ứng | 📐 |
| F10 | `components/` và `pages/` không import DTO trực tiếp — chỉ `services/` | MUST NOT | `fe-gate.sh` section tương ứng | 📐 |
| F11 | `components/` không inject service lấy dữ liệu (component dumb) | MUST NOT | `fe-gate.sh` section tương ứng | 📐 |
| F12 | Mọi `services/*.service.ts` có `.spec.ts` cạnh nó | MUST | `fe-gate.sh` section tương ứng | 📐 |
| F13 | Mọi `@for` có `track` | MUST | `ng lint` (angular-eslint) | 📐 |
| F14 | Bundle không vượt ngân sách đã khai | MUST | `ng build` — `budgets` trong `angular.json` | 📐 |

> 🛑 **Cột "Ép bằng gì" ở bảng này KHÔNG được ghi tên section của `fe-gate.sh`.** Script đó chưa tồn tại — nó thuộc giai đoạn 2 — nên mọi tên section viết ra lúc này là bịa. Khi viết `scripts/fe-gate.sh` thật, đặt tên section theo chính mã luật ở cột đầu (`F6`, `F7`…) rồi cập nhật cột này — một mã, không phải hai hệ song song.
>
> **F3 và F4 là hệ quả trực tiếp của quyết định giữ cấu trúc thư mục thay vì Angular workspace** ([`adr/0007-fe-giu-cau-truc-thu-muc.md`](adr/0007-fe-giu-cau-truc-thu-muc.md)). Không có compiler ép ranh giới, nên ESLint là hàng rào duy nhất — mà ESLint bỏ qua được bằng một dòng comment. Thiếu F3 thì F1/F2 chỉ là gợi ý.

## 8. Luật kiểm thử

📖 Chi tiết: [`wiki-core/be/04-testing-strategy.md`](wiki-core/be/04-testing-strategy.md) · [`wiki-core/fe/06-testing-strategy.md`](wiki-core/fe/06-testing-strategy.md)

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| T1 | **Mọi detector trong ArchTest phải có test kiểm chính detector đó** | MUST | Quy ước đặt tên `Detector_*`, soát trong review | 📐 |
| T2 | Integration test chạy trên PostgreSQL thật, không mock DB | MUST | Testcontainers trong `IntegrationTests` | 📐 |
| T3 | Coverage `Core.Domain` + `Core.Application` ≥ 80%; module ≥ 60% | MUST | Cổng coverage trong CI | 📐 |
| T4 | Build không warning | MUST | `TreatWarningsAsErrors` trong `Directory.Build.props` | 📐 |
| T5 | **Test nhánh lỗi khẳng định ĐÚNG mã trạng thái, không khẳng định "khác 200"** | MUST | Review — xem [§10](#10-danh-sách-nợ--luật-chưa-có-cổng) | 🕳️ §10 |
| T6 | **Mọi bộ dò phải khẳng định tập đầu vào khác rỗng trước khi khẳng định "không vi phạm"** | MUST | `check-docs.sh` tự áp cho chính nó — đếm bằng lệnh, không chép danh sách (xem dưới bảng); phía BE xem [§10](#10-danh-sách-nợ--luật-chưa-có-cổng) | ✅ một phần |
| T8 | **Mock chỉ dùng cho biên hạ tầng.** Không giả lập entity, value object, `Result<T>`, handler hay validator của chính mình | MUST NOT | Review — [`wiki-core/be/04-testing-strategy.md`](wiki-core/be/04-testing-strategy.md) §7.1; xem [§10](#10-danh-sách-nợ--luật-chưa-có-cổng) | 📐 |
| T7 | **Mỗi lớp cắt mạch request phải có test seam động chứng minh nó ĐÃ ĐƯỢC NỐI vào pipeline** | MUST | Review — xem [§10](#10-danh-sách-nợ--luật-chưa-có-cổng) | 🕳️ §10 |

> **Kiểm mục nào của cổng đang tự áp T6** — đừng chép danh sách vào đây (§6 của [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md)):
>
> ```bash
> awk '/^section "/ { if (n!="") { if (!g) print "KHONG CO CHOT: " n }; n=$0; sub(/^section "/,"",n); sub(/".*/,"",n); g=0; next }
>      /đang không kiểm gì|canary hỏng|KHÔNG kiểm gì/ { if (n!="") g=1 }
>      END { if (n!="" && !g) print "KHONG CO CHOT: " n }' .claude/check-docs.sh
> ```
>
> **PASS khi lệnh này không in dòng nào.** Mỗi mục cổng phải làm được một trong ba việc: đếm đầu vào và FAIL khi bằng 0 · chạy canary chứng minh phép dò còn sống · khai báo tường minh bằng `NOTE` rằng nó không kiểm gì.
>
> Phép thử phải so **đúng tập**, không so số lần khớp chuỗi với số mục: hai con số có thể bằng nhau do **trùng số** — một mục đóng góp hai lần khớp, một mục khác không lần nào — và mục không có chốt nào vẫn không bị lộ. Một phép tự kiểm đếm sai tập là phép tự kiểm che đúng thứ nó sinh ra để lộ.

> **T6 và T7 nói hai nửa của cùng một sai lầm.** T6: một bộ dò xét 0 mục vẫn in "không có vi phạm". T7: một validator có unit test xanh vẫn có thể **không được ai gọi**. Cả hai đều là "xanh vì lý do sai", và cả hai đều không lộ ra qua bất kỳ test hành vi nào.

> **T1 là luật đắt giá nhất trong bảng này.** Một cổng hỏng âm thầm còn tệ hơn không có cổng: nó tạo cảm giác được bảo vệ. Ở dự án tiền nhiệm, một script cổng **không tồn tại trên đĩa** khiến ba mục cổng FE không chạy suốt thời gian dài, trong khi tài liệu vẫn ghi bình thường. Xem [`audit/2026-08-23-cong-khong-ton-tai.md`](audit/2026-08-23-cong-khong-ton-tai.md).

---

## 9. Luật multi-tenant

📖 Quyết định: [`adr/0013-multi-tenant.md`](adr/0013-multi-tenant.md) · Thi công: [`wiki-core/be/17-multi-tenant.md`](wiki-core/be/17-multi-tenant.md) · Schema: [`database/schema-core.md`](database/schema-core.md)

> 🛑 **Nhóm luật này canh rủi ro nghiêm trọng nhất của toàn hệ: rò dữ liệu giữa hai đơn vị.**
> Cách ly ở đây là cách ly **logic** — dữ liệu hai tenant nằm chung một bảng, và thứ giữ chúng tách nhau là bộ lọc truy vấn, không phải ranh giới vật lý. Một chỗ hở là một lần lộ dữ liệu của đơn vị khác, và nó **không gây lỗi, không gây ngoại lệ** — truy vấn chạy bình thường, chỉ trả về nhiều hơn đáng ra được thấy.

| # | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| M1 | Mọi entity mang `ITenantScoped` phải có bộ lọc truy vấn toàn cục theo `TenantId` | MUST | ArchTest `EveryTenantScopedEntity_HasTenantQueryFilter` | 📐 |
| M2 | `TenantId` lấy từ claim của phiếu xác thực, **không** từ tham số client gửi lên | MUST NOT | ArchTest `TenantId_IsNeverBoundFrom_RequestInput` | 📐 |
| M3 | Mọi index duy nhất trên entity `ITenantScoped` phải gồm `TenantId` | MUST | ArchTest `EveryUniqueIndex_OnTenantScoped_Includes_TenantId` | 📐 |
| M4 | Mọi entity dữ liệu nghiệp vụ phải khai `ITenantScoped` — trừ danh sách miễn trừ đã khai | MUST | ArchTest `EveryBusinessEntity_IsTenantScoped_OrExempt` | 📐 |
| M5 | Mọi lần gọi bỏ bộ lọc truy vấn nằm trong allowlist khai tường minh | MUST | ArchTest `EveryIgnoreQueryFilters_IsOnTheAllowlist` | 📐 |
| M6 | Truy vấn SQL thô trên bảng có tenant phải tự thêm điều kiện `TenantId` | MUST | ArchTest `EveryRawSqlOnTenantTable_FiltersByTenant` | 📐 |
| M7 | Truy cập bản ghi của tenant khác trả **không tìm thấy**, không trả **cấm truy cập** | MUST | Integration test `CrossTenantAccess_Returns_NotFound` | 📐 |
| M9 | **Tài khoản mang cờ vận hành hệ thống không được gán vai trò nghiệp vụ nào**, và không đọc được dữ liệu của đơn vị nghiệp vụ | MUST NOT | Integration test `SystemOperator_CannotHoldBusinessRole` + `SystemOperator_ReadsNoTenantData` — [`adr/0017-khu-quan-tri-he-thong.md`](adr/0017-khu-quan-tri-he-thong.md) | 📐 |
| M8 | Lưu thực thể `ITenantScoped` khi **chưa có đơn vị** thì **từ chối** — không bao giờ ghi `Guid.Empty` | MUST | Integration test `TenantScopedWrite_WithoutTenant_IsRejected` | 📐 |
| M10 | **Nhiều nhất một** dòng trong bảng đơn vị mang cờ đơn vị hệ thống | MUST | Index duy nhất một phần trên `core.tenant` + integration test `SecondSystemTenant_IsRejected` — [`adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) | 📐 |
| M11 | `has_permission_bypass` và `is_system_operator` **loại trừ nhau** trên cùng một tài khoản | MUST NOT | Ràng buộc kiểm tra trên `core.app_user` + integration test `UserWithBothPrivilegeFlags_IsRejected` — [`adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md`](adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md) | 📐 |
| M12 | Không đường nào đặt `has_permission_bypass` lên một tài khoản **đã tồn tại** — cờ chỉ sinh cùng lúc với tài khoản mang nó | MUST NOT | ArchTest `PermissionBypassFlag_IsAssigned_OnlyAtUserCreation` — allowlist vị trí gán khai tường minh | 📐 |

> **M7 nhìn như chi tiết vặt nhưng không phải.** Trả *cấm truy cập* là xác nhận bản ghi đó **tồn tại** — người ngoài dò được danh sách mã đơn hàng, mã hồ sơ của đơn vị khác chỉ bằng cách thử. Trả *không tìm thấy* làm hai trường hợp "không có" và "có nhưng không thuộc về bạn" **không phân biệt được từ bên ngoài**.

> **M4 có danh sách miễn trừ, và danh sách đó phải ngắn.** Dữ liệu dùng chung toàn hệ — bảng tenant, danh mục permission, bảng lịch sử áp dụng script, bộ khoá bảo vệ dữ liệu — không thuộc tenant nào. Mỗi mục miễn trừ phải nêu lý do tại chỗ khai. Một danh sách miễn trừ dài là dấu hiệu ranh giới tenant đang bị hiểu sai.

## 10. Danh sách nợ — luật CHƯA có cổng

Đây là các luật hiện chỉ được ép **bằng review của người hoặc agent**.

Cột **Chặn bởi** tách hai loại nợ vốn bị trộn làm một:

| Giá trị | Nghĩa |
| --- | --- |
| 🔧 **làm được ngay** | Cổng dựng được **hôm nay** — chỉ cần có người viết. Nợ này không có cớ |
| ⏳ **chờ `src/`** | Cổng cần code thật để đối chiếu (ArchTest, build stats, coverage, OpenAPI). Không dựng được ở giai đoạn 1, và đó là sự thật chứ không phải trì hoãn |
| 🚫 **không ép được bằng máy** | Luật đúng nhưng không có hình dạng nào máy nhận ra. Lớp bắt được là người đọc và `core-reviewer`. Ghi ra để không ai đi tìm một cổng không tồn tại |

Đếm bằng lệnh, đừng chép số. Phép đếm neo vào **hình dạng hàng 5 cột**, không neo vào chuỗi — nhờ vậy nó không đếm bảng chú giải ngay trên, và không đếm chính nó:

```bash
awk '/^## 10\./,0' docs/RULES.md | grep -cE '^\|.*\|.*\|.*\| [^|]*src/[^|]*\|$'
awk '/^## 10\./,0' docs/RULES.md | grep -cE '^\|.*\|.*\|.*\| 🔧[^|]*\|$'
```

D19 có mặt ở **cả hai** vì nó bị tách đôi; các hàng mang 🚫 **không** thuộc lệnh nào trong hai lệnh trên. Vì vậy đừng cộng hai số ra tổng — chúng trả lời hai câu hỏi khác nhau.
 Danh sách này tồn tại để nợ nhìn thấy được, không phải để hợp thức hoá việc thiếu cổng.

| # | Luật | Vì sao chưa có cổng | Ý tưởng cổng tương lai | Chặn bởi |
| --- | --- | --- | --- | --- |
| D13 (phần còn lại) | Một chủ đề một file chủ — **phần diễn đạt lại**, không phải chép nguyên văn | §15 chỉ bắt được bản sao gần như nguyên văn của chuỗi đã đăng ký. Một file mô tả cùng một bảng bằng câu chữ khác thì cổng im lặng | Skill `/doc-sync` so tiêu đề + từ khoá giữa các file, báo trùng để người quyết. Và: `core-reviewer` đọc hiểu — đó là lớp bắt được thứ này, xem [`OWNERSHIP.md`](OWNERSHIP.md) §5 | 🔧 làm được ngay |
| D14 | Không chép thứ đếm được bằng lệnh | Khó phân biệt "số liệu chép cứng" với "số liệu minh hoạ" | Quét mẫu số + danh từ đếm được trong `docs/`, cảnh báo mềm | 🔧 làm được ngay |
| D15 | Nhãn trạng thái không chép vào mục lục | Cần biết đâu là "nhãn cấp file" | Cấm chuỗi nhãn trong `docs/README.md` ngoài bảng cấp khu | 🔧 làm được ngay |
| C2 | Không chép nội dung `docs/` sang `.claude/` | §2 chỉ bắt được code block; văn xuôi chép lại thì không | Đo độ tương đồng đoạn văn giữa `.claude/` và `docs/` | 🔧 làm được ngay |
| C3 | Dòng trỏ đường không kèm tóm tắt | Cần phán đoán "thế nào là tóm tắt" | Giới hạn độ dài ô trong bảng định tuyến | 🔧 làm được ngay |
| B1 | Khoá của `fieldErrors` giữ **PascalCase** trong khi cả payload là camelCase — `DictionaryKeyPolicy` cố ý để `null` | Cần đọc cấu hình serializer **và** một response thật để biết khoá ra dạng nào; hình dạng code không nói lên điều đó. Đây là luật mà "sửa cho nhất quán" sẽ phá, và phá **im lặng** — cả hai bên vẫn là JSON hợp lệ | Integration test gọi một endpoint trả lỗi validate rồi khẳng định response chứa đúng `"Email"`, không phải `"email"` | ⏳ chờ `src/` |
| B2 | `ValidationBehavior` chạy **trước** `TransactionBehavior` trong pipeline | Luật A10 chỉ canh mỗi behavior đăng ký đúng một lần, **không** canh thứ tự. Đảo thứ tự thì mọi request sai định dạng cũng mở một transaction rồi rollback — không sai kết quả, nên không test hành vi nào đỏ | ArchTest đọc thứ tự lời gọi `AddOpenBehavior` trong `AddCoreApplication()` và so với danh sách khai ở [`quy-uoc/be-cqrs-handler.md`](quy-uoc/be-cqrs-handler.md) §5.1 | ⏳ chờ `src/` |
| B3 | `SortBy` chỉ nhận giá trị thuộc allowlist của từng endpoint | Allowlist là dữ liệu trong validator của từng query; không có hình dạng chung để quét. Thiếu nó thì tốt nhất là lỗi lúc chạy, tệ nhất là injection | ArchTest: mọi query record có property `SortBy` phải có một validator khai `Must`/`IsInEnum` trên chính property đó | ⏳ chờ `src/` |
| B4 | `Core.Infrastructure` không phụ thuộc `HttpContext` | Luật A2 canh chiều `Application → Infrastructure`; **không** có luật nào canh việc `Infrastructure` kéo `Microsoft.AspNetCore.Http` vào. Đã lệch thật một lần: một tài liệu đặt `HttpContextCurrentUser` vào Infrastructure | ArchTest kiểu A1: `Core.Infrastructure` không được tham chiếu assembly `Microsoft.AspNetCore.Http.*` | ⏳ chờ `src/` |
| B5 | Handler không tự gọi `SaveChangesAsync` / `BeginTransaction` | Bảng "handler KHÔNG làm gì" ở [`quy-uoc/be-cqrs-handler.md`](quy-uoc/be-cqrs-handler.md) §3.1 hiện chỉ được ép bằng review. Một handler tự lưu sẽ ghi **ngoài** transaction của behavior, và ca đó chỉ lộ khi có lỗi ở nửa sau | ArchTest quét AST: trong `*.Application`, không lời gọi `SaveChangesAsync`/`BeginTransactionAsync` nào nằm ngoài `TransactionBehavior` | ⏳ chờ `src/` |
| B6 | Mỗi lời gọi `IgnoreQueryFilters` phải **nêu tên filter** được bỏ | Luật M5 canh *có nằm trong allowlist không*, không canh *bỏ filter nào*. Gọi không tên bỏ **cả hai** filter cùng lúc — người viết định bỏ lọc xoá mềm lại bỏ luôn lọc đơn vị, và không có gì báo | ArchTest quét AST: mọi `IgnoreQueryFilters` phải có tham số tên filter; dạng không tham số bị cấm tuyệt đối | ⏳ chờ `src/` |
| B7 | Khoá phân quyền đúng khuôn `<resource_key>.<action>` và khớp danh mục ở [`database/schema-core.md`](database/schema-core.md) §5.2 | Luật S2 canh *"kiểm bằng permission, không bằng tên vai trò"*, không canh **giá trị chuỗi**. Một hằng số lệch một ký tự làm `IPermissionChecker` trả `false` cho **mọi** người vì deny-by-default — và đây là ca đã xảy ra: ba tài liệu từng khai ba bộ khoá khác nhau | Hosted service kiểm danh mục đã gộp **lúc khởi động** — khoá trùng, sai khuôn, nhãn rỗng thì tiến trình không khởi động ([`quy-uoc/be-architecture.md`](quy-uoc/be-architecture.md) §1.1); kèm script CI đối chiếu hằng số với dữ liệu seed, **hai chiều** | ⏳ chờ `src/` |
| F15 | Sàn coverage FE theo tầng (`core/` cao nhất, `modules/` thấp nhất) | Cần `src/FE` và bộ chạy test — giai đoạn 2 | Ngưỡng khai trong cấu hình test runner, CI đọc báo cáo coverage | ⏳ chờ `src/` |
| F16 | Mọi khoá i18n mà FE tra cứu phải khớp một mã lỗi BE đã khai | Cần cả hai phía tồn tại để đối chiếu | Script so danh mục mã lỗi BE với tập khoá trong file ngôn ngữ, hai chiều | ⏳ chờ `src/` |
| D18 | Mỗi card trong `docs/contracts/` mô tả một route **có thật** trong `src/` | Khu `contracts/` là nơi FE và BE cam kết với nhau, nhưng card viết tay và không sinh từ OpenAPI. Cả repo dựng trên tiền đề "tài liệu phải mô tả thứ có thật", còn chính khu này thì chỉ có review canh | Sinh OpenAPI lúc build, so tập `(method, path)` trong tài liệu với tập route thật — **hai chiều**, vì thiếu card cũng tệ ngang card thừa | ⏳ chờ `src/` |
| D19 | `docs/Design/` là nguồn UI duy nhất — không dựng prototype HTML song song | Luật này ở [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §7 nhưng chưa từng có mã luật ở file này. "Nguồn thứ hai" khó dò tĩnh: một file HTML có thể là ảnh chụp hợp lệ, cũng có thể là prototype đang bị dùng làm nguồn | Cấm file `.html` ngoài `src/FE`; cấm giá trị hex color ngoài `docs/Design/` và hai file token đã khai | 🔧 nửa hex đã thành **D30**; nửa *cấm `.html` ngoài `src/FE`* ⏳ chờ `src/` |
| D20 | Toàn bộ `docs/` và `.claude/` viết bằng tiếng Việt | Luật ở [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §10, chưa có mã luật ở file này. Ngưỡng máy đọc được thì dễ, nhưng đặt sai ngưỡng sẽ báo sai cho file gần như toàn định danh kỹ thuật | Mỗi `.md` phải có tỉ lệ ký tự có dấu thanh trên tổng ký tự chữ vượt một ngưỡng; đặt ngưỡng bằng cách đo file thấp nhất hiện có rồi trừ biên | 🔧 làm được ngay |
| D21 | Nhãn `🚧 ĐÃ CHỐT — ĐANG THI CÔNG` phải kèm bảng *"có thật hôm nay → sẽ thành"* | Luật ở [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4. Chưa file nào dùng nhãn này nên cổng sẽ là no-op — và một no-op im lặng chính là thứ T6 cấm | File chứa `🚧 ĐÃ CHỐT` phải chứa thêm chuỗi `sẽ thành`; mục phải khai rõ khi xét 0 file | 🔧 làm được ngay |
| T5 (nợ) | Test nhánh lỗi khẳng định đúng mã trạng thái | Cần đọc hiểu ý định của assertion; `!IsSuccessStatusCode` là cú pháp hợp lệ và không có hình dạng tĩnh nào phân biệt nó với một assertion chặt | ArchTest quét AST test: trong thư mục test nhánh lỗi, cấm assertion chỉ phủ định `IsSuccessStatusCode` mà không nêu mã cụ thể | ⏳ chờ `src/` |
| T7 (nợ) | Test seam động cho mỗi lớp cắt mạch request | Cần `src/` và bộ chạy test — giai đoạn 2. Bản hỏng ở dự án tiền nhiệm nằm ở chỗ **không ai gọi validator**, mà unit test validator vẫn xanh | Với mỗi behavior/middleware khai trong tài liệu, một integration test gửi request thật và khẳng định lớp đó đã can thiệp | ⏳ chờ `src/` |
| T8 (nợ) | Mock chỉ dùng cho biên hạ tầng | Phân biệt "kiểu của mình" với "kiểu của thư viện ngoài" cần đọc được đồ thị tham chiếu **và** ý định của test. Một detector ngây thơ sẽ báo sai mọi lần giả lập một interface hợp lệ | ArchTest quét project test: cấm `Substitute.For<T>` khi `T` thuộc `Core.Domain` hoặc là kiểu cụ thể của `Core.Application` — cho phép khi `T` là interface seam đã khai | ⏳ chờ `src/` |
| D23 (phần còn lại) | Đổi tiêu đề một bảng định tuyến sang chữ ngoài tập làm bảng đó **âm thầm** ra khỏi tầm canh của §8 | Cổng chỉ phát hiện khi **mọi** bảng rơi ra ngoài (chốt `rows -eq 0`); mất một bảng thì con số hàng giảm mà không ai đối chiếu con số đó | In số hàng đã xét vào output và so với lần chạy trước trong CI — cần một mốc lưu giữa hai lần chạy | 🔧 làm được ngay |
| D25 | **Thư viện biểu đồ không nằm trong bundle khởi động** — cùng một hướng khoá với `K33` ([`wiki-core/fe/12-charting.md`](wiki-core/fe/12-charting.md) §10), và là ràng buộc 1 của ADR-0019 | F14 là một **trần kích thước**, không phải allowlist nội dung: một thư viện 40 kB lọt vào chunk khởi động vẫn qua F14 nếu tổng còn dưới trần. F14 không biết *cái gì* nằm trong bundle | Allowlist module được phép có mặt trong chunk khởi động, đọc từ build stats (`--stats-json`); CI đối chiếu danh sách module với allowlist khai trong tài liệu | ⏳ chờ `src/` |
| D26 | **`Chart`, `EditableGrid`, `Input.daterange` nằm ở nhánh tải chậm** — ADR-0019 ràng buộc 2 | Cùng lý do D25. `budgets` có loại `initial` nên đây là suy đoán **gián tiếp** qua kích thước: nó đỏ khi bundle phình, không đỏ khi đúng ba component đó nằm sai chỗ mà vẫn nhẹ | Cùng cổng với D25 — allowlist chunk khởi động là thứ duy nhất diễn tả được cả hai ràng buộc | ⏳ chờ `src/` |
| D27 | **Component trong `docs/Design/Components/` không mang từ vựng nghiệp vụ** — ADR-0019 ràng buộc 3, phép thử ở [`Design/CLAUDE.md`](Design/CLAUDE.md) §1 | Phép thử là *"xoá phần nghiệp vụ mà spec vẫn còn nghĩa"* — đọc hiểu, không quét tĩnh được. Ranh giới giữa **ví dụ minh hoạ** (được phép) và **từ vựng ăn vào mô hình** (cấm) là phán đoán. Đã rò **hai lần** và cả hai đều do người đọc bắt | **Một lát cắt đã có cổng: D28** (§1) — từ ngành ở vị trí định danh. Phần còn lại (từ ngành trong văn xuôi, trong tên mục, trong cách diễn đạt) nhiều khả năng *không ép được bằng máy*; lớp bắt được là `core-reviewer` đọc hiểu. Một phép dò theo **mục** đã được thử và sai **5/5** — [`audit/2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md`](audit/2026-09-12-cong-neo-vao-muc-thay-vi-vai-tro-cua-chuoi.md). Mỗi lần rò ghi **một mục trong [`audit/`](audit/) mang mốc `[RO-TU-VUNG-D27]`** — đó là bộ đếm cho điều kiện lật 3 của ADR-0019, ngưỡng **1**, PASS khi đếm ra `0`. Hai lần rò trước ADR không có mục audit nên **không** nằm trong bộ đếm; ngưỡng đã hạ để bù đúng chỗ đó | 🔧 phần còn lại — xem D28 |
| F17 | Template đạt bộ quy tắc tiếp cận đã chọn | Phần lớn tiêu chí tiếp cận cần render thật, không quét tĩnh được | Bật nhóm quy tắc a11y của `angular-eslint` cho phần kiểm được; phần còn lại là rà tay theo checklist | ⏳ chờ `src/` |
| D31 | **`kind` là khoá CẤP FILE và lấy giá trị của câu MẠNH NHẤT trong file.** Một file trộn thân bài giải thích (không ràng buộc) với mục §Áp dụng (ràng buộc) vẫn mang `kind: luat`. Phân biệt câu nào ràng buộc thì đọc §Áp dụng, **đừng suy từ `kind` ra** | Cần đọc hiểu từng câu để biết câu nào ràng buộc. Không có hình dạng tĩnh nào phân biệt một câu cam kết phạm vi với một câu mô tả cách làm ở quy mô lớn hơn | Nhiều khả năng không có cổng. Dấu hiệu luật này đang tốn tiền thật: `core-reviewer` mở từ **ba** finding trở lên trong một lượt mà đều bị bác vì *"chỗ đó là văn xuôi giải thích"* — khi đó câu trả lời là tách mục §Áp dụng ra file riêng, **không** phải hạ `kind` | 🚫 không ép được bằng máy |
| S10 | Không mã nào ra quyết định dựa trên chuỗi tên đăng nhập | Một phép so chuỗi với tên đăng nhập là cú pháp hợp lệ và trông giống hệt một phép tra cứu người dùng bình thường. Phép dò ngây thơ báo sai ở mọi chỗ tra theo tên đăng nhập — mà luồng đăng nhập thì bắt buộc phải tra như vậy. Luật này canh ca một tên đăng nhập seed sẵn lặng lẽ trở thành vai trò, dù không hằng số vai trò nào được khai | ArchTest quét AST: trong `Core.*`, cấm so sánh thuộc tính tên đăng nhập với một chuỗi hằng, trừ tệp khai dữ liệu seed đã đăng ký | ⏳ chờ `src/` |

**Quy tắc bổ sung luật mới:** thêm một dòng vào bảng nào ở §1–§9 thì **phải** khai cột "Ép bằng gì". Nếu chưa có cổng, dòng đó thuộc §10 — không được để trống cột.
