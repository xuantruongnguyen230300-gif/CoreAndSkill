---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0116 — Mã đơn vị nghiệp vụ lấy từ cấu hình FE của bản cài; form đăng nhập hai ô; tài khoản vận hành gõ mã qua liên kết "Quản trị hệ thống"; hợp đồng và luồng BE không đổi

> **Trạng thái:** Đã chấp nhận (2026-10-06)
>
> Người dùng chốt ngày 2026-10-06: hướng chính; QC2 — mã đơn vị hệ thống gõ tay sau liên kết "Quản trị hệ thống", không vào bundle; QC4 — **4C**, build sản xuất hỏng khi bản cài không khai mã. Thay hướng của [ADR-0115](0115-bo-o-ma-don-vi-suy-don-vi-tu-ten-mien-con-phien-chi-dung-duoc-o-origin-cua-don-vi-minh.md) (đã loại bỏ, chưa từng được chấp nhận). Không đổi trạng thái ADR nào đã chấp nhận: [ADR-0013](0013-multi-tenant.md), [ADR-0015](0015-fe-va-api-khac-nguon.md), [ADR-0017](0017-khu-quan-tri-he-thong.md), [ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md) giữ nguyên; hướng khoá `K01` giữ nguyên.

## Bối cảnh

Người dùng muốn bỏ ô mã đơn vị khỏi form đăng nhập. [ADR-0115](0115-bo-o-ma-don-vi-suy-don-vi-tu-ten-mien-con-phien-chi-dung-duoc-o-origin-cua-don-vi-minh.md) đề xuất suy đơn vị từ tên miền con; người dùng loại hướng đó ngày 2026-10-06 với một thông tin mới: **mô hình triển khai là mỗi công ty một bản cài**, và hiện chưa cần nhiều đơn vị nghiệp vụ trong một bản cài.

Hệ quả của mô hình đó với dữ liệu đơn vị: mỗi bản cài có đúng hai đơn vị do lệnh bootstrap tạo — đơn vị nghiệp vụ của công ty và đơn vị hệ thống cho tài khoản vận hành ([ADR-0017](0017-khu-quan-tri-he-thong.md), [ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md) §3). Với mọi người dùng của công ty, mã đơn vị là **cùng một giá trị**; bắt họ gõ nó là ma sát thuần. Chỉ tài khoản vận hành cần một mã khác.

Người dùng nói rõ: **người dùng cuối không bao giờ được thấy ô mã đơn vị**. Người dùng có hỏi có thể nhập mã **sau** khi đăng nhập không. Không: tên đăng nhập chỉ duy nhất trong một đơn vị, nên BE phải biết đơn vị **trước** khi tra tài khoản; chọn đơn vị sau xác thực là hướng khoá `K01` của [ADR-0013](0013-multi-tenant.md).

Hiện trạng ngày 2026-10-06:

