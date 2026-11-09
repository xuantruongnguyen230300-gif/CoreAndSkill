---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Toast

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có thật ở `src/FE/src/app/shared/ui/toast/` — selector `app-toast`, class `ToastComponent`, đặt một lần ở gốc trong `src/FE/src/app/app.html`. Bảng dưới khai **đúng những mục đã mở `toast.component.ts`, `toast.component.html`, `toast.component.scss`, `toast.component.spec.ts` và `src/FE/src/app/core/toast/toast.service.ts` ra so**; mục không có tên trong bảng thì chưa ai đối chiếu. Lượt đầu là 2026-09-20, nhưng **dòng nào so lại sau đó mang ngày của chính nó** trong ô "Có thật hôm nay" — không có lượt nào quét lại cả bảng, nên đừng đọc một ngày duy nhất cho mọi dòng. Lớp bọc vẫn **mỏng**, nhưng không còn trần: template là `<p-toast>` mang hai binding — một binding tắt lớp tự ghi của thư viện (dòng `--z-toast`) và một binding pass-through cho vùng thông báo (dòng *Vùng thông báo, `role`, ngăn chứa tồn tại sẵn*) (đọc mã 2026-11-07) — còn `toast.component.scss` chỉ khai đúng lớp xếp chồng (đọc mã 2026-10-06). Gần như toàn bộ §Kích thước, §Responsive và phần hình thức của §Trạng thái vì thế còn ở dạng mặc định của thư viện.

