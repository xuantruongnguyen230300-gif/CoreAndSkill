---
kind: luat
scope: core
verified: chua-doi-chieu
---

# AuthCard

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, đây là một bề mặt được căn giữa; không có bẫy focus, không có lớp nổi, không có ảo hoá. Toàn bộ độ khó nằm ở bố cục, và bố cục là thứ không đi mượn được.

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
| `form` | Logo + tiêu đề + mô tả + form + khu lỗi + hành động | **Mặc định.** Đăng nhập, đổi mật khẩu bắt buộc |
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
| Giữa các vùng: logo · khối tiêu đề · khu lỗi · form · liên kết phụ · `LanguageSwitcher` | `--sp-8` |
| Tiêu đề → mô tả | `--sp-3` |
| Giữa hai trường trong form | `--sp-6` |
| Trường cuối → nút hành động chính | `--sp-7` |

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền `--color-border`, `--radius-xl`, `--shadow-2`; căn giữa cả dọc lẫn ngang trong một vùng cao `100dvh` | Có |
| `hover` | **Không áp dụng.** `AuthCard` là bề mặt, không phải control bấm được; hover thuộc về ô nhập, nút và liên kết bên trong | — |
| `focus-visible` | **Không áp dụng cho chính khung** — khung không nhận focus. Nhưng nó **quyết định focus đầu tiên**: khi màn mở, focus đặt vào ô nhập đầu tiên còn trống | — |
| `active` | **Không áp dụng.** Cùng lý do với `hover` | — |
| `disabled` | **Không áp dụng cho chính khung.** Không có ca nào cả màn xác thực bị vô hiệu hoá mà vẫn hiển thị; ca gần nhất là `loading` ở dòng dưới | — |
| `loading` | Nút hành động chính chuyển sang `loading`; mọi ô nhập nhận `disabled`; `aria-busy="true"` trên `<form>`. Khung **không** đổi kích thước, không hiện lớp phủ | Có |
| `error` | Khu lỗi là một [`NoticeBanner.md`](./NoticeBanner.md) vai `danger` đặt **trên** trường đầu tiên, mang `role="alert"`. Lỗi từng trường vẫn hiện dưới trường theo [`FormRow.md`](./FormRow.md) — hai chỗ này không thay nhau. Khoá `fieldErrors` **không khớp ô nào** cũng vào khu lỗi này, không thành `Toast` | Có |
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

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Khung căn giữa dọc và ngang trong `100dvh`; bề rộng cố định; nền trang `--color-bg` |
| `$bp-xs` … `$bp-md` | Khung giữ bề rộng cố định nhưng nhỏ hơn khoảng cách hai mép; đệm trong hạ xuống `--sp-8`; biến thể `wide` về một cột |
| < `$bp-xs` | Khung trải hết bề rộng, **bỏ viền và bỏ bo góc**, đệm `--sp-6`; căn trên thay vì căn giữa dọc — khi bàn phím ảo mở, căn giữa dọc đẩy tiêu đề ra khỏi màn hình |

🛑 **Dùng `100dvh`, không dùng `100vh`.** Trên trình duyệt di động, `100vh` tính theo chiều cao khi thanh địa chỉ **đã ẩn**, nên khi thanh địa chỉ còn hiện thì khung bị đẩy quá đáy và trang sinh ra một đoạn cuộn thừa không chứa nội dung nào. Đây là bẫy quen thuộc và nó chỉ lộ ra trên thiết bị thật.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Vùng chứa là `<main>` — trang xác thực vẫn cần một landmark chính. Khung là một khối bề mặt bên trong. Tiêu đề là `<h1>` duy nhất của trang, nói rõ bước đang ở, ví dụ "Đăng nhập" |
| Form | `<form>` thật với `submit` — `Enter` trong ô nhập phải gửi được form. Đây là thao tác người dùng chờ đợi ở màn đăng nhập |
| Khu lỗi | `role="alert"`, đặt **trước** các trường trong DOM. Nội dung đổi thì trình đọc màn hình đọc lại ngay, không cần chuyển focus |
| Focus ban đầu | Đặt vào ô nhập đầu tiên còn trống. Không đặt vào nút gửi — người dùng sẽ gõ vào hư không |
| Bàn phím | Thứ tự Tab: ô nhập theo thứ tự đọc → liên kết phụ, nếu màn có → nút chính → [`LanguageSwitcher.md`](./LanguageSwitcher.md), đặt **cuối** vì nó là công cụ chứ không phải một bước của luồng |
| Nhãn | Logo mang `alt` là tên hệ thống; nếu tên hệ thống đã hiện bằng chữ ngay cạnh thì ảnh mang `alt=""` để không bị đọc hai lần |
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
| `loading` | input | `boolean` | `false` | Truyền xuống form và nút chính |
| `showLanguageSwitcher` | input | `boolean` | `true` | |
| `languages` · `currentLanguage` | input | `ReadonlyArray<LanguageOption>` · `string \| null` | `[]` · `null` | Chuyển thẳng xuống `LanguageSwitcher` |
| `pendingLanguage` | input | `string \| null` | `null` | Chuyển thẳng xuống `LanguageSwitcher`, cùng nghĩa như ở đó — cùng khuôn với [`Topbar.md`](./Topbar.md) §API |
| `languageChangeRequested` | output | `string` | — | Chỉ chuyển tiếp yêu cầu lên trên; `AuthCard` không tự đổi ngôn ngữ |

Nội dung form, liên kết phụ và nút hành động vào qua các khe nội dung có tên, không qua input — một màn xác thực có quá nhiều hình dạng để mô tả bằng dữ liệu. `AuthCard` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5): nó không gọi API đăng nhập, không đọc token, không điều hướng; trang xác thực ở tầng nền tảng làm những việc đó rồi truyền `loading` và `errorMessage` xuống.

[`Footer.md`](./Footer.md) biến thể `auth` nằm **dưới** khung chứ không bên trong — `Footer` ở tầng khung, `AuthCard` ở tầng vùng, và [`../COMPONENTS.md`](../COMPONENTS.md) §2.4 cấm lồng ngược chiều.

## Do / Don't

- ✅ Chạy hoàn toàn ngoài khung ứng dụng — không `Sidebar`, không `Topbar`.
- ✅ Dùng `<form>` thật để `Enter` gửi được.
- ✅ Đặt focus ban đầu vào ô nhập đầu tiên.
- ✅ Giữ bề rộng cố định giữa các bước xác thực, và dùng `100dvh`.
- ❌ Không hiện sidebar rỗng hay menu mặc định khi chưa đăng nhập.
- ❌ Không gắn lỗi "sai tài khoản hoặc mật khẩu" vào riêng ô mật khẩu.
- ❌ Không căn giữa dọc ở màn rất nhỏ — bàn phím ảo sẽ đẩy tiêu đề ra ngoài.
- ❌ Không lồng `Footer` vào trong `AuthCard`, và không để `LanguageSwitcher` đứng đầu thứ tự Tab.

## Cần chốt

Không còn.
