---
kind: luat
scope: core
verified: chua-doi-chieu
---

# EditableGrid

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Cùng nhóm "khó" với [`DataTable.md`](./DataTable.md) theo [`../COMPONENTS.md`](../COMPONENTS.md) §4 — cộng thêm một tập ca biên riêng: di chuyển ô bằng bàn phím, mở và đóng trình sửa, dán nhiều ô cùng lúc.

---

## Mục đích

Nhập và sửa nhiều dòng dữ liệu ngay trên lưới, không mở hộp thoại cho từng dòng.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Dòng chi tiết của một chứng từ, bảng chấm công, nhập điểm, kiểm kê kho — nơi người dùng nhập hàng chục dòng liên tiếp | 🛑 Danh sách để **đọc**, sửa thì mở form → [`DataTable.md`](./DataTable.md). Xem ranh giới dưới đây |
| Ma trận tick quyền, nơi mọi ô đều tương tác | 🛑 Sửa **một** bản ghi có nhiều trường → [`Dialog.md`](./Dialog.md) chứa [`FormRow.md`](./FormRow.md); một dòng lưới không đủ chỗ cho mười trường |
| Bảng nhỏ nằm trong [`Dialog.md`](./Dialog.md) hoặc [`Drawer.md`](./Drawer.md) | 🛑 Bảng tĩnh vài dòng chỉ để trình bày → [`Table.md`](./Table.md) |

### Vì sao đây là component riêng, không phải biến thể của `DataTable`

[`../COMPONENTS.md`](../COMPONENTS.md) §1 ưu tiên mở rộng hơn đẻ mới, nên lựa chọn này cần lý do. Lý do là ba thứ **ngược nhau**, không phải khác nhau:

| Khoản | `DataTable` | `EditableGrid` |
| --- | --- | --- |
| Nguồn sự thật | Máy chủ. Dữ liệu trên màn là ảnh chụp | Trình duyệt. Dữ liệu trên màn là bản nháp chưa gửi |
| Phân trang | Phía máy chủ, đổi trang là một lần gọi | 🛑 **Không phân trang.** Đổi trang giữa chừng là vứt bản nháp |
| Phím `Enter` | Không có nghĩa | Xuống ô dưới cùng cột — thao tác cốt lõi |

Một component vừa phân trang phía máy chủ vừa giữ bản nháp chưa lưu là một component có hai nguồn sự thật. Đó là lý do tách.

**Hệ quả cứng:** `EditableGrid` **không phân trang**. Một chứng từ có 500 dòng chi tiết thì hoặc nhập theo lô nhỏ, hoặc dùng chức năng nhập từ tệp.

## Biến thể

| Biến thể | Cách vào chế độ sửa | Dùng khi |
| --- | --- | --- |
| `cell` | Bấm một ô là sửa ô đó | **Mặc định.** Nhập liệu nhanh, nhiều dòng |
| `row` | Bấm nút Sửa ở đầu dòng, cả dòng vào chế độ sửa cùng lúc | Khi các trường trong dòng phụ thuộc nhau và phải kiểm cùng lúc |
| `matrix` | Mọi ô luôn ở chế độ sửa, không có bước "vào" | Ma trận tick quyền — ô là ô đánh dấu, không phải ô nhập |

## Kích thước

