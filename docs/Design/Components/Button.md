---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Button

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`. Bảng dưới đây khai **đúng những mục đã mở source ra so** — mục không có trong bảng là mục chưa đối chiếu.

**Nền:** tự dựng, không bọc PrimeNG. Lý do ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — một nút không có hành vi khó nào; bọc thư viện chỉ đổi một lớp trung gian này lấy một lớp trung gian khác, mà lại nhận thêm một tập CSS mặc định phải đè.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/components/button/button.component.ts` — selector `app-button`, class gốc `.nut`.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| API | Đủ chữ ký §API dự kiến: `variant`, `size`, `block`, `icon`, `iconPosition`, `type`, `disabled`, `loading`, `ariaLabel`, output `clicked` | — |
| `haspopup` · `expanded` · `controls` | Có (2026-10-08, chỉ đọc mã, chưa chạy test): `button.component.ts` chuỗi `readonly haspopup = input<'dialog' | 'menu' | null>(null);`, `readonly expanded = input<boolean | null>(null);`, `readonly controls = input<string | null>(null);`; `button.component.html` chuỗi `[attr.aria-haspopup]="haspopup()"`, `[attr.aria-expanded]="expanded() === null ? null : expanded()"`, `[attr.aria-controls]="controls()"` — `null` không sinh thuộc tính. Nền giữ lớp nổi: `button.component.scss` § `&[aria-expanded='true']:not(:disabled)` | Có ca phủ trong `button.component.spec.ts` § `describe('ButtonComponent — nút mở lớp nổi (haspopup / expanded / controls)'` (người soạn chưa chạy test). Chỉ host `<button>` — host `<a>` không nhận |
| `form` | Có (2026-10-08, đọc mã; FE-3): `button.component.ts` chuỗi `readonly form = input<string \| null>(null);`; `button.component.html` chuỗi `[attr.form]="form()"` — `null` không sinh thuộc tính. Dùng ở chân `Drawer` của [`FilterPanel.md`](./FilterPanel.md): `type="submit"` + `[form]="idForm()"` vì nút đứng ngoài thẻ `<form>`. Ca `form: mặc định không sinh thuộc tính form; khai id thì nút thuộc về form đó dù đứng ngoài thẻ`. Đã ghi vào §API dự kiến | Chỉ host `<button>` — host `<a>` không nhận. Người soạn chưa chạy test |
| Mặc định `variant` = `secondary` | Đúng chủ đích spec — khai ngay tại `input<...>('secondary')` | — |
| `clicked` không phát khi `disabled`/`loading` | Có, chặn trong `onClick()` | — |
| Nhãn qua `<ng-content>`, không qua input `label` | Có | — |
| Bốn biến thể | Đủ: `.nut--primary`, `.nut--secondary`, `.nut--ghost`, `.nut--danger`, đúng token nền/chữ/viền spec khai | — |
| Ba cỡ | Đủ: `.nut--sm`/`.nut--md`/`.nut--lg` theo `--size-control-*`, `--sp-4`/`--sp-5`/`--sp-6`, `--fs-xs`/`--fs-sm`/`--fs-md` | — |
| `block` | Có, `.nut--block { width: 100% }` | — |
| `hover` bọc trong `@media (hover: hover)` | Có ở cả bốn biến thể | — |
| `focus-visible` | Có, `--border-w-strong` + `--color-focus` + `outline-offset: 2px` | — |
| `active` | Có, `translateY(1px)` và bỏ bóng; không dùng `scale` | — |
| `disabled` | Có, `opacity: 0.55` + `cursor: not-allowed`; thuộc tính `disabled` thật, gắn cả khi `loading` | — |
| `loading` | Có: `pi-spinner pi-spin` thay icon dẫn, nhãn giữ nguyên, `aria-busy="true"`, nút tự vô hiệu hoá | **Không dự trữ bề rộng.** Spec §Trạng thái đòi `min-width` đo ở trạng thái nghỉ để bề rộng không đổi khi sang `loading`; không có gì trong `.ts`/`.scss` làm việc đó — nút sẽ co giãn nếu nhãn hẹp hơn spinner |
| Icon mang `aria-hidden="true"` | Có ở cả ba nhánh (`loading`, `leading`, `trailing`) | — |
| Thẻ `<button>` thật, `type` luôn tường minh | Có, `[type]="type()"` | — |
| Họ font của nhãn | Có (đối chiếu 2026-10-06, đọc mã): `src/FE/src/app/shared/components/button/button.component.scss` § `font-family: var(--font-sans)` ở `.nut`. Trình duyệt không cho `<button>` kế thừa họ font của trang nên khai tường minh là đúng; lượt đo 2026-10-05 thấy `Arial` là trước khi khai. **Đo lại 2026-10-06** trên `/dang-nhap` (Chromium do Playwright điều khiển, `ng serve`, bề rộng 1280/768/390, sáng và tối; tệp nút không đổi sau lượt đo): font vẽ nhãn nút chính và liên kết chuyển chế độ là họ đầu của `--font-sans`, nét `--fw-semibold`. Lưới Playwright `src/FE/e2e/` phép đo A4 — phiên chính báo xanh, kết quả không lưu trong repo | — |
| Host thẻ `<a>` (`a[app-button]`) | **Có** (đối chiếu 2026-10-06, đọc mã + test xanh): `src/FE/src/app/shared/components/button/button.component.ts` § `selector: 'app-button, a[app-button]'`; `button.component.spec.ts` chuỗi `<a app-button [routerLink]` (chạy xanh trong Chrome). Nơi dùng: `danh-sach-vai-tro.page.html`, `dang-nhap.page.html`, `empty-state.component.html` chuỗi `<a app-button`. Không còn chuỗi `nut-lien-ket` dưới `src/FE/src/app`. **Đo 2026-10-06 một nơi dùng** — liên kết chuyển chế độ ở `/dang-nhap`, `ghost` cỡ `md`, cùng lượt đo dòng trên: cao `--size-control-md`, chữ `--fs-sm` màu `--color-text-muted`, đệm ngang `--sp-5`, không gạch chân; hover nền `--color-surface-2`, chữ `--color-text`; đang gửi thì `aria-disabled="true"`, không còn `href`, `opacity: 0.55`, `cursor: not-allowed` | Chưa đo: `focus-visible`, `active`, và hai nơi dùng còn lại |
| Token `danger` bậc hover/active | `.nut--danger` dùng `--color-danger-hover` và `--color-danger-active`; hai token này có khai ở [`../DESIGN.md`](../DESIGN.md) | §Token dùng của **chính spec này** chưa liệt kê hai token đó — thiếu sót của spec, không phải của code |

