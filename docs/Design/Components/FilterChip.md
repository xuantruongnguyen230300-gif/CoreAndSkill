---
kind: luat
scope: core
verified: chua-doi-chieu
---

# FilterChip

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG. Theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 nó là một nút nhỏ có nhãn và một nút gỡ — không có hành vi nào thuộc nhóm "khó".

---

## Mục đích

Hiện một điều kiện lọc đang bật và cho gỡ nó bằng một thao tác.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Mỗi điều kiện đang bật của một danh sách, đặt thành hàng dưới ô tìm trong [`Toolbar.md`](./Toolbar.md) | 🛑 Nhãn **không tương tác** mang trạng thái hoặc định danh → [`Badge.md`](./Badge.md). Đây là ranh giới quan trọng nhất của cả hai component — xem dưới |
| Một lựa chọn đã chọn trong ô nhập nhiều giá trị của [`Autocomplete.md`](./Autocomplete.md) | 🛑 Chuyển giữa vài chế độ xem loại trừ nhau → [`SegmentedControl.md`](./SegmentedControl.md) |
| Nhóm lọc nhanh bật/tắt được đặt sẵn: "Chỉ của tôi", "Quá hạn" | 🛑 Hành động — chip không làm gì cả, nó chỉ mô tả một điều kiện → [`Button.md`](./Button.md) |

### Ranh giới với `Badge` — một câu

> **`Badge` không bao giờ bấm được; `FilterChip` luôn bấm được.**

[`Badge.md`](./Badge.md) ghi thẳng điều này ở mục Biến thể và ở dòng `hover` của bảng trạng thái: *"Một nhãn đổi màu khi rê chuột là một lời hứa bấm được mà nó không giữ nổi — cần bấm được thì đó là chip lọc, không phải `Badge`."* File bạn đang đọc là component mà câu đó trỏ tới.

Hệ quả: một cột bảng đầy nhãn trạng thái là `Badge`. Cũng những chữ đó đặt trên `Toolbar` và bấm vào thì lọc được — đó là `FilterChip`. Cùng nội dung, khác vai, khác component.

## Biến thể

| Biến thể | Vai | Có nút gỡ? | Dùng khi |
| --- | --- | --- | --- |
| `applied` | Một điều kiện **đang bật**, do người dùng vừa đặt | ✅ Có | **Mặc định.** Hàng chip dưới ô tìm |
| `toggle` | Một lựa chọn lọc nhanh, bật hoặc tắt | 🛑 Không — bấm vào chính chip để tắt | Nhóm lọc đặt sẵn, số lượng cố định |
| `readonly` | Điều kiện do hệ thống áp, người dùng **không gỡ được** | 🛑 Không | Bộ lọc theo đơn vị mà tài khoản bị giới hạn — phải thấy được là nó đang bật |

`readonly` tồn tại vì một lý do cụ thể: khi hệ thống tự lọc theo đơn vị của người dùng, danh sách ngắn hơn họ tưởng. Không hiện điều kiện đó ra thì người dùng đi tìm một bản ghi "bị mất". Hiện ra mà cho gỡ thì lại phá luật phân quyền. Nên nó hiện, và nó khoá — kèm [`Tooltip.md`](./Tooltip.md) nói vì sao.

🛑 **Không có biến thể mang màu trạng thái.** Chip luôn dùng hệ màu thương hiệu. Một chip đỏ trông như một điều kiện lỗi, trong khi nó chỉ là một điều kiện lọc như mọi điều kiện khác.

## Kích thước

