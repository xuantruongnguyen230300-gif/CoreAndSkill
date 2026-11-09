---
kind: luat
scope: core
verified: chua-doi-chieu
---

# AuthCard

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`, hiện thực **một** trong hai biến thể. Bảng dưới đây khai đúng những mục đã mở source ra so.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, đây là một bề mặt được căn giữa; không có bẫy focus, không có lớp nổi, không có ảo hoá. Toàn bộ độ khó nằm ở bố cục, và bố cục là thứ không đi mượn được.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/components/auth-card/auth-card.component.ts` — selector `app-auth-card`, class gốc `.auth-card`, vùng bọc `.auth-card-vung`. Cột cao tối thiểu `100dvh` là component riêng: `src/FE/src/app/shared/components/auth-page/auth-page.component.ts` — selector `app-auth-page`.

Bằng chứng ngày 2026-10-06, ghi tắt trong ô: **đọc mã** — đọc tệp sau hai lượt sửa của frontend cùng ngày; **đo** — lượt đo trên trình duyệt (Chromium do Playwright điều khiển, `ng serve`, bề rộng 1280/768/390, sáng và tối), chạy **trước** hai lượt sửa đó, nên không làm bằng chứng cho mục vừa sửa; **lưới** — lưới Playwright `src/FE/e2e/`, phép đo lớp A gọi theo mã (A1–A8), phiên chính báo xanh, kết quả không lưu lại trong repo; **ca kiểm** — ca kiểm đơn vị, gọi theo tên.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| API | `title` (`input.required`), `description`, `errorMessage`, `brandName` — khớp §API dự kiến (đọc mã 2026-10-08). Input `loading` đã gỡ, đúng §API (chốt 2026-10-08): thư mục `shared/components/auth-card/` — cả `auth-card.component.ts` lẫn `auth-card.component.spec.ts` — không còn chuỗi `loading`; thẻ `<app-auth-card` ở `dang-nhap.page.html` và `doi-mat-khau-bat-buoc.page.html` không còn `[loading]`, chuỗi `[loading]="dangGui()"` chỉ còn trên `<app-button` | Không có input `variant` lẫn `size` |
| Component dumb | Đúng — không gọi API đăng nhập, không đọc token, không điều hướng, không `inject` gì; tên thương hiệu vào qua input, trang đọc seam (luật F11 ở [`../../RULES-FE.md`](../../RULES-FE.md)) | — |
| Biến thể `form` | Có, là hình dạng duy nhất component dựng | — |
| Biến thể `wide` + cỡ `lg` | — | **Chưa có.** Không có `--layout-auth-w-wide`, không có lưới hai cột. Chú thích trong chính component khai đây là cắt bớt có ghi nhận |
| `LanguageSwitcher` trong khung | — | Chưa có. Chú thích component khai lý do: `CORE_I18N.languages` v1 chỉ có một mục nên `LanguageSwitcher` vốn không render — tức **đúng spec**, không phải thiếu sót. Chưa tự mở `CORE_I18N` ra xác nhận con số đó |
| Bề rộng, đệm, cỡ chữ tiêu đề | Khớp cỡ `md` của §Kích thước từ `$bp-md`: `--layout-auth-w`, `--sp-11`, `--fs-2xl`. Dưới `$bp-md` đệm hạ theo dòng *Responsive* | — |
| `default` | Nền `--color-surface`. Từ `$bp-xs`: viền `--color-border`, `--radius-xl`, `--shadow-2` (đọc mã: `auth-card.component.scss` § `@include s.tu-man-hinh(s.$bp-xs)` trong khối `.auth-card`). Vùng bọc nền `--color-bg`. Cột `100dvh` gồm vùng bọc và `Footer` nằm ở `auth-page.component.scss` § `min-height: 100dvh`; § `.auth-card-vung` không còn tự khai chiều cao. Đo: không thanh cuộn ở 1280 × 800 và 390 × 844, hai chế độ, hai theme; ca kiểm `đủ chỗ (1280×900): cột đúng bằng khung nhìn, Footer sát đáy, không thanh cuộn`; lưới A3. Dưới `$bp-xs` không viền, không bo góc, không bóng — khớp §Responsive (chốt 2026-10-08, đọc mã 2026-10-08) | — |
| Responsive — ba ngưỡng | Đọc mã: mobile-first trong `auth-card.component.scss`. Dưới `$bp-xs`: khung trải hết bề rộng, đệm trong `--sp-6`, không viền, không bo góc, không bóng; vùng bọc § `align-items: flex-start`, đệm 0. Từ `$bp-xs`: vùng bọc căn giữa dọc, đệm `--sp-6`; khung đệm `--sp-8`, có viền, bo góc, bóng. Từ `$bp-md`: khung đệm `--sp-11`. Ca kiểm `≥ $bp-md (1280): đệm --sp-11, có viền và bo góc, căn giữa dọc`, `$bp-xs … $bp-md (700): đệm --sp-8, vẫn có viền và bo góc`, `< $bp-xs (390): trải hết bề rộng, bỏ viền và bo góc, đệm --sp-6, căn trên` — ba ca không hỏi bóng. Khớp §Responsive, kể cả bóng và vùng bọc không đệm dưới `$bp-xs` (chốt 2026-10-08, đọc mã 2026-10-08) | **Chưa đo trên trình duyệt sau lượt sửa** — lượt đo chạy trước, thấy khung ở 390 chưa trải hết bề rộng. Ca kiểm `< $bp-xs` chưa hỏi bóng. `wide` về một cột: không áp, chưa có biến thể `wide` |
| Khe giữa các vùng | `--sp-8`, đúng bậc spec khai — `gap` của `.auth-card`. Lưới A6b (đo từ tiêu đề trở xuống, không đo thương hiệu → tiêu đề) | — |
| Khe trong form — giữa hai trường, trường cuối → nút chính | Có. § `.auth-card > form` xếp dọc với `gap: var(--sp-6)`; § `.auth-card > form > app-button` bù `calc(var(--sp-7) - var(--sp-6))`. Style chạm được nội dung chiếu nhờ `ViewEncapsulation.None`, gói trong khối § `app-auth-card {`. Đo: giữa hai ô `--sp-6`, ô cuối → nút `--sp-7`, ở cả ba bề rộng; ca kiểm trong `auth-card.component.khe-form.spec.ts`, ví dụ `trường trên đang lỗi: đo từ mép dưới DÒNG LỖI tới nhãn trường dưới = 16px`; lưới A6a | — |
| Khe tiêu đề → mô tả | Có — § `.auth-card__mo-ta` kéo `calc(var(--sp-3) - var(--sp-8))`, khe ra `--sp-3`. Ca kiểm `khe tiêu đề → mô tả = --sp-3 (6px), không phải 18px`; lưới A6c (chế độ vận hành, đổi mật khẩu bắt buộc). Lượt đo chạy trước lượt sửa nên còn ghi 18px | Chưa đo trên trình duyệt sau lượt sửa |
| Vùng thương hiệu | Có (đọc mã): § `auth-card__thuong-hieu` — một `<p>` chữ, đứng trước `<h1>`, chỉ vẽ khi `brandName()` có giá trị; khe tới tiêu đề là `gap` `--sp-8`. Hình thức: `--fs-md`, `--fw-bold`, `--color-text-muted`. Ca kiểm `vùng thương hiệu: tên từ CORE_BRANDING đứng TRƯỚC <h1>`. Khớp §Kích thước đoạn *Vùng thương hiệu* và §API `brandName` (chốt 2026-10-08, đọc mã 2026-10-08) | Chưa đo trên trình duyệt |
| Tiêu đề là `<h1>` thật, trong `<main>` | Có; lưới A1 | — |
| `hover` / `active` / `disabled` / `empty` không áp dụng | Đúng — không có quy tắc nào trên `.auth-card` | — |
| Khu lỗi | Có — một [`NoticeBanner.md`](./NoticeBanner.md) vai `danger`: `auth-card.component.html` § `<app-notice-banner class="auth-card__khu-loi" severity="danger"`, tiêu đề lấy khoá § `'chung.thongBao.loi'`; `role="alert"` và ngắt dòng `white-space: pre-line` đến từ chính `NoticeBanner`. Ca kiểm `khu lỗi là NoticeBanner vai danger có tiêu đề tiền tố "Lỗi", đứng TRƯỚC form trong DOM` và `câu nhiều dòng → khu lỗi role=alert giữ ngắt dòng (white-space: pre-line)`; lưới: trạng thái *lỗi đăng nhập* qua lớp A và axe | Chưa đo hình thức trên trình duyệt sau lượt sửa — lượt đo chạy trước, lúc khu lỗi còn tự vẽ |
| `loading` — ba dấu hiệu do trang đặt | Có ở cả hai trang (đọc mã 2026-10-08): `aria-busy` trên `<form>` — `dang-nhap.page.html` và `doi-mat-khau-bat-buoc.page.html` § `[attr.aria-busy]="dangGui() ? 'true' : null"`; khoá ô — `dang-nhap.page.ts` và `doi-mat-khau-bat-buoc.page.ts` § `this.form.disable({ emitEvent: false })` trong `khoaForm()`; nút chính quay — § `[loading]="dangGui()"` trên `<app-button`. Khung không mang `aria-busy`, không đổi kích thước, không lớp phủ. Ca kiểm `aria-busy KHÔNG nằm trên khung — nó thuộc <form> của trang`, `đang gửi → ba ô và nút hiện/ẩn disabled, giữ giá trị; API lỗi → mở lại`; lưới: trạng thái *đang tải (đang gửi)* chờ đúng `main form[aria-busy="true"]` | — |
| Focus sau khi gửi hỏng | **Có ở cả hai trang** (đọc mã 2026-10-08), khớp §Accessibility dòng *Focus sau khi gửi hỏng*. Hai hàm dùng chung ở `src/FE/src/app/shared/forms/tra-focus-sau-khi-mo-form.ts`: § `phanTuDangFocus` ghi phần tử đang giữ focus trong vùng của trang, § `traFocus` trả về đó nếu nó còn trong DOM, không `disabled` và còn hiện, không thì gọi phương án của trang. `dang-nhap.page.ts` và `doi-mat-khau-bat-buoc.page.ts`: `khoaForm()` gọi § `this.phanTuGiuFocus = phanTuDangFocus(this.host.nativeElement)` trước khi khoá; nhánh lỗi gọi `moForm()` rồi § `traFocusSauLoi()`, chạy trong `afterNextRender` (ô đã hết `disabled`). Phương án: ô lỗi đầu tiên, không có thì ô đầu form — `dang-nhap.page.ts` § `this.focusO((c) => c.invalid)`, `doi-mat-khau-bat-buoc.page.ts` § `focusOSaiDauTien(this.host.nativeElement)` rồi § `focusLai()`. Trả focus đi qua `focusLai()`, không gắn cờ ô tự nhận focus. Ca kiểm: `tra-focus-sau-khi-mo-form.spec.ts` (ba nhánh + `null`); `Enter ở ô mật khẩu rồi lỗi máy chủ → focus về đúng ô mật khẩu` và hai ca phương án (`dang-nhap.page.spec.ts`); `lỗi máy chủ → focus về đúng ô đang giữ focus lúc gửi` và hai ca phương án (`doi-mat-khau-bat-buoc.page.spec.ts`); e2e `src/FE/e2e/man/01-dang-nhap.spec.ts` ca ``lỗi máy chủ khi Enter ở ${ten} → focus về đúng ô đó, không ở body`` cho ô mật khẩu và ô tên đăng nhập (người soạn chưa chạy test) | Nhánh "bấm nút chính" chưa có ca kiểm — mọi ca đều gửi bằng `Enter` trong ô hoặc không có phần tử giữ focus |
| Không có tóm tắt lỗi đầu form | Đúng §Accessibility dòng *Tóm tắt lỗi* (chốt 2026-10-08, đọc mã 2026-10-08): khu lỗi chỉ nhận `errorMessage` từ § `[errorMessage]="loiChung()"`; gửi khi còn ô sai chỉ đưa focus về ô sai đầu tiên — `dang-nhap.page.ts` § `this.focusO((c) => c.invalid)`, `doi-mat-khau-bat-buoc.page.ts` § `focusOSaiKhiGuiSai(this.lanGuiSai` | — |
| Focus đầu tiên vào ô trống đầu | Không do `AuthCard` làm — trang gọi `focus()` của [`AuthField.md`](./AuthField.md): `dang-nhap.page.ts` § `afterNextRender(() => this.focusO((c) => c.value === '')`, `doi-mat-khau-bat-buoc.page.ts` § `afterNextRender(() => this.oMatKhauHienTai()?.focus())`. Đo: màn mở thì ô đầu còn trống có focus | — |