| Mục trong spec | Có thật hôm nay (neo bằng chuỗi trong `src/FE`) | Sẽ thành |
| --- | --- | --- |
| §Nền — bọc PrimeNG | `toast.component.ts` import `Toast` từ `primeng/toast` và `MessageService` từ `primeng/api`; `toast.component.html` là **một** thẻ `<p-toast`, mang hai binding: `[autoZIndex]` — dòng `--z-toast` — và `[pt]` — dòng *Vùng thông báo, `role`, ngăn chứa tồn tại sẵn*. Hai binding thời lượng cũ đã gỡ — dòng cuối bảng (so lại 2026-11-07) | Giữ nguyên |
| §API — tách hàng đợi (smart, `core/`) khỏi component vẽ | Hàng đợi là `ToastService` ở `core/toast/toast.service.ts`, giữ trạng thái bằng `signal` và phơi ra `latest`; `ToastComponent` **không** giữ mảng thông báo nào | Giữ nguyên |
| `core/` không được biết thư viện UI | `toast.service.ts` không import gì từ `primeng/`; chỗ nối hai bên là `effect()` trong constructor của `ToastComponent` | Giữ nguyên |
| Lớp `--z-toast` cao nhất | Token `--z-toast` **có khai** trong `src/FE/src/styles/_tokens.scss`, trên `--z-dialog` và `--z-popover`. Chỗ dùng (đọc mã 2026-10-06): `toast.component.html` § `[autoZIndex]="false"` tắt lớp thư viện tự ghi; `toast.component.scss` § `z-index: var(--z-toast)` khai trên phần tử chủ `p-toast`. Tệp không khai `position` cho phần tử chủ đó | **Chưa đo trên trình duyệt** — chưa ai thấy toast nằm trên một `Dialog` đang mở. `z-index` khai trên một phần tử tĩnh có thể không có tác dụng, nên phép đo này quyết định dòng này đúng hay sai |
| Bốn vai và tên của chúng | **Lệch tên.** `toast.service.ts` khai `export type ToastSeverity = 'success' \| 'error' \| 'info' \| 'warn'`; spec khai `success` / `info` / `warning` / `danger` | Chốt một bộ tên rồi sửa bên còn lại — tên vai lọt ra tận API công khai của service |
| Thời gian tự tắt theo vai (4 / 5 / 8 giây) | **Đã có** (dựng 2026-09-23). `toast.component.ts` — `const TOAST_THOI_GIAN_TU_TAT_MS` giữ ba giá trị theo vai và `private thoiGianTuTat(` trả chúng vào `life` của lời gọi `add(...)`. Tên vai của spec ánh xạ sang tên severity của service ngay tại chú thích của hằng đó: `warning` ⇒ `warn`, `danger` ⇒ `error` | Giữ nguyên |
| 🛑 Toast lỗi **không** tự tắt | **Đã có** (dựng 2026-09-23, đóng chiều hỏng nguy hiểm). `toast.component.ts` — `thoiGianTuTat` trả `undefined` cho vai `error`, và lời gọi `add(...)` đặt `sticky: tuTatSau === undefined`. `sticky` là đường duy nhất tắt đồng hồ: `primeng/toast` bọc `setTimeout` trong `if (!this.message?.sticky)`, còn bên trong thì `this.message?.life \|\| this.life \|\| 3000` rơi về **3000** khi `life` rỗng — bỏ trống `life` KHÔNG đủ. Khoá bằng test: `toast.component.spec.ts` — `🛑 toast LỖI không tự tắt — sticky bật và KHÔNG có life` | Giữ nguyên |
| Ngăn xếp tối đa ba | **Chưa có** (so lại ngày 2026-09-22). `ToastService` nay giữ hàng đợi `_hangDoi = signal<readonly ToastMessage[]>([])`, phơi ra `hangDoi` và `layHet()`; `latest` vẫn còn nhưng chỉ để đọc. Hàng đợi đó **không** phải ngăn xếp đang hiện: `effect()` của `ToastComponent` gọi `this.toast.layHet()` rồi `this.messageService.add(...)` từng thông báo, nên hàng đợi rỗng ngay sau mỗi lượt vẽ. Không chỗ nào trong `toast.service.ts`, `toast.component.ts` hay `toast.component.html` cắt số lượng — số toast hiện cùng lúc do thư viện quyết | Đặt trần ba cho ngăn xếp đang hiện theo §Kích thước |
| API component: `items`, `position`, `dismissed`, `nutHanhDong` | **Có một trong bốn** (đọc mã 2026-10-09). `nutHanhDong` khớp §API — dòng *Nút hành động*. Ba mục còn lại **chưa có**: `ToastComponent` không khai `output()` nào, không khai `input()` nào khác ngoài `nutHanhDong`; nó `inject(ToastService)` và đọc thẳng. Hai thành viên `protected` về thời lượng chuyển động đã gỡ khỏi class (đọc mã 2026-11-07); thành viên `protected` mới, § `protected readonly pt = TOAST_PT;`, chỉ nối template với pass-through, không phải `input()`, nên không đếm vào §API dự kiến | `items`, `position`, `dismissed`: hoặc dựng đúng §API dự kiến, hoặc sửa spec cho khớp cách nối qua `ToastService` |
| Kiểu `ToastItem` của `../../quy-uoc/fe-ui-conventions.md` §9 | **Không dùng.** Không tệp `.ts` nào dưới `src/FE/src` chứa chuỗi `ToastItem`; chỗ đó là `interface ToastMessage` tự khai trong `toast.service.ts`, với `traceId` mà §9 không có | Dùng kiểu gốc, hoặc chuyển chủ quyền kiểu |
| Nút hành động | **Có cho ca "Tải lại trang", khớp §API** (đọc mã 2026-10-09). Dữ liệu: `core/toast/toast.service.ts` § `export interface ToastHanhDong` (`nhan` đã dịch, `chay`), `ToastMessage` mang `hanhDong`; nơi gọi truyền qua § `loi(summary: string, traceId: string \| null = null, hanhDong: ToastHanhDong \| null = null)`. Vẽ: `toast.component.html` đặt § `*ngTemplateOutlet` trong `.p-toast-message-text` — trước nút đóng của thư viện — với mẫu nhận qua input § `readonly nutHanhDong = input<TemplateRef<ToastNutHanhDongContext> \| null>(null);`. Nơi ghép truyền mẫu: `app.html` khai § `<ng-template #nutHanhDong` chứa `<app-button variant="secondary" size="sm" [disabled]="daBam"` gọi `bam(id, hanhDong)`, rồi truyền § `<app-toast [nutHanhDong]="nutHanhDong" />`. Chạy một lần: `toast.component.ts` § `bam` ghi id vào `daBam` trước rồi gọi `hanhDong.chay()`. `sticky` giữ như dòng *Toast lỗi không tự tắt*: nơi gọi duy nhất truyền hành động là `core/navigation/loi-tai-phan-ma.ts` § `this.toast.loi(`, vai `error`. Ca kiểm `toast.component.spec.ts`: `KHÔNG cướp focus: focus ở lại phần tử người dùng đang giữ, không vào nút hành động`, `bấm nút → hành động chạy một lần, nút disabled ngay, bấm lần hai không chạy lại`, `toast không có hanhDong → không vẽ nút hành động`; e2e `src/FE/e2e/man/02-trang-loi.spec.ts` khối `không tải được phần ứng dụng — đã có màn đang hiện (toast)` — ca `màn giữ nguyên, toast lỗi kèm nút, URL ở lại màn cũ, không khối toàn trang` chạy trên app thật, đòi nút "Tải lại trang" hiện, nên `app.html` thôi truyền mẫu là ca này đỏ (người soạn chưa chạy test) | Giữ nguyên. Nhánh §Accessibility "hành động không rời trang → toast đóng, focus về phần tử đã giữ focus trước lúc bấm" **chưa có** trong `bam` — chưa có ca dùng. "Hoàn tác" chờ endpoint |
| Hình thức: nền `--color-surface`, viền `--color-border`, `--radius-md`, `--shadow-4`, bề rộng `--layout-toast-w`, các bậc đệm | **Chưa dựng bằng token.** `toast.component.scss` chỉ khai lớp xếp chồng — dòng `--z-toast`; hình thức tới từ preset `AuraToast` đăng ký trong `core/theme/prime-preset.ts` (so lại 2026-10-06) | Tạo hình theo §Kích thước bằng cơ chế theme của thư viện |
| §Responsive — ba ngưỡng, đảo lên trên ở màn nhỏ | **Chưa có.** `toast.component.scss` không có điểm ngắt nào, và không có `position` nào được truyền cho `<p-toast` (so lại 2026-10-06) | Dựng cả ba ngưỡng |
| §Accessibility, hàng *Bàn phím* — toast **không** cướp focus | **Đã có** (đọc mã 2026-10-06). Template của `primeng/toast` gắn cứng `autofocus` lên nút đóng; `toast.component.ts` § `chanToastCuopFocus` gỡ thuộc tính đó ngay khi nút xuất hiện, và nếu focus vẫn rơi vào nút đóng thì trả về phần tử đang giữ focus trước đó. Ca kiểm `toast lỗi xuất hiện → focus vẫn ở phần tử người dùng đang giữ, không nhảy vào nút đóng`. Lượt đo trên trình duyệt cùng ngày (Chromium do Playwright điều khiển, `/dang-nhap`, phản hồi 500 giả) chạy **trước** lượt sửa: toast lỗi kéo focus sang nút đóng, cả khi gửi bằng chuột lẫn bằng `Enter`; gửi bằng `Enter` thì nút đóng còn hiện vòng focus | Đo lại trên trình duyệt sau lượt sửa, đủ hai cách gửi: bấm chuột và `Enter` |
| §Accessibility, ba hàng *Vùng thông báo*, `role`, *Ngăn chứa tồn tại sẵn* | **Khớp ở DOM** (đọc mã 2026-11-07, bản 21.1.10). Thư viện vẫn gắn cứng `role="alert"`, `aria-live="assertive"`, `aria-atomic="true"` lên mọi phần tử thông báo (tệp `primeng-toast.mjs` trong gói `primeng` đã cài); lớp bọc đè bằng pass-through: `toast.component.html` chuỗi `[pt]="pt"`, `toast.component.ts` § `const TOAST_PT: ToastPassThroughOptions` — khoá `host` mang `{ 'aria-live': 'polite' }`; khoá `message` là hàm trả `{ role: 'status', 'aria-live': 'polite' }` khi `severity` là `success` hoặc `info`, trả `{}` cho vai còn lại để giữ `role="alert"` + `aria-live="assertive"` của thư viện. Phần tử chủ `p-toast` có mặt từ lúc khởi động vì `app.html` đặt `<app-toast>` cố định, nên vùng `polite` có trước toast đầu tiên. Ca DOM `toast.component.spec.ts`: `p-toast mang aria-live="polite" TRƯỚC toast đầu tiên (vùng live có mặt từ đầu)`, `success → role="status" + aria-live="polite"`, `info → role="status" + aria-live="polite"`, `warn → GIỮ role="alert" + aria-live="assertive" của thư viện`, `error → GIỮ role="alert" + aria-live="assertive" của thư viện` — phiên chính báo xanh 2026-11-07. Phiên chính cũng báo cùng ngày: trên app chạy thật, cờ giảm chuyển động bật, `p-toast` mang `aria-live="polite"`. Kết quả không lưu trong repo | **Chưa đo bằng trình đọc màn hình thật.** Hai rủi ro DOM không trả lời được — cả hai khai tại chỗ trong chú thích của `TOAST_PT`: (1) toast `warning`, `danger` nằm trong vùng `polite` của phần tử chủ, có thể bị đọc hai lần; (2) khe giữa lúc thư viện ghi thuộc tính tĩnh và lúc pass-through ghi đè. Đo đủ bốn vai rồi mới coi ba hàng §Accessibility này là đạt |
| §Biến thể (cột Icon) và §Accessibility, hàng *Icon* — bốn icon theo vai | **Khớp ở mã, tên và `aria-hidden`** (đọc mã 2026-11-07). `toast.component.html` vẽ lại icon trong `<ng-template #message` bằng `@switch (message.severity)`: `success` → `<i class="pi pi-check-circle p-toast-message-icon" aria-hidden="true"></i>`; `error` → `<svg data-p-icon="times-circle" class="p-toast-message-icon" aria-hidden="true">`; `warn` → `<svg data-p-icon="exclamation-triangle" … aria-hidden="true">`; nhánh `@default` (vai `info`) → `<svg data-p-icon="info-circle" … aria-hidden="true">`. Cả bốn mang `aria-hidden="true"`; ba tệp icon của thư viện (gói `primeng` đã cài: `primeng-icons-timescircle.mjs`, `primeng-icons-infocircle.mjs`, `primeng-icons-exclamationtriangle.mjs`) và `primeng-icons-baseicon.mjs` không chứa chuỗi `aria`, nên không có gì trong thư viện ghi đè thuộc tính đó. Tên biểu tượng khớp bảng §Biến thể ở cả bốn vai | **Hình thức vẽ khác nhau theo vai:** `success` là icon font (`pi-check-circle`), ba vai còn lại là SVG nội tuyến của thư viện mang cùng tên biểu tượng — [`../Icons.md`](../Icons.md) §1 nhận SVG của thư viện ở lớp bọc. **Chưa đo:** màu icon theo vai (`--color-success`…`--color-danger`) — `toast.component.scss` không khai màu, chưa so preset `AuraToast`; thuộc tính `aria-hidden` trên DOM chạy thật chưa mở (`toast.component.spec.ts` không có ca nào về icon); người soạn chưa chạy test |
| Chữ qua i18n | **Một chỗ đang sai.** `effect()` dựng `detail` bằng nối chuỗi trực tiếp với `traceId`, không qua tầng dịch | Đưa chuỗi đó qua khoá dịch |
| §Trạng thái, đoạn vào/ra — bốn khoản `--dur-slow` · `--ease-decelerate` · `--dur-base` · `--ease-accelerate` | **Có trong mã, đủ bốn khoản** (đọc mã 2026-11-07, PrimeNG 21.1.10). Khối đè ở `src/FE/src/styles/_thu-vien.scss`: § `p-toast .p-toast-message-enter-active` mang `var(--dur-slow)` + `var(--ease-decelerate)`; § `p-toast .p-toast-message-leave-active` mang `var(--dur-base)` + `var(--ease-accelerate)`; § `p-toast .p-toast-message-content` đè `transition` của `padding` mà thư viện chạy trong pha ra bằng `var(--dur-base) var(--ease-accelerate)`. Chỉ đè hai thuộc tính tách, nên keyframes của thư viện giữ nguyên — vào mờ dần + phóng từ nhỏ; ra mờ dần + thu, dịch lên, gập chiều cao bằng `grid-template-rows`, `margin-bottom` — đúng câu chữ §Trạng thái, kể cả lệch có tên. **Bất đối xứng vào chậm, ra nhanh còn nguyên.** Lớp bọc không truyền thời lượng nào: không tệp nào dưới `src/FE/src` còn chuỗi `TOAST_SHOW_TRANSITION_OPTIONS`, `TOAST_HIDE_TRANSITION_OPTIONS`, `[showTransitionOptions]` hay `[hideTransitionOptions]`. Ca kiểm `toast.component.spec.ts`: `pha vào: --dur-slow, --ease-decelerate`, `pha ra: --dur-base, --ease-accelerate — kể cả transition padding của phần tử con`; người soạn chưa chạy test | Nhịp trên trình duyệt **chưa đo**, kể cả phần gập chiều cao ở chiều ra. Nhánh giảm chuyển động **chưa đo** — ⚠️ ngay dưới bảng, điều (3) |

