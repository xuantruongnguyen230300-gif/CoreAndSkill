---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Check

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`; biến thể `checkbox` đã qua màn thật, biến thể `radio` mới khai chữ ký. Bảng dưới đây khai đúng những mục đã mở source ra so.

**Nền:** bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — dòng "ngữ nghĩa control biểu mẫu gốc + trạng thái nửa chọn": `indeterminate` **không phải một thuộc tính HTML**, nó chỉ đặt được bằng property trên phần tử DOM, nên một template thuần không khai được nó.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/ui/check/check.component.ts` — selector `app-check`, class gốc `.check`. Bọc `p-checkbox` / `p-radioButton` đúng như §Nền chốt.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| API | Đủ chữ ký §API dự kiến: `type`, `checked`, `indeterminate`, `size`, `disabled`, `describedBy`, `name`, `value`, `ariaLabel`, output `checkedChange`; thêm `inputId` (tự sinh) mà spec chưa khai | §API dự kiến chưa có dòng `inputId` — thiếu sót của spec, không phải của code |
| Ba thuộc tính trợ năng (`aria-describedby`, `aria-label`, `aria-checked`) tới đúng `<input>` (đối chiếu 2026-09-23) | Có, cả ba đi **chung một đường**: pass-through `pt` của thư viện, cùng khuôn `ptBang` của [`DataTable.md`](./DataTable.md) — `check.component.ts` § `protected readonly ptO = computed(`, `check.component.html` § `[pt]="ptO()"`, ăn vào § `[pBind]="ptm('input')"` trong template thư viện. 🛑 **Không** đặt bằng `[attr.…]` trên `<p-checkbox>`/`<p-radioButton>`: thuộc tính đó rơi lên HOST của component thư viện chứ không xuống `<input>`, mà trình đọc màn hình đọc theo phần tử đang **focus** — và host `<p-checkbox>` còn không mang role nào, nên thuộc tính nằm đó bị bỏ qua hẳn trong khi DOM vẫn "trông đúng". Khoá bằng test: `check.component.spec.ts` § `KHÔNG nằm trên host`, mỗi ca khẳng định **cả hai vế** (có trên `<input>` và không có trên host) — bỏ vế thứ hai thì một `[attr.…]` đặt nhầm chỗ vẫn làm test xanh | — |
| `aria-checked` của `radio` là của thư viện, `pt` không đụng vào (đối chiếu 2026-09-23) | `ptO` **cố ý bỏ hẳn** khoá `aria-checked` ở nhánh `radio` — neo: `check.component.ts` § `const laRadio = this.type() === 'radio';`. Lý do đọc thẳng từ gói đang cài: template `p-radioButton` đã có `[attr.aria-checked]="checked"` riêng trên `<input>`, còn template `p-checkbox` thì **không** có binding nào tên đó, nên chỉ nhánh `checkbox` phải tự mang `"mixed"` tới. Hai nguồn cùng ghi một thuộc tính là lỗi im lặng: thư viện ghi bằng binding của template, `pBind` ghi bằng `renderer.setAttribute` trong một `effect` — hai nhịp khác nhau | — |
| `ControlValueAccessor` | Có, đủ bốn phương thức; hai chế độ (gắn control / không gắn control) tách rõ qua `ngControl` | — |
| `radio` ghi `value` vào control chung | Có — `chonRadio()` ghi `value()`; `daChon` so `this.giaTriCVA() === this.value()` đúng luật spec khai ở dòng `value` | **Chưa qua màn thật nào.** Chú thích trong chính component khai điều đó — chữ ký đủ nhưng chưa được thực tế kiểm |
| `error` suy từ control | Có, getter `loi` = `invalid && touched`, cùng khuôn [`Input.md`](./Input.md); truyền xuống PrimeNG qua `[invalid]` | — |
| `indeterminate` | Có, truyền vào `[indeterminate]` **và** đặt `aria-checked="mixed"` lên chính `<input>` qua `ptO` — đúng dòng "Nửa chọn" của §Accessibility. Hết nửa chọn thì khoá `aria-checked` **vẫn còn mặt** trong `ptO` với giá trị `null` để `pBind` gỡ thuộc tính đi; bỏ hẳn khoá ra sẽ để `"mixed"` cũ bám lại trên `<input>`, vì `pBind` chỉ gỡ cho những khoá có trong đối tượng | — |
| Giá trị và trạng thái khoá từ control vẽ lại được dưới `OnPush` (đối chiếu 2026-09-23) | Có. `check.component.ts` § `protected readonly giaTriCVA = signal<unknown>(false);` và § `private readonly disabledCVA = signal(false);` — cùng khuôn `input.component.ts` § `protected readonly gia = signal('')`. `writeValue` và `setDisabledState` do `@angular/forms` gọi thẳng, **ngoài** mọi binding, nên chúng phải ghi vào `signal` thì lượt đọc trong template (getter `daChon` / `voHieuHoa`) mới đánh dấu view bẩn và lên lịch vẽ lại. Khoá bằng test: `check.component.spec.ts` § `vẽ lại được sau lượt dựng đầu` — mỗi ca dựng xong lượt đầu **trước** rồi mới đổi control, vì đổi trước lượt dựng đầu thì chính lượt đó đọc giá trị mới nên ca xanh kể cả khi lỗi còn nguyên | — |
| `checkedChange` không phát khi `disabled` | `p-checkbox` nhận `[disabled]` nên không phát sự kiện — chặn gián tiếp qua thư viện | Không có chốt chặn tường minh trong `doiGiaTriCheckbox()` như `Button` làm; `chonRadio()` thì **có** (`if (this.voHieuHoa) return`). Hai nhánh không cùng một mức chắc chắn |
| Nhãn bên phải, khe `--sp-4` | Có (`.check` là `inline-flex`, `gap: var(--sp-4)`, `<span class="check__nhan">` đứng sau ô) | — |
| Vùng bấm nới bằng `padding` của `<label>` | Có — `padding: var(--sp-2)` trên `.check`, bù lại bố cục bằng `margin` âm; đúng luật spec (không nới bằng `margin`) | **Chưa đạt số spec khai**: không có `min-height` theo `--size-control-sm`/`--size-control-md`, nên vùng bấm tối thiểu 28px/34px chưa được bảo đảm |
| Cỡ `sm`/`md` | `.check--sm` đổi cỡ chữ nhãn (`--fs-xs`, nghỉ là `--fs-sm`) | **Cạnh ô chưa đổi theo cỡ.** Spec đòi `--icon-sm` (sm) và `--icon-md` (md); không có quy tắc nào trong SCSS của component đặt kích thước ô — kích thước đến từ preset PrimeNG, và preset không phân biệt theo cỡ của `Check` |
| `disabled` | `.check--disabled` đặt `cursor: not-allowed` và nhãn `--color-text-disabled` | Nền `--color-surface-3` và viền `--color-border` **của chính cái ô** không do component đặt — thuộc preset |
| Nhãn được phép xuống dòng | Không có `white-space: nowrap` nào chặn — nhãn xuống dòng tự nhiên | — |
| §Token dùng — `--dur-fast` (đối chiếu 2026-09-23, chỉ đọc mã) | Chuyển tiếp **có chạy**, nhưng không lấy số từ token. CSS của thư viện khai `@primeuix/styles/checkbox` § `transition-duration: dt('checkbox.transition.duration')`, giải ra biến `--p-checkbox-transition-duration`; preset Aura nối biến đó về `{form.field.transition.duration}` rồi về primitive `{transition.duration}`, và primitive đó là `0.2s`. Thời lượng thật hôm nay: **200ms** | Spec đòi `--dur-fast` = `120ms`. ✅ **Token tới được chỗ này** — đây là một biến CSS, Core chỉ cần khai `--p-checkbox-transition-duration` (và bản `radiobutton` tương ứng) trỏ về token. Chưa nơi nào trong `src/FE/src/styles` khai nó. Việc của `frontend-expert`, không cần đổi spec |

