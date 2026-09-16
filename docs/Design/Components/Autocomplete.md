---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Autocomplete

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** **bọc PrimeNG**. Định vị lớp nổi khi cuộn hoặc tràn viewport thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4. Phần Core tự viết là luật **khi nào gọi máy chủ** và **kết quả cũ về sau thì xử lý ra sao** — hai thứ thư viện không quyết hộ được.

---

## Mục đích

Chọn một hoặc nhiều mục từ một danh mục lớn bằng cách gõ vài ký tự.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Chọn người nhận để gửi duyệt, chọn nhân viên khi phân công — danh mục hàng trăm tới hàng nghìn mục | 🛑 Dưới vài chục mục, biết trước, không đổi → [`Input.md`](./Input.md) biến thể `select`. Bắt người dùng gõ để tìm trong tám lựa chọn là thêm việc |
| Gán nhiều vai trò cho một tài khoản | 🛑 Danh mục **phân cấp** → [`TreeSelect.md`](./TreeSelect.md) |
| Chọn khách hàng, chọn mã hàng trên một dòng chứng từ | 🛑 Lọc một danh sách đang hiển thị → ô tìm của [`Toolbar.md`](./Toolbar.md); nó lọc bảng, không chọn giá trị |
| Ô nhập cho phép tạo mới ngay nếu không tìm thấy | 🛑 Danh sách hành động → [`Menu.md`](./Menu.md) |

## Biến thể

| Biến thể | Kết quả | Dùng khi |
| --- | --- | --- |
| `single` | Một mục; chọn xong ô hiện nhãn của mục đó | **Mặc định.** Chọn người, chọn khách hàng |
| `multiple` | Nhiều mục, mỗi mục thành một chip nằm trong ô. Chip là phần cấu trúc của lớp bọc: thư viện vẽ, tạo hình bằng token theo hình thức của [`FilterChip.md`](./FilterChip.md) — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 | Gán nhiều vai trò, chọn nhiều người nhận |

Một công tắc, dùng được với cả hai:

| Công tắc | Hiệu ứng | Dùng khi |
| --- | --- | --- |
| `allowCreate` | Khi không có kết quả, hiện thêm một dòng "Tạo mới '…'" | Chỉ khi người dùng **thật sự có quyền** tạo mục đó. Hiện dòng này rồi báo lỗi quyền sau khi bấm là tệ hơn không hiện |

🛑 **Không có biến thể "gõ tự do".** Một ô vừa chọn từ danh mục vừa nhận chuỗi bất kỳ sẽ sinh ra dữ liệu không tham chiếu được — hôm nay là "Nguyễn Văn An", mai là "nguyen van an", và không truy vấn nào gộp được hai dòng đó. Cần chuỗi tự do thì đó là `Input` biến thể `text`.

## Kích thước

| Cỡ | Chiều cao ô | Cỡ chữ | Dùng khi |
| --- | --- | --- | --- |
| `sm` | `--size-control-sm` | `--fs-xs` | Ô sửa tại chỗ trong lưới; ô lọc trong một `Toolbar` cỡ `sm` |
| `md` | `--size-control-md` | `--fs-sm` | **Mặc định.** Form, dialog |
| `lg` | `--size-control-lg` | `--fs-md` | Chỉ khi ô là hành động chính của cả màn |

**Ô đặt trong [`Toolbar.md`](./Toolbar.md) lấy cỡ theo cỡ control con của `Toolbar`, và `Toolbar` là chủ của con số đó** ([`Toolbar.md`](./Toolbar.md) §Kích thước): `Toolbar` cỡ `md` → `Autocomplete` cỡ `md`; `Toolbar` cỡ `sm` → cỡ `sm`. Một ô lọc thấp hơn ô tìm nằm cạnh nó làm gãy đường chân của cả dải điều khiển, và đường chân đó là thứ duy nhất giữ cho một hàng nhiều control trông như một hàng.

Ở biến thể `multiple`, chiều cao ô **giãn theo số chip** và không còn bám thang trên. Trần cứng: quá **năm chip** thì hiện bốn chip đầu cộng một chip "và N mục khác" bấm để bung — một ô nhập cao năm dòng làm vỡ nhịp dọc của cả form.

