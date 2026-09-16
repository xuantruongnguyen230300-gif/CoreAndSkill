---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Tabs

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, ngữ nghĩa `tablist` / `tab` / `tabpanel` cộng với bàn phím theo chuẩn ARIA (mũi tên trái/phải, `Home`, `End`, roving tabindex, liên kết hai chiều giữa tab và panel) là một khối khá dài và rất nhiều chỗ sai lặng lẽ. Tự dựng thì phần nhìn xong trong một buổi, còn phần bàn phím thì hỏng âm thầm cho tới lần audit đầu tiên.

---

## Mục đích

Chuyển giữa các phần nội dung khác nhau của cùng một trang, mỗi lần hiện đúng một phần.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Một bản ghi có nhiều mặt: Thông tin chung, Lịch sử, Tệp đính kèm; nhóm nội dung trong một [`Card.md`](./Card.md) hoặc [`Dialog.md`](./Dialog.md) | 🛑 **Lọc hoặc đổi cách trình bày cùng một tập dữ liệu** → [`SegmentedControl.md`](./SegmentedControl.md), xem ranh giới ngay dưới; 🛑 biểu mẫu nhiều bước phải điền theo thứ tự → một luồng có nút Tiếp / Quay lại, xem bẫy ở cuối mục API |
| Nội dung mà người dùng chỉ cần xem **một phần tại một lúc** | 🛑 Điều hướng sang tuyến khác → [`Sidebar.md`](./Sidebar.md); 🛑 nội dung ngắn cần đọc **cùng lúc** → xếp dọc, đừng bắt họ bấm để so sánh |

### Ranh giới với `SegmentedControl`

| | `Tabs` | `SegmentedControl` |
| --- | --- | --- |
| Đổi cái gì | **Panel nội dung** — mỗi tab một khối nội dung khác hẳn | **Cùng một tập dữ liệu**, đổi cách lọc hoặc cách trình bày |
| Ví dụ | Thông tin chung ↔ Lịch sử thay đổi | Tất cả ↔ Đang hoạt động ↔ Đã khoá; xem dạng bảng ↔ dạng thẻ |
| Ngữ nghĩa, bàn phím | `tablist` / `tab` / `tabpanel`; mũi tên di chuyển, `Tab` đi vào panel | Nhóm nút chọn một; `Tab` đi qua từng nút |

Nhầm hai cái này là lỗi hay gặp và nó hỏng ở phần nghe được: nếu "Đang hoạt động / Đã khoá" được dựng bằng `tablist`, trình đọc màn hình thông báo "tab 2 trên 3" cho một thứ thật ra là **bộ lọc**, và người dùng đi tìm ba khối nội dung khác nhau không hề tồn tại.

## Biến thể

| Biến thể | Hình thức tab hiện hành | Dùng khi |
| --- | --- | --- |
| `page` | Gạch dưới dày `--border-w-strong` màu `--color-brand`; cả dải có vạch nền `--color-border-subtle` | **Mặc định.** Tabs cấp trang, ngay dưới [`PageHeader.md`](./PageHeader.md) |
| `section` | Nền `--color-brand-subtle`, chữ `--color-brand-on-subtle`, `--radius-sm`; không có vạch nền | Tabs bên trong một `Card` hoặc một `Dialog` |

## Kích thước

| Cỡ | Chiều cao tab | Đệm ngang | Cỡ chữ | Icon | Dùng khi |
| --- | --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | `--sp-4` | `--fs-xs` | `--icon-sm` | Biến thể `section` trong khối hẹp |
| `md` | `--size-control-md` (34px) | `--sp-5` | `--fs-sm` | `--icon-md` | **Mặc định** |

