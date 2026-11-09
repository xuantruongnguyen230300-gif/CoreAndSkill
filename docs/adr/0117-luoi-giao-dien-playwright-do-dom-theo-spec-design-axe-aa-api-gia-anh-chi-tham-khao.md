---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0117 — FE có lưới chặn lỗi giao diện bằng Playwright: đo DOM và bố cục theo spec `docs/Design/`, quét axe WCAG A/AA, API giả mang kiểu DTO; ảnh chụp chỉ để tham khảo, không so, không commit; chạy trong `frontend-gate`, đỏ là không hợp nhất

> **Trạng thái:** Đã chấp nhận (2026-10-06)

## Bối cảnh

### Lỗi giao diện lọt qua mọi cổng FE trong hai ngày 05–06/10

| # | Lỗi | Neo | Vì sao cổng hiện có không thấy |
| --- | --- | --- | --- |
| 1 | URL `/` ra trang trắng | [`../audit/2026-10-05-nhanh-path-rong-khong-component-nuot-url-goc.md`](../audit/2026-10-05-nhanh-path-rong-khong-component-nuot-url-goc.md) | Hành vi chỉ có khi router chạy trên mảng route thật; console sạch, build sạch |
| 2 | Ô đang focus có hai đường song song — viền rồi vòng cách 2px; ô lỗi thành đỏ trong, xanh ngoài | [`AuthField.md`](../Design/Components/AuthField.md) bảng *Đã có → còn thiếu*, dòng `focus-visible`; [`DESIGN.md`](../Design/DESIGN.md) §2.6 *Ô nhập chữ* | Hai khối CSS cùng độ ưu tiên; không công cụ nào đọc thứ tự cascade |
| 3 | Khe ô → ô → nút chính bằng 0 | [`AuthCard.md`](../Design/Components/AuthCard.md) bảng *Đã có → còn thiếu*, dòng khe trong form | Style đóng gói của khung không tới nội dung chiếu vào; chỉ thấy khi đo trang đã render |
| 4 | Ô trống hiện chữ `null` | [`AuthField.md`](../Design/Components/AuthField.md) dòng `placeholder` = `null`; [`Input.md`](../Design/Components/Input.md) cùng khuôn | Gắn thuộc tính DOM nhận `null` là template hợp lệ |
| 5 | Nền xanh nhạt khi Chrome tự điền | [`AuthField.md`](../Design/Components/AuthField.md) dòng *Trình duyệt tự điền* — ảnh người dùng gửi 2026-10-05 | Style mặc định của trình duyệt; trình duyệt không giao diện không tự điền |
| 6 | `Footer` làm trang xác thực luôn cuộn thêm | [`AuthCard.md`](../Design/Components/AuthCard.md) dòng `default`; [`01-dang-nhap.md`](../Design/Screens/01-dang-nhap.md) bảng *Có thật hôm nay → sẽ thành*, dòng `Footer` | Chiều cao tính từ hai khối độc lập; chỉ thấy khi đo |
| 7 | Khối hover đè viền lỗi | Người dùng báo 2026-10-06; chưa có dòng neo trong `docs/Design/` | Thứ tự cascade giữa hover và lỗi |
| 8 | Nhãn nút chính là `Arial` trong khi cả màn là `Be Vietnam Pro` | [`Button.md`](../Design/Components/Button.md) dòng *Họ font của nhãn* | `<button>` không kế thừa họ font của trang |

Cả tám lỗi đi qua `ng test` (Karma), `ng lint`, `scripts/fe-gate.sh` và `ng build`. Chúng lộ ra khi có người mở trang: lượt audit chạy Chrome không giao diện qua DevTools Protocol ngày 2026-10-05, hoặc chính người dùng.

### Tài liệu luật đang xếp lớp lỗi này sai chỗ

