---
kind: luat
scope: core
verified: chua-doi-chieu
---

# AuthField

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`. Bảng dưới đây khai đúng những mục đã mở source ra so.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, đây chỉ là một ô nhập gốc kèm hai phần trang trí ở hai đầu. Toàn bộ hành vi khó ở đây là hành vi **của trình duyệt** (tự điền, quản lý mật khẩu), và bọc thêm một lớp thư viện chỉ làm hành vi đó khó giữ đúng hơn.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/components/auth-field/auth-field.component.ts` — selector `app-auth-field`, class gốc `.auth-field`.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| API | `label` và `autocomplete` đều `input.required`; thêm `variant`, `size` (mặc định `lg`), `placeholder`, `errorMessage`, `hint`, `controlId`, `revealable` | — |
| `ControlValueAccessor` | Có, đủ bốn phương thức | — |
| Dựng thẳng trên `<input>` gốc | Có — **không** lồng `app-input` bên trong. Chú thích component khai đây là đơn giản hoá có ghi nhận để tránh CVA lồng CVA | Spec mô tả `AuthField` như một lớp trên [`Input.md`](./Input.md); code không đi đường đó. Hai bên cần thống nhất lời, không phải sửa code vội |
| Bốn biến thể + icon dẫn | Đủ, đúng bảng §Biến thể: `identifier` → `pi-user`, `password` → `pi-key`, `tenantCode` → `pi-building`, `code` → không icon | — |
| Nút hiện/ẩn ở `password` | Có, `<button>` thật, `aria-label` qua i18n (`xacThuc.matKhau.hien`/`.an`), đổi `pi-eye`/`pi-eye-slash`, có vòng focus **riêng** đúng spec | — |
| `focus()` công khai | Có — `viewChild` tới `<input>`, để trang xác thực đặt focus ban đầu như [`AuthCard.md`](./AuthCard.md) §Trạng thái đòi | — |
| Khuôn id dòng phụ | Có, `<controlId>-error` / `<controlId>-hint`; `describedBy` tự chọn lỗi trước, gợi ý sau | — |
| Lỗi thay chỗ gợi ý, `role="alert"` | Có, cùng khuôn [`FormRow.md`](./FormRow.md) | — |
| `default` | Nền `--color-surface`, viền `--border-w` `--color-border-strong`, `--radius-sm`, icon dẫn `--color-text-muted` — khớp spec | — |
| Cỡ `lg` | Chiều cao `--size-control-lg`, cỡ chữ `--fs-md`, đệm ngang `--sp-5` — khớp spec | — |
| Cỡ `md` | `.auth-field__input--md` đổi chiều cao `--size-control-md` và cỡ chữ `--fs-sm` | Đệm ngang **không** đổi sang `--sp-4` như §Kích thước khai — giữ `--sp-5` của cỡ `lg` |
| `focus-visible` | Vòng `--border-w-strong` `--color-focus` + `outline-offset: 2px`, viền chuyển `--color-brand` | Icon dẫn **không** chuyển `--color-brand` khi ô nhận focus — spec đòi |
| `error` | Viền `--border-w-strong` `--color-danger-border`, `aria-invalid`, icon `pi-times-circle` và dòng lỗi dưới ô — kênh thứ hai là chữ, đúng §2.7 | Điều kiện bật **khác spec**: bật theo chuỗi `errorMessage()` trang truyền vào, không theo `invalid && touched` của control như §Trạng thái khai (đây là khuôn của `FormRow`, không phải khuôn của `Input`) |
| `disabled` | Nền `--color-surface-3`, chữ `--color-text-disabled`, `cursor: not-allowed`; đến từ control qua `setDisabledState` | Nút hiện/ẩn **không** bị `disabled` kèm — spec đòi |
| `loading` giữ nguyên giá trị đã gõ | Đúng — `setDisabledState` chỉ đặt cờ, không đụng `gia`; không có gì xoá ô hay đổi placeholder | — |
| `empty` | Đúng — không có gì tô đỏ ô trước khi trang truyền `errorMessage` | — |
| `hover` | — | **Chưa có.** Không có khối `@media (hover: hover)` nào; spec đòi viền đậm thêm một bậc và con trỏ `text` |
| `active` của nút hiện/ẩn | — | Chưa có quy tắc `:active` nào |
| Biến thể `code` | Chỉ khác ở chỗ không có icon dẫn | **Hình thức riêng chưa có**: spec đòi chữ căn giữa và giãn chữ `--ls-wide`; không có class nào theo `variant() === 'code'` trong SCSS |
| Cỡ nút hiện/ẩn | — | ⚠️ **Giá trị trần trong SCSS**: `.auth-field__nut-hien-an` đặt `width: 28px; height: 28px` thay vì một token. 28px đúng bằng `--size-control-sm`, nên đây là chỗ thay được bằng token mà không đổi hình thức. Trái luật cấm hardcode ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) |

