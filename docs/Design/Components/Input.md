---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Input

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng quanh phần tử biểu mẫu gốc của HTML. Không bọc PrimeNG — theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4, một ô nhập không có hành vi khó nào; `<input>` gốc đã cho sẵn toàn bộ ngữ nghĩa, hỗ trợ trình quản lý mật khẩu, tự động điền, và bàn phím phù hợp trên di động.

---

## Mục đích

Một hợp đồng hình thức **duy nhất** cho mọi ô nhập liệu, để cả ứng dụng chỉ có một cái ô nhập.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Mọi ô nhập trong form, dialog, `Toolbar` | 🛑 Ô nhập trên màn xác thực → [`AuthField.md`](./AuthField.md). Nó có icon dẫn, cỡ `lg` và `autocomplete` thuộc nhóm xác thực |
| Ô tìm kiếm | 🛑 Cần nhãn + gợi ý + chỗ hiện lỗi → bọc trong [`FormRow.md`](./FormRow.md). `Input` **không** tự vẽ nhãn |
| Ô chỉ đọc hiển thị dữ liệu không sửa được | 🛑 Bật/tắt một giá trị nhị phân → [`Check.md`](./Check.md); 🛑 ô ngày, khoảng ngày → [`DatePicker.md`](./DatePicker.md) |
| | 🛑 Chọn một trong ba, bốn giá trị → [`SegmentedControl.md`](./SegmentedControl.md), dễ thấy hơn một select; 🛑 danh mục lớn cần gõ để tìm → [`Autocomplete.md`](./Autocomplete.md) — `select` không có biến thể combobox |

**`Input` không tự vẽ nhãn — đây là quyết định, không phải thiếu sót.** Nhãn, dấu bắt buộc, gợi ý và chỗ hiện lỗi thuộc `FormRow`. Nếu `Input` tự vẽ nhãn thì mỗi lần cần một bố cục nhãn khác (nhãn nằm ngang, nhãn ẩn đi chỉ còn cho trình đọc màn hình) lại phải thêm một tuỳ chọn vào `Input`, và nó sẽ phình ra.

## Biến thể

| Biến thể | Phần tử gốc | Khác gì |
| --- | --- | --- |
| `text` | `<input type="text">` | Mặc định |
| `number` | `<input type="text" inputmode="decimal">` | Căn phải nội dung. Không dùng `type="number"` — nó không nhận dấu phân cách. Tự định dạng phân cách hàng nghìn khi blur, bỏ định dạng khi focus; giá trị đi qua form control là số, không phải chuỗi đã định dạng |
| `password` | `<input type="password">` | Có nút hiện/ẩn ở vị trí `suffix` khi `revealable` bật (mặc định). Hợp đồng của nút — `type="button"`, nhãn đổi theo trạng thái, thứ tự Tab, vòng focus riêng, vùng bấm, trạng thái nhấn và vô hiệu hoá — dùng chung với [`AuthField.md`](./AuthField.md) §Accessibility, không khai lại ở đây. Icon theo [`../Icons.md`](../Icons.md) §5 dòng Hiện/Ẩn mật khẩu; đệm phải chừa chỗ nút tính bằng `calc()` từ bậc `--sp-*` và `--icon-md` như [`AuthField.md`](./AuthField.md) §Kích thước, bằng token riêng của `Input` |
| `select` | `<select>` | Có mũi tên chỉ xuống ở mép phải; đệm phải nới thêm để chữ không đè lên mũi tên |
| `textarea` | `<textarea>` | Cao tối thiểu ba dòng; chỉ cho kéo giãn theo chiều dọc |
| `search` | `<input type="search">` | Có icon `pi-search` dẫn và nút xoá khi có nội dung |

Ô ngày và khoảng ngày **không** là biến thể của `Input`: chúng là [`DatePicker.md`](./DatePicker.md), bọc PrimeNG, vì `dd/mm/yyyy` và lối tắt không ép được bằng bộ chọn gốc của trình duyệt.