| Cỡ | Chiều cao dòng | Cỡ chữ | Đệm ô | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` + `--sp-2` dọc | `--fs-xs` | `--sp-2` / `--sp-3` | Lưới trong `Dialog`, lưới nhiều dòng cần nhìn hết một màn |
| `md` | `--size-control-md` + `--sp-3` dọc | `--fs-sm` | `--sp-3` / `--sp-4` | **Mặc định** |

Chỉ hai cỡ. Cỡ `lg` không có nghĩa: lưới nhập liệu tồn tại để nhét được nhiều dòng vào một màn, và một cỡ lớn hơn đi ngược mục đích đó.

Khung: viền `--color-border-strong` và bo góc `--radius-lg`, giống `DataTable` — đây cũng là vùng nội dung tương tác được, đúng vai của bậc viền đó theo [`../DESIGN.md`](../DESIGN.md) §2.3.

Cột số **luôn căn phải** và dùng `font-variant-numeric: tabular-nums`. Không có nó, cột số trông răng cưa và mắt không so được hàng nghìn giữa hai dòng.

## Trạng thái

Component này có **hai tầng trạng thái**: của cả lưới, và của từng ô. Bảng tám dòng dưới đây là của **cả lưới**; bảng ô ở mục sau.

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Dòng nền `--color-surface`, vạch giữa hai dòng `--color-border-subtle`, `th` nền `--color-surface-2` dính đỉnh (`--z-sticky`) | Có |
| `hover` | Dòng đổi nền `--color-surface-2`; ô sửa được hiện viền mảnh `--color-border` để nói "chỗ này gõ được". **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | Ô đang được trỏ tới có `outline` `--color-focus` với `outline-offset: -2px` — offset **âm** vì vòng ngoài bị vùng cuộn cắt mất | Có |
| `active` | Ô đang mở trình sửa: viền `--color-brand`, nền `--color-surface` | Có |
| `disabled` | Cả lưới chỉ đọc: mọi ô thành `--color-text-muted`, không vào được chế độ sửa, hàng nút ở chân ẩn. **Vẫn đọc được** — không giảm opacity cả lưới | Có |
| `loading` | *Nạp lần đầu:* [`SkeletonLoader.md`](./SkeletonLoader.md) dạng hàng. *Đang lưu:* phủ `--color-scrim` + khoá thao tác, giữ nguyên dữ liệu để người dùng còn thấy mình vừa nhập gì. `aria-busy` cả hai ca | Có |
| `error` | **Hai mức.** *Mức dòng/ô:* xem bảng ô. *Mức cả lưới* (tổng không khớp, lưu hỏng): [`NoticeBanner.md`](./NoticeBanner.md) vai `danger` đặt **trên** lưới, và nút Lưu bị khoá | Có |
| `empty` | Chưa có dòng nào: thân lưới hiện một dòng mời kèm nút "Thêm dòng", **giữ nguyên tiêu đề cột** — chúng cho biết sắp nhập cái gì | Có |

### Năm trạng thái của một ô

| Trạng thái ô | Hình thức | Nghĩa |
| --- | --- | --- |
| `readonly` | Chữ `--color-text-muted`, không viền | Không sửa được |
| `calc` | Chữ `--color-text-muted`, nền `--color-surface-2`, căn phải | Máy tính ra. 🛑 Phải trông khác ô nhập được, nếu không người dùng gõ vào rồi thắc mắc vì sao bị ghi đè |
| `editing` | Viền `--color-brand`, nền `--color-surface`, có vòng focus | Đang gõ |
| `dirty` | Nền `--color-warning-bg`, viền `--color-warning-border`, **cộng một dấu tam giác nhỏ ở góc trên trái** | Đã đổi, chưa lưu |
| `invalid` | Nền `--color-danger-bg`, viền `--color-danger` dày `--border-w-strong`, cộng một dòng lỗi ngắn dưới ô | Sai, chặn lưu |

**`dirty` là trạng thái không lưới đọc nào cần, và là trạng thái quan trọng nhất ở đây.** Người dùng sửa mười hai ô rồi bấm sang màn khác — không thấy ô nào đang chưa lưu thì họ mất cả mười hai. Dấu tam giác ở góc là kênh thứ hai bên cạnh màu nền, theo [`../DESIGN.md`](../DESIGN.md) §2.7.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-subtle`, `--color-border-strong`, `--color-brand`, `--color-warning`, `--color-warning-bg`, `--color-warning-border`, `--color-danger`, `--color-danger-bg`, `--color-scrim`, `--color-focus` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fw-regular`, `--fw-semibold`, `--fw-bold`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-xs`, `--radius-lg`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--icon-sm` |
| Lớp | `--z-sticky` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-lg` | Mọi cột hiện; cột số thứ tự và cột hành động ghim hai mép |
| `--bp-md` … `--bp-lg` | Bắt đầu cuộn ngang, ghim cột đầu — thụt vào rồi thì không biết đang sửa dòng nào |
| < `--bp-md` | 🛑 **Không chuyển sang dạng thẻ** như `DataTable` làm. Giữ nguyên lưới và cuộn ngang |