Chưa đối chiếu — và đây là **lỗ mù lớn nhất của file này**: toàn bộ §Trạng thái ở mức hình thức của *chính cái ô* (`default` chưa chọn/đã chọn/nửa chọn, `hover`, `focus-visible`, `active`, viền `error`) cùng hai hình dạng `--radius-xs`/`--radius-full` **không nằm trong SCSS của component**. Chúng đến từ sub-preset `checkbox`/`radiobutton` của Aura ghép ở `src/FE/src/app/core/theme/prime-preset.ts` (khoá `checkbox`, `radiobutton` trong `components`). Tôi mới xác nhận hai sub-preset đó **được ghép vào**, chưa soát từng giá trị token mà chúng sinh ra — nên chưa kết luận được là khớp hay lệch spec. Cũng chưa đối chiếu §Responsive và cảnh báo lúc phát triển khi `indeterminate` và giá trị `true` mâu thuẫn (spec đòi, không thấy trong code).

---

## Mục đích

Bật/tắt một tuỳ chọn (ô đánh dấu) hoặc chọn đúng một trong nhiều tuỳ chọn (radio), có cả trạng thái nửa chọn.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Bật/tắt một tuỳ chọn độc lập — "Gửi email thông báo" | 🛑 Chuyển giữa vài **chế độ xem** loại trừ nhau → [`SegmentedControl.md`](./SegmentedControl.md); radio là để chọn **giá trị**, không phải để đổi cái đang nhìn |
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

