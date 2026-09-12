---
kind: luat
scope: core
verified: chua-doi-chieu
---

# NoticeBanner

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — nó nằm **trong luồng bố cục**, không nổi lên trên, nên không có phần khó nào của một lớp nổi: không định vị theo viewport, không hàng đợi, không tự hẹn giờ. Đó cũng chính là điều tách nó khỏi [`Toast.md`](./Toast.md), thứ **có** bọc thư viện vì đúng những lý do đó.

---

## Mục đích

Nói một điều về **bối cảnh hiện tại của trang**, ngay tại chỗ điều đó có nghĩa, và ở lại cho tới khi bối cảnh đổi.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Một điều kiện còn đúng **liên tục**: "Tài khoản chưa xác thực", "Bạn đang xem dữ liệu của kỳ đã khoá" | 🛑 Xác nhận một hành động vừa xong ("Đã lưu") → [`Toast.md`](./Toast.md). Nó nổi lên, tự biến mất, và không chiếm chỗ vĩnh viễn |
| Giải thích vì sao một vùng đang trống hoặc đang bị khoá thao tác | 🛑 Lỗi của **một trường** trong form → [`FormRow.md`](./FormRow.md); lỗi phải nằm ngay dưới trường đó, không nằm ở đầu trang |
| Tổng kết lỗi của cả một form dài sau khi gửi, kèm liên kết nhảy tới từng trường | 🛑 Câu hỏi bắt người dùng quyết định trước khi đi tiếp → [`ConfirmDialog.md`](./ConfirmDialog.md); banner không chặn được gì |
| Hàng lỗi hoặc hàng cảnh báo chiếm hết cột trong [`Table.md`](./Table.md) | 🛑 Một nhãn gắn vào một bản ghi → [`Badge.md`](./Badge.md); banner nói cho cả vùng, không cho một đối tượng |

**Ranh giới với `Toast` là ranh giới quan trọng nhất của component này**, và nó nằm ở hai câu hỏi: *thông tin này còn đúng bao lâu*, và *nếu người dùng bỏ lỡ nó thì có sao không*. `Toast` biến mất sau vài giây, nên nó chỉ được mang thứ mà bỏ lỡ cũng không sao — một lời xác nhận. `NoticeBanner` ở lại, nên nó mang thứ mà bỏ lỡ thì hỏng việc — một điều kiện đang chi phối cái người dùng sắp làm.

Hệ quả thực tế: **một lỗi khiến người dùng không lưu được phải là `NoticeBanner`, không phải `Toast`.** Người dùng đọc chậm, hoặc đang nhìn chỗ khác, sẽ mất luôn thông báo đó và ngồi bấm Lưu lần thứ ba.

## Biến thể

Bốn vai, khớp đúng bốn vai trạng thái ở [`../DESIGN.md`](../DESIGN.md) §2.5 và khớp với [`Badge.md`](./Badge.md), [`Toast.md`](./Toast.md).

| Vai | Dùng khi | Icon ([`../Icons.md`](../Icons.md) §5) | Nền | Viền + dải | Màu tiêu đề |
| --- | --- | --- | --- | --- | --- |
| `info` | **Mặc định.** Bối cảnh trung lập cần biết: "Đang xem bản nháp" | `pi-info-circle` | `--color-info-bg` | `--color-info-border` | `--color-info` |
| `success` | Một quy trình dài đã hoàn tất và kết quả còn đang được nhìn | `pi-check-circle` | `--color-success-bg` | `--color-success-border` | `--color-success` |
| `warning` | Còn dùng được nhưng có điều kiện: "Kỳ này sẽ khoá sau 3 ngày" | `pi-exclamation-triangle` | `--color-warning-bg` | `--color-warning-border` | `--color-warning` |
| `danger` | Đang hỏng hoặc đang bị chặn: "Không lưu được", "Phiên đăng nhập đã hết hạn" | `pi-times-circle` | `--color-danger-bg` | `--color-danger-border` | `--color-danger` |

Dải cạnh trái dày `--border-w-strong` màu viền của vai — đúng vai của token đó theo [`../DESIGN.md`](../DESIGN.md) §5.3. Bo góc `--radius-md`.

**Phần thân dùng `--color-text`, chỉ tiêu đề dùng màu vai.** [`../DESIGN.md`](../DESIGN.md) §2.5 đã tính sẵn: `--color-text` trên các nền `*-bg` đạt thấp nhất 14.10:1 ở theme sáng và 11.38:1 ở tối. Tô cả đoạn văn bằng màu vai thì tương phản tụt xuống khoảng 5.4:1 — vẫn qua AA nhưng đọc mệt hơn hẳn ở đoạn dài, và màu vai mất tác dụng nhấn vì không còn gì để nhấn hơn.

