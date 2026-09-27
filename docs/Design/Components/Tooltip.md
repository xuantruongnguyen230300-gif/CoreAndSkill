---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Tooltip

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Lớp bọc đã có ở `src/FE` đúng hình dạng **component** mà spec chốt ngày 2026-09-22. Bảng dưới khai những mục đã mở source ra so ngày 2026-09-22 và 2026-09-23, chỉ bằng đọc mã; mục không có tên trong bảng thì chưa ai đối chiếu, nên `verified:` giữ `chua-doi-chieu`. Khoản hở cuối — **cách nối `aria-describedby` khi phần tử neo là một component Core**, chốt ngày 2026-09-23 ở §Accessibility — đã **đóng bằng sửa code** cùng ngày (hàng `aria-describedby`).

Khoản hở còn lại cho tới hôm qua — **thời lượng chuyển tiếp** — đã đóng ngày 2026-09-23, và đóng bằng cách đổi luật chứ không bằng sửa code: người dùng chốt **nhận thời lượng của thư viện**, spec thôi đòi `--dur-fast`. Luật mới ở §Accessibility, "Thời lượng chuyển tiếp — đã chốt".

Lớp bọc hôm nay: `src/FE/src/app/shared/ui/tooltip/tooltip.component.ts` — class `TooltipComponent`, selector `app-tooltip`, kèm `tooltip.component.html`, `tooltip.component.scss` và `tooltip.component.spec.ts` cùng thư mục. Khối override hình thức nằm ở `src/FE/src/styles/_thu-vien.scss` § `.app-tooltip`.

| Mục trong spec | Có thật hôm nay (neo bằng chuỗi trong `src/FE/src`) | Sẽ thành |
| --- | --- | --- |
| §Nền — bọc PrimeNG | `tooltip.component.ts` gắn `Tooltip` của `primeng/tooltip` qua § `hostDirectives: [Tooltip]`; preset đăng ký sub-preset tương ứng (`core/theme/prime-preset.ts` § `tooltip: AuraTooltip`) | Giữ nguyên |
| Hình dạng lớp bọc | **Khớp.** Component dạng bọc, § `selector: 'app-tooltip'`; phần tử neo vào bằng nội dung chiếu (`tooltip.component.html` § `<ng-content />`). Ràng buộc "đúng MỘT phần tử" được cưỡng chế, không chỉ được viết: § `doNoiDungChieuVao()` đo nội dung sau lần render đầu và hạ cờ § `noiDungHopLe` — sai thì không dựng gì và `console.error` lúc phát triển. Host có hộp riêng — `tooltip.component.scss` § `display: inline-flex`. Ba chỗ gọi trong hai tệp đã đổi theo: `filter-chip.component.html` § `<app-tooltip [text]="lockReason() ?? ''"`, `ma-tran-phan-quyen.page.html` § `<app-tooltip [text]="hang.code">` và § `variant="hint"` | — |
| §Accessibility — `role="tooltip"` | **Có ở cả hai kênh.** Hộp nổi: thư viện dựng kèm vai trò này (`primeng/tooltip` § `role: 'tooltip'`). Phần tử mô tả: lớp bọc tự vẽ (`tooltip.component.html` § `class="sr-only" role="tooltip"`) | Giữ nguyên |
| §Accessibility — `aria-describedby` trên phần tử neo | **Khớp, cả bốn khoản của "Phần tử neo là một component Core" (đối chiếu 2026-09-23).** Neo là phần tử thường (`<button>`, `<span>`): lớp bọc vẫn tự đặt và gỡ theo trạng thái (`tooltip.component.ts` § `'aria-describedby', this.idMoTa`). Neo là component tự dựng control bên trong: lớp bọc **thôi** ghi — § `neoLaComponent` nhận ra bằng dấu gạch ngang trong tên thẻ, thứ mà tên thẻ HTML gốc không bao giờ có — và `id` ra ngoài thành bề mặt công khai § `readonly idMoTa =` + § `exportAs: 'appTooltip'`. Nơi gọi nối: `ma-tran-phan-quyen.page.html` § `#tipO="appTooltip"` và § `[describedBy]="tipO.idMoTa"`. 🛑 Chuỗi chỉ tới được `<input>` thật nhờ pass-through của thư viện (`check.component.ts` § `protected readonly ptO = computed(`, `check.component.html` § `[pt]="ptO()"`): PrimeNG chuyển tiếp `aria-labelledby`/`aria-label` xuống `<input>` nhưng **không** chuyển `aria-describedby`, nên đặt bằng `[attr.…]` trên `<p-checkbox>` là đặt lên host. Quên nối thì báo lúc phát triển, không im lặng — § `console.warn` | — |
| §Trạng thái `focus-visible` — hiện ngay khi neo nhận focus | **Khớp.** `tooltip.component.ts` § `'(focusin)': 'khiNhanFocus()'` gọi thẳng § `this.prime.show()`, không đi qua nhánh có độ trễ; § `khiMatFocus` bỏ qua khi focus còn quanh quẩn bên trong host. `tooltipEvent` không được đặt ở đâu nên giữ mặc định `'hover'`, đúng ràng buộc 1 của §Bàn phím | — |
| §Accessibility — `Escape` do lớp bọc tự lo | **Khớp.** § `'(keydown.escape)': 'khiEscape()'` chỉ gọi `deactivate()`, không đụng tới focus | — |
| §API — `text`, `position`, `disabled`, `variant`, `delay` | **Khớp chữ ký.** § `input.required<string>()` cho `text`; `position` và `disabled` đi vào tuỳ chọn thư viện qua § `this.prime.setOption(`; `variant` ra hộp nổi thành một lớp § ``app-tooltip--${this.variant()}``; mặc định của `delay` là hằng số có tên trỏ về spec § `export const TOOLTIP_DELAY_CHUOT_MS`, và nó chỉ vào § `showDelay:` — tức chỉ đường chuột chờ. Không có output | — |
| §Kích thước, §Trạng thái `default`, §Token dùng — hình thức hộp nổi | **Đã áp, qua biến khai tại chỗ.** `_thu-vien.scss` § `--p-tooltip-background` và các biến `--p-tooltip-*` cùng khối nhận giá trị từ token của [`../DESIGN.md`](../DESIGN.md); cỡ chữ đi riêng vì preset không có token chữ cho tooltip (§ `.p-tooltip-text`). Thang lớp đi qua § `tooltipZIndex: 'var(--z-popover)'`. Mũi nhọn tự theo nền, không phải khai lại: thư viện tô nó bằng chính biến nền (`@primeuix/styles/dist/tooltip` § `border-top-color: dt('tooltip.background')`) | — |
| §Trạng thái `empty` và §API `disabled` — không dựng gì | **Khớp.** § `tat = computed(` gộp ba điều kiện tắt (nội dung rỗng sau `trim()`, `disabled`, nội dung chiếu vào sai) rồi tắt **cả hai** kênh; template § `@if (moTaHienDuoc())` nên phần tử mô tả cũng không được dựng | — |
| §Trạng thái `hover` — bọc trong `@media (hover: hover)` | **Đã áp ở dạng đảo, và tách theo nguồn mở hộp.** `_thu-vien.scss` § `@media (hover: none)` chỉ ẩn `.app-tooltip--chuot`; lớp bọc gắn lớp theo nguồn § ``app-tooltip--${nguon}`` với hai giá trị `'chuot'` / `'ban-phim'`. Hệ quả có chủ đích: trên thiết bị không rê được, hộp mở bằng bàn phím **vẫn hiện** — bàn phím rời cắm vào máy bảng | Giữ nguyên — đã ghi thành luật ở §Trạng thái |
| §Responsive — tắt phần nhìn dưới `$bp-sm` | **Đã áp bằng `visibility`, không bằng `display`.** `_thu-vien.scss` § `@media (max-width: #{s.$bp-sm - 1px})` đặt `visibility: hidden`. Lý do ghi tại chỗ: `show()` của thư viện ghi `style.display` **nội tuyến**, mà một khai báo nội tuyến chỉ đè được bằng `!important`; `visibility` thì đè được bằng độ ưu tiên thường. Phần tử `.sr-only` không nằm trong khối này nên lời chú vẫn tới được trình đọc màn hình — đúng cái §Responsive đòi | Giữ nguyên — đã ghi thành luật ở §Responsive |
| §Accessibility — chuyển động hiện/ẩn | **Khớp luật mới, sau khi luật đổi ngày 2026-09-23.** Hiện: `primeng/tooltip` § `fadeIn(this.container, 250)` — một vòng `requestAnimationFrame` ghi `opacity` nội tuyến, không đọc CSS; con số nằm trong mã thư viện, không đổi được từ ngoài. Ẩn: § `hide()` gọi thẳng § `this.remove()`, gỡ hộp khỏi DOM ngay — **không có chuyển tiếp nào**. Vế `prefers-reduced-motion` **đạt**: `_thu-vien.scss` § `@media (prefers-reduced-motion: reduce)` đặt `opacity: 1 !important`, đè được vòng `fadeIn` vì nó ghi nội tuyến | Giữ nguyên — §Accessibility nay mô tả đúng hành vi này |

