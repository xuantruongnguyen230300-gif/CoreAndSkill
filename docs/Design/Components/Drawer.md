---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Drawer

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Component đã có ở `src/FE/src/app/shared/ui/drawer/` (FE-3, 2026-10-08) — mới biến thể `edit`; biến thể `inspect` **hoãn** theo quyết định của người dùng (§"Biến thể `inspect` — hoãn"). Bảng "Đã có → còn thiếu" ngay dưới khai đúng những mục đã mở source ra so; mục không có tên trong bảng thì chưa ai đối chiếu.

**Nền:** **bọc PrimeNG**. Cùng nhóm "khó" với [`Dialog.md`](./Dialog.md) theo [`../COMPONENTS.md`](../COMPONENTS.md) §4: bẫy focus, khôi phục focus khi đóng, khoá cuộn nền. Đúng được toàn bộ ca biên là hàng trăm dòng và rất nhiều lần sai.

## Đã có → còn thiếu

Component: `src/FE/src/app/shared/ui/drawer/drawer.component.ts` — selector `app-drawer`, class `DrawerComponent`, bọc `p-drawer` (PrimeNG 21.1.10 cài ở thư mục `node_modules` — không có trong git; khai phụ thuộc ở `src/FE/package.json`). Đối chiếu 2026-10-08 bằng **đọc mã**; ba dòng dựa vào mã thư viện — lớp xếp chồng, `role`, bẫy focus — và dòng chuyển động đọc lại trên bản 21.1.10 ngày 2026-11-07; có ca kiểm ở `drawer.component.spec.ts` (tên ca ghi ở cột "Đã có"), nhưng **người soạn spec chưa tự chạy** bộ test — xanh/đỏ do lượt chạy test báo. Hình thức thật (bề rộng, bo góc, bóng, chuyển động) **chưa đo trên trình duyệt**.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| Biến thể `edit` | Có: `drawer.component.html` chuỗi `[modal]="true"`, `[blockScroll]="true"`, `[dismissible]="true"` — lớp phủ, khoá cuộn nền, bấm lớp phủ đóng. `variant = input<'edit'>('edit')` chỉ nhận một giá trị | — |
| Biến thể `inspect` | Không dựng — đúng quyết định hoãn (2026-10-08): `variant` chỉ nhận `'edit'`, khớp §API; chú thích lớp ở `drawer.component.ts` ghi cùng quyết định (chuỗi `HOÃN`, trỏ về mục hoãn của file này) | — (không phải nợ của code hôm nay; điều kiện mở lại ở §"Biến thể `inspect` — hoãn") |
| Cỡ `sm` / `md` / `lg` | Có: lớp `app-drawer--<cỡ>` (`drawer.component.ts` § `lopGoc`); bề rộng `min(var(--layout-drawer-w-lg), 92vw)` ở `src/FE/src/styles/_thu-vien.scss` § `.app-drawer--lg.app-drawer--right`; ba token là bí danh thang dialog ở `src/FE/src/styles/_spacing.scss` § `--layout-drawer-w-lg` | — |
| Vị trí `right` / `left` / `bottom` | Có. Dưới `$bp-sm` neo `bottom`, `left` giữ nguyên — ca `dưới $bp-sm: neo đáy; position="left" giữ neo trái ở mọi ngưỡng`. Truy vấn lấy từ hằng dùng chung, không tự khai (đọc mã 2026-10-08): `drawer.component.ts` § `matchMedia?.(TRUY_VAN_DUOI_BP_SM)`, hằng ở `src/FE/src/app/core/theme/diem-ngat.ts` § `TRUY_VAN_DUOI_BP_SM`; cổng F58 của `scripts/fe-gate.sh` OK cùng ngày. Cao tối đa 85vh, có vạch ở đầu: `_thu-vien.scss` § `.app-drawer--bottom`. Vạch là phần tử giả `&::before` với `content: ''` — không mang chữ nào để trình đọc màn hình đọc, không nhận focus; không có trình nghe kéo nào dưới `src/FE/src/app/shared/ui/drawer/` (không có chuỗi `pointerdown`, `touchstart`) — khớp §Responsive, dòng < `$bp-sm`. Chú thích trong `_thu-vien.scss` nay gọi đúng tên (chuỗi `vạch trang trí ở đầu`); bốn token của vạch khớp bảng "Vạch trang trí của drawer neo đáy" ở §Kích thước | — |
| Lớp xếp chồng | Có: `p-drawer` đặt `z-index` của tấm bằng bậc `modal` trong cấu hình thư viện, lớp phủ thấp hơn tấm một đơn vị (gói `primeng` đã cài, tệp `primeng-drawer.mjs`, chuỗi `ZIndexUtils.set('modal'`); bậc đó là `src/FE/src/app/core/theme/prime-z-index.ts` chuỗi `modal: 400, // --z-backdrop` — khớp §Kích thước | — |
| Ba phần, chỉ thân cuộn | Có: slot đầu (`<h2>` tiêu đề + nút đóng do thư viện vẽ, bật bằng `drawer.component.html` chuỗi `[closable]="true"` — đọc mã 2026-11-07), thân, chân (`[slot=footer]`); ca `chân nằm ngoài thân cuộn (chỉ thân cuộn)` | — |
| `role="dialog"`, `aria-modal`, `aria-labelledby` | Có — **đặt lại sau khi mở** vì `p-drawer` cứng `role="complementary"` (gói `primeng` đã cài, tệp `primeng-drawer.mjs`, chuỗi `role="complementary"`): `drawer.component.ts` § `khiHien`; ca `mở: phần tử gốc mang role="dialog", aria-modal, aria-labelledby trỏ vào tiêu đề và id = panelId` | Thuộc tính đặt bằng DOM sau sự kiện `onShow`, không bằng khai báo — PrimeNG vẽ lại phần tử gốc thì mất; chưa thấy ca ép điều đó |
| Bẫy focus | Có, do `p-drawer` (`pFocusTrap` gắn sẵn); ca `bẫy focus: Tab ra khỏi phần tử cuối vòng về phần tử đầu, không thoát ra nền` | — |
| Focus khi mở | Có: vào ô nhập đầu tiên của thân, không có thì vào nút đóng — ca `mở: focus vào ô nhập ĐẦU TIÊN, không vào nút đóng` và `mở: thân không có ô nhập → focus vào nút đóng`. Đặt focus qua hàm gắn cờ ô tự nhận focus (đọc mã 2026-10-08): `drawer.component.ts` § `khiHien` gọi `datFocusLucMo(dich, { preventScroll: true })`, hàm ở `src/FE/src/app/shared/forms/focus-luc-mo.ts` — khớp §Accessibility dòng *Focus khi mở* (rời ô đầu khi chưa sửa gì thì không báo) | Không ca nào của `drawer.component.spec.ts` kiểm cờ `data-focus-luc-mo` trên ô đầu; hành vi cờ có ca ở `focus-luc-mo.spec.ts` và `input.component.cham.spec.ts`, không qua `Drawer` |
| Focus khi đóng | Có, mọi đường: nút đóng, `Escape`, bấm lớp phủ, trang cha đặt `open = false`; phần tử đã mở không còn trong DOM thì về `main` (`drawer.component.ts` § `traFocus`). Ca: `Escape: phát closed…`, `nút đóng…`, `bấm lớp phủ…`, `trang cha đặt open = false…`, `phần tử đã mở không còn trong DOM → focus về vùng <main>` | — |
| `loading` | Có: `aria-busy="true"` trên thân, đầu và chân vẫn dựng — ca `loading → aria-busy="true" trên thân, đầu và chân vẫn có mặt` | — |
| Nhả lớp phủ và khoá cuộn nền khi đóng — §Accessibility dòng *Cuộn nền* (đọc mã 2026-11-07) | Có. Đóng thường: thư viện gỡ lớp phủ khi pha ra kết thúc. Component bị huỷ giữa pha ra — khe thư viện không tự dọn: `drawer.component.ts` chuỗi `this.destroyRef.onDestroy(() => this.donLopPhuKhiHuy())`; § `donLopPhuKhiHuy` gọi `destroyModal()` của thư viện rồi gỡ thẳng lớp phủ khỏi DOM. Ca `đóng xong hoạt ảnh ra: lớp phủ được gỡ khỏi <body> và khoá cuộn nền được nhả`, `huỷ component giữa lúc lớp phủ đang ra: không để lại lớp phủ và khoá cuộn nền`; người soạn chưa chạy test | Chưa đo trên trình duyệt |
| Chuyển động (đọc mã 2026-11-07) | **Có trong mã, theo bảng ba cơ chế ở §Trạng thái.** Tấm: `_thu-vien.scss` § `&:is(.p-drawer-enter-left, .p-drawer-enter-right, .p-drawer-enter-bottom)` mang `var(--dur-base)` + `var(--ease-decelerate)`, § `&:is(.p-drawer-leave-left, .p-drawer-leave-right, .p-drawer-leave-bottom)` mang `var(--dur-base)` + `var(--ease-accelerate)`; keyframes của thư viện giữ nguyên. `transition: transform` của thư viện: **gỡ hẳn** — § `.app-drawer` khai `transition: none`, nhánh thứ hai mà dòng hai của bảng ba cơ chế cho phép. Chế độ toàn màn: không đè, đúng §Cần chốt 3. Lớp phủ: § `.p-drawer-mask.p-overlay-mask-enter-active` và § `.p-drawer-mask.p-overlay-mask-leave-active` — trỏ đúng lớp pha của bản đang cài — chạy keyframes riêng của app (§ `@keyframes app-drawer-mask-vao`, § `@keyframes app-drawer-mask-ra`, màu `--color-overlay`) với `var(--dur-base)` và `var(--ease-standard)` ở cả hai pha. Lớp bọc không truyền thời lượng nào: không tệp nào dưới `src/FE/src` còn chuỗi `DRAWER_THOI_LUONG_MS`, `DRAWER_DUONG_CONG` hay `[transitionOptions]`; ca kiểm hằng cũ đã gỡ. Ca kiểm `drawer.component.spec.ts`: `tấm — pha vào: --dur-base, --ease-decelerate`, `tấm — pha ra: --dur-base, --ease-accelerate`, `lớp phủ — cả hai pha: --dur-base, --ease-standard, keyframes riêng của app`; người soạn chưa chạy test | **Spec không khai đường cong và keyframes của lớp phủ** — §Trạng thái chỉ quyết tấm; mã chọn `--ease-standard` và keyframes riêng, chưa ai chốt đó là ý định. Nhịp trên trình duyệt **chưa đo**, kể cả việc tấm không nhảy về chỗ cũ ở pha ra. Nhánh giảm chuyển động **chưa đo**: các ca trên tự `pending` khi cờ bật — cách kiểm ở [`../DESIGN.md`](../DESIGN.md) §7 *Giảm chuyển động* |
| `panelId` | Có, **ngoài bảng API ban đầu**: `drawer.component.ts` chuỗi `readonly panelId = input<string \| null>(null)` — đặt `id` lên phần tử gốc, đích `aria-controls` của nút mở. Đã thêm vào §API dự kiến | — |
| `dirty` · `confirmDiscard` | — | **Chưa dựng.** Hôm nay không màn nào cần: panel lọc luôn `dirty = false`, chưa có màn sửa nằm trong Drawer |
| `fullScreen` của thư viện | Lớp bọc **không phơi** (không có chuỗi `fullScreen` ngoài chú thích dưới `src/FE/src/app`) — khớp §Kích thước: trần `92vw`, tấm phủ kín màn là `Dialog` toàn màn | — |
| `closed` | Phát khi người dùng đóng (nút đóng, `Escape`, lớp phủ); **không** phát khi trang cha tự đặt `open = false` — khớp danh sách ở §API | — |
| `Escape` hỏi xác nhận khi `dirty` · `aria-live` khi đổi bản ghi | — | Thuộc `dirty` (chưa dựng) và `inspect` (hoãn) |

