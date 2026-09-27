---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Toast

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có thật ở `src/FE/src/app/shared/ui/toast/` — selector `app-toast`, class `ToastComponent`, đặt một lần ở gốc trong `src/FE/src/app/app.html`. Bảng dưới khai **đúng những mục đã mở `toast.component.ts`, `toast.component.html`, `toast.component.spec.ts` và `src/FE/src/app/core/toast/toast.service.ts` ra so**; mục không có tên trong bảng thì chưa ai đối chiếu. Lượt đầu là 2026-09-20, nhưng **dòng nào so lại sau đó mang ngày của chính nó** trong ô "Có thật hôm nay" — không có lượt nào quét lại cả bảng, nên đừng đọc một ngày duy nhất cho mọi dòng. Lớp bọc vẫn **mỏng**, nhưng không còn trần: template là `<p-toast>` mang đúng hai binding thời lượng chuyển động (dòng cuối bảng), và thư mục component **không có tệp `.scss` nào** — nên gần như toàn bộ §Kích thước, §Responsive và phần hình thức của §Trạng thái còn ở dạng mặc định của thư viện.

| Mục trong spec | Có thật hôm nay (neo bằng chuỗi trong `src/FE`) | Sẽ thành |
| --- | --- | --- |
| §Nền — bọc PrimeNG | `toast.component.ts` import `Toast` từ `primeng/toast` và `MessageService` từ `primeng/api`; `toast.component.html` là **một** thẻ `<p-toast`, mang đúng hai binding `[showTransitionOptions]` và `[hideTransitionOptions]` — xem dòng cuối bảng | Giữ nguyên |
| §API — tách hàng đợi (smart, `core/`) khỏi component vẽ | Hàng đợi là `ToastService` ở `core/toast/toast.service.ts`, giữ trạng thái bằng `signal` và phơi ra `latest`; `ToastComponent` **không** giữ mảng thông báo nào | Giữ nguyên |
| `core/` không được biết thư viện UI | `toast.service.ts` không import gì từ `primeng/`; chỗ nối hai bên là `effect()` trong constructor của `ToastComponent` | Giữ nguyên |
| Lớp `--z-toast` cao nhất | Token `--z-toast` **có khai** trong `src/FE/src/styles/_tokens.scss`, trên `--z-dialog` và `--z-popover` | Giữ nguyên ở tầng token. Việc `p-toast` **dùng** token đó thì **chưa so** — xem ghi chú dưới bảng |
| Bốn vai và tên của chúng | **Lệch tên.** `toast.service.ts` khai `export type ToastSeverity = 'success' \| 'error' \| 'info' \| 'warn'`; spec khai `success` / `info` / `warning` / `danger` | Chốt một bộ tên rồi sửa bên còn lại — tên vai lọt ra tận API công khai của service |
| Thời gian tự tắt theo vai (4 / 5 / 8 giây) | **Đã có** (dựng 2026-09-23). `toast.component.ts` — `const TOAST_THOI_GIAN_TU_TAT_MS` giữ ba giá trị theo vai và `private thoiGianTuTat(` trả chúng vào `life` của lời gọi `add(...)`. Tên vai của spec ánh xạ sang tên severity của service ngay tại chú thích của hằng đó: `warning` ⇒ `warn`, `danger` ⇒ `error` | Giữ nguyên |
| 🛑 Toast lỗi **không** tự tắt | **Đã có** (dựng 2026-09-23, đóng chiều hỏng nguy hiểm). `toast.component.ts` — `thoiGianTuTat` trả `undefined` cho vai `error`, và lời gọi `add(...)` đặt `sticky: tuTatSau === undefined`. `sticky` là đường duy nhất tắt đồng hồ: `primeng/toast` bọc `setTimeout` trong `if (!this.message?.sticky)`, còn bên trong thì `this.message?.life \|\| this.life \|\| 3000` rơi về **3000** khi `life` rỗng — bỏ trống `life` KHÔNG đủ. Khoá bằng test: `toast.component.spec.ts` — `🛑 toast LỖI không tự tắt — sticky bật và KHÔNG có life` | Giữ nguyên |
| Ngăn xếp tối đa ba | **Chưa có** (so lại ngày 2026-09-22). `ToastService` nay giữ hàng đợi `_hangDoi = signal<readonly ToastMessage[]>([])`, phơi ra `hangDoi` và `layHet()`; `latest` vẫn còn nhưng chỉ để đọc. Hàng đợi đó **không** phải ngăn xếp đang hiện: `effect()` của `ToastComponent` gọi `this.toast.layHet()` rồi `this.messageService.add(...)` từng thông báo, nên hàng đợi rỗng ngay sau mỗi lượt vẽ. Không chỗ nào trong `toast.service.ts`, `toast.component.ts` hay `toast.component.html` cắt số lượng — số toast hiện cùng lúc do thư viện quyết | Đặt trần ba cho ngăn xếp đang hiện theo §Kích thước |
| API component: `items`, `position`, `dismissed`, `actionClicked` | **Chưa có cái nào.** `ToastComponent` không khai `input()` hay `output()` nào; nó `inject(ToastService)` và đọc thẳng. Hai thành viên `protected` khai trong class — `showTransitionOptions`, `hideTransitionOptions` — **cố ý không** mở thành `input()` (lý do ghi tại chỗ khai trong `toast.component.ts`), nên chúng không đếm vào §API dự kiến | Chốt lại: hoặc dựng đúng §API dự kiến, hoặc sửa spec cho khớp cách nối hiện tại |
| Kiểu `ToastItem` của `../../quy-uoc/fe-ui-conventions.md` §9 | **Không dùng.** Không tệp `.ts` nào dưới `src/FE/src` chứa chuỗi `ToastItem`; chỗ đó là `interface ToastMessage` tự khai trong `toast.service.ts`, với `traceId` mà §9 không có | Dùng kiểu gốc, hoặc chuyển chủ quyền kiểu |
| Nút hành động ("Hoàn tác") | **Chưa có.** `ToastMessage` không có trường nhãn hành động; template không vẽ nút nào | Dựng khi có endpoint hoàn tác — §API dự kiến đã ràng điều kiện đó |
| Hình thức: nền `--color-surface`, viền `--color-border`, `--radius-md`, `--shadow-4`, bề rộng `--layout-toast-w`, các bậc đệm | **Chưa dựng bằng token.** Thư mục `shared/ui/toast/` chỉ có `.ts`, `.html`, `.spec.ts` — không có `.scss`; hình thức tới từ preset `AuraToast` đăng ký trong `core/theme/prime-preset.ts` | Tạo hình theo §Kích thước bằng cơ chế theme của thư viện |
| §Responsive — ba ngưỡng, đảo lên trên ở màn nhỏ | **Chưa có.** Không có `.scss` và không có `position` nào được truyền cho `<p-toast />` | Dựng cả ba ngưỡng |
| Chữ qua i18n | **Một chỗ đang sai.** `effect()` dựng `detail` bằng nối chuỗi trực tiếp với `traceId`, không qua tầng dịch | Đưa chuỗi đó qua khoá dịch |
| §Trạng thái, đoạn vào/ra — bốn khoản `--dur-slow` · `--ease-decelerate` · `--dur-base` · `--ease-accelerate` | **Đã có, cả bốn** (dựng 2026-09-23, chỉ đọc mã). Lớp bọc **truyền cả hai input**: `toast.component.html` § `[showTransitionOptions]` và § `[hideTransitionOptions]`; `toast.component.ts` § `TOAST_SHOW_TRANSITION_OPTIONS` và § `TOAST_HIDE_TRANSITION_OPTIONS` ghép từ bốn hằng có tên — `TOAST_THOI_LUONG_VAO_MS`, `TOAST_DUONG_CONG_VAO`, `TOAST_THOI_LUONG_RA_MS`, `TOAST_DUONG_CONG_RA` — mỗi hằng nêu đích danh token nó phản chiếu ngay trong chú thích. Bốn giá trị khớp `src/FE/src/styles/_tokens.scss` § `--dur-slow`, § `--ease-decelerate`, § `--dur-base`, § `--ease-accelerate`. Hai mặc định của thư viện (`300ms ease-out` / `250ms ease-in`) **không còn** là thứ đang chạy | **Giữ nguyên — và bất đối xứng KHÔNG được gỡ.** Vào chậm hơn ra là cố ý (§Trạng thái); `Toast` là component duy nhất trong họ lớp nổi giữ được nó, vì nền cho **hai** input rời nhau. [`Dialog.md`](./Dialog.md) và [`Drawer.md`](./Drawer.md) chỉ có một input dùng chung hai chiều nên đã bỏ hẳn khoản này — "cho ba component trông giống nhau" bằng cách gỡ chỗ này là vứt một thứ đang chạy ([`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) quyết định 6). Vì sao bốn con số sống trong TypeScript chứ không trong CSS: §Token dùng — không nhắc lại ở đây. Còn nợ đúng một khoản của đoạn này: nhánh `prefers-reduced-motion`, xem ghi chú ⚠️ dưới bảng |

⚠️ Ba điều **chưa so**, và không so được từ bốn tệp trên: (1) các khoản §Accessibility — mức `aria-live` theo vai, `role`, vùng live tồn tại sẵn trong DOM, `pointer-events` của ngăn chứa, `aria-hidden` của icon — đều do `<p-toast />` tự vẽ, muốn kiểm phải mở DOM lúc chạy; (2) preset `AuraToast` có giải ra đúng các token ở §Token dùng hay không; (3) khối `@media (prefers-reduced-motion: reduce)` toàn cục ở `src/FE/src/styles/styles.scss` có **với tới** hoạt ảnh vào/ra của `<p-toast />` hay không — khối đó ép `animation-duration`/`transition-duration` của CSS, còn hai chuỗi tham số ở dòng cuối bảng đi qua bộ chạy hoạt ảnh của Angular. Ranh giới với tới của khối toàn cục — và ca đã chứng minh trong repo — ở [`../DESIGN.md`](../DESIGN.md) §7; không nhắc lại ở đây, và **`Toast` nằm bên khối đó KHÔNG với tới: hoạt ảnh vào/ra của nó là loại 2 của §7 đó** (kết luận 2026-09-24 bằng đọc mã, chưa bằng trình duyệt) — `primeng/toast` dựng chúng bằng `trigger()` của `@angular/animations`, và hai input ở dòng cuối bảng là đúng dấu hiệu §7 nêu cho loại 2. Nhánh giảm chuyển động của `Toast` vì thế là nửa provider: `src/FE/src/app/core/theme/core-animations.ts` § `chonProviderHoatAnh` cấp bộ chạy rỗng `provideAnimationsAsync('noop')` khi hệ điều hành bật cờ, được gọi trong cùng tệp bằng § `chonProviderHoatAnh(globalThis.matchMedia)` từ § `export function provideCoreAnimations(`; `src/FE/src/app/app.config.ts` nối hàm đó vào `appConfig` bằng § `provideCoreAnimations(),`; việc chọn khoá bằng `src/FE/src/app/core/theme/core-animations.spec.ts` — `hệ điều hành bật giảm chuyển động → bộ chạy rỗng (NoopAnimations)`. Kiểm phải mở trình duyệt với cờ giảm chuyển động bật.

📐 **Những mục dưới đây CHƯA đối chiếu, và vẫn là đích đến:** dừng đồng hồ khi hover và khi focus, nhánh `prefers-reduced-motion` của hiệu ứng vào/ra, luật không cướp focus, nút đóng, §Khi nào dùng / khi nào KHÔNG dùng, §Do / Don't.

**Nền:** **bọc PrimeNG**. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4, định vị lớp nổi khi cuộn và khi tràn viewport là ca "khó": phải xử lý container cuộn, `position: fixed` lồng nhau, và trở về đúng chỗ khi hết chỗ.

---

## Mục đích

Báo kết quả một thao tác **vừa xảy ra**, không chặn người dùng làm việc tiếp.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Xác nhận một thao tác đã thành công: "Đã lưu người dùng" | 🛑 Thông tin cần **ở lại** cho tới khi bối cảnh đổi → [`NoticeBanner.md`](./NoticeBanner.md). Toast biến mất; thứ quan trọng thì không được biến mất |
| Báo một thao tác nền đã xong | 🛑 Lỗi của một trường trong form → [`FormRow.md`](./FormRow.md). Lỗi phải nằm cạnh chỗ sai |
| Báo lỗi mà người dùng **không cần làm gì ngay** | 🛑 Lỗi buộc phải xử lý mới đi tiếp được → [`Dialog.md`](./Dialog.md) hoặc `NoticeBanner` |
| Kèm một hành động hoàn tác | 🛑 Nội dung dài hơn hai dòng → nó không đọc kịp trước khi biến mất |
| | 🛑 Nhiều thông báo cùng lúc từ một thao tác → gộp thành một |

**Phép thử một câu:** *nếu người dùng bỏ lỡ thông báo này, họ có mất gì không?* Có → không dùng `Toast`.

## Biến thể

| Biến thể | Icon | Màu icon | Thời gian tự tắt |
| --- | --- | --- | --- |
| `success` | `pi-check-circle` | `--color-success` | 4 giây |
| `info` | `pi-info-circle` | `--color-info` | 5 giây |
| `warning` | `pi-exclamation-triangle` | `--color-warning` | 8 giây |
| `danger` | `pi-times-circle` | `--color-danger` | **Không tự tắt** |

**Toast lỗi không tự tắt.** Người dùng có thể đang nhìn chỗ khác đúng lúc nó hiện. Với một thông báo thành công thì bỏ lỡ không sao — kết quả đã thấy trên màn hình. Với một lỗi thì bỏ lỡ nghĩa là họ tin thao tác đã thành công.

🛑 **"Không tự tắt" phải TẮT HẲN đồng hồ — bỏ trống thời gian là KHÔNG đủ.** Đây là tri thức về nền, không phải chi tiết thi công, nên nó ở lại spec: `primeng/toast` chỉ bỏ qua lời gọi hẹn giờ khi thông báo mang cờ `sticky`; thiếu cờ đó thì một `life` rỗng **rơi về mặc định 3000 ms của thư viện**, và toast lỗi biến mất sau ba giây y như khi không ai chặn gì. Chiều hỏng này im lặng tuyệt đối — không lỗi biên dịch, không đỏ ở một ca kiểm chỉ hỏi "có hiện toast không", và triệu chứng (một lỗi thoáng qua) trông giống hệt hành vi bình thường. Người sửa sau rất dễ "dọn cho gọn" bằng cách gỡ cờ ấy vì nó trông thừa cạnh một `life` đã bỏ trống; nó không thừa. Khoá bằng ca kiểm `toast.component.spec.ts` — `🛑 toast LỖI không tự tắt — sticky bật và KHÔNG có life`: ca này khẳng định **cả hai** vế, nên gỡ một vế là đỏ.

**Thời gian tăng dần theo mức nghiêm trọng** vì mức nghiêm trọng tỉ lệ với lượng chữ cần đọc, và với xác suất người dùng muốn đọc lại. Thời gian cố định theo vai, không tính theo độ dài chữ — nội dung đã bị chặn ở hai dòng nên không có gì để đo.

Nền `Toast` luôn là `--color-surface` với viền `--color-border`; **chỉ icon mang màu vai**. Tô cả nền theo vai làm bốn loại toast trông như bốn component khác nhau, và làm chữ trên nền màu khó đọc ở theme tối.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Bề rộng | `--layout-toast-w`, **cố định** — bí danh bậc `sm` của thang bề rộng ([`../DESIGN.md`](../DESIGN.md) §6.1), để các toast trong ngăn xếp thẳng mép; ở khổ hẹp co theo §Responsive |
| Đệm | `--sp-5` |
| Khe icon → nội dung | `--sp-4` |
| Khe tiêu đề → nội dung | `--sp-2` |
| Khe giữa hai toast trong ngăn xếp | `--sp-4` |
| Bo góc | `--radius-md` |
| Bóng | `--shadow-4` |
| Cỡ chữ tiêu đề | `--fs-sm`, `--fw-semibold` |
| Cỡ chữ nội dung | `--fs-sm`, `--fw-regular`, `--color-text-muted` |
| Lớp | `--z-toast` — cao nhất trong hệ, trên cả `Dialog` |

**`Toast` phải nằm trên `Dialog`.** Một thao tác trong dialog thất bại thì thông báo phải thấy được; nằm dưới backdrop là thông báo không tồn tại.

**Ngăn xếp tối đa ba toast cùng lúc.** Cái thứ tư đẩy cái cũ nhất ra. Quá ba thì chúng che mất nội dung và không ai đọc kịp — mà nhiều toast cùng lúc gần như luôn là dấu hiệu nên gộp thành một.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`; viền `--border-w` `--color-border`; `--radius-md`; `--shadow-4`; icon vai bên trái; nút đóng bên phải | Có |
| `hover` | **Đồng hồ đếm ngược dừng lại** trong lúc con trỏ ở trên. Nếu không, toast biến mất đúng lúc người dùng đang với tay tới nút "Hoàn tác" | Có |
| `focus-visible` | Focus vào bất kỳ control nào bên trong cũng **dừng đồng hồ** — cùng lý do với hover, cho người dùng bàn phím | Có |
| `active` | **Không áp dụng.** `Toast` không phải control; nút bên trong có trạng thái của chúng | — |
| `disabled` | **Không áp dụng.** Một thông báo không có trạng thái vô hiệu hoá | — |
| `loading` | **Không áp dụng.** `Toast` báo việc **đã xong**. Muốn báo việc đang chạy thì dùng chỉ báo tại chỗ hoặc `NoticeBanner` | — |
| `error` | Không phải một trạng thái mà là biến thể `danger` | — |
| `empty` | **Không áp dụng.** Không có toast thì không vẽ gì cả — ngăn chứa rỗng phải **không chiếm chỗ** và không nhận sự kiện chuột | Có, dạng suy biến |

**Ngăn chứa rỗng phải trong suốt với chuột.** Một `<div>` cố định phủ góc màn hình với `pointer-events` mặc định sẽ chặn mọi cú bấm vào vùng đó — kể cả khi nó rỗng và vô hình. Đây là bug hay gặp và rất khó tìm: một góc màn hình đơn giản là không bấm được.

Vào: trượt vào từ mép + mờ dần (`--dur-slow`, `--ease-decelerate`). Ra: mờ dần + thu nhỏ chiều cao để những cái còn lại trượt lên (`--dur-base`, `--ease-accelerate`). Tắt khi `prefers-reduced-motion: reduce` ([`../DESIGN.md`](../DESIGN.md) §7) — khi đó toast hiện và biến mất tức thì.

**Bất đối xứng ở trên là cố ý, và `Toast` là component duy nhất trong họ lớp nổi còn giữ được nó.** Vào chậm hơn ra có lý do cụ thể: một toast bay vào ở rìa tầm nhìn cần đủ thời gian để mắt bắt được nó đã xuất hiện, còn lúc ra thì người dùng đã đọc xong và một cái nấn ná chỉ chiếm chỗ. [`Dialog.md`](./Dialog.md) và [`Drawer.md`](./Drawer.md) phải bỏ khoản này vì nền chỉ cho chúng một tham số; ở đây nền cho hai, nên spec giữ.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-text`, `--color-text-muted`, `--color-border`, `--color-success`, `--color-info`, `--color-warning`, `--color-danger`, `--color-focus` |
| Chữ | `--fs-sm`, `--fw-semibold`, `--fw-regular`, `--lh-snug`, `--lh-normal` |
| Khoảng cách | `--sp-2`, `--sp-4`, `--sp-5`, `--sp-8` |
| Hình dạng | `--radius-md`, `--border-w` |
| Kích thước | `--icon-lg`, `--layout-toast-w` |
| Bóng | `--shadow-4` |
| Lớp | `--z-toast` |
| Chuyển động | `--dur-base`, `--dur-slow`, `--ease-decelerate`, `--ease-accelerate` |

🛑 **Bốn token ở dòng cuối ở lại bảng, nhưng chỗ áp chúng nằm trong TypeScript.** Không dòng CSS nào của lớp bọc đọc `var(--dur-slow)` cho khoản này: hai thời lượng và hai đường cong đi thành hai chuỗi tham số hoạt ảnh Angular, dựng từ hằng số có tên khai cạnh component. Giữ token trong bảng là đúng chứ không phải sót — `Toast` vẫn là nơi **quyết** bốn giá trị đó, và một hằng số trong code chỉ là chỗ áp, ngang vai với một tên token trong stylesheet. Muốn thấy khuôn ngược lại — spec thôi quyết vì thư viện không nhận số nào — thì đó là [`Tooltip.md`](./Tooltip.md), và nó là ca khác hẳn.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Ngăn xếp ở góc **phải dưới**, cách mép `--sp-8`. Toast mới ở dưới cùng |
| `$bp-sm` … `$bp-md` | Vẫn phải dưới, bề rộng co lại, cách mép `--sp-5` |
| < `$bp-sm` | Ngăn xếp lên **trên cùng**, chiếm gần hết bề rộng, cách mép `--sp-4`. Toast mới ở trên cùng |

**Vì sao đảo lên trên ở màn nhỏ:** phần dưới màn hình điện thoại là chỗ ngón tay cái nằm và là chỗ bàn phím ảo chiếm. Toast ở dưới sẽ bị ngón tay che hoặc bị bàn phím đẩy khuất.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vùng thông báo | Ngăn chứa mang `aria-live`. **Mức khác nhau theo vai**: `polite` cho `success` và `info`; `assertive` cho `warning` và `danger` |
| `role` | `role="status"` cho `polite`, `role="alert"` cho `assertive` |
| Ngăn chứa tồn tại sẵn | Vùng `aria-live` phải **có mặt trong DOM từ đầu**, rỗng. Chèn cả vùng `aria-live` cùng lúc với nội dung thì nhiều trình đọc màn hình **không đọc gì cả** — chúng chỉ theo dõi thay đổi bên trong một vùng đã tồn tại. Đây là bẫy phổ biến nhất của mọi thông báo động |
| `pointer-events` | Ngăn chứa `none`; từng toast `auto` |
| Nút đóng | Icon `pi-times` ([`../Icons.md`](../Icons.md) §5), có `aria-label`. Phần cấu trúc của lớp bọc: thư viện vẽ, tạo hình bằng token theo hình thức của [`IconButton.md`](./IconButton.md) — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 |
| Icon | `aria-hidden="true"` — vai đã có trong chữ ([`../Icons.md`](../Icons.md) §7) |
| Thời gian đọc | Toast tự tắt sau vài giây có thể vi phạm WCAG 2.2 SC 2.2.1 nếu nội dung cần thời gian đọc. Giảm nhẹ bằng: dừng đồng hồ khi hover/focus, luôn có nút đóng, và **lỗi thì không tự tắt** |
| Bàn phím | Toast **không** tự cướp focus. Người dùng đang gõ dở mà bị nhảy focus ra chỗ khác là mất chỗ. Toast có nút hành động thì nút đó nằm trong thứ tự Tab bình thường |
| Chữ | Qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Toast không cướp focus — kể cả toast lỗi.** Với lỗi thật sự cần xử lý ngay thì `Toast` là component sai; dùng `Dialog`.

## API dự kiến

`Toast` tách làm **hai phần**, và đây là một quyết định kiến trúc chứ không phải chi tiết thi công:

| Phần | Vai | Ở đâu |
| --- | --- | --- |
| Hàng đợi | Giữ danh sách thông báo, hẹn giờ, giới hạn ba cái | Một service ở tầng `core/` — **smart** |
| Component | Vẽ danh sách nhận qua `input()` | Tầng dùng chung — **dumb** |

Không tách thì component phải là smart (nó tự giữ trạng thái toàn cục), và luật dumb ở [`../COMPONENTS.md`](../COMPONENTS.md) §5 cấm — đây đúng ca thứ hai trong bảng "dễ nhầm" của mục đó.

🛑 **Không cổng nào bắt được ca này.** [`../../RULES.md`](../../RULES.md) §7 F11 chỉ quét `shared/components/`; `Toast` là lớp bọc thư viện nên nó nằm ở `shared/ui/` ([`../COMPONENTS.md`](../COMPONENTS.md) §5, cột "Ở đâu"). Ràng buộc thật sự ép được ở tầng này là chiều import giữa hai tầng — [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2, ép bằng [`../../RULES.md`](../../RULES.md) §7 F24. Việc component không tự giữ hàng đợi thì **giữ bằng review**, không bằng lệnh: người dựng phải biết điều đó trước khi viết dòng đầu tiên.

**Component:**

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `items` | input | `ReadonlyArray<ToastItem>` | `[]` | Tối đa ba; cắt bớt là việc của service |
| `position` | input | `'bottom-right' \| 'top-center'` | `'bottom-right'` | Tự đổi theo điểm ngắt; input chỉ để ghi đè |
| `dismissed` | output | `string` | — | Id của toast bị đóng |
| `actionClicked` | output | `string` | — | Id của toast vừa được bấm nút hành động. Nút ("Hoàn tác") chỉ có khi item mang `actionLabel`; nó là phần cấu trúc của lớp bọc — thư viện vẽ, tạo hình bằng token theo hình thức của [`Button.md`](./Button.md) ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5). Tầng khung chuyển id cho hàng đợi. Chưa card nào trong [`../../contracts/README.md`](../../contracts/README.md) khai endpoint hoàn tác — nút này chỉ dùng khi card của thao tác đó có, và cửa sổ hoàn tác chốt cùng lúc với endpoint |

**`ToastItem`:** chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. `duration` bỏ trống thì lấy mặc định theo vai.

## Do / Don't

- ✅ Dùng `Toast` cho việc **đã xong**.
- ✅ Gộp nhiều kết quả từ một thao tác thành một toast: "Đã xoá 12 người dùng".
- ✅ Dừng đồng hồ khi hover hoặc focus.
- ✅ Kèm nút "Hoàn tác" thay vì hỏi xác nhận trước — xem [`ConfirmDialog.md`](./ConfirmDialog.md).
- ✅ Giữ vùng `aria-live` trong DOM từ đầu.
- ❌ Không dùng `Toast` cho thông tin người dùng không được bỏ lỡ.
- ❌ Không để toast lỗi tự tắt.
- ❌ Không cướp focus.
- ❌ Không quá ba toast cùng lúc.
- ❌ Không tô nền theo vai.
- ❌ Không để ngăn chứa rỗng chặn chuột.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có giữ lịch sử thông báo để xem lại không? Nó cứu được ca bỏ lỡ toast, nhưng thêm một chỗ chứa trạng thái và một biểu tượng chuông trên `Topbar` | Sau F3 — khu thông báo trong ứng dụng |
