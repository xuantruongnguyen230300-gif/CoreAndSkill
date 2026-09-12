---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Badge

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — nó là một `<span>` có đệm và có màu, không có hành vi nào cả. Đây là component rẻ nhất trong mục lục để tự dựng và đắt nhất để đi bọc, vì phần lớn công việc là đè lại hệ màu mặc định của thư viện.

---

## Mục đích

Gắn một nhãn chữ ngắn vào một đối tượng, để nói **trạng thái** của nó hoặc **nó thuộc loại gì**.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Trạng thái của một bản ghi trong ô bảng: "Hoạt động", "Đã khoá", "Chờ duyệt" | 🛑 Một câu thông báo cho cả trang → [`NoticeBanner.md`](./NoticeBanner.md); `Badge` gắn vào một đối tượng, không nói cho cả màn |
| Tên vai trò, mã đơn vị, phiên bản — thứ mang **định danh**, không mang màu ngữ nghĩa | 🛑 Bấm vào được để lọc → đó là chip lọc, thuộc [`Toolbar.md`](./Toolbar.md); `Badge` không bao giờ tương tác |
| Đếm số nhỏ đi kèm một nhãn: "Chờ duyệt 12" | 🛑 Thay cho một cột dữ liệu thật. Bốn `Badge` trong một ô bảng là một danh sách bị nén, không phải một nhãn |
| Đánh dấu một mục trong danh sách là mới, là nháp | 🛑 Hành động → [`Button.md`](./Button.md) hoặc [`IconButton.md`](./IconButton.md) |

## Biến thể

Hai **họ**, và ranh giới giữa chúng là ranh giới quan trọng nhất của component này.

### Họ trạng thái — mang màu ngữ nghĩa

| Biến thể | Vai | Chữ | Nền | Viền |
| --- | --- | --- | --- | --- |
| `success` | Kết thúc tốt, đang hoạt động, đã duyệt | `--color-success` | `--color-success-bg` | `--color-success-border` |
| `warning` | Cần chú ý nhưng chưa hỏng: sắp hết hạn, chờ duyệt | `--color-warning` | `--color-warning-bg` | `--color-warning-border` |
| `danger` | Hỏng, bị khoá, quá hạn, đã từ chối | `--color-danger` | `--color-danger-bg` | `--color-danger-border` |
| `info` | Trung lập nhưng đáng chú ý: bản nháp, đang xử lý | `--color-info` | `--color-info-bg` | `--color-info-border` |

Bốn vai này khớp đúng bốn vai trạng thái ở [`../DESIGN.md`](../DESIGN.md) §2.5, và khớp cả với [`NoticeBanner.md`](./NoticeBanner.md). Cùng một nghĩa phải cùng một màu ở mọi chỗ trong ứng dụng — nếu "chờ duyệt" là `warning` ở bảng thì nó cũng là `warning` ở banner.

### Họ định danh — trung tính, có viền

| Biến thể | Vai | Chữ | Nền | Viền |
| --- | --- | --- | --- | --- |
| `neutral` | **Mặc định.** Tên vai trò, mã phòng ban, phiên bản, nhãn phân loại | `--color-text` | `--color-surface-2` | `--color-border` |
| `outline` | Định danh phụ, nơi một mảng nền đặc sẽ nặng: mã tham chiếu, tag | `--color-text-muted` | trong suốt | `--color-border` |

**Vì sao tách hai họ thay vì cho phép "màu tuỳ chọn":** khoảnh khắc `Badge` nhận một màu tự do là khoảnh khắc màu mất nghĩa. Ai đó sẽ chọn `success` cho vai trò "Quản trị" vì màu xanh trông hợp, và từ đó người dùng không còn suy được "xanh = ổn" nữa. Cái giá của việc tách: tên vai trò không được tô màu để phân biệt nhanh, phải đọc chữ. Đó là đánh đổi đã chọn — nghĩa của màu đắt hơn một lần quét mắt.