| Chỗ | Nói gì | Neo |
| --- | --- | --- |
| Form đăng nhập | Ba ô, thứ tự cố định, ô mã đơn vị đứng đầu và nhận focus | `docs/luong/D1-dang-nhap.md:17` · `docs/Design/Screens/01-dang-nhap.md:98` · `src/FE/src/app/platform/xac-thuc/pages/dang-nhap/dang-nhap.page.ts`, chuỗi `tenantCode: this.fb.control('', [Validators.required]),` |
| Hợp đồng | `tenantCode` bắt buộc trong thân `POST /api/v1/core/auth/login` | `docs/contracts/auth.md:154` |
| Tài khoản vận hành | Gõ mã đơn vị hệ thống vào ô mã đơn vị, cùng luồng với mọi người | `docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md:94` · `docs/contracts/auth.md:173` · `docs/wiki-core/be/17-multi-tenant.md:332` |
| Phương án đang chọn ở tệp luật | Một ô mã đơn vị trên form | `docs/wiki-core/be/17-multi-tenant.md:356` |
| Mã đơn vị nghiệp vụ đầu tiên | Khoá bootstrap `Core:Bootstrap:FirstTenantCode`, chỉ đọc lúc chạy lệnh bootstrap — tiến trình API phục vụ thật không đọc nhóm khoá này | `docs/database/script-runbook.md:293` · `src/BE/Core/CoreAndSkill.Core.Application/Configuration/CoreBootstrapOptions.cs`, chuỗi `KHÔNG gắn ValidateOnStart` · giá trị dev `src/BE/CoreAndSkill.Api/appsettings.Development.json`, chuỗi `"FirstTenantCode": "DEMO"` |
| Cấu hình FE | Nhúng lúc build, mỗi môi trường triển khai một artifact; `environments/*.ts` mang `apiBaseUrl` và cờ `production` | `docs/wiki-core/fe/17-phuc-vu-va-trien-khai.md:145` · `docs/quy-uoc/fe-architecture.md:76` · `src/FE/src/environments/environment.ts`, chuỗi `một artifact riêng cho mỗi môi trường` |
| Build sản xuất | Cấu hình mặc định của target build là `production`; nó dùng thẳng `environment.ts` — tệp đang mang placeholder `api.core-and-skill.invalid` | `src/FE/angular.json`, chuỗi `"defaultConfiguration": "production"` |
| Biên dịch | `ng build` biên dịch **mọi** tệp `.ts` dưới `src/` | `src/FE/tsconfig.app.json`, chuỗi `"src/**/*.ts"` |
| CI | Job `frontend-gate`, bước `4. ng build`, chạy `npx ng build` — tức build sản xuất — trên `environment.ts` của repo | `.github/workflows/docs-gate.yml`, chuỗi `run: npx ng build` |

Ghi nhận, không quyết ở đây: với mỗi công ty một bản cài, bộ máy đơn vị của [ADR-0013](0013-multi-tenant.md) — viết cho nhiều cơ quan chung một bản cài (`docs/adr/0013-multi-tenant.md:15`) — ở dự án này chỉ còn làm một việc: tách đơn vị hệ thống khỏi đơn vị của công ty. Đó **chưa** là lý do lật ADR-0013: code đã có, và Core còn mang sang dự án khác. Xem mục *Dấu hiệu*.

## Quyết định

Người dùng chọn (2026-10-06) lấy mã đơn vị nghiệp vụ từ cấu hình của bản cài, giữ nguyên hợp đồng và luồng đăng nhập phía BE, và buộc build sản xuất hỏng khi thiếu mã. Kiến trúc sư chốt cơ chế như sau:

1. **Mã đơn vị nghiệp vụ là một khoá của `environment`, nhúng lúc build, nằm trong tệp môi trường của bản cài.** Người triển khai tạo tệp đó cho từng bản cài, đặt mã bằng đúng giá trị đã đặt cho `Core:Bootstrap:FirstTenantCode`.
2. **Form đăng nhập có hai ô** — tên đăng nhập, mật khẩu. FE gửi mã cấu hình làm `tenantCode`; thân request không đổi. Người dùng cuối không thấy ô mã đơn vị ở bất kỳ trạng thái nào.
3. **Dưới form có liên kết "Quản trị hệ thống"**. Bấm vào thì **cùng trang đăng nhập** chuyển sang chế độ vận hành, trạng thái nằm trên URL (tham số truy vấn) nên đánh dấu được; chế độ này hiện thêm ô mã đơn vị, người vận hành **gõ tay** mã đơn vị hệ thống. Mã đơn vị hệ thống **không** nằm trong cấu hình FE.
4. **Build sản xuất hỏng khi bản cài không khai mã** — cơ chế ở QC4.
5. **BE không đổi gì**: không endpoint mới, không khoá cấu hình mới, không nhánh mới trong luồng đăng nhập.

## Quyết định con

Nhãn **[architect]** = kiến trúc sư chốt; **[người dùng chốt]** = người dùng đã chọn ngày 2026-10-06.

### QC1 — Mã cấu hình nằm ở đâu, ai đặt · [architect]

