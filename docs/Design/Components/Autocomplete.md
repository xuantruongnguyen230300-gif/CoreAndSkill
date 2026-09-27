---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Autocomplete

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`, bọc `primeng/autocomplete`. Bảng dưới đây khai đúng những mục đã mở source ra so.

**Nền:** **bọc PrimeNG**. Định vị lớp nổi khi cuộn hoặc tràn viewport thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4. Phần Core tự viết là luật **khi nào gọi máy chủ** và **kết quả cũ về sau thì xử lý ra sao** — hai thứ thư viện không quyết hộ được.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/ui/autocomplete/autocomplete.component.ts` — selector `app-autocomplete`, chữ ký lựa chọn `Option`, `inputStyleClass` gắn `.app-autocomplete__o--<cỡ>`. Phần chạm DOM do PrimeNG dựng nằm ở `src/FE/src/styles/_thu-vien.scss` (khối `.app-autocomplete__o`).

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| Nơi duy nhất import `primeng/autocomplete` | Đúng — chú thích đầu file khai vai trò này | Chưa tự kiểm bằng lệnh là không còn chỗ nào khác import |
| API | `options`, `value`, `variant`, `size`, `minChars`, `debounceMs`, `loading`, `errorTemplate`, `disabled`, `placeholder` (`input.required`), `inputId`, `describedBy`; output `search`, `valueChange` | — |
| Hai biến thể | Có, `[multiple]` ánh theo `variant()`; `onModelChange` trả mảng khoá ở `multiple`, khoá đơn ở `single` | — |
| Ba cỡ | Có ở `_thu-vien.scss`, đúng §Kích thước: `--size-control-sm/-md/-lg` kèm `--fs-xs`/`--fs-sm`/`--fs-md` | — |
| `minChars` / `debounceMs` | Map thẳng sang `minQueryLength` / `delay` của PrimeNG; component **không** tự debounce lần hai | — |
| Component dumb, không tự gọi HTTP | Đúng — chỉ phát `search`, trang cha gọi API | — |
| Luật "bỏ kết quả request cũ" | Đúng chỗ: component **cố ý không làm**, vì nó không sở hữu lời gọi; chú thích đầu file nói rõ trang cha giữ số thứ tự request | Chưa mở trang gọi nào ra xác nhận trang **thật sự có** làm việc đó — luật này hiện chưa có gì cưỡng chế |
| Giữ nhãn cho khoá đã chọn | Có, cache `nhanDaBiet` gom dần nhãn từ mọi `options()` từng thấy, nên đổi trang kết quả không làm mất nhãn mục đã chọn | Spec **không** mô tả cơ chế này; đây là giải pháp cho một vấn đề thật mà spec chưa ghi. Nên bổ sung vào spec |
| Dòng phụ phân biệt mục trùng tên | Có, `Option.hint` vẽ thành `.app-autocomplete__muc-goi-y` (`--fs-xs`, `--color-text-muted`) dưới nhãn | — |
| `forceSelection` | Bật — người dùng không bỏ lại chuỗi tự do không khớp mục nào | Spec không khai công tắc này; hành vi hợp lý nhưng chưa được chốt ở spec |
| `allowCreate` / `createRequested` | — | **Chưa dựng.** Không có input lẫn output nào; chú thích component khai không màn nào cần. Kéo theo ca `empty` thứ hai của spec (dòng "Tạo mới '…'") cũng không có |
| `loading` | Input có, giữ đúng chữ ký §API dự kiến | ⚠️ **Không nối vào gì cả.** Chú thích trong chính code khai: bản `p-autoComplete` đang dùng không có input `loading` công khai, nên cờ này **không** sinh vòng quay `pi-spinner` ở đuôi ô và **không** đặt `aria-busy` trên hộp kết quả như §Trạng thái đòi. Trang chỉ dùng được nó để khoá nút gửi |
| `errorTemplate` | Có, nhưng vẽ thành một khối **dưới ô** (`.app-autocomplete__loi`, `--color-danger`) | Spec đòi lỗi gọi máy chủ hiện **trong lớp nổi**, kèm nút "Thử lại". Vị trí và hình dạng đều khác; không có nút thử lại |
| `empty` — ba ca | — | **Chưa phân biệt ca nào.** Không có câu "Gõ ít nhất N ký tự", không có "Không tìm thấy '…'", không có câu riêng cho danh mục rỗng hoàn toàn. `minQueryLength` chặn việc gọi tìm, nhưng không sinh ra câu giải thích nào |
| Tô đậm phần chuỗi khớp | — | Chưa có. §Trạng thái `default` đòi phần khớp tô `--color-brand` + `--fw-semibold`; `<ng-template #item>` vẽ nhãn trần |
| `describedBy` | Truyền vào `[ariaLabelledBy]` | ⚠️ **Nối sai thuộc tính.** `describedBy` mang nghĩa `aria-describedby` (id dòng lỗi/gợi ý, cùng khuôn [`Input.md`](./Input.md)); gắn nó vào `aria-labelledby` biến dòng lỗi thành **tên** của ô thay vì phần mô tả kèm theo |
| Chuyển động — mở/đóng lớp nổi và đổi màu (phát hiện 2026-09-23, chỉ đọc mã) | Lớp nổi bung ra và thu lại bằng hoạt ảnh Angular, mặc định thư viện: `primeng/autocomplete` § `showTransitionOptions = '.12s cubic-bezier(0, 0, 0.2, 1)'` và § `hideTransitionOptions = '.1s linear'`; lớp bọc Core không truyền gì vào hai input đó | Spec nay khai đủ khoản này và **giữ quyền đặt số**: `--dur-base` cho cả hai chiều, `--ease-decelerate` chiều hiện, `--ease-accelerate` chiều ẩn (§Trạng thái). Việc của lớp bọc là truyền vào hai input trên hai chuỗi dựng từ **hằng số có tên**, mỗi hằng nêu đích danh token nó phản chiếu — luật **F37** ([`../../RULES.md`](../../RULES.md) §7); khuôn ở lớp bọc [`Toast.md`](./Toast.md). Sau lượt dựng đó hai mặc định ghi ở cột trái không còn chỗ nào trong mã — [`../../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](../../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) quyết định 1, 2, 3 |

Chưa đối chiếu — **lỗ mù chính của file này**: hình thức của ô và của lớp nổi ở mọi trạng thái (`default` nền/viền, `hover` dòng kết quả, `focus-visible` trên ô và dòng đang trỏ tới, `active`, `disabled`, và hình thức chip ở biến thể `multiple`) **không nằm trong** hai tệp style đã mở — chúng đến từ sub-preset `autocomplete` của PrimeNG, chưa soát. Cũng chưa đối chiếu §Responsive, §Accessibility, và việc dòng đang trỏ tới có **được cuộn vào tầm nhìn** hay không (phải chạy thật mới biết).

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

**Đổi màu:** viền ô lúc nhận focus, nền dòng kết quả lúc hover, nền dòng đang được bàn phím trỏ tới — cả ba đổi trong `--dur-fast` với `--ease-standard`. Nhịp này phải ngắn hơn nhịp gõ phím: người dùng đang rà `↑` `↓` qua danh sách sẽ thấy nền "đuổi theo" nếu nó dài hơn khoảng cách giữa hai lần bấm.

**Mở và đóng lớp nổi:** danh sách kết quả mờ dần lúc hiện và lúc ẩn, **một** thời lượng `--dur-base` cho cả hai chiều; đường cong chia theo hướng, `--ease-decelerate` lúc hiện và `--ease-accelerate` lúc ẩn. 🛑 Lớp nổi **không** mang chuyển động mỗi lần danh sách kết quả đổi nội dung — người dùng gõ tiếp một ký tự thì nội dung thay ngay, không mờ vào mờ ra. Lý do ở [`../DESIGN.md`](../DESIGN.md) §7, dòng "nội dung bảng đổi sau khi lọc": chớp một cái ở mỗi phím làm mắt mất chỗ đang đọc, và ở đây mỗi phím là một lần đổi. Tắt cả hai khi `prefers-reduced-motion: reduce`.

**Vì sao bậc này.** [`../DESIGN.md`](../DESIGN.md) §7 chia bậc theo thứ đang đổi: bậc ngắn nhất dành cho sơn màu, còn `transform` và `opacity` của một lớp nổi thuộc `--dur-base`. Lớp nổi kết quả nằm ở vế sau, nên nó lấy `--dur-base` kể cả khi một con số ngắn hơn đang sẵn trong nền bọc — chọn theo con số đang trùng là lấy một sự trùng hợp làm căn cứ ([`../../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](../../adr/0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) quyết định 2). Hệ quả người dùng thấy: lớp nổi hiện và ẩn chậm hơn nhịp mặc định, một cái giá đã được duyệt. Nhịp mới nặng hay vừa tay là câu hỏi để ngỏ ở [`../DESIGN.md`](../DESIGN.md) §10.