Chưa đối chiếu: §Accessibility ngoài `<main>`, `<h1>`, `role="alert"`, vùng thương hiệu, hai dòng focus và tóm tắt lỗi, và lượt axe WCAG A/AA của lưới (còn thứ tự Tab); toàn bộ luật về `fieldErrors` không khớp ô nào — luật đó thuộc trang gọi.

---

## Mục đích

Khung của **màn xác thực** — một bề mặt căn giữa màn hình, chạy hoàn toàn ngoài khung ứng dụng, chứa trọn một bước xác thực.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Đăng nhập | 🛑 Bất kỳ màn nào **sau khi đã đăng nhập** → khung ứng dụng với [`Sidebar.md`](./Sidebar.md) + [`Topbar.md`](./Topbar.md) — **trừ** màn đổi mật khẩu bắt buộc ở ô bên trái |
| Đổi mật khẩu bắt buộc ngay sau lần đăng nhập đầu — luồng [`../../luong/D2-doi-mat-khau-lan-dau.md`](../../luong/D2-doi-mat-khau-lan-dau.md) | 🛑 Một khối nội dung trong trang → [`Card.md`](./Card.md); 🛑 hộp thoại chặn trên một trang đang mở → [`Dialog.md`](./Dialog.md) |
| | 🛑 Trang lỗi 403/404 → khung ứng dụng, để người dùng còn đường quay lại. Hai trang này chỉ hiện khi đã đăng nhập; chưa đăng nhập thì về màn đăng nhập kèm `returnUrl` ([`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §1) |

**Quyết định: đây là khung thứ hai của ứng dụng — màn xác thực chạy KHÔNG có `Sidebar` và KHÔNG có `Topbar`.** Lý do rất cụ thể chứ không phải thẩm mỹ: `Sidebar` hiển thị cây menu, mà cây menu là dữ liệu **phụ thuộc quyền của người đang đăng nhập**. Trước khi đăng nhập thì dữ liệu đó chưa có — dựng khung ứng dụng ở màn đăng nhập nghĩa là hiện một dải điều hướng rỗng, hoặc tệ hơn, hiện một cây menu mặc định mà bấm vào sẽ bị đá ngược về đúng màn đang đứng. `Topbar` cũng vậy: khu người dùng không có người dùng nào để hiện.

**Màn đổi mật khẩu bắt buộc chạy trong khung này dù phiên đã có.** Trong lúc cờ buộc đổi mật khẩu còn bật, mọi endpoint ngoài bốn cửa mở đều bị chặn ([`../../luong/D2-doi-mat-khau-lan-dau.md`](../../luong/D2-doi-mat-khau-lan-dau.md) §3), nên menu vẫn không dựng được — cùng một lý do với màn đăng nhập. Dựng khung ứng dụng ở đây là quay lại đúng dải điều hướng rỗng vừa tránh.

Cái giá phải nói thẳng: có **hai** bố cục gốc phải bảo trì, và mọi thứ dùng chung giữa hai bên (theme, ngôn ngữ) phải sống ở tầng dưới cả hai. Đổi lại, không có một dòng `if` nào trong `Sidebar` hay `Topbar` để xử lý ca "chưa đăng nhập" — và những dòng `if` đó chính là chỗ lỗi phân quyền hay trốn vào.

**Khung xác thực không có nút đổi theme.** Lựa chọn đã lưu vẫn áp trước khi trang vẽ; người chưa từng chọn nhận theme sáng — đúng mặc định và cái giá đã chấp nhận ở [`../DESIGN.md`](../DESIGN.md) §8. Đổi theme làm ở [`Topbar.md`](./Topbar.md) sau khi đăng nhập; chỉ ngôn ngữ mới cần đổi được trước đó.

## Biến thể

| Biến thể | Chứa gì | Dùng khi |
| --- | --- | --- |
| `form` | Vùng thương hiệu + tiêu đề + mô tả + form + khu lỗi + hành động | **Mặc định.** Đăng nhập, đổi mật khẩu bắt buộc |
| `wide` | Như `form` nhưng bề rộng nới ra cho lưới hai cột | Bước xác thực có nhiều trường, ví dụ đăng ký có hồ sơ |

🛑 **Không có biến thể chỉ hiện thông báo.** Không luồng xác thực nào của v1 kết thúc bằng một màn không còn gì để nhập — khôi phục mật khẩu tự phục vụ nằm ngoài v1 ([`../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)). Luồng sau cần hình dạng đó thì thêm biến thể vào chính file này, theo luật mở rộng thay vì đẻ mới ở [`../COMPONENTS.md`](../COMPONENTS.md) §1.