---

## Mục đích

Mở một tấm trượt từ cạnh màn hình để xem hoặc sửa nhanh một bản ghi, mà danh sách phía sau vẫn còn nguyên.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Xem chi tiết một hàng trong [`DataTable.md`](./DataTable.md) mà không rời danh sách | 🛑 Việc cần **dứt điểm** trước khi làm tiếp: xác nhận xoá, form tạo mới → [`Dialog.md`](./Dialog.md). Xem ranh giới dưới đây |
| Sửa nhanh vài trường của một bản ghi, rồi sửa tiếp bản ghi kế bên | 🛑 Nội dung dài, nhiều nhóm trường, cần đọc kỹ → một **màn riêng**, không phải lớp nổi |
| Panel lọc của màn danh sách — [`FilterPanel.md`](./FilterPanel.md) tự đặt `Drawer` biến thể `edit`, cỡ `lg`, `dirty` luôn `false` (đặc tả ở §"Trong `Drawer`" của file đó); mở bằng nút lọc của [`Toolbar.md`](./Toolbar.md), đóng thì focus về nút lọc | 🛑 Danh sách hành động ngắn → [`Menu.md`](./Menu.md) |
| Dạng drawer của [`Sidebar.md`](./Sidebar.md) ở màn nhỏ | 🛑 Một dòng chú → [`Tooltip.md`](./Tooltip.md) |

