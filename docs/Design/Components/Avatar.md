---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Avatar

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — một hình tròn chứa ảnh, chữ cái đầu hoặc một icon. Hành vi phức tạp duy nhất là bắt sự kiện ảnh tải hỏng để rơi về chữ cái đầu, và đó là vài dòng chứ không phải một thư viện.

---

## Mục đích

Hiển thị danh tính của một người bằng ảnh, và rơi về chữ cái đầu của tên khi không có ảnh.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Khu người dùng ở [`Topbar.md`](./Topbar.md) | 🛑 Trang trí một hàng bảng vốn đã có cột tên đầy đủ — thêm một vòng tròn ở mỗi hàng chỉ tốn bề rộng mà không thêm thông tin nào |
| Cột "Người phụ trách" trong [`Table.md`](./Table.md), khi ảnh giúp nhận ra người nhanh hơn đọc tên | 🛑 Biểu tượng của một **đối tượng** không phải người (dự án, phòng ban) — đó là một icon hoặc một [`Badge.md`](./Badge.md), không phải `Avatar` |
| Danh sách thành viên, nhóm chồng lên nhau để nói "và những người khác" | 🛑 Nút mở menu người dùng — `Avatar` nằm **trong** [`IconButton.md`](./IconButton.md) hoặc trong một `<button>`, chứ bản thân nó không bấm được |
| Đầu một màn hồ sơ | 🛑 Ảnh minh hoạ hoặc logo — chúng là tài sản thương hiệu, xem [`../Icons.md`](../Icons.md) §1 |

## Biến thể

| Biến thể | Vai | Hình dạng | Ghi chú |
| --- | --- | --- | --- |
| `single` | **Mặc định.** Một người | `--radius-full` | Ba lớp rơi lùi, xem dưới |
| `stacked` | Một nhóm người, các vòng chồng mép lên nhau | `--radius-full` | Có vòng viền `--color-surface` để tách khỏi vòng phía sau |

Chỉ hình tròn, không có biến thể vuông. `--radius-full` là hình dạng người dùng đã học được là "một người"; một ô vuông bo góc ở cùng vị trí đọc thành một hình thu nhỏ hoặc một icon. [`../DESIGN.md`](../DESIGN.md) §5.1 dành riêng `--radius-full` cho hình tròn thật, và đây là một trong hai chỗ dùng nó.

### Ba lớp rơi lùi, theo đúng thứ tự

| Thứ tự | Điều kiện | Hiển thị |
| --- | --- | --- |
| 1 | Có `src` **và** ảnh tải được | Ảnh, `object-fit: cover`, cắt giữa |
| 2 | Không có `src`, hoặc ảnh tải hỏng, **và** có `name` | Chữ cái đầu: nền `--color-brand-subtle`, chữ `--color-brand-on-subtle`, `--fw-semibold` |
| 3 | Không có cả hai | Icon `pi-user` trên nền `--color-surface-2`, icon `--color-text-muted` |

**Vì sao chữ cái đầu đứng trước icon chung:** ở một danh sách mười người mà không ai có ảnh, mười cái icon `pi-user` giống hệt nhau — vòng tròn không còn phân biệt được ai với ai và trở thành thứ trang trí thuần tuý. Chữ cái đầu giữ lại đúng cái chức năng đó. Cái giá: hai người trùng chữ cái đầu vẫn nhìn giống nhau, nên `Avatar` **không bao giờ là cách duy nhất** để nhận ra một người — tên phải có mặt ở đâu đó gần bên.

**Vì sao chữ cái đầu dùng `--color-brand-subtle` chứ không dùng một dãy màu sinh theo tên:** một dãy màu ngẫu nhiên theo tên trông sinh động và mang hai vấn đề thật. Nó cần một bảng màu mới, và bảng màu duy nhất có sẵn — bảng biểu đồ ở [`../DESIGN.md`](../DESIGN.md) §2.8 — là **màu dành riêng cho chuỗi dữ liệu**, mượn sang làm màu người là cách làm nó mất nghĩa; và một dãy sinh theo tên sẽ tạo ra những cặp nền–chữ chưa ai tính tương phản. Một nền duy nhất đã tính sẵn (chữ trên nền `brand-subtle` đạt 7.47:1 sáng / 8.35:1 tối) là lựa chọn buồn tẻ mà đúng. Xem `Cần chốt` #1.

Số chữ cái: **một hoặc hai**, lấy theo quy ước tên người dùng của dự án. Ba chữ cái ở cỡ `sm` không còn đọc được.

## Kích thước

