---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0123 — Thời lượng và đường cong chuyển động của component PrimeNG đi bằng token **qua CSS**, đè trên lớp chuyển động của thư viện; gỡ mọi hằng `*_MS` của nhóm B; F37 hết áp dụng

> **Trạng thái:** Đã chấp nhận (2026-11-06)

## Bối cảnh

[ADR-0073](0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) và [ADR-0075](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) dựng "nhóm B" trên một sự thật về nền: `Dialog`, `Drawer`, `Toast`, `Menu`, `DatePicker`, `Autocomplete` chạy chuyển động bằng bộ chạy hoạt ảnh của Angular, tham số là chuỗi bị phân tích thành số, **không giải `var()`** — nên số buộc phải sống ở một hằng TypeScript, và luật **F37** đòi hằng đó nêu đích danh token nó phản chiếu. ADR-0073 §*Rút lui* tự đặt điều kiện quay lại: *"cần một ADR mới nêu nền đã đổi thế nào"*. ADR này là ADR đó.

Nền đổi vì FE vừa nâng PrimeNG 20.4 lên 21 — lượt nâng mà [ADR-0028](0028-toolchain-fe-va-ke-hoach-nang-cap.md) đã lên kế hoạch. `frontend-expert` báo ba input cũ không còn tác dụng; `architect` đọc thẳng gói đã cài ngày 2026-11-06 để đối chiếu. **Toàn bộ dưới đây là đọc tĩnh mã thư viện — chưa ai mở ứng dụng trên trình duyệt để đo.**

| Đo gì | Kết quả |
| --- | --- |
| Phiên bản đang cài | `primeng` 21.1.10 · `@primeuix/motion` 0.0.10 · `@primeuix/styles` 2.0.3 · `@angular/core` 21.2.26 |
| Input cũ | `transitionOptions` của `Dialog`, `Drawer`; `showTransitionOptions` / `hideTransitionOptions` của `Toast`, `Menu`, `DatePicker`, `AutoComplete` — đều mang `@deprecated since v21.0.0. Use motionOptions instead` |
| Input cũ còn ai đọc | `Drawer`: khai rồi không ai đọc. `Toast`, `Menu`, `DatePicker`, `AutoComplete`: khai, không template nào đọc — chuyển động đi qua directive `pMotion` / component `p-motion` nhận `motionOptions`. `Dialog`: chỉ hàm `_focus` đọc, để hẹn giờ đặt focus; `_focus` chỉ chạy khi `focusOnShow` bật, mà lớp bọc đặt `[focusOnShow]="false"` và tự đặt focus ở `(onShow)` — `src/FE/src/app/shared/ui/dialog/dialog.component.html`, chuỗi `[focusOnShow]="false"` |
| Chuyển động mới chạy bằng gì | Gắn rồi gỡ lớp CSS theo pha. Thời lượng nằm trong khai báo `animation` của lớp pha, ở CSS thư viện nạp: `p-dialog-enter-active` / `p-dialog-leave-active` 300ms; `Drawer` dùng `p-drawer-enter-<vị trí>` / `p-drawer-leave-<vị trí>` 0.5s; `p-toast-message-enter-active` 300ms, `p-toast-message-leave-active` 250ms; ba lớp nổi dùng tên chung `p-anchored-overlay` — lớp `p-anchored-overlay-enter-active` / `-leave-active` 300ms ở `@primeuix/styles/base` |
| `p-anchored-overlay` còn ai dùng | Mọi lớp nổi neo của thư viện: `select`, `multiselect`, `overlay`, `table`, `contextmenu`, `tieredmenu`, `popover`… Trong app hôm nay: ba component trên, cộng ô số dòng của `Paginator` — một `Select` mà thư viện tự dựng bên trong |
| Thời lượng lấy từ đâu khi không truyền `duration` | `@primeuix/motion` đọc `getComputedStyle` của phần tử chuyển động — `transitionDuration`, `animationDuration` cộng delay — lấy **giá trị lớn nhất**, chờ `transitionend` / `animationend` của chính phần tử, kèm hẹn giờ dự phòng ở đúng mốc đó. `var()` đã được trình duyệt giải trước khi thư viện đọc, nên **token tới được** |
| Có truyền `duration` thì sao | `duration` chỉ nhận số mili-giây (`MotionDuration = number \| { enter?, leave? }`). Directive ghi nó thành style nội tuyến của phần tử rồi kết thúc pha bằng `setTimeout` — không đọc CSS nữa |
| Đổi `motionOptions` sau khi dựng | Không tác dụng: directive tạo bộ chuyển động một lần, chỗ cập nhật còn để `@todo` trong mã thư viện |
| Giảm chuyển động | `safe` mặc định `true`: khi `prefers-reduced-motion: reduce` khớp lúc tạo bộ chuyển động thì bỏ qua pha, gọi thẳng các hook. Ngoài ra chuyển động nay là CSS, nên khối `0.01ms !important` toàn cục ở `styles.scss` cũng với tới |
| Backdrop | Vẫn `dt('mask.transition.duration')` — khoá `semantic.mask.transitionDuration` mà ADR-0073 quyết định 7 đã nối về `--dur-base` vẫn có tác dụng |
| Thư viện còn dùng bộ chạy hoạt ảnh của Angular không | Không tệp nào trong `primeng/fesm2022` import `@angular/animations` |
| Override đã có bị lượt nâng làm chết không | Có. `src/FE/src/styles/_thu-vien.scss`, chuỗi `.p-drawer-mask.p-overlay-mask-enter` — v21 gắn `p-overlay-mask-enter-active` / `p-overlay-mask-leave-active`, nên khối đó không khớp phần tử nào. Không lỗi build, không lint nào báo |
| CSS của thư viện nằm ở đâu so với CSS của app | `cssLayer: true` ở `providePrimeNG` — thư viện vào `@layer primeng`, CSS không lớp của app thắng mà không cần tăng độ ưu tiên |
| `Drawer` còn `transition` riêng không | `@primeuix/styles/drawer` vẫn khai `transition: transform 0.3s` trên `.p-drawer`; khối `.app-drawer` ở `_thu-vien.scss` đã đè nó bằng `--dur-base` |