- [`06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §1 (dòng 25) xếp bố cục, màu vào ưu tiên thấp vì *"sai thì thấy ngay lần chạy đầu"*; §5 (dòng 139) loại test bố cục và pixel vì *"đỏ mỗi lần đổi thiết kế"*; §10 (dòng 216) hoãn hồi quy hình ảnh. Tám lỗi trên đã chạy qua nhiều lần mà không ai thấy — giả định *"thấy ngay"* sai với chính lớp lỗi nó nói tới.
- [`05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §10 (dòng 648) ghi *trải nghiệm hỏng* là thứ cổng không bao giờ bắt.
- Playwright đã là công cụ E2E đã chọn ([`06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §6, dòng 161), nhưng E2E hoãn vì chưa có cách dựng dữ liệu thử ổn định (dòng 159). API giả gỡ điều kiện đó cho việc kiểm giao diện. Bốn luồng E2E chạy BE thật ở §6 vẫn hoãn — ADR này không đụng tới chúng.

### Người dùng chọn hướng, rồi bỏ phần so ảnh

Ngày 2026-10-06, người dùng chọn hướng: Playwright chụp ảnh, axe-core quét trợ năng, API giả nên CI không cần BE hay DB. Lý do: muốn giao diện thật chuẩn, không lặp lại các lỗi trên.

Bản đề xuất của ADR này dựng thêm một lớp so ảnh với ảnh chuẩn commit vào repo, và hỏi người dùng ảnh chuẩn lưu ở đâu. Người dùng trả lời cùng ngày: *"chỉ tham khảo chứ không dùng làm tài liệu, vì đã có sẵn docs/Design rồi"*. Đọc theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §7 — `docs/Design/` là nguồn giao diện duy nhất — câu trả lời đó có nghĩa: **ảnh chụp không thành ảnh chuẩn để so, không vào repo**. Một ảnh chuẩn mà test so điểm ảnh với nó chính là một nguồn giao diện thứ hai, có quyền phán đúng sai. Cơ chế so ảnh ở dạng đã đề xuất nằm lại ở phương án B, kèm cái giá của việc bỏ nó.

### Ràng buộc có thật, đo ngày 2026-10-06

- `src/FE/package.json` chưa có gói Playwright hay axe nào. Node của FE đọc từ `src/FE/.nvmrc`; khoá `engines` nhận Node 20.19+, 22.12+ và từ 24.
- Bản `latest` trên npm: `@playwright/test` 1.63.0 (đòi Node từ 20), `@axe-core/playwright` 4.13.0 — gói này kéo `axe-core` theo khoảng `~4.13.0`. Đo bằng `npm view <gói> version`. Đây là số lúc ghi ADR, không phải số được chọn: nguồn phiên bản vẫn chỉ là `package.json` ([ADR-0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) quyết định 4).
- Job `frontend-gate` chạy trên `ubuntu-latest`, Node từ `.nvmrc`, theo thứ tự script → lint → test → build ([`docs-gate.yml`](../../.github/workflows/docs-gate.yml)); đã chạy xanh lần đầu ([`../README.md`](../README.md) mục *Trạng thái repo*).
- `src/FE/angular.json` khối `serve` bật SSL bằng chứng chỉ ngoài git — chuỗi `"sslCert": ".cert/localhost.pem"`. Máy CI không có chứng chỉ đó.
- Font chữ là tệp cục bộ trong `src/FE/public/fonts/`: chữ render không phụ thuộc font hệ thống, trừ phần tử rơi về font dự phòng — đúng ca lỗi 8.
- `src/FE/.browserslistrc` khai hỗ trợ Chrome, Edge, Firefox, Safari, iOS.
- FE có sẵn kiểu envelope — `src/FE/src/app/core/http/api-result.model.ts`, chuỗi `interface ApiSuccess<T>` và `interface ApiFailure` — và kiểu DTO ở các tệp `*.dto.ts`.

## Quyết định

Người dùng chọn (2026-10-06) lưới chặn lỗi giao diện bằng Playwright và axe-core trên API giả, ảnh chụp chỉ để tham khảo. Kiến trúc sư chốt cơ chế như sau:

1. **Một bộ kiểm giao diện — gọi là *lưới giao diện* — nằm ở `src/FE/e2e/`**, chạy ứng dụng thật qua `ng serve` với cấu hình `e2e`, chặn mọi request API bằng `page.route`.
2. **Hai lớp chặn.** **A** — đo DOM và bố cục; **mỗi phép đo neo về một dòng spec ở `docs/Design/`**, giá trị token đọc từ trang lúc chạy. **C** — axe theo WCAG A/AA. Thêm hai điều kiện hợp lệ của lượt chạy: không lỗi console, không request ngoài dữ liệu giả.
3. **Ảnh chụp chỉ để tham khảo.** Mỗi trạng thái được kiểm chụp một ảnh vào thư mục kết quả bị `.gitignore` loại, đính vào báo cáo và tải lên artifact CI. Không commit, không so, không chặn, không phải tài liệu.
4. **Cùng một lệnh trên máy dev và CI**, Chromium do Playwright cài theo đúng bản đã ghim. Không image Docker, không Git LFS.
5. **Chạy là các bước cuối của job `frontend-gate`**; đỏ là không hợp nhất, không chạy lại khi đỏ.
6. **Chỉ Chromium.**
7. **`src/FE/e2e/` và `src/FE/playwright.config.ts` vào khối `core-paths`** — cùng lý do `scripts/tests/` đã nằm ở đó: nới một phép đo hay gỡ một ca là tắt một cổng của Core ([ADR-0059](0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md)).

Quan hệ: **bổ sung** [ADR-0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) — Karma + Jasmine vẫn là bộ chạy unit test; lưới giao diện là lớp thêm vào, không thay.

## Quyết định con

**[architect]**: kiến trúc sư chốt. **[người dùng chốt]**: người dùng chọn, ngày 2026-10-06.

### QC1 — Gói, phiên bản, chỗ đặt, cấu hình, script · [architect]

**Gói.** Ba gói vào `devDependencies`, ghim chính xác, không `^` hay `~`: `@playwright/test`, `@axe-core/playwright`, và `axe-core` khai **tường minh**. Lý do khai `axe-core`: gói bọc kéo nó theo khoảng `~`, nên lần sinh lại lockfile theo [`repo-artifact.md`](../quy-uoc/repo-artifact.md) §3 điều 3 có thể đổi bộ luật axe mà `package.json` không đổi dòng nào — vi phạm mới xuất hiện hay biến mất mà code không đổi. Cùng lý lẽ với [ADR-0061](0061-primeicons-la-phu-thuoc-tuong-minh-ghim-chinh-xac-nap-mot-lan.md). Bản ghim là bản `latest` lúc cài; tài liệu không chép số, đọc bằng `npm ls`.

**Chỗ đặt:**

| Đường dẫn | Chứa gì |
| --- | --- |
| `src/FE/playwright.config.ts` | Cấu hình |
| `src/FE/e2e/tsconfig.json` | Kiểu chặt; ép `import type` khi lấy kiểu từ `src/app/` (ví dụ bằng `verbatimModuleSyntax`) để Playwright không nạp mã Angular lúc chạy |
| `src/FE/e2e/ha-tang/` | Chặn API, dựng envelope, các phép đo lớp A, bọc axe, tệp ngoại lệ axe |
| `src/FE/e2e/du-lieu-gia/` | Dữ liệu giả, mỗi tệp ứng với một card ở `docs/contracts/` |
| `src/FE/e2e/man/` | Mỗi tệp spec màn ở `docs/Design/Screens/` một tệp spec cùng số và tên — `01-dang-nhap.spec.ts` |
| `src/FE/src/environments/environment.e2e.ts` + cấu hình `e2e` trong `angular.json` | Build: tối ưu như `production`, thay `environment.ts` bằng tệp e2e, không `budgets` (bước 4b của `frontend-gate` đã canh). Serve: tắt SSL, cổng cố định, tắt nạp lại nóng. Tệp e2e mang giá trị giả, `apiBaseUrl` trên miền `.invalid`, vào git như `environment.development.ts` |

Cấu hình `e2e` **không** dùng tệp bản cài của [ADR-0116](0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md): máy dev có thể đang giữ tệp bản cài thật, và kết quả lưới không được phụ thuộc giá trị riêng của một máy.

**Cấu hình Playwright:** một trình duyệt Chromium, bản do Playwright cài; các project theo khung nhìn × theme (QC3); `locale` `vi-VN`, `timezoneId` `Asia/Ho_Chi_Minh`, `reducedMotion` `reduce`, đồng hồ cố định qua `page.clock`; `retries: 0`; `forbidOnly` khi chạy ở CI; `webServer` dựng `ng serve --configuration e2e`. Không dùng `toHaveScreenshot`.

**Script npm** — vai chốt ở đây, tên chốt lúc thi công:

| Script | Vai |
| --- | --- |
| `e2e` | Toàn bộ lưới — lớp A, lớp C, ảnh tham khảo. Cùng một lệnh trên Windows và trên CI |
| `e2e:kieu` | `tsc --noEmit -p e2e/tsconfig.json` — Playwright không kiểm kiểu, nên dữ liệu giả lệch DTO chỉ đỏ ở đây. CI chạy trước `e2e` |

### QC2 — API giả · [architect]

| Khoản | Chốt |
| --- | --- |
| Chặn | `page.route` theo tiền tố `apiBaseUrl` của tệp môi trường e2e. **Đóng mặc định:** request API không có dữ liệu giả bị huỷ và ghi lại; test kết thúc mà còn request chưa khai là đỏ, nêu phương thức và đường dẫn. Request ra ngoài origin của app và ngoài tiền tố API cũng bị chặn |
| Khác nguồn | `apiBaseUrl` e2e khác origin của dev server, đúng hình dạng sản phẩm ([ADR-0015](0015-fe-va-api-khac-nguon.md)). Hạ tầng tự gắn header CORS có credentials và tự trả preflight. Cách Playwright xử lý preflight khi fulfill **chưa kiểm**: việc đầu của hạ tầng là một canary chứng minh POST JSON khác nguồn kèm header XSRF nhận đúng dữ liệu giả. Sai ở đây thì màn hiện câu mất kết nối, và mọi phép đo sau đó chạy trên nhầm trạng thái |
| Envelope | Chỉ dựng qua hai hàm của hạ tầng, trả `ApiSuccess<T>` và `ApiFailure` lấy từ `core/http/api-result.model.ts`. Dữ liệu giả không tự viết `success`, `traceId`; `traceId` cố định |
| Dữ liệu | Module TypeScript trong `e2e/du-lieu-gia/`, đặt tên theo card. `data` mang kiểu DTO của FE qua `import type`. DTO đổi mà dữ liệu giả không theo thì `e2e:kieu` đỏ |
| Mã lỗi | Mỗi `code` trong dữ liệu giả lỗi phải có trong `docs/contracts/`: hạ tầng đọc các card lúc chạy, trích mã trong code span, mã không có là đỏ — kể cả mã chung ở [`auth.md`](../contracts/auth.md) §11. Trích được 0 mã là đỏ, không phải xanh |
| Ai giữ | Hạ tầng — chặn, envelope, đối chiếu mã: `test-engineer`. Dữ liệu giả của một màn: `frontend-expert`, trong cùng PR đổi DTO hay màn đó |

**Giới hạn, nói thẳng:** dữ liệu giả đúng theo **DTO của FE**, không theo BE thật. DTO lệch card hợp đồng là chỗ spec service với `HttpTestingController` canh ([`06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §2.1); lưới này không canh thêm. Hành vi BE thật — mã lỗi mới, thứ tự kiểm — không được thử.

### QC3 — Kiểm gì, ở đâu · [architect]

**Ba khung nhìn** — tham số test, không phải token; đổi điểm ngắt ở [`DESIGN.md`](../Design/DESIGN.md) §6.3 thì người đổi kiểm ba khung còn nằm đúng dải:

| Tên | Khung nhìn | Dải | Vì sao |
| --- | --- | --- | --- |
| `rong` | 1280 × 800 | Từ `$bp-lg` — `Sidebar` cố định | Khổ máy để bàn; lượt audit 2026-10-05 đo ở bề rộng này |
| `vua` | 1024 × 768 | `$bp-md` … `$bp-lg` — `Sidebar` thu gọn | Khổ bảng |
| `hep` | 390 × 844, giả lập cảm ứng | Dưới `$bp-xs` — drawer, nút `Toolbar` giãn | Bật cảm ứng để `(hover: hover)` sai như máy thật |

**Hai theme:** `sang` ở cả ba khung; `toi` ở `rong`. Theme đặt vào `localStorage` trước khi trang tải, đúng cơ chế script nội tuyến của `index.html`.

**Trạng thái được kiểm** — luật bền; danh sách cụ thể đọc từ mục *Trạng thái* của từng spec màn, không chép vào đây:

| # | Trạng thái | Lớp A | axe |
| --- | --- | --- | --- |
| 1 | `mặc định` của mỗi màn | Ba khung `sang`, cộng `rong`·`toi` | `rong`·`sang`, `rong`·`toi`, `hep`·`sang` |
| 2 | Mỗi trạng thái khác ở mục *Trạng thái* của spec màn mà không ghi *không áp dụng* — đang tải, rỗng, lỗi, kiểm tra dữ liệu, chuyển chế độ | `rong`·`sang`, `hep`·`sang` | `rong`·`sang` |
| 3 | Trạng thái tương tác của mỗi biến thể ô nhập chữ và nút: focus đến bằng phím Tab, hover, lỗi × focus, lỗi × hover, vô hiệu — một lần cho mỗi biến thể, trên màn đầu tiên dùng nó | `rong`·`sang`, `rong`·`toi` | — |
| 4 | Lớp nổi: hộp thoại, drawer của khung ở `hep`, `Sidebar` thu gọn ở `vua`, menu thả xuống | Khung nhìn sinh ra lớp đó | Cùng khung nhìn |

Hướng khoá `K21` giữ nguyên: không phép đo nào khẳng định theo chuỗi tiếng Việt.

**Lớp A — mỗi phép đo một nguồn ở `docs/Design/`.** Phép đo chưa có dòng spec nào làm nguồn thì **không bật**. Trong code, mỗi phép đo mang một chuỗi neo tới tệp và mục của dòng spec, để người gặp đỏ mở đúng chỗ; giá trị token đọc từ trang lúc chạy, không chép vào test — giá trị chỉ sống ở `DESIGN.md` (luật D30):

| # | Đo gì | Nguồn ở `docs/Design/` | Lỗi ở Bối cảnh |
| --- | --- | --- | --- |
| A1 | Màn dựng nội dung: đúng một `<h1>`, đang hiện, sau khi điều hướng tới URL của màn | [`PageHeader.md`](../Design/Components/PageHeader.md) — tiêu đề là `<h1>` duy nhất của trang; [`AuthCard.md`](../Design/Components/AuthCard.md) §Accessibility — tiêu đề là `<h1>` trong `<main>` | 1 |
| A2 | Không `null`, `undefined`, `NaN`, `[object Object]` trong chữ hiển thị và trong `placeholder`, `aria-label`, `title`, `alt`; không chuỗi mang hình khoá dịch hiện ra | [`Input.md`](../Design/Components/Input.md) và [`AuthField.md`](../Design/Components/AuthField.md) §API — `placeholder` mặc định `null` nghĩa là không có placeholder; mục *Câu chữ* của spec màn — mọi chữ hiển thị có khoá dịch | 4 |
| A3 | Màn xác thực không có thanh cuộn dọc khi khung nhìn cao hơn nội dung | [`AuthCard.md`](../Design/Components/AuthCard.md) §Responsive — *Đủ chỗ*: `Footer` sát đáy, không thanh cuộn | 6 |
| A4 | Mọi phần tử có chữ hiện ra dùng họ font đầu của `--font-sans` hoặc `--font-mono`, và font đó đã nạp | [`DESIGN.md`](../Design/DESIGN.md) §3 — họ font | 8 |
| A5 | Ô nhập chữ: đúng một đường biên ở mọi trạng thái, không vòng ngoài; màu và độ dày theo hai trục hợp lệ/lỗi × focus; ô lỗi giữ màu lỗi khi focus và khi hover; kích thước ô và vị trí chữ không đổi giữa các trạng thái | [`DESIGN.md`](../Design/DESIGN.md) §2.6 *Ô nhập chữ* — bảng hai trục, dòng *Kích thước*, *`hover`*, *Ô đang lỗi*; §Trạng thái của spec component | 2, 7 |
| A6 | Phần tử liền kề trong form không chồng nhau; khe bằng bậc spec khai | [`AuthCard.md`](../Design/Components/AuthCard.md) §Kích thước — bảng khe dọc trong khung; [`FormRow.md`](../Design/Components/FormRow.md) — khe nhãn → ô | 3 |
| A7 | Ô được trình duyệt tự điền giữ nền và chữ của trạng thái nghỉ | [`DESIGN.md`](../Design/DESIGN.md) §2.6 *Ô nhập chữ*, dòng *Trình duyệt tự điền* | 5 — **chưa có cách tái hiện**: trình duyệt không giao diện không tự điền. Thử ép trạng thái tự điền qua DevTools Protocol là một việc của hạ tầng; chưa được thì phép đo này không bật, ca này kiểm tay |

Một phép đo lớp A thường được mong có nhưng **chưa có nguồn**: *trang không có thanh cuộn ngang ở khung nhìn nào*. `docs/Design/` mới nói về vùng cuộn ngang bên trong `DataTable`, `EditableGrid`, `Chart`, chưa có dòng nào cho cả trang. Phép đo đó chờ `design-expert` thêm dòng spec (việc D1); không thêm thì không bật.

**Điều kiện hợp lệ của lượt chạy** — không phải phép đo giao diện, nên không cần nguồn ở `docs/Design/`; vi phạm là đỏ:

| # | Điều kiện | Vì sao |
| --- | --- | --- |
| H1 | Không `console.error`, không ngoại lệ trang | Một màn văng ngoại lệ có thể vẫn vẽ ra trông bình thường |
| H2 | Không request nào ngoài dữ liệu giả (QC2) | Request rơi vào khoảng trống thì màn đang ở trạng thái không ai định |

**Thứ tự phủ màn:** đợt 1 là các màn đã có lỗi lọt — đăng nhập (hai chế độ), đổi mật khẩu bắt buộc, hai trang lỗi, khung ứng dụng cùng trang chủ. Đợt 2: hồ sơ, người dùng (danh sách, chi tiết), vai trò, ma trận phân quyền, đơn vị.

### QC4 — Ảnh chụp tham khảo · [người dùng chốt; cơ chế: architect]

| Khoản | Chốt |
| --- | --- |
| Có chụp không | Có — mỗi trạng thái × khung nhìn × theme được kiểm ở QC3 chụp một ảnh, đính vào báo cáo của test đó |
| Lưu ở đâu | Thư mục kết quả của Playwright dưới `src/FE/`, bị `.gitignore` của FE loại. CI tải lên artifact ở **mọi** lượt chạy, kể cả xanh, giữ 7 ngày |
| Vai | Để người xem tham khảo. **Không** so với ảnh nào, **không** chặn, **không** commit, **không** đưa vào `docs/Design/`, **không** là tài liệu. Ảnh và spec Design khác nhau thì spec đúng |
| Không có | `toHaveScreenshot`, thư mục ảnh chuẩn, ngưỡng lệch điểm ảnh, image Docker để sinh ảnh, Git LFS, bước duyệt ảnh chuẩn |

Câu hỏi *ai duyệt khi ảnh chuẩn đổi* của bản đề xuất **không còn**: không có ảnh chuẩn nào để đổi.

**Hai máy, kết quả có thể lệch ở mức điểm ảnh.** Không có so ảnh nên chuyện render khác nhau giữa Windows và Linux không làm đỏ gì. Phép đo bố cục (A3, A6) so số đo với giá trị token, dung sai nửa điểm ảnh CSS để chịu phần làm tròn — dung sai này là của phép đo, không phải nới luật.

### QC5 — axe-core · [architect]

| Khoản | Chốt |
| --- | --- |
| Mức | Thẻ `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `wcag22aa` — mục tiêu AA ở [`DESIGN.md`](../Design/DESIGN.md) §2 (dòng 70). Không chạy thẻ AAA: Core cố ý không nhắm AAA. Không chạy `best-practice`: không phải tiêu chí đã chốt; tiêu đề cấp 1 do A1 kiểm |
| Quét ở đâu | Cột *axe* ở bảng trạng thái của QC3, sau khi trang ổn định |
| Ngoại lệ có tên | Một tệp duy nhất `src/FE/e2e/ha-tang/ngoai-le-axe.ts`. Mỗi mục: mã quy tắc axe, bộ chọn phần tử, màn, lý do, ngày, căn cứ — dòng spec Design hay issue của thư viện. Áp bằng cách lọc kết quả theo cặp *quy tắc × phần tử*; không tắt quy tắc toàn cục, không loại trừ cả vùng |
| Ngoại lệ mồ côi | Một mục không khớp vi phạm nào trong lượt chạy là **đỏ** — lỗi đã sửa thì ngoại lệ phải gỡ, không nằm lại che lỗi sau |
| Chữ vô hiệu ở theme tối | [`DESIGN.md`](../Design/DESIGN.md) §2.2 (dòng 110) cố ý để dưới 4.5:1. WCAG miễn trừ control vô hiệu và axe bỏ qua phần tử mang thuộc tính vô hiệu, nên không cần mục ngoại lệ. axe vẫn báo thì phần tử đó không vô hiệu thật — đó là lỗi |

**Giới hạn:** axe chỉ kiểm phần quyết được từ DOM và màu đã render. Thứ tự focus, nhãn có nghĩa, phóng to 400%, bẫy bàn phím vẫn là việc đọc và kiểm tay. Lớp lint của F17 giữ nguyên.

### QC6 — CI · [architect]

| Khoản | Chốt |
| --- | --- |
| Hình dạng | Các bước cuối của job `frontend-gate`, sau bước 4b — đúng thứ tự từ rẻ tới đắt ở [`05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) §2. Cùng runner, cùng Node từ `.nvmrc`, dùng lại `npm ci` của job. Không dựng job riêng: lý do duy nhất để tách — môi trường render riêng cho ảnh chuẩn — đã mất |
| Các bước | 5a `e2e:kieu` → 5b cài Chromium của Playwright kèm thư viện hệ thống, cache thư mục trình duyệt theo bản Playwright → 5c `e2e` → 5d tải ảnh tham khảo và báo cáo HTML lên artifact, chạy cả khi bước trước đỏ |
| Thời gian | **Chưa đo.** Ước lượng job dài thêm 4–6 phút: cài trình duyệt và thư viện khoảng 1 phút, build `e2e` 1–2 phút, chạy 2–3 phút. Đo ở lần chạy đầu, ghi cạnh cổng ở `05-gate.md` |
| Chặn | Đỏ là không hợp nhất ([`repo-artifact.md`](../quy-uoc/repo-artifact.md) §14). Không chạy lại khi đỏ: một ca lúc xanh lúc đỏ là nguồn bất định phải tìm và sửa |
| Lỗi đã biết lúc bật cổng | Lỗi đã ghi ở bảng *Đã có → còn thiếu* của spec Design mà chưa sửa thì phép đo của nó đánh dấu `test.fail()` kèm tên dòng đó. Sửa xong mà quên gỡ dấu thì test xanh ngoài dự kiến và Playwright báo đỏ — dấu tự hết hạn, không nằm lại che lỗi |
| Điều kiện chạy | Mọi PR có `src/FE` — như các bước trước của job |

### QC7 — Đổi giao diện cố ý · [architect]

1. Spec ở `docs/Design/` đổi trước, hoặc trong cùng PR — `design-expert`.
2. `frontend-expert` sửa code. Phép đo nào neo vào dòng spec vừa đổi thì sửa cùng PR — phần lớn không phải sửa, vì giá trị token đọc lúc chạy.
3. `core-reviewer` — vì lưới nằm trong `core-paths` — soát phép đo bị nới hay gỡ, ngoại lệ axe mới, ca bị bỏ qua, dấu `test.fail()` không trỏ dòng *Đã có → còn thiếu* nào.

Ảnh tham khảo trong artifact giúp người duyệt PR nhìn màn mà không phải chạy ứng dụng; không ai phải duyệt chúng.

### QC8 — Luật mới đề xuất · [architect]

Chưa thêm vào [`RULES-FE.md`](../RULES-FE.md) — một lượt khác đang sửa tệp đó. F55 đã dùng cho luật `ViewEncapsulation.None`; F56, F57 còn trống ngày 2026-10-06 theo lệnh tra ở [`RULES.md`](../RULES.md) (`git log -S` và `grep`). Lượt thêm tra lại; mã đã bị lấy thì dùng mã kế tiếp — mã trong bảng dưới chỉ là mã dự kiến, mã thật là mã ở `RULES-FE.md`.

| Mã dự kiến | Luật | Mức | Ép bằng gì | Trạng thái |
| --- | --- | --- | --- | --- |
| F56 | Mỗi màn có tuyến trong `*.routes.ts` có tệp spec lưới giao diện dưới `src/FE/e2e/man/`, kiểm các trạng thái ở mục *Trạng thái* của spec màn theo QC3, quét axe theo QC5 của ADR-0117 | MUST | Bước 5c của job `frontend-gate` — lớp A, axe, H1, H2; bước 5a cho kiểu dữ liệu giả. Vế *mọi màn có spec, mọi trạng thái của spec Design được kiểm*: chưa có cổng — `core-reviewer` và `design-expert` canh. Nền tự điền: chưa có cổng — kiểm tay | 📐 — các bước chưa có trong workflow |
| F57 | Không nới lưới cho xanh: mọi phép đo lớp A mang neo tới một dòng spec `docs/Design/`; ngoại lệ axe chỉ ở tệp ngoại lệ, mỗi mục kèm lý do; `test.fail()` chỉ cho lỗi đã ghi ở bảng *Đã có → còn thiếu* và nêu tên dòng; không `test.only`, `test.skip`, `test.fixme` trong `src/FE/e2e/` | MUST NOT | Ngoại lệ mồ côi: hạ tầng lưới, đỏ lúc chạy. `test.only`: `forbidOnly` của Playwright ở CI. `test.fail()` đã sửa xong: Playwright báo đỏ khi test xanh ngoài dự kiến. Các vế còn lại: chưa có cổng — `core-reviewer` canh vì lưới nằm trong `core-paths`; phép dò bằng `fe-gate.sh` chỉ dựng khi có vi phạm thật lặp lại ([ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)) | 📐 ba vế máy ép · 🕳️ các vế còn lại |

**F17 đổi ô *Ép bằng gì*:** phần cần render chuyển từ *không ép được bằng máy* sang *axe ở bước 5c của `frontend-gate`* cho phần axe kiểm được; phần còn lại vẫn kiểm tay.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ kiểm tay: lượt audit trên trình duyệt khi có nghi ngờ

**Được:** không công cụ mới; chính cách này đã tìm ra tám lỗi. **Mất:** không lặp lại tự động — lỗi đã sửa quay lại thì không ai thấy cho tới lượt audit sau; phụ thuộc người nhớ chạy, đúng hình dạng sự cố [`../audit/2026-08-23-cong-khong-ton-tai.md`](../audit/2026-08-23-cong-khong-ton-tai.md). **Vì sao loại:** người dùng muốn chặn lặp lại, và kiểm tay không chặn được gì.

### Phương án B — Thêm lớp so ảnh: ảnh chuẩn commit vào repo, sinh và so trong image Playwright chính thức

Đây là cơ chế của bản đề xuất. Ảnh chuẩn sinh trong image `mcr.microsoft.com/playwright:v<bản>-noble` (912 MiB nén, đo 2026-10-06) để Windows và Linux render như nhau; không dung sai điểm ảnh; một người duyệt mỗi lần ảnh đổi. Hai biến thể lưu: Git LFS, hoặc lấy artifact của `main` làm ảnh chuẩn.

**Được:** bắt cả lỗi thị giác **chưa ai nghĩ tới** — màu lệch theme, icon lệch dòng, khối đè nhau ở chỗ không phép đo nào nhìn. Ba trong tám lỗi ở Bối cảnh lộ ra bằng mắt trước khi có ai viết phép đo cho chúng. Đợt nâng Angular 21 và PrimeNG sẽ có lưới so ảnh, đúng mục kiểm số 1 ở [`16-nen-tang-va-nang-cap.md`](../wiki-core/fe/16-nen-tang-va-nang-cap.md) §4 (dòng 85).

**Mất:** ảnh chuẩn thành nguồn giao diện thứ hai có quyền phán đúng sai, cạnh `docs/Design/`; repo lớn dần vĩnh viễn, hoặc thêm LFS ở mọi máy và mọi dự án hạ nguồn ([ADR-0016](0016-phan-phoi-core-bang-clone.md)); Docker trên máy dev; mỗi lần đổi giao diện hay nâng Playwright là một vòng sinh và duyệt ảnh; đổi câu chữ cũng đỏ ảnh.

**Vì sao loại:** người dùng chốt ngày 2026-10-06 — ảnh chỉ để tham khảo, `docs/Design/` đã là nguồn. Điều kiện xét lại: *Dấu hiệu* thứ nhất ở dưới.

### Phương án C — Đo computed style trong Karma

**Được:** không gói mới, chạy trong `ng test` đã có. **Mất:** TestBed dựng component rời — không khung, không route, không cascade toàn cục như ứng dụng thật; đúng bài học của [`../audit/2026-10-05-nhanh-path-rong-khong-component-nuot-url-goc.md`](../audit/2026-10-05-nhanh-path-rong-khong-component-nuot-url-goc.md) §5: gọi riêng từng mảnh không kiểm được cách ghép. **Vì sao loại:** lỗi 3, 6 nằm ở chỗ ghép.

### Phương án D — Dịch vụ so ảnh trả phí (Chromatic, Percy, Applitools), thường đi cùng Storybook

**Được:** giao diện duyệt ảnh tốt, ảnh lưu ngoài repo. **Mất:** phụ thuộc dịch vụ ngoài có phí, gửi ảnh giao diện ra bên thứ ba; Storybook là bộ dựng thứ hai phải nâng cùng Angular; component trong Storybook không phải màn thật. **Vì sao loại:** cùng lý do với B, cộng thêm hạ tầng ngoài.

### Phương án E — Job CI riêng cho lưới, song song với `frontend-gate`

**Được:** thời gian chờ của PR ngắn hơn vì hai job chạy cùng lúc; cổng nào đỏ thấy ngay từ tên job. **Mất:** thêm một lần checkout và `npm ci` mỗi PR; một khối YAML nữa phải giữ khớp Node và cache với `frontend-gate`. **Vì sao loại:** lý do tách job ở bản đề xuất là môi trường render riêng cho ảnh chuẩn, và lý do đó không còn. Tên bước đủ để biết bước nào đỏ. Xét lại khi số đo cho thấy job vượt ngưỡng ở *Dấu hiệu*.

### Phương án F — Cả ba engine: Chromium, Firefox, WebKit

**Được:** phủ đúng danh sách `.browserslistrc`. **Mất:** thời gian lưới gấp hai tới ba; WebKit trên Linux không phải Safari trên macOS hay iOS, nên lớp phủ Safari chỉ là một phần. **Vì sao loại có điều kiện:** cả tám lỗi đều thấy trên Chrome; chưa lỗi nào chỉ có ở Firefox hay Safari. Thêm engine khi một lỗi như vậy lọt thật.

### Phương án G — Chạy với BE thật và database có dữ liệu dựng sẵn

**Được:** thử cả cầu nối thật. **Mất:** CI cần Postgres, build BE, seed; chậm và chập chờn — đúng điều kiện khiến E2E bị hoãn. **Vì sao loại:** người dùng đã loại ngày 2026-10-06.

## Hệ quả

### Tích cực

- Sáu trong tám lỗi ở Bối cảnh có phép đo bật được ngay khi spec nguồn có sẵn (A1–A6). Lỗi 5 có nguồn nhưng chưa có cách tái hiện — ghi là hở, không bị che.
- Mỗi phép đo trỏ về một dòng spec: đỏ là chỉ được dòng Design nào bị vi phạm, và `docs/Design/` vẫn là nguồn duy nhất.
- Phép đo đọc token lúc chạy, nên đổi giá trị token trong Design không làm lưới đỏ oan — đúng chỗ *"đỏ mỗi lần đổi thiết kế"* mà [`06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §5 lo.
- axe đo tương phản trên trang đã render ở cả hai theme — số tính tay ở `DESIGN.md` lần đầu được đối chiếu với thứ trình duyệt vẽ ra.
- Không tệp nhị phân trong repo, không Docker, không LFS, không bước duyệt ảnh. Cùng một lệnh trên Windows và trên CI.
- Rút lui sạch: không phần nào để lại dấu trong lịch sử git.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Lỗi thị giác chưa ai nghĩ tới đi qua** | Lớp A chỉ bắt lớp lỗi đã có phép đo. Màu lệch theme, icon lệch dòng, khối đè nhau ở chỗ không phép đo nào nhìn — lọt như hôm nay, tới khi có người mở màn. Ba trong tám lỗi ở Bối cảnh thuộc loại này trước khi có ai viết phép đo. Đây là cái giá trực tiếp của việc bỏ phương án B |
| **Đợt nâng thư viện UI vẫn so bằng mắt** | Mục kiểm số 1 ở `16-nen-tang-va-nang-cap.md` §4 không được tự động hoá. Ảnh tham khảo của hai lượt chạy trước và sau đợt nâng có sẵn trong artifact, nhưng không gì bắt ai so chúng — và artifact hết hạn sau 7 ngày |
| **Mỗi lớp lỗi mới cần một dòng spec rồi một phép đo** | Một lỗi lọt dạy được một phép đo mới, nhưng phải qua `design-expert` thêm dòng spec trước. Phép đo thiếu nguồn không bật — trang cuộn ngang hôm nay chưa được canh |
| **CI dài thêm** | Ước lượng 4–6 phút trên `frontend-gate`, chưa đo; job chạy nối tiếp nên PR chờ đủ thời gian đó |
| **Chỉ Chromium** | Lỗi riêng của Firefox hay Safari — trong danh sách hỗ trợ của `.browserslistrc` — không bị bắt |
| **Nền tự điền vẫn hở** | Lỗi 5 chỉ kiểm tay cho tới khi tìm được cách ép trạng thái tự điền |
| **API giả không phải BE** | Dữ liệu giả đúng theo DTO của FE; hành vi BE thật không được thử. Lưới xanh không nói gì về cầu nối thật |
| **Dev server không phải máy chủ thật** | Header bảo mật của reverse proxy — CSP — không có mặt; lỗi do CSP chặn style hay script không bị bắt |
| **Phép đo lỗi thời khi spec đổi mà neo không đổi theo** | Neo trong code là chuỗi trỏ về tệp và mục; spec dời mục thì neo gãy im lặng — không cổng nào kiểm. `core-reviewer` canh |

## Sáu câu hỏi bắt buộc

| # | Câu hỏi | Trả lời | Chắc chắn? |
| --- | --- | --- | --- |
| 1 | Vấn đề thật | Tám lỗi giao diện quan sát được trên trình duyệt ngày 05–06/10, đi qua cả bốn cổng FE; neo ở bảng Bối cảnh | Có — đo trên trình duyệt; riêng lỗi 5 và 7 là người dùng báo, chưa đo |
| 2 | Phương án đơn giản hơn | Kiểm tay (A) — không chặn lặp lại. Phương án đã chọn đã là bản bỏ so ảnh của hướng người dùng chọn; bỏ thêm axe thì mất lớp duy nhất đo tương phản trên trang thật | Có |
| 3 | Chi phí vận hành thêm | `frontend-gate` dài thêm 4–6 phút mỗi PR (ước lượng); dữ liệu giả và phép đo phải sửa cùng PR đổi DTO hay màn; ngoại lệ axe phải gỡ khi lỗi được sửa; ba gói dev phải nâng theo lịch | Thời gian CI chưa đo |
| 4 | Ai bảo trì | Cấu hình, hạ tầng, phép đo lớp A, các bước CI: `test-engineer`. Dữ liệu giả và spec của một màn: `frontend-expert`, cùng PR đổi màn. Dòng spec làm nguồn cho phép đo: `design-expert`. Phép đo bị nới, ngoại lệ, ca bị bỏ: `core-reviewer` | Có |
| 5 | Rút lui thế nào | Gỡ bốn bước 5a–5d khỏi `frontend-gate`; xoá `src/FE/e2e/`, `src/FE/playwright.config.ts`, cấu hình `e2e` trong `angular.json`, `environment.e2e.ts`, ba gói dev và hai script, sinh lại lockfile; F56, F57 thành mã ngừng dùng, không cấp lại; ô *Ép bằng gì* của F17 trả về cũ; bỏ hai dòng khỏi `core-paths`; ADR mới lật ADR này. Không dữ liệu chạy nào phải chuyển, không tệp nào nằm lại trong lịch sử git. Ước lượng một lượt agent | Có |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Lưới kiểm màn của Core bằng dữ liệu giả mang khái niệm của Core — người dùng, vai trò, đơn vị — và đọc card hợp đồng của Core. Không biết tên module nào | Có |

## Dấu hiệu quyết định này bắt đầu sai

- **Một lỗi thị giác lọt ra người dùng mà không phép đo nào diễn tả được nếu không có ảnh** — lần thứ hai cùng loại thì xét lại phương án B bằng ADR mới, kèm câu hỏi lưu ảnh chuẩn ở đâu cho người dùng.
- Đợt nâng Angular 21 hay PrimeNG làm vỡ giao diện mà không ai thấy trước khi hợp nhất.
- Phép đo bị gỡ hay nới *"cho xanh"* thay vì sửa code hay sửa spec.
- Tệp ngoại lệ axe dài dần với mục không có căn cứ; dấu `test.fail()` sống qua nhiều PR.
- Trong 20 lần chạy liên tiếp trên `main`, có từ 2 lần đỏ mà code không đổi: bất định chưa trị.
- `frontend-gate` vượt 15 phút: xét phương án E.
- Một lỗi lọt riêng ở Firefox hay Safari: xét phương án F.
- Dự án hạ nguồn đầu tiên thêm màn của module: quyết vùng sở hữu của spec lưới theo [ADR-0100](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) và [`repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.5.

## Câu hỏi đã chốt

| Câu | Trả lời |
| --- | --- |
| Q1 — Ảnh chuẩn lưu ở đâu | Người dùng, 2026-10-06: *"chỉ tham khảo chứ không dùng làm tài liệu, vì đã có sẵn docs/Design rồi"*. Không có ảnh chuẩn; ảnh là artifact tham khảo — QC4 |
| Q2 — Ai duyệt khi ảnh chuẩn đổi | Không còn — không có ảnh chuẩn |

## Việc sau khi chấp nhận — mỗi việc 30–40 phút

Thứ tự: hạ tầng và phép đo trước; lỗi đã biết đánh dấu `test.fail()` để bật cổng mà không đỏ `main`; sửa lỗi gỡ dần các dấu đó.

| # | Agent | Việc | Tệp | Sau việc |
| --- | --- | --- | --- | --- |
| A1 | `architect` | Dòng trạng thái ADR-0028 thêm `· Bổ sung bởi ADR-0117 (2026-10-06)` | [ADR-0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) | — |
| A2 | `architect` | Thêm F56, F57 — tra lại mã; ô *Ép bằng gì* của F17. Tài liệu cổng: hàng cổng ở §3.2, lệnh bước 5 ở §5, ràng buộc ở §6, mục 3 của §10 | [`../RULES-FE.md`](../RULES-FE.md) · [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) | — |
| A3 | `architect` | Chiến lược test: §1 hạng của bố cục; §5 phân biệt test pixel với phép đo theo token; §6 phân biệt lưới với E2E luồng thật; §10 hàng hồi quy hình ảnh trỏ ADR này và điều kiện xét lại. Khối `core-paths`: thêm `src/FE/e2e/`, `src/FE/playwright.config.ts` — tệp đó nằm trong `core-paths`, cần `core-reviewer` | [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) · [`../kien-truc-core-module.md`](../kien-truc-core-module.md) | — |
| D1 | `design-expert` | Đối chiếu bảng nguồn của lớp A ở QC3 với spec thật: xác nhận mỗi dòng nguồn còn đúng mục; đề xuất dòng spec còn thiếu — trang không cuộn ngang; hover trên ô lỗi ở §Trạng thái của `AuthField`, `Input` nếu chưa nói rõ. Dòng mới là luật giao diện mới — người dùng duyệt | `docs/Design/` | — |
| F1 | `frontend-expert` | Cấu hình `e2e` cho build và serve trong `angular.json`; `environment.e2e.ts`; `.gitignore` của FE thêm thư mục kết quả và báo cáo của Playwright. Kiểm `ng build --configuration e2e` xanh và bước 4a của `frontend-gate` vẫn đỏ đúng | `src/FE/angular.json` · `src/FE/src/environments/` · `src/FE/.gitignore` | — |
| T1 | `test-engineer` | Cài ba gói, ghim chính xác; `playwright.config.ts`; `e2e/tsconfig.json`; hai script npm; một ca khói mở `/dang-nhap` chạy được trên Windows | `src/FE/package.json` · `src/FE/playwright.config.ts` · `src/FE/e2e/` | F1 |
| T2 | `test-engineer` | Hạ tầng chặn API đóng mặc định, header CORS và preflight, hai hàm dựng envelope, đối chiếu mã lỗi với `docs/contracts/`. Canary: request không dữ liệu giả → đỏ; POST JSON khác nguồn kèm header XSRF → nhận dữ liệu giả; mã lỗi bịa → đỏ; trích 0 mã → đỏ | `src/FE/e2e/ha-tang/` | T1 |
| F2 | `frontend-expert` | Dữ liệu giả khởi động và xác thực: token XSRF, `me` 401 và 200, đăng nhập thành công và các mã lỗi ở mục *Trạng thái* của spec màn đăng nhập, đổi mật khẩu bắt buộc, menu | `src/FE/e2e/du-lieu-gia/` | T2 |
| T3 | `test-engineer` | Phép đo A1–A6 kèm neo nguồn, H1, H2, axe kèm tệp ngoại lệ — ngoại lệ mồ côi là đỏ; ảnh tham khảo đính vào báo cáo. Chạy trên đăng nhập, đổi mật khẩu, hai trang lỗi, khung cùng trang chủ. Trên code hôm nay, mỗi lỗi chưa sửa phải **đỏ** ở đúng phép đo của nó — ghi lại làm bằng chứng từng đỏ ([`06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) §8) — rồi đánh dấu `test.fail()` kèm tên dòng *Đã có → còn thiếu*. Thử ép trạng thái tự điền qua DevTools Protocol; ghi kết quả | `src/FE/e2e/ha-tang/` · `src/FE/e2e/man/` | F2, D1 |
| T4 | `test-engineer` | Bước 5a–5d trong `frontend-gate`; cache trình duyệt; artifact ở mọi lượt chạy, giữ 7 ngày. Đo thời gian lần chạy đầu | `.github/workflows/docs-gate.yml` | T3 |
| — | `frontend-expert` | Sửa các lỗi đã ghi ở bảng *Đã có → còn thiếu* của `AuthField`, `AuthCard`, `Button`, `Input`, `Toolbar` — mỗi component một lượt. Xong khi gỡ dấu `test.fail()` tương ứng và test xanh | Theo từng spec component | T3 |
| T5 | `test-engineer` | Ca cho khung cùng trang chủ và hai trang lỗi ở cả ba khung nhìn, hai theme | `src/FE/e2e/man/` | T4 |
| F3–F7 | `frontend-expert` | Đợt 2, mỗi màn một lượt — hồ sơ, người dùng, vai trò, ma trận phân quyền, đơn vị: dữ liệu giả theo card, ca theo mục *Trạng thái* của spec màn | `src/FE/e2e/` | T5 |
| A4 | `architect` | Sau 20 lần chạy trên `main`: ghi thời gian thật và số lần đỏ không do đổi code vào nhật ký; xét các mục ở *Dấu hiệu* | [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md) | T4 |

Phiên chính gọi `core-reviewer` phạm vi FE sau A3, sau T4 và sau mỗi đợt màn — lưới nằm trong `core-paths` từ A3. Không có việc BE.

## Liên quan

- [ADR-0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) — toolchain FE; ADR này bổ sung
- [ADR-0015](0015-fe-va-api-khac-nguon.md) — FE và API khác nguồn, lý do API giả cũng khác nguồn
- [ADR-0059](0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md) — cổng FE và canary vào `core-paths`
- [ADR-0061](0061-primeicons-la-phu-thuoc-tuong-minh-ghim-chinh-xac-nap-mot-lan.md) — khai tường minh phụ thuộc bắc cầu
- [ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md) — điều kiện dựng cổng mới
- [ADR-0116](0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md) — tệp bản cài, lý do e2e có tệp môi trường riêng
- [`../wiki-core/fe/06-testing-strategy.md`](../wiki-core/fe/06-testing-strategy.md) · [`../wiki-core/fe/trien-khai/05-gate.md`](../wiki-core/fe/trien-khai/05-gate.md) · [`../Design/DESIGN.md`](../Design/DESIGN.md) · [`../contracts/README.md`](../contracts/README.md)
