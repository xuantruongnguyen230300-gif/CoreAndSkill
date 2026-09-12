---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Button

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — một nút không có hành vi khó nào; bọc thư viện chỉ đổi một lớp trung gian này lấy một lớp trung gian khác, mà lại nhận thêm một tập CSS mặc định phải đè.

---

## Mục đích

Kích hoạt một hành động, có nhãn chữ.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Bất kỳ hành động nào **xảy ra tại chỗ**: lưu, huỷ, mở dialog, gửi form | 🛑 **Điều hướng sang tuyến khác** → dùng `<a routerLink>`. Bàn phím và menu chuột phải của trình duyệt hành xử khác nhau giữa nút và liên kết; giả một cái bằng cái kia là lấy đi thao tác "mở tab mới" của người dùng |
| Hành động cần nhãn chữ để hiểu | 🛑 Hành động đã rõ nghĩa bằng icon và bị lặp ở mỗi hàng bảng → [`IconButton.md`](./IconButton.md) |
| Hành động chính hoặc phụ của một vùng | 🛑 Chuyển giữa hai chế độ xem → [`SegmentedControl.md`](./SegmentedControl.md) |
| Nút submit của form | 🛑 Bật/tắt một tuỳ chọn → [`Check.md`](./Check.md) |

**Ngoại lệ được phép, một cái:** một thẻ `<a>` được tạo hình như nút khi nó thật sự là điều hướng nhưng lại là hành động chính của màn. Khi đó nó **vẫn là `<a>`** — chỉ mượn lớp hình thức, không mượn thẻ.

## Biến thể

| Biến thể | Vai | Nền | Chữ | Viền |
| --- | --- | --- | --- | --- |
| `primary` | **Một cái mỗi vùng.** Hành động chính | `--color-brand` | `--color-text-on-brand` | trong suốt |
| `secondary` | Hành động phụ có nhãn: Huỷ, Xoá lọc, Đóng | `--color-surface` | `--color-text` | `--color-border` |
| `ghost` | Hành động thứ ba, nơi một cái viền sẽ làm rối vùng | trong suốt | `--color-text-muted` | không |
| `danger` | **Chỉ hành động phá huỷ** — nút xác nhận cuối cùng của `ConfirmDialog` | `--color-danger` | `--color-text-on-brand` | trong suốt |

Tương phản chữ trên nền đặc (đã tính, [`../DESIGN.md`](../DESIGN.md) §2): `primary` **6.85:1** sáng / **7.19:1** tối; `danger` **6.78:1** / **7.80:1**. Cả bốn đều vượt AA.

🛑 **Không quá một `primary` trong cùng một vùng nhìn.** Hai nút primary cạnh nhau nghĩa là không cái nào là chính, và người dùng phải đọc cả hai để chọn. Đây là lỗi hay gặp nhất ở chân `Dialog`: Lưu là `primary`, Huỷ là `secondary`.

🛑 **`danger` không phải "nút màu đỏ cho vui".** Nó dành cho thao tác không hoàn tác được. Dùng cho một nút "Xoá bộ lọc" sẽ làm màu đỏ mất nghĩa đúng lúc nó cần có nghĩa nhất.

## Kích thước