⚠️ Ba điều **chưa so**, và không so được từ bốn tệp trên: (1) các khoản §Accessibility ngoài dòng *Vùng thông báo, `role`, ngăn chứa tồn tại sẵn* ở bảng trên — `pointer-events` của ngăn chứa — muốn kiểm phải mở DOM lúc chạy (`aria-hidden` của icon đã có dòng riêng ở bảng trên: khớp ở mã, DOM chạy thật chưa mở); (2) preset `AuraToast` có giải ra đúng các token ở §Token dùng hay không; (3) nhánh `prefers-reduced-motion` của chuyển động vào/ra. Từ bản thư viện đang cài, chuyển động đó là lớp CSS theo pha — loại 1 của [`../DESIGN.md`](../DESIGN.md) §7 — nên khối `@media (prefers-reduced-motion: reduce)` toàn cục ở `src/FE/src/styles/styles.scss` với tới, và lớp `safe` mặc định của thư viện còn tự bỏ qua pha khi cờ đã bật lúc dựng — [`../DESIGN.md`](../DESIGN.md) §7, mục *Giảm chuyển động*. `Toast` không mang chuyển động nào đi qua bộ chạy hoạt ảnh của Angular, nên không phụ thuộc provider nào. Kết luận bằng đọc mã, chưa đo: kiểm phải mở trình duyệt với cờ giảm chuyển động bật, rồi tải lại trang. Hai ca chuyển động ở bảng trên tự `pending` khi cờ bật, nên không ca nào canh nhánh này; lượt chạy app thật dưới cờ bật ngày 2026-11-07 chỉ báo thuộc tính `aria-live`, không báo chuyển động.

