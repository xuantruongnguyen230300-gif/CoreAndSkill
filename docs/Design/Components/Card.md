---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Card

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — nó thuần trình bày: một bề mặt có viền, có bóng, có ba chỗ đặt nội dung. Không có hành vi nào thuộc nhóm "khó", nên bọc thư viện chỉ nhận thêm một tập CSS mặc định phải đè.

---

## Mục đích

Gom một khối nội dung thuộc về nhau thành **một bề mặt có ranh giới**, kèm tiêu đề và chân tuỳ chọn.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Gom một nhóm trường trong form dài, để người đọc thấy đâu là một phần | 🛑 Bọc **mọi thứ** trên trang trong một `Card` — khi mọi thứ đều được đóng khung thì không gì nổi lên, và trang chỉ tốn thêm hai lớp đệm |
| Đóng khung một bảng, kèm tiêu đề và tổng số ở chân — khung bảng chính là một `Card` | 🛑 Thông báo cần chú ý ngay → [`NoticeBanner.md`](./NoticeBanner.md); nó có màu vai và có `role`, `Card` thì không |
| Khối nội dung độc lập trong lưới: một hồ sơ, một bản tóm tắt | 🛑 Nội dung nổi lên trên trang, chặn thao tác → [`Dialog.md`](./Dialog.md) |
| Bề mặt của màn đăng nhập | 🛑 Màn đăng nhập → [`AuthCard.md`](./AuthCard.md); nó có đệm, bo góc và cỡ chữ riêng |

🛑 **`Card` không lồng trong `Card`.** [`../COMPONENTS.md`](../COMPONENTS.md) §2.4 xếp nó ở tầng 2, và hai lớp viền lồng nhau tạo ra một khung tranh chứ không tạo ra phân cấp. Cần chia nhỏ bên trong thì dùng một tiêu đề phụ và khoảng cách `--sp-8`, không dùng thêm một cái viền.

## Biến thể

| Biến thể | Vai | Viền | Bóng | Ghi chú |
| --- | --- | --- | --- | --- |
| `default` | **Mặc định.** Khối nội dung tĩnh | `--color-border` | `--shadow-1` | Không tương tác |
| `interactive` | Cả thẻ là một liên kết hoặc một nút — thẻ trong lưới bấm vào để mở chi tiết | `--color-border` | `--shadow-1`, lên `--shadow-2` khi hover | Phải là `<a>` hoặc `<button>` thật, xem mục Accessibility |
| `flat` | Nằm **trong** một vùng đã có ranh giới riêng, nơi thêm một cái bóng nữa sẽ thành nhiễu | `--color-border` | `--shadow-0` | Vẫn giữ viền — xem luật dưới |

🛑 **Mọi biến thể đều có `--color-border`, kể cả `flat`.** [`../DESIGN.md`](../DESIGN.md) §2.1 tính sẵn: `--color-surface` chỉ chênh `--color-bg` 1.08:1 ở theme sáng và 1.09:1 ở theme tối. Đó là chênh lệch của một cái bóng nhẹ, không phải của một đường biên. Và [`../DESIGN.md`](../DESIGN.md) §5.2 nói tiếp: bóng không bao giờ là ranh giới — người dùng chế độ tương phản cao và người dùng màn hình rẻ không thấy bóng nào cả. Bỏ viền để thẻ trông "sạch hơn" là một hồi quy accessibility, không phải một lựa chọn thẩm mỹ.

**Vì sao vẫn giữ `--shadow-1` khi đã có viền:** viền trả lời "ranh giới ở đâu", bóng trả lời "cái nào nằm trên cái nào". Trên một trang có nhiều thẻ cạnh nhau, bóng là thứ giúp mắt tách chúng khỏi nền mà không cần đọc từng đường viền. Đây là hai việc khác nhau, và bỏ một cái đi thì việc kia không gánh thay được.

## Kích thước

`Card` không phải một control, nên nó **không** dùng thang `--size-control-*`. Cỡ ở đây là **mật độ đệm**.