Ba công tắc, dùng được với mọi biến thể:

| Công tắc | Hiệu ứng |
| --- | --- |
| `prefix` / `suffix` | Chèn một icon hoặc một đơn vị ("VNĐ", "%") vào trong khung ô, dùng cùng nền, không có viền riêng |
| `readonly` | Đọc được, chọn được, copy được, không sửa được. **Khác `disabled`** — xem bảng trạng thái |
| `width` | `full` (mặc định) · `md` · `sm` — dùng cho ô số, nơi một ô rộng hết hàng trông sai |

## Kích thước

| Cỡ | Chiều cao | Đệm | Cỡ chữ | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--sp-3` / `--sp-4` | `--fs-xs` | Ô lọc trong `Toolbar`, ô sửa tại chỗ trong bảng |
| `md` | `--size-control-md` (34px) | `--sp-3` / `--sp-4` | `--fs-sm` | **Mặc định.** Form, dialog |
| `lg` | `--size-control-lg` (42px) | `--sp-4` / `--sp-5` | `--fs-md` | Chỉ khi ô nhập là hành động chính của cả màn |

`textarea` không nhận cỡ — nó luôn dùng đệm và cỡ chữ của `md`, chiều cao do số dòng quyết định.

**Cỡ chữ trong ô nhập không được nhỏ hơn `--fs-sm` (13px) trên di động.** Safari trên iOS **tự phóng to cả trang** khi focus vào một ô có cỡ chữ dưới 16px; với 13px thì nó vẫn phóng, nhưng đây là đánh đổi đã cân nhắc — ứng dụng quản trị dày dữ liệu không chịu được cỡ chữ 16px ở mọi ô. Màn nào thật sự dùng nhiều trên di động thì đặt cỡ `lg`.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`; viền `--border-w` `--color-border-strong`; chữ `--color-text`; `--radius-sm`. Placeholder dùng `--color-text-muted` | Có |
| `hover` | Viền đậm thêm một bậc trong thang xám; nền không đổi. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px`; viền chuyển `--color-brand`. Ô nhập là chỗ duy nhất trong hệ dùng **cả hai** dấu hiệu — vòng ngoài cho người dùng bàn phím, viền đổi màu cho người dùng chuột đã bấm vào ô | Có |
| `active` | **Không áp dụng.** Ô nhập không có trạng thái "đang bị nhấn" phân biệt được với focus | — |
| `disabled` | Nền `--color-surface-3`; chữ `--color-text-disabled`; viền `--color-border`; `cursor: not-allowed`. Thuộc tính `disabled` thật; nút hiện/ẩn của `password` cũng `disabled`. **Giá trị không được gửi lên khi submit** — đó là điểm khác cốt lõi với `readonly` | Có |
| `loading` | Ô bị vô hiệu hoá tạm; hiện spinner ở vị trí `suffix`; `aria-busy="true"`. Chỉ dùng khi bản thân ô đang chờ dữ liệu (danh sách gợi ý, kiểm tra trùng), **không** dùng khi cả form đang gửi — lúc đó khoá ở nút submit | Có |
| `error` | Bật khi control `invalid && touched` (§API dự kiến); ô không gắn control không có trạng thái này. Viền `--border-w-strong` `--color-danger-border`; nền giữ `--color-surface` (không tô đỏ — chữ người dùng gõ phải đọc được); `aria-invalid="true"`; `aria-describedby` trỏ tới dòng lỗi do `FormRow` vẽ | Có |
| `empty` | **Không áp dụng.** Ô rỗng là trạng thái bình thường, và `placeholder` đã xử lý nó. Xem cảnh báo dưới | — |

**`readonly` khác `disabled` ở ba điểm, và chọn nhầm là một lỗi thật:**

| | `readonly` | `disabled` |
| --- | --- | --- |
| Nhận focus bằng Tab | ✅ Có | 🛑 Không |
| Copy được nội dung | ✅ Có | ⚠️ Khó |
| Gửi lên khi submit | ✅ Có | 🛑 **Không** |

Dùng `disabled` cho một trường chỉ để hiển thị (mã bản ghi tự sinh chẳng hạn) sẽ khiến giá trị đó **biến mất khỏi request**, và bug sẽ xuất hiện ở tầng máy chủ chứ không ở đây.

**Placeholder không phải nhãn.** Nó biến mất ngay khi người dùng gõ, nên người dùng mất mốc tham chiếu giữa chừng, và trình đọc màn hình xử lý nó không nhất quán. Nhãn thật do `FormRow` vẽ; placeholder chỉ dùng cho **ví dụ định dạng** ("0912345678"), không dùng để nhắc lại tên trường.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-strong`, `--color-brand`, `--color-danger-border`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-regular`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-sm`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