Nhãn tab **không xuống dòng**. Nhãn dài thì rút chữ; một dải tab cao hai dòng làm hỏng nhịp của cả trang mà vẫn không giúp đọc dễ hơn.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Tab chưa chọn: chữ `--color-text-muted`, `--fw-medium`. Tab hiện hành: theo biến thể ở bảng trên, `--fw-semibold` | Có |
| `hover` | Tab chưa chọn: chữ `--color-text`, nền `--color-surface-2`. Bọc trong `@media (hover: hover)` | Có |
| `focus-visible` | `outline` `--border-w-strong` màu `--color-focus`, `outline-offset` theo [`../DESIGN.md`](../DESIGN.md) §2.6. Panel cũng nhận vòng focus khi người dùng `Tab` vào nó | Có |
| `active` | Nền đậm thêm một bậc trong lúc nhấn; không dịch chuyển vị trí — dải tab dịch chuyển làm cả panel bên dưới trông như rung | Có |
| `disabled` | Tab riêng lẻ mang `aria-disabled="true"`, chữ `--color-text-disabled`, **vẫn nằm trong dãy phím mũi tên** để người dùng biết nó tồn tại và vì sao khoá. Nếu tab đó vĩnh viễn không dùng được thì bỏ hẳn khỏi danh sách, đừng để nó xám mãi | Có |
| `loading` | Dải tab **giữ nguyên và vẫn bấm được**; chỉ panel hiện [`SkeletonLoader.md`](./SkeletonLoader.md) do màn đặt vào slot panel, và panel mang `aria-busy="true"`. Khoá cả dải tab khi một panel đang tải là giam người dùng trong tab họ vừa rời đi | Có |
| `error` | Tải panel hỏng → màn đặt [`NoticeBanner.md`](./NoticeBanner.md) **vào slot panel** kèm nút thử lại. Dải tab không tự tô đỏ; nếu một tab thật sự chứa lỗi cần chú ý thì đặt số lỗi vào `TabItem.badge`, không đổi màu nhãn. Badge trên nhãn là phần cấu trúc của lớp bọc: thư viện vẽ, tạo hình bằng token theo hình thức của [`Badge.md`](./Badge.md) — [`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5 | Có |
| `empty` | Panel không có nội dung → màn đặt [`EmptyState.md`](./EmptyState.md) vào slot panel. Danh sách chỉ có **một** tab → không render dải tab, chỉ render nội dung; một tab đơn độc là một cái nhãn không bấm được | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-border-subtle`, `--color-focus` |
| Chữ, khoảng cách | `--fs-xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--lh-snug`, `--sp-2`, `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng, kích thước | `--radius-sm`, `--border-w`, `--border-w-strong`, `--size-control-sm`, `--size-control-md`, `--icon-sm`, `--icon-md` |
| Chuyển động | `--dur-base`, `--ease-standard` |

⚠️ Biến thể `section` dùng `--color-brand-subtle`, chỉ chênh 1.25:1 với bề mặt ([`../DESIGN.md`](../DESIGN.md) §2.4) — kênh thứ hai bắt buộc là chữ `--color-brand-on-subtle` cộng nét `--fw-semibold`. Biến thể `page` đã có kênh thứ hai sẵn là gạch dưới `--color-brand`, đạt 6.85:1 sáng / 6.57:1 tối.

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Dải tab xếp ngang, khe `--sp-2`; chiều rộng theo nội dung nhãn |
| < `$bp-md` | Dải tab **cuộn ngang** trong chính nó, không xuống dòng; tab hiện hành tự cuộn vào tầm nhìn khi đổi, và cuộn dải không kéo theo cả trang. Dưới `$bp-xs` nhãn rút gọn nhưng **không** thay bằng icon trần — một dải toàn icon buộc người dùng đoán nội dung từng panel |

🛑 **Không tự chuyển `Tabs` thành một ô chọn thả xuống ở màn nhỏ.** Nghe hợp lý và nó phá ngữ nghĩa: `tabpanel` mất phần tử điều khiển tương ứng, và người dùng trình đọc màn hình gặp hai giao diện khác nhau tuỳ bề rộng cửa sổ. Cuộn ngang giữ nguyên cấu trúc.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Dải tab là một phần tử mang `role="tablist"`; mỗi tab là `<button role="tab">`; mỗi panel là một phần tử mang `role="tabpanel"` |
| Vai trò ARIA | Mỗi tab có `aria-controls` trỏ tới `id` của panel; mỗi panel có `aria-labelledby` trỏ ngược lại `id` của tab — thiếu một chiều là trình đọc màn hình mất ngữ cảnh |
| Chọn, roving tabindex | Tab hiện hành mang `aria-selected="true"` và `tabindex="0"`; các tab khác `false` và `tabindex="-1"`. Nhờ vậy một lần `Tab` đi qua cả dải, không phải bấm `Tab` năm lần để vượt năm tab |
| Bàn phím | Mũi tên trái/phải di chuyển giữa các tab và **vòng lại** ở hai đầu; `Home` về tab đầu; `End` tới tab cuối; `Tab` từ dải tab nhảy **vào panel**. Kích hoạt **thủ công**: mũi tên chỉ dời focus, `Enter`/`Space` mới mở tab — `lazy` mặc định dựng panel lúc mở, nên lướt phím qua bốn tab không được bắn bốn request |
| Focus | Panel mang `tabindex="0"` để nhận được focus khi `Tab` vào, kể cả khi bên trong không có control nào |
| Nhãn | `aria-label` trên `tablist` nói nó nhóm cái gì ("Chi tiết người dùng"); icon trên tab mang `aria-hidden="true"` vì nhãn chữ đã mang hết thông tin — [`../Icons.md`](../Icons.md) §7 |
| Chuyển tab, chữ | Chỉ chuyển màu và hiện panel, **không** trượt panel ngang: [`../DESIGN.md`](../DESIGN.md) §7 chỉ cho animate `transform` và `opacity`, một lần đổi `opacity` trong `--dur-base` là đủ. Chữ đi qua tầng i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` · `size` | input | `'page' \| 'section'` · `'sm' \| 'md'` | `'page'` · `'md'` | Chỉ hai biến thể: hai kiểu tab đã phủ hết hai bối cảnh có thật, kiểu thứ ba sẽ chỉ khác về thẩm mỹ ([`../COMPONENTS.md`](../COMPONENTS.md) §2.3) |
| `tabs` | input | `ReadonlyArray<TabItem>` | `[]` | Chữ ký ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 |
| `activeKey` · `ariaLabel` | input | `string \| null` · `string` | `null` · — | `activeKey` là khoá tab hiện hành, trang cha giữ nguồn sự thật; `ariaLabel` bắt buộc, là nhãn của `tablist` |
| `lazy` · `keepAlive` | input | `boolean` | `true` | `lazy` chỉ dựng panel khi tab được mở lần đầu; `keepAlive` giữ panel đã dựng trong DOM khi đổi tab — xem bẫy dưới |
| `activeKeyChanged` | output | `string` | — | Khoá tab người dùng **yêu cầu**. Không phát khi bấm lại tab đang mở |

Nội dung từng panel vào qua khe nội dung, không qua dữ liệu — một panel có thể chứa bất cứ thứ gì và một input chuỗi sẽ chặn điều đó. Khung đang tải, khối lỗi, khối trống của panel cũng do màn đặt vào khe đó — `Tabs` ở tầng bọc nên không import component tự dựng ([`../COMPONENTS.md`](../COMPONENTS.md) §4 luật 5). `Tabs` là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5): nó không gọi API để tải nội dung tab, không đọc route.