**Chưa đối chiếu:** `pointer-events: none` ở §Accessibility — chuỗi đó không có trong `primeng/tooltip`, trong stylesheet tooltip của `@primeuix/styles`, lẫn trong khối override của Core, nên chưa kết luận được hộp nổi có chặn chuột hay không. Cùng với đó là mọi thứ chỉ thấy khi chạy ứng dụng thật: hộp lật hướng khi chạm mép màn hình, độ trễ cảm nhận được, mũi nhọn vẽ ra sao.

**`Escape`:** thư viện có `hideOnEscape` bật mặc định, nhưng nó chỉ đăng ký lắng nghe bên trong nhánh mở có độ trễ, nên đường bàn phím đã chốt ở §Accessibility **không** đi qua đó. Lớp bọc tự chịu khoản này.

**Nền:** **bọc PrimeNG**. Cùng lý do với [`Menu.md`](./Menu.md): định vị một lớp nổi khi trang cuộn hoặc khi nó chạm mép màn hình thuộc nhóm "khó" ở [`../COMPONENTS.md`](../COMPONENTS.md) §4. Phần Core tự viết là luật **khi nào được hiện** và **hiện cái gì** — hai thứ thư viện không quyết hộ được.

**Nền đã được xét lại và giữ nguyên — chốt 2026-09-23.** Câu hỏi đưa ra là có nên bỏ thư viện, tự dựng trên lớp overlay của `@angular/cdk` để lấy lại input `for`, `aria-describedby` trỏ thẳng vào hộp thật, quyền đặt thời lượng chuyển tiếp, và bỏ ràng buộc "đúng một phần tử chiếu vào". Người dùng chốt **giữ nền bọc thư viện**. Cái giá bị từ chối là cái giá cấu trúc, không phải cái giá công sức: một tooltip tự dựng làm hệ có **hai nền lớp nổi song song** cạnh [`Menu.md`](./Menu.md), [`Dialog.md`](./Dialog.md) và [`Drawer.md`](./Drawer.md) — hai cơ chế định vị, hai cách xử lý cuộn trang, hai chỗ phải sửa mỗi lần nâng cấp. Ba cách đi vòng đã chốt ở tệp này (hình dạng bọc thay cho `for`, hai kênh tách nhau, `focusin` thay cho `focus`) là cái giá đã trả xong và không phải trả lại; khoản thời lượng thì spec nhường hẳn — xem §Accessibility, "Thời lượng chuyển tiếp".