### Ranh giới với `Dialog` — câu hỏi quyết định

> **Người dùng có cần nhìn thấy thứ phía sau trong lúc thao tác không?**
>
> Có → `Drawer`. Không → `Dialog`.

Đó là khác biệt thật, và nó kéo theo mọi thứ còn lại: `Dialog` nằm giữa màn hình vì nó muốn bạn nhìn nó và chỉ nó; `Drawer` nép vào cạnh vì nó muốn bạn còn thấy danh sách. Một `Dialog` đặt sát mép màn hình vẫn là `Dialog`, và một `Drawer` phủ kín màn hình thì đã không còn là `Drawer`.

Hệ quả: `Drawer` **không dùng cho thao tác phá huỷ**. Xác nhận xoá cần cắt đứt ngữ cảnh để người dùng dừng lại và đọc — đó đúng là việc của [`ConfirmDialog.md`](./ConfirmDialog.md).

`Drawer` là component riêng, không phải biến thể của `Dialog`: `inspect` không bẫy focus và không mang `aria-modal`, ngược hẳn định nghĩa `Dialog`; mục lục [`../COMPONENTS.md`](../COMPONENTS.md) §3 giữ hai dòng.

## Biến thể

| Biến thể | Nền phía sau | Dùng khi |
| --- | --- | --- |
| `inspect` | **Không có lớp phủ.** Danh sách phía sau vẫn bấm được, chọn hàng khác thì drawer đổi nội dung theo | Xem chi tiết, duyệt qua nhiều bản ghi liên tiếp. **Hoãn** — §"Biến thể `inspect` — hoãn" |
| `edit` | Có lớp phủ `--color-overlay`, nền phía sau khoá lại | Khi drawer chứa form đang sửa — tránh mất dữ liệu đang gõ vì một cú bấm nhầm ra ngoài. **Mặc định** trong lúc `inspect` hoãn |