**Vì sao không chuyển dạng thẻ:** giá trị của lưới nhập liệu nằm ở chỗ mắt so được cột này với cột kia giữa các dòng — nhìn cột Số lượng của cả mười dòng cùng lúc. Dạng thẻ phá đúng điều đó. Cái giá phải chấp nhận: **lưới nhập liệu là thứ khó dùng trên điện thoại**, và màn nào dựa hẳn vào nó thì nên nói rõ là dành cho máy tính.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<table>` thật với `<thead>`, `<tbody>`, `<th scope="col">`. Vai trò `role="grid"` với `aria-readonly="false"` |
| Ô | Mỗi ô sửa được là `role="gridcell"` với `aria-readonly` phản ánh đúng; ô tính toán khai `aria-readonly="true"` |
| Bàn phím — di chuyển | `↑` `↓` `←` `→` giữa các ô. `Tab` sang ô kế **trong cùng dòng**, hết dòng thì sang dòng dưới. `Home` / `End` đầu / cuối dòng |
| Bàn phím — sửa | `Enter` hoặc gõ trực tiếp để mở trình sửa. `Enter` khi đang sửa thì **xác nhận và xuống ô dưới cùng cột** — đây là thao tác cốt lõi của nhập liệu. `Escape` huỷ ô đang gõ và trả về giá trị cũ |
| Ô lỗi | `aria-invalid="true"` và `aria-describedby` trỏ vào dòng lỗi |
| Ô chưa lưu | Không có thuộc tính ARIA chuẩn cho "dirty" — dùng chữ chỉ dành cho trình đọc màn hình đặt trong ô: "đã sửa, chưa lưu" |
| Thông báo | Số dòng lỗi và số dòng chưa lưu báo qua `aria-live="polite"` ở chân lưới |
| Rời trang | Còn ô `dirty` mà người dùng điều hướng đi thì **phải hỏi**. Mất dữ liệu nhập tay là lỗi người dùng không tha thứ |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `columns` | input | `ReadonlyArray<EditableColumnDef<T>>` | `[]` | Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 — mở rộng của `ColumnDef` chung |
| `rows` | input | `ReadonlyArray<T>` | `[]` | Bản nháp đang sửa, không phải ảnh chụp từ máy chủ |
| `variant` | input | `'cell' \| 'row' \| 'matrix'` | `'cell'` | |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `readonly` | input | `boolean` | `false` | |
| `loading` | input | `boolean` | `false` | |
| `showFooter` | input | `boolean` | `true` | Dòng tổng ở chân lưới |
| `cellChanged` | output | `{row, column, value}` | — | Phát khi một ô rời chế độ sửa với giá trị mới |
| `rowAdded` | output | `void` | — | |
| `rowRemoved` | output | `string` | — | Phát khoá dòng |
| `dirtyChanged` | output | `number` | — | Số ô đang chưa lưu, để trang cha bật/tắt nút Lưu và chặn điều hướng |

Xác thực chạy ở **ba mức, và không được trộn**: **mức ô** (số phải lớn hơn 0) chạy ngay khi rời ô; **mức dòng** (hai trường trong cùng dòng phải khớp nhau) chạy khi rời dòng; **mức lưới** (tổng phải khớp một con số bên ngoài) chạy khi bấm Lưu. Trộn hai mức làm một thì hoặc báo lỗi quá sớm lúc người ta chưa gõ xong, hoặc báo quá muộn.

`EditableGrid` là component **dumb** — nó giữ bản nháp và phát sự kiện; nó **không** tự gọi API lưu ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Phân biệt rõ ô tính toán với ô nhập được bằng nền và màu chữ.
- ✅ Đánh dấu ô chưa lưu bằng cả màu lẫn một dấu hình học.
- ✅ Cho `Enter` xuống ô dưới cùng cột. Thiếu phím tắt thì lưới nhập liệu chậm hơn hộp thoại, và không ai dùng.
- ✅ Hỏi trước khi rời trang nếu còn ô chưa lưu.
- ❌ Không phân trang một lưới nhập liệu.
- ❌ Không chạy xác thực mức lưới ngay khi người dùng còn đang gõ dòng đầu.
- ❌ Không chuyển dạng thẻ ở màn nhỏ.
- ❌ Không cho sửa ô tính toán.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có hỗ trợ dán nhiều ô từ Excel không? Đây là thao tác người dùng kế toán mong đợi nhất, và cũng là phần khó nhất: phải tách chuỗi dán, khớp cột, kiểm từng ô, và báo phần không khớp | Dự án đầu tiên có màn nhập chứng từ |
| 2 | Lưu theo từng ô hay lưu cả lưới một lần? Lưu từng ô thì không mất dữ liệu nhưng sinh rất nhiều request và không kiểm được luật mức lưới; lưu một lần thì ngược lại | `architect`, vì nó chạm cả hợp đồng API |
| 3 | Hoàn tác một bước (`Ctrl+Z`) có làm không? Người nhập liệu mong đợi nó, nhưng nó đòi một ngăn xếp thao tác mà Core chưa có ở đâu cả | Sau khi có màn thật |