---

## Mục đích

Hiện một dòng chú ngắn cho một phần tử, khi và chỉ khi người dùng tỏ ý muốn biết thêm.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Hiện nhãn chữ của một mục nav khi [`Sidebar.md`](./Sidebar.md) đang thu gọn còn dải icon | 🛑 **Làm nhãn cho một [`IconButton.md`](./IconButton.md).** Nhãn là việc của `aria-label` — xem ràng buộc cứng dưới đây |
| Nói vì sao một control đang bị khoá: "Cần quyền duyệt chứng từ" | 🛑 Thông tin người dùng **cần** để hoàn thành việc → dòng gợi ý của [`FormRow.md`](./FormRow.md), luôn nhìn thấy được |
| Hiện giá trị đầy đủ của một ô bảng đã bị cắt bằng dấu ba chấm — **khai theo cột**, không tự đo mọi ô lúc chạy; cờ trong `ColumnDef` chốt khi dựng [`DataTable.md`](./DataTable.md) | 🛑 Nội dung dài hơn hai dòng → [`Dialog.md`](./Dialog.md) hoặc một dòng mô tả tại chỗ |
| Giải thích một icon trạng thái trong ô bảng | 🛑 Bất cứ thứ gì **bấm được** bên trong — xem Do / Don't |

### Ràng buộc cứng — tooltip không bao giờ là nhãn

🛑 **Tooltip KHÔNG thay được `aria-label`.** Ba lý do, mỗi lý do đủ để một mình quyết định:

1. Nhiều hiện thực không hiện tooltip khi phần tử nhận focus bằng bàn phím — người dùng bàn phím không bao giờ thấy nó.
2. Nó biến mất ngay khi chạm, nên trên thiết bị cảm ứng nó gần như không tồn tại.
3. Trình đọc màn hình đọc nhãn, không đọc lớp nổi.

Tooltip là thứ **bổ sung** cho một phần tử đã có nhãn. [`IconButton.md`](./IconButton.md) ghi đúng dòng này trong mục Accessibility của nó, và đây là lời giải thích đầy đủ cho dòng đó.

## Biến thể

| Biến thể | Nội dung | Dùng khi |
| --- | --- | --- |
| `label` | Đúng một cụm từ, không dấu chấm câu | **Mặc định.** Nhãn của icon, tên mục nav thu gọn, giá trị ô bị cắt |
| `hint` | Một câu đầy đủ, tối đa hai dòng | Giải thích vì sao một control bị khoá, hoặc một quy tắc ngắn |

Chỉ hai biến thể, và ranh giới giữa chúng là **độ dài**. Tooltip quá hai dòng là dấu hiệu nội dung đó thuộc về trang chứ không thuộc về một lớp nổi biến mất sau một giây.

🛑 **Không có biến thể mang màu trạng thái.** Một tooltip đỏ trông như một thông báo lỗi, trong khi nó chỉ là lời chú. Lỗi thuộc về [`NoticeBanner.md`](./NoticeBanner.md), [`Toast.md`](./Toast.md) và dòng lỗi của `FormRow`.

## Kích thước

`Tooltip` **không** dùng thang `--size-control-*` — nó không phải control, chiều cao sinh ra từ cỡ chữ và đệm.

| Khoản | Giá trị |
| --- | --- |
| Cỡ chữ | `--fs-xs` |
| Cân nặng | `--fw-medium` |
| Đệm | `--sp-2` dọc / `--sp-4` ngang |
| Bo góc | `--radius-xs` |
| Bề rộng tối đa | `--layout-tooltip-w-max` ([`../DESIGN.md`](../DESIGN.md) §6.1) — giá trị riêng, một trần cho mọi điểm ngắt; quá thì xuống dòng |
| Khe tới phần tử neo | `--sp-3` |
| Đổ bóng, lớp | `--shadow-2`, `--z-popover` |

Một cỡ duy nhất, có chủ đích. Tooltip luôn là chữ phụ đứng cạnh thứ khác; cho nó nhiều cỡ là mời người ta dùng nó làm tiêu đề.

