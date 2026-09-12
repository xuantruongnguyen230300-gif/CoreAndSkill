---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Check

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — dòng "ngữ nghĩa control biểu mẫu gốc + trạng thái nửa chọn": `indeterminate` **không phải một thuộc tính HTML**, nó chỉ đặt được bằng property trên phần tử DOM, nên một template thuần không khai được nó.

---

## Mục đích

Bật/tắt một tuỳ chọn (ô đánh dấu) hoặc chọn đúng một trong nhiều tuỳ chọn (radio), có cả trạng thái nửa chọn.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Bật/tắt một tuỳ chọn độc lập — "Ghi nhớ đăng nhập", "Gửi email thông báo" | 🛑 Chuyển giữa vài **chế độ xem** loại trừ nhau → [`SegmentedControl.md`](./SegmentedControl.md); radio là để chọn **giá trị**, không phải để đổi cái đang nhìn |
| Chọn nhiều mục trong một danh sách các lựa chọn | 🛑 Hơn khoảng bảy lựa chọn radio → dùng ô chọn (`select`) theo [`Input.md`](./Input.md); một cột hai chục nút radio không đọc được |
| Chọn một trong hai đến bảy lựa chọn loại trừ (radio) | 🛑 Ô chọn tất cả ở đầu cột bảng có phân trang máy chủ → [`DataTable.md`](./DataTable.md) sở hữu ngữ nghĩa "tất cả" ở đó, vì "tất cả" là trang này hay toàn bộ kết quả là một quyết định nghiệp vụ |
| Ô chọn hàng trong [`Table.md`](./Table.md) tĩnh, và ô "chọn tất cả" ở `th` | 🛑 Thực thi một hành động ngay khi bấm → [`Button.md`](./Button.md); một ô đánh dấu gây tác dụng phụ tức thì là bẫy cho người dùng bàn phím đang lướt qua |

**Bẫy đã biết — bấm vào chữ phải ăn.** Vùng bấm của riêng cái ô chỉ bằng cạnh ô (`--icon-md`, 16px), dưới ngưỡng thoải mái. Nhãn phải là `<label>` gắn với input (qua `for`/`id` hoặc bằng cách bọc input), khi đó cả cụm ô + chữ thành một vùng bấm. Một `<span>` đặt cạnh input trông giống hệt nhưng bấm không ăn, và không ai phát hiện cho tới khi có người dùng thật.

## Biến thể

| Biến thể | Vai | Hình dạng | Ghi chú |
| --- | --- | --- | --- |
| `checkbox` | **Mặc định.** Bật/tắt độc lập, hoặc chọn nhiều trong danh sách | `--radius-xs` | Có trạng thái nửa chọn |
| `radio` | Chọn đúng một trong nhiều, luôn đứng trong một nhóm | `--radius-full` | Không có nửa chọn; không tự bỏ chọn được bằng cách bấm lại |

**Hình vuông và hình tròn là hợp đồng với người dùng, không phải trang trí.** Vuông = chọn được nhiều; tròn = chọn được một. Đổi bo góc của radio thành `--radius-xs` cho "đồng bộ" là xoá mất tín hiệu duy nhất báo trước rằng chọn cái này sẽ bỏ cái kia. Đây là lý do hai biến thể tách bạch hình dạng chứ không dùng chung.

Nhãn đặt **bên phải** ô, khe `--sp-4`. Không đặt nhãn bên trái: người dùng quét cột ô đánh dấu theo chiều dọc, và nhãn bên trái làm mép ô so le theo độ dài chữ.

## Kích thước