🛑 **Màu không bao giờ là kênh duy nhất.** [`../DESIGN.md`](../DESIGN.md) §2.7 ghi thẳng dòng cho component này: `Badge` trạng thái phải có **chữ nhãn** ("Hoạt động", "Khoá"), không được rút xuống thành một chấm màu. Một cột bảng toàn chấm màu là một cột không đọc được với người mù màu, và cũng không đọc được với trình đọc màn hình.

## Kích thước

`Badge` không phải control, nên nó **không** dùng thang `--size-control-*`. Chiều cao sinh ra từ cỡ chữ, `--lh-snug` và đệm dọc.

| Cỡ | Cỡ chữ | Đệm dọc | Đệm ngang | Icon | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--fs-2xs` | `--sp-1` | `--sp-3` | `--icon-sm` | Trong ô bảng, cạnh một mục danh sách |
| `md` | `--fs-xs` | `--sp-2` | `--sp-4` | `--icon-sm` | **Mặc định.** Cạnh tiêu đề, trong `Toolbar`, trong đầu [`Card.md`](./Card.md) |

Chỉ hai cỡ. `Badge` bám vào chữ bên cạnh nó, và một cỡ thứ ba to hơn `--fs-xs` sẽ cao hơn dòng chữ nó đi kèm, đẩy chiều cao dòng của cả hàng bảng.

| Công tắc hình dạng | Hiệu ứng | Dùng khi |
| --- | --- | --- |
| `pill` | `--radius-pill` | **Mặc định.** Nhãn trạng thái, nhãn ngắn |
| `square` | `--radius-sm` | Mã, phiên bản, chuỗi kỹ thuật — hình chữ nhật hợp với chuỗi dài hơn |

Nhãn dùng `--fw-bold` và **không xuống dòng**. Nhãn viết hoa toàn phần thì kèm `--ls-wide`; chữ hoa sát nhau ở cỡ 11px rất khó đọc, nhất là với dấu tiếng Việt.

🛑 **`--fs-2xs` (11px) chỉ cho nhãn ngắn, không bao giờ cho một câu** — [`../DESIGN.md`](../DESIGN.md) §3.2. Nhãn `Badge` quá ba từ là dấu hiệu nội dung đó thuộc về một ô dữ liệu, không thuộc về một nhãn.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Theo biến thể; `--fw-bold`; `--lh-snug`; icon dẫn tuỳ chọn, khe `--sp-1` | Có |
| `hover` | **Không áp dụng.** `Badge` không tương tác. Một nhãn đổi màu khi rê chuột là một lời hứa bấm được mà nó không giữ nổi — cần bấm được thì đó là chip lọc, không phải `Badge` | — |
| `focus-visible` | **Không áp dụng.** Không tương tác thì không nhận focus, và không được đặt `tabindex` để "cho tiện" — mỗi `Badge` trong bảng sẽ thành một điểm dừng `Tab` thừa | — |
| `active` | **Không áp dụng.** Cùng lý do với `hover` | — |
| `disabled` | **Không áp dụng.** `Badge` mô tả một sự thật về đối tượng; sự thật đó không "bị khoá". Đối tượng bị khoá thì chính nhãn nói điều đó ("Đã khoá") | — |
| `loading` | **Không áp dụng.** Chưa biết trạng thái thì chưa vẽ `Badge`; chỗ đó do [`SkeletonLoader.md`](./SkeletonLoader.md) giữ, đúng hình dạng viên thuốc sắp có | — |
| `error` | **Không áp dụng như một trạng thái hình thức.** Lỗi ở đây là **nội dung**, không phải trạng thái: biến thể `danger` chính là cách nói "đối tượng này đang hỏng" | — |
| `empty` | Nhãn rỗng hoặc `null` → **không vẽ gì cả**, kể cả một viên rỗng. Một viên trống trong ô bảng trông như dữ liệu bị mất chứ không như "không có nhãn" | Có |

Sáu dòng "không áp dụng" liên tiếp là **có chủ đích và là điểm mạnh của component này**. Một `Badge` không có trạng thái tương tác nghĩa là nó không bao giờ cần JavaScript, không bao giờ vào thứ tự Tab, và không bao giờ là chỗ để một hành vi len vào. Nếu có lúc nào một trong sáu dòng này cần đổi thành "Có", đó là dấu hiệu component đang bị đẩy sang gánh vai của chip lọc — lúc đó tách ra, đừng sửa dòng này.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu — trạng thái | `--color-success`, `--color-success-bg`, `--color-success-border`, `--color-warning`, `--color-warning-bg`, `--color-warning-border`, `--color-danger`, `--color-danger-bg`, `--color-danger-border`, `--color-info`, `--color-info-bg`, `--color-info-border` |
| Màu — định danh | `--color-text`, `--color-text-muted`, `--color-surface-2`, `--color-border`, `--color-border-strong` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fw-bold`, `--lh-snug`, `--ls-wide`, `--ls-normal` |
| Khoảng cách | `--sp-1`, `--sp-2`, `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-pill`, `--radius-sm`, `--border-w` |
| Kích thước | `--icon-sm` |