| Phương án | Được | Mất |
| --- | --- | --- |
| **1A — khoá trong `environment`, nhúng lúc build** | Đúng cơ chế đang dùng cho `apiBaseUrl`; mỗi bản cài vốn đã có artifact riêng vì `apiBaseUrl`, nên không tăng số artifact; không request thêm lúc khởi động; BE và hợp đồng không đổi | Hai giá trị phải bằng nhau mà không máy nào kiểm: mã FE và mã đơn vị trong database (đặt qua khoá bootstrap). Đặt sai thì **mọi** lần đăng nhập trượt bằng câu chung *"sai thông tin đăng nhập"*, và sửa phải build lại FE |
| 1B — tệp JSON trong `public/`, đọc lúc chạy | Sửa mã sai không cần build lại | Thêm một request chặn lúc khởi động, tệp không được cache, cần màn lỗi khi tải hỏng — các ràng buộc ở `docs/wiki-core/fe/17-phuc-vu-va-trien-khai.md` §5.3. Lợi ích *"một artifact cho mọi bản cài"* không đến được vì `apiBaseUrl` vẫn nhúng lúc build. Thiếu mã chỉ lộ lúc chạy — trái 4C |
| 1C — BE trả mã qua một endpoint ẩn danh | Một nguồn ở phía server | Hợp đồng API đổi — trái điều kiện người dùng đặt. Thêm một mục vào allowlist `[AllowAnonymous]` (luật S4). BE không có sẵn nguồn để trả: khoá bootstrap không được tiến trình phục vụ đọc, nên phải thêm một khoá cấu hình BE mới — lại là hai giá trị — hoặc chọn *"đơn vị nghiệp vụ duy nhất"* trong database, cách hỏng ngay khi bản cài có đơn vị thứ hai. Phát mã cho bất kỳ ai gọi — vô hại với bản cài một công ty, không nên ở bản cài nhiều đơn vị |
| 1D — BE nhận `tenantCode` rỗng và tự dùng mã mặc định | Không cấu hình FE | Đổi hợp đồng (`tenantCode` thành tuỳ chọn) và thêm vào luồng nhạy cảm nhất một nhánh *kích hoạt bằng việc bỏ trống một ô* — đúng thứ [ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md) phương án E đã loại. Trái điều kiện người dùng đặt |

**Chốt: 1A.** Người đặt: người triển khai bản cài, cùng lúc với khoá bootstrap. Không ép một nguồn duy nhất cho hai giá trị: muốn vậy phải dựng quy trình build FE đọc biến môi trường của bản cài, mà hôm nay chưa có quy trình triển khai FE nào. Runbook thêm một bước kiểm sau cài: đăng nhập tài khoản quản trị đơn vị — trượt thì mã FE lệch.

### QC2 — Liên kết "Quản trị hệ thống" hoạt động thế nào · [architect; phần mã gõ tay: người dùng chốt]

| Phương án | Được | Mất |
| --- | --- | --- |
| **2A — cùng trang, chế độ vận hành nằm trên URL bằng tham số truy vấn** | Một màn, một form, một đường gửi; liên kết là điều hướng thật nên đúng ngữ nghĩa *liên kết* về truy cập; người vận hành đánh dấu thẳng chế độ của mình; tải lại trang không mất chế độ | Trang đăng nhập có hai trạng thái phải test |
| 2B — route riêng cho đăng nhập vận hành | Tách bạch | Hai trang gần trùng nhau — lệch dần khi một bên được sửa |
| 2C — nút bật tắt ô, không ghi lên URL | Đơn giản nhất | Tải lại trang là mất chế độ; không đánh dấu được; một nút đổi bố cục tại chỗ mang ngữ nghĩa khác một liên kết |

**Chốt: 2A.** Tên tham số do `frontend-expert` khai ở quy ước routing.

**Mã đơn vị hệ thống: gõ tay** (người dùng chốt). Đưa nó vào cấu hình FE thì mã đó nằm trong bundle JS ai cũng đọc được, và bản cài phải giữ thêm một giá trị nữa cho khớp `Core:Bootstrap:SystemTenantCode`.

**Có làm lộ gì không.** Liên kết chỉ cho biết bản cài có khu quản trị hệ thống — một tính năng chung của Core, không bí mật. Nó không làm lộ mã đơn vị hệ thống. Ô mã đơn vị ở chế độ vận hành nhận mã nào cũng được, kể cả mã của công ty; mọi ca trượt vẫn trả một mã lỗi chung — đúng hành vi của form ba ô hôm nay. Mã đơn vị của công ty nằm trong bundle; với bản cài một công ty, nó không nói thêm điều gì mà tên miền chưa nói.