**Ô nhập là một trong hai chỗ duy nhất dùng `--color-border-strong`** (chỗ kia là khung bảng có thể sửa). Đó là tín hiệu *"chỗ này gõ được"* — xem [`../DESIGN.md`](../DESIGN.md) §2.3. Đặt `--color-border-strong` lên một `Card` là xoá mất tín hiệu duy nhất báo điều đó.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Ô rộng theo cột lưới của `FormRow`; công tắc `width` có tác dụng |
| < `$bp-md` | Mọi ô về `width: full`; lưới form về một cột |
| < `$bp-xs` | Ô số cũng full width; `suffix` xuống dòng nếu là chữ dài |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Phần tử biểu mẫu gốc (`<input>`, `<select>`, `<textarea>`). 🛑 Không `<div contenteditable>` |
| Nhãn | **Bắt buộc có nhãn**, gắn bằng `<label for>` hoặc `aria-labelledby`. `FormRow` chịu trách nhiệm phần này; `Input` phải nhận và gắn `id` |
| `autocomplete` | Đặt đúng giá trị chuẩn. Thiếu nó thì trình quản lý mật khẩu và tự động điền không hoạt động — người dùng phải gõ tay mọi thứ |
| `inputmode` | Đặt cho ô số và ô điện thoại, để di động hiện bàn phím số |
| Lỗi | `aria-invalid="true"` suy từ control: `invalid && touched` (§API dự kiến); `aria-describedby` = `describedBy` trang truyền, trỏ tới id của dòng lỗi. Chỉ đổi viền là không đủ — [`../DESIGN.md`](../DESIGN.md) §2.7 |
| Bắt buộc | `required` **và** dấu hiệu nhìn thấy được ở nhãn. Chỉ có dấu sao mà không có `required` thì trình đọc màn hình không biết |
| Focus | `outline` + `outline-offset` ≥ 2px, và viền đổi màu |
| `readonly` | Vẫn nhận Tab. Người dùng bàn phím phải đọc được nội dung |
| Nút hiện/ẩn (`password`) | Theo [`AuthField.md`](./AuthField.md) §Accessibility |
| Chữ | Placeholder, nhãn, thông báo lỗi đều qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `id` | input | `string \| null` | `null` | Gắn lên phần tử biểu mẫu gốc. Trong `FormRow`, giá trị phải trùng `controlId` của `FormRow` để `<label for>` trỏ đúng ô |
| `type` | input | `'text' \| 'number' \| 'password' \| 'select' \| 'textarea' \| 'search'` | `'text'` | Ngày, khoảng ngày → [`DatePicker.md`](./DatePicker.md) |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `width` | input | `'full' \| 'md' \| 'sm'` | `'full'` | |
| `placeholder` | input | `string \| null` | `null` | Chỉ dùng cho ví dụ định dạng |
| `disabled` | input | `boolean` | `false` | Chỉ khi ô **không** gắn control; gắn control thì vô hiệu hoá đến qua `setDisabledState` |
| `readonly` | input | `boolean` | `false` | |
| `describedBy` | input | `string \| null` | `null` | Id của dòng lỗi hoặc dòng gợi ý mà `FormRow` vẽ, theo khuôn cố định `<controlId>-error` / `<controlId>-hint` ([`FormRow.md`](./FormRow.md) §Accessibility). **Trang truyền tường minh**: có lỗi thì trỏ dòng lỗi; không lỗi mà có gợi ý thì trỏ dòng gợi ý. Gắn thành `aria-describedby` trên phần tử gốc |
| `loading` | input | `boolean` | `false` | |
| `prefix` / `suffix` | input | `string \| null` | `null` | Tên icon hoặc chuỗi đơn vị |
| `autocomplete` | input | `string \| null` | `null` | |
| `inputmode` | input | `'text' \| 'decimal' \| 'numeric' \| 'tel' \| 'email' \| 'url' \| 'search' \| null` | `null` | Gắn thành `inputmode` trên phần tử gốc, quyết bàn phím ảo trên di động (§Accessibility). `null` = không đặt thuộc tính |
| `revealable` | input | `boolean` | `true` | Chỉ có nghĩa khi `type` là `'password'`. Trạng thái hiện/ẩn do `Input` tự giữ — trạng thái trình bày thuần, cùng lý do ở [`AuthField.md`](./AuthField.md) §API |
| `options` | input | `ReadonlyArray<SegmentOption>` | `[]` | Chỉ dùng khi `type` là `'select'`. Dùng lại kiểu đã có ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 thay vì khai một hình dạng thứ ba cho cùng khái niệm "một lựa chọn" |
| `valueChange` | output | `string \| number \| null` | — | Chỉ khi ô **không** gắn control — ô tìm của [`Toolbar.md`](./Toolbar.md) |