| Cỡ | Chiều cao | Đệm ngang | Cỡ chữ | Icon | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--sp-4` | `--fs-xs` | `--icon-sm` | Trong hàng bảng, trong chip |
| `md` | `--size-control-md` (34px) | `--sp-5` | `--fs-sm` | `--icon-md` | **Mặc định.** Form, toolbar, dialog |
| `lg` | `--size-control-lg` (42px) | `--sp-6` | `--fs-md` | `--icon-md` | Nút submit chính, màn xác thực |

Thêm hai công tắc hình dạng, dùng được với mọi cỡ:

| Công tắc | Hiệu ứng | Dùng khi |
| --- | --- | --- |
| `block` | `width: 100%`, nội dung căn giữa | Submit trên `AuthCard`; nút trong `Dialog` ở màn nhỏ |
| `icon-leading` / `icon-trailing` | Chèn icon trước/sau nhãn, khe `--sp-3` | Icon trước cho hành động ("Thêm"); icon sau cho tiết lộ ("Mở rộng") |

Nhãn nút **không xuống dòng** (`white-space: nowrap`). Nhãn dài quá thì rút ngắn câu, đừng để nút cao gấp đôi làm vỡ nhịp hàng.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Theo biến thể; `--radius-sm`; `--fw-semibold`; `--lh-snug` | Có |
| `hover` | `primary` → nền `--color-brand-hover`. `secondary` → nền `--color-surface-2`, viền `--color-border-strong`. `ghost` → nền `--color-surface-2`, chữ `--color-text`. `danger` → nền tối/sáng thêm một bậc theo cùng thang. Thêm `--shadow-2`. Chuyển tiếp `--dur-fast` với `--ease-standard`. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px`. Chỉ `:focus-visible`. Khoảng hở 2px là thứ giữ cho vòng focus không tàng hình trên nút `primary` — xem [`../DESIGN.md`](../DESIGN.md) §2.6 | Có |
| `active` | Nền của bậc `active` (`--color-brand-active` cho `primary`); `transform: translateY(1px)`; bỏ bóng. Không dùng `scale` — nó làm chữ nhoè | Có |
| `disabled` | `opacity: 0.55`; `cursor: not-allowed`; bỏ mọi hiệu ứng hover/active. Bắt buộc thuộc tính `disabled` thật, không chỉ đổi màu | Có |
| `loading` | Icon `pi-spinner` quay thay cho icon dẫn; nhãn **giữ nguyên**; nút bị vô hiệu hoá; `aria-busy="true"`. Bề rộng nút **không đổi** — dự trữ chỗ cho spinner bằng `min-width` đo ở trạng thái nghỉ | Có |
| `error` | **Không áp dụng.** Nút không tự mang lỗi. Kết quả hỏng của một hành động hiện ở `Toast` hoặc `NoticeBanner` | — |
| `empty` | **Không áp dụng.** Nút không có nhãn là lỗi thi công, không phải một trạng thái | — |

**Vì sao `loading` giữ nguyên nhãn:** đổi nhãn thành "Đang lưu…" làm nút thay đổi bề rộng, đẩy các nút bên cạnh nhảy chỗ, và người dùng đang di chuột tới nút Huỷ có thể bấm nhầm. Spinner + `aria-busy` đã đủ báo trạng thái.