## Kích thước

| Cỡ | Bề rộng | Đệm trong | Cỡ chữ tiêu đề | Dùng khi |
| --- | --- | --- | --- | --- |
| `md` | `--layout-auth-w`, một cột | `--sp-11` | `--fs-2xl` | **Mặc định.** Biến thể `form` |
| `lg` | `--layout-auth-w-wide`, đủ hai cột | `--sp-11` | `--fs-2xl` | Biến thể `wide` |

Bề rộng là **cố định**, không co theo nội dung: một khung xác thực đổi bề rộng giữa hai bước xác thực liền nhau sẽ nhảy mỗi lần chuyển bước, và người dùng đọc đó là "trang vừa tải lại". Hai bậc bề rộng dùng chung thang với [`Dialog.md`](./Dialog.md) — [`../DESIGN.md`](../DESIGN.md) §6.1. Ô nhập bên trong dùng `--size-control-lg` (42px), nút hành động chính cũng vậy và trải hết bề rộng.

Khe dọc trong khung, theo vai bậc ở [`../DESIGN.md`](../DESIGN.md) §4 — trong nhóm bậc nhỏ, giữa nhóm `--sp-7`, giữa vùng `--sp-8`:

| Khe | Bậc |
| --- | --- |
| Giữa các vùng: vùng thương hiệu · khối tiêu đề · khu lỗi · form · liên kết phụ · `LanguageSwitcher` | `--sp-8` |
| Tiêu đề → mô tả | `--sp-3` |
| Giữa hai trường trong form — đo từ mép dưới **phần tử cuối** của trường trên (dòng lỗi hoặc gợi ý nếu đang hiện, ô nhập nếu không) tới nhãn trường dưới | `--sp-6` |
| Trường cuối → nút hành động chính — đo cùng cách | `--sp-7` |