Chưa đối chiếu: §Responsive, phần còn lại của §Accessibility (mới so thẻ `<input>`/`<button>`, `aria-invalid`, `aria-describedby`, `aria-label` của nút hiện/ẩn), và việc `type` có đúng `email`/`tel` cho từng ca hay không — code chỉ sinh `text` hoặc `password`.

---

## Mục đích

Ô nhập dùng riêng cho màn xác thực: có icon dẫn bên trái, và với mật khẩu thì có nút hiện/ẩn bên phải.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Mọi ô nhập bên trong [`AuthCard.md`](./AuthCard.md) | 🛑 Ô nhập trong biểu mẫu nghiệp vụ sau khi đã đăng nhập → [`Input.md`](./Input.md) đặt trong [`FormRow.md`](./FormRow.md) |
| Ô mật khẩu của màn xác thực | 🛑 Ô tìm kiếm trên một danh sách → ô tìm của [`Toolbar.md`](./Toolbar.md) |
| Ô nhập mã xác thực một lần | 🛑 Ô chọn từ danh sách → hợp đồng chung ở [`Input.md`](./Input.md); ô ngày → [`DatePicker.md`](./DatePicker.md); 🛑 bật/tắt một tuỳ chọn → [`Check.md`](./Check.md) |

**Vì sao `AuthField` tách khỏi `Input` thay vì làm một biến thể của nó:** ba thứ chỉ đúng ở màn xác thực và chỉ sai ở mọi chỗ khác — cỡ `lg` mặc định, icon dẫn mặc định có, và `autocomplete` mang giá trị thuộc nhóm xác thực. Nhồi cả ba vào `Input` nghĩa là mọi ô nhập trong ứng dụng phải mang theo ba nhánh điều kiện mà chúng không bao giờ chạy vào. Cái giá của việc tách: hai component cùng phải theo kịp khi hợp đồng ô nhập đổi — nên `AuthField` **dựng trên** `Input`, không khai lại viền, nền hay trạng thái lỗi từ đầu.

## Biến thể

| Biến thể | Icon dẫn | Đuôi phải | Dùng khi |
| --- | --- | --- | --- |
| `identifier` | `pi-user` | không | Tên đăng nhập, mã nhân viên, địa chỉ thư điện tử |
| `password` | `pi-key` | Nút hiện/ẩn | Mật khẩu hiện tại và mật khẩu mới |
| `code` | không | không | Mã xác thực một lần; chữ căn giữa, giãn chữ `--ls-wide`. **Một ô duy nhất**, không tách mỗi ký tự một ô — dãy ô phá dán từ bộ nhớ tạm và phá `autocomplete="one-time-code"` |
| `tenantCode` | `pi-building` | không | Mã đơn vị — ô đứng trước tên đăng nhập ở màn đăng nhập ([`../../contracts/auth.md`](../../contracts/auth.md) §3); `autocomplete="organization"` |

## Kích thước