| Cỡ | Cạnh ô | Cỡ chữ nhãn | Vùng bấm tối thiểu | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--icon-sm` (14px) | `--fs-xs` | `--size-control-sm` (28px) | Trong hàng bảng, trong chip lọc |
| `md` | `--icon-md` (16px) | `--fs-sm` | `--size-control-md` (34px) | **Mặc định.** Form, dialog, danh sách tuỳ chọn |

**Không có cỡ `lg`, có chủ đích.** Ba cỡ ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.2 là ba **chiều cao control**; một ô đánh dấu 42px đứng cạnh nhãn cỡ `--fs-md` trông như một ô nhập bị hỏng chứ không như một tuỳ chọn. Cái giá: màn xác thực dùng `md` thay vì `lg` như các control khác trên [`AuthCard.md`](./AuthCard.md), nên hàng "Ghi nhớ đăng nhập" thấp hơn ô nhập phía trên — chấp nhận được vì nó vốn là một dòng phụ.

Vùng bấm nới bằng `padding` của `<label>`, không bằng `margin` — `margin` không nhận sự kiện chuột ([`../COMPONENTS.md`](../COMPONENTS.md) §2.2).

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Chưa chọn: nền `--color-surface`, viền `--color-border-strong` (`--border-w`). Đã chọn: nền `--color-brand`, dấu tick `--color-text-on-brand`. Nửa chọn: nền `--color-brand`, một gạch ngang `--color-text-on-brand` | Có |
| `hover` | Viền đậm thêm; nền `<label>` chuyển `--color-surface-2` để lộ ra rằng cả dòng bấm được. `--dur-fast` với `--ease-standard`. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px` **trên chính cái ô**, không trên cả dòng. Vòng focus ôm cả dòng làm người dùng bàn phím mất dấu ô nào đang được chọn trong một cột dài | Có |
| `active` | Viền `--color-brand-active`; không dịch chuyển hình — một ô nhỏ cỡ `--icon-md` mà nhích 1px thì trông như lỗi vẽ chứ không như phản hồi | Có |
| `disabled` | Nền `--color-surface-3`; viền `--color-border`; nhãn `--color-text-disabled`; `cursor: not-allowed`. Thuộc tính `disabled` thật trên input | Có |
| `loading` | **Không áp dụng cho chính ô.** Ô đánh dấu đổi giá trị tức thì; nếu việc lưu cần thời gian thì trạng thái chờ thuộc về nút Lưu ở [`Button.md`](./Button.md) hoặc về cả cụm form. Ca lưu-ngay (autosave) xem `Cần chốt` #2 | — |
| `error` | Viền `--color-danger-border`; dòng lỗi chữ `--color-danger` đặt dưới **cả nhóm**, không dưới từng ô. Kênh thứ hai là chữ, viền đỏ một mình không đủ — [`../DESIGN.md`](../DESIGN.md) §2.7 | Có |
| `empty` | **Không áp dụng.** Một nhóm radio không có lựa chọn nào là lỗi dữ liệu ở tầng gọi, không phải trạng thái của control. Nơi gọi phải hiện [`EmptyState.md`](./EmptyState.md) thay cho cả nhóm | — |

**Vì sao nửa chọn dùng gạch ngang chứ không dùng ô mờ:** nửa chọn nghĩa là "một số con đã chọn", và người dùng phải phân biệt nó với `disabled`. Ô mờ đã là ngôn ngữ của `disabled` rồi; dùng lại nó ở đây tạo ra hai nghĩa cho một hình thức. Gạch ngang là hình dạng khác hẳn, nên nó phân biệt được cả với người mù màu — đúng luật [`../Icons.md`](../Icons.md) §7 về hai trạng thái cùng hình dạng khác màu.

🛑 **Nửa chọn không phải một giá trị thứ ba.** Nó là **cách hiển thị** của một ô cha khi các ô con không đồng nhất. Bấm vào một ô đang nửa chọn phải chuyển sang "chọn tất cả", không bao giờ chuyển ngược lại vào nửa chọn — người dùng không nhập được trạng thái đó, chỉ máy suy ra được.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-brand`, `--color-brand-active`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-text-on-brand`, `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-border`, `--color-border-strong`, `--color-danger`, `--color-danger-border`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fw-regular`, `--fw-medium`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-xs`, `--radius-full`, `--border-w`, `--border-w-strong` |
| Kích thước | `--icon-sm`, `--icon-md`, `--size-control-sm`, `--size-control-md` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Nhóm radio ít lựa chọn xếp ngang được, khe `--sp-6` giữa các lựa chọn |
| < `--bp-md` | Mọi nhóm xếp dọc một cột, khe `--sp-5`; nhãn xuống dòng thay vì cắt bằng dấu ba chấm |
| < `--bp-xs` | Cỡ tối thiểu là `md`; vùng bấm của `<label>` kéo hết bề rộng cụm để ngón tay chạm đâu cũng trúng |