### QC3 — Quan hệ với `Core:Bootstrap:FirstTenantCode` · [architect]

Hai khoá, hai vai, **một giá trị**:

| Khoá | Ai đọc | Khi nào |
| --- | --- | --- |
| `Core:Bootstrap:FirstTenantCode` | Lệnh bootstrap | Một lần, lúc tạo đơn vị nghiệp vụ đầu tiên. Sau đó database là nơi giữ mã |
| Khoá mã đơn vị trong tệp môi trường của bản cài | FE | Mỗi lần đăng nhập ở chế độ thường |

Thứ phải bằng nhau thật ra là **mã FE và cột `code` của đơn vị nghiệp vụ trong database**; khoá bootstrap chỉ là đường đặt cột đó lần đầu. Ràng buộc này ghi ở runbook, cạnh hàng khoá bootstrap, kèm bước kiểm sau cài ở QC1. Không gộp hai khoá: chúng thuộc hai tiến trình, hai thời điểm, và khoá bootstrap cố ý không được tiến trình phục vụ đọc.

### QC4 — Bản cài không khai mã · [người dùng chốt: 4C]

| Phương án | Được | Mất |
| --- | --- | --- |
| 4A — form hiện ô mã đơn vị như hôm nay, ẩn liên kết | Một quy tắc, có sẵn chế độ cho bản cài nhiều đơn vị | Người dùng cuối thấy ô mã đơn vị khi bản cài quên khai — trái điều người dùng đặt ra |
| 4B — màn lỗi cấu hình lúc chạy | Lỗi lộ ngay | Artifact hỏng đã lên máy chủ; người vận hành cũng bị khoá ngoài cho tới khi build lại |
| **4C — build sản xuất hỏng khi thiếu mã** | Không artifact thiếu mã nào ra được tới máy chủ; người dùng cuối không bao giờ thấy ô | Phải dựng phép kiểm lúc build; bản cài nhiều đơn vị cần ADR mới (QC5) |

#### Cơ chế của 4C · [architect]

**Build nào hỏng.** `ng build` ở cấu hình `production` — cấu hình mặc định của target build, nên `npx ng build` và `npm run build` đều đi qua nó. Build `development` và `ng serve` không đổi.

**Hỏng bằng gì — hai lớp:**

| Lớp | Bắt ca nào | Cơ chế |
| --- | --- | --- |
| 1 | Bản cài **không có** tệp môi trường riêng | Cấu hình `production` khai `fileReplacements` thay `environment.ts` bằng **tệp môi trường của bản cài** (tên gợi ý `environment.ban-cai.ts`, cùng thư mục). Tệp này **không vào git** của repo (`.gitignore`); người triển khai tạo nó cho từng bản cài. Thiếu tệp ⇒ build dừng vì không có tệp thay thế |
| 2 | Tệp có nhưng mã để **rỗng** | Tệp bản cài khai giá trị qua một hàm khai báo ở `core/` mà kiểu tham số từ chối chuỗi rỗng ⇒ lỗi biên dịch. Mẫu cho người triển khai chép **không mang đuôi `.ts`**: `ng build` biên dịch mọi `src/**/*.ts`, nên một mẫu `.ts` để trống sẽ làm hỏng mọi build kể cả CI |

Giới hạn, nói thẳng: lớp 2 chỉ bắt tệp viết theo mẫu. Tệp viết tay không qua hàm khai báo, hoặc mã sai mà không rỗng, thì không lớp nào bắt — lưới cuối là bước kiểm sau cài ở runbook.

**`environment.ts` trong git** thành tệp nền: cho `ng test`, `ng lint` và kiểu của `environment`; **không bao giờ** vào artifact sản xuất. Giá trị của nó là placeholder **không rỗng**, để `ng test` vẫn biên dịch được. `environment.development.ts` mang `DEMO`, khớp `appsettings.Development.json`.

**Phương án cơ chế đã loại:**