Ba điều rút ra, theo thứ tự nặng nhẹ:

**Một — mọi giá trị nhóm B đã trôi khỏi token, ngay hôm nay.** Hằng `*_MS` còn trong mã nhưng nuôi những input không ai đọc. `Toast` chạy 300 / 250ms của thư viện thay cho bậc mà spec đặt; `Dialog`, `Menu`, `Autocomplete` chạy 300ms; `Drawer` 0.5s. Lệch có thật, chỉ chưa ai nhìn.

**Hai — cổng F37 xanh mà rỗng nghĩa.** Nó so hằng với token, hai bên vẫn khớp nhau. Nhưng hằng không còn đi tới đâu, nên cổng đang canh một thứ không ảnh hưởng gì tới màn hình.

**Ba — lý do tồn tại của nhóm B không còn.** *"Tham số không giải `var()`"* là sự thật của bộ chạy Angular. Bộ chạy mới không nhận tham số chuỗi nào; nó đọc giá trị đã tính của CSS. Hai nơi giữ cùng một giá trị — cái giá chính mà ADR-0073 nhận vì không tránh được — nay tránh được.

Người dùng chốt hướng ngày 2026-11-06. ADR này ghi lại và chốt cách thi hành.

## Quyết định

**Người dùng chốt hướng:** thời lượng và đường cong chuyển động của nhóm B đi bằng token qua CSS; gỡ mọi hằng `*_MS` của nhóm; bỏ luật F37. **`architect` chốt cách thi hành:**