Chưa đối chiếu: §Responsive (ba ngưỡng đều là luật của nơi **đặt** nút — `Dialog`, `Toolbar` — không có gì tương ứng trong SCSS của component, chưa mở hai chỗ đó ra so), vùng bấm ≥ 28×28px (đo trên app đang chạy), và tầng i18n của nhãn (nhãn do nơi gọi truyền vào).

---

## Mục đích

Kích hoạt một hành động, có nhãn chữ.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Bất kỳ hành động nào **xảy ra tại chỗ**: lưu, huỷ, mở dialog, gửi form | 🛑 **Điều hướng sang tuyến khác** → dùng `<a routerLink>`; nếu cần hình thức nút thì `<a app-button routerLink>` (§Liên kết mang hình thức nút), không phải `<app-button>`. Bàn phím và menu chuột phải của trình duyệt hành xử khác nhau giữa nút và liên kết; giả một cái bằng cái kia là lấy đi thao tác "mở tab mới" của người dùng |
| Hành động cần nhãn chữ để hiểu | 🛑 Hành động đã rõ nghĩa bằng icon và bị lặp ở mỗi hàng bảng → [`IconButton.md`](./IconButton.md) |
| Hành động chính hoặc phụ của một vùng | 🛑 Chuyển giữa hai chế độ xem → [`SegmentedControl.md`](./SegmentedControl.md) |
| Nút submit của form | 🛑 Bật/tắt một tuỳ chọn → [`Check.md`](./Check.md) |