| Cỡ | Đường kính | Cỡ chữ cái đầu | Icon lớp 3 | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--fs-2xs` | `--icon-sm` | Trong hàng bảng, trong danh sách dày |
| `md` | `--size-control-md` (34px) | `--fs-xs` | `--icon-md` | **Mặc định.** `Topbar`, danh sách thành viên |
| `lg` | `--size-control-lg` (42px) | `--fs-md` | `--icon-lg` | Đầu màn hồ sơ, [`Card.md`](./Card.md) giới thiệu một người |

`Avatar` **mượn** thang `--size-control-*` làm đường kính. Ba giá trị này đứng đúng chỗ vì `Avatar` gần như luôn đứng cạnh một control cùng hàng, và mượn thang là cách giữ cho nó không cao hơn hay thấp hơn thứ bên cạnh. Cái giá: tên token nói "control" trong khi `Avatar` không phải control — xem `Cần chốt` #2.

Vòng viền `--border-w` màu `--color-border` bao quanh mọi `Avatar`, để một ảnh nền sáng không tan vào nền `--color-surface`. Đặt trên nền `--color-surface-2` thì đổi sang `--color-border-strong` ([`../DESIGN.md`](../DESIGN.md) §2.3).

Biến thể `stacked`: mỗi vòng chồng lên vòng trước khoảng một phần ba đường kính, và mang thêm một vòng `--border-w-strong` màu `--color-surface` phía ngoài để tách khỏi vòng bên dưới. Quá bốn người thì vòng cuối cùng đổi thành một `Avatar` chữ dạng "+N".

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Theo lớp rơi lùi ở mục Biến thể; `--radius-full`; viền `--border-w` màu `--color-border` | Có |
| `hover` | **Không áp dụng cho chính `Avatar`.** Nó không tương tác. Khi nó nằm trong một `<button>` (menu người dùng ở `Topbar`), hiệu ứng hover thuộc về cái nút đó, không thuộc về vòng tròn | — |
| `focus-visible` | **Không áp dụng.** Không nhận focus, và 🛑 không đặt `tabindex` cho nó. Phần tử bao mới là thứ nhận focus | — |
| `active` | **Không áp dụng.** Cùng lý do với `hover` | — |
| `disabled` | **Không áp dụng.** `Avatar` mô tả một người, và một người không "bị vô hiệu hoá". Tài khoản bị khoá thì nói bằng một [`Badge.md`](./Badge.md) đặt cạnh, không bằng cách làm mờ khuôn mặt | — |
| `loading` | Vòng tròn xám `--color-surface-3` đúng đường kính, do [`SkeletonLoader.md`](./SkeletonLoader.md) vẽ. **Không** hiện chữ cái đầu trong lúc chờ rồi đổi sang ảnh — cú nhảy đó khiến người dùng tưởng dữ liệu vừa đổi | Có |
| `error` | **Ảnh tải hỏng.** Bắt sự kiện lỗi của thẻ ảnh và rơi ngay về lớp 2. 🛑 Không để trình duyệt hiện icon ảnh vỡ mặc định — nó vuông, nó lệch màu, và nó nói với người dùng rằng ứng dụng hỏng chứ không phải ảnh thiếu | Có |
| `empty` | **Không có cả ảnh lẫn tên** → lớp 3, icon `pi-user`. Đây là trạng thái hợp lệ, không phải lỗi: bản ghi hệ thống hoặc người dùng đã bị xoá vẫn cần một chỗ đứng trong danh sách | Có |

**Vì sao `error` rơi về chữ cái đầu chứ không hiện một thông báo:** ảnh đại diện không tải được là chuyện xảy ra thường xuyên và không quan trọng — mạng chậm, ảnh bị xoá ở kho lưu trữ. Báo lỗi ở đây tạo ra nhiễu cho một sự cố không ai xử lý được, ngay giữa danh sách. Rơi lùi im lặng là ứng xử đúng. Đây là chỗ hiếm mà "nuốt lỗi" là hành vi mong muốn, và lý do rất hẹp: nội dung thay thế mang **cùng một thông tin** (danh tính người đó), chỉ kém đẹp hơn.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text-muted`, `--color-border`, `--color-border-strong` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-md`, `--fw-semibold`, `--lh-tight` |
| Khoảng cách | `--sp-2`, `--sp-3` |
| Hình dạng | `--radius-full`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md`, `--icon-lg` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Cỡ theo khai báo; `stacked` hiện tới bốn vòng rồi tới "+N" |
| `--bp-md` … `--bp-lg` | `Avatar` trong `Topbar` giữ `md`; tên người dùng bên cạnh vẫn hiện |
| < `--bp-md` | `Topbar` chỉ còn `Avatar`, ẩn tên — lúc này `Avatar` trở thành nội dung mang nghĩa, nên nút bao ngoài **phải** có `aria-label` chứa tên |
| < `--bp-xs` | `stacked` rút xuống hai vòng rồi "+N"; cột `Avatar` trong bảng bị ẩn hẳn, giữ lại cột tên chữ |

