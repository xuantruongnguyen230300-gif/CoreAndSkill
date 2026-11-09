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
| Chuyển động — mở/đóng lớp nổi (đọc mã 2026-11-07, PrimeNG 21.1.10) | **Có trong mã, theo bộ chung của mọi lớp nổi neo** ([`../DESIGN.md`](../DESIGN.md) §7 *Lớp nổi neo*). `p-autoComplete` chuyển lớp nổi qua `p-overlay` của thư viện (gói `primeng` đã cài, tệp `primeng-autocomplete.mjs`, chuỗi `<p-overlay`), và lớp nổi đó chạy lớp pha chung (tệp `primeng-overlay.mjs`, chuỗi `name="p-anchored-overlay"`); **một** khối đè ở `src/FE/src/styles/_thu-vien.scss`: § `body .p-anchored-overlay-enter-active` mang `var(--dur-base)` + `var(--ease-decelerate)`, § `body .p-anchored-overlay-leave-active` mang `var(--dur-base)` + `var(--ease-accelerate)`; keyframes của thư viện giữ nguyên. Lớp bọc không mang mã chuyển động nào: `autocomplete.component.ts`, `autocomplete.component.html` không chứa `transition`, `duration`, `motion` | **Không ca kiểm nào đi qua `Autocomplete`:** khối đè chung chỉ có ca ở `menu.component.spec.ts` (`pha bung: --dur-base, --ease-decelerate`, `pha thu: --dur-base, --ease-accelerate`), và việc `Autocomplete` nhận khối đó suy từ tên lớp pha, chưa đo. Nhánh giảm chuyển động của khối đè chung không có ca token nào canh; việc bảng gợi ý còn mở dưới cờ đó do dòng ngay dưới canh. Nhịp trên trình duyệt **chưa đo** |
| Lớp nổi gắn vào `body` (đọc mã và ca kiểm, 2026-11-07, PrimeNG 21.1.10) | **Có trong mã.** `autocomplete.component.html` mở bằng `<p-autoComplete` mang `appendTo="body"`. Vì sao cần: giá trị mặc định của thư viện là `'self'` (gói `primeng` đã cài, tệp `primeng-config.mjs`, chuỗi `overlayAppendTo = signal('self'`; `primeng-autocomplete.mjs` chuyển `$appendTo()` vào `p-overlay`), và app không đặt `overlayAppendTo` ở đâu. Bảng gợi ý để tại chỗ nằm trong khung cuộn của trang hoặc của hộp thoại, nên bộ nghe cuộn của thư viện đóng nó ngay dưới cờ giảm chuyển động (theo chú thích đầu `e2e/man/ca-lop-noi-neo.ts`). Ca Karma: `autocomplete.component.spec.ts` `describe('AutocompleteComponent — lớp nổi gắn vào body, không nằm trong khung cuộn'` — `.p-autocomplete-overlay` không nằm trong khung cuộn của host, sau khi gõ để có gợi ý. Ca e2e dưới cờ giảm chuyển động (`playwright.config.ts`, chuỗi `reducedMotion: 'reduce'` ở khối `use`): `10-nguoi-dung.spec.ts` `describe('người dùng — danh sách — lớp nổi neo khi giảm chuyển động'`, ca `bảng gợi ý trong hộp "Gán vai trò": hiện, còn mở, chọn được gợi ý` — danh sách `role="listbox"` còn hiện sau khi đứng yên (`choLopNoiDungYen` ở `ca-lop-noi-neo.ts`), ô giữ `aria-expanded="true"`, chọn được một gợi ý và thêm một chip. Phiên chính báo xanh 2026-11-07: Karma 1310 ca (ba lần chạy), e2e bốn màn 287/287; kết quả không lưu trong repo, người soạn chưa chạy lại | **Chưa đo:** e2e chỉ chạy dưới cờ giảm chuyển động bật, nên nhánh có hoạt ảnh chỉ do ca Karma về nơi gắn lớp nổi canh. Ca e2e đi qua biến thể `multiple` ở hộp "Gán vai trò"; biến thể `single` chưa có ca riêng. Ca e2e chỉ chạy ở một chỗ dùng (hộp thoại "Gán vai trò"), một chiều cao viewport (chuỗi `viewport` `height: 800` của project `rong-sang` trong `playwright.config.ts`); chỗ dùng khác và chiều cao khác chưa có ca |
| Chiều cao tối đa của lớp nổi | Có (đối chiếu 2026-10-06): `src/FE/src/app/shared/ui/autocomplete/autocomplete.component.html` chuỗi `scrollHeight="var(--layout-popover-h-max)"`; `src/FE/src/styles/_thu-vien.scss` không còn đặt `max-width` bằng token này (chỉ còn chú thích nói đó là trần chiều cao) | Bề rộng lớp nổi "bằng ô" **chưa đối chiếu**; kết quả trên trình duyệt **chưa đo** |

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
| `focus-visible` | Ô: một đường biên theo [`../DESIGN.md`](../DESIGN.md) §2.6 *Ô nhập chữ* — `--border-w-strong` màu `--color-text`, ô lỗi giữ `--color-danger-border` và dày `--border-w-accent`; không vòng ngoài, không khoảng hở, không `--color-focus`. Dòng đang được bàn phím trỏ tới dùng nền `--color-brand-subtle` + chữ `--color-brand-on-subtle`, và **luôn được cuộn vào tầm nhìn** | Có |
| `active` | Lớp nổi đang mở; ô giữ đường biên focus | Có |
| `disabled` | Nền `--color-surface-3`, chữ `--color-text-disabled`, con trỏ `not-allowed`, thuộc tính `disabled` thật. Chip ở `multiple` mất nút gỡ | Có |
| `loading` | Vòng quay `pi-spinner` ở **đuôi ô**, không phủ lớp nổi. 🛑 **Không khoá ô** — khoá sẽ nuốt ký tự người dùng đang gõ, lỗi tệ hơn nhiều so với một request thừa. `aria-busy` đặt trên hộp kết quả | Có |
| `error` | Gọi máy chủ hỏng: lớp nổi hiện `errorTemplate` — màn ghép dòng lỗi `--color-danger` kèm nút "Thử lại" — và ô giữ nguyên chuỗi đang gõ. Lỗi **xác thực** của trường thì viền ô đổi `--color-danger-border` và dòng lỗi hiện dưới ô qua [`FormRow.md`](./FormRow.md) | Có |
| `empty` | **Ba ca phải phân biệt.** *Chưa đủ ký tự tối thiểu:* "Gõ ít nhất 2 ký tự" — **không** hiện danh sách rỗng. *Không có kết quả:* "Không tìm thấy '…'", kèm dòng tạo mới nếu `allowCreate`. *Danh mục rỗng hoàn toàn:* câu khác hẳn, nói danh mục chưa có dữ liệu | Có |