🛑 **Màu nền không bao giờ là kênh duy nhất.** [`../DESIGN.md`](../DESIGN.md) §2.7 ghi thẳng dòng cho component này: bắt buộc **icon theo vai** cộng **tiền tố chữ** ("Lỗi:", "Cảnh báo:"). Bốn cái banner chỉ khác nhau sắc nền là bốn cái banner giống hệt nhau với người mù màu.

## Kích thước

Cỡ ở đây là **mật độ đệm**, không phải chiều cao control.

| Cỡ | Đệm | Icon vai | Cỡ chữ tiêu đề | Cỡ chữ thân | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--sp-4` | `--icon-md` | `--fs-sm` | `--fs-sm` | Trong [`Card.md`](./Card.md), trong hàng `colspan` của [`Table.md`](./Table.md), trong [`Dialog.md`](./Dialog.md) |
| `md` | `--sp-6` | `--icon-lg` | `--fs-md` | `--fs-md` | **Mặc định.** Đầu một trang, dưới [`PageHeader.md`](./PageHeader.md) |

Chỉ hai cỡ. Một banner lớn hơn `md` bắt đầu cạnh tranh chú ý với chính nội dung nó đang chú thích.

Bố cục ngang: icon vai — cụm chữ (tiêu đề, thân, hành động) — nút đóng. Khe giữa icon và chữ `--sp-4`; giữa tiêu đề và thân `--sp-2`; giữa thân và hàng hành động `--sp-5`. Icon căn theo **dòng đầu** của chữ, không căn giữa theo chiều cao khối — banner hai dòng có icon nằm giữa trông như đang trôi.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền, viền và dải theo vai; icon vai tự đặt màu (một trong bốn ca được phá luật kế thừa màu — [`../Icons.md`](../Icons.md) §4); tiêu đề `--fw-semibold`; thân `--color-text`, `--lh-normal` | Có |
| `hover` | **Không áp dụng cho banner.** Nó không tương tác. Hiệu ứng hover thuộc về nút đóng và về các nút bên trong; banner đổi màu khi rê chuột là một lời hứa bấm được mà nó không giữ | — |
| `focus-visible` | Áp cho **các phần tử bên trong**: nút đóng và các nút hành động. `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px`. Vòng focus nằm trên nền `*-bg` chứ không trên `--color-surface` — đây là chỗ cần kiểm lại tỉ lệ, xem `Cần chốt` #1 | Có |
| `active` | **Không áp dụng cho banner.** Cùng lý do với `hover` | — |
| `disabled` | **Không áp dụng.** Một thông báo không "bị vô hiệu hoá" — không còn đúng nữa thì gỡ nó đi, đừng làm nó mờ. Một banner mờ vẫn chiếm chỗ mà không còn nói gì | — |
| `loading` | **Không áp dụng cho banner.** Nếu một nút hành động trong banner đang chờ máy chủ thì chính nút đó mang `loading` và `aria-busy` theo [`Button.md`](./Button.md); banner giữ nguyên | — |
| `error` | **Không áp dụng như một trạng thái hình thức.** Lỗi ở đây là **nội dung**: vai `danger` chính là cách component nói "đang hỏng". Một banner báo lỗi mà lại có thêm trạng thái lỗi là một vòng lặp không có nghĩa | — |
| `empty` | Không có nội dung → **không vẽ gì cả**, kể cả một khung rỗng. Banner phải được gỡ khỏi DOM, không chỉ ẩn bằng CSS — xem giải thích dưới | Có |

**Vì sao `empty` phải gỡ khỏi DOM chứ không chỉ ẩn:** một banner ẩn bằng `visibility: hidden` hay `opacity: 0` vẫn chiếm chỗ trong bố cục, tạo ra một khoảng trống không giải thích được ở đầu trang. Tệ hơn, tuỳ cách ẩn, nó có thể vẫn nằm trong cây accessibility và vẫn được đọc lên. Với một vùng mang `role="alert"` thì đó là đọc một thông báo rỗng.

**Vì sao `NoticeBanner` không tự biến mất, và cái giá của điều đó:** nó ở lại vì điều kiện nó mô tả vẫn còn đúng. Cái giá thật là **banner cũ tích lại**: một trang gặp ba điều kiện sẽ có ba banner xếp chồng, đẩy nội dung thật xuống dưới nếp gấp. Vì vậy nơi gọi phải chủ động gỡ banner khi điều kiện hết đúng — không có cơ chế nào trong component tự làm việc đó, và đó là điểm yếu đã biết chứ không phải thiếu sót.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu — vai | `--color-info`, `--color-info-bg`, `--color-info-border`, `--color-success`, `--color-success-bg`, `--color-success-border`, `--color-warning`, `--color-warning-bg`, `--color-warning-border`, `--color-danger`, `--color-danger-bg`, `--color-danger-border` |
| Màu — chung | `--color-text`, `--color-text-muted`, `--color-focus` |
| Chữ | `--fs-sm`, `--fs-md`, `--fw-semibold`, `--fw-regular`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-2`, `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-md`, `--border-w`, `--border-w-strong` |
| Kích thước | `--icon-md`, `--icon-lg` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