**Biến thể `inspect` là lý do component này tồn tại.** Nó cho phép bấm hàng này xem, bấm hàng kia xem tiếp, không phải đóng mở liên tục. Đó là thao tác thật của người kiểm tra dữ liệu, và `Dialog` không làm được vì nó chặn. Chọn hàng khác thì nội dung **đổi tại chỗ**, không đóng rồi mở lại; đang tải dở mà chọn tiếp thì màn bỏ kết quả của request cũ — luật ở [`Autocomplete.md`](./Autocomplete.md) §API.

Cái giá của `inspect`, phải nói rõ: nó **không** bẫy focus (nền sau vẫn thao tác được thì bẫy focus là mâu thuẫn), nên nó cũng **không** mang `aria-modal`. Hai biến thể vì vậy khác nhau cả về ngữ nghĩa ARIA — xem mục Accessibility. `p-drawer` **không** cho tắt bẫy focus (đã đọc 2026-10-08, đọc lại trên bản 21.1.10 ngày 2026-11-06) — §Cần chốt 2.

### Biến thể `inspect` — hoãn

Chốt 2026-10-08 (người dùng): **không dựng `inspect` bây giờ.** Biến thể vẫn nằm trong spec — mọi dòng nói về `inspect` ở §Responsive, §Accessibility, §Do / Don't là đích đến của nó khi mở lại — nhưng không phải việc còn nợ của code hôm nay.

| Khoản | Nội dung |
| --- | --- |
| Vì sao hoãn | Hôm nay không màn nào cần: màn danh sách chỉ mở panel lọc, và panel lọc dùng `edit` ([`FilterPanel.md`](./FilterPanel.md) §"Trong `Drawer`"). `inspect` lại không bọc được `p-drawer` (§Cần chốt 2), nên dựng nó là tự dựng cả phần khung — một khối việc lớn không có màn nào để đối chiếu |
| Trong lúc hoãn | `variant` chỉ nhận `'edit'`, mặc định `'edit'` (§API) |
| Điều kiện mở lại | Một spec màn dưới [`../Screens/`](../Screens/) cần thao tác "bấm hàng này xem, bấm hàng kia xem tiếp" trong khi danh sách phía sau vẫn bấm được, và ghi rõ nó cần `inspect`. Chưa có spec màn như vậy thì không dựng |
| Khi mở lại | Tự dựng phần khung theo câu trả lời ở §Cần chốt 2; `edit` vẫn bọc. `variant` thành `'inspect' \| 'edit'`, mặc định `'inspect'` — dạng nhẹ nhất, sai theo hướng an toàn; rà mọi nơi gọi không truyền `variant`, vì mặc định đổi dưới chân chúng |

| Vị trí neo | Dùng khi |
| --- | --- |
| `right` | **Mặc định.** Chi tiết bản ghi, panel lọc — hướng đọc tự nhiên đi từ danh sách bên trái sang |
| `left` | Chỉ cho dạng drawer của `Sidebar` ở màn nhỏ, để khớp vị trí nó vốn đứng |
| `bottom` | Chỉ ở màn < `$bp-sm`, nơi tấm trượt ngang không đủ bề rộng |

## Kích thước

`Drawer` **không** dùng thang `--size-control-*`. Cỡ ở đây là **bề rộng**, và chiều cao luôn là trọn màn hình.