**Khe trong form là của khung, không của từng màn.** Mọi màn dùng `AuthCard` nhận cùng các khe trên mà không phải tự khai; một màn quên khai không được làm khe về 0. Cách áp — lớp dùng chung, khe nội dung có tên — là việc thi công.

**Vì sao các bậc này — nguyên tắc gần–xa.** Thứ thuộc về nhau phải gần nhau hơn thứ khác nhóm, nếu không mắt ghép sai. Dòng lỗi cách ô của nó `--sp-3` ([`AuthField.md`](./AuthField.md) §Trạng thái) nhưng cách nhãn ô kế `--sp-6` — xa hơn gấp đôi, nên lỗi đọc thành của ô phía trên, không phải lời dẫn của ô phía dưới. Nút chính cách trường cuối `--sp-7`, xa hơn khe giữa hai trường một bậc: nó là nhóm hành động chứ không phải trường thứ tư. Nhưng nó không xa tới `--sp-8` của khe giữa vùng — nút vẫn thuộc form.

**Vùng thương hiệu** (chốt 2026-10-08). Tên sản phẩm **dạng chữ**, đứng đầu khung, trước `<h1>`; vào qua input `brandName` (§API dự kiến) — `null` thì vùng không có trong DOM và khe `--sp-8` theo nó cũng không còn. Chữ `--fs-md`, `--fw-bold`, `--color-text-muted`; khe tới tiêu đề là khe giữa vùng `--sp-8`. Vì sao bậc này: nó đứng dưới tiêu đề `--fs-2xl` ba bậc chữ, bằng cỡ chữ thân, và lùi màu, nên không tranh vai `<h1>` — người dùng đọc bước đang ở trước, tên sản phẩm sau. Không ảnh logo trong mặc định của Core ([`../Screens/01-dang-nhap.md`](../Screens/01-dang-nhap.md), bảng *Cố ý không có*).

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`. Từ `$bp-xs`: viền `--color-border`, `--radius-xl`, `--shadow-2`; căn giữa cả dọc lẫn ngang trong phần khung nhìn còn lại sau `Footer`. Dưới `$bp-xs` khung là mặt trang — không viền, không bo góc, không bóng — §Responsive | Có |
| `hover` | **Không áp dụng.** `AuthCard` là bề mặt, không phải control bấm được; hover thuộc về ô nhập, nút và liên kết bên trong | — |
| `focus-visible` | **Không áp dụng cho chính khung** — khung không nhận focus. Nhưng nó **quyết định focus đầu tiên**: khi màn mở, focus đặt vào ô nhập đầu tiên còn trống | — |
| `active` | **Không áp dụng.** Cùng lý do với `hover` | — |
| `disabled` | **Không áp dụng cho chính khung.** Không có ca nào cả màn xác thực bị vô hiệu hoá mà vẫn hiển thị; ca gần nhất là `loading` ở dòng dưới | — |
| `loading` | Nút hành động chính chuyển sang `loading`; mọi ô nhập nhận `disabled`; `aria-busy="true"` trên `<form>`. Khung **không** đổi kích thước, không hiện lớp phủ. Ba dấu hiệu này do **trang gọi** đặt lên nội dung chiếu của nó: khung không có input `loading` và không chạm tới form hay nút (chốt 2026-10-08). Gửi hỏng thì focus trở lại chỗ cũ — §Accessibility, dòng *Focus sau khi gửi hỏng* | Có |
| `error` | Khu lỗi là một [`NoticeBanner.md`](./NoticeBanner.md) vai `danger` đặt **trên** trường đầu tiên, mang `role="alert"`. Lỗi từng trường vẫn hiện dưới trường theo [`FormRow.md`](./FormRow.md) — hai chỗ này không thay nhau. Khoá `fieldErrors` **không khớp ô nào** cũng vào khu lỗi này, không thành `Toast`. Khu lỗi **giữ ngắt dòng** của chuỗi (`white-space: pre-line`) — mỗi mã một dòng ([`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.2) | Có |
| `empty` | **Không áp dụng.** `AuthCard` luôn có tiêu đề và ít nhất một hành động; một khung xác thực rỗng là lỗi định tuyến, không phải một trạng thái để thiết kế | — |