| Cỡ | Chiều cao | Đệm ngang | Cỡ chữ | Icon | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `lg` | `--size-control-lg` (42px) | `--sp-5` | `--fs-md` | `--icon-md` | **Mặc định.** Mọi ô trong `AuthCard` |
| `md` | `--size-control-md` (34px) | `--sp-4` | `--fs-sm` | `--icon-md` | Chỉ trong `AuthCard` biến thể `wide`, nơi một lưới hai cột có nhiều trường |

**Đệm trái phải chừa chỗ cho icon dẫn.** Icon nằm chồng lên ô nhập chứ không đẩy ô nhập đi, nên chữ người dùng gõ phải bắt đầu sau icon: đệm trái bằng `--sp-5` + `--icon-md` + `--sp-4`. Thiếu phần cộng này thì ký tự đầu tiên chui xuống dưới icon và người dùng thấy chữ mình gõ bị che. Với biến thể `password`, đệm phải cũng phải cộng tương tự để chuỗi mật khẩu dài không chạy xuống dưới nút hiện/ẩn. Ba số cộng lại khai thành token riêng component ở tầng 3 ([`../DESIGN.md`](../DESIGN.md) §1): `--auth-field-pad-icon` = `calc(var(--sp-5) + var(--icon-md) + var(--sp-4))` — không mang giá trị mới, tự đúng khi bậc đổi.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền `--color-border-strong` (bậc "chỗ này gõ được", [`../DESIGN.md`](../DESIGN.md) §2.3), `--radius-sm`; icon dẫn `--color-text-muted` | Có |
| `hover` | Viền đậm thêm một bậc theo cùng thang; con trỏ `text`. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | `outline` `--border-w-strong` màu `--color-focus`, `outline-offset` theo [`../DESIGN.md`](../DESIGN.md) §2.6; icon dẫn chuyển `--color-brand`. Nút hiện/ẩn có vòng focus **riêng**, không dùng chung với ô nhập | Có |
| `active` | **Chỉ áp cho nút hiện/ẩn** khi đang nhấn. Bản thân ô nhập không có trạng thái nhấn — nó có con trỏ nhập, đó mới là phản hồi | Có |
| `disabled` | Nền `--color-surface-3`, chữ `--color-text-disabled`, con trỏ `not-allowed`; nút hiện/ẩn cũng `disabled`. Thuộc tính `disabled` thật, không chỉ đổi màu; đến từ control qua `setDisabledState` (§API dự kiến) | Có |
| `loading` | Khi form đang gửi: ô nhận `disabled` nhưng **giữ nguyên giá trị đã gõ**. 🛑 Không xoá ô, không đổi placeholder — đăng nhập hỏng thì người dùng phải còn tên tài khoản để sửa mật khẩu | Có |
| `error` | Bật khi control `invalid && touched` (§API dự kiến). Viền `--color-danger-border` dày `--border-w-strong`, icon `pi-times-circle` và dòng lỗi dưới ô. Màu **không bao giờ** là kênh duy nhất — [`../DESIGN.md`](../DESIGN.md) §2.7 | Có |
| `empty` | Ô trống là trạng thái khởi đầu bình thường, **không phải lỗi**: không tô đỏ trước khi người dùng rời ô hoặc bấm gửi. Placeholder chỉ gợi định dạng, không lặp lại nhãn | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-3`, `--color-border-strong`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-brand`, `--color-danger`, `--color-danger-border`, `--color-focus` |
| Chữ, khoảng cách | `--fs-sm`, `--fs-md`, `--fs-xs`, `--fw-medium`, `--fw-regular`, `--lh-snug`, `--ls-wide`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng, kích thước, chuyển động | `--radius-sm`, `--border-w`, `--border-w-strong`, `--size-control-md`, `--size-control-lg`, `--icon-md`, `--dur-fast`, `--ease-standard` |