**Vì sao `loading` phải khoá nút:** không khoá thì hai lần bấm nhanh tạo hai request. Với một lệnh tạo bản ghi, đó là hai bản ghi.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-brand`, `--color-brand-hover`, `--color-brand-active`, `--color-danger`, `--color-text`, `--color-text-muted`, `--color-text-on-brand`, `--color-surface`, `--color-surface-2`, `--color-border`, `--color-border-strong`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-sm`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md` |
| Bóng | `--shadow-2` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Nút giữ bề rộng theo nội dung; nhóm nút xếp ngang, khe `--sp-4` |
| < `--bp-md` | Nhóm nút trong chân `Dialog` xuống dòng; nút `primary` **lên trên** khi xếp dọc |
| < `--bp-xs` | Nút trong `Toolbar` và chân `Dialog` chuyển sang `block` |

**Khi xếp dọc, `primary` lên trên chứ không xuống dưới.** Ở màn hình nhỏ người dùng quét từ trên xuống; hành động chính phải gặp trước. Thứ tự trong DOM giữ nguyên (Huỷ trước, Lưu sau, để thứ tự Tab hợp lý ở desktop) và đảo bằng `order` của flex ở màn nhỏ.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<button>` thật. 🛑 Không `<div role="button">` — mất Enter/Space, mất `disabled`, mất mọi thứ miễn phí |
| `type` | **Luôn đặt tường minh.** Mặc định của `<button>` trong form là `submit`; một nút "Thêm dòng" quên `type="button"` sẽ gửi cả form |
| Bàn phím | `Enter` và `Space` kích hoạt — có sẵn nếu dùng `<button>` |
| Focus | Nhìn thấy được ở `:focus-visible`, tương phản ≥ 3:1 nhờ `outline-offset` |
| Nhãn | Nhãn chữ nhìn thấy được. Có icon thì icon mang `aria-hidden="true"` — [`../Icons.md`](../Icons.md) §7 |
| `disabled` | Thuộc tính `disabled` thật. Nếu cần nút vẫn nhận focus để giải thích vì sao khoá thì dùng `aria-disabled="true"` + chặn xử lý, và **phải** có tooltip hoặc chữ giải thích |
| `loading` | `aria-busy="true"`. Kết quả xong báo qua vùng `aria-live` của `Toast`, không báo bằng chính cái nút |
| Vùng bấm | ≥ 28×28px kể cả cỡ `sm` (WCAG 2.2 SC 2.5.8) |
| Chữ | Qua tầng i18n, không viết thẳng vào template — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'primary' \| 'secondary' \| 'ghost' \| 'danger'` | `'secondary'` | Mặc định là `secondary` **có chủ đích**: quên khai biến thể thì ra nút phụ, không ra nút chính. Sai theo hướng an toàn |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `block` | input | `boolean` | `false` | |
| `icon` | input | `string \| null` | `null` | Tên icon PrimeIcons, không có tiền tố `pi ` |
| `iconPosition` | input | `'leading' \| 'trailing'` | `'leading'` | |
| `type` | input | `'button' \| 'submit' \| 'reset'` | `'button'` | Mặc định `'button'` để bẫy submit ngoài ý muốn không xảy ra |
| `disabled` | input | `boolean` | `false` | |
| `loading` | input | `boolean` | `false` | Khi `true` thì nút tự vô hiệu hoá, không cần đặt `disabled` kèm |
| `ariaLabel` | input | `string \| null` | `null` | Chỉ dùng khi nhãn nhìn thấy không đủ ngữ cảnh |
| `clicked` | output | `void` | — | **Không phát khi đang `disabled` hoặc `loading`.** Chặn ở component, không bắt mỗi nơi gọi tự nhớ |

Nội dung nhãn vào qua slot mặc định (`<ng-content>`), không qua một input `label`. Nhãn hay chứa cả chữ lẫn phần tử con (một `Badge` đếm số), và một input chuỗi sẽ chặn điều đó.

`Button` là component **dumb** — không inject service lấy dữ liệu, không biết route ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Một `primary` mỗi vùng. Còn lại là `secondary` hoặc `ghost`.
- ✅ Luôn khai `type`.
- ✅ Dùng `loading` thay vì tự khoá bằng `disabled` ở nơi gọi — như vậy spinner và trạng thái khoá luôn đi cùng nhau.
- ✅ Nhãn là **động từ và đối tượng**: "Lưu người dùng", không phải "OK".
- ❌ Không dùng `Button` để điều hướng. Dùng `<a>`.
- ❌ Không đặt `outline: none` mà không thay bằng dấu hiệu focus khác.
- ❌ Không đổi nhãn khi `loading`.
- ❌ Không đẻ cỡ thứ tư. Ba cỡ ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.2 là toàn bộ.
- ❌ Không dùng `danger` cho hành động hoàn tác được.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cần biến thể `link` (nút trông như liên kết) không? Hôm nay `ghost` gánh vai đó. Rủi ro: `ghost` và `link` gần giống nhau sẽ tạo ra hai thứ không ai phân biệt được | Dự án đầu tiên gặp nhu cầu thật |
| 2 | Nút có menu thả xuống (split button) là biến thể của `Button` hay một component riêng? Nghiêng về component riêng vì nó có hành vi overlay, thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 | Khi có màn cần nó |
| 3 | `min-width` dự trữ chỗ cho spinner đặt bằng số cụ thể hay đo lúc chạy? Số cụ thể đơn giản hơn nhưng sai với nhãn dài; đo lúc chạy đúng hơn nhưng thêm một lần đọc bố cục | Người dựng component |