**Vì sao lỗi xác thực hiện ở khu lỗi chung chứ không dưới ô mật khẩu:** máy chủ trả về "tên đăng nhập hoặc mật khẩu không đúng" — cố ý không nói sai cái nào, vì nói ra là để lộ tài khoản nào tồn tại. Gắn thông báo đó dưới ô mật khẩu là ngầm khẳng định tên đăng nhập đã đúng, tức là phá đúng thứ mà câu chữ đang cố giữ.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-bg`, `--color-surface`, `--color-border`, `--color-text`, `--color-text-muted`, `--color-brand`, `--color-danger`, `--color-danger-bg`, `--color-danger-border`, `--color-focus` |
| Chữ, khoảng cách | `--fs-2xl`, `--fs-md`, `--fs-sm`, `--fw-bold`, `--fw-regular`, `--lh-tight`, `--lh-normal`, `--ls-tight`, `--sp-3`, `--sp-5`, `--sp-6`, `--sp-7`, `--sp-8`, `--sp-9`, `--sp-11` |
| Hình dạng, kích thước | `--radius-xl`, `--border-w`, `--size-control-lg`, `--layout-auth-w`, `--layout-auth-w-wide` |
| Bóng, chuyển động | `--shadow-2`, `--dur-base`, `--ease-standard` |

## Responsive

**Vùng xác thực là một cột cao tối thiểu `100dvh`, gồm hai phần theo thứ tự:** `main` chứa khung, rồi [`Footer.md`](./Footer.md) biến thể `auth`. `main` chiếm phần chiều cao còn lại và căn khung trong phần đó; `Footer` nằm ở đáy cột. Từ `$bp-xs`, đệm của `main` là `--sp-6` ở mọi phía; dưới `$bp-xs` `main` **không có đệm** — khung đã là mặt trang (hàng `< $bp-xs` dưới đây).

- **Đủ chỗ** — khung nhìn cao hơn nội dung: `Footer` nằm sát đáy khung nhìn, không có thanh cuộn.
- **Thiếu chỗ** — khung nhìn thấp hơn nội dung, ví dụ điện thoại nằm ngang: cột cao theo nội dung, `Footer` xếp ngay dưới khung, cách khung đúng đệm dưới của `main`, và trang cuộn.

**Ngưỡng cuộn không phải một con số px cố định.** Trang cuộn khi và chỉ khi chiều cao khung nhìn nhỏ hơn tổng: đệm trên của `main` + chiều cao khung + đệm dưới của `main` + chiều cao `Footer` — đệm là `--sp-6` từ `$bp-xs`, bằng 0 dưới đó. Chiều cao khung đổi theo màn (ba hay bốn trường), theo khu lỗi và dòng lỗi đang hiện, theo độ dài câu chữ của ngôn ngữ. Một ngưỡng px cố định thì hoặc bắt cuộn khi còn đủ chỗ, hoặc để nội dung tràn khi đã hết chỗ.

**Cột này là của khung xác thực, không của từng màn.** Mọi màn dùng `AuthCard` nhận cùng cột mà không tự khai. `Footer` vẫn không lồng vào `AuthCard` ([`../COMPONENTS.md`](../COMPONENTS.md) §2.4): cột bọc cả hai từ bên ngoài. Chỗ đặt cột trong code là việc thi công.

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Khung căn giữa dọc và ngang trong phần còn lại của cột; bề rộng cố định; nền trang `--color-bg` |
| `$bp-xs` … `$bp-md` | Khung giữ bề rộng cố định nhưng nhỏ hơn khoảng cách hai mép; đệm trong hạ xuống `--sp-8`; biến thể `wide` về một cột |
| < `$bp-xs` | Khung trải hết bề rộng, **bỏ viền, bỏ bo góc và bỏ bóng**, đệm trong `--sp-6`; `main` **không còn đệm** — khung đã là mặt trang. Căn trên thay vì căn giữa dọc — khi bàn phím ảo mở, căn giữa dọc đẩy tiêu đề ra khỏi màn hình. `Footer` vẫn ở đáy cột |

**Vì sao bỏ cả bóng dưới `$bp-xs`** (chốt 2026-10-08; trước đó hàng này chỉ bỏ viền và bo góc, và `main` đệm `--sp-6` ở mọi bề rộng). Viền, bo góc và bóng cùng nói một điều: *tấm thẻ nổi trên nền trang*. Ở bề rộng này khung là chính mặt trang, nên cả ba cùng đi. Giữ bóng thì nó chỉ còn lộ ở mép dưới, thành một dải mờ giữa khung và `Footer` không thuộc về hình nào. Đệm của `main` đi theo cùng lý do: còn đệm thì khung không trải hết bề rộng được.

🛑 **Cột dùng chiều cao *tối thiểu*, không dùng chiều cao cố định.** Cột cố định `100dvh` mà căn giữa thì ở khung nhìn thấp, phần đầu khung bị đẩy lên quá mép trên của trang và không cuộn tới được. Với chiều cao tối thiểu, cột tự cao theo nội dung và trang cuộn bình thường.

🛑 **`Footer` không dùng `position: fixed`.** Nó vẫn thuộc luồng cuộn như ở mọi trang khác ([`Footer.md`](./Footer.md) §Khi nào dùng). Dính cứng vào đáy thì ở khung nhìn thấp nó đè lên nút chính.

🛑 **Dùng `100dvh`, không dùng `100vh`.** Trên trình duyệt di động, `100vh` tính theo chiều cao khi thanh địa chỉ **đã ẩn**, nên khi thanh địa chỉ còn hiện thì khung bị đẩy quá đáy và trang sinh ra một đoạn cuộn thừa không chứa nội dung nào. Đây là bẫy quen thuộc và nó chỉ lộ ra trên thiết bị thật.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Vùng chứa là `<main>` — trang xác thực vẫn cần một landmark chính. Khung là một khối bề mặt bên trong. Tiêu đề là `<h1>` duy nhất của trang, nói rõ bước đang ở, ví dụ "Đăng nhập" |
| Form | `<form>` thật với `submit` — `Enter` trong ô nhập phải gửi được form. Đây là thao tác người dùng chờ đợi ở màn đăng nhập |
| Khu lỗi | `role="alert"`, đặt **trước** các trường trong DOM. Nội dung đổi thì trình đọc màn hình đọc lại ngay, không cần chuyển focus |
| Focus ban đầu | Đặt vào ô nhập đầu tiên còn trống. Không đặt vào nút gửi — người dùng sẽ gõ vào hư không. Ô vừa nhận focus này mà người dùng rời đi khi chưa sửa gì thì **không** báo lỗi — [`FormRow.md`](./FormRow.md) §Trạng thái, bảng *Lỗi hiện khi nào* |
| Focus sau khi gửi hỏng | Chốt 2026-10-08. Form mở khoá sau một lần gửi hỏng — khu lỗi hiện, hoặc lỗi về ô — thì focus **trở về đúng phần tử đang giữ focus ngay trước lúc gửi**: ô người dùng vừa bấm `Enter`, hoặc nút chính nếu họ bấm nút. Phần tử đó không còn trong DOM, hoặc không nhận focus được nữa → focus vào ô đầu tiên đang báo lỗi; không ô nào báo lỗi → ô đầu tiên của form. Không bao giờ để focus nằm lại ở `<body>`: khoá form lúc gửi làm phần tử đang focus mất focus, và trả lại là việc của trang ngay sau khi mở khoá. Khu lỗi vẫn được đọc lên nhờ `role="alert"`, không cần kéo focus vào nó |
| Tóm tắt lỗi | **Không có** khối tóm tắt lỗi đầu form, kể cả khi từ hai ô lỗi trở lên — ngoại lệ khai ở [`FormRow.md`](./FormRow.md) §Accessibility, dòng *Submit lỗi*. Gửi khi còn ô sai thì focus về ô sai đầu tiên; mỗi dòng lỗi dưới ô mang `role="alert"` ([`AuthField.md`](./AuthField.md)) |
| Bàn phím | Thứ tự Tab theo đúng thứ tự vùng ở §Kích thước: ô nhập theo thứ tự đọc → nút chính → liên kết phụ, nếu màn có → [`LanguageSwitcher.md`](./LanguageSwitcher.md), đặt **cuối** vì nó là công cụ chứ không phải một bước của luồng |
| Vùng thương hiệu | Một `<p>` chữ đứng trước `<h1>` — **không** phải heading, để `<h1>` vẫn là tiêu đề duy nhất và nói bước đang ở. Tên sản phẩm là dữ liệu từ seam, không đi qua khoá dịch ([`../Screens/01-dang-nhap.md`](../Screens/01-dang-nhap.md) §Câu chữ) |
| Vòng focus | `outline` kèm `outline-offset` theo [`../DESIGN.md`](../DESIGN.md) §2.6 |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8. Đặc biệt quan trọng ở đây vì người dùng có thể chưa đổi được ngôn ngữ trước khi đăng nhập |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'form' \| 'wide'` | `'form'` | |
| `size` | input | `'md' \| 'lg'` | `'md'` | `'lg'` chỉ có nghĩa với `variant = 'wide'` |
| `title` | input | `string` | — | Bắt buộc. Nội dung của `<h1>` |
| `description` | input | `string \| null` | `null` | Một câu dưới tiêu đề |
| `errorMessage` | input | `string \| null` | `null` | `null` thì khu lỗi không tồn tại trong DOM, không chỉ là ẩn |
| `brandName` | input | `string \| null` | `null` | Tên sản phẩm cho vùng thương hiệu — §Kích thước. Trang đọc seam `CORE_BRANDING` rồi truyền xuống. `null` thì vùng không tồn tại trong DOM |
| `showLanguageSwitcher` | input | `boolean` | `true` | |
| `languages` · `currentLanguage` | input | `ReadonlyArray<LanguageOption>` · `string \| null` | `[]` · `null` | Chuyển thẳng xuống `LanguageSwitcher` |
| `pendingLanguage` | input | `string \| null` | `null` | Chuyển thẳng xuống `LanguageSwitcher`, cùng nghĩa như ở đó — cùng khuôn với [`Topbar.md`](./Topbar.md) §API |
| `languageChangeRequested` | output | `string` | — | Chỉ chuyển tiếp yêu cầu lên trên; `AuthCard` không tự đổi ngôn ngữ |