- *Script `prebuild` của npm* — chỉ chạy với `npm run build`, không chạy với `npx ng build`, đúng lệnh CI và phần lớn người dùng gõ. Kiểm phụ thuộc người nhớ dùng đúng lệnh.
- *Kiểm trong `scripts/fe-gate.sh`* — cổng đó chạy ở CI và máy dev, không chạy trên máy dựng bản cài; nó không chặn được artifact.
- *Kiểu chặn chuỗi rỗng ngay trong `environment.ts`* — `environment.ts` của repo không có mã thật, nên mọi `ng test` và `ng build` của chính repo đỏ.
- *Một cấu hình build `ci` riêng thay tệp môi trường bằng giá trị giả* — CI build theo một đường mà không bản cài nào đi; cách bên dưới kiểm đúng đường thật.

**CI không đỏ vĩnh viễn.** Bước `4. ng build` của job `frontend-gate` tách làm hai:

| Bước | Lệnh | Vì sao |
| --- | --- | --- |
| 4a — canary của 4C | `npx ng build` khi **chưa có** tệp bản cài phải **hỏng**, và thông báo lỗi phải nêu tên tệp bản cài | Ai gỡ dòng `fileReplacements` khỏi cấu hình `production` là 4C biến mất trong im lặng — build lại xanh bằng placeholder. Bước này bắt đúng ca đó. Hỏng vì lý do khác (thông báo không nêu tên tệp) thì bước này đỏ |
| 4b — build thật | Chép một **tệp bản cài giả** có sẵn trong repo (đặt ngoài `src/`, giá trị giả, `apiBaseUrl` miền `.invalid`) vào chỗ, rồi `npx ng build` | CI build đúng đường mà một bản cài thật đi |

Bước 4a không phải một cổng mới theo nghĩa của [ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md): người dùng chọn 4C, tức chọn một phép chặn; 4a là thứ duy nhất chứng minh phép chặn đó còn sống. Thiếu nó, 4C là một cổng xanh rỗng.

**Hệ quả phụ:** `apiBaseUrl` cũng nằm trong tệp bản cài, nên build sản xuất không còn ra được artifact mang placeholder `api.core-and-skill.invalid`. Cái giá *"build cho môi trường A đem lên B"* ở `docs/wiki-core/fe/17-phuc-vu-va-trien-khai.md` §5.2 vẫn còn; ca *"quên thay placeholder"* thì hết.

### QC5 — Đường mở rộng khi một bản cài có nhiều đơn vị · [architect]

BE không khoá cửa: hợp đồng đăng nhập luôn mang `tenantCode`, và BE không biết FE lấy mã từ đâu. FE thì có khoá — đó là cái giá của 4C:

1. **Bản cài nhiều đơn vị, người dùng gõ mã** — cần ADR mới cho một chế độ tường minh (ví dụ một khoá *hiện ô mã đơn vị* trong tệp bản cài) và nới lớp 2 cho chế độ đó. Ô và biến thể của nó đã có sẵn ở chế độ vận hành, nên phần FE là một nhánh hiển thị.
2. **Bản cài nhiều đơn vị, không muốn gõ mã** — phân tích tên miền con ở [ADR-0115](0115-bo-o-ma-don-vi-suy-don-vi-tu-ten-mien-con-phien-chi-dung-duoc-o-origin-cua-don-vi-minh.md) là điểm xuất phát, kèm điều kiện hạ tầng ở QC6 của ADR đó.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ ô mã đơn vị, điền sẵn mã cấu hình, vẫn cho sửa

**Được:** đơn giản hơn quyết định này — không chế độ, không liên kết; tài khoản vận hành xoá mã điền sẵn rồi gõ mã hệ thống. **Mất:** form vẫn ba ô; người dùng thường thấy và có thể vô tình sửa một ô họ không cần biết. **Vì sao loại:** người dùng cuối không được thấy ô.

### Phương án B — Giữ ô, FE nhớ mã lần trước trong trình duyệt

**Được:** chỉ sửa FE, không cấu hình. **Mất:** lần đầu trên mỗi máy vẫn phải gõ; ô vẫn còn. **Vì sao loại:** với bản cài một công ty, mã đã biết trước khi có người dùng nào.