Nhãn của ô đánh dấu **được phép xuống dòng**, khác hẳn nhãn nút ở [`Button.md`](./Button.md). Nhãn ở đây thường là một câu điều kiện dài, và cắt nó bằng dấu ba chấm sẽ giấu mất chính thứ người dùng đang đồng ý.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<input type="checkbox">` / `<input type="radio">` thật, dù có tạo hình lại. 🛑 Không `<div role="checkbox">` — mất luôn hành vi nhóm của radio |
| Vai trò ARIA | Vai trò gốc là đủ. Nhóm nhiều ô cùng chủ đề bọc trong `<fieldset>` + `<legend>`, hoặc một phần tử `role="group"` có `aria-labelledby` |
| Nửa chọn | Đặt property `indeterminate` trên phần tử DOM **và** `aria-checked="mixed"`. Property một mình chỉ đổi hình vẽ, trình đọc màn hình vẫn đọc là "chưa chọn" |
| Bàn phím — checkbox | `Space` bật/tắt. `Tab` đi qua **từng** ô, vì mỗi ô độc lập |
| Bàn phím — radio | `Tab` vào **cả nhóm một lần** (nhóm là một điểm dừng); phím mũi tên di chuyển giữa các lựa chọn và chọn luôn cái đi tới. Đây là hành vi gốc của trình duyệt khi các radio dùng chung `name` — đừng chặn nó |
| `name` | Mọi radio trong một nhóm phải dùng **chung một `name`**. Thiếu, chúng thành các ô đánh dấu tròn chọn được nhiều cái — lỗi trông không thấy được |
| Focus | `:focus-visible` trên ô, `outline-offset` ≥ 2px |
| Nhãn | `<label for>` trỏ đúng `id`, hoặc `<label>` bọc input. `id` phải duy nhất trong cả trang |
| Lỗi | Dòng lỗi nối vào nhóm bằng `aria-describedby`; nhóm mang `aria-invalid="true"` |
| Vùng bấm | ≥ 28×28px tính cả `<label>`, nới bằng `padding` (WCAG 2.2 SC 2.5.8) |
| Chữ | Nhãn đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `type` | input | `'checkbox' \| 'radio'` | `'checkbox'` | |
| `checked` | input | `boolean` | `false` | Với `radio`, do nhóm cha quyết định |
| `indeterminate` | input | `boolean` | `false` | Chỉ có nghĩa với `checkbox`. Đặt cùng `checked = true` là mâu thuẫn — component ưu tiên `indeterminate` và ghi cảnh báo lúc phát triển |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `disabled` | input | `boolean` | `false` | |
| `invalid` | input | `boolean` | `false` | Chỉ đổi hình thức và `aria-invalid`; **không** giữ câu chữ lỗi — câu chữ thuộc [`FormRow.md`](./FormRow.md) |
| `name` | input | `string \| null` | `null` | Bắt buộc với `radio` |
| `value` | input | `string \| number \| null` | `null` | Giá trị phát ra khi `radio` được chọn |
| `ariaLabel` | input | `string \| null` | `null` | Chỉ dùng khi không có nhãn nhìn thấy được — ca hiếm, ví dụ ô chọn hàng trong bảng |
| `checkedChange` | output | `boolean` | — | Không phát khi `disabled` |

Nhãn nhìn thấy được vào qua slot mặc định (`<ng-content>`), không qua input chuỗi — nhãn hay chứa một liên kết ("Tôi đồng ý với **điều khoản**") và một input chuỗi sẽ chặn điều đó.

**Bọc nghĩa là giấu hẳn:** API trên **không** để lọt kiểu dữ liệu hay tên sự kiện của PrimeNG ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 2). Tạo hình bằng token và cơ chế theme của thư viện, 🛑 không `::ng-deep`.

`Check` là component **dumb** — không inject service lấy dữ liệu ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Luôn gắn `<label>` với input; cả dòng phải bấm được.
- ✅ Dùng chung một `name` cho mọi radio trong một nhóm.
- ✅ Đặt `aria-checked="mixed"` cùng lúc với property `indeterminate`.
- ✅ Bọc nhóm trong `<fieldset>` + `<legend>` khi các lựa chọn thuộc cùng một câu hỏi.
- ❌ Không dùng radio để đổi chế độ xem — đó là việc của [`SegmentedControl.md`](./SegmentedControl.md).
- ❌ Không cho người dùng nhập vào trạng thái nửa chọn.
- ❌ Không bo tròn ô đánh dấu, không bo vuông nút radio.
- ❌ Không thực thi hành động có tác dụng phụ ngay khi ô đổi giá trị.
- ❌ Không đè style PrimeNG bằng `::ng-deep`.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Cạnh ô đang mượn `--icon-sm`/`--icon-md`, vốn là token **cỡ icon** chứ không phải cỡ ô control. Có khai một token riêng (ví dụ một bậc `--size-check-*`) trong [`../DESIGN.md`](../DESIGN.md) §6.2 không, hay chấp nhận mượn? | Người sở hữu hệ token |
| 2 | Ca lưu-ngay: khi bật một ô là gọi máy chủ luôn, phản hồi chờ hiện ở đâu — trên chính ô, hay bằng `Toast`? Hôm nay spec đóng cửa `loading` trên ô, nhưng chưa có màn thật để kiểm | Dự án đầu tiên có màn cấu hình lưu-ngay |
| 3 | Ô "chọn tất cả" trong `th` có thuộc `Check` hay thuộc [`DataTable.md`](./DataTable.md)? Hình thức thì thuộc đây, nhưng ngữ nghĩa "tất cả" (trang này hay toàn bộ kết quả) thì không | Khi dựng `DataTable` |