| Cỡ | Bề rộng | Dùng khi |
| --- | --- | --- |
| `sm` | `--layout-drawer-w-sm` | Xem nhanh vài trường |
| `md` | `--layout-drawer-w-md` | **Mặc định.** Chi tiết bản ghi, form ngắn |
| `lg` | `--layout-drawer-w-lg` | Panel lọc nhiều trường, nội dung có bảng con |

Ba bậc là bí danh của thang `--layout-dialog-w-*` ([`../DESIGN.md`](../DESIGN.md) §6.1) — cùng họ lớp nổi có đầu–thân–chân, một thang, không đẻ bậc mới.

Trần cứng: `min(<bề rộng cỡ>, 92vw)`. Một drawer rộng hơn 92% màn hình đã là một `Dialog` toàn màn, và lúc đó nên dùng đúng component đó.

Ba phần, giống `Dialog`: đầu (tiêu đề + nút đóng), thân (cuộn được), chân (hành động). **Chỉ thân cuộn** — đầu và chân đứng yên, vì nút đóng phải luôn với tới được và nút Lưu không được trôi mất ở một form dài.

Bo góc: `--radius-0` ở cạnh dính mép màn hình, `--radius-lg` ở hai góc còn lại. Đổ bóng `--shadow-4`.

**Vạch trang trí của drawer neo đáy** (dưới `$bp-sm`, §Responsive; chốt 2026-10-08, người dùng — ghi đúng bộ code đang vẽ, `src/FE/src/styles/_thu-vien.scss` § `.app-drawer--bottom`, khối `&::before`):

| Khoản | Token |
| --- | --- |
| Bề rộng | `--sp-8` |
| Độ dày | `--sp-2` |
| Khoảng cách tới mép trên của tấm | `--sp-3` (đệm trên của vạch) |
| Bo góc | `--radius-full` |
| Màu | `--color-border` |

Vạch căn giữa theo trục ngang, không co lại khi tấm hẹp. Là nét vẽ thuần (phần tử giả không có nội dung): không nhận focus, không vào cây accessibility.

Lớp: `--z-backdrop` cho cả tấm lẫn lớp phủ — cùng bậc `Dialog`, **dưới** `ConfirmDialog`, để hộp "bỏ thay đổi?" mở ra nằm trên drawer. Thang lớp và luật thứ tự: [`../DESIGN.md`](../DESIGN.md) §6.4; file này không khai lại.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền `--color-border` ở cạnh giáp nội dung, `--shadow-4`. Trượt bằng `transform`, `--dur-base` cho cả hai chiều; đường cong `--ease-decelerate` chiều vào, `--ease-accelerate` chiều ra — **một thời lượng, hai đường cong**, §Chuyển động vào/ra | Có |
| `hover` | **Không áp dụng cho chính tấm drawer.** Nó là vùng chứa; hover thuộc về từng control bên trong | — |
| `focus-visible` | **Không áp dụng cho chính tấm drawer** — nó không nhận focus. Nhưng thứ tự Tab bên trong là bắt buộc: nút đóng → nội dung theo thứ tự đọc → nhóm hành động ở chân | — |
| `active` | **Không áp dụng.** Không có gì để nhấn xuống | — |
| `disabled` | **Không áp dụng cho chính tấm drawer.** Từng control con nhận `disabled` thật. 🛑 Không phủ một lớp `pointer-events: none` lên cả drawer — control biến mất khỏi thứ tự Tab mà trình đọc màn hình không biết vì sao | — |
| `loading` | Nội dung chưa về: màn đặt [`SkeletonLoader.md`](./SkeletonLoader.md) theo đúng hình dạng nội dung sắp tới vào slot thân; `Drawer` đặt `aria-busy="true"` trên thân. **Đầu và chân dựng ngay** — tiêu đề và nút đóng phải có mặt từ khung hình đầu tiên, nếu không người dùng mở nhầm thì không có đường thoát | Có |
| `error` | Tải nội dung hỏng: màn đặt [`EmptyState.md`](./EmptyState.md) biến thể `error` kèm nút "Thử lại" vào slot thân. **Drawer không tự đóng** — đóng đi thì người dùng không biết vừa có chuyện gì | Có |
| `empty` | Bản ghi không còn tồn tại (vừa bị người khác xoá): màn đặt `EmptyState` nói rõ điều đó vào slot thân, chỉ có nút "Đóng". Phân biệt hẳn với `error` — một cái là hỏng, một cái là mất | Có |

### Chuyển động vào/ra — một thời lượng, hai đường cong, đến từ ba cơ chế