Mũi nhọn chỉ về phần tử neo là **bắt buộc**. Không có nó, một tooltip nằm giữa hai nút cạnh nhau không nói được nó đang chú cho nút nào.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-inverse-surface`, chữ `--color-inverse-text` ([`../DESIGN.md`](../DESIGN.md) §2.6). Không viền — nền đảo đã tách khỏi bề mặt ở mức 14.68:1 sáng / 14.64:1 tối | Có |
| `hover` | **Đây chính là cơ chế hiện**, không phải một hình thức riêng. Hiện sau độ trễ `delay` rê chuột; ẩn ngay khi con trỏ rời. Bọc trong `@media (hover: hover)` — 🛑 **chỉ đường chuột**, xem ngay dưới | Có |
| `focus-visible` | Hiện **ngay lập tức**, không có độ trễ, khi phần tử neo nhận focus bàn phím. Người dùng bàn phím đã chủ động đi tới đó — bắt họ chờ thêm là vô nghĩa. Đường bàn phím đi qua `focusin`/`focusout` trên host, không qua đường `focus` của thư viện (§Accessibility) | Có |
| `active` | **Không áp dụng.** Tooltip không bấm được; nó không có trạng thái đang-bị-nhấn | — |
| `disabled` | **Không áp dụng cho chính tooltip.** Nhưng phần tử neo bị khoá thì tooltip **vẫn phải hiện** — đó là ca dùng chính của biến thể `hint`. Một nút khoá dùng `aria-disabled` thay cho `disabled` để vẫn nhận được focus | — |
| `loading` | **Không áp dụng.** Nội dung tooltip là chữ tĩnh truyền vào lúc dựng. Cần tải mới có nội dung thì đó không phải tooltip mà là một popover, và Core chưa có component đó | — |
| `error` | **Không áp dụng.** Tooltip không mang lỗi. Xem ghi chú ở mục Biến thể | — |
| `empty` | Nội dung rỗng thì **không dựng gì cả** — không hiện một hộp rỗng. Đây là hành vi bắt buộc, vì chuỗi i18n thiếu khoá sẽ trả về rỗng và một hộp đen nhỏ nhảy ra giữa màn hình là lỗi rất khó truy | Có |

### `@media (hover: hover)` chỉ tắt đường chuột — đã chốt

🛑 **Hộp mở bằng bàn phím vẫn phải hiện trên thiết bị không rê được.** Điều kiện `@media (hover: hover)` ở dòng `hover` nói về *cơ chế rê chuột*, không phải về *cả component*. Một máy bảng có bàn phím rời là thiết bị không rê được nhưng vẫn `Tab` được; tắt cả hai đường ở đó là lấy mất lời chú của đúng nhóm người dùng đã chủ động đi tìm nó.

Nên hộp nổi phải **mang dấu của nguồn đã mở nó** — chuột hay bàn phím — để khối `@media` phân biệt được hai đường. Đây là ràng buộc của component, không phải của nơi gọi: nơi gọi không biết hộp được mở bằng gì.

Ranh giới với §Responsive, vì hai luật trông giống nhau mà không giống nhau: dưới `$bp-sm` thì **cả hai** đường cùng tắt phần nhìn; `@media (hover: hover)` thì **chỉ** đường chuột tắt. Hai điều kiện độc lập, không cái nào thay cái nào.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-inverse-surface`, `--color-inverse-text` |
| Chữ | `--fs-xs`, `--fw-medium`, `--lh-snug` |
| Khoảng cách | `--sp-2`, `--sp-3`, `--sp-4` |
| Hình dạng | `--radius-xs` |
| Kích thước | `--layout-tooltip-w-max` |
| Bóng, lớp | `--shadow-2`, `--z-popover` |

🛑 **Không có nhóm `Chuyển động` — đã chốt 2026-09-23.** `--dur-fast` và `--ease-standard` từng đứng ở đây và đã bị gỡ: trên nền bọc thư viện, **không giá trị nào trong hai cái đó tới được chuyển động của tooltip**. Thư viện tự chạy vòng mờ dần bằng `requestAnimationFrame` ghi `opacity` nội tuyến, nên nó không đọc thời lượng từ CSS và cũng không nhận đường cong nào — vòng đó tăng `opacity` tuyến tính. Giữ hai token trong bảng là khai một thẩm quyền spec không có. Luật thay thế ở §Accessibility, "Thời lượng chuyển tiếp — đã chốt".

**`--dur-slow` cũng cố ý KHÔNG có trong bảng này**, nhưng vì một lý do khác hẳn: độ trễ trước khi hiện là tham số hành vi, không phải thời lượng chuyển tiếp — lý do đầy đủ ở §API dự kiến, dòng `delay`. Hai chỗ vắng mặt này đừng đọc lẫn nhau: `--dur-slow` vắng vì **spec không muốn dùng**, `--dur-fast` vắng vì **spec không dùng được**.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Hiện bên trên phần tử neo; lật xuống dưới, sang trái hoặc sang phải khi không đủ chỗ |
| `$bp-sm` … `$bp-md` | Giữ nguyên, cùng trần bề rộng |
| < `$bp-sm` | 🛑 **Tắt phần nhìn.** Hộp nổi không hiện. Phần tử mô tả ẩn cho trình đọc màn hình (§Accessibility) **vẫn còn**. Xem dưới |

**Vì sao tắt ở màn nhỏ thay vì đổi cách hiện:** không có chuột thì không có "rê chuột", và mọi cách giả lập — chạm giữ, chạm lần một để xem chạm lần hai để bấm — đều tạo ra một nút hành xử khác với mọi nút còn lại. Cái giá phải trả rất cụ thể và phải chấp nhận công khai: **mọi thông tin chỉ có trong tooltip là thông tin người dùng di động không bao giờ thấy.** Đó chính là lý do luật ở mục Biến thể cấm đặt thông tin cần thiết vào đây.

### Tắt ở đâu — đã chốt