**Tab đang mở phản ánh lên URL bằng tham số truy vấn `tab=<key>`** — không phải đoạn đường dẫn con. `tab` là tên tham số dùng chung ở [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.8: `ListStateStore` **bỏ qua** nó (không coi là bộ lọc); trang cha đọc và ghi `tab` qua `queryParams` trực tiếp, có store hay không. Không có nó, người dùng ở tab "Lịch sử" bấm F5 sẽ quay về tab đầu, và một liên kết gửi cho đồng nghiệp luôn mở ra tab đầu chứ không mở đúng chỗ đang bàn. Cái giá: mỗi lần đổi tab sinh một mục trong lịch sử trình duyệt, và nút Back của trình duyệt trở thành nút "tab trước" — thường là đúng ý người dùng, nhưng cần thay thế mục lịch sử thay vì đẩy thêm nếu người dùng bấm qua lại nhiều lần. Việc đọc và ghi URL thuộc về trang cha; `Tabs` chỉ phát `activeKeyChanged`.

🛑 **Bẫy: nhồi một biểu mẫu nhiều bước vào `Tabs`.** Tabs không hứa thứ tự, không hứa lưu, và nếu `keepAlive` bị tắt thì panel bị huỷ khi đổi tab — người dùng điền nửa chừng, sang tab khác kiểm một con số, quay lại và mất sạch. Ngay cả khi `keepAlive` bật, việc kiểm tra hợp lệ vẫn rải ra nhiều panel và người dùng bấm Lưu ở tab 3 không thấy lỗi đang nằm ở tab 1. Biểu mẫu nhiều bước cần một luồng có nút Tiếp / Quay lại, có trạng thái từng bước, và có một chỗ tổng hợp lỗi.

## Do / Don't

- ✅ Dùng `tablist` / `tab` / `tabpanel` thật, đủ cả `aria-controls` và `aria-labelledby`.
- ✅ Roving tabindex — một lần `Tab` đi qua cả dải.
- ✅ Phản ánh tab đang mở lên URL bằng `tab=<key>`.
- ✅ Cuộn ngang dải tab ở màn nhỏ, và giữ dải tab bấm được trong lúc panel đang tải.
- ❌ Không dùng `Tabs` để lọc cùng một tập dữ liệu.
- ❌ Không nhồi biểu mẫu nhiều bước vào `Tabs`.
- ❌ Không đổi `Tabs` thành ô chọn thả xuống ở màn nhỏ, không trượt panel ngang khi chuyển tab.
- ❌ Không render dải tab khi chỉ có một tab.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | `keepAlive` mặc định `true` có gây tốn bộ nhớ với panel chứa bảng lớn không? Chưa có số đo; đảo mặc định sang `false` thì lại mở đúng cái bẫy mất dữ liệu ghi ở trên | F3 — khi dựng màn đầu tiên dùng `Tabs` |
