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
| Mọi ô nhập trong form, dialog, `Toolbar` | 🛑 Ô nhập trên màn xác thực → [`AuthField.md`](./AuthField.md). Nó có icon dẫn và nút hiện/ẩn mật khẩu, và cỡ `lg` |
| Ô tìm kiếm | 🛑 Cần nhãn + gợi ý + chỗ hiện lỗi → bọc trong [`FormRow.md`](./FormRow.md). `Input` **không** tự vẽ nhãn |
| Ô chỉ đọc hiển thị dữ liệu không sửa được | 🛑 Bật/tắt một giá trị nhị phân → [`Check.md`](./Check.md) |
| | 🛑 Chọn một trong ba, bốn giá trị → [`SegmentedControl.md`](./SegmentedControl.md), dễ thấy hơn một select |

**`Input` không tự vẽ nhãn — đây là quyết định, không phải thiếu sót.** Nhãn, dấu bắt buộc, gợi ý và chỗ hiện lỗi thuộc `FormRow`. Nếu `Input` tự vẽ nhãn thì mỗi lần cần một bố cục nhãn khác (nhãn nằm ngang, nhãn ẩn đi chỉ còn cho trình đọc màn hình) lại phải thêm một tuỳ chọn vào `Input`, và nó sẽ phình ra.

## Biến thể

| Biến thể | Phần tử gốc | Khác gì |
| --- | --- | --- |
| `text` | `<input type="text">` | Mặc định |
| `number` | `<input type="number">` | Căn phải nội dung; ẩn mũi tên tăng giảm mặc định của trình duyệt |
| `password` | `<input type="password">` | Không có nút hiện/ẩn — đó là việc của `AuthField` |
| `date` | `<input type="date">` | Dùng bộ chọn ngày của trình duyệt |
| `select` | `<select>` | Có mũi tên chỉ xuống ở mép phải; đệm phải nới thêm để chữ không đè lên mũi tên |
| `textarea` | `<textarea>` | Cao tối thiểu ba dòng; chỉ cho kéo giãn theo chiều dọc |
| `search` | `<input type="search">` | Có icon `pi-search` dẫn và nút xoá khi có nội dung |
| `daterange` | Hai ô ngày trong một khung | Kỳ báo cáo, lọc chứng từ theo khoảng. Xem luật riêng dưới đây |

### Biến thể `daterange` — bốn luật riêng

Khoảng ngày là chỗ dữ liệu sai mà không báo lỗi, nên nó có luật riêng:

1. 🛑 **Định dạng là `dd/mm/yyyy`, không thương lượng.** Người dùng Việt Nam đọc `03/09` là ngày 3 tháng 9. Hiện theo `mm/dd` ở bất cứ đâu là tạo ra một lỗi nhập liệu im lặng — không sai cú pháp, chỉ sai ngày, và không ai phát hiện cho tới lúc đối chiếu.
2. **Chặn khoảng ngược ngay lúc chọn**, không phải lúc bấm Tìm. Cách gọn: chọn ngày đầu xong thì mọi ngày trước đó bị làm mờ trong lịch.
3. **Gõ tay phải chấp nhận được.** Cho gõ `01092026` và tự chèn dấu gạch chéo. Ép mở lịch mới chọn được là làm chậm đúng nhóm người dùng mà màn danh sách phục vụ — họ nhập nhanh hơn chuột rất nhiều.
4. **Lối tắt đặt bên trái panel lịch**, và danh sách lối tắt là **đầu vào của trang**, không phải hằng số của component. Core chỉ khai bộ mặc định độc lập nghiệp vụ: hôm nay · 7 ngày qua · tháng này · quý này · năm nay. Trang truyền thêm lối tắt của riêng mình qua `presets`.

🛑 **Lối tắt mang nghĩa nghiệp vụ không được khai ở đây.** Một dự án kế toán có "kỳ đang mở", một dự án nhân sự có "kỳ lương", một dự án đào tạo có "khoá hiện hành" — ba thứ đó là **luật hiển thị theo nghiệp vụ**, và [`../CLAUDE.md`](../CLAUDE.md) §1 quy định chúng sống ở `spec/<feature>/ui-spec.md`, không sống trong `Components/`. Component chỉ cam kết một điều: nhận được danh sách lối tắt thì vẽ ra đúng như vậy.