## Liên kết mang hình thức nút — `a[app-button]`

✅ **CÓ THẬT** (đối chiếu 2026-10-06: `button.component.ts` chuỗi `a[app-button]`; quyết định của người dùng 2026-10-06, Q62; kiểu Angular Material `<a mat-button>`). Thay cho ngoại lệ cũ "một thẻ `<a>` mượn lớp hình thức", vốn buộc từng nơi dùng **chép CSS** của nút.

| Khoản | Luật |
| --- | --- |
| Cách dùng | `<a app-button routerLink="…" variant="ghost">Nhãn</a>` — host là thẻ `<a>` thật do nơi dùng viết; cùng một `ButtonComponent`, **không** component thứ hai, **không** `<button>` hay `<app-button>` lồng trong `<a>` |
| Khi nào | Điều hướng thật nhưng đứng ở chỗ nút đứng: nhóm hành động của màn ([`PageHeader.md`](./PageHeader.md), chân [`Dialog.md`](./Dialog.md), [`EmptyState.md`](./EmptyState.md) `actionRoute`) và liên kết phụ dưới form của [`AuthCard.md`](./AuthCard.md) (chỉ `ghost`). Hành động tại chỗ vẫn là `<app-button>`; liên kết chữ trong đoạn văn vẫn là `<a>` trần |
| Input nhận | `variant`, `size`, `block`, `icon`, `iconPosition`, `disabled`, `ariaLabel` — cùng nghĩa, cùng mặc định như §API. **Không** nhận `type` và `loading` (liên kết không submit, không có trạng thái chờ); **không** có output `clicked` — điều hướng là việc của `routerLink` |
| Hình thức | Y hệt `<app-button>`: bốn biến thể, ba cỡ, các token ở §Token dùng, `hover` trong `@media (hover: hover)`, `focus-visible`, `active`, `font-family` là `--font-sans`; thêm `text-decoration: none`. Không nơi dùng nào được viết lại các giá trị này trong SCSS của mình |
| `disabled` | Thẻ `<a>` không có `disabled` thật. Khi `disabled`: `aria-disabled="true"`, `opacity: 0.55`, `cursor: not-allowed`, bỏ hover/active, và chặn kích hoạt (không điều hướng). Nơi dùng đặt `[routerLink]` bằng `null` để thẻ không còn `href` — như `Sidebar` làm với mục bị khoá |
| Accessibility | Giữ nguyên là liên kết: vai `link` ngầm của `<a>`, `Enter` kích hoạt (không `Space`), mở tab mới/menu chuột phải còn nguyên — đó chính là lý do chọn `<a>`. Nhãn, icon `aria-hidden`, vùng bấm ≥ 28×28px theo §Accessibility |
| Một `primary` mỗi vùng | Vẫn áp dụng, tính chung nút và liên kết mang hình thức nút |

**Vì sao một selector chứ không phải mixin/lớp dùng chung:** lớp dùng chung vẫn để mỗi nơi tự nhớ gắn lớp và nhớ `font-family`, `:focus-visible`, `[aria-disabled]` — đúng ba chỗ đã lệch ở các bản chép hiện có (lệch font, thiếu hover). Một component thì sửa một nơi.

## Biến thể

| Biến thể | Vai | Nền | Chữ | Viền |
| --- | --- | --- | --- | --- |
| `primary` | **Một cái mỗi vùng.** Hành động chính | `--color-brand` | `--color-text-on-brand` | trong suốt |
| `secondary` | Hành động phụ có nhãn: Huỷ, Xoá lọc, Đóng | `--color-surface` | `--color-text` | `--color-border` |
| `ghost` | Hành động thứ ba, nơi một cái viền sẽ làm rối vùng | trong suốt | `--color-text-muted` | không |
| `danger` | **Chỉ hành động phá huỷ** — nút xác nhận cuối cùng của `ConfirmDialog` | `--color-danger` | `--color-text-on-brand` | trong suốt |