Banner **không animate lúc xuất hiện**. [`../DESIGN.md`](../DESIGN.md) §7 nói rõ: thông báo lỗi phải thấy ngay, và một hiệu ứng mờ dần là chừng ấy thời gian người dùng không biết mình vừa sai. `--dur-fast` ở đây chỉ dùng cho hover của các nút bên trong.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Icon, chữ và nút đóng trên một hàng; hàng hành động nằm dưới cụm chữ, căn trái |
| < `--bp-md` | Cỡ hạ xuống `sm`; nút hành động xuống dòng, xếp dọc; nút đóng giữ nguyên góc trên bên phải |
| < `--bp-xs` | Nút hành động chuyển sang `block` theo [`Button.md`](./Button.md); tiêu đề và thân giữ nguyên, **không** cắt bằng dấu ba chấm |

Banner luôn chiếm hết bề rộng vùng chứa, ở mọi ngưỡng. Một thông báo hẹp hơn nội dung nó chú thích trông như một chú thích lề, và người dùng bỏ qua nó.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<div>` bao ngoài. Tiêu đề dùng thẻ heading thật khi banner có tiêu đề riêng và nằm ở đầu một vùng |
| Vai trò ARIA | `role="alert"` cho vai `danger`; `role="status"` cho `info`, `success` và `warning`. Xem giải thích dưới |
| `aria-live` | `role="alert"` đã hàm ý `aria-live="assertive"`, `role="status"` hàm ý `polite` — 🛑 **không khai thêm `aria-live` chồng lên**, một số trình đọc màn hình sẽ đọc hai lần |
| Banner có sẵn lúc tải trang | `role` vẫn giữ, nhưng đừng trông chờ nó được đọc lên: vùng live chỉ thông báo những gì **thêm vào sau** khi nó đã tồn tại. Banner quan trọng có sẵn từ đầu phải nằm sớm trong thứ tự đọc, ngay dưới tiêu đề trang |
| Icon vai | `aria-hidden="true"`. Nó là kênh **nhìn**; kênh **nghe** là tiền tố chữ trong tiêu đề — [`../Icons.md`](../Icons.md) §7 |
| Tiền tố chữ | Tiêu đề mở đầu bằng vai ("Lỗi:", "Cảnh báo:"). Đây là thứ duy nhất nói cho người dùng trình đọc màn hình biết banner này thuộc vai nào |
| Nút đóng | [`IconButton.md`](./IconButton.md) với `pi-times`, `aria-label` nói rõ đóng cái gì ("Đóng thông báo lỗi lưu"), không chỉ "Đóng" |
| Focus sau khi đóng | Focus phải trả về một chỗ hợp lý — phần tử trước banner trong thứ tự đọc. Để focus rơi về `<body>` là ném người dùng bàn phím về đầu trang |
| Thứ tự đọc | Banner đặt **ngay trước** vùng nội dung nó nói về. Một banner ở cuối DOM nhưng được CSS kéo lên đầu là hai thứ tự khác nhau cho hai nhóm người dùng |
| Liên kết nhảy trường | Banner tổng kết lỗi form chứa liên kết tới từng trường; bấm vào phải **đặt focus** vào trường đó, không chỉ cuộn tới |
| Chữ | Tiêu đề, thân và nhãn nút đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Vì sao `warning` là `status` chứ không `alert`:** `role="alert"` cắt ngang thứ trình đọc màn hình đang đọc dở. Đó là hành vi đúng khi người dùng cần dừng lại ngay, và là hành vi thô lỗ khi họ chỉ cần biết một điều kiện. Một cảnh báo "kỳ này sẽ khoá sau 3 ngày" không đáng để cắt lời giữa câu. Cái giá: một cảnh báo thật sự gấp sẽ bị đọc muộn hơn — nếu gặp ca đó, câu trả lời là dùng vai `danger`, không phải nâng cấp `role` của `warning`.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `severity` | input | `'info' \| 'success' \| 'warning' \| 'danger'` | `'info'` | Quyết định cả màu, cả icon, cả `role`. Ba thứ đó **không** tách thành ba input — tách ra là mở đường cho một banner đỏ mang `role="status"` |
| `heading` | input | `string \| null` | `null` | Không có tiêu đề thì thân gánh cả tiền tố vai |
| `headingLevel` | input | `2 \| 3 \| 4 \| null` | `null` | `null` = tiêu đề là `<strong>`, không phải heading. Chỉ dùng heading khi banner mở đầu một vùng |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `dismissible` | input | `boolean` | `false` | Mặc định **không** đóng được — xem ghi chú dưới |
| `icon` | input | `string \| null` | `null` | Ghi đè icon mặc định của vai. Ca hiếm; dùng sai là phá luật một-nghĩa-một-icon ở [`../Icons.md`](../Icons.md) §2 |
| `dismissed` | output | `void` | — | Component **không tự gỡ mình** khỏi DOM; nó báo ra và nơi gọi quyết định |

Nội dung thân và hàng hành động vào qua slot, không qua input chuỗi — thân hay chứa liên kết và chữ đậm, còn hàng hành động chứa [`Button.md`](./Button.md).

**Vì sao `dismissible` mặc định `false`:** phần lớn banner mô tả một điều kiện người dùng không tự gỡ được. Một nút đóng trên "Phiên đăng nhập đã hết hạn" chỉ cho phép giấu vấn đề đi, rồi người dùng gặp lại nó ở lần bấm kế tiếp mà không còn lời giải thích. Đóng được chỉ hợp lý khi thông báo là **thông tin thuần**, ví dụ một lời giới thiệu tính năng mới.

**Vì sao component không tự gỡ mình:** trạng thái "banner này còn hiện không" phải thuộc về trang, nếu không nó biến mất khỏi tầm nhìn của nơi gọi và không ai khôi phục được sau một lần vẽ lại. Đây cũng là điều giữ cho component ở đúng phía **dumb** của ranh giới [`../COMPONENTS.md`](../COMPONENTS.md) §5 — nó nhận `input()`, phát `output()`, không tự quyết định vòng đời của mình.

## Do / Don't

- ✅ Dùng `NoticeBanner` cho điều kiện còn đúng lâu; dùng `Toast` cho lời xác nhận thoáng qua.
- ✅ Luôn có icon vai **và** tiền tố chữ.
- ✅ `role="alert"` chỉ cho `danger`.
- ✅ Đặt banner ngay trước vùng nội dung nó nói về, trong DOM.
- ✅ Trả focus về chỗ hợp lý sau khi đóng.
- ❌ Không dùng banner cho lỗi của một trường đơn lẻ.
- ❌ Không tô cả đoạn văn bằng màu vai.
- ❌ Không khai `aria-live` chồng lên `role`.
- ❌ Không cho banner tự biến mất theo giờ — đó là `Toast`.
- ❌ Không ẩn banner bằng CSS thay vì gỡ khỏi DOM.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | `--color-focus` đặt trên các nền `*-bg` chưa có số đo trong [`../DESIGN.md`](../DESIGN.md) §2 — bảng ở đó tính tương phản của vòng focus trên `--color-surface` và `--color-bg`. Có cần thêm một hàng số đo cho ca này, hay `outline-offset` đã đẩy vòng ra ngoài mép banner nên nó luôn nằm trên `--color-surface`? | Người sở hữu hệ token |
| 2 | Nhiều banner cùng lúc trên một trang thì xếp thế nào, và có trần số lượng không? Ba banner xếp chồng đẩy nội dung xuống dưới nếp gấp; gom chúng lại thì mất mất vị trí "ngay trước vùng liên quan" | Dự án đầu tiên gặp ca nhiều điều kiện cùng lúc |
| 3 | Banner tổng kết lỗi form là biến thể của component này hay một thứ riêng? Nó có danh sách liên kết nhảy trường, một cấu trúc không banner nào khác có — cần quyết trước khi có form dài đầu tiên | Người dựng [`FormRow.md`](./FormRow.md) và màn form đầu tiên |