Chiều vào và chiều ra dùng **chung** `--dur-base`; đường cong tách theo hướng — `--ease-decelerate` chiều vào, `--ease-accelerate` chiều ra (chốt 2026-11-06, người dùng — dòng cùng ngày ở [`../../adr/nhat-ky-quyet-dinh.md`](../../adr/nhat-ky-quyet-dinh.md)). Pha vào và pha ra là hai lớp CSS rời nhau, nên hai đường cong dựng được ([`../../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md`](../../adr/0123-chuyen-dong-component-primeng-di-bang-token-qua-css-go-hang-ms-f37-het-ap-dung.md) quyết định 7), và [`../DESIGN.md`](../DESIGN.md) §7 khai đích danh hai vai hướng đó. Thời lượng không tách: §7 giao việc trượt drawer cho **một** bậc, `--dur-base`, và thang không có vai nào cho một thời lượng riêng chiều ra — cùng lập luận với [`Dialog.md`](./Dialog.md) §Trạng thái. Trạng thái trước chốt này — một thời lượng, một đường cong `--ease-standard` cho hai chiều — là [`../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md`](../../adr/0073-token-thoi-luong-noi-o-mot-khoa-semantic-nhom-b-di-bang-hang-so-co-ten.md) quyết định 5. Hình dạng là keyframes của thư viện: tấm trượt vào từ cạnh neo, trượt ra về cạnh đó, không mờ dần.

🛑 **Người dựng phải biết: chuyển động của component này đến từ BA cơ chế, không phải một.** Đây là chỗ phức tạp nhất spec để lại, và giấu nó đi thì người sửa sau sẽ đổi một cơ chế rồi tưởng đã đổi hết.

| Cơ chế | Chở khoản nào | Token tới bằng đường nào |
| --- | --- | --- |
| CSS của thư viện — lớp pha vào/ra theo vị trí neo, gắn lên tấm trong lúc chuyển động | Nhịp vào/ra của chính tấm trượt | Thời lượng, đường cong viết cứng trong lớp pha. **Đè** bằng một khối ở `src/FE/src/styles/_thu-vien.scss`: `--dur-base` cho cả hai pha, `--ease-decelerate` cho pha vào, `--ease-accelerate` cho pha ra, giữ nguyên keyframes của thư viện; lớp bọc không truyền số nào bằng TypeScript — luật **F62** ([`../../RULES-FE.md`](../../RULES-FE.md) §7) |
| CSS tĩnh của thư viện — `@primeuix/styles/drawer` § `transition: transform 0.3s` trên `.p-drawer` | Thời lượng của chính phép trượt `transform` | Một con số trần, không qua biến nào. **Đè** cùng tệp: thời lượng `--dur-base`, bằng hai pha — thư viện định thời theo thời lượng lớn nhất trên tấm, một `transition` dài hơn giành quyền kết thúc pha và tấm nhảy về chỗ cũ trước khi ẩn. Đường cong theo pha đang chạy — `--ease-decelerate` trong pha vào, `--ease-accelerate` trong pha ra — hoặc gỡ hẳn `transition` này: ADR-0123 quyết định 3 đòi mọi `transition` trên phần tử chuyển động mang cùng token với pha đang chạy, hoặc bị gỡ. Chọn cách nào là việc của người dựng |
| CSS của thư viện — lớp pha vào/ra riêng của chế độ toàn màn | Chuyển động của chế độ toàn màn thư viện | Cùng đường đè như dòng đầu — **không dựng** vì lớp bọc không phơi chế độ này (§Cần chốt 3) |

Mọi khối đè là CSS và sống ở `_thu-vien.scss` — quyết định 8 của ADR-0073, nay là khuôn chung của F62 cho mọi component PrimeNG (ADR-0123 quyết định 1). Khối nào đã dựng, khối nào chưa: §Đã có → còn thiếu, dòng *Chuyển động*.

