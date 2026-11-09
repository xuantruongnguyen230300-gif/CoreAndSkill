---
kind: luat
scope: core
verified: chua-doi-chieu
---

# OWNERSHIP — mỗi nội dung có đúng MỘT nguồn đối chiếu

> **Đây là sổ đăng ký chủ quyền, và nó là ĐẦU VÀO CỦA CỔNG.** `check-docs.sh` §15 đọc bảng ở §3 và kiểm từng dòng. Bảng §4 là danh sách chờ đặt mốc, cổng không đọc; bảng §7 chia mục của một màn Module giữa `spec/` và `docs/Design/`, cổng cũng không đọc. Sổ này không lệch khỏi thứ nó ép **cho những dòng cổng đọc được**, vì chính chúng là thứ được ép. Giới hạn của phạm vi đó nói ở §6 — đọc trước khi tin.

---

## 1. Vì sao có file này

Luật "một chủ đề một file chủ" ([`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §5, luật D13 ở [`RULES.md`](RULES.md)) không có cổng thì chỉ là gợi ý. Khuôn hỏng nó chặn: **hai hoặc ba file cùng `kind: luat` định nghĩa cùng một thứ, và chúng nói khác nhau**:

| Nội dung | Số file cùng định nghĩa | Hậu quả nếu thi công theo |
| --- | --- | --- |
| Danh mục khoá phân quyền | 3 | Code chặn theo bộ này, dữ liệu seed nạp bộ kia → **403 cho mọi người, kể cả tài khoản đủ quyền** |
| Loại lỗi của một mã lỗi | 2 | Người dùng nhận "bạn không có quyền" trong khi họ có quyền |
| Tiền tố đường dẫn API | 2 | Một hàng rào chống dò mật khẩu **chạy mỗi request và không chặn gì cả** |
| Chỗ đặt lớp đọc `HttpContext` | 2 | Kéo phụ thuộc web vào project bị cấm chứa nó |

Không lỗi nào trong số đó bị cổng bắt, vì cổng khớp chuỗi và kiểm sự tồn tại — nó **không so nội dung hai file với nhau**.

**Bài học:** hai bản sao của một định nghĩa thì chúng sẽ lệch nhau, và bản sao không bao giờ được sửa cùng lúc. Đồng bộ hai bản là hoãn vấn đề; gỡ một bản mới là giải nó.

---

## 2. Ranh giới — sổ này quản cái gì

**Quản: ĐỊNH NGHĨA.** Một catalog, một bảng ánh xạ, một chữ ký kiểu, một hình dạng dữ liệu — thứ tồn tại ở đúng một chỗ và mọi nơi khác chỉ *tra cứu*.

**Không quản: CÁCH DÙNG.** Một quy ước áp khắp nơi (viết đường dẫn thế nào, đặt tên biến ra sao) xuất hiện ở mọi file là bình thường — đó không phải bản sao của định nghĩa, đó là việc tuân thủ nó.

Phép phân biệt:

> Nếu sửa ở một chỗ mà chỗ kia trở thành **sai**, đó là **định nghĩa bị nhân bản** → thuộc sổ này.
> Nếu sửa ở một chỗ mà chỗ kia vẫn đúng theo cách của nó, đó là **cách dùng** → không thuộc sổ này.

### Dạng trỏ đường thay cho nhân bản

File không phải chủ **không được chép lại định nghĩa**. Nó viết một dòng:

```markdown
> 📖 Bảng ánh xạ loại lỗi → mã HTTP: đọc `docs/quy-uoc/be-api-controller.md` §Ánh xạ.
```

Được phép nhắc **tên** của thứ đó, và nhắc **một giá trị cụ thể** khi đang nói về ca đó (một card API ghi endpoint này cần quyền nào là hợp lệ). Không được phép **chép lại cả bảng hoặc cả danh sách**.

---

## 3. Bảng chủ quyền — cổng đọc bảng này

Cột **Chuỗi định danh** là một đoạn văn bản **chỉ xuất hiện ở nơi định nghĩa**. Cổng đếm số file trong `docs/` chứa chuỗi đó; nhiều hơn một file là vi phạm.

Chọn chuỗi cho tốt: phải đủ đặc trưng để không khớp nhầm vào một câu văn xuôi. Đặt nó ở **tiêu đề của chính phần định nghĩa** là cách ổn định nhất — tiêu đề ít bị viết lại hơn thân bài, và mắt người đọc thấy ngay.

### Hợp đồng hai chiều — đây là chỗ luật §1 trở thành cổng

Quy ước đánh mốc: tiêu đề (hoặc một cụm in đậm) của phần định nghĩa mang hậu tố **`— định nghĩa gốc`**.

| Chiều | Luật | Cổng |
| --- | --- | --- |
| Sổ → tài liệu | Mỗi chuỗi đã đăng ký xuất hiện ở **đúng một** file, và đúng file khai làm chủ | `check-docs.sh` §15 (luật D13, D17) |
| Tài liệu → sổ | Mỗi mốc `— định nghĩa gốc` **bắt buộc** có một dòng ở bảng dưới | `check-docs.sh` §16 (luật **D22**) |

Hai chiều cộng lại cho một câu duy nhất mà người viết cần nhớ:

> **Viết một định nghĩa mới thì gắn mốc `— định nghĩa gốc` vào tiêu đề của nó. Cổng lo phần còn lại.**

Gắn mốc mà quên đăng ký → cổng đỏ, kèm `file:dòng`. Đăng ký một chuỗi không tồn tại → cổng đỏ. Chép định nghĩa sang file thứ hai → cổng đỏ. Nhờ vậy sổ này **tự duy trì trong phạm vi §6** — nó không lệch khỏi tài liệu **ở những chỗ có mốc**.

🛑 **Giới hạn phải biết:** cả hai chiều chỉ canh được thứ **có gắn mốc**. Hai file mô tả cùng một thứ bằng câu chữ khác, không file nào gắn mốc, thì cổng im lặng — đó là phần chưa có cổng của **D13** ở [`RULES-DOCS.md`](RULES-DOCS.md), và lớp bắt được nó là `core-reviewer` đọc hiểu, không phải máy.

| Chủ đề | File chủ | Chuỗi định danh |
| --- | --- | --- |
| Danh sách màn hình Core | `docs/quy-uoc/fe-architecture.md` | `Màn hình Core — định nghĩa gốc` |
| Trên dây là DTO, không phải entity | `docs/quy-uoc/be-cqrs-handler.md` | `Trên dây là DTO, không bao giờ là entity — định nghĩa gốc` |
| Thứ tự ưu tiên của cấu hình theo đơn vị | `docs/wiki-core/be/19-cau-hinh-theo-don-vi.md` | `Thứ tự ưu tiên của cấu hình — định nghĩa gốc` |
| Khuôn sinh mã nghiệp vụ | `docs/wiki-core/be/20-sinh-ma-nghiep-vu.md` | `Khuôn sinh mã nghiệp vụ — định nghĩa gốc` |
| Bảng ánh xạ loại lỗi → mã HTTP | `docs/quy-uoc/be-api-controller.md` | `Bảng ánh xạ ErrorType → HTTP — định nghĩa gốc` |
| Danh mục khoá phân quyền của Core | `docs/database/schema-core.md` | `Danh mục khoá phân quyền Core — định nghĩa gốc` |
| Hình dạng envelope lỗi | `docs/quy-uoc/be-api-controller.md` | `Hình dạng envelope — định nghĩa gốc` |
| Tiền tố đường dẫn và phiên bản API | `docs/quy-uoc/be-api-controller.md` | `Tiền tố đường dẫn — định nghĩa gốc` |
| Chữ ký `CoreDbContext` và hai bộ lọc toàn cục | `docs/quy-uoc/be-entity-domain.md` | `Chữ ký CoreDbContext — định nghĩa gốc` |
| Thứ tự middleware trong `UseCoreAsync()` | `docs/quy-uoc/be-architecture.md` | `Thứ tự pipeline — định nghĩa gốc` |
| Vòng đời DI của các seam Core | `docs/quy-uoc/be-architecture.md` | `Vòng đời DI — định nghĩa gốc` |
| Danh sách project của Core và ranh giới | `docs/kien-truc-core-module.md` | `Năm project Core — định nghĩa gốc` |
| Kiểu envelope phía FE | `docs/quy-uoc/fe-api-client.md` | `Kiểu envelope phía FE — định nghĩa gốc` |
| Chuỗi interceptor FE và thứ tự | `docs/quy-uoc/fe-api-client.md` | `Chuỗi interceptor FE — định nghĩa gốc` |
| Hình dạng phân trang phía FE | `docs/quy-uoc/fe-api-client.md` | `Hình dạng phân trang phía FE — định nghĩa gốc` |
| Allowlist import thư viện UI (luật F5) | `docs/wiki-core/fe/05-component-library.md` | `Allowlist import thư viện UI — định nghĩa gốc` |
| Danh sách rule ESLint cấm tắt (luật F3) | `docs/quy-uoc/fe-architecture.md` | `Danh sách rule cấm tắt — định nghĩa gốc` |
| Hàm gắn `fieldErrors` vào form | `docs/wiki-core/fe/09-forms-validation.md` | `Hàm gắn fieldErrors vào form — định nghĩa gốc` |
| Hàm gắn lỗi của phản hồi thất bại vào form và trả câu cho khu lỗi chung (`applyFormFailure`) | `docs/quy-uoc/fe-ui-conventions.md` | `Hàm gắn lỗi của phản hồi thất bại vào form — định nghĩa gốc` |
| Hàm đưa focus về ô sai khi gửi sai (`focusOSaiKhiGuiSai`, `focusOSaiDauTien`) | `docs/quy-uoc/fe-ui-conventions.md` | `Hàm đưa focus về ô sai khi gửi sai — định nghĩa gốc` |
| Ô tự đặt focus lúc mở — cờ trên phần tử, `datFocusLucMo`, `roiOLaCham` | `docs/quy-uoc/fe-ui-conventions.md` | `Ô tự đặt focus lúc mở — định nghĩa gốc` |
| Validator "ô nhập lại phải khớp" (`nhapLaiPhaiKhop`) | `docs/quy-uoc/fe-ui-conventions.md` | `Hàm kiểm ô nhập lại phải khớp — định nghĩa gốc` |
| Phép trừ của truy vấn "dưới điểm ngắt" và tệp phản chiếu điểm ngắt cho TypeScript (`core/theme/diem-ngat.ts`) | `docs/quy-uoc/fe-ui-conventions.md` | `Dưới điểm ngắt, và điểm ngắt trong TypeScript — định nghĩa gốc` |
| Lớp lỗi luôn toast kèm `traceId`, kể cả khi request tắt toast ([ADR-0094](adr/0094-lop-loi-xuyen-suot-luon-toast-ke-ca-khi-man-tat-toast.md)) | `docs/quy-uoc/fe-api-client.md` | `Lớp lỗi xuyên suốt — định nghĩa gốc` |
| Bảng hình dạng SQL thô mà luật M6 phủ | `docs/wiki-core/be/17-multi-tenant.md` | `Bảng hình dạng SQL thô — định nghĩa gốc` |
| Khuôn mã lỗi và biểu thức kiểm | `docs/quy-uoc/be-cqrs-handler.md` | `Khuôn mã lỗi — định nghĩa gốc` |
| Ngưỡng cân nhắc thêm thư viện store FE | `docs/wiki-core/fe/03-state-management.md` | `Ba ngưỡng để cân nhắc thêm thư viện store — định nghĩa gốc` |
| Kiểu CLR hợp lệ cho concurrency token | `docs/quy-uoc/be-entity-domain.md` | `Kiểu CLR nào hợp lệ cho concurrency token — định nghĩa gốc` |
| Hợp đồng `code` / `message` / `messageParams` | `docs/quy-uoc/be-cqrs-handler.md` | `Hợp đồng thông điệp lỗi — định nghĩa gốc` |
| Sơ đồ bốn tầng FE và chiều phụ thuộc | `docs/quy-uoc/fe-architecture.md` | `Bốn tầng — và một chiều duy nhất — định nghĩa gốc` |
| Luật sở hữu migration theo schema | `docs/database/migration-policy.md` | `Mỗi module sở hữu migration của schema mình.** — định nghĩa gốc` |
| Cột của bảng nhật ký kiểm toán | `docs/database/schema-core.md` | `Bảng nhật ký kiểm toán — định nghĩa gốc` |
| Lỗi mà chiến lược thử lại của Core không thử lại | `docs/quy-uoc/be-performance.md` | `Lỗi KHÔNG được thử lại — định nghĩa gốc` |
| Hình dạng dòng nhật ký của lần ghi ma trận phân quyền | `docs/contracts/permissions.md` | `Nhật ký kiểm toán của lần ghi ma trận — định nghĩa gốc` |
| Hình dạng dòng nhật ký của lần đổi vai trò người dùng | `docs/contracts/users.md` | `Nhật ký kiểm toán của lần đổi vai trò người dùng — định nghĩa gốc` |
| Tên đăng nhập dành riêng và phép so của nó | `docs/contracts/users.md` | `Tên dành riêng — định nghĩa gốc` |
| Danh tính và đơn vị cho mã chạy ngoài request | `docs/quy-uoc/be-architecture.md` | `Danh tính và đơn vị khi không có request — định nghĩa gốc` |
| Seam danh mục khoá quyền do module cấp | `docs/quy-uoc/be-architecture.md` | `Danh mục khoá quyền do module cấp — định nghĩa gốc` |
| Hai tầng tệp dịch Core / dự án | `docs/wiki-core/fe/08-i18n.md` | `Hai tầng tệp dịch — định nghĩa gốc` |
| Cỡ icon (`--icon-sm` … `--icon-xl`) | `docs/Design/DESIGN.md` | `Cỡ icon — định nghĩa gốc` |
| Bố cục màn danh sách của Core | `docs/Design/Templates/ListScreen.md` | `Bố cục màn danh sách — định nghĩa gốc` |
| Kiểu dữ liệu công khai của thư viện UI | `docs/quy-uoc/fe-ui-conventions.md` | `Kiểu dữ liệu công khai của thư viện UI — định nghĩa gốc` |
| Seam mở rộng cho màn Core | `docs/quy-uoc/fe-architecture.md` | `Seam cho MÀN Core — định nghĩa gốc` |
| Tầng giữ trạng thái màn danh sách | `docs/quy-uoc/fe-architecture.md` | `Trạng thái màn danh sách — định nghĩa gốc` |
| Mã hướng khoá `K##` | `docs/wiki-core/README.md` | `Mã hướng khoá `K##` — định nghĩa gốc` |
| Mã lỗi dùng chung của Core | `docs/contracts/auth.md` | `Mã lỗi dùng chung — định nghĩa gốc` |
| Quyền của hai tài khoản database | `docs/database/script-runbook.md` | `Quyền của hai tài khoản database — định nghĩa gốc` |
| Tài khoản được chạy từng loại script schema — có hay không có khối tự chặn | `docs/database/script-runbook.md` | `Tài khoản chạy script — định nghĩa gốc` |
| Tập đường dẫn chạm Core (khối máy đọc) | `docs/kien-truc-core-module.md` | `Đường dẫn chạm Core — định nghĩa gốc` |
| Khoá phân vùng rate limit đăng nhập | `docs/quy-uoc/be-api-controller.md` | ``LoginPartitionKey` — định nghĩa gốc` |
| Chữ ký `ICurrentUser` và `ITenantContext` | `docs/quy-uoc/be-architecture.md` | `Danh tính và đơn vị của request — định nghĩa gốc` |
| Xử lý phiên hết hạn phía FE | `docs/quy-uoc/fe-api-client.md` | ``SessionExpiryHandler` — định nghĩa gốc` |
| Seam nguồn seed cho đơn vị mới | `docs/quy-uoc/be-architecture.md` | `Nguồn seed cho đơn vị mới — định nghĩa gốc` |
| Tham số danh sách trên dây (phân trang, sắp xếp, lọc) | `docs/contracts/README.md` | `Tham số danh sách dùng chung: phân trang, sắp xếp, lọc — định nghĩa gốc` |
| Ngoại lệ của luật E6 cho khoá quyền của module | `docs/database/migration-policy.md` | `Ngoại lệ có tên của E6: khoá quyền của module — định nghĩa gốc` |
| Allowlist token được inject trong component dumb (F11) | `docs/wiki-core/fe/trien-khai/05-gate.md` | `Token được inject trong component dumb — định nghĩa gốc` |
| Bảng đổi tên module PrimeNG sang tên sub-preset (F29) | `docs/wiki-core/fe/trien-khai/05-gate.md` | `Tên module PrimeNG khác tên sub-preset — định nghĩa gốc` |
| Bảng module PrimeNG không có sub-preset (F29) | `docs/wiki-core/fe/trien-khai/05-gate.md` | `Module PrimeNG không có sub-preset — định nghĩa gốc` |
| Quy ước đặt khoá dịch phía FE | `docs/quy-uoc/fe-ui-conventions.md` | `Đặt khoá dịch — định nghĩa gốc` |
| Interface audit cho kiểu không kế thừa `BaseEntity` | `docs/quy-uoc/be-entity-domain.md` | ``IAuditableEntity` cho kiểu không kế thừa `BaseEntity` — định nghĩa gốc` |
| Khoá tài khoản: ngưỡng, thời hạn, cách đếm lần sai | `docs/wiki-core/be/02-identity-auth.md` | `Khoá tài khoản — định nghĩa gốc` |
| Cách mang token đồng thời trên dây | `docs/wiki-core/be/06-concurrency-control.md` | `Token đồng thời trên dây — định nghĩa gốc` |
| Bảng tệp đính kèm | `docs/database/schema-core.md` | ``core.file` — tệp đính kèm — định nghĩa gốc` |
| Danh sách lệnh của runner dòng lệnh Core | `docs/quy-uoc/be-architecture.md` | `Lệnh của runner — định nghĩa gốc` |
| Bảng việc chạy nền | `docs/database/schema-core.md` | ``core.job` — việc chạy nền — định nghĩa gốc` |
| Token tầng 3 theo component | `docs/Design/DESIGN.md` | `Token tầng 3 theo component — định nghĩa gốc` |
| Luật thang lớp chồng (`--z-*`, thứ tự Dialog < ConfirmDialog < lớp nổi < Toast) | `docs/Design/DESIGN.md` | `Luật thang lớp chồng — định nghĩa gốc` |
| Bộ chuyển động của mọi lớp nổi neo (`--dur-base`, `--ease-decelerate` chiều bung, `--ease-accelerate` chiều thu) | `docs/Design/DESIGN.md` | `Lớp nổi neo — định nghĩa gốc` |
| Tập khoá cấu hình của lệnh bootstrap, tiền tố `Core:Bootstrap:` | `docs/database/script-runbook.md` | `Khoá cấu hình của lệnh bootstrap — định nghĩa gốc` |
| Tập khoá cấu hình của phép kiểm migration lúc khởi động, tiền tố `Core:SchemaCheck:` | `docs/database/script-runbook.md` | `Khoá cấu hình của phép kiểm khởi động — định nghĩa gốc` |
| Dạng giá trị cấu hình mã hoá `enc:v1:`, chỗ được phép, khoá giải mã tiền tố `Core:ConfigEncryption:`, khoá mang mã `dev` ([ADR-0097](adr/0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md), [ADR-0098](adr/0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md)) | `docs/quy-uoc/repo-artifact.md` | `Giá trị cấu hình mã hoá — định nghĩa gốc` |
| Giá trị dev công khai trong `appsettings.Development.json` và `database/dev/tao-db-local.sql` — các nhóm và ranh giới của chúng, ngoại lệ bản rõ của S6 ([ADR-0110](adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md)) | `docs/quy-uoc/repo-artifact.md` | `Giá trị dev công khai — định nghĩa gốc` |
| Cụm cổng API ↔ `apiBaseUrl` và origin máy chủ dev FE ↔ `Core:Auth:AllowedOrigins` của máy dev — hai cặp phải đổi cùng nhau (luật T14) | `docs/quy-uoc/chay-may-dev.md` | `Cụm cổng và origin của máy dev — định nghĩa gốc` |
| Khởi động app và thiết lập phiên phía FE | `docs/quy-uoc/fe-routing-guard.md` | `khởi động và thiết lập phiên — định nghĩa gốc` |
| Bắt lỗi tải phần mã của tuyến — chỗ nghe, hàm nhận dạng, phép chọn nhánh | `docs/quy-uoc/fe-routing-guard.md` | `Điều hướng hỏng vì không tải được phần mã — định nghĩa gốc` |
| Áp theme trước paint và nạp font | `docs/quy-uoc/fe-ui-conventions.md` | `Theme trước paint và font — định nghĩa gốc` |
| Seam dựng principal, tên claim và các kiểu của phiên (`ISessionPrincipalFactory`, `CoreClaimTypes`, `LoginOutcome`, `SessionDto`) | `docs/quy-uoc/be-api-controller.md` | `Seam dựng principal và kiểu của phiên — định nghĩa gốc` |
| Catalog mã lỗi mà hạ tầng HTTP ở `Core.Web` phát, và helper ghi envelope của chúng (`SecurityErrors`, `SecurityEnvelopeWriter`) | `docs/quy-uoc/be-api-controller.md` | `Catalog mã của hạ tầng HTTP — định nghĩa gốc` |
| Giới hạn kích thước upload và khoá cấu hình `Core:File:MaxUploadMb` | `docs/wiki-core/be/14-file-storage.md` | `Kích thước và giới hạn — định nghĩa gốc` |
| Khoá cấu hình proxy tin cậy, tiền tố `Core:Network:` | `docs/wiki-core/be/18-trien-khai-va-van-hanh.md` | `Khoá cấu hình proxy tin cậy — định nghĩa gốc` |
| Khoá cấu hình ngưỡng rate limit theo IP, tiền tố `Core:RateLimit:` — tên khoá, miền, mặc định ở lớp `CoreRateLimitOptions` | `docs/quy-uoc/be-api-controller.md` | `Khoá cấu hình rate limit — định nghĩa gốc` |
| Header bảo mật nào do proxy đặt, header nào do middleware Core đặt ([ADR-0120](adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md)) | `docs/wiki-core/be/09-security-beyond-auth.md` | `Chia vai header bảo mật giữa proxy và Core — định nghĩa gốc` |
| Hai giá trị hợp lệ của cột "Nền" và vai nguồn quyết thư mục component | `docs/Design/COMPONENTS.md` | `Cột Nền — định nghĩa gốc` |
| Phân loại dòng trống / chú thích / mã cho ngưỡng kích thước tệp FE (luật F22) | `docs/quy-uoc/fe-architecture.md` | `Phân loại một dòng — định nghĩa gốc` |
| Ba vùng sở hữu tệp — Core, dự án, dùng chung có thủ tục; cách đọc luật 1 của ADR-0016 ([ADR-0100](adr/0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md)) | `docs/quy-uoc/repo-artifact.md` | `Vùng sở hữu tệp — định nghĩa gốc` |
| Ca được bỏ bộ lọc truy vấn — bỏ lọc đơn vị, bỏ riêng lọc xoá mềm (luật M5, B6) | `docs/wiki-core/be/17-multi-tenant.md` | `Ca được bỏ bộ lọc — định nghĩa gốc` |
| Danh mục mã phía client `CORE.CLIENT.*` | `docs/quy-uoc/be-cqrs-handler.md` | `Mã cho điều kiện phía client — định nghĩa gốc` |
| Thứ tự bốn interceptor của `DbContext` | `docs/quy-uoc/be-performance.md` | `Thứ tự interceptor — định nghĩa gốc` |
| Cách Core ghi một ngoại lệ vào log — hàm `FailureText.DescribeWithStack`, ba vế của luật B28 | `docs/wiki-core/be/07-observability.md` | `Ghi một ngoại lệ vào log — định nghĩa gốc` |
| Khung khối danh sách của `DataTable` — `Toolbar`, bảng, dải phân trang chung một viền; thứ tự dải, vạch ngăn, chỗ cắm đầu khung | `docs/Design/Components/DataTable.md` | `Khung — khối danh sách liền một viền — định nghĩa gốc` |
| Cột hành động dùng chung của `DataTable` — `rowActionsTemplate`, vị trí, ghim, luật hiện/ẩn | `docs/Design/Components/DataTable.md` | `Cột hành động — hợp đồng dùng chung — định nghĩa gốc` |
| Vùng báo kết quả của màn danh sách cho trình đọc màn hình — một vùng duy nhất do `DataTable` giữ, kể cả khi không có kết quả | `docs/Design/Components/DataTable.md` | `Vùng báo kết quả — định nghĩa gốc` |
| Quy trình đổi lược đồ phá vỡ — các bước mở rộng trước, thu hẹp sau, điều kiện sang bước, câu kiểm backfill | `docs/wiki-core/be/13-core-data-migration.md` | `Quy trình đổi lược đồ phá vỡ — định nghĩa gốc` |

> Bảng này **chưa phủ hết** mọi định nghĩa trong repo. Nó phủ những chỗ đã thật sự lệch. Thêm dòng khi phát hiện một định nghĩa bị nhân bản — đó là cách sổ này lớn lên, và mỗi dòng thêm vào là một lớp bảo vệ vĩnh viễn.

---

## 4. Chờ áp — đã chọn chủ, CHƯA đặt mốc

Cổng **không** đọc bảng này. Nó là chỗ hở nhìn thấy được.

Một dòng chỉ được chuyển lên §3 **sau khi** chuỗi định danh đã thật sự nằm trong file chủ. Đưa lên sớm thì cổng báo *"chuỗi không xuất hiện ở đâu cả — dòng này đang không canh gì"*, và một dòng sổ không canh gì còn tệ hơn không có dòng nào: nó làm bảng trông đầy đủ hơn thực tế.

| Chủ đề | File chủ đã chọn | Chuỗi định danh sẽ đặt |
| --- | --- | --- |
| Allowlist endpoint ẩn danh (luật S4) | `docs/quy-uoc/be-api-controller.md` §5 | `Allowlist endpoint ẩn danh — định nghĩa gốc` |
| Chữ ký `ILoginAttemptLimiter` | `docs/quy-uoc/be-api-controller.md` §6.5 | `Chữ ký ILoginAttemptLimiter — định nghĩa gốc` |
| Luật lỗi hiện lúc nào, câu nào — hàm `fieldErrorText` | `docs/quy-uoc/fe-ui-conventions.md` §6.5 | `Hàm fieldErrorText — định nghĩa gốc` |
| Biến thể của `EmptyState` | `docs/Design/Components/EmptyState.md` | `Biến thể EmptyState — định nghĩa gốc` |
| Ba slot nội dung trạng thái của `DataTable` | `docs/Design/Components/DataTable.md` | `Ba slot nội dung trạng thái — định nghĩa gốc` |
| Chip chỉ đọc của `Toolbar` (`readonlyChips`) | `docs/Design/Components/Toolbar.md` | `Chip chỉ đọc của Toolbar — định nghĩa gốc` |

**Các dòng trên mới chỉ chọn xong chủ.** Mỗi dòng là một định nghĩa mà hôm nay chỉ có **một** nguồn trong `docs/` — nhưng không nguồn nào mang mốc, nên bản sao đầu tiên của chúng sẽ không làm cổng đỏ. Việc còn lại là mở file chủ, gắn mốc `— định nghĩa gốc` vào đúng tiêu đề của phần định nghĩa, rồi chuyển dòng lên §3. Người đặt mốc là người có quyền ghi file chủ: dưới `docs/Design/` là `design-expert`, mọi file chủ khác dưới `docs/` — ở bảng trên là `docs/quy-uoc/` — là `architect`. Bảng ai ghi gì: [`../.claude/README.md`](../.claude/README.md) §2.

Một dòng chuyển lên §3 **trước khi** mốc thật sự nằm trong file chủ thì cổng báo ngay *"chuỗi không xuất hiện ở đâu cả — dòng này đang không canh gì"*. Đó là thứ tự bắt buộc, không phải khuyến nghị.

Bảng này **không** phải danh sách đầy đủ những định nghĩa chưa được canh: nó chỉ chứa những chỗ **đã có người phát hiện và chọn chủ**. Chỗ chưa ai phát hiện thì không có dòng nào ở cả hai bảng — giới hạn đó nói ở §6.

> **Vì sao bảng này tồn tại thay vì gộp vào §3:** bản đầu của sổ đưa cả tám dòng vào §3 khi mới có một mốc được đặt. Cổng lập tức báo bảy dòng *"đang không canh gì"* — đúng, và đó là bài học: một sổ đăng ký khai chủ quyền cho thứ chưa đánh dấu là đang mô tả **ý định**, không phải **thực tế**. Cùng lỗi mà [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §4 cấm, chỉ khác chỗ áp.

---

## 5. Quy trình khi phát hiện hai file cùng định nghĩa một thứ

1. **Chọn file chủ.** Tiêu chí, theo thứ tự: (a) file mà thứ đó *sinh ra* ở đó — schema sở hữu tên cột, quy ước thi công sở hữu chữ ký kiểu; (b) file mà người đọc tìm đến trước; (c) file `kind: luat` thay vì `kind: quyet-dinh`.
2. **Rút file kia còn một dòng trỏ đường.** Không xoá mục — xoá mục thì người đang đọc file đó mất đường đi tiếp.
3. **Đặt một chuỗi định danh** vào phần định nghĩa ở file chủ, và thêm một dòng vào bảng §3.
4. **Chạy cổng.** Nó phải xanh, và phải **đỏ thật** nếu bạn thử chép lại định nghĩa sang file khác.

🛑 **Không "giữ cả hai cho chắc".** Hai bản đầy đủ của một định nghĩa là hai nguồn, và người sửa sẽ chỉ sửa một. Đó chính là cách repo tiền nhiệm có bốn sơ đồ đặt tên project nói ngược nhau.

---

## 6. Giới hạn của cổng này — nói rõ để không ai tin quá

Cổng §15 chỉ bắt được **bản sao gần như nguyên văn** của một chuỗi đã đăng ký. Năm thứ nó **không** bắt:

1. **Diễn đạt lại.** Một file mô tả cùng bảng ánh xạ bằng câu chữ khác thì chuỗi định danh không khớp, và cổng im lặng.
2. **Định nghĩa chưa đăng ký MÀ KHÔNG GẮN MỐC.** §16 ép việc đăng ký cho mọi định nghĩa **có mốc**; một định nghĩa viết ra mà không gắn mốc thì nằm ngoài cả hai chiều. Việc gắn mốc vẫn là kỷ luật của người viết — cổng không thể biết một đoạn văn *có phải* định nghĩa hay không.
3. **Nội dung sai ở chính file chủ.** Cổng bảo đảm *chỉ có một nguồn*, không bảo đảm *nguồn đó đúng*.
4. **Bản sao đặt ngoài `docs/`.** Cổng chỉ đếm file trong `docs/`. Một định nghĩa bị chép sang `.claude/` hoặc `spec/` **không bị bắt** — mà chép nội dung sang `.claude/` chính là hành vi [`../.claude/CLAUDE.md`](../.claude/CLAUDE.md) §3 dành bốn lý do để cấm. Luật C2 canh chiều đó bằng review, xem [`RULES-DOCS.md`](RULES-DOCS.md).
5. **Chuỗi định danh chọn kém.** Một chuỗi quá ngắn hoặc quá phổ thông sẽ khớp nhầm vào văn xuôi của file vô can, và cổng đổ lỗi sai chỗ. §16 có sàn độ dài 12 ký tự để chặn ca tệ nhất, nhưng phần còn lại là phán đoán của người thêm dòng.

Vì vậy sổ này là lớp bổ sung cho `core-reviewer`, không phải thay thế. Việc đọc hiểu và đối chiếu ngữ nghĩa vẫn thuộc về người và về agent review.

---

## 7. Mục của một màn Module — chia giữa `spec/` và `docs/Design/`

Một màn Module có hai tệp mô tả nó: nghiệp vụ ở `spec/<feature>/`, do `ba-analyst` viết; giao diện ở spec màn dưới [`Design/Screens/`](Design/Screens/), do `design-expert` viết theo khuôn [`Design/Templates/Screen.md`](Design/Templates/Screen.md). Mỗi mục dưới đây có đúng một chủ; tệp còn lại chỉ làm việc ở cột phải. Màn Core không có `spec/`, nên mọi mục của nó nằm ở spec màn.

| Mục của một màn | Chủ | Tệp còn lại chỉ được |
| --- | --- | --- |
| Dữ liệu hiện, thao tác có, điều kiện hiện theo nghiệp vụ; mặc định nghiệp vụ của danh sách — thứ tự sắp xếp, bộ lọc bật sẵn | `spec/<feature>/ui-spec.md` | Spec màn ghép đúng các trường, thao tác đó; không thêm thao tác ui-spec không nói |
| Hành động → permission, kể cả quyền vào màn | `spec/<feature>/business-rules.md` §6 | Gọi hành động bằng đúng tên ở §6 — kể cả dòng **Quyền** và bảng phân quyền theo nút của spec màn; không chép tên permission |
| Ý nghĩa của từng thông báo | `spec/<feature>/business-rules.md` §8, mỗi dòng một mã `TB-NN` | Spec màn giữ câu và khoá dịch, mỗi câu trỏ mã `TB-NN` của nó |
| Bố cục, component, trạng thái màn, responsive | Spec màn — các mục H3 bắt buộc của khuôn | ui-spec không có mục nào trong số này |
| Câu hiển thị và khoá dịch | Spec màn — mục *Câu chữ* | ui-spec không khai khoá, không khai câu |
| Đường dẫn của màn | Spec màn — tiêu đề `## <Tên màn> (đường dẫn tuyến)` | ui-spec trỏ tệp spec màn, không ghi đường dẫn |
| Tên, kiểu tham số danh sách trên URL | Card hợp đồng của endpoint danh sách dưới [`contracts/`](contracts/) — query param mang đúng tên dây ([`quy-uoc/fe-routing-guard.md`](quy-uoc/fe-routing-guard.md) §7.2) | Không tệp nào khai lại bảng tham số |

Cổng §15, §16 không đọc bảng này: chủ của ba hàng đầu nằm dưới `spec/`, và bản sao của mọi hàng sẽ nằm dưới `spec/` — ngoài vùng cổng đếm (§6 mục 4). Lớp canh là người đọc, theo phần chưa có cổng của D13 ở [`RULES-DOCS.md`](RULES-DOCS.md).