Nội dung form, liên kết phụ và nút hành động vào qua các khe nội dung có tên, không qua input — một màn xác thực có quá nhiều hình dạng để mô tả bằng dữ liệu. `AuthCard` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5): nó không gọi API đăng nhập, không đọc token, không điều hướng; trang xác thực ở tầng nền tảng làm những việc đó rồi truyền `errorMessage` và `brandName` xuống.

**Không có input `loading`** (chốt 2026-10-08; trước đó §API khai `loading` "truyền xuống form và nút chính"). Form và nút là nội dung chiếu của trang, khung không chạm tới được; một input không có tác dụng làm nơi gọi tin rằng truyền vào là đủ. Trang tự đặt ba dấu hiệu của §Trạng thái `loading` lên form và nút của nó.

[`Footer.md`](./Footer.md) biến thể `auth` nằm **dưới** khung chứ không bên trong — `Footer` ở tầng khung, `AuthCard` ở tầng vùng, và [`../COMPONENTS.md`](../COMPONENTS.md) §2.4 cấm lồng ngược chiều.

## Do / Don't

- ✅ Chạy hoàn toàn ngoài khung ứng dụng — không `Sidebar`, không `Topbar`.
- ✅ Dùng `<form>` thật để `Enter` gửi được.
- ✅ Đặt focus ban đầu vào ô nhập đầu tiên.
- ✅ Giữ bề rộng cố định giữa các bước xác thực. Dùng `100dvh` làm chiều cao **tối thiểu** của cột gồm `main` và `Footer`.
- ❌ Không hiện sidebar rỗng hay menu mặc định khi chưa đăng nhập.
- ❌ Không gắn lỗi "sai tài khoản hoặc mật khẩu" vào riêng ô mật khẩu.
- ❌ Không căn giữa dọc ở màn rất nhỏ — bàn phím ảo sẽ đẩy tiêu đề ra ngoài.
- ❌ Không lồng `Footer` vào trong `AuthCard`, và không để `LanguageSwitcher` đứng đầu thứ tự Tab.
- ❌ Không để `Footer` nằm ngoài khung nhìn khi còn đủ chỗ, và không ghim nó bằng `position: fixed`.

## Cần chốt

Không còn.

Ba câu mở 2026-10-06 — bóng và đệm `main` dưới `$bp-xs`, vùng thương hiệu, input `loading` — người dùng chốt 2026-10-08 theo đúng đề xuất. Lời mới nằm ở §Responsive, §Kích thước đoạn *Vùng thương hiệu*, §API. Cùng ngày chốt thêm hai dòng §Accessibility: *Focus sau khi gửi hỏng* và *Tóm tắt lỗi*. Phần code của cả năm mục đã về (đọc mã 2026-10-08); thứ còn lại — đo trên trình duyệt, ca kiểm còn thiếu — ghi ở cột "Còn thiếu" của bảng *Đã có → còn thiếu*.