### Phương án C — Suy từ tên miền con

**Vì sao loại:** [ADR-0115](0115-bo-o-ma-don-vi-suy-don-vi-tu-ten-mien-con-phien-chi-dung-duoc-o-origin-cua-don-vi-minh.md) — wildcard DNS, cert, đổi CORS và phép kiểm phiên cho một bài toán mà mỗi bản cài chỉ có một đơn vị nghiệp vụ.

### Phương án D — Bỏ hẳn đơn vị hệ thống, tài khoản vận hành nằm trong đơn vị của công ty

**Được:** mỗi bản cài một đơn vị; form không bao giờ cần ô mã. **Vì sao loại:** lật [ADR-0017](0017-khu-quan-tri-he-thong.md) — ranh giới *tài khoản vận hành không thấy dữ liệu nghiệp vụ* đang do bộ lọc đơn vị giữ, không do một phép kiểm quyền. Bỏ đơn vị hệ thống là đổi ranh giới đó thành kỷ luật lập trình. Không ai đề nghị, và nó không cần để bỏ được ô.

### Phương án E — Nhập mã đơn vị sau khi đăng nhập

**Vì sao loại:** không làm được mà không đổi mô hình. Tên đăng nhập chỉ duy nhất trong một đơn vị; chưa có mã thì `UserManager` không định danh được người, và chọn đơn vị sau xác thực là hướng khoá `K01`.

Các phương án về chỗ đặt cấu hình (1B, 1C, 1D), về liên kết (2B, 2C), về cấu hình thiếu (4A, 4B) và về cơ chế của 4C nằm ở mục *Quyết định con*.

## Hệ quả

### Tích cực

- Người dùng cuối đăng nhập bằng hai ô và không bao giờ thấy ô mã đơn vị.
- BE, hợp đồng, test BE, giới hạn tần suất theo cặp mã + tên đăng nhập, ngoại lệ đăng nhập của luật M2: không đổi.
- Không hạ tầng mới, không endpoint ẩn danh mới; mã đơn vị hệ thống không vào bundle.
- Không artifact sản xuất nào ra được khi thiếu tệp môi trường của bản cài — kể cả ca quên thay `apiBaseUrl` placeholder.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Hai giá trị phải bằng nhau, không máy nào kiểm** | Mã FE và mã đơn vị nghiệp vụ trong database. Lệch thì **mọi** người dùng của công ty trượt đăng nhập bằng câu chung, không ai đoán ra nguyên nhân; sửa phải build lại và triển khai lại FE. Chỉ bước kiểm sau cài canh. Người vận hành vẫn vào được qua liên kết |
| **Repo không còn build sản xuất được khi vừa clone** | `npx ng build` trên bản clone sạch hỏng — có chủ đích. Người mới phải đọc runbook; lệnh đo kích thước bundle ở tài liệu cổng FE phải chép tệp bản cài giả trước |
| **Lớp 2 chỉ bắt tệp viết theo mẫu** | Tệp viết tay không qua hàm khai báo, hoặc mã sai mà không rỗng, lọt qua build |
| **Bản cài nhiều đơn vị cần ADR mới ở FE** | Không còn đường *"để trống mã là về form ba ô"* — QC5 |
| **CI thêm một bước** | Bước 4a; rẻ vì build hỏng ngay ở khâu tìm tệp thay thế — phải xác nhận lúc thi công |
| **Ở máy dev, hai giá trị cùng nằm trong git ở hai tệp** | `environment.development.ts` và `appsettings.Development.json` phải cùng `DEMO`. Lệch thì lộ ở lần đăng nhập dev đầu tiên, nhưng không test nào bắt |
| **Trang đăng nhập có hai trạng thái** | Thêm nhánh hiển thị và test cho một màn mà hôm nay chỉ có một trạng thái |
| **Lỗi kiểm dữ liệu của `TenantCode` không còn ô để gắn** ở chế độ thường | Chỉ xảy ra khi mã cấu hình sai khuôn hay quá dài; FE hiện ở mức form |
| **Tài khoản vận hành thêm một bước** | Bấm liên kết trước khi gõ — hoặc đánh dấu thẳng URL chế độ vận hành |