1. **Token tới component bằng khối đè trên lớp chuyển động của thư viện, khai ở `src/FE/src/styles/_thu-vien.scss`.** Mỗi khối đặt `animation-duration` và `animation-timing-function` cho pha vào và pha ra bằng `var(--dur-*)`, `var(--ease-*)` — viết dạng tách thuộc tính, không viết lại cả `animation`, để giữ nguyên `@keyframes` của thư viện. Mọi thời lượng viết cứng khác nằm trên đường vào/ra của component — kể cả phần tử con chuyển động cùng pha, như `transition: padding 250ms` của `.p-toast-message-content` — đè bằng token của pha đó. Bậc nào cho pha nào lấy ở spec `docs/Design/Components/<X>.md`; ADR này không chép.
2. **Lớp bọc không truyền thời lượng nào bằng TypeScript.** Không bind `transitionOptions`, `showTransitionOptions`, `hideTransitionOptions`; không truyền `duration` trong `motionOptions`. Không truyền `duration` là chính điều kiện để thư viện đọc CSS — truyền số thì số đó thành style nội tuyến và token bị bỏ qua.
3. **Trên phần tử chuyển động, mọi `transition` và `animation` của chính phần tử đó mang cùng token với pha đang chạy, hoặc bị gỡ.** Thư viện lấy thời lượng **lớn nhất** để biết pha xong lúc nào: một `transition` dài hơn giành quyền định thời, thư viện chờ một `transitionend` không tới rồi kết thúc theo hẹn giờ dự phòng; trong khoảng lệch đó, `@keyframes` ra của thư viện không mang `forwards` nên phần tử nhảy về chỗ cũ trước khi bị ẩn. Khối `.app-drawer` đang đè `transform` bằng `--dur-base` — giữ, và giữ cùng token với pha.
4. **Lớp nổi neo đè MỘT lần, trên lớp chung `p-anchored-overlay`, không theo từng lớp bọc.** Cùng một lớp phục vụ `Menu`, `DatePicker`, `Autocomplete` và mọi lớp nổi neo không qua lớp bọc nào — như ô số dòng của `Paginator` — và [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 giao cho chúng **một** vai: bung và thu lớp nổi neo. Đè theo lớp bọc thì lớp nổi không có lớp bọc chạy nhịp khác trong cùng một màn. `Dialog`, `Drawer`, `Toast`: chọn phạm vi selector theo [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §4.4 là việc của `frontend-expert`.
5. **Mỗi lớp bọc có spec chứng minh token tới đích.** Spec mở component thật và khẳng định: phần tử đang mang lớp pha vào — và lớp pha ra — có `animation-duration`, `animation-timing-function` đã tính bằng giá trị đã tính của token, đọc qua `getComputedStyle(document.documentElement)`; không chép số vào spec. Đây là nửa *đối chiếu giá trị* của F37 đổi chỗ, và nó bắt thêm hai ca F37 không bao giờ bắt: thư viện đổi tên lớp ở một lượt nâng — đúng ca vừa làm chết khối backdrop `Drawer` — và một lần truyền `duration` bằng số. Karma nạp `styles.scss` (`src/FE/angular.json`, mục `test`), nên đo được.
6. **F37 mang trạng thái `Hết áp dụng (2026-11-06)`; luật mới F62 giữ phần ràng buộc còn sống.** F37 không đổi nghĩa — mã luật mang một nghĩa mãi mãi ([`../RULES.md`](../RULES.md)). F62 gồm quyết định 1–3 và hai ràng buộc giá trị mà F37 đang chở từ ADR-0075: bậc chọn theo **vai**, không thêm bậc. Không dựng section cổng script nào cho F62 ([ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)): nửa TypeScript do `core-reviewer` canh — `shared/ui/` thuộc `core-paths`; nửa CSS do spec ở quyết định 5. Section `F37` của `scripts/fe-gate.sh` và canary của nó **gỡ cùng lượt** với hằng: gỡ hằng mà giữ section thì section còn đúng một hằng để đọc — ngoại lệ có tên `TOOLTIP_DELAY_CHUOT_MS` — và tiếp tục xanh mà không canh gì; chốt chống xanh rỗng của nó (0 hằng `*_MS` là FAIL) không bắt ca này, vì bộ đọc vẫn đếm ngoại lệ.
7. **`Dialog` và `Drawer`: nền đã chở được bất đối xứng vào/ra; ADR này KHÔNG khôi phục nó.** Pha vào và pha ra là hai lớp CSS rời nhau, nên hai thời lượng và hai đường cong dựng được. Quyết định 5 của ADR-0073 mất lý do nền nhưng vẫn là trạng thái hiện hành của hai spec — một thời lượng, một đường cong cho hai chiều. Khôi phục hay không là quyết định của hệ thiết kế: `design-expert` đề xuất, người dùng chốt. Chốt khôi phục thì ghi một dòng ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md) trỏ về ADR này rồi sửa spec — không cần ADR thứ hai, vì *"nền đã đổi thế nào"* đã ghi ở đây.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Chuyển hằng sang `motionOptions.duration`, giữ F37

Đây là **phương án đơn giản nhất**: đổi tên input, giữ nguyên hằng, cổng và canary.

**Được:** ít dòng đổi nhất; F37 và canary sống nguyên; mỗi con số vẫn có chủ và vẫn truy được về token.

**Mất:** giữ lại đúng cái giá mà ADR-0073 gọi là *cái giá chính* — giá trị sống ở hai nơi, đổi token thì phải nhớ rà hằng — trong khi nền đã cho tránh nó. `duration` chỉ chở số nên đường cong vẫn phải đè bằng CSS: một chuyển động lấy thời lượng từ TypeScript và đường cong từ CSS, hai cơ chế cho cùng một nhịp. Và vì thư viện ghi `duration` thành style nội tuyến, token không bao giờ tới phần tử.

**Vì sao loại:** nó trả một cái giá vĩnh viễn để tiết kiệm một lượt sửa.

### Phương án B — Lớp chuyển động riêng của app qua `motionOptions.enterClass` / `leaveClass`

**Được:** tên lớp là của app, không gãy khi thư viện đổi tên lớp ở bản nâng cấp — đúng rủi ro mà phương án đã chọn mang theo.

**Mất:** lớp riêng cần `@keyframes`. Mượn tên `@keyframes` của thư viện thì vẫn gãy khi thư viện đổi tên — chỉ dời chỗ gãy. Tự viết thì app sở hữu luôn **hình dạng** chuyển động của sáu component — một nền song song với thư viện, cùng loại cái giá mà ADR-0073 vế 3 phương án B đã từ chối. Phải bind `motionOptions` ở từng lớp bọc qua ba đường khác nhau (`Menu` nhận thẳng, `AutoComplete` chuyển qua `p-overlay`, `DatePicker` chuyển qua `p-motion`). Lớp nổi neo không có lớp bọc — ô số dòng của `Paginator` — vẫn chạy số của thư viện.

**Vì sao loại:** mua miễn nhiễm đổi tên lớp bằng giá đắt nhất trong các phương án, trong khi spec ở quyết định 5 bắt đúng rủi ro đó với giá một ca test mỗi lớp bọc.

### Phương án C — Chờ thư viện đưa thời lượng chuyển động vào token của preset

**Được:** một chỗ áp ở preset, như nhóm A và như backdrop.

**Vì sao loại:** không có ngày, không ai trong repo kiểm soát nó; trong lúc chờ, mọi giá trị nhóm B đã lệch khỏi token. Cùng lập luận ADR-0073 vế 3 phương án A: nợ chờ một thay đổi ở bên thứ ba là nợ vĩnh viễn đội lốt nợ.

### Phương án D — Đọc token lúc chạy rồi truyền vào `duration`

**Vì sao loại:** ADR-0073 vế 2 phương án B đã loại nó vì đổi một chỗ lệch nhìn thấy được lấy một chế độ hỏng không nhìn thấy được. Nay nó còn thừa: không truyền `duration` thì chính thư viện đọc giá trị đã tính.

### Phương án E — Giữ mã F37, đổi nghĩa thành luật mới

**Được:** không thêm mã; ai quen F37 vẫn tìm thấy chỗ cũ.

**Vì sao loại:** mã luật mang một nghĩa mãi mãi ([`../RULES.md`](../RULES.md)). ADR-0073, ADR-0075 và [ADR-0080](0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md) bất biến và gọi F37 theo nghĩa cũ; đổi nghĩa là để ba ADR trỏ vào một luật chúng chưa từng nói tới.

## Hệ quả

### Tích cực

- **Giá trị thời lượng của nhóm B sống đúng một nơi: token.** Đổi `--dur-base` một lần thì `Dialog`, `Drawer`, ba lớp nổi và backdrop đổi cùng lúc. Cái giá *"phải nhớ rà hằng số"* của ADR-0073 biến mất thay vì được canh.
- **Đường cong lần đầu đi bằng token.** Không còn chuỗi `cubic-bezier(…)` chép tay trong TypeScript cạnh `var(--ease-*)` trong CSS.
- Gỡ một section cổng, một canary, và mọi hằng cùng chuỗi ghép của nhóm B — bớt mã phải bảo trì, bớt một cổng xanh rỗng nghĩa.
- Chuyển động của nhóm B thành loại 1 ở [`../Design/DESIGN.md`](../Design/DESIGN.md) §7: nửa CSS của giảm chuyển động với tới, cộng thêm `safe` của thư viện.
- Lớp nổi neo không qua lớp bọc — ô số dòng của `Paginator` — cũng theo token, thay vì chạy một số không ai đặt.

### Tiêu cực

- **Phụ thuộc ngầm vào tên lớp CSS nội bộ của thư viện.** Khối đè trỏ vào `p-dialog-enter-active`, `p-drawer-enter-<vị trí>`, `p-toast-message-*-active`, `p-anchored-overlay-*-active` — tên trong CSS của thư viện, không phải API công bố. Thư viện đổi tên ở một lượt nâng thì khối đè chết im lặng, thư viện chạy lại số của nó, build và lint vẫn xanh. Đã xảy ra đúng lượt này với khối backdrop `Drawer`. Spec ở quyết định 5 là thứ duy nhất bắt; component bọc mới không có spec đó thì không gì bắt.
- **Bẫy *thời lượng lớn nhất* không lint nào thấy.** Một `transition` dài hơn trên phần tử chuyển động lặng lẽ giành quyền định thời và sinh một cú nhảy ở pha ra. Quyết định 3 là một câu văn; chỉ spec ở quyết định 5 — nếu đo cả pha ra — mới chạm được hệ quả của nó.
- **`src/FE/src/styles/` nằm ngoài khối `core-paths`** ([`../kien-truc-core-module.md`](../kien-truc-core-module.md)). Thời lượng của component Core từ nay đặt ở `_thu-vien.scss`, và sửa tệp đó không kéo lượt `core-reviewer` nào. Hằng `*_MS` cũ nằm dưới `shared/ui/` — trong khối. Tức nửa CSS của F62 được canh **yếu hơn** chỗ F37 từng canh, cho tới khi có spec ở quyết định 5. ADR này không mở rộng khối `core-paths`; đó là một câu hỏi riêng.
- **Quyết định 4 rộng hơn ba component có spec.** Ô số dòng của `Paginator` và mọi lớp nổi neo của thư viện dùng về sau nhận `--dur-base` cùng hai đường cong mà spec của chúng chưa nói tới. Hôm nay chúng chạy 300ms của thư viện — vốn cũng không ai đặt — nhưng đổi vẫn là đổi không ai đặt hàng.
- **Spec ở quyết định 5 phụ thuộc thời điểm.** Nó phải đọc style trong lúc lớp pha còn trên phần tử, và phải chạy khi `prefers-reduced-motion` không bật — bật thì thư viện bỏ qua pha. Một test phụ thuộc cửa sổ thời gian là một test dễ chập chờn.
- **Đúng vai vẫn không máy nào canh.** Spec và khối đè thường do cùng một người viết trong cùng một lượt; nêu `--dur-slow` cho chỗ đáng lẽ `--dur-base` ở cả hai nơi thì spec xanh. Chỗ này `design-expert` canh lúc soát spec — y như dưới F37.
- **Bất đối xứng của `Dialog`, `Drawer` vẫn mất.** Ý đồ *"vào chậm hơn ra"* chưa trở lại; nó mất vì chưa ai quyết, không còn vì không dựng được. Ai đọc ADR-0073 quyết định 5 mà không đọc ADR này sẽ tin rằng vẫn không dựng được.

### Rút lui nếu sai

**Dấu hiệu sai:** spec ở quyết định 5 đỏ sau một lượt nâng PrimeNG mà không tìm ra tên lớp mới để trỏ vào; hoặc thư viện bắt đầu ghi thời lượng nội tuyến, hoặc thôi đọc giá trị đã tính.

**Quy trình**, áp riêng cho từng component bị ảnh hưởng — component khác không phải theo:

1. Khai lại một hằng thời lượng cạnh lớp bọc và truyền `motionOptions.duration` — một số, hoặc `{ enter, leave }` — là một hằng và một binding cho mỗi lớp bọc.
2. Giữ phần `animation-timing-function` ở `_thu-vien.scss`: `duration` không chở đường cong.
3. Cấp một mã luật mới cho ràng buộc *hằng nêu đích danh token* và viết ADR nêu nền đã đổi lần nữa. F37 không hồi sinh: mã của nó gắn với những input đã gỡ.

Không dữ liệu nào sinh ra, không migration: chỉ có CSS và TypeScript. Chi phí: một lượt `frontend-expert`, cộng một lượt `test-engineer` nếu cần dựng lại cổng script.

## ADR cũ — phần nào còn hiệu lực

| ADR | Còn hiệu lực | Hết hiệu lực |
| --- | --- | --- |
| [0073](0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) | Quyết định 1, 2 nguyên vẹn. 5 — là trạng thái hiện hành của spec, lý do nền đã gỡ (quyết định 7 ở trên). 6 — `Toast` giữ bất đối xứng; cơ chế nay là CSS. 7 — đọc *"cùng token với hằng số thời lượng của `Dialog`"* thành *"cùng token với khối đè thời lượng của `Dialog`"*. 8 — nay là khuôn chung, quyết định 1 ở trên mở rộng nó. 9 — như ADR-0075 đã sửa | 3 (số sống trong hằng TypeScript) · 4 (F37) |
| [0075](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) | Quyết định 1 — phần phân loại: spec của ba component có quyền đặt số. 2, 3, 5 nguyên vẹn. 4 — đối tượng đổi từ hằng sang khai báo CSS, ép ở F62 | 1 — phần cơ chế (hằng TypeScript, F37) · 6 (phạm vi của F37) |
| [0080](0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md) | Toàn bộ — ADR này không sửa phần nào | — |

**Về ADR-0080 — ghi nhận, không quyết.** Hai điều kiện xét lại của nó đã chạm: điều kiện 1 (thư viện UI chuyển lớp phủ khỏi bộ chạy hoạt ảnh của Angular — không tệp nào của `primeng` còn import `@angular/animations`) và điều kiện 4 (F37 đổi hình dạng). Độ trễ focus mà điều 4 của nó mô tả đi qua `_focus` của `primeng/dialog`, hàm mà lớp bọc hiện không chạy (`focusOnShow` tắt); chưa ai đo lại. Nửa provider là một hàm Core (`provideCoreAnimations`) và F39 là cổng của nó: bỏ hay giữ là câu hỏi riêng cho người dùng, không thuộc ADR này.

## Việc thi công

**`frontend-expert` — `src/FE/`:**

1. `shared/ui/dialog/`, `shared/ui/drawer/`, `shared/ui/toast/` — gỡ hằng thời lượng, hằng đường cong, chuỗi ghép và binding input cũ (quyết định 2); viết lại chú thích đang trỏ về F37. Lớp bọc `Dialog` không bind `transitionOptions` thay thế nào: bật lại `focusOnShow` thì hẹn giờ mặc định 150ms của thư viện quay lại.
2. `styles/_thu-vien.scss` — khối đè cho từng component (quyết định 1, 3, 4), mỗi khối kèm *"Vì sao"* và *"Hỏng khi nào"* theo [`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §4.4; *"Hỏng khi nào"* nêu đích danh tên lớp của thư viện. Sửa khối backdrop `Drawer` cho trỏ đúng lớp `-active` của v21.
3. `shared/ui/menu/`, `shared/ui/autocomplete/` — không cần mã chuyển động nào: khối đè `p-anchored-overlay` phủ cả hai. `shared/ui/date-picker/` chưa dựng; khi dựng thì cũng vậy.
4. Spec cạnh từng lớp bọc theo quyết định 5; gỡ các ca đang khẳng định giá trị hằng (`drawer.component.spec.ts`, chuỗi `DRAWER_TRANSITION_OPTIONS`); đổi tên ca ở `core/theme/prime-preset.spec.ts` đang nhắc `DIALOG_THOI_LUONG_MS`, và chú thích ở `core/theme/prime-preset.ts` nhắc hằng đó cùng F37.
5. Ngoài quyết định này, cùng lượt cho gọn: `[showCloseIcon]` của `Drawer` deprecated — dùng `closable`.

**`test-engineer` — `scripts/`:** gỡ section `F37` của `scripts/fe-gate.sh`, `scripts/tests/fe-gate-f37.test.sh`, mã F37 trong dòng liệt kê đầu `fe-gate.sh`, và câu *"cùng bẫy với F37"* ở chú thích một section khác — **cùng lượt** với bước 1 của `frontend-expert` (quyết định 6). Không dựng section cho F62.

**`design-expert` — `docs/Design/`:** mọi câu nói số chuyển động đi qua hằng TypeScript hay qua `transitionOptions` / `showTransitionOptions` / `hideTransitionOptions` — ở `Components/Dialog.md`, `Drawer.md`, `Toast.md`, `Menu.md`, `Autocomplete.md`, `DatePicker.md` — đổi sang *"đè bằng token ở `_thu-vien.scss`"*. [`../Design/DESIGN.md`](../Design/DESIGN.md) §7: hàng loại 2 của bảng *Giảm chuyển động* không còn ví dụ PrimeNG nào; §6.4 mục 3 viện dẫn F37 cho hằng `zIndex`. Đề xuất có khôi phục bất đối xứng vào/ra cho `Dialog`, `Drawer` không (quyết định 7), và soát nhịp của lớp nổi neo không có spec (quyết định 4).

## Liên quan

- [`0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) · [`0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md`](0075-ba-component-lop-noi-vao-nhom-b-va-bac-thang-chon-theo-vai.md) — hai ADR bị sửa một phần.
- [`0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md`](0080-nua-provider-giam-chuyen-dong-nap-luoi-va-song-o-core.md) — điều kiện xét lại đã chạm, chưa xét.
- [`../RULES-FE.md`](../RULES-FE.md) §7 — F37 (hết áp dụng), F62.
- [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md) §7.2 — cơ chế, hai bẫy.
- [`../Design/DESIGN.md`](../Design/DESIGN.md) §7 — thang thời lượng, vai từng bậc, ba loại chuyển động.