📐 **Những mục dưới đây CHƯA đối chiếu, và vẫn là đích đến:** dừng đồng hồ khi hover và khi focus, nhánh `prefers-reduced-motion` của hiệu ứng vào/ra, nút đóng, §Khi nào dùng / khi nào KHÔNG dùng, §Do / Don't. Luật không cướp focus đã có trong mã — dòng *Bàn phím* ở bảng trên — nhưng chưa đo lại trên trình duyệt.

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

Vào: mờ dần hiện ra, kèm phóng từ nhỏ tới cỡ thật (`--dur-slow`, `--ease-decelerate`). Ra: mờ dần, kèm thu nhỏ và dịch lên, đồng thời gập chiều cao để những cái còn lại trượt lên (`--dur-base`, `--ease-accelerate`). Tắt khi `prefers-reduced-motion: reduce` ([`../DESIGN.md`](../DESIGN.md) §7) — khi đó toast hiện và biến mất tức thì.

**Bất đối xứng ở trên là cố ý.** Vào chậm hơn ra có lý do cụ thể: một toast bay vào ở rìa tầm nhìn cần đủ thời gian để mắt bắt được nó đã xuất hiện, còn lúc ra thì người dùng đã đọc xong và một cái nấn ná chỉ chiếm chỗ. [`Dialog.md`](./Dialog.md) và [`Drawer.md`](./Drawer.md) tách đường cong theo hướng như `Toast` nhưng giữ **một** thời lượng `--dur-base` cho hai chiều (chốt 2026-11-06): chúng không đi vào từ rìa tầm nhìn theo cách một toast bay vào góc màn hình. Nền chở được hai bộ cho cả ba ([`../../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md`](../../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) quyết định 7), nên chỗ khác nhau giữa ba spec là quyết định của hệ thiết kế, không phải giới hạn của nền.