Lớp nổi kết quả: bề rộng bằng ô, chiều cao tối đa `--layout-popover-h-max` rồi cuộn — token dùng chung với [`TreeSelect.md`](./TreeSelect.md), khai ở [`../DESIGN.md`](../DESIGN.md) §6.1. Mỗi dòng kết quả cao `--size-control-md`, gồm nhãn chính và một dòng phụ `--fs-2xs` màu `--color-text-muted` — dòng phụ là thứ phân biệt hai người trùng tên.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Ô: nền `--color-surface`, viền `--color-border-strong`. Lớp nổi đóng. Phần chuỗi khớp trong mỗi kết quả tô `--color-brand` và `--fw-semibold` — **không** đổi nền, vì nền vàng kiểu công cụ tìm kiếm phá hệ màu | Có |
| `hover` | Dòng kết quả dưới con trỏ đổi nền `--color-surface-2`. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline` `--color-focus` + `outline-offset: 2px` trên ô. Dòng đang được bàn phím trỏ tới dùng nền `--color-brand-subtle` + chữ `--color-brand-on-subtle`, và **luôn được cuộn vào tầm nhìn** | Có |
| `active` | Lớp nổi đang mở; ô giữ vòng focus | Có |
| `disabled` | Nền `--color-surface-3`, chữ `--color-text-disabled`, con trỏ `not-allowed`, thuộc tính `disabled` thật. Chip ở `multiple` mất nút gỡ | Có |
| `loading` | Vòng quay `pi-spinner` ở **đuôi ô**, không phủ lớp nổi. 🛑 **Không khoá ô** — khoá sẽ nuốt ký tự người dùng đang gõ, lỗi tệ hơn nhiều so với một request thừa. `aria-busy` đặt trên hộp kết quả | Có |
| `error` | Gọi máy chủ hỏng: lớp nổi hiện `errorTemplate` — màn ghép dòng lỗi `--color-danger` kèm nút "Thử lại" — và ô giữ nguyên chuỗi đang gõ. Lỗi **xác thực** của trường thì viền ô đổi `--color-danger-border` và dòng lỗi hiện dưới ô qua [`FormRow.md`](./FormRow.md) | Có |
| `empty` | **Ba ca phải phân biệt.** *Chưa đủ ký tự tối thiểu:* "Gõ ít nhất 2 ký tự" — **không** hiện danh sách rỗng. *Không có kết quả:* "Không tìm thấy '…'", kèm dòng tạo mới nếu `allowCreate`. *Danh mục rỗng hoàn toàn:* câu khác hẳn, nói danh mục chưa có dữ liệu | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-strong`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-danger`, `--color-focus`, `--color-danger-border` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-medium`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-sm`, `--radius-md`, `--radius-pill`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md`, `--layout-popover-h-max` |
| Bóng, lớp | `--shadow-3`, `--z-popover` |
| Icon | `pi-search`, `pi-times` để xoá, `pi-spinner` khi tải — [`../Icons.md`](../Icons.md) §5 |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Lớp nổi neo dưới ô, lật lên trên khi không đủ chỗ |
| `$bp-sm` … `$bp-md` | Giữ nguyên. Ở `multiple`, ngưỡng gom chip giảm từ năm xuống ba |
| < `$bp-sm` | Lớp nổi chuyển thành [`Drawer.md`](./Drawer.md) neo đáy với ô gõ dính đỉnh. Mỗi dòng cao tối thiểu `--size-control-lg` |