Cùng lý do đó, **trạng thái "khoảng đã chọn bị khoá sửa"** — dữ liệu chỉ đọc vì kỳ đã chốt sổ, vì hồ sơ đã duyệt, vì bất cứ lý do nghiệp vụ nào — không phải trạng thái của ô nhập. Ô vẫn hợp lệ; việc cảnh báo là của trang, và nó dùng [`NoticeBanner.md`](./NoticeBanner.md) vai `warning`. Đó là cảnh báo chứ không phải lỗi, và hai thứ đó không được trộn.

Ba công tắc, dùng được với mọi biến thể:

| Công tắc | Hiệu ứng |
| --- | --- |
| `prefix` / `suffix` | Chèn một icon hoặc một đơn vị ("VNĐ", "%") vào trong khung ô, dùng cùng nền, không có viền riêng |
| `readonly` | Đọc được, chọn được, copy được, không sửa được. **Khác `disabled`** — xem bảng trạng thái |
| `width` | `full` (mặc định) · `md` · `sm` — dùng cho ô ngày tháng và ô số, nơi một ô rộng hết hàng trông sai |

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
| `disabled` | Nền `--color-surface-3`; chữ `--color-text-disabled`; viền `--color-border`; `cursor: not-allowed`. Thuộc tính `disabled` thật. **Giá trị không được gửi lên khi submit** — đó là điểm khác cốt lõi với `readonly` | Có |
| `loading` | Ô bị vô hiệu hoá tạm; hiện spinner ở vị trí `suffix`; `aria-busy="true"`. Chỉ dùng khi bản thân ô đang chờ dữ liệu (danh sách gợi ý, kiểm tra trùng), **không** dùng khi cả form đang gửi — lúc đó khoá ở nút submit | Có |
| `error` | Viền `--border-w-strong` `--color-danger`; nền giữ `--color-surface` (không tô đỏ — chữ người dùng gõ phải đọc được); `aria-invalid="true"`; `aria-describedby` trỏ tới dòng lỗi do `FormRow` vẽ | Có |
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
| Màu | `--color-surface`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-strong`, `--color-brand`, `--color-danger`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-regular`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-sm`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

**Ô nhập là một trong hai chỗ duy nhất dùng `--color-border-strong`** (chỗ kia là khung bảng có thể sửa). Đó là tín hiệu *"chỗ này gõ được"* — xem [`../DESIGN.md`](../DESIGN.md) §2.3. Đặt `--color-border-strong` lên một `Card` là xoá mất tín hiệu duy nhất báo điều đó.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Ô rộng theo cột lưới của `FormRow`; công tắc `width` có tác dụng |
| < `--bp-md` | Mọi ô về `width: full`; lưới form về một cột |
| < `--bp-xs` | Ô ngày và ô số cũng full width; `suffix` xuống dòng nếu là chữ dài |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Phần tử biểu mẫu gốc (`<input>`, `<select>`, `<textarea>`). 🛑 Không `<div contenteditable>` |
| Nhãn | **Bắt buộc có nhãn**, gắn bằng `<label for>` hoặc `aria-labelledby`. `FormRow` chịu trách nhiệm phần này; `Input` phải nhận và gắn `id` |
| `autocomplete` | Đặt đúng giá trị chuẩn. Thiếu nó thì trình quản lý mật khẩu và tự động điền không hoạt động — người dùng phải gõ tay mọi thứ |
| `inputmode` | Đặt cho ô số và ô điện thoại, để di động hiện bàn phím số |
| Lỗi | `aria-invalid="true"` + `aria-describedby` trỏ tới id của dòng lỗi. Chỉ đổi viền là không đủ — [`../DESIGN.md`](../DESIGN.md) §2.7 |
| Bắt buộc | `required` **và** dấu hiệu nhìn thấy được ở nhãn. Chỉ có dấu sao mà không có `required` thì trình đọc màn hình không biết |
| Focus | `outline` + `outline-offset` ≥ 2px, và viền đổi màu |
| `readonly` | Vẫn nhận Tab. Người dùng bàn phím phải đọc được nội dung |
| Chữ | Placeholder, nhãn, thông báo lỗi đều qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `type` | input | `'text' \| 'number' \| 'password' \| 'date' \| 'daterange' \| 'select' \| 'textarea' \| 'search'` | `'text'` | `'daterange'` có bốn luật riêng ở mục Biến thể |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `width` | input | `'full' \| 'md' \| 'sm'` | `'full'` | |
| `placeholder` | input | `string \| null` | `null` | Chỉ dùng cho ví dụ định dạng |
| `disabled` | input | `boolean` | `false` | |
| `readonly` | input | `boolean` | `false` | |
| `invalid` | input | `boolean` | `false` | Chỉ **hình thức**. Việc quyết định có lỗi hay không thuộc `FormRow` |
| `describedBy` | input | `string \| null` | `null` | Id của dòng lỗi/gợi ý; `FormRow` truyền vào |
| `loading` | input | `boolean` | `false` | |
| `prefix` / `suffix` | input | `string \| null` | `null` | Tên icon hoặc chuỗi đơn vị |
| `autocomplete` | input | `string \| null` | `null` | |
| `options` | input | `ReadonlyArray<SegmentOption>` | `[]` | Chỉ dùng khi `type` là `'select'`. Dùng lại kiểu đã có ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 thay vì khai một hình dạng thứ ba cho cùng khái niệm "một lựa chọn" |
| `presets` | input | `ReadonlyArray<DateRangePreset>` | `[]` | Chỉ dùng khi `type` là `'daterange'`. **Đây là điểm mở rộng của biến thể đó**: lối tắt mang nghĩa nghiệp vụ do trang truyền vào, không khai trong Core. Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9
| `valueChange` | output | `string \| number \| null` | — | |
| `blurred` | output | `void` | — | `FormRow` dùng sự kiện này để quyết định lúc nào hiện lỗi |

**`invalid` chỉ là hình thức, không phải logic.** `Input` không biết luật kiểm tra dữ liệu và không được biết. Nó nhận một cờ và vẽ theo. Đây là điều giữ cho nó là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5) và cũng là điều cho phép nó dùng được với bất kỳ cơ chế form nào.

## Do / Don't

- ✅ Luôn bọc trong `FormRow` khi ô có nhãn. `Input` trần chỉ dùng trong `Toolbar` — nơi có `aria-label` thay cho nhãn nhìn thấy.
- ✅ Luôn đặt `autocomplete` cho ô có ý nghĩa chuẩn (email, tên, mật khẩu, địa chỉ).
- ✅ Dùng `readonly` cho trường chỉ hiển thị nhưng vẫn phải gửi lên.
- ✅ Đặt `inputmode` cho ô số — nó đổi bàn phím trên di động.
- ❌ Không dùng placeholder thay nhãn.
- ❌ Không tô nền đỏ khi lỗi. Chữ người dùng vừa gõ phải đọc được.
- ❌ Không dùng `disabled` cho trường cần gửi lên máy chủ.
- ❌ Không cho `textarea` kéo giãn theo chiều ngang — nó sẽ tràn khỏi lưới form.
- ❌ Không đặt `--color-border-strong` lên component không nhập liệu được.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | `type="date"` dùng bộ chọn ngày của trình duyệt hay bọc một bộ chọn của thư viện? Bộ của trình duyệt miễn phí và accessibility tốt, nhưng hình thức khác nhau giữa các trình duyệt và không đổi được định dạng ngày kiểu Việt Nam | Dự án đầu tiên có nhiều trường ngày |
| 2 | Ô select có cần biến thể tìm kiếm trong danh sách (combobox) không? Nếu có thì nó thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 và phải bọc thư viện, tách thành component riêng | Khi có danh sách trên vài chục mục |
| 3 | Ô số có tự định dạng phân cách hàng nghìn khi mất focus không? Định dạng làm dễ đọc nhưng phá `type="number"` (nó không nhận dấu phân cách) | Người dựng component |