## Sáu câu hỏi bắt buộc

| # | Câu hỏi | Trả lời | Chắc chắn? |
| --- | --- | --- | --- |
| 1 | Vấn đề thật | Với bản cài một công ty, mã đơn vị là một hằng số cho mọi người dùng — bắt gõ là ma sát không mang thông tin. Suy ra từ mô hình triển khai, không đo; chưa có người dùng thật | Có, về logic; chưa đo |
| 2 | Phương án đơn giản hơn | Phương án A (điền sẵn, vẫn hiện ô); loại vì người dùng cuối không được thấy ô | Có |
| 3 | Chi phí vận hành thêm | Mỗi bản cài một tệp môi trường riêng ngoài git, mã trong đó phải bằng mã trong database; một bước kiểm sau cài | Có |
| 4 | Ai bảo trì | Trang đăng nhập, hàm khai báo và cấu hình build thuộc FE — `frontend-expert` thi công, `core-reviewer` soát phần trong `core-paths`. Tệp bản cài: người triển khai từng bản cài | Có |
| 5 | Rút lui thế nào | Đảo commit của việc F2 (trang đăng nhập) và F3 (cơ chế 4C) — form ba ô và build sản xuất từ `environment.ts` quay lại. Không dữ liệu nào sinh ra, BE không đổi. Tệp bản cài đã tạo ở các máy dựng thì bỏ đi | Có |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Khoá mã là cấu hình triển khai, mỗi bản cài một giá trị; Core không biết công ty nào | Có |

## Dấu hiệu quyết định này bắt đầu sai

- Có bản cài cần đơn vị nghiệp vụ thứ hai — QC5, ADR mới.
- Sự cố *"không ai đăng nhập được sau triển khai"* lặp lại vì mã FE lệch — xét đưa mã về một nguồn ở quy trình build (QC1).
- Có người gỡ bước 4a hay thêm một tệp bản cài vào git *"cho CI xanh"* — 4C đang bị tắt.
- Có đề nghị đưa mã đơn vị hệ thống vào cấu hình FE *"cho người vận hành đỡ gõ"*.
- Có đề nghị cho BE tự điền mã khi `tenantCode` rỗng — đó là 1D, đổi hợp đồng và thêm nhánh vào luồng đăng nhập.
- Mọi bản cài đều chỉ có đúng hai đơn vị trong thời gian dài, và chi phí bảo trì bộ máy đơn vị bắt đầu bị nhắc tới — lúc đó mới là câu hỏi về [ADR-0013](0013-multi-tenant.md), không phải về form đăng nhập.

## Việc sau khi chấp nhận — mỗi việc 30–40 phút