🛑 **`Badge` đặt trên nền `--color-surface-2` phải đổi viền sang `--color-border-strong`.** [`../DESIGN.md`](../DESIGN.md) §2.3 tính sẵn: `--color-border` trên `--color-surface-2` chỉ đạt 2.80:1 sáng và 2.68:1 tối — **trượt** ngưỡng 3:1 của SC 1.4.11; `--color-border-strong` ở cùng chỗ đạt 4.67:1 và 4.33:1. Đây là ca có thật, không phải giả định: một `Badge` viền nằm trong ô `th` của [`Table.md`](./Table.md), hoặc trong chân [`Card.md`](./Card.md). Component phải nhận biết được nền của mình — xem input `onSurface2` ở mục API.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Cỡ theo khai báo; `Badge` đứng cùng dòng với chữ bên cạnh |
| < `--bp-md` | Trong ô bảng, cỡ hạ xuống `sm` để cột trạng thái không ép các cột khác |
| < `--bp-xs` | Bảng chuyển sang dạng thẻ ([`../DESIGN.md`](../DESIGN.md) §6.3); `Badge` xuống dòng riêng dưới tên bản ghi thay vì chen cùng dòng |

🛑 **Không cắt nhãn `Badge` bằng dấu ba chấm ở màn nhỏ.** "Chờ duy…" và "Chờ duyệt lại" không phân biệt được, và nhãn trạng thái bị cắt là nhãn nói dối. Nhãn không vừa thì rút gọn **câu chữ** ở tầng i18n, không rút gọn bằng CSS.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<span>`. 🛑 Không `<button>`, không `<a>`, không `tabindex` |
| Vai trò ARIA | Không thêm `role`. Nó là chữ thường trong luồng đọc, và trình đọc màn hình đọc nó như mọi chữ khác — đó là hành vi đúng |
| Nhãn | Chữ nhìn thấy được **là** nhãn. Không dùng `aria-label` để nói một điều khác với chữ đang hiện |
| Icon | Icon dẫn luôn `aria-hidden="true"` — nó lặp lại điều nhãn đã nói ([`../Icons.md`](../Icons.md) §7) |
| Ngữ cảnh trong bảng | Trong ô bảng, `Badge` đứng một mình không nói rõ nó mô tả cái gì. Ô `th` của cột ("Trạng thái") là thứ cung cấp ngữ cảnh đó — đây là một lý do nữa để [`Table.md`](./Table.md) bắt buộc `scope="col"` |
| Cập nhật động | Trạng thái đổi tại chỗ mà không tải lại trang thì vùng chứa phải có `aria-live="polite"`. 🛑 Không đặt `aria-live` lên chính `Badge` — hai chục vùng live trong một bảng sẽ đọc chồng lên nhau |
| Màu | Nhãn chữ là kênh bắt buộc; màu là kênh thứ hai, không phải kênh thứ nhất ([`../DESIGN.md`](../DESIGN.md) §2.7) |
| Chữ | Nhãn đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'neutral' \| 'outline' \| 'success' \| 'warning' \| 'danger' \| 'info'` | `'neutral'` | Mặc định là họ định danh **có chủ đích**: quên khai thì ra một nhãn trung tính, không ra một tuyên bố ngữ nghĩa sai |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `shape` | input | `'pill' \| 'square'` | `'pill'` | |
| `icon` | input | `string \| null` | `null` | Tên icon PrimeIcons, không kèm tiền tố `pi `. Luôn là trang trí |
| `uppercase` | input | `boolean` | `false` | Bật thì kèm `--ls-wide`. Không bật cho chữ tiếng Việt có dấu — chữ hoa có dấu ở 11px rất khó đọc |
| `onSurface2` | input | `boolean` | `false` | Báo rằng `Badge` đang nằm trên nền `--color-surface-2`, để đổi viền sang `--color-border-strong`. Xem ghi chú dưới |