| Cỡ | Đệm thân | Khe giữa các phần | Cỡ chữ tiêu đề | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--sp-5` | `--sp-4` | `--fs-md` | Thẻ nhỏ trong lưới nhiều cột, thẻ tóm tắt |
| `md` | `--sp-6` | `--sp-6` | `--fs-lg` | **Mặc định.** Đệm mặc định của `Card` theo [`../DESIGN.md`](../DESIGN.md) §4 |
| `lg` | `--sp-8` | `--sp-8` | `--fs-lg` | Thẻ chiếm hết bề rộng trang, nội dung dài cần thở |

Bo góc `--radius-lg` cho mọi cỡ. Bề rộng do lưới của trang quyết, `Card` không tự đặt `width`. Ba cỡ là thang đệm riêng của `Card`; câu hỏi mật độ toàn hệ sống ở [`../DESIGN.md`](../DESIGN.md) §10 — chốt ở đó thì `Card` theo.

Ba phần, đều tuỳ chọn trừ thân:

| Phần | Nội dung | Ranh giới với phần kế |
| --- | --- | --- |
| Đầu | Thẻ heading + mô tả phụ + chỗ đặt hành động bên phải | Vạch `--color-border-subtle` — vạch **bên trong** một bề mặt, đúng vai của bậc này ([`../DESIGN.md`](../DESIGN.md) §2.3) |
| Thân | Nội dung chính, vào qua slot | — |
| Chân | Hành động, tổng số, chú thích — nền `--color-surface-2`; `Badge` hoặc `Avatar` đặt ở đây do nơi gọi bật `onSurface2`, `Card` không tự xử | Vạch `--color-border-subtle` phía trên |

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền `--color-border` (`--border-w`), `--radius-lg`, `--shadow-1` theo biến thể | Có |
| `hover` | **Chỉ biến thể `interactive`**: bóng lên `--shadow-2`, viền `--color-border-strong`. Biến thể `default` và `flat` không phản ứng — một thẻ sáng lên khi rê chuột mà bấm không ăn là một lời hứa suông. `--dur-fast` với `--ease-standard`, bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | **Chỉ biến thể `interactive`**: `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px` trên chính phần tử tương tác | Có |
| `active` | **Chỉ biến thể `interactive`**: bóng về `--shadow-1`, `transform: translateY(1px)` | Có |
| `disabled` | **Chỉ biến thể `interactive`**: chữ `--color-text-disabled`, `cursor: not-allowed`, bỏ hover, `aria-disabled="true"`. Biến thể tĩnh không có khái niệm này | Có |
| `loading` | Nội dung thân thay bằng [`SkeletonLoader.md`](./SkeletonLoader.md) giữ đúng hình dạng sắp có; đầu và chân **giữ nguyên**; cả thẻ mang `aria-busy="true"`. Ca tải lại trên nội dung cũ thì phủ `--color-scrim` thay vì thay nội dung | Có |
| `error` | Thân thay bằng một [`NoticeBanner.md`](./NoticeBanner.md) vai `danger` kèm hành động thử lại (icon `pi-refresh`). Viền `Card` **không đổi sang đỏ** — xem giải thích dưới | Có |
| `empty` | Thân thay bằng [`EmptyState.md`](./EmptyState.md) thu nhỏ: icon `pi-inbox`, một câu, một hành động. Đầu và chân giữ nguyên để người dùng còn biết mình đang ở khối nào | Có |

**Vì sao viền `Card` không đổi sang đỏ khi `error`:** viền `Card` là ranh giới của bề mặt, không phải kênh trạng thái. Đổi nó sang `--color-danger-border` làm hai việc hỏng cùng lúc — mất một bậc tương phản đã tính cho vai ranh giới, và tạo ra một cách báo lỗi thứ hai song song với cách đã chốt ở [`../DESIGN.md`](../DESIGN.md) §2.7 (icon + chữ). Lỗi nằm **trong** thân thẻ, nơi có chỗ cho cả icon lẫn câu giải thích lẫn nút thử lại.

**Vì sao `loading` và `empty` giữ nguyên phần đầu:** phần đầu mang tiêu đề — thứ duy nhất nói cho người dùng biết khối này là gì. Thay cả thẻ bằng một khối xám làm người dùng mất chỗ đứng trên trang, và ở màn nhiều thẻ thì không phân biệt được thẻ nào đang tải.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-subtle`, `--color-border-strong`, `--color-focus`, `--color-scrim` |
| Chữ | `--fs-md`, `--fs-lg`, `--fs-sm`, `--fw-semibold`, `--fw-regular`, `--lh-tight`, `--lh-normal` |
| Khoảng cách | `--sp-4`, `--sp-5`, `--sp-6`, `--sp-8` |
| Hình dạng | `--radius-lg`, `--border-w`, `--border-w-strong` |
| Bóng | `--shadow-0`, `--shadow-1`, `--shadow-2` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Đệm theo cỡ đã khai; nhiều `Card` xếp lưới, khe `--sp-8` |
| `$bp-md` … `$bp-lg` | Giữ nguyên đệm; lưới giảm số cột |
| < `$bp-md` | Đệm hạ một bậc (`md` dùng `--sp-5`); lưới về một cột; hành động ở phần đầu xuống dòng dưới tiêu đề |
| < `$bp-xs` | Bo góc giữ nguyên `--radius-lg`; chân thẻ chuyển sang xếp dọc, nút bên trong thành `block` |