**Không có cỡ `lg`, có chủ đích.** Ba cỡ ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.2 là ba **chiều cao control**; một ô đánh dấu 42px đứng cạnh nhãn cỡ `--fs-md` trông như một ô nhập bị hỏng chứ không như một tuỳ chọn. Cạnh ô mượn `--icon-sm`/`--icon-md` là quyết định, không khai bậc `--size-check-*` riêng — ô đứng cạnh icon cùng hàng và phải bằng nó.

Ô "chọn tất cả" trong `th` **thuộc [`DataTable.md`](./DataTable.md)**: là phần cấu trúc của lớp bọc, thư viện vẽ, mượn hình thức của `Check` ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5); phạm vi "tất cả" khai ở file đó. Cái giá: trong một form mà các control khác dùng cỡ `lg`, hàng ô đánh dấu vẫn ở `md` nên thấp hơn ô nhập phía trên — chấp nhận được vì nó vốn là một dòng phụ.

Vùng bấm nới bằng `padding` của `<label>`, không bằng `margin` — `margin` không nhận sự kiện chuột ([`../COMPONENTS.md`](../COMPONENTS.md) §2.2).

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Chưa chọn: nền `--color-surface`, viền `--color-border-strong` (`--border-w`). Đã chọn: nền `--color-brand`, dấu tick `--color-text-on-brand`. Nửa chọn: nền `--color-brand`, một gạch ngang `--color-text-on-brand` | Có |
| `hover` | Viền đậm thêm; nền `<label>` chuyển `--color-surface-2` để lộ ra rằng cả dòng bấm được. `--dur-fast` với `--ease-standard`. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px` **trên chính cái ô**, không trên cả dòng. Vòng focus ôm cả dòng làm người dùng bàn phím mất dấu ô nào đang được chọn trong một cột dài | Có |
| `active` | Viền `--color-brand-active`; không dịch chuyển hình — một ô nhỏ cỡ `--icon-md` mà nhích 1px thì trông như lỗi vẽ chứ không như phản hồi | Có |
| `disabled` | Nền `--color-surface-3`; viền `--color-border`; nhãn `--color-text-disabled`; `cursor: not-allowed`. Thuộc tính `disabled` thật trên input | Có |
| `loading` | **Không áp dụng cho chính ô.** Ô đánh dấu đổi giá trị tức thì; nếu việc lưu cần thời gian thì trạng thái chờ thuộc về nút Lưu ở [`Button.md`](./Button.md) hoặc về cả cụm form. Ca lưu-ngay (autosave) xem `Cần chốt` #1 | — |
| `error` | Bật khi control `invalid && touched` (§API dự kiến), cùng khuôn `error` của [`Input.md`](./Input.md). Viền `--color-danger-border`; dòng lỗi chữ `--color-danger` đặt dưới **cả nhóm**, không dưới từng ô. Kênh thứ hai là chữ, viền đỏ một mình không đủ — [`../DESIGN.md`](../DESIGN.md) §2.7 | Có |
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
| ≥ `$bp-md` | Nhóm radio ít lựa chọn xếp ngang được, khe `--sp-6` giữa các lựa chọn |
| < `$bp-md` | Mọi nhóm xếp dọc một cột, khe `--sp-5`; nhãn xuống dòng thay vì cắt bằng dấu ba chấm |
| < `$bp-xs` | Cỡ tối thiểu là `md`; vùng bấm của `<label>` kéo hết bề rộng cụm để ngón tay chạm đâu cũng trúng |

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
| Lỗi | `aria-invalid="true"` trên `<input>` suy từ control: `invalid && touched` (§API dự kiến) — radio cùng nhóm gắn cùng một control nên bật cùng lúc; `aria-describedby` = `describedBy` trang truyền, trỏ tới id của dòng lỗi do `FormRow` vẽ dưới cả nhóm — cùng khuôn [`Input.md`](./Input.md) §Accessibility |
| Vùng bấm | ≥ 28×28px tính cả `<label>`, nới bằng `padding` (WCAG 2.2 SC 2.5.8) |
| Chữ | Nhãn đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `type` | input | `'checkbox' \| 'radio'` | `'checkbox'` | |
| `checked` | input | `boolean` | `false` | Chỉ khi ô **không** gắn control — ô chọn hàng trong [`Table.md`](./Table.md); gắn control thì giá trị đến qua `writeValue` |
| `indeterminate` | input | `boolean` | `false` | Chỉ có nghĩa với `checkbox`. Không đi qua control — là cách hiển thị do máy suy (§Trạng thái). Đặt cùng giá trị `true` là mâu thuẫn — component ưu tiên `indeterminate` và ghi cảnh báo lúc phát triển |
| `size` | input | `'sm' \| 'md'` | `'md'` | |
| `disabled` | input | `boolean` | `false` | Chỉ khi ô **không** gắn control; gắn control thì vô hiệu hoá đến qua `setDisabledState` |
| `describedBy` | input | `string \| null` | `null` | Id của dòng lỗi hoặc dòng gợi ý mà `FormRow` vẽ (`<controlId>-error` / `<controlId>-hint`), trang truyền tường minh — cùng luật với [`Input.md`](./Input.md) §API. Gắn thành `aria-describedby` trên `<input>`. **Nhận được danh sách nhiều `id` cách nhau bằng dấu cách** — ô bọc trong một `Tooltip` thì `id` lời chú vào đây, đứng **sau** `id` lỗi và gợi ý; luật đầy đủ ở [`Tooltip.md`](./Tooltip.md) §Accessibility "Phần tử neo là một component Core", không chép lại |
| `name` | input | `string \| null` | `null` | Bắt buộc với `radio` |
| `value` | input | `string \| number \| null` | `null` | Giá trị của lựa chọn `radio`: ghi vào control khi được chọn; `writeValue` so giá trị control với nó để đặt `checked` |
| `ariaLabel` | input | `string \| null` | `null` | Chỉ dùng khi không có nhãn nhìn thấy được — ca hiếm, ví dụ ô chọn hàng trong bảng |
| `checkedChange` | output | `boolean` | — | Chỉ khi ô **không** gắn control — ô chọn hàng trong [`Table.md`](./Table.md). Không phát khi `disabled` |

**Form control chuẩn Angular.** `Check` cài `ControlValueAccessor` cùng khuôn [`Input.md`](./Input.md) §API: nhận `[formControl]` / `formControlName`; giá trị, `touched` (lúc rời ô) và vô hiệu hoá đi qua control. `checkbox` mang giá trị `boolean`. `radio`: mọi lựa chọn trong một nhóm gắn **cùng một** control và cùng `name` — mỗi lựa chọn là một accessor của control đó, cùng cơ chế với radio gốc của Angular. Trạng thái `error` — viền, `aria-invalid` — suy từ control: `invalid && touched`; câu lỗi do trang lấy từ `fieldErrorText` ([`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.5) và truyền cho `error` của `FormRow` biến thể `group`. Ca không gắn control chỉ còn ô chọn hàng và ô "chọn tất cả" trong [`Table.md`](./Table.md): `checked`, `disabled`, `checkedChange` dành cho ca đó.

Nhãn nhìn thấy được vào qua slot mặc định (`<ng-content>`), không qua input chuỗi — nhãn hay chứa một liên kết ("Tôi đồng ý với **điều khoản**") và một input chuỗi sẽ chặn điều đó.

**Bọc nghĩa là giấu hẳn:** API trên **không** để lọt kiểu dữ liệu hay tên sự kiện của PrimeNG ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 2). Tạo hình bằng token và cơ chế theme của thư viện, 🛑 không `::ng-deep`.

`Check` là component **dumb** — không inject service lấy dữ liệu ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Luôn gắn `<label>` với input; cả dòng phải bấm được.
- ✅ Trong form, gắn qua `[formControl]` / `formControlName`; `checked` / `checkedChange` chỉ cho ô chọn hàng trong `Table`.
- ✅ Dùng chung một `name` và một control cho mọi radio trong một nhóm.
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
| 1 | Ca lưu-ngay: khi bật một ô là gọi máy chủ luôn, phản hồi chờ hiện ở đâu — trên chính ô, hay bằng `Toast`? Hôm nay spec đóng cửa `loading` trên ô, nhưng chưa có màn thật để kiểm | Sau F3 — dự án hạ nguồn đầu tiên có màn cấu hình lưu-ngay |