Tương phản chữ trên nền đặc (đã tính, [`../DESIGN.md`](../DESIGN.md) §2): `primary` **6.85:1** sáng / **7.19:1** tối; `danger` **6.78:1** / **7.80:1**. Cả bốn đều vượt AA.

🛑 **Không quá một `primary` trong cùng một vùng nhìn.** Hai nút primary cạnh nhau nghĩa là không cái nào là chính, và người dùng phải đọc cả hai để chọn. Đây là lỗi hay gặp nhất ở chân `Dialog`: Lưu là `primary`, Huỷ là `secondary`.

🛑 **`danger` không phải "nút màu đỏ cho vui".** Nó dành cho thao tác không hoàn tác được. Dùng cho một nút "Xoá bộ lọc" sẽ làm màu đỏ mất nghĩa đúng lúc nó cần có nghĩa nhất.

Bốn biến thể là toàn bộ. Không có `link`: điều hướng là `<a>` (§Khi nào dùng), hành động tại chỗ cần trông nhẹ là `ghost`. Không có nút kèm menu thả xuống: hành vi lớp nổi thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 — khi cần thì là component riêng, hôm nay hành động phụ đi qua [`Menu.md`](./Menu.md) mở từ một [`IconButton.md`](./IconButton.md).

## Kích thước

| Cỡ | Chiều cao | Đệm ngang | Cỡ chữ | Icon | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--sp-4` | `--fs-xs` | `--icon-sm` | Trong hàng bảng, trong chip |
| `md` | `--size-control-md` (34px) | `--sp-5` | `--fs-sm` | `--icon-md` | **Mặc định.** Form, toolbar, dialog |
| `lg` | `--size-control-lg` (42px) | `--sp-6` | `--fs-md` | `--icon-md` | Nút submit chính, màn xác thực |

Thêm hai công tắc hình dạng, dùng được với mọi cỡ:

| Công tắc | Hiệu ứng | Dùng khi |
| --- | --- | --- |
| `block` | `width: 100%`, nội dung căn giữa | Submit trên `AuthCard`; nút trong `Dialog` ở màn nhỏ |
| `icon-leading` / `icon-trailing` | Chèn icon trước/sau nhãn, khe `--sp-3` | Icon trước cho hành động ("Thêm"); icon sau cho tiết lộ ("Mở rộng") |

Nhãn nút **không xuống dòng** (`white-space: nowrap`). Nhãn dài quá thì rút ngắn câu, đừng để nút cao gấp đôi làm vỡ nhịp hàng.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Theo biến thể; `--radius-sm`; `--fw-semibold`; `--lh-snug` | Có |
| `hover` | `primary` → nền `--color-brand-hover`. `secondary` → nền `--color-surface-2`, viền `--color-border-strong`. `ghost` → nền `--color-surface-2`, chữ `--color-text`. `danger` → nền tối/sáng thêm một bậc theo cùng thang. Thêm `--shadow-2`. Chuyển tiếp `--dur-fast` với `--ease-standard`. **Bọc trong `@media (hover: hover)`** | Có |
| `focus-visible` | `outline: var(--border-w-strong) solid var(--color-focus)`, `outline-offset: 2px`. Chỉ `:focus-visible`. Khoảng hở 2px là thứ giữ cho vòng focus không tàng hình trên nút `primary` — xem [`../DESIGN.md`](../DESIGN.md) §2.6 | Có |
| `active` | Nền của bậc `active` (`--color-brand-active` cho `primary`); `transform: translateY(1px)`; bỏ bóng. Không dùng `scale` — nó làm chữ nhoè | Có |
| `disabled` | `opacity: 0.55`; `cursor: not-allowed`; bỏ mọi hiệu ứng hover/active. Bắt buộc thuộc tính `disabled` thật, không chỉ đổi màu | Có |
| `loading` | Icon `pi-spinner` quay thay cho icon dẫn; nhãn **giữ nguyên**; nút bị vô hiệu hoá; `aria-busy="true"`. Bề rộng nút **không đổi** — dự trữ chỗ cho spinner bằng `min-width` đo ở trạng thái nghỉ, đúng một lần lúc chuyển sang `loading`; không đặt số cứng vì nhãn dài ngắn khác nhau | Có |
| `expanded` | Nút đang giữ một lớp nổi mở (`aria-expanded="true"`, ví dụ nút lọc của [`Toolbar.md`](./Toolbar.md)): nền `--color-surface-2`, viền `--color-border-strong` — như `hover` của `secondary`, giữ cho tới khi lớp đóng để người dùng thấy nút nào đang giữ lớp. Không áp cho nút `disabled`. Không phải `active`: không dịch chuyển, không đổi bóng | Có — chỉ khi nơi dùng khai `haspopup` và `expanded` |
| `error` | **Không áp dụng.** Nút không tự mang lỗi. Kết quả hỏng của một hành động hiện ở `Toast` hoặc `NoticeBanner` | — |
| `empty` | **Không áp dụng.** Nút không có nhãn là lỗi thi công, không phải một trạng thái | — |

**Vì sao `loading` giữ nguyên nhãn:** đổi nhãn thành "Đang lưu…" làm nút thay đổi bề rộng, đẩy các nút bên cạnh nhảy chỗ, và người dùng đang di chuột tới nút Huỷ có thể bấm nhầm. Spinner + `aria-busy` đã đủ báo trạng thái.

**Vì sao `loading` phải khoá nút:** không khoá thì hai lần bấm nhanh tạo hai request. Với một lệnh tạo bản ghi, đó là hai bản ghi.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-brand`, `--color-brand-hover`, `--color-brand-active`, `--color-danger`, `--color-text`, `--color-text-muted`, `--color-text-on-brand`, `--color-surface`, `--color-surface-2`, `--color-border`, `--color-border-strong`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-sm`, `--border-w`, `--border-w-strong` |
| Kích thước | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-sm`, `--icon-md` |
| Bóng | `--shadow-2` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Nút giữ bề rộng theo nội dung; nhóm nút xếp ngang, khe `--sp-4` |
| < `$bp-md` | Nhóm nút trong chân `Dialog` xuống dòng; nút `primary` **lên trên** khi xếp dọc |
| < `$bp-xs` | Nút trong `Toolbar` và chân `Dialog` chuyển sang `block` |