**Đệm hạ một bậc ở màn nhỏ chứ không bỏ hẳn.** Bỏ đệm để lấy bề rộng làm chữ chạm sát viền và thẻ trông như một khối bị nén. Một bậc là đủ để lấy lại chỗ mà vẫn còn khoảng thở.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Bao ngoài là `<section>` khi thẻ có tiêu đề, `<div>` khi không. `<section>` không có tiêu đề thì không thêm gì cho trình đọc màn hình mà lại thêm một vùng rỗng vào danh sách vùng |
| Tiêu đề | 🛑 **Thẻ heading thật (`<h2>`…`<h4>`), đúng cấp trong trang.** Không `<div>` to chữ đậm. Người dùng trình đọc màn hình điều hướng trang bằng danh sách heading; một `<div>` biến mất khỏi danh sách đó |
| Cấp heading | Cấp do nơi gọi quyết, vào qua input `headingLevel`. Component **không** đoán cấp — nó không biết mình nằm ở đâu trong trang, và đoán sai sẽ tạo lỗ hổng cấp heading |
| Vai trò ARIA | Không thêm `role`. `<section>` có tiêu đề đã đủ; gắn `aria-labelledby` trỏ vào `id` của heading |
| Biến thể `interactive` | Phải là `<a>` (điều hướng) hoặc `<button>` (hành động tại chỗ) thật. 🛑 Không `<div (click)>` — mất `Enter`/`Space`, mất menu chuột phải, mất "mở tab mới" |
| Bấm lồng nhau | Thẻ `interactive` **không** được chứa nút hay liên kết khác bên trong. Một vùng bấm được lồng trong một vùng bấm được là hành vi không xác định, và bàn phím không thoát ra được |
| Focus | `:focus-visible` trên phần tử tương tác, `outline-offset` ≥ 2px |
| `loading` | `aria-busy="true"` trên phần tử bao. Không dùng `aria-live` ở đây — mỗi thẻ tự đọc lên khi tải xong sẽ thành một tràng thông báo ở màn nhiều thẻ |
| Chữ | Tiêu đề và mô tả đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'default' \| 'interactive' \| 'flat'` | `'default'` | Mặc định là thẻ tĩnh — quên khai thì ra thứ không hứa hẹn gì |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | Mật độ đệm, không phải chiều cao control |
| `heading` | input | `string \| null` | `null` | `null` = không có phần đầu |
| `headingLevel` | input | `2 \| 3 \| 4` | `2` | Cấp thẻ heading sinh ra. Nơi gọi chịu trách nhiệm cho đúng cấp trong trang |
| `description` | input | `string \| null` | `null` | Dòng phụ dưới tiêu đề, `--color-text-muted` |
| `loading` | input | `boolean` | `false` | |
| `disabled` | input | `boolean` | `false` | Chỉ có nghĩa với `interactive` |
| `activated` | output | `void` | — | Chỉ biến thể `interactive`. Không phát khi `disabled` hoặc `loading` |

Nội dung vào qua slot: slot mặc định cho thân, hai slot đặt tên cho hành động ở phần đầu và cho phần chân. Không dùng input chuỗi cho ba chỗ này — cả ba thường chứa component con ([`Badge.md`](./Badge.md), [`Button.md`](./Button.md)), và một input chuỗi sẽ chặn điều đó.

`Card` là component **dumb** — không inject service lấy dữ liệu, không tự biết nội dung của mình đang tải hay lỗi; nơi gọi truyền vào ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Luôn có `--color-border`, kể cả biến thể `flat`.
- ✅ Dùng thẻ heading thật cho tiêu đề, và để nơi gọi chọn cấp.
- ✅ Dùng `--color-border-subtle` cho vạch giữa các phần **bên trong** thẻ.
- ✅ Giữ phần đầu khi thân đang `loading`, `error` hoặc `empty`.
- ❌ Không lồng `Card` trong `Card`.
- ❌ Không dựa vào riêng bóng để làm ranh giới.
- ❌ Không đổi viền thẻ sang màu trạng thái.
- ❌ Không làm cả thẻ bấm được bằng `<div (click)>`.
- ❌ Không đặt nút bên trong một thẻ `interactive`.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Thẻ `interactive` có cần một biến thể "có một hành động phụ ở góc" không? Hôm nay spec cấm hẳn nút lồng trong thẻ bấm được, nhưng lưới thẻ hồ sơ gần như luôn muốn một nút menu ở góc | Sau F3 — dự án hạ nguồn đầu tiên có lưới thẻ |