**Bất đối xứng thời lượng của nền bọc không được mang theo.** Hai input rời nhau chở được hai số, nhưng thang §7 không có bậc nào trùng giá trị mặc định thứ hai, và một mặc định của gói không phải một ý đồ của hệ này. Thứ giữ lại bất đối xứng ở đây là **đường cong**, vì §7 khai riêng một vai cho phần tử đi vào và một vai cho phần tử rời đi.

🛑 **Hai đoạn chuyển động ở trên đều lấy số từ token, nhưng áp ở hai chỗ khác nhau.** Đổi màu chạy bằng CSS: sub-preset `autocomplete` trỏ thời lượng của nó về khoá semantic dùng chung mà preset Core nối về `--dur-fast`, nên component thừa hưởng không cần khai gì riêng ([`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) quyết định 1). Mở/đóng lớp nổi chạy bằng hoạt ảnh Angular, và chuỗi tham số bị phân tích thành số trước khi có ai giải `var()` — ba giá trị của khoản đó đi qua **hằng số có tên** khai cạnh lớp bọc, mỗi hằng nêu đích danh token nó phản chiếu (luật **F37**, [`../../RULES.md`](../../RULES.md) §7). Khác nhau ở chỗ áp, không ở ai quyết.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-strong`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-danger`, `--color-focus`, `--color-danger-border` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-medium`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-sm`, `--radius-md`, `--radius-pill`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md`, `--layout-popover-h-max` |
| Bóng, lớp | `--shadow-3`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--ease-standard` phủ đoạn "Đổi màu" của §Trạng thái; `--dur-base`, `--ease-decelerate`, `--ease-accelerate` phủ đoạn "Mở và đóng lớp nổi". Ba token sau áp trong TypeScript chứ không trong stylesheet — xem khối 🛑 ở §Trạng thái |
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
