---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Dialog

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE`, bọc `primeng/dialog`. Bảng dưới đây khai đúng những mục đã mở source ra so.

**Nền:** **bọc PrimeNG**. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4, bẫy focus và khôi phục focus là ca "khó" điển hình: làm đúng toàn bộ ca biên (Tab vòng lại, Shift+Tab ngược, phần tử xuất hiện động, Escape, khoá cuộn nền, iframe bên trong) là hàng trăm dòng và rất nhiều lần sai.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/ui/dialog/dialog.component.ts` — selector `app-dialog`, `styleClass` gắn `.app-dialog` và `.app-dialog--<cỡ>`. Phần chạm DOM do PrimeNG dựng nằm ở `src/FE/src/styles/_thu-vien.scss` (khối `.app-dialog`), ngoài encapsulation của component.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| Nơi duy nhất import `primeng/dialog` | Đúng — chú thích đầu file khai vai trò này | Chưa tự kiểm bằng lệnh là không còn chỗ nào khác import |
| API | `open`, `size`, `title` (`input.required`), `description`, `closable`, `dismissOnBackdrop`, `dirty`, `loading`, `loadingBlocksClose`, `errorTemplate`; output `closed`, `dismissAttempted` | — |
| Bốn cỡ bề rộng | Có, ở `_thu-vien.scss`: `sm`/`md`/`lg` dùng `min(94vw, --layout-dialog-w-*)`, `full` trừ `--sp-8` mỗi mép | — |
| Đệm đầu / thân / chân `--sp-6` | Có cả ba (`.p-dialog-header`, `.p-dialog-content`, `.p-dialog-footer`) | — |
| Khe giữa các nút ở chân `--sp-4` | Có, `.p-dialog-footer` là flex căn phải, `gap: var(--sp-4)` | — |
| Khe tiêu đề → mô tả `--sp-3` | Có, `.app-dialog__dau` `gap: var(--sp-3)` | — |
| Hình thức tiêu đề, mô tả | Tiêu đề `--fs-lg` `--fw-bold` `--lh-tight` `--color-text`; mô tả `--fs-sm` `--lh-normal` `--color-text-muted` | — |
| Tiêu đề là `<h2>` thật | Có | — |
| Bẫy focus, khoá cuộn nền | Có, `[focusTrap]`, `[focusOnShow]`, `[blockScroll]`, `[modal]` — đúng lý do §Nền chọn bọc thư viện | Việc `closed` phát **sau** khi focus đã về nơi mở (chú thích trong code khẳng định) chưa kiểm được — cần chạy thật |
| `dirty` chặn đóng | Có, đúng luật: `onVisibleChange` giữ dialog mở rồi phát `dismissAttempted`, **không** tự vẽ hộp hỏi — trang cha ghép `ConfirmDialog` | — |
| `loadingBlocksClose` | Có: `khoaDong` tắt `closeOnEscape` và chặn cả backdrop khi đang gửi dữ liệu; `loading` thường thì Escape **vẫn đóng được** — khớp đúng §Trạng thái | — |
| `closable` / `dismissOnBackdrop` | Có, `maskDongDuoc` ghép cả hai trước khi truyền `dismissableMask` | — |
| `aria-busy` khi `loading` | Có | — |
| Nhãn nút đóng qua i18n | Có, `closeAriaLabel` nhận `chung.dong` | — |
| `errorTemplate` ở đầu thân | Có, `.app-dialog__loi` đặt **trên** `<ng-content />` | **Không cuộn thân lên đầu** khi template chuyển từ `null` sang khác `null` — §Trạng thái đòi, không có gì trong code làm việc đó |
| Lớp phủ khi `loading` | Có, `.app-dialog__phu-tai` phủ `--color-scrim`, `pointer-events: none`, `aria-hidden` | **Không có spinner.** §Trạng thái khai "phủ `--color-scrim` **+ spinner**"; lớp phủ hiện chỉ là một mảng màu |
| Chiều cao tối đa | Chỉ có ở nhánh màn nhỏ (`max-height: 90vh` dưới `$bp-sm`) | Không thấy quy tắc nào cho màn thường. §Kích thước đòi `min(viewport − --sp-9 * 2, chiều cao nội dung)` với thân cuộn còn đầu/chân đứng yên — chưa kiểm được là preset PrimeNG có tự lo hay chưa dựng |
| `draggable` / `resizable` / `maximizable` | Tắt tường minh (`[draggable]="false"`, `[resizable]="false"`) | Chưa dựng — chú thích component khai không màn nào cần. Chưa đối chiếu với spec xem ba thứ này có được khai là bắt buộc không |
| Dính đáy dưới `$bp-sm` | Có, đúng §Responsive: rộng hết màn, dính đáy, chỉ bo hai góc trên, chân xếp dọc ngược | — |
| Bốn trạng thái "không áp dụng" | Đúng — không có quy tắc `hover`/`focus`/`active`/`disabled` nào ở mức hộp | — |
| §Trạng thái, đoạn "Mở và đóng" — thời lượng và đường cong (đối chiếu 2026-09-23, chỉ đọc mã) | Thư viện chạy vào/ra bằng hoạt ảnh Angular, tham số lấy từ **một** input duy nhất: `primeng/dialog` § `transitionOptions = '150ms cubic-bezier(0, 0, 0.2, 1)'`, và cùng chuỗi đó đi vào **cả hai** chiều — § `params: { transform: transformOptions, transition: transitionOptions }` dùng chung cho `showAnimation` và `hideAnimation`. Lớp bọc Core chưa truyền gì vào input đó, nên mặc định của thư viện là thứ đang chạy | Truyền vào `transitionOptions` một chuỗi dựng từ **một hằng số có tên** phản chiếu `--dur-base`, kèm `--ease-standard`. Chuỗi đó là đối số hoạt ảnh Angular nên **không giải `var()`**: token quyết giá trị, hằng số TypeScript chỉ là chỗ áp — [`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) quyết định 3 và 4, ép bằng luật **F37** ([`../../RULES.md`](../../RULES.md) §7). Một input chở được đúng một bộ giá trị, và §Trạng thái nay chỉ đòi một bộ — xem đoạn "Mở và đóng" |
| Backdrop — "mờ dần cùng lúc" (đối chiếu 2026-09-23, chỉ đọc mã) | **Chưa cùng lúc.** Backdrop đi đường khác hẳn hộp: CSS thuần, `@primeuix/styles/base` § `.p-overlay-mask-enter` chạy `animation: p-overlay-mask-enter-animation dt('mask.transition.duration') forwards`. Khoá đó là `semantic.mask.transitionDuration`, và preset Aura cho nó một chuỗi trần **riêng** — nó không đi theo khoá thời lượng chung của thư viện | Khai `semantic.mask.transitionDuration` trong `definePreset` ở `src/FE/src/app/core/theme/prime-preset.ts`, giá trị là **cùng token với hộp** (`--dur-base`). Backdrop là biến CSS nên token tới thẳng được, không cần hằng số. 🛑 Khai **cùng lượt** với hằng số của hộp: nối lẻ backdrop về một token khác làm khoảng lệch giữa hai thứ **rộng ra**, không hẹp lại |

Chưa đối chiếu: nền/viền/bo góc/bóng ở trạng thái `default` (`--color-surface`, `--color-border`, `--radius-lg`, `--shadow-4`) và màu backdrop `--color-overlay` — những giá trị này đến từ preset PrimeNG ở `src/FE/src/app/core/theme/prime-preset.ts`, chưa soát từng cái. Cũng chưa đối chiếu §Accessibility ngoài bẫy focus và nhãn nút đóng.

---

## Mục đích

Một lớp nổi **chặn** buộc người dùng xử lý xong rồi mới quay lại trang.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Form tạo/sửa một bản ghi ngắn | 🛑 Hỏi xác nhận một hành động → [`ConfirmDialog.md`](./ConfirmDialog.md), nó có đúng hai nút và có mức nguy hiểm |
| Xem chi tiết nhanh mà không rời màn danh sách | 🛑 Báo kết quả một thao tác → [`Toast.md`](./Toast.md). Dialog bắt người dùng bấm để đóng; một thông báo thành công không đáng bị thế |
| Một bước cần tập trung tuyệt đối | 🛑 Thông tin luôn hiện, gắn với bối cảnh trang → [`NoticeBanner.md`](./NoticeBanner.md) |
| | 🛑 Form dài nhiều bước → dùng một trang riêng. Dialog cuộn nội bộ ở màn nhỏ là trải nghiệm tệ |
| | 🛑 Menu hoặc chọn nhanh → lớp nổi không chặn, không thuộc component này |

**Ngưỡng quyết định giữa dialog và trang riêng:** nếu form có trên khoảng bảy trường, hoặc cần cuộn ở màn desktop, hoặc người dùng cần tra cứu thông tin ở trang nền để điền — thì đó là một trang, không phải một dialog. Ba dấu hiệu này là ngưỡng, bất kỳ cái nào đúng là đủ; không có con số nào khác. Xem chi tiết mà vẫn cần thấy danh sách phía sau là [`Drawer.md`](./Drawer.md), không phải một biến thể của `Dialog`.

## Biến thể

| Biến thể | Bề rộng | Dùng khi |
| --- | --- | --- |
| `sm` | `--layout-dialog-w-sm` | Form một tới ba trường; thông báo cần xác nhận có nội dung |
| `md` | `--layout-dialog-w-md` | **Mặc định.** Form thông thường |
| `lg` | `--layout-dialog-w-lg` | Form hai cột; nội dung có bảng |
| `full` | Gần hết viewport, chừa `--sp-8` mỗi bên | Chỉ khi nội dung thật sự cần — xem trước tài liệu, biên tập nội dung dài |

Bề rộng là **tối đa**. Ở màn hẹp hơn, dialog co lại và giữ khoảng hở `--sp-5` mỗi bên.

Bậc `sm` và `md` còn là bề rộng của [`AuthCard.md`](./AuthCard.md), và bậc `md` là bề rộng tối đa của khối form trong trang (`--layout-form-w`) — [`../DESIGN.md`](../DESIGN.md) §6.1. Đổi giá trị hai bậc này là đổi theo cả những chỗ đó.

Không có biến thể "không đóng được". Mọi dialog phải đóng được bằng Escape — xem §Accessibility.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Đệm phần đầu / thân / chân | `--sp-6` |
| Khe giữa tiêu đề và mô tả | `--sp-3` |
| Khe giữa các nút ở chân | `--sp-4` |
| Bo góc | `--radius-lg` |
| Bóng | `--shadow-4` |
| Chiều cao tối đa | `min(<chiều cao viewport> - --sp-9 * 2, <chiều cao nội dung>)` — phần thân cuộn, đầu và chân đứng yên |
| Lớp | `--z-dialog`; backdrop ở `--z-backdrop` |

**Phần đầu và phần chân không cuộn.** Nội dung dài mà nút "Lưu" trôi khỏi tầm nhìn là cách chắc chắn để người dùng tưởng dialog không có nút lưu.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`; viền `--color-border`; `--radius-lg`; `--shadow-4`. Backdrop `--color-overlay` phủ toàn màn | Có |
| `hover` | **Không áp dụng ở mức dialog.** Hover thuộc các control bên trong | — |
| `focus-visible` | **Không áp dụng ở mức dialog** — bản thân hộp không nhận focus. Nhưng focus **bên trong** bị bẫy: xem §Accessibility | — |
| `active` | **Không áp dụng.** Dialog không phải control | — |
| `disabled` | **Không áp dụng.** Dialog không có trạng thái vô hiệu hoá; muốn chặn thao tác thì khoá từng control bên trong | — |
| `loading` | Phần thân phủ `--color-scrim` + spinner; các nút ở chân vào trạng thái `loading`; **Escape vẫn đóng được** trừ khi đang gửi dữ liệu — xem dưới. `aria-busy="true"` | Có |
| `error` | Phần thân hiện `errorTemplate` ở đầu, **trên** nội dung form; màn ghép [`NoticeBanner.md`](./NoticeBanner.md) vai `danger` vào đó. Khi template chuyển từ `null` sang khác `null`, phần thân cuộn lên đầu để người dùng thấy nó | Có |
| `empty` | **Không áp dụng.** Một dialog không có nội dung thì không nên mở | — |

**Escape trong lúc đang gửi dữ liệu:** đóng dialog lúc request đang bay khiến người dùng không biết thao tác thành công hay không. Luật: trong lúc `loading` **do một thao tác ghi**, Escape **không** đóng; thay vào đó không làm gì và giữ nguyên. Trong lúc `loading` do đang tải dữ liệu để hiển thị, Escape **đóng bình thường** — chưa có gì để mất.

**Mở và đóng — một thời lượng, một đường cong, cho cả hai chiều.** Vào bằng mờ dần + phóng nhẹ; ra bằng mờ dần. Cả hai chiều dùng `--dur-base` với `--ease-standard`. Backdrop mờ dần cùng lúc, **cùng token thời lượng** với hộp. Cả hai tắt khi `prefers-reduced-motion: reduce` ([`../DESIGN.md`](../DESIGN.md) §7).

**Vì sao một bộ giá trị chứ không phải hai.** Nền bọc thư viện chở chuyển động vào/ra qua **một** input duy nhất (§Đã có → còn thiếu), nên "vào chậm, ra nhanh" là một ý đồ nền này không dựng được — [`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) quyết định 5. Hai token hướng — `--ease-decelerate` cho thứ đi vào, `--ease-accelerate` cho thứ rời đi — vì thế không dùng được ở đây: một đường cong phải phục vụ cả hai hướng, và `--ease-standard` là token [`../DESIGN.md`](../DESIGN.md) §7 giao đúng vai đó. Thời lượng thì không phải chọn: §7 đã giao `--dur-base` cho việc mở/đóng `Dialog`.

🛑 **Giá trị này đi vào TypeScript, không vào CSS.** Xem §Token dùng.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-text`, `--color-text-muted`, `--color-border`, `--color-overlay`, `--color-scrim`, `--color-focus` |
| Chữ | `--fs-lg`, `--fs-sm`, `--fw-bold`, `--fw-regular`, `--lh-tight`, `--lh-normal` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-5`, `--sp-6`, `--sp-8`, `--sp-9` |
| Hình dạng | `--radius-lg`, `--border-w` |
| Kích thước | `--layout-dialog-w-sm`, `--layout-dialog-w-md`, `--layout-dialog-w-lg` |
| Bóng | `--shadow-4` |
| Lớp | `--z-dialog`, `--z-backdrop` |
| Chuyển động | `--dur-base`, `--ease-standard` |

🛑 **Hai token chuyển động ở trên đi vào TypeScript, không vào CSS — và chúng vẫn ở lại bảng này.** `Dialog` giữ quyền đặt số; chỗ **áp** số thì khác mọi dòng còn lại của bảng: không phải một khai báo `var(--dur-base)` trong stylesheet mà một hằng số có tên cạnh lớp bọc, vì input nhận chuỗi tham số hoạt ảnh Angular và chuỗi đó bị phân tích thành số trước khi có ai giải `var()`. Hằng số đó phải nêu **đích danh** tên token nó phản chiếu — luật **F37** ([`../../RULES.md`](../../RULES.md) §7). Đây **không** phải ca [`Tooltip.md`](./Tooltip.md): ở đó spec nhường hẳn quyền đặt số vì thư viện không nhận số nào; ở đây spec vẫn quyết, chỉ là quyết ở một chỗ áp khác.

⚠️ Riêng backdrop thì `--dur-base` đi bằng CSS thật, qua `semantic.mask.transitionDuration` của preset. Cùng token, hai đường áp — ai đổi giá trị phải sửa cả hai.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Dialog căn giữa cả hai chiều; bề rộng theo biến thể |
| `$bp-sm` … `$bp-md` | Bề rộng co theo viewport, chừa `--sp-5` mỗi bên; căn giữa dọc |
| < `$bp-sm` | Dialog **dính đáy** và chiếm hết bề rộng, bo góc chỉ ở hai góc trên; nhóm nút ở chân xếp dọc, nút chính lên trên |

**Vì sao ở màn nhỏ dialog dính đáy chứ không căn giữa:** bàn phím ảo mở lên chiếm nửa dưới màn hình. Một dialog căn giữa sẽ bị đẩy lên và cắt mất phần đầu. Dính đáy thì dialog trượt lên cùng bàn phím và giữ được ô đang gõ trong tầm nhìn.

## Accessibility

Đây là phần nặng nhất của component này, và cũng là lý do nó bọc thư viện.

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | `role="dialog"` + `aria-modal="true"` |
| Nhãn | `aria-labelledby` trỏ tới id của tiêu đề. Không có tiêu đề nhìn thấy thì dùng `aria-label` |
| Mô tả | `aria-describedby` trỏ tới đoạn mô tả, nếu có |
| **Bẫy focus** | Tab và Shift+Tab **chỉ đi vòng trong dialog**. Không thoát ra được nền phía sau |
| **Focus lúc mở** | Đặt vào phần tử tương tác **đầu tiên có ý nghĩa** — thường là ô nhập đầu, hoặc chính hộp dialog nếu chỉ có chữ. 🛑 **Không** đặt vào nút đóng: người dùng bàn phím sẽ vô tình bấm Enter và đóng ngay |
| **Focus lúc đóng** | Trả về **đúng phần tử đã mở dialog**. Không trả về là người dùng bàn phím bị ném về đầu trang |
| Escape | Đóng dialog (trừ ca đang gửi dữ liệu ở §Trạng thái) |
| Bấm ra ngoài | Đóng — **trừ khi form đã có thay đổi chưa lưu**. Lúc đó `Dialog` phát `dismissAttempted` và trang cha hỏi xác nhận. Mất mười phút gõ vì một cú bấm hụt là lỗi không tha thứ được |
| Khoá cuộn nền | Nền không cuộn khi dialog mở. 🛑 Nhưng **phải giữ vị trí cuộn** — nhiều cách khoá cuộn làm trang nhảy về đầu khi đóng |
| Nội dung nền | Nội dung phía sau mang `inert` hoặc `aria-hidden="true"` để trình đọc màn hình không đọc xuyên qua |
| Nút đóng | Ở góc phải phần đầu, icon `pi-times` ([`../Icons.md`](../Icons.md) §5), có `aria-label`. Là phần cấu trúc của lớp bọc: thư viện vẽ, tạo hình bằng token theo hình thức của [`IconButton.md`](./IconButton.md) — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 |
| Chữ | Tiêu đề, mô tả, nhãn nút qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Không lồng dialog trong dialog.** Tầng lồng nhau ở [`../COMPONENTS.md`](../COMPONENTS.md) §2.4 đặt `Dialog` ở tầng cao nhất, nên nó không được chứa một `Dialog` khác. Ngoại lệ duy nhất là [`ConfirmDialog.md`](./ConfirmDialog.md) hỏi xác nhận cho một thao tác phát sinh từ dialog đang mở — và ngay cả khi đó, hai lớp là tối đa tuyệt đối. Bẫy focus lồng ba tầng gần như luôn hỏng.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `open` | input | `boolean` | `false` | |
| `size` | input | `'sm' \| 'md' \| 'lg' \| 'full'` | `'md'` | |
| `title` | input | `string` | — | Bắt buộc. Không có mặc định |
| `description` | input | `string \| null` | `null` | |
| `closable` | input | `boolean` | `true` | `false` chỉ ẩn nút đóng và chặn bấm-ra-ngoài; **Escape vẫn hoạt động** |
| `dismissOnBackdrop` | input | `boolean` | `true` | Tự động thành `false` khi `dirty` là `true` |
| `dirty` | input | `boolean` | `false` | Form đã có thay đổi chưa lưu. Bật cờ này thì bấm ra ngoài và Escape **không đóng** mà phát `dismissAttempted` |
| `loading` | input | `boolean` | `false` | |
| `loadingBlocksClose` | input | `boolean` | `false` | Bật khi `loading` là do một thao tác ghi |
| `errorTemplate` | input | `TemplateRef<unknown> \| null` | `null` | Khối lỗi ở đầu phần thân. Màn chỉ truyền khi đang lỗi — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 |
| `closed` | output | `void` | — | Phát sau khi đã trả focus về nơi mở |
| `dismissAttempted` | output | `void` | — | Phát khi người dùng cố đóng lúc `dirty`. Trang cha mở [`ConfirmDialog.md`](./ConfirmDialog.md) — `Dialog` ở tầng bọc nên không tự dựng hộp hỏi |

Nội dung vào qua ba slot: phần đầu (mặc định là tiêu đề + mô tả), phần thân, phần chân. Slot chân **không có nút mặc định** — mỗi dialog tự khai nút của mình, vì thứ tự và nhãn nút là quyết định của từng ca.

`Dialog` là component **dumb**: nó không tự đóng mình. Nó **phát** `closed` và trang cha đặt `open` về `false`. Tự đóng làm trạng thái nằm ở hai nơi và chúng sẽ lệch.

## Do / Don't

- ✅ Luôn có tiêu đề, và tiêu đề nói **việc gì đang xảy ra**: "Sửa người dùng", không phải "Biểu mẫu".
- ✅ Đặt focus vào ô nhập đầu tiên khi mở.
- ✅ Trả focus về nơi mở khi đóng.
- ✅ Bật `dirty` khi form có thay đổi.
- ✅ Nút chính bên phải ở desktop, lên trên ở màn nhỏ.
- ❌ Không lồng quá hai lớp dialog.
- ❌ Không nhồi form dài vào dialog. Dùng một trang.
- ❌ Không đặt focus vào nút đóng khi mở.
- ❌ Không chặn Escape, kể cả khi `closable` là `false`.
- ❌ Không dùng dialog để báo thành công.

## Cần chốt

Không còn.