**Đổi màu:** viền ô lúc nhận focus, nền dòng kết quả lúc hover, nền dòng đang được bàn phím trỏ tới — cả ba đổi trong `--dur-fast` với `--ease-standard`. Nhịp này phải ngắn hơn nhịp gõ phím: người dùng đang rà `↑` `↓` qua danh sách sẽ thấy nền "đuổi theo" nếu nó dài hơn khoảng cách giữa hai lần bấm.

Nhịp đổi màu đó đi qua khoá thời lượng chung của preset: sub-preset `autocomplete` trỏ thời lượng của nó về khoá semantic mà preset Core nối về `--dur-fast`, nên component thừa hưởng không cần khai gì riêng ([`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) quyết định 1).

**Mở và đóng lớp nổi:** theo bộ chung của mọi lớp nổi neo — [`../DESIGN.md`](../DESIGN.md) §7 *Lớp nổi neo*; spec này không đặt số riêng.

🛑 Lớp nổi **không** mang chuyển động mỗi lần danh sách kết quả đổi nội dung — người dùng gõ tiếp một ký tự thì nội dung thay ngay, không mờ vào mờ ra. Lý do ở [`../DESIGN.md`](../DESIGN.md) §7, dòng "nội dung bảng đổi sau khi lọc": chớp một cái ở mỗi phím làm mắt mất chỗ đang đọc, và ở đây mỗi phím là một lần đổi.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-surface-3`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-border-strong`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-danger`, `--color-focus`, `--color-danger-border` |
| Chữ | `--fs-2xs`, `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-medium`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4`, `--sp-5` |
| Hình dạng | `--radius-sm`, `--radius-md`, `--radius-pill`, `--border-w`, `--border-w-strong`, `--border-w-accent` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md`, `--layout-popover-h-max` |
| Bóng, lớp | `--shadow-3`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--ease-standard` — đoạn "Đổi màu" của §Trạng thái. Mở và đóng lớp nổi: §Trạng thái, mục "Mở và đóng lớp nổi" |
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