**Ẩn tên ở màn nhỏ đổi hẳn vai của `Avatar`.** Khi có tên bên cạnh, `Avatar` là trang trí và ảnh mang `alt=""`. Khi tên bị ẩn, nó là **thông tin duy nhất** và phải có nhãn chữ. Đây là bẫy đã biết: hai ngưỡng cùng một component nhưng hai luật accessibility khác nhau, và luật thứ hai rất dễ bị quên vì nó chỉ sai ở màn nhỏ.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<span>` bao ngoài; `<img>` thật ở lớp 1. 🛑 Không đặt ảnh bằng `background-image` — mất `alt`, mất sự kiện lỗi để rơi lùi |
| `alt` khi có tên bên cạnh | 🛑 **`alt=""` rỗng.** Tên đã hiện dưới dạng chữ; đặt `alt="Ảnh của Nguyễn Văn A"` khiến trình đọc màn hình đọc tên hai lần liên tiếp ở mỗi hàng bảng |
| `alt` khi **không** có tên bên cạnh | `alt` chứa tên người. Đây là ca `Avatar` mang nghĩa, theo phép thử ở [`../Icons.md`](../Icons.md) §7: tắt hình đi mà nội dung mất nghĩa thì hình phải có nhãn |
| Chữ cái đầu | Bọc trong một phần tử `aria-hidden="true"` khi tên đã hiện bên cạnh. "NV" đọc lên thành hai chữ cái rời là tạp âm |
| Icon lớp 3 | `aria-hidden="true"` luôn; nếu `Avatar` đang mang nghĩa thì nhãn nằm ở phần tử bao |
| Vai trò ARIA | Không thêm `role`. Cần bấm được thì bọc trong `<button>` hoặc `<a>` thật và để phần tử đó mang nhãn |
| Focus | `Avatar` không nhận focus. Phần tử bao nhận, với `:focus-visible` và `outline-offset` ≥ 2px |
| `stacked` | Cả nhóm là **một** phần tử có nhãn nói đủ số người ("5 thành viên"), không phải năm phần tử tự đọc lên. Từng vòng bên trong mang `aria-hidden="true"` |
| Vòng viền | Viền không phải trang trí ở đây: nó ngăn ảnh nền sáng tan vào bề mặt. Giữ đủ ngưỡng 3:1 bằng cách đổi bậc viền theo nền |
| Chữ | Nhãn và văn bản thay thế đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `src` | input | `string \| null` | `null` | `null` hoặc tải hỏng đều rơi xuống lớp 2 |
| `name` | input | `string \| null` | `null` | Nguồn của chữ cái đầu, và của `alt` khi `standalone` bật |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `standalone` | input | `boolean` | `false` | `false` = có tên hiện bên cạnh → `alt=""`. `true` = `Avatar` đứng một mình → `alt` chứa tên. Xem ghi chú dưới |
| `onSurface2` | input | `boolean` | `false` | Đổi viền sang `--color-border-strong` khi nằm trên nền `--color-surface-2` |
| `loading` | input | `boolean` | `false` | Vẽ vòng giữ chỗ thay vì nội dung |

Biến thể `stacked` **không** là một input của `Avatar` mà là một component bao riêng nhận một mảng — nếu để `Avatar` tự biết mình đang chồng lên ai thì nó phải biết về anh em của nó, và đó là ghép cứng.

**Vì sao `standalone` mặc định `false`:** quên khai thì ra `alt=""`, tức trình đọc màn hình bỏ qua ảnh. Ở ca sai phổ biến nhất — `Avatar` đứng cạnh một cột tên — đó chính là hành vi đúng. Sai theo hướng im lặng tốt hơn sai theo hướng đọc thừa: một tên bị đọc hai lần ở mỗi hàng của bảng hai mươi dòng là bốn mươi lần lặp.

`Avatar` là component **dumb** — không tự gọi API lấy ảnh, không tự tra tên từ mã người dùng ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Dùng `<img>` thật để bắt được sự kiện tải hỏng.
- ✅ Rơi lùi im lặng: ảnh → chữ cái đầu → icon `pi-user`.
- ✅ Đặt `alt=""` khi tên đã hiện bên cạnh.
- ✅ Bật `standalone` khi ẩn tên ở màn nhỏ.
- ❌ Không dùng `Avatar` làm cách duy nhất để nhận ra một người.
- ❌ Không đặt ảnh bằng `background-image`.
- ❌ Không cho `Avatar` nhận focus.
- ❌ Không làm mờ `Avatar` để nói tài khoản bị khoá — dùng `Badge`.
- ❌ Không để icon ảnh vỡ mặc định của trình duyệt hiện ra.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Chữ cái đầu có cần một dãy màu sinh theo tên không? Hôm nay dùng một nền duy nhất `--color-brand-subtle`. Dãy màu đòi bảng màu mới, phân biệt được với người mù màu và có số đo tương phản — cùng loại việc mà [`../DESIGN.md`](../DESIGN.md) §10 để ngỏ cho biểu đồ | Người sở hữu hệ token, cùng dự án đầu tiên có danh sách người dài |
| 2 | Đường kính đang mượn `--size-control-*`, vốn là chiều cao control. Có khai một bậc `--size-avatar-*` riêng trong [`../DESIGN.md`](../DESIGN.md) §6.2 không, hay chấp nhận mượn để `Avatar` luôn khớp chiều cao control cạnh nó? | Người sở hữu hệ token |
| 3 | `stacked` là component riêng thì tên nó là gì, và nó vào mục lục [`../COMPONENTS.md`](../COMPONENTS.md) §3 ở tầng nào? Nó là tầng 0 hay tầng 1 chưa rõ, vì nó chứa nhiều `Avatar` | Khi có màn danh sách thành viên thật |