| Cỡ | Chiều cao | Cỡ chữ | Icon | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` | `--fs-xs` | `--icon-sm` | **Mặc định, và gần như luôn là cỡ này** |

Một cỡ duy nhất, có chủ đích. [`Toolbar.md`](./Toolbar.md) đã chốt: *"Chip điều kiện luôn ở cỡ `sm` kể cả trong `Toolbar` cỡ `md` — chip là nhãn phụ; để nó cao bằng ô tìm sẽ giành mất trọng lượng thị giác của chính ô tìm."* Thêm một cỡ thứ hai là mở đường phá quyết định đó.

Bo góc `--radius-pill`. Đệm ngang `--sp-4`, khe giữa nhãn và nút gỡ `--sp-2`.

**Vùng bấm của nút gỡ phải đủ 28×28px** kể cả khi icon chỉ 12px — nới bằng `padding`, không bằng `margin` ([`../COMPONENTS.md`](../COMPONENTS.md) §2.2). Đây là chỗ dễ sai nhất của component này: nút gỡ trông nhỏ nên người dựng hay để nó nhỏ thật, và trên cảm ứng thì không ai bấm trúng.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-brand-subtle`, chữ `--color-brand-on-subtle`, viền trong suốt, `--radius-pill`. Biến thể `toggle` khi **tắt** thì dùng nền trong suốt, chữ `--color-text-muted`, viền `--color-border` | Có |
| `hover` | Nền đậm hơn một bậc. Riêng nút gỡ có vùng hover của chính nó: nền `--color-surface-2` dạng tròn. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px`. Nhãn chip và nút gỡ là **hai điểm dừng Tab riêng** ở biến thể `applied` | Có |
| `active` | Nền `--color-brand-subtle` đậm thêm một bậc; không dùng `transform` — chip nhỏ, dịch nó xuống trông như rung | Có |
| `disabled` | Chỉ có ở biến thể `readonly`: chữ `--color-text-muted`, không có nút gỡ, con trỏ `default` chứ không phải `not-allowed` — nó không phải một thứ hỏng, nó là một thứ cố định. Kèm icon khoá và `Tooltip` nói lý do | Có |
| `loading` | **Không áp dụng.** Chip không tự tải gì. Việc lọc lại danh sách sau khi gỡ chip là trạng thái `loading` của [`DataTable.md`](./DataTable.md), và ô tìm trong `Toolbar` là nơi hiện vòng quay | — |
| `error` | **Không áp dụng.** Một điều kiện lọc không hợp lệ thì không bao giờ được tạo thành chip. Lỗi thuộc về panel lọc, nơi người dùng nhập điều kiện | — |
| `empty` | Không điều kiện nào bật → **cả hàng chip biến mất hẳn**, không để lại một hàng cao rỗng. `Toolbar.md` đã ghi luật này ở dòng `empty` của nó | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-surface`, `--color-surface-2`, `--color-text-muted`, `--color-border`, `--color-focus` |
| Chữ | `--fs-xs`, `--fw-medium`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-pill`, `--radius-full`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--icon-sm` |
| Icon | `pi-times` để gỡ, `pi-lock` cho `readonly` — [`../Icons.md`](../Icons.md) §5 |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Chip xếp thành hàng, xuống dòng khi hết chỗ, khe `--sp-3` |
| `--bp-sm` … `--bp-md` | Giữ nguyên. Nhãn chip dài cắt bằng dấu ba chấm ở phần **giá trị**, giữ nguyên phần tên điều kiện |
| < `--bp-sm` | Chip **cuộn ngang trong một dải riêng** thay vì xuống nhiều dòng, theo đúng luật đã ghi ở [`Toolbar.md`](./Toolbar.md). Vùng cuộn phải nhận được focus |