⚠️ **Chế độ toàn màn của thư viện không phải một biến thể của spec này.** §Biến thể chỉ có `inspect` và `edit`, và §Kích thước chặn trần ở `min(<bề rộng cỡ>, 92vw)` — một tấm trượt phủ kín màn đã là `Dialog` toàn màn. Khối đè ở dòng cuối bảng trên vì thế sẽ canh một chế độ Core **không** vẽ tới. Đã chốt đóng (2026-10-08, theo điều kiện ghi ở §Cần chốt 3): lớp bọc khoá hẳn chế độ này nên khối đó **không được dựng**; nếu sau này phơi `fullScreen` thì phải dựng khối đè cùng lúc.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-border`, `--color-border-subtle`, `--color-text`, `--color-text-muted`, `--color-overlay`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-lg`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-2` (độ dày vạch trang trí), `--sp-3` (khoảng cách vạch tới mép trên), `--sp-4`, `--sp-5`, `--sp-6`, `--sp-8` (bề rộng vạch trang trí) |
| Hình dạng | `--radius-0`, `--radius-lg`, `--radius-full` (vạch trang trí), `--border-w`, `--border-w-strong` |
| Kích thước | `--layout-drawer-w-sm`, `--layout-drawer-w-md`, `--layout-drawer-w-lg` |
| Bóng, lớp | `--shadow-4`, `--z-backdrop` |
| Chuyển động | `--dur-base`, `--ease-decelerate`, `--ease-accelerate` |

🛑 **Ba token ở dòng cuối bảng tới đích bằng khối đè CSS ở `_thu-vien.scss`, và mọi khối chạm tấm phải mang token của pha đang chạy.** Một khối đè lớp pha vào/ra, một khối đè — hoặc gỡ — `transition: transform` của tấm — §Trạng thái, bảng ba cơ chế. Không phần nào đi qua TypeScript. Bảng này vẫn giữ token vì `Drawer` vẫn **quyết** giá trị — khác hẳn [`Tooltip.md`](./Tooltip.md), nơi bảng token bỏ hẳn nhóm chuyển động vì spec ở đó không đặt được số nào cả.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Neo phải, bề rộng theo cỡ. Biến thể `inspect` không có lớp phủ |
| `$bp-md` … `$bp-lg` | Giữ nguyên; cỡ `lg` co xuống 92vw nếu cần |
| < `$bp-md` | **Biến thể `inspect` chuyển thành `edit`** — có lớp phủ, có bẫy focus. Ở màn hẹp drawer che gần hết danh sách rồi, nên lời hứa "vẫn thấy phía sau" không còn đúng, và giữ nó là nói dối |
| < `$bp-sm` | Neo **đáy**, cao tối đa 85vh, có một **vạch trang trí** ở đầu (chốt 2026-10-08, người dùng): chỉ là nét vẽ, **không kéo được** — không có thao tác kéo, không nhận focus, ẩn với trình đọc màn hình. Đóng vẫn bằng nút đóng, `Escape`, bấm lớp phủ. Trượt từ đáy đặt nút hành động vào vùng ngón cái với tới. **Miễn trừ:** `position: left` giữ neo trái ở mọi ngưỡng — drawer điều hướng của [`Sidebar.md`](./Sidebar.md) trượt từ trái, không đổi sang `bottom` |

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Vai trò | `role="dialog"`. Biến thể `edit` thêm `aria-modal="true"`; biến thể `inspect` **không** mang thuộc tính đó — nền sau vẫn thao tác được thì khai `aria-modal` là khai sai |
| Nhãn | `aria-labelledby` trỏ vào tiêu đề ở đầu drawer. Tiêu đề phải nói **bản ghi nào**, không phải "Chi tiết" |
| Bẫy focus | Chỉ ở `edit`. `Tab` vòng trong drawer, không thoát ra nền sau |
| Focus khi mở | Vào **nút đóng** ở `inspect`; vào **ô nhập đầu tiên** ở `edit`. Ô đầu tự nhận focus đó theo luật chỉ báo *bắt buộc* của [`FormRow.md`](./FormRow.md) (rời ô khi chưa sửa gì thì không báo) |
| Focus khi đóng | Trả về đúng phần tử đã mở drawer — thường là một [`IconButton.md`](./IconButton.md) trong hàng bảng, hoặc nút lọc của [`Toolbar.md`](./Toolbar.md). Áp cho **mọi** đường đóng: nút đóng, `Escape`, bấm lớp phủ, và khi trang cha đặt `open = false` (bấm Áp dụng ở panel lọc). `Drawer` ghi nhớ phần tử đang giữ focus lúc mở rồi trả về đó; phần tử đó không còn trong DOM thì focus về vùng `main`. Đây là khoản hay bị bỏ nhất |
| Bàn phím | `Escape` đóng. Ở `edit` có dữ liệu chưa lưu thì `Escape` **hỏi xác nhận** trước, không đóng thẳng |
| Cuộn nền | Khoá ở `edit`, giữ nguyên ở `inspect` |
| Đổi bản ghi khi đang mở | Ở `inspect`, chọn hàng khác thì nội dung đổi — phải báo qua `aria-live="polite"`, nếu không người dùng trình đọc màn hình không biết drawer vừa đổi nội dung |
| Chữ | Đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `open` | input | `boolean` | `false` | Trang cha giữ nguồn sự thật |
| `variant` | input | `'edit'` | `'edit'` | `'inspect'` hoãn — kiểu và mặc định khi mở lại: §"Biến thể `inspect` — hoãn" |
| `position` | input | `'right' \| 'left' \| 'bottom'` | `'right'` | |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `panelId` | input | `string \| null` | `null` | `id` đặt lên phần tử gốc — đích `aria-controls` của nút mở (nút lọc của [`Toolbar.md`](./Toolbar.md)). Thêm 2026-10-08 theo [`FilterPanel.md`](./FilterPanel.md) §"Trong `Drawer`", dòng `id` |
| `title` | input | `string` | — | **Bắt buộc.** Không có mặc định; một lớp nổi không tiêu đề là một lớp nổi vô danh |
| `loading` | input | `boolean` | `false` | |
| `dirty` | input | `boolean` | `false` | Có dữ liệu chưa lưu. Khi `true`, mọi đường đóng đều đi qua bước hỏi xác nhận |
| `closed` | output | `void` | — | Phát mọi lần drawer đóng: nút đóng, `Escape`, bấm lớp phủ |
| `confirmDiscard` | output | `void` | — | Phát khi người dùng chọn bỏ thay đổi ở bước hỏi xác nhận |

Nội dung vào qua ba slot: đầu, thân, chân. Khác [`Menu.md`](./Menu.md) — ở đó nội dung phải vào qua mảng vì component cần biết danh sách mục để làm phím mũi tên; ở đây không có hành vi nào phụ thuộc nội dung, nên slot là đúng. Nội dung tự dựng của `loading`, `error`, `empty` cũng vào qua slot thân — `Drawer` ở tầng bọc nên không import chúng ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5).

`Drawer` là component **dumb** — nó không tự tải bản ghi ([`../COMPONENTS.md`](../COMPONENTS.md) §5).

## Do / Don't

- ✅ Dùng `inspect` khi người dùng sẽ xem nhiều bản ghi liên tiếp.
- ✅ Dựng đầu và chân ngay từ khung hình đầu tiên, kể cả khi thân còn đang tải.
- ✅ Hỏi xác nhận trước khi đóng một drawer có dữ liệu chưa lưu.
- ✅ Trả focus về đúng nút đã mở nó.
- ❌ Không dùng `Drawer` cho xác nhận xoá. Đó là việc của `ConfirmDialog`.
- ❌ Không cho thân, đầu và chân cùng cuộn.
- ❌ Không mở drawer chồng lên drawer. Cần đi sâu một cấp nữa thì đó là một màn riêng.
- ❌ Không khai `aria-modal` cho biến thể `inspect`.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có cho kéo đổi bề rộng không? Người dùng bảng nhiều cột hay muốn drawer hẹp lại. Đổi lại là một thao tác chuột nữa phải có đường đi bằng bàn phím | Sau F3 — dự án hạ nguồn đầu tiên có bảng nhiều cột kèm drawer |
| 2 | **Đã chốt 2026-10-08 (người dùng): hoãn `inspect`** — §"Biến thể `inspect` — hoãn". Bằng chứng kỹ thuật giữ lại cho lần mở lại: `p-drawer` 21.1.10 **không** cho tắt bẫy focus (đọc lại 2026-11-06) — template của nó gắn cứng `pFocusTrap` trên phần tử gốc, không nối `[pFocusTrapDisabled]` ra input nào (đọc tệp `primeng-drawer.mjs` trong gói `primeng` đã cài, chuỗi `pFocusTrap`; tệp này không nằm trong git nên người đối chiếu tự mở từ `node_modules`); và cứng `role="complementary"`, không `aria-modal`. Hệ quả khi mở lại: `inspect` **tự dựng phần khung**, `edit` vẫn bọc | Đã trả lời |
| 3 | **Tự đóng 2026-10-08:** lớp bọc không phơi `fullScreen` (`drawer.component.ts` không có chuỗi đó ngoài chú thích), khối override không dựng; mở lại nếu ai phơi `fullScreen`. Câu hỏi gốc: khối đè cho chế độ toàn màn (lớp pha riêng của chế độ đó, §Trạng thái) có đáng dựng không, khi §Biến thể và §Kích thước của chính spec này **không** cho `Drawer` phủ kín màn? Dựng thì `_thu-vien.scss` mang một khối canh thứ chưa ai vẽ tới — và một khối override không có gì trên màn để đối chiếu là khối sẽ mục ruỗng trong im lặng. Không dựng thì thời lượng thư viện đặt cho chế độ đó nằm ngoài hệ, chờ ngày có người bật chế độ đó mà không biết nó không theo token nào | `frontend-expert` khi dựng `Drawer`, dựa trên việc lớp bọc có phơi chế độ đó ra hay khoá hẳn. Khoá hẳn thì câu hỏi này tự đóng |
| 4 | **Đã chốt 2026-10-08 (người dùng):** vạch ở đầu drawer neo đáy chỉ là nét trang trí, không kéo được — §Responsive, dòng < `$bp-sm` | Đã trả lời |
| 5 | **Đã chốt 2026-10-08 (người dùng):** hình thức vạch trang trí ghi đúng bộ code đang vẽ — §Kích thước, "Vạch trang trí của drawer neo đáy", và §Token dùng | Đã trả lời |