**Form control chuẩn Angular.** `app-input` cài `ControlValueAccessor`: nhận `[formControl]` và `formControlName` như một phần tử biểu mẫu gốc; giá trị, `touched` (lúc rời ô) và vô hiệu hoá đi qua control. Trạng thái `error` — viền và `aria-invalid` — suy từ control: `invalid && touched`. Câu lỗi do trang lấy từ `fieldErrorText` ([`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.5) và truyền cho `error` của `FormRow`; `describedBy` vẫn do trang truyền vì `Input` không biết `FormRow` có gợi ý hay không. Ca không gắn control chỉ còn ô tìm của [`Toolbar.md`](./Toolbar.md): `valueChange` và `disabled` dành cho ca đó; không có output rời ô — ca có control đã có `touched`, ca không có control không cần. Mẫu gắn vào form, kèm `id` trùng `controlId` của `FormRow`: cùng file §6.4.

**`Input` đọc trạng thái, không đọc luật.** Nó không biết validator nào gắn ở control và không được biết; nó chỉ hỏi control *đang lỗi và đã chạm chưa* rồi vẽ theo. Đây là điều giữ cho nó là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Luôn bọc trong `FormRow` khi ô có nhãn. `Input` trần chỉ dùng trong `Toolbar` — nơi có `aria-label` thay cho nhãn nhìn thấy.
- ✅ Trong form, gắn qua `[formControl]` / `formControlName`; `valueChange` chỉ cho ô tìm của `Toolbar`.
- ✅ Luôn đặt `autocomplete` cho ô có ý nghĩa chuẩn (email, tên, mật khẩu, địa chỉ).
- ✅ Dùng `readonly` cho trường chỉ hiển thị nhưng vẫn phải gửi lên.
- ✅ Đặt `inputmode` cho ô số — nó đổi bàn phím trên di động.
- ❌ Không dùng placeholder thay nhãn.
- ❌ Không tô nền đỏ khi lỗi. Chữ người dùng vừa gõ phải đọc được.
- ❌ Không dùng `disabled` cho trường cần gửi lên máy chủ.
- ❌ Không cho `textarea` kéo giãn theo chiều ngang — nó sẽ tràn khỏi lưới form.
- ❌ Không đặt `--color-border-strong` lên component không nhập liệu được.

## Cần chốt

Không còn.