**Khi xếp dọc, `primary` lên trên chứ không xuống dưới.** Ở màn hình nhỏ người dùng quét từ trên xuống; hành động chính phải gặp trước. Thứ tự trong DOM giữ nguyên (Huỷ trước, Lưu sau, để thứ tự Tab hợp lý ở desktop) và đảo bằng `order` của flex ở màn nhỏ.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<button>` thật cho hành động tại chỗ; `<a>` thật cho `a[app-button]` (§Liên kết mang hình thức nút). 🛑 Không `<div role="button">` — mất Enter/Space, mất `disabled`, mất mọi thứ miễn phí |
| `type` | **Luôn đặt tường minh.** Mặc định của `<button>` trong form là `submit`; một nút "Thêm dòng" quên `type="button"` sẽ gửi cả form |
| Bàn phím | `Enter` và `Space` kích hoạt — có sẵn nếu dùng `<button>` |
| Focus | Nhìn thấy được ở `:focus-visible`, tương phản ≥ 3:1 nhờ `outline-offset` |
| Nhãn | Nhãn chữ nhìn thấy được. Có icon thì icon mang `aria-hidden="true"` — [`../Icons.md`](../Icons.md) §7 |
| `disabled` | Thuộc tính `disabled` thật. Nếu cần nút vẫn nhận focus để giải thích vì sao khoá thì dùng `aria-disabled="true"` + chặn xử lý, và **phải** có tooltip hoặc chữ giải thích |
| `haspopup` · `expanded` · `controls` | Nút **mở một lớp nổi** khai ba input này; khớp `IconButton`. `haspopup` sinh `aria-haspopup`, `expanded` sinh `aria-expanded` (chỉ có nghĩa khi `haspopup` khác `null`), `controls` là `id` của lớp nổi → `aria-controls`. Nút điều hướng hay nút hành động thường không khai |
| `loading` | `aria-busy="true"`. Kết quả xong báo qua vùng `aria-live` của `Toast`, không báo bằng chính cái nút |
| Vùng bấm | ≥ 28×28px kể cả cỡ `sm` (WCAG 2.2 SC 2.5.8) |
| Chữ | Qua tầng i18n, không viết thẳng vào template — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'primary' \| 'secondary' \| 'ghost' \| 'danger'` | `'secondary'` | Mặc định là `secondary` **có chủ đích**: quên khai biến thể thì ra nút phụ, không ra nút chính. Sai theo hướng an toàn |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `block` | input | `boolean` | `false` | |
| `icon` | input | `string \| null` | `null` | **Tên trần** của icon PrimeIcons: `plus`, không phải `pi-plus`, không phải `pi pi-plus`. Component tự ghép — [`../Icons.md`](../Icons.md) §5 |
| `iconPosition` | input | `'leading' \| 'trailing'` | `'leading'` | |
| `type` | input | `'button' \| 'submit' \| 'reset'` | `'button'` | Mặc định `'button'` để bẫy submit ngoài ý muốn không xảy ra |
| `form` | input | `string \| null` | `null` | `id` của `<form>` mà nút `submit` thuộc về khi nút đứng **ngoài** thẻ form (chân `Drawer`, chân `Dialog` tách khỏi thân). Chỉ có nghĩa với `type="submit"`. `null` không sinh thuộc tính `form`. Chỉ host `<button>` |
| `disabled` | input | `boolean` | `false` | |
| `loading` | input | `boolean` | `false` | Khi `true` thì nút tự vô hiệu hoá, không cần đặt `disabled` kèm |
| `ariaLabel` | input | `string \| null` | `null` | Chỉ dùng khi nhãn nhìn thấy không đủ ngữ cảnh |
| `haspopup` | input | `'dialog' \| 'menu' \| null` | `null` | Nút **mở một lớp nổi** (nút lọc của [`Toolbar.md`](./Toolbar.md) mở `FilterPanel`: `'dialog'`): sinh `aria-haspopup`. `null` = không sinh. Chỉ host `<button>` |
| `expanded` | input | `boolean \| null` | `null` | Chỉ có nghĩa khi `haspopup` khác `null`: sinh `aria-expanded` phản ánh lớp nổi đang mở, và bật nền trạng thái `expanded` (§Trạng thái). `null` = không sinh |
| `controls` | input | `string \| null` | `null` | `id` của lớp nổi nút điều khiển → `aria-controls`. `null` = không sinh |
| `clicked` | output | `void` | — | **Không phát khi đang `disabled` hoặc `loading`.** Chặn ở component, không bắt mỗi nơi gọi tự nhớ |