Icon dẫn dùng `--color-text-muted` theo [`../Icons.md`](../Icons.md) §4 — nó là trang trí và phải lùi lại sau chữ người dùng gõ.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Ô trải hết bề rộng khung `AuthCard`; nhãn nằm trên ô, khe `--sp-5` |
| < `$bp-md` | Hình dạng không đổi — `AuthCard` co thì ô co theo. Giữ cỡ `lg` kể cả khi khung thu hẹp: ô nhập nhỏ đi trên màn cảm ứng là đi ngược vùng chạm tối thiểu; chật thì rút chữ của nhãn, đừng rút chiều cao ô |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<input>` thật, `type` đúng: `text` / `password` / `email` / `tel`. Nút hiện/ẩn là `<button>` |
| Nhãn | `<label for>` thật, **nhìn thấy được**. Placeholder không thay được nhãn: nó biến mất khi gõ, và mất nhãn khi ô đã có chữ là mất ngữ cảnh lúc soát lại |
| `autocomplete` | `organization` cho mã đơn vị (`tenantCode`), `username` cho tên đăng nhập, `current-password` cho mật khẩu đăng nhập, `new-password` cho ô đặt mật khẩu mới, `one-time-code` cho mã xác thực |
| Nút hiện/ẩn | `type="button"` **bắt buộc**; `aria-label` đổi theo trạng thái: "Hiện mật khẩu" ↔ "Ẩn mật khẩu"; icon bên trong mang `aria-hidden="true"` — [`../Icons.md`](../Icons.md) §7. Hợp đồng nút ở bảng này dùng chung cho [`Input.md`](./Input.md) biến thể `password` |
| Bàn phím | Thứ tự Tab: ô nhập → nút hiện/ẩn → trường kế tiếp. Nút phải Tab tới được; bỏ nó khỏi thứ tự Tab là lấy tính năng này khỏi người không dùng chuột. `Enter` gửi form của [`AuthCard.md`](./AuthCard.md), kể cả khi con trỏ đang ở ô cuối |
| Gợi ý | `id` là `<controlId>-hint`; ô trỏ tới qua `aria-describedby` — cùng khuôn [`FormRow.md`](./FormRow.md). Dòng lỗi **thay chỗ** dòng gợi ý theo quyết định 1 của file đó |
| Lỗi | `aria-invalid="true"` suy từ control: `invalid && touched` (§API dự kiến). `aria-describedby` trỏ tới `id` của dòng lỗi (`<controlId>-error`) khi `errorMessage` khác `null` — cùng nguồn control nên hai dấu hiệu bật cùng lúc |
| Focus, vùng bấm | Vòng focus của ô và của nút tách rời nhau, mỗi cái đạt ≥ 3:1; nút hiện/ẩn ≥ 28×28px, nới bằng `padding` |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

🛑 **Bẫy: thiếu `autocomplete` làm trình quản lý mật khẩu không điền được.** Trình duyệt và trình quản lý mật khẩu nhận diện trường bằng chính thuộc tính này, không đoán từ tên biến hay từ nhãn tiếng Việt. Thiếu nó thì người dùng phải gõ tay mật khẩu ở mọi lần đăng nhập — và hệ quả thật là họ chọn mật khẩu ngắn hơn. Nặng hơn nữa: đặt `current-password` cho ô đặt mật khẩu mới sẽ khiến trình quản lý điền đè mật khẩu **cũ** vào, người dùng bấm gửi mà không đọc, rồi mật khẩu không đổi được và không ai hiểu vì sao.

🛑 **Bẫy: nút hiện/ẩn thiếu `type="button"`.** Mặc định của `<button>` trong một `<form>` là `submit` — nút hiện mật khẩu sẽ gửi luôn form đăng nhập với mật khẩu gõ dở. Cùng bẫy đã ghi ở [`Button.md`](./Button.md).

**Vì sao `aria-label` đổi theo trạng thái chứ không dùng `aria-pressed`:** với `aria-pressed`, trình đọc màn hình đọc "nút, không nhấn" — người dùng phải tự suy ra "không nhấn" nghĩa là mật khẩu đang ẩn hay đang hiện. Một nhãn nói thẳng hành động sẽ xảy ra khi bấm thì không cần suy luận nào. Cái giá: nhãn đổi trong lúc focus đang ở trên nút, và vài trình đọc màn hình đọc lại nhãn mới — chấp nhận được, vì đó chính là thông tin người dùng cần nghe.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'identifier' \| 'password' \| 'code' \| 'tenantCode'` | `'identifier'` | |
| `size` | input | `'md' \| 'lg'` | `'lg'` | Ngược với mặc định chung của Core, có chủ đích: mọi ô ở màn xác thực là `lg` |
| `label` | input | `string` | — | Bắt buộc. Không có nhãn là lỗi thi công |
| `placeholder` | input | `string \| null` | `null` | Chỉ gợi định dạng, không lặp lại nhãn |
| `autocomplete` | input | `string` | — | Bắt buộc khai tường minh; không suy ra từ `variant`, vì `password` có hai giá trị hợp lệ khác nhau |
| `errorMessage` | input | `string \| null` | `null` | Câu đã dịch, trang lấy từ `fieldErrorText` ([`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.5). `null` thì dòng lỗi không tồn tại trong DOM |
| `hint` | input | `string \| null` | `null` | Gợi ý tĩnh dưới ô (chính sách mật khẩu, định dạng mã); `id` là `<controlId>-hint`, nối vào `aria-describedby` |
| `controlId` | input | `string` | tự sinh | Gốc của `<controlId>-hint` và `<controlId>-error`, cùng khuôn [`FormRow.md`](./FormRow.md) |
| `revealable` | input | `boolean` | `true` | Chỉ có nghĩa với `variant = 'password'` |
| `revealToggled` | output | `boolean` | — | Báo trạng thái hiện/ẩn mới, nơi gọi không bắt buộc phải nghe |

**Form control chuẩn Angular.** `AuthField` cài `ControlValueAccessor`: nhận `[formControl]` / `formControlName`; giá trị, `touched` (lúc rời ô) và vô hiệu hoá đi qua control, không qua input/output riêng. Không có API cho ca không gắn control: mọi ô của `AuthField` nằm trong `<form>` của [`AuthCard.md`](./AuthCard.md), và form chỉ dùng reactive form ([`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §6.1). Trạng thái `error` — viền, icon, `aria-invalid` — suy từ control: `invalid && touched`. Câu lỗi thì component **không** suy: trang gọi `fieldErrorText` (§6.5 cùng file) rồi truyền `errorMessage`; hai thứ trùng nhau vì cùng đọc một control.

Trạng thái hiện/ẩn do chính `AuthField` giữ — nó là trạng thái **trình bày thuần tuý**, không rời khỏi component, nên giữ nó bên trong vẫn không phá ranh giới dumb ở [`../COMPONENTS.md`](../COMPONENTS.md) §5. Cái không được giữ bên trong là giá trị đã nhập và kết quả kiểm tra hợp lệ: hai thứ đó thuộc về form ở trang cha.

## Do / Don't

- ✅ Luôn khai `autocomplete` tường minh, đúng giá trị cho từng ô.
- ✅ Gắn qua `[formControl]` / `formControlName` — không có đường truyền giá trị nào khác.
- ✅ Nhãn nhìn thấy được, đặt trên ô; `type="button"` cho nút hiện/ẩn.
- ✅ Giữ nguyên giá trị đã gõ khi form đang gửi.
- ✅ Chỉ báo lỗi sau khi người dùng rời ô hoặc bấm gửi.
- ❌ Không dùng placeholder thay nhãn.
- ❌ Không đặt `current-password` cho ô mật khẩu mới.
- ❌ Không bỏ nút hiện/ẩn khỏi thứ tự Tab.
- ❌ Không tô đỏ ô ngay khi màn vừa mở, và không thu nhỏ chiều cao ô ở màn cảm ứng.

## Cần chốt

Không còn.