Không có output nào, và đó là điểm chính: component này không tương tác. Nhãn vào qua slot mặc định (`<ng-content>`) để chỗ gọi ghép được cả chữ lẫn một con số đã định dạng.

**Vì sao `onSurface2` là một input chứ không tự dò:** dò nền thật lúc chạy đòi đọc lại style tính toán của phần tử cha, một thao tác bắt trình duyệt tính lại bố cục và chạy ở mỗi lần vẽ — trong một bảng hai mươi hàng thì đó là hai mươi lần. Một cờ tường minh rẻ hơn nhiều. Cái giá phải trả rất thật: **người dựng có thể quên**, và quên thì viền trượt xuống 2.97:1 mà không có gì báo. Đây là chỗ cần một mục kiểm khi có màn hình thật.

`Badge` là component **dumb** — không inject service lấy dữ liệu, không tự ánh xạ mã trạng thái sang màu; nơi gọi truyền `variant` vào ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Luôn có nhãn chữ; màu chỉ là kênh thứ hai.
- ✅ Giữ đúng bốn vai trạng thái, khớp với `NoticeBanner` và `Toast`.
- ✅ Dùng họ định danh cho tên vai trò, mã, phiên bản.
- ✅ Bật `onSurface2` khi đặt `Badge` trong `th` hoặc trong chân `Card`.
- ❌ Không rút `Badge` trạng thái xuống thành một chấm màu.
- ❌ Không cho `Badge` bấm được — đó là chip lọc.
- ❌ Không dùng màu trạng thái cho một định danh.
- ❌ Không cắt nhãn bằng dấu ba chấm.
- ❌ Không vẽ một viên rỗng khi không có nhãn.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cần biến thể nền đặc (chữ `--color-text-on-brand` trên `--color-danger`) không? [`../DESIGN.md`](../DESIGN.md) §2.5 đã tính sẵn tương phản cho ca này, nhưng một mảng đỏ đặc lặp ở mỗi hàng bảng nặng hơn nhiều so với nền nhạt | Dự án đầu tiên cần nhấn mạnh mạnh hơn |
| 2 | Cờ `onSurface2` có cách nào ép được bằng máy không? Quên nó là lỗi im lặng, và cổng FE ở [`../../RULES.md`](../../RULES.md) §7 hiện không có luật nào bắt được | Người viết cổng FE, ở giai đoạn có code để kiểm |
| 3 | `Badge` chỉ có số (đếm thông báo trên icon chuông) là biến thể của component này hay một thứ riêng? Nó tròn, không có nhãn chữ, và vì vậy vi phạm luật "màu không phải kênh duy nhất" theo một cách khác — cần quyết trước khi có màn thông báo | Dự án đầu tiên có thông báo |