⚠️ **Lệch có tên với luật *chỉ animate `transform` và `opacity`* của [`../DESIGN.md`](../DESIGN.md) §7 — chỉ cho chiều ra.** "Thu nhỏ chiều cao để những cái còn lại trượt lên" không dựng được bằng hai thuộc tính đó mà không đo vị trí bằng JavaScript — loại 3 của §7, thứ không lớp giảm chuyển động nào với tới. Chuyển động ra của thư viện gập chiều cao bằng `grid-template-rows`, `margin-bottom` và `transition` của `padding`; spec nhận cách đó, không đòi viết lại keyframes. Biên: chỉ pha ra của `Toast`, ba thuộc tính trên, thời lượng `--dur-base`; ngăn xếp tối đa ba phần tử nên phần bố cục phải tính lại mỗi khung hình chỉ là ngăn chứa toast. Pha vào **không** được hưởng ngoại lệ này — keyframes vào của thư viện có khai `grid-template-rows`, nhưng giá trị cuối trùng giá trị sẵn có của phần tử nên không có gì đổi (đọc mã 2026-11-06, chưa đo).

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

🛑 **Bốn token ở dòng cuối áp bằng khối đè ở `src/FE/src/styles/_thu-vien.scss`, không bằng stylesheet của lớp bọc và không bằng TypeScript.** Thời lượng, đường cong viết cứng trong lớp pha vào/ra mà thư viện gắn lên từng thông báo; khối đè đặt `animation-duration`, `animation-timing-function` của pha vào bằng `--dur-slow`, `--ease-decelerate`, của pha ra bằng `--dur-base`, `--ease-accelerate`, và mọi `transition` của phần tử con chuyển động cùng pha ra mang token của pha ra. Lớp bọc không truyền thời lượng nào: truyền `duration` thì thư viện ghi số thành style nội tuyến và bỏ qua token. Luật **F62** ([`../../RULES-FE.md`](../../RULES-FE.md) §7); cơ chế và hai bẫy: [`../../wiki-core/fe/04-design-token-system.md`](../../wiki-core/fe/04-design-token-system.md) §7.2.

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
| Toast có nút hành động (chốt 2026-10-08, ca "Tải lại trang") | **Focus:** không đặt focus vào nút khi toast hiện, kể cả toast `danger` không tự tắt — người dùng có thể đang gõ dở, và chính ca này tồn tại để không làm mất dữ liệu dở. Thứ tự Tab trong một toast: nút hành động → nút đóng (nội dung đọc rồi mới tới hành động, đóng ở cuối). Ngăn chứa toast đứng cuối DOM nên Tab tới toast sau nội dung trang. **Tên truy cập:** nhãn hành động đã dịch ("Tải lại trang"), là chữ trần của nút, không thêm `aria-label` khác chữ nhìn thấy (WCAG 2.5.3). **Đọc to:** nút nằm trong thân thông báo, nên vùng `role="alert"` ở vai `danger` đọc cả câu lẫn nhãn nút; không thêm vùng live nào. **Hình thức:** [`Button.md`](./Button.md) `secondary` cỡ `sm`, vòng focus `--color-focus`, vùng bấm ≥ 28×28px. **Sau khi bấm:** hành động chạy một lần; nút `disabled` ngay để không bấm đôi. Hành động là tải lại toàn trang thì focus do trình duyệt đặt lại, không cần trả focus; hành động không rời trang thì toast đóng và focus về phần tử đã giữ focus trước lúc bấm |
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
| `nutHanhDong` | input | `TemplateRef<ToastNutHanhDongContext> \| null` | `null` | Mẫu vẽ nút hành động. 🛑 **Nơi ghép ứng dụng — chỗ đặt `<app-toast>` duy nhất — phải truyền mẫu này.** Để `null` thì toast mang hành động **không hiện nút**: hành động có trong dữ liệu mà người dùng không thấy, và không có lỗi nào báo. Vì sao nơi ghép vẽ chứ không phải lớp bọc: nút là [`Button.md`](./Button.md) `secondary` cỡ `sm`, thư viện không có chỗ vẽ nút hành động, còn lớp bọc ở `shared/ui/` không được import `shared/components/` ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2). Ngữ cảnh mẫu nhận: `$implicit` là hành động (`nhan` đã dịch, `chay`), `id`, `daBam`, `bam(id, hanhDong)`. Mẫu gọi `bam`, không gọi thẳng `chay`, và đặt `disabled` theo `daBam` — lớp bọc giữ luật chạy một lần của §Accessibility, hàng *Toast có nút hành động*. Hành động đi cùng thông báo qua `ToastService`; lớp bọc không mở `output()` cho nút này. Hai ca dùng: "Tải lại trang" (chốt 2026-10-08, [`../Screens/02-trang-loi.md`](../Screens/02-trang-loi.md)) và "Hoàn tác". Chưa card nào trong [`../../contracts/README.md`](../../contracts/README.md) khai endpoint hoàn tác — "Hoàn tác" chỉ dùng khi card của thao tác đó có, và cửa sổ hoàn tác chốt cùng lúc với endpoint; "Tải lại trang" không cần endpoint |

**`ToastItem`:** chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9. `duration` bỏ trống thì lấy mặc định theo vai.

## Do / Don't

- ✅ Dùng `Toast` cho việc **đã xong**.
- ✅ Gộp nhiều kết quả từ một thao tác thành một toast: "Đã xoá 12 người dùng".
- ✅ Dừng đồng hồ khi hover hoặc focus.
- ✅ Điều hướng hỏng khi đang có màn: giữ màn, bật toast `danger` không tự tắt kèm nút "Tải lại trang".
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

Câu mở 2026-10-08 — ai vẽ nút hành động — người dùng chốt 2026-10-09: spec theo cách nối đang chạy. Input `nutHanhDong` do nơi ghép truyền mẫu thay cho output `actionClicked` với nút do lớp bọc tự vẽ; vì sao: nút phải mang đúng hình thức `Button`, mà lớp bọc không import được `Button`. Luật ở §API dự kiến, dòng `nutHanhDong`.