| # | Agent | Việc | Tệp |
| --- | --- | --- | --- |
| A1 | `architect` | Tệp luật đơn vị: §5 (câu ngoại lệ đăng nhập — mã đến từ client, không nhất thiết do người gõ), §10 (tài khoản vận hành gõ mã sau liên kết), §11.1 (phương án đang chọn; hàng tên miền con trỏ ADR-0115). Tệp Identity: §6.2 và hàng *Ô mã đơn vị trên form* ở bảng *Áp dụng* | [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) · [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) |
| A2 | `architect` | Card đăng nhập §3: ghi chú của `tenantCode` (nguồn là cấu hình bản cài hoặc ô ở chế độ vận hành; thân không đổi, `Status` giữ nguyên) và đoạn về tài khoản vận hành. Luồng đăng nhập §1 và bước 2; luồng cài đặt bước 6 | [`../contracts/auth.md`](../contracts/auth.md) · [`../luong/D1-dang-nhap.md`](../luong/D1-dang-nhap.md) · [`../luong/V1-cai-dat-lan-dau.md`](../luong/V1-cai-dat-lan-dau.md) |
| A3 | `architect` | Runbook: bước tạo tệp môi trường của bản cài trước khi build FE, hàng khoá mã FE cạnh `FirstTenantCode`, hàng mã đơn vị hệ thống (gõ sau liên kết), bước kiểm sau cài. Quy ước FE §1.1: tệp môi trường của bản cài, `environment.ts` không vào sản phẩm. Phục vụ FE §5.2. Tài liệu cổng FE: bước 4a/4b và lệnh đo bundle. Bảng luật FE: một hàng cho 4C, ép bằng bước 4a | [`../database/script-runbook.md`](../database/script-runbook.md) · [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) · [`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) · [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) · [`../RULES-FE.md`](../RULES-FE.md) |
| D1 | `design-expert` | Màn đăng nhập: hai ô, liên kết "Quản trị hệ thống" dưới form, chế độ vận hành ba ô, focus mặc định vào ô tên đăng nhập, khoá dịch của liên kết. Không có trạng thái *thiếu mã cấu hình*. `AuthField` chỉ sửa nếu biến thể mã đơn vị đổi — biến thể đó vẫn dùng ở chế độ vận hành | [`../Design/Screens/01-dang-nhap.md`](../Design/Screens/01-dang-nhap.md) · [`../Design/Components/AuthField.md`](../Design/Components/AuthField.md) |
| F1 | `frontend-expert` | Khoá mã đơn vị trong `environment` (`environment.ts` mang placeholder không rỗng; `environment.development.ts` mang `DEMO`); cấp giá trị qua token ở `core/` theo khuôn `API_BASE_URL`; hàm khai báo từ chối chuỗi rỗng; spec | `src/FE/src/environments/environment.ts` · `src/FE/src/environments/environment.development.ts` |
| F2 | `frontend-expert` | Trang đăng nhập: hai ô ở chế độ thường, gửi mã cấu hình; liên kết sang chế độ vận hành bằng tham số truy vấn của cùng route; focus; lỗi `TenantCode` ở mức form; khoá dịch; spec cho hai trạng thái | `src/FE/src/app/platform/xac-thuc/pages/dang-nhap/dang-nhap.page.ts`, tệp HTML và spec cùng thư mục |
| F3 | `frontend-expert` | Cơ chế 4C: `fileReplacements` của cấu hình `production`; dòng `.gitignore` cho tệp bản cài; mẫu tệp bản cài không đuôi `.ts`; tệp bản cài giả cho CI ngoài `src/`; tách bước `4. ng build` thành 4a, 4b. Chạy thử trên máy cả hai ca — không tệp thì hỏng và thông báo nêu tên tệp; có tệp giả thì xanh — và ca mã rỗng hỏng ở biên dịch | `src/FE/angular.json` · `.gitignore` · `.github/workflows/docs-gate.yml` |

Sau F1–F3, phiên chính gọi `core-reviewer` phạm vi FE — `core/` và `platform/` của FE nằm trong khối `core-paths` ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md). BE không có việc. Không cần `test-engineer`: test là spec đi kèm F1, F2 và hai ca chạy thử của F3.

**Luật:** một hàng mới ở bảng luật FE — *build sản xuất hỏng khi thiếu tệp môi trường của bản cài* — ép bằng bước 4a của job `frontend-gate`, trạng thái `📐` tới khi bước đó có trong workflow. Hành vi ẩn, hiện ô là hành vi của một màn, canh bằng spec ở F2, không thành hàng luật. Bất biến *mã FE bằng mã đơn vị nghiệp vụ trong database* là bất biến triển khai — sống ở runbook, canh bằng bước kiểm sau cài do người vận hành chạy, không cổng máy nào.

## Liên quan

- [ADR-0115](0115-bo-o-ma-don-vi-suy-don-vi-tu-ten-mien-con-phien-chi-dung-duoc-o-origin-cua-don-vi-minh.md) — hướng tên miền con, đã loại bỏ
- [ADR-0013](0013-multi-tenant.md) — mô hình đơn vị, hướng khoá `K01`
- [ADR-0017](0017-khu-quan-tri-he-thong.md) — đơn vị hệ thống và tài khoản vận hành
- [ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md) §3, §6 — lệnh bootstrap, mã đơn vị hệ thống
- [ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md) — điều kiện dựng cổng mới
- [`../contracts/auth.md`](../contracts/auth.md) §3 · [`../luong/D1-dang-nhap.md`](../luong/D1-dang-nhap.md) · [`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §5