**Vì sao cắt phần giá trị chứ không cắt phần tên:** một chip ghi "Trạng thái: Hoạt đ…" vẫn nói được nó đang lọc theo cái gì; một chip ghi "Trạ…: Hoạt động" thì không. Khi phải bỏ bớt, giữ lại cái trả lời câu *"đây là điều kiện gì"*.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Biến thể `toggle` là `<button>` thật với `aria-pressed`. Biến thể `applied` là một khối chứa **một `<button>` gỡ**; bản thân nhãn không bấm được |
| Nhãn nút gỡ | Phải nói **cả tên điều kiện và giá trị**: "Gỡ lọc Trạng thái: Hoạt động", không phải "Xoá". `Toolbar.md` đã ghi yêu cầu này |
| Bàn phím | `Tab` tới nút gỡ, `Enter` hoặc `Space` để gỡ. Với `toggle`, cả chip là một điểm dừng |
| Sau khi gỡ | Focus chuyển sang **chip kế tiếp**; gỡ chip cuối thì focus về nút mở bộ lọc. Để focus rơi về `<body>` là lỗi hay gặp nhất khi gỡ một phần tử đang được focus |
| Thông báo | Số bản ghi sau khi lọc lại báo qua vùng `aria-live="polite"` **đặt cạnh bảng**, không đặt trong chip |
| Vùng bấm | Nút gỡ ≥ 28×28px, nới bằng `padding` (WCAG 2.2 SC 2.5.8) |
| `readonly` | `aria-disabled="true"` chứ không phải `disabled` — nó vẫn phải nhận focus để người dùng đọc được lý do |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `key` | input | `string` | — | **Bắt buộc.** Khoá điều kiện, dùng khi phát sự kiện gỡ |
| `label` | input | `string` | — | **Bắt buộc.** Tên điều kiện: "Trạng thái" |
| `value` | input | `string \| null` | `null` | Giá trị: "Hoạt động". `null` với biến thể `toggle`, nơi nhãn đã là toàn bộ ý nghĩa |
| `variant` | input | `'applied' \| 'toggle' \| 'readonly'` | `'applied'` | |
| `pressed` | input | `boolean` | `false` | Chỉ có nghĩa với `toggle` |
| `lockReason` | input | `string \| null` | `null` | Bắt buộc khác `null` khi `variant` là `'readonly'`; nội dung đi vào `Tooltip` |
| `removed` | output | `string` | — | Phát `key`. **Không phát ở biến thể `readonly`** — chặn ở component, không bắt mỗi nơi gọi tự nhớ |
| `toggled` | output | `boolean` | — | Chỉ phát ở biến thể `toggle` |

`FilterChip` là component **dumb** — nó không biết điều kiện này lọc ra bao nhiêu bản ghi, và không tự gọi lại danh sách ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Một chip cho một điều kiện. Ba giá trị của cùng một trường thì gộp một chip ghi "Vai trò: 3 đã chọn", bấm vào mở lại panel lọc.
- ✅ Nhãn nút gỡ luôn nói đủ tên điều kiện và giá trị.
- ✅ Chuyển focus sang chip kế tiếp sau khi gỡ.
- ✅ Hiện điều kiện do hệ thống áp bằng `readonly` thay vì giấu đi.
- ❌ Không cho `Badge` bấm được để thay chip, và không dùng chip làm nhãn tĩnh.
- ❌ Không tô màu trạng thái cho chip.
- ❌ Không để hàng chip rỗng chiếm chỗ khi không có điều kiện nào.
- ❌ Không đẻ cỡ thứ hai.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Nhiều giá trị của cùng một trường gộp thành một chip hay tách nhiều chip? Gộp thì hàng chip gọn nhưng phải mở panel mới biết gồm những gì; tách thì thấy ngay nhưng năm giá trị là năm chip | Dự án đầu tiên có bộ lọc nhiều trường |
| 2 | Có cần nút "Xoá tất cả điều kiện" ở cuối hàng chip không? Nó tiện nhưng dễ bấm nhầm và không hoàn tác được | Sau khi có màn danh sách thật |
| 3 | Chip có phản ánh được điều kiện đến từ tham số trên URL không, và gỡ nó thì URL đổi theo thế nào? Liên quan tới cách [`DataTable.md`](./DataTable.md) giữ trạng thái trên URL | Người dựng màn danh sách đầu tiên |