Nội dung nhãn vào qua slot mặc định (`<ng-content>`), không qua một input `label`. Nhãn hay chứa cả chữ lẫn phần tử con (một `Badge` đếm số), và một input chuỗi sẽ chặn điều đó.

`Button` là component **dumb** — không inject service lấy dữ liệu, không biết route ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Một `primary` mỗi vùng. Còn lại là `secondary` hoặc `ghost`.
- ✅ Luôn khai `type`.
- ✅ Dùng `loading` thay vì tự khoá bằng `disabled` ở nơi gọi — như vậy spinner và trạng thái khoá luôn đi cùng nhau.
- ✅ Nhãn là **động từ và đối tượng**: "Lưu người dùng", không phải "OK".
- ❌ Không dùng `<app-button>` để điều hướng. Dùng `<a>`, hoặc `<a app-button>` khi cần hình thức nút.
- ❌ Không chép CSS của nút vào SCSS của màn hay component để vẽ một liên kết trông như nút.
- ❌ Không đặt `outline: none` mà không thay bằng dấu hiệu focus khác.
- ❌ Không đổi nhãn khi `loading`.
- ❌ Không đẻ cỡ thứ tư. Ba cỡ ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.2 là toàn bộ.
- ❌ Không dùng `danger` cho hành động hoàn tác được.

## Cần chốt

Không còn.