**Vì sao đổi hẳn hình dạng ở màn nhỏ:** bàn phím ảo chiếm nửa dưới màn hình, nên một lớp nổi neo dưới ô gần như luôn bị bàn phím che. Drawer neo đáy đặt ô gõ ngay trên bàn phím và danh sách phía trên nó — đó là chỗ duy nhất còn nhìn thấy được.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | Ô là `role="combobox"` với `aria-expanded`, `aria-controls`, `aria-autocomplete="list"`. Hộp kết quả là `role="listbox"`, mỗi dòng `role="option"` |
| Dòng đang trỏ tới | `aria-activedescendant` trên ô trỏ tới `id` của dòng. Focus **ở lại ô** — không di focus vào danh sách, nếu không người dùng không gõ tiếp được |
| Bàn phím | `↑` `↓` di chuyển, vòng lại khi hết. `Enter` chọn. `Escape` đóng lớp nổi, `Escape` lần hai xoá nội dung ô. `Tab` chọn dòng đang trỏ rồi đi tiếp |
| `multiple` | `Backspace` ở ô trống gỡ chip cuối — phản xạ ai cũng có. Nút gỡ trên chip có nhãn nói cả tên mục |
| Thông báo số kết quả | Qua `aria-live="polite"`: "12 kết quả". Báo sau khi ngừng gõ, không báo mỗi phím |
| Nhãn | `<label>` thật qua `FormRow`. Placeholder **không** thay được nhãn — nó biến mất ngay khi gõ ký tự đầu |
| Vùng bấm | Nút gỡ trên chip ≥ 28×28px, nới bằng `padding` |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `options` | input | `ReadonlyArray<Option>` | `[]` | Kết quả do trang cha truyền xuống. Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `value` | input | `string \| string[] \| null` | `null` | |
| `variant` | input | `'single' \| 'multiple'` | `'single'` | |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `minChars` | input | `number` | `2` | Dưới ngưỡng thì hiện gợi ý, không gọi máy chủ. Một ký tự trả về vài nghìn dòng, vô dụng với người dùng và nặng với máy chủ |
| `debounceMs` | input | `number` | `300` | Chờ người dùng ngừng gõ rồi mới phát `search` |
| `allowCreate` | input | `boolean` | `false` | |
| `loading` | input | `boolean` | `false` | |
| `errorTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Khối lỗi trong lớp nổi, gồm nút "Thử lại". Màn chỉ truyền khi gọi máy chủ hỏng — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 |
| `disabled` | input | `boolean` | `false` | |
| `placeholder` | input | `string` | — | **Bắt buộc** |
| `search` | output | `string` | — | Phát chuỗi cần tìm sau khi đã chờ `debounceMs` |
| `valueChange` | output | `string \| string[]` | — | |
| `createRequested` | output | `string` | — | Chỉ phát khi `allowCreate` và người dùng chọn dòng tạo mới |

`Autocomplete` là component **dumb** — nó **không** tự gọi HTTP. Nó phát `search`, trang cha gọi API và truyền `options` xuống ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

### Một luật thi công không được bỏ

🛑 **Bỏ qua kết quả của request cũ khi request mới đã về.** Gõ nhanh sinh nhiều request, và mạng không đảm bảo thứ tự trả về — không chặn thì người dùng thấy danh sách nhảy về kết quả của chuỗi đã gõ xong từ lâu. Đây là lỗi rất khó truy vì nó chỉ hiện ra khi mạng chậm. Trang cha giữ số thứ tự request và bỏ kết quả đến muộn; component không làm thay được vì nó không sở hữu lời gọi.

## Do / Don't

- ✅ Hiện dòng phụ để phân biệt các mục trùng tên.
- ✅ Nói rõ còn bao nhiêu kết quả chưa hiện: "12 trên 41 — gõ thêm để thu hẹp".
- ✅ Cho `Backspace` gỡ chip cuối ở `multiple`.
- ✅ Thiết kế trạng thái không-có-kết-quả kèm lối thoát.
- ❌ Không gọi máy chủ ở mỗi phím.
- ❌ Không khoá ô khi đang tải.
- ❌ Không hiện danh sách rỗng khi chưa đủ ký tự tối thiểu.
- ❌ Không cho gõ tự do rồi lưu thẳng chuỗi đó thành dữ liệu.
- ❌ Không tô nền vàng cho phần chuỗi khớp.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có hiện vài mục gợi ý **trước khi gõ** không (mục dùng gần đây)? Nó rút ngắn thao tác lặp lại rất nhiều, nhưng cần một chỗ lưu lịch sử theo người dùng mà Core chưa có | Sau F3 — dự án hạ nguồn đầu tiên đo được thao tác lặp |
| 2 | `minChars` = 2 có đúng cho tiếng Việt không? Nhiều họ tên bắt đầu bằng hai ký tự rất phổ biến ("Ng", "Tr"), nên hai ký tự vẫn trả về hàng trăm dòng | F3 — khi dựng màn `10-nguoi-dung` trên danh mục thật |
| 3 | Ngưỡng gom chip là năm — đúng chưa? Cao hơn thì ô phình, thấp hơn thì phải bung ra liên tục | F3 — khi dựng màn `10-nguoi-dung` (ô gán nhiều vai trò) |