🛑 **Ngưỡng này là luật của component, không phải việc của nơi gọi.** Nơi gọi chịu trách nhiệm nghĩa là sẽ có nơi quên, và không có gì báo.

Thư viện không có công tắc theo điểm ngắt, nên tắt bằng **CSS**: lớp bọc gắn một lớp riêng lên hộp nổi qua tuỳ chọn `styleClass` của thư viện, và một khối `@media` trong tệp gom override của thư viện ẩn hộp dưới `$bp-sm`. Đó là tệp duy nhất trong Core có sẵn `$bp-sm` dưới dạng biến SCSS thật, và đã có tiền lệ đúng khuôn này ở [`Dialog.md`](./Dialog.md) §Responsive — khuôn override bắt buộc ghi "vì sao / hỏng khi nào" nằm ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §4.4.

Hai lý do chọn đường CSS thay vì cho component tự nghe media query trong TypeScript:

1. **Ngưỡng chỉ khai một chỗ.** Nghe media query trong TypeScript đòi một bản sao của `$bp-sm` bằng số trong mã — đúng thứ [`.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §5 cấm, và bản sao đó sẽ không được sửa cùng lúc.
2. **Lời chú không biến mất khỏi trình đọc màn hình.** Tắt trong TypeScript thì không dựng gì cả, kể cả phần tử mô tả ẩn. Tắt bằng CSS thì người dùng trình đọc màn hình trên máy nhỏ vẫn nghe được — một cải thiện thật so với "tắt hẳn".

Cái giá: hộp vẫn được dựng và định vị rồi mới bị ẩn. Lãng phí nhỏ, và nó chỉ xảy ra khi có thiết bị trỏ trên màn nhỏ — `@media (hover: hover)` ở §Trạng thái đã chặn phần lớn ca chạm.

🛑 **Ẩn bằng `visibility`, không bằng `display` — đã chốt.** Lý do là một, và nó thuộc về thư viện chứ không phải sở thích: **thư viện ghi `display` nội tuyến lúc hiện hộp**, mà không độ ưu tiên chọn lọc nào thắng được một khai báo nội tuyến — đè nó cần `!important`. `visibility` thì thư viện không ghi, nên khối `@media` đè được bằng độ ưu tiên thường: một khối sạch, không `!important`, không phụ thuộc thứ tự nạp stylesheet.

⚠️ **Và một lý do KHÔNG được viện tới, vì nó sai:** `visibility: hidden` **không** giữ hộp lại trong cây accessibility — nó gỡ hộp khỏi đó y như `display: none`. Thứ giữ lời chú cho người dùng trình đọc màn hình là **phần tử mô tả riêng** ở §Accessibility, và phần tử đó cố ý nằm **ngoài** khối `@media` này. Ai đọc đoạn trên rồi kết luận "chọn `visibility` để trình đọc màn hình vẫn nghe được" là đã hiểu sai cả hai cơ chế; hai kênh tách nhau chính là thứ làm việc đó, không phải một từ khoá CSS.

Riêng ca `Sidebar` thu gọn thì không bị ảnh hưởng: dưới `$bp-md` sidebar đã chuyển sang dạng drawer và hiện nhãn đầy đủ.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | `role="tooltip"` trên phần tử mô tả; phần tử neo trỏ tới nó bằng `aria-describedby` — **không** bằng `aria-labelledby`. Phần tử mô tả **không** phải hộp nổi của thư viện — xem "Hai kênh tách nhau" dưới |
| Quan hệ với nhãn | Tooltip **mô tả thêm**, không thay nhãn. Phần tử neo phải tự có nhãn hợp lệ trước đã |
| Bàn phím | Hiện khi neo nhận focus, ẩn khi mất focus. `Escape` ẩn tooltip mà **không** làm mất focus khỏi phần tử neo. Cách nối đã chốt ở "Bàn phím" dưới |
| Nội dung tương tác | 🛑 Không bao giờ. Không liên kết, không nút. Chuột không đi tới đó được mà không rời vùng kích hoạt, nên nội dung bấm được trong tooltip là nội dung không bấm được |
| Con trỏ chạm qua | Tooltip không chặn sự kiện chuột (`pointer-events: none`), nếu không nó tự che mất chính phần tử vừa mở nó |
| Chuyển động | Hiện bằng `opacity` mờ dần, **thời lượng do thư viện quyết** — spec không đặt số. Ẩn **không có chuyển tiếp**: hộp biến mất ngay. Khai `prefers-reduced-motion: reduce` thì bỏ hẳn phần mờ dần. Đã chốt — xem "Thời lượng chuyển tiếp" dưới |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8. Chuỗi dịch dài ra ở ngôn ngữ khác vẫn phải vừa hai dòng |

### Hai kênh tách nhau — đã chốt

🛑 **Hộp nổi là kênh NHÌN; nó không tham gia quan hệ ARIA nào.** Kênh **nghe** là một phần tử mô tả riêng do lớp bọc tự vẽ:

| Phần tử | Ai vẽ | Mang gì |
| --- | --- | --- |
| Hộp nổi | Thư viện | Hình thức theo §Kích thước và §Trạng thái. Có sẵn `role="tooltip"`, nhưng không có `id` nên không ai trỏ tới được |
| Phần tử mô tả | Lớp bọc | Ẩn về mặt thị giác nhưng **còn trong cây accessibility** (lớp tiện ích `.sr-only` đã có ở stylesheet toàn cục). Mang `role="tooltip"`, một `id` tự sinh, và đúng chuỗi `text` |
| Phần tử neo | Nơi gọi, chiếu vào | Lớp bọc đặt `aria-describedby` trỏ tới `id` ở trên — 🛑 **chỉ khi phần tử chiếu vào chính nó là phần tử nhận focus.** Chiếu vào một component Core tự dựng phần tử nhận focus bên trong thì `describedBy` của component đó ghi, lớp bọc thôi ghi: xem "Phần tử neo là một component Core" ngay dưới |

**Vì sao không trỏ thẳng vào hộp nổi:** nó chỉ được dựng lúc hiện, gắn vào `body`, và không mang `id` — tuỳ chọn `id` có trong bảng mặc định của thư viện nhưng `create()` không truyền nó vào phần tử, nên không có đích ổn định. Chi tiết đã đối chiếu ở bảng đầu tệp.

Tách hai kênh mua thêm hai thứ, và phải nói ra cả cái giá:

- ✅ Lời chú tới được người dùng trình đọc màn hình **kể cả khi hộp nổi chưa bao giờ hiện** — đúng vai "mô tả thêm" của dòng *Quan hệ với nhãn*, và là lý do §Responsive tắt được phần nhìn dưới `$bp-sm` mà không lấy mất thông tin của ai.
- ✅ Core không phải thò tay vào DOM của thư viện, nên không hỏng ở lần nâng cấp kế tiếp.
- ⚠️ Cái giá: chuỗi `text` có mặt hai lần trong cây accessibility khi tooltip đang hiện. Hộp nổi không được phần tử nào trỏ tới và không phải vùng `aria-live`, nên trình đọc màn hình không tự đọc nó lên; người dùng duyệt tuần tự qua cuối `body` thì vẫn gặp. Chấp nhận được, và **không** sửa bằng cách đặt `aria-hidden` lên hộp — làm thế là thò tay vào DOM của thư viện, đúng thứ gạch đầu dòng trên vừa tránh.

### Phần tử neo là một component Core — đã chốt

🛑 **`aria-describedby` phải nằm trên phần tử NHẬN FOCUS, không trên phần tử được bọc.** Hai thứ đó là một khi nội dung chiếu vào là `<button>` hay `<span tabindex="0">`. Chúng **không** là một khi nội dung chiếu vào là một component Core tự dựng control bên trong — [`Check.md`](./Check.md), [`Input.md`](./Input.md), [`Autocomplete.md`](./Autocomplete.md): thuộc tính rơi lên host của component, còn focus ở `<input>` bên trong, và trình đọc màn hình không đọc gì cả.

Bốn khoản dưới đây là hợp đồng, người dựng phải khớp cả bốn:

| Khoản | Luật |
| --- | --- |
| `id` phần tử mô tả | Là **bề mặt công khai** của `Tooltip` — nơi gọi đọc được nó qua một tham chiếu template |
| Ai ghi thuộc tính | Component nào tự dựng phần tử nhận focus bên trong thì **chính nó** ghi, qua input `describedBy` của nó. Lớp bọc **thôi** ghi lên host của component đó. 🛑 Hai nơi cùng ghi một thuộc tính là hai nơi sẽ giẫm lên nhau, và bên thua không cố định |
| Nhiều nguồn mô tả | `describedBy` nhận **danh sách `id` cách nhau bằng dấu cách**. Thứ tự trong `aria-describedby` là thứ tự đọc, nên `id` dòng lỗi và dòng gợi ý của [`FormRow.md`](./FormRow.md) đứng **trước**, `id` lời chú đứng **sau** — thứ đang chặn người dùng đi tiếp phải được đọc trước |
| Quên nối | Phải **báo lúc phát triển**, không im lặng. Cách dò do người dựng chọn; spec chỉ đòi nó không im — cùng lý do với ràng buộc "đúng MỘT phần tử chiếu vào" ở §API dự kiến |

Cái giá, nói thẳng: nơi gọi phải nhớ thêm một bước mỗi lần bọc một component Core. Đó chính là lý do khoản thứ tư không bỏ được — một lời chú rơi khỏi trình đọc màn hình là thứ **không ai nhìn thấy**, kể cả người vừa dựng xong màn và bấm thử.

### Bàn phím — đã chốt

Lớp bọc nghe **`focusin` / `focusout` trên chính host**, không dùng đường `focus` của thư viện. `focusin`/`focusout` nổi bọt nên phần tử neo chiếu vào nhận focus thì lớp bọc biết; `focus`/`blur` thì không, và đó là lý do đường của thư viện không dùng được ở hình dạng bọc (bảng đầu tệp).

Hai hệ quả bắt buộc:

1. **Giữ `tooltipEvent` của thư viện ở `'hover'`** (đúng mặc định của nó) để thư viện không gắn thêm một cặp `focus`/`blur` lên host — hai đường cùng mở một hộp là hai đường sẽ lệch nhau ở ca biên.
2. **`Escape` do lớp bọc tự lo.** Thư viện chỉ đăng ký lắng nghe `Escape` bên trong nhánh mở có độ trễ, mà đường bàn phím cố ý không đi qua nhánh đó (§Trạng thái: bàn phím hiện ngay). Lớp bọc đăng ký và gỡ lắng nghe của riêng nó.

### Thời lượng chuyển tiếp — đã chốt

🛑 **Spec KHÔNG đặt thời lượng hiện/ẩn. Thư viện quyết, Core nhận.** Người dùng chốt ngày 2026-09-23, sau khi đối chiếu mã thư viện đang cài.

Ba khoản của luật này, đọc như một:

| Chiều | Luật | Vì sao là thế |
| --- | --- | --- |
| Hiện | Mờ dần bằng `opacity`, **thời lượng của thư viện** | Vòng mờ dần chạy bằng `requestAnimationFrame` ghi `opacity` **nội tuyến**, không đọc CSS. Con số là đối số truyền thẳng trong mã thư viện, không phải một tuỳ chọn — không input nào, không biến CSS nào với tới |
| Ẩn | **Không có chuyển tiếp.** Hộp biến mất ngay | Đường ẩn của thư viện gỡ hộp khỏi DOM bằng một lời gọi đồng bộ. Không có gì để làm chậm lại, kể cả bằng CSS: phần tử không còn ở đó nữa |
| `prefers-reduced-motion: reduce` | **Vẫn bắt buộc bỏ hẳn phần mờ dần** | Khoản duy nhất trong ba khoản mà Core thắng được, và nó **đang đạt**. Đè được vì một khai báo `!important` thắng khai báo nội tuyến mà vòng mờ dần ghi ra — xem bảng đầu tệp. Cơ chế chung (khối giảm chuyển động toàn cục với tới tới đâu) ở [`../DESIGN.md`](../DESIGN.md) §7; đây là ca chứng minh của nó |

**Vì sao không đổi nền để lấy lại quyền đặt thời lượng.** Đổi sang tự dựng thì thời lượng lại là CSS và `--dur-fast` áp được. Cái giá bị từ chối: hệ sẽ có **hai nền lớp nổi song song** — một lớp nổi bọc thư viện cho [`Menu.md`](./Menu.md), [`Dialog.md`](./Dialog.md), [`Drawer.md`](./Drawer.md), và một lớp nổi tự dựng chỉ cho tooltip. Hai cơ chế định vị, hai cách xử lý cuộn trang, hai chỗ hỏng khi nâng cấp. Một chênh lệch thời lượng mà người dùng không đo được bằng mắt không mua nổi cái giá đó.

⚠️ **Đây là ngoại lệ của [`../CLAUDE.md`](../CLAUDE.md) §5, không phải một lần lật chiều cập nhật.** Chiều "`Design/` quyết, code áp" vẫn đứng cho mọi khoản còn lại của tệp này. Riêng khoản thời lượng, spec **nhường quyền** — và nhường một cách công khai, vì cách nói dối rẻ nhất ở chỗ này là giữ nguyên `--dur-fast` trong bảng token rồi để mọi người tưởng nó có hiệu lực.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `text` | input | `string` | — | **Bắt buộc.** Rỗng thì không dựng gì — xem trạng thái `empty` |
| `variant` | input | `'label' \| 'hint'` | `'label'` | Mặc định là dạng ngắn nhất, sai theo hướng an toàn |
| `position` | input | `'top' \| 'bottom' \| 'left' \| 'right'` | `'top'` | Chỉ là **hướng ưu tiên**; component tự lật khi không đủ chỗ |
| `delay` | input | `number` | `320` | Mili-giây, **chỉ áp cho chuột**. Bàn phím luôn hiện ngay, không nhận giá trị này. Vì sao là một con số chứ không phải một token: xem dưới |
| `disabled` | input | `boolean` | `false` | Tắt tooltip mà không phải gỡ nó khỏi template |

Không có output. Tooltip không phát sự kiện nào ra ngoài — nó không phải một điểm tương tác.

🛑 **Không có input `for`.** Phần tử neo đi vào bằng nội dung chiếu, không bằng một tham chiếu template — xem ngay dưới.

`Tooltip` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

### Hình dạng — bọc, không phải `for`. Đã chốt

Phần tử neo là **nội dung chiếu vào** lớp bọc:

```text
<app-tooltip [text]="…">
  <button …>…</button>
</app-tooltip>
```

**Vì sao không phải `<app-tooltip [for]="neo">`:** §Nền đã chốt bọc thư viện, và thư viện chỉ vào được bằng cách gắn directive của nó lên **chính host** của lớp bọc. Một input `for` đòi gắn directive lên một phần tử mà lớp bọc không sở hữu — Angular không cho, nên `for` đồng nghĩa với việc bỏ thư viện và tự viết lại phần định vị lớp nổi, đúng nhóm hành vi mà [`../COMPONENTS.md`](../COMPONENTS.md) §4 xếp là "khó" và cố ý đi bọc.

Ba ràng buộc đi kèm, cả ba đều là hợp đồng người dựng phải khớp:

| Ràng buộc | Vì sao |
| --- | --- |
| **Đúng MỘT phần tử chiếu vào.** Không phải một text node trần, không phải nhiều phần tử anh em | §Accessibility phải đặt `aria-describedby` lên một phần tử thật và xác định được. Không đúng một phần tử thì lớp bọc **không dựng gì** và báo lỗi lúc phát triển — im lặng bỏ qua là cách một tooltip biến mất mà không ai biết |
| **Host có hộp riêng, không dùng `display: contents`** | Thư viện căn vị trí bằng hình chữ nhật của chính host. Phần tử `display: contents` không sinh hộp, nên hình chữ nhật đó rỗng và hộp nổi rơi về góc màn hình. Chốt `inline-flex`: ôm sát nội dung, không đổi chiều dòng chữ |
| **Nơi gọi biết là có một hộp chen vào** | Hệ quả trực tiếp của dòng trên |

Cái giá của ràng buộc thứ ba, nói thẳng: ca "ô bảng đã bị cắt bằng dấu ba chấm" ở §Khi nào dùng không còn bọc được một cách vô tư — `text-overflow: ellipsis` cần chính phần tử mang chữ là phần tử bị cắt, nên phần tử bên trong phải tự đặt `min-width: 0` và `max-width: 100%`. Ba nơi gọi hôm nay (bảng đầu tệp) không dính ca này. Ca đó sẽ dính khi [`DataTable.md`](./DataTable.md) dựng cờ cắt theo cột — câu hỏi #3 ở §Cần chốt, và một phần lý do của câu hỏi #1 ở đó.

### `delay` là tham số hành vi, không phải token chuyển động

**Mặc định là `320` mili-giây, quyết ở chính tệp này; code khai đúng một hằng số có tên và trỏ về đây.** Không token, không `getComputedStyle`.

Ba lý do:

1. **Giá trị này không bao giờ xuất hiện trong CSS.** Nó đi thẳng vào TypeScript. Cho nó một CSS custom property chỉ để TypeScript đọc ngược lại bằng `getComputedStyle` là một vòng tròn không mua được gì, và trong `src/FE` chưa có mã sản phẩm nào đọc token lúc chạy.
2. **`--dur-slow` đang mang một nghĩa khác.** [`../DESIGN.md`](../DESIGN.md) §7 giao nó cho *thời lượng một chuyển tiếp chậm*; mượn nó làm *độ trễ chờ trước khi hiện* là hai nghĩa trên một giá trị. Ai chỉnh lại nhịp bay vào của [`Toast.md`](./Toast.md) sẽ đổi luôn độ trễ tooltip mà không biết mình vừa đổi.
3. **Chiều cập nhật không đổi.** [`../CLAUDE.md`](../CLAUDE.md) §5: `Design/` quyết, code áp. Con số quyết ở đây; hằng số trong code chỉ là chỗ áp, đúng như một tên token là chỗ áp.

Thời lượng **chuyển tiếp** hiện/ẩn là một thứ khác hẳn, và nay **không** là token nào cả: thư viện quyết nó, spec không đặt số (§Accessibility, "Thời lượng chuyển tiếp"). Nên `delay` và thời lượng chuyển tiếp không chỉ khác nghĩa — chúng còn khác **chủ**: `delay` do tệp này quyết, thời lượng chuyển tiếp thì không.

## Do / Don't

- ✅ Coi tooltip là thứ **thêm vào**, không phải thứ mang thông tin duy nhất.
- ✅ Hiện ngay khi nhận focus bàn phím; chỉ chuột mới phải chờ.
- ✅ Giữ nội dung trong hai dòng.
- ✅ Luôn có mũi nhọn chỉ về phần tử neo.
- ❌ Không dùng tooltip làm nhãn cho `IconButton`.
- ❌ Không đặt liên kết, nút, hay bất cứ thứ gì bấm được vào trong.
- ❌ Không tô màu trạng thái cho tooltip.
- ❌ Không hiện tooltip cho phần tử mà nhãn của nó đã nói đúng điều đó — trình đọc màn hình sẽ đọc hai lần.
- ❌ Không đặt thông tin bắt buộc phải biết vào tooltip: người dùng di động sẽ không bao giờ thấy nó.
- ❌ Không chiếu nhiều phần tử, hay chữ trần, vào `app-tooltip` — §API đòi đúng một phần tử.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | `320`ms có đúng là độ trễ tốt không? Ngắn quá thì tooltip nhảy ra khi con trỏ chỉ đi ngang; dài quá thì người dùng bỏ cuộc trước khi nó kịp hiện. Con số hiện tại kế thừa từ `--dur-slow` chứ chưa ai đo trên ứng dụng chạy | Khi có ứng dụng chạy để thử |
| 2 | Ca "ô bảng cắt ba chấm" ghép vào [`DataTable.md`](./DataTable.md) ra sao khi hộp `inline-flex` của lớp bọc chen vào giữa ô và chữ? Hoặc cột tự đặt `min-width: 0` / `max-width: 100%`, hoặc `DataTable` không đi qua `app-tooltip` cho ca này | Khi dựng cờ cắt theo cột của `DataTable`. Không còn phụ thuộc câu hỏi nền — nền đã chốt giữ nguyên 2026-09-23, nên ràng buộc "host có hộp riêng" là ràng buộc cố định phải thiết kế quanh nó, không phải thứ chờ một quyết định khác gỡ hộ |
| 3 | Có cần một component `Popover` riêng cho nội dung giàu và bấm được không? Hôm nay chưa có, và mọi nhu cầu kiểu đó đang bị đẩy sang `Dialog` — chấp nhận được nhưng nặng tay với những ca nhỏ | Sau F3 — dự án hạ nguồn đầu tiên có nhu cầu thật |

**Hai câu hỏi đã đóng ngày 2026-09-23**, ghi lại đây để người đọc sau không mở lại chúng như thể chưa ai xét: **nền bọc thư viện hay tự dựng** (chốt: giữ bọc — lý do ở dòng `Nền` đầu tệp) và **thời lượng hiện/ẩn có theo `--dur-fast` không** (chốt: không, thư viện quyết — luật ở §Accessibility, "Thời lượng chuyển tiếp"). Cả hai đóng bằng quyết định của người dùng, không bằng một thay đổi code.
