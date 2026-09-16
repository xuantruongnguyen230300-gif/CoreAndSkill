---
kind: luat
scope: core
verified: chua-doi-chieu
---

# COMPONENTS.md — mục lục component Core

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`. Mọi dòng dưới đây mang trạng thái `📐 spec xong, chưa dựng` — định nghĩa các trạng thái ở [`CLAUDE.md`](./CLAUDE.md) §4.

> **File này là cổng.** Một screen spec chỉ được ghép những component có tên trong bảng §3. Thêm một file vào `Components/` mà không thêm dòng ở đây thì component đó **chưa tồn tại** với phần còn lại của khu Design.
>
> Giá trị token nhắc tới ở các spec sống ở [`DESIGN.md`](./DESIGN.md). File này không lặp lại một mã màu nào.

---

## 1. Luật mà mục lục này ép

### Một component, một định nghĩa

Không màn hình nào, không trang nào, không stylesheet cục bộ nào được khai lại một cái nút, một ô nhập, một badge, một khung bảng hay một chân dialog. Màn hình **ghép** những thứ dưới đây.

Đây không phải luật thẩm mỹ, nó là luật khối lượng việc: mỗi dòng trong bảng §3 ánh xạ sang **đúng một thứ phải dựng**. Hai định nghĩa nút bấm nghĩa là hai chỗ phải sửa mỗi lần đổi token, và một trong hai sẽ bị quên.

### Mở rộng thay vì đẻ mới

Cần một biến thể chưa có → thêm dòng vào spec đang có **và** vào lớp dùng chung. Không tạo một class song song.

Ở dự án tiền nhiệm, một class từng gánh **hai** component khác hẳn nhau — nút chữ ở màn này, nút chỉ icon ở màn kia — và phải tách ra sau khi đã lan khắp nơi. Cùng lúc đó có hai primitive chip cùng cỡ nhưng khác bo góc, khác đệm, khác hệ màu. Cả hai đều bắt đầu từ một lần "thêm nhanh một class cho riêng màn này".

### Chỉ component Core

`Components/` **không chứa component nghiệp vụ**. Phép thử một dòng ở [`CLAUDE.md`](./CLAUDE.md) §1: xoá phần nghiệp vụ khỏi spec mà spec vẫn còn nghĩa thì nó là Core.

Component nghiệp vụ sống ở tầng `modules/` của dự án dùng Core và được mô tả trong screen spec của dự án đó, không ở đây.

---

## 2. Nguyên tắc chung cho mọi component

### 2.1 Trạng thái bắt buộc

Mỗi spec phải khai **đủ tám** trạng thái. Cái nào không áp dụng thì viết `không áp dụng` **kèm lý do** — không bỏ trống dòng.

| Trạng thái | Ý nghĩa | Bẫy hay gặp |
| --- | --- | --- |
| `default` | Trạng thái nghỉ | — |
| `hover` | Con trỏ ở trên | Chỉ dùng cho thiết bị có chuột — bọc trong `@media (hover: hover)`, nếu không màn cảm ứng sẽ dính trạng thái hover sau khi chạm |
| `focus-visible` | Nhận focus bàn phím | Dùng `:focus-visible`, **không** dùng `:focus`. `:focus` làm chuột bấm cũng hiện vòng, và người ta sẽ "sửa" bằng `outline: none` |
| `active` | Đang bị nhấn | Phải phân biệt được với `hover`, nếu không nút không có phản hồi khi bấm |
| `disabled` | Không dùng được | Màu nhạt **không đủ** — phải có `disabled` hoặc `aria-disabled` để trình đọc màn hình biết |
| `loading` | Đang chờ kết quả | Phải khoá luôn thao tác, nếu không người dùng bấm hai lần. Phải có `aria-busy` |
| `error` | Dữ liệu hoặc thao tác sai | Viền đỏ **không đủ** — bắt buộc kèm chữ. Xem [`DESIGN.md`](./DESIGN.md) §2.7 |
| `empty` | Không có gì để hiển thị | Trống là một trạng thái phải thiết kế, không phải chỗ để trống |

**Bỏ trống một dòng nguy hiểm hơn viết sai nó.** Dòng sai thì có người cãi; dòng trống thì mỗi người dựng tự bịa một kiểu.

### 2.2 Kích thước

Ba cỡ control. Component nào có nhiều cỡ thì **phải dùng đúng ba cỡ này**, không đẻ cỡ thứ tư.

| Cỡ | Token chiều cao | Dùng khi |
| --- | --- | --- |
| `sm` | `--size-control-sm` | Trong hàng bảng, trong chip, chỗ mật độ cao |
| `md` | `--size-control-md` | **Mặc định.** Form, toolbar, dialog |
| `lg` | `--size-control-lg` | Nút submit chính, ô nhập trên màn đăng nhập |

> 📖 Giá trị ba token: đọc [`DESIGN.md`](./DESIGN.md) §6.2. File này không giữ bản sao — một con số chép ra đây sẽ không được sửa cùng lúc với bản gốc ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5).

Vùng bấm nhỏ nhất là **24×24px** (WCAG 2.2 SC 2.5.8 — ngưỡng của chuẩn ngoài, không phải token). Cỡ `sm` vượt ngưỡng đó, nhưng với component chỉ có icon thì vùng bấm phải nới bằng `padding` chứ không bằng `margin` — `margin` không nhận sự kiện chuột.

### 2.3 Biến thể

Đặt tên biến thể theo **vai**, không theo hình thức: `primary` / `secondary` / `ghost` / `danger`, không phải `blue` / `grey` / `red`.

Số biến thể của một component **phải nhỏ**. Nếu một component có hơn năm biến thể, gần như chắc chắn nó đang gánh hai vai và cần tách.

### 2.4 Chống lồng nhau vô hạn

Một component chỉ được lồng component **cùng cấp hoặc thấp hơn** trong danh sách sau. Ràng buộc này ngăn `Dialog` nằm trong `Card` nằm trong `Dialog`:

```text
Tầng 0 — hạt: Badge · Avatar · SkeletonLoader · Check · ProgressBar
Tầng 1 — control: Button · IconButton · Input · DatePicker · SegmentedControl · Pagination · FilterChip · LanguageSwitcher
Tầng 2 — cụm: FormRow · AuthField · Card · Table · EmptyState · NoticeBanner · Tabs · FileUpload · FilterPanel
                TreeSelect · Autocomplete · Timeline · Chart
Tầng 3 — vùng: Toolbar · DataTable · EditableGrid · PageHeader · AuthCard · Stepper
Tầng 4 — khung: Sidebar · Topbar · Footer
Tầng 5 — nổi: Dialog · ConfirmDialog · Toast · Menu · Tooltip · Drawer
```

**Tầng 5 là ngoại lệ của chính luật này, và ngoại lệ có lý do.** Component nổi được dựng trong một cổng (portal) ở gốc tài liệu, không nằm trong cây DOM của thứ đã gọi nó. Vì vậy một `Input` ở tầng 1 được phép **mở** một `Menu` ở tầng 5 mà không vi phạm gì — nó không lồng `Menu` vào trong mình, nó chỉ yêu cầu dựng một lớp nổi.

Luật chống lồng nhau vẫn giữ nguyên hiệu lực ở chỗ nó sinh ra để canh: **một lớp nổi không được mở một lớp nổi cùng loại**. Không `Dialog` trong `Dialog`, không `Drawer` chồng `Drawer`. Cần đi sâu thêm một cấp thì đó là một màn riêng.

---

## 3. Mục lục

| Component | Là gì | Spec | Nền | Trạng thái |
| --- | --- | --- | --- | --- |
| `Button` | Nút có nhãn chữ, bốn vai, ba cỡ | [Components/Button.md](./Components/Button.md) | tự dựng | 📐 spec xong, chưa dựng |
| `IconButton` | Nút chỉ có icon, luôn kèm nhãn cho trình đọc màn hình | [Components/IconButton.md](./Components/IconButton.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Input` | Hợp đồng ô nhập dùng chung cho text, number, select, textarea | [Components/Input.md](./Components/Input.md) | tự dựng | 📐 spec xong, chưa dựng |
| `DatePicker` | Ô chọn một ngày hoặc một khoảng ngày, lịch thả xuống, luôn `dd/mm/yyyy` | [Components/DatePicker.md](./Components/DatePicker.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `FormRow` | Cụm nhãn + ô nhập + gợi ý + lỗi; nơi duy nhất quyết định lỗi hiện ở đâu | [Components/FormRow.md](./Components/FormRow.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Check` | Ô đánh dấu và nút chọn một trong nhiều, gồm trạng thái nửa chọn | [Components/Check.md](./Components/Check.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `SegmentedControl` | Chuyển đổi giữa vài chế độ xem loại trừ nhau | [Components/SegmentedControl.md](./Components/SegmentedControl.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Card` | Bề mặt gom một khối nội dung, có tiêu đề và chân tuỳ chọn | [Components/Card.md](./Components/Card.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Badge` | Nhãn nhỏ mang trạng thái hoặc mang định danh | [Components/Badge.md](./Components/Badge.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Avatar` | Ảnh đại diện, rơi về chữ cái đầu khi không có ảnh | [Components/Avatar.md](./Components/Avatar.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Table` | Bảng tĩnh — chỉ trình bày, không phân trang, không sắp xếp | [Components/Table.md](./Components/Table.md) | tự dựng | 📐 spec xong, chưa dựng |
| `DataTable` | Lưới dữ liệu: phân trang phía máy chủ, sắp xếp, chọn dòng, cột ghim | [Components/DataTable.md](./Components/DataTable.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `Dialog` | Hộp thoại chặn, bẫy focus, ba cỡ bề rộng | [Components/Dialog.md](./Components/Dialog.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `ConfirmDialog` | Hỏi xác nhận — đúng hai nút, có mức độ nguy hiểm | [Components/ConfirmDialog.md](./Components/ConfirmDialog.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Toast` | Thông báo nổi tạm thời, tự biến mất | [Components/Toast.md](./Components/Toast.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `NoticeBanner` | Thông báo nằm trong trang, ở lại cho tới khi bối cảnh đổi | [Components/NoticeBanner.md](./Components/NoticeBanner.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Sidebar` | Dải điều hướng của khung ứng dụng: cây menu, thu gọn, drawer | [Components/Sidebar.md](./Components/Sidebar.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Topbar` | Thanh đầu trang dính: hamburger, tiêu đề tuyến, khu người dùng | [Components/Topbar.md](./Components/Topbar.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Toolbar` | Dải điều khiển trên một danh sách: tìm kiếm, bộ lọc, nhóm hành động | [Components/Toolbar.md](./Components/Toolbar.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Footer` | Dòng chân trang: phiên bản, bản quyền, liên kết phụ | [Components/Footer.md](./Components/Footer.md) | tự dựng | 📐 spec xong, chưa dựng |
| `AuthCard` | Khung thứ hai — màn xác thực chạy **không** có sidebar và topbar | [Components/AuthCard.md](./Components/AuthCard.md) | tự dựng | 📐 spec xong, chưa dựng |
| `AuthField` | Ô nhập của màn xác thực: có icon dẫn, có nút hiện/ẩn mật khẩu | [Components/AuthField.md](./Components/AuthField.md) | tự dựng | 📐 spec xong, chưa dựng |
| `LanguageSwitcher` | Đổi ngôn ngữ lúc chạy | [Components/LanguageSwitcher.md](./Components/LanguageSwitcher.md) | tự dựng | 📐 spec xong, chưa dựng |
| `EmptyState` | Màn trống có nghĩa: icon, câu giải thích, một hành động | [Components/EmptyState.md](./Components/EmptyState.md) | tự dựng | 📐 spec xong, chưa dựng |
| `SkeletonLoader` | Khối xám giữ chỗ trong lúc chờ dữ liệu | [Components/SkeletonLoader.md](./Components/SkeletonLoader.md) | tự dựng | 📐 spec xong, chưa dựng |
| `FileUpload` | Chọn và tải tệp lên: kéo thả, kiểm loại và dung lượng, tiến trình | [Components/FileUpload.md](./Components/FileUpload.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `PageHeader` | Đầu một trang: đường dẫn phân cấp, tiêu đề, mô tả, hành động chính | [Components/PageHeader.md](./Components/PageHeader.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Pagination` | Điều hướng trang và chọn số dòng mỗi trang | [Components/Pagination.md](./Components/Pagination.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `Tabs` | Chuyển giữa các phần nội dung trong cùng một trang | [Components/Tabs.md](./Components/Tabs.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `Menu` | Danh sách hành động ngắn trong một lớp nổi neo vào nút đã mở nó | [Components/Menu.md](./Components/Menu.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `Tooltip` | Một dòng chú ngắn, chỉ hiện khi người dùng tỏ ý muốn biết thêm | [Components/Tooltip.md](./Components/Tooltip.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `Drawer` | Tấm trượt từ cạnh màn hình — xem hoặc sửa nhanh mà danh sách phía sau vẫn còn | [Components/Drawer.md](./Components/Drawer.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `ProgressBar` | Một việc đã đi được bao xa, hoặc một giá trị so với một mốc | [Components/ProgressBar.md](./Components/ProgressBar.md) | tự dựng | 📐 spec xong, chưa dựng |
| `FilterChip` | Một điều kiện lọc đang bật, gỡ được bằng một thao tác | [Components/FilterChip.md](./Components/FilterChip.md) | tự dựng | 📐 spec xong, chưa dựng |
| `TreeSelect` | Chọn một nút trong cấu trúc phân cấp, khi danh sách phẳng không diễn tả được | [Components/TreeSelect.md](./Components/TreeSelect.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `Autocomplete` | Chọn từ danh mục lớn bằng cách gõ vài ký tự, một hoặc nhiều giá trị | [Components/Autocomplete.md](./Components/Autocomplete.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `EditableGrid` | Nhập và sửa nhiều dòng ngay trên lưới, không mở hộp thoại từng dòng | [Components/EditableGrid.md](./Components/EditableGrid.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `Stepper` | Chia một việc dài thành các bước có thứ tự mà **người dùng** điều khiển | [Components/Stepper.md](./Components/Stepper.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Timeline` | Chuỗi sự việc theo thời gian: nhật ký đã xảy ra, hoặc luồng duyệt còn bước phía trước | [Components/Timeline.md](./Components/Timeline.md) | tự dựng | 📐 spec xong, chưa dựng |
| `Chart` | Vẽ số liệu thành hình để so sánh nhanh hơn đọc bảng | [Components/Chart.md](./Components/Chart.md) | bọc PrimeNG | 📐 spec xong, chưa dựng |
| `FilterPanel` | Nơi nhập nhiều điều kiện lọc cùng lúc rồi áp một lần | [Components/FilterPanel.md](./Components/FilterPanel.md) | tự dựng | 📐 spec xong, chưa dựng |

**Cột "Nền" nhận đúng một trong hai giá trị: `bọc PrimeNG` · `tự dựng`.** FE đọc cột này bằng máy để xếp component vào thư mục — quy tắc ánh xạ ở [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md). Component dựng **trên** một component bọc khác mà không tự import thư viện (như `ConfirmDialog` trên `Dialog`) mang `tự dựng`. Component bọc mà chỉ **một phần** biến thể cần thư viện (như `Chart`: chỉ `line`) vẫn mang `bọc PrimeNG` — các biến thể còn lại vẽ bên trong cùng lớp bọc.

Đừng chép số lượng vào tài liệu ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6). Đếm bằng lệnh:

```bash
ls docs/Design/Components/*.md | wc -l
grep -cE '^\| `[A-Za-z]+` \|.*\]\(\./Components/' docs/Design/COMPONENTS.md
```

PASS = hai số bằng nhau.

> Lệnh thứ hai **cố ý neo vào hình dạng của một dòng bảng có link spec**, không neo vào chuỗi trạng thái. Đếm theo chuỗi trạng thái sẽ đếm luôn dòng nhãn fidelity ở đầu file và đếm luôn chính khối lệnh này — một phép kiểm luôn lệch là một phép kiểm không ai chạy lần thứ hai. Cơ chế hỏng ghi ở [`../audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md`](../audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md).

---

## 4. Ranh giới bọc PrimeNG ↔ tự dựng

Quyết định này đổi hẳn khối lượng việc của một component, nên nó nằm ngay trong mục lục chứ không giấu trong từng spec.

### Quy tắc

**Bọc PrimeNG khi hành vi khó và đã được giải đúng ở thư viện; tự dựng khi component chỉ là trình bày.**

"Khó" ở đây có nghĩa cụ thể, không phải cảm giác:

| Loại hành vi | Vì sao khó | Component thuộc nhóm này |
| --- | --- | --- |
| Bẫy focus, khôi phục focus, khoá cuộn nền | Đúng được toàn bộ ca biên (Tab vòng, Escape, phần tử động) là hàng trăm dòng và rất nhiều lần sai | `Dialog`, `Drawer` |
| Định vị lớp nổi khi cuộn/tràn viewport | Phải xử lý container cuộn, `position: fixed` lồng nhau, trở về khi hết chỗ | `Toast`, `Menu`, `Tooltip`, `TreeSelect`, `Autocomplete` |
| Ảo hoá dòng, cột ghim, sắp xếp, phân trang máy chủ | Vừa nhiều ca biên vừa nhạy hiệu năng | `DataTable`, `EditableGrid`, `Pagination` |
| Di chuyển ô bằng bàn phím, mở và đóng trình sửa tại chỗ | Bàn phím của một lưới nhập liệu là lý do nó tồn tại; sai một phím là người dùng quay về dùng hộp thoại | `EditableGrid` |
| Bàn phím theo chuẩn ARIA cho một cây có nhánh mở/đóng | `→` `←` vừa đi ngang vừa mở/đóng nhánh, kèm `aria-level` / `aria-setsize` cho từng dòng | `TreeSelect` |
| Kéo thả tệp, đọc tệp, tiến trình tải lên | Nhiều API trình duyệt, nhiều ca biên bảo mật | `FileUpload` |
| Ngữ nghĩa control biểu mẫu gốc + trạng thái nửa chọn | `indeterminate` không có trong HTML thuần, phải đặt bằng script | `Check` |
| Ngữ nghĩa `tablist`/`tab`/`tabpanel` + phím mũi tên | Bàn phím theo chuẩn ARIA khá dài | `Tabs` |
| Thang đo, nội suy đường, bắt con trỏ gần nhất trên nhiều chuỗi | Nhiều ca biên hình học, và sai thì số hiện trong hộp giá trị không khớp điểm đang trỏ | `Chart` (dạng `line`) |

Mọi thứ còn lại **tự dựng**. Một `Button` bọc thư viện chỉ đổi một lớp trung gian lấy một lớp trung gian khác, mà lại nhận thêm CSS mặc định phải đè.

### Luật khi bọc

1. **Chỉ tầng dùng chung được import PrimeNG.** Component nghiệp vụ không bao giờ import trực tiếp — ép bằng cổng FE, xem [`../RULES.md`](../RULES.md) §7 F5.
2. **Bọc nghĩa là giấu hẳn.** API của component bọc **không** để lọt kiểu dữ liệu, tên sự kiện hay tên slot của PrimeNG ra ngoài. Lọt ra là mất luôn cái lợi duy nhất của việc bọc: đổi thư viện mà không phải sửa màn hình.
3. **Không đè style bằng `::ng-deep`.** Tạo hình bằng token và bằng cơ chế theme của thư viện. `::ng-deep` không bị đóng gói theo component, nên nó rò ra toàn ứng dụng và hỏng ở lần nâng cấp kế tiếp.
4. **Trạng thái vẫn phải khai đủ tám.** Bọc thư viện không miễn trừ §2.1 — vẫn phải viết ra `disabled` trông thế nào, `loading` trông thế nào.
5. **Lớp bọc không import component tự dựng.** Chiều import giữa hai tầng là luật thi công, khai ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §2.2. Thứ lớp bọc hiện ra chia làm hai loại, và hai loại đi hai đường khác nhau:
   - **Phần cấu trúc của chính lớp bọc do thư viện vẽ, tạo hình bằng token qua preset theme của thư viện.** Đó là những control luôn giống nhau ở mọi nơi dùng: nút đóng của `Dialog` và `Toast`, nút hành động của `Toast`, chip trong ô chọn nhiều của `Autocomplete` và `TreeSelect`, nút chọn tệp, nút theo dòng và thanh tiến trình của `FileUpload`, badge đếm trên nhãn `Tabs`, nút sửa dòng của `EditableGrid`, nút "Đóng hết" của `DataTable`. Spec gọi tên một component tự dựng ở những chỗ này (`IconButton`, `Button`, `FilterChip`, `Badge`, `ProgressBar`) là để nói phần đó **mượn token và hình thức** của component nào — không phải để import nó. Đây **không** phải định nghĩa hình thức thứ hai theo §1: giá trị vẫn sống ở [`DESIGN.md`](./DESIGN.md), hình thức vẫn sống ở spec được gọi tên; preset chỉ là nơi áp.
   - **Nội dung do màn quyết vào qua template.** Khối trống, khung đang tải, khối lỗi, và nút hành động nghiệp vụ nằm trong các khối đó vào qua input `TemplateRef` đặt tên `<vai>Template`: `emptyTemplate`, `loadingTemplate`, `errorTemplate`. Lớp bọc quyết **khi nào** hiện, màn quyết **hiện gì**; nút trong template gọi thẳng hàm của màn, nên lớp bọc không mở output riêng cho nút đó. Khuôn gốc ở [`Components/DataTable.md`](./Components/DataTable.md) §API.
   - Phép thử một câu: *hai màn khác nhau có cần hiện thứ khác nhau ở chỗ này không?* Có → template. Không, chỗ đó luôn là cùng một control của lớp bọc → phần cấu trúc; nó báo ra ngoài bằng `output()` của lớp bọc.
   - Lớp bọc có input `state` thì hiện template theo `state`. Lớp bọc chỉ có `loading` thì `loadingTemplate` hiện khi `loading` là `true`, còn `errorTemplate` hiện khi màn truyền nó khác `null` — màn chỉ truyền lúc đang lỗi — và thắng mọi slot khác.
   - Vùng đã là slot nội dung của màn (thân `Dialog`, thân `Drawer`, panel `Tabs`) thì màn đặt component tự dựng thẳng vào slot đó, không cần template.

---

## 5. Dumb và smart — ranh giới

| | **Dumb** (trình bày) | **Smart** (kết nối) |
| --- | --- | --- |
| Ở đâu | `shared/ui/` hoặc `shared/components/`, theo cột "Nền" ở §3 | `platform/`, `modules/<x>/pages/` |
| Nhận dữ liệu | Qua `input()` | Tự gọi service |
| Báo ra ngoài | Qua `output()` | Gọi service, điều hướng |
| Được inject service? | 🛑 Không service lấy dữ liệu | ✅ Có |
| Biết về HTTP, route, store? | 🛑 Không | ✅ Có |
| Test bằng | Dựng với input thuần, không cần mock gì | Cần mock service |

**Mọi component trong bảng §3 là dumb.** Không có ngoại lệ.

Cổng chỉ phủ **một nửa** luật này, và nửa kia phải nói ra: [`../RULES.md`](../RULES.md) §7 F11 quét `shared/components/`, nên component `tự dựng` được ép bằng lệnh. Component `bọc PrimeNG` nằm ở `shared/ui/` — F11 không chạm tới, và luật dumb ở đó giữ bằng review.

Ba ca dễ nhầm, giải sẵn:

| Ca | Nhìn có vẻ smart | Cách giữ dumb |
| --- | --- | --- |
| `Sidebar` / `Topbar` hiển thị menu và người đang đăng nhập | Chúng cần dữ liệu phiên và menu | Nhận cả hai qua `input()`. `platform/shell` là tầng smart inject phiên và menu rồi truyền xuống |
| `Toast` được gọi từ bất cứ đâu | Nó cần một hàng đợi toàn cục | Tách đôi: một service giữ hàng đợi (smart, ở `core/`), một component chỉ vẽ danh sách nhận qua `input()` (dumb) |
| `DataTable` phân trang phía máy chủ | Nó phải gọi API mỗi lần đổi trang | Nó **phát** sự kiện đổi trang qua `output()`. Trang cha gọi API và truyền dữ liệu mới xuống |

**Vì sao giữ ranh giới này:** một component dumb dựng lên được trong test chỉ với vài input. Một component smart cần dựng cả tầng HTTP để hiện được một cái nút. Khi tất cả đều smart, test trở nên đắt tới mức không ai viết nữa.

---

## 6. Quy tắc đặt tên

| Thứ | Khuôn | Ví dụ |
| --- | --- | --- |
| Tên component | `PascalCase`, danh từ hoặc cụm danh từ | `NoticeBanner`, `FormRow` |
| File spec | `Components/<Tên>.md`, trùng khít tên component | `Components/NoticeBanner.md` |
| Class CSS gốc | `kebab-case`, trùng tên component | `.notice-banner` |
| Class biến thể | `.<gốc>--<biến thể>` | `.btn--danger` |
| Class trạng thái | `.is-<trạng thái>` | `.is-loading` |
| Class phần con | `.<gốc>__<phần>` | `.card__header` |
| Token riêng component | `--<gốc>-<thuộc tính>` | `--btn-height` |

Bốn điều cấm:

- 🛑 **Không tiền tố công ty/dự án** (`csk-`, `core-`) trong tên class. Core đã là gốc của mọi thứ; tiền tố chỉ thêm nhiễu.
- 🛑 **Không đặt tên theo màu hay vị trí**: không `.btn-blue`, không `.card-left`. Ngày đổi màu hoặc đổi bố cục là ngày tên nói dối.
- 🛑 **Không đặt tên theo màn hình**: không `.user-list-toolbar`. Đó là dấu hiệu component đang bị chặt vào một màn.
- 🛑 **Không viết tắt** trừ khi viết tắt phổ biến hơn từ đầy đủ (`id`, `url`, `api`). `btn` là ngoại lệ đã chốt vì nó phổ biến tới mức không gây mơ hồ.

---

## 7. Danh sách kiểm khi thêm một component mới

- [ ] Đã kiểm rằng không component nào trong §3 mở rộng được để làm việc này.
- [ ] Nó là **Core** — qua phép thử ở [`CLAUDE.md`](./CLAUDE.md) §1.
- [ ] Đã tạo `Components/<Tên>.md` từ [`Templates/Components.md`](./Templates/Components.md), đủ mọi mục bắt buộc ở [`CLAUDE.md`](./CLAUDE.md) §6.
- [ ] Đủ **tám** trạng thái ở §2.1, mục nào không áp dụng thì ghi rõ lý do.
- [ ] Chỉ dùng token từ [`DESIGN.md`](./DESIGN.md), không giá trị thô.
- [ ] Đã khai bọc PrimeNG hay tự dựng, kèm lý do theo §4.
- [ ] Đã khai vai trò ARIA, phím tắt, hành vi focus.
- [ ] Đã thêm một dòng vào bảng §3, trạng thái đúng theo [`CLAUDE.md`](./CLAUDE.md) §4.
- [ ] Đã ghi tầng lồng nhau ở §2.4.
- [ ] Frontmatter đủ ba khoá — cổng [`../RULES.md`](../RULES.md) §1 D1 chặn nếu thiếu.
- [ ] `bash .claude/check-docs.sh` xanh.

---

## 8. Rút một component

Một component bị rút khi nó không còn nơi dùng, hoặc khi nó được gộp vào một component khác. Đây là việc phải làm **có thủ tục**, vì một component biến mất lặng lẽ khiến mọi screen spec trỏ tới nó trở thành trỏ vào hư không.

Bốn bước, không bỏ bước nào:

1. **Kiểm không còn nơi dùng.** Không screen spec nào ghép nó, và khi đã có `src/` thì không màn nào import nó.
2. **Đổi trạng thái dòng ở §3 thành `⛔ đã rút`**, và ghi ngay trong dòng đó **vì sao rút** và **đi đâu**.
3. **Giữ file spec, đổi `kind` thành `lich-su`**, dán banner ở đầu file theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5, và trỏ về component thay thế.
4. **Gỡ mọi link trỏ tới nó.** Cổng [`../RULES.md`](../RULES.md) §1 D10 cấm trích dẫn file mang `kind: lich-su`, nên bước này không phải tuỳ chọn.

**Dòng `⛔ đã rút` được giữ lại, không xoá.** Lý do rút — *"vì màn hình dùng nó đã bỏ"* chứ không phải *"vì nó là component tồi"* — chính là bằng chứng mà người sau cần khi cân nhắc dựng lại nó. Một bảng chỉ hiện trạng thái hiện tại sẽ mất thông tin đó, và ai đó sẽ tranh luận lại từ đầu.

Ở dự án tiền nhiệm, một loạt spec bị rút vì hai màn hình bị gỡ ra để làm lại, rồi được khôi phục nguyên vẹn ít lâu sau. Điều cứu được lần khôi phục đó là **bảng rút vẫn còn** và nó nói rõ lý do rút là lịch trình chứ không phải thiết kế.

**Không rút một component chỉ vì hôm nay chưa có màn nào dùng.** Ở giai đoạn 1 thì **không màn nào** dùng bất cứ thứ gì — đó là chuyện bình thường, không phải lý do rút.

---

## 9. Vì sao không có bảng "vấn đề đã biết" tập trung ở đây

Một vấn đề còn treo của component nào thì ghi trong mục `Cần chốt` **của chính spec component đó**. Mục lục này chỉ **dẫn đường** tới đó; nó không giữ bản sao thứ hai.

Ở dự án tiền nhiệm, nhiều screen spec trỏ tới một mục "Known inconsistencies" ở mục lục — mục đó **chưa từng tồn tại**. Chúng là bản chép lại một đoạn mẫu, không ai kiểm, và người đọc tin rằng có một quyết định đang chờ trong khi nó đã được chốt xong trong chính spec của component.

Đếm được bằng lệnh thì đừng chép ra ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6):

```bash
grep -L 'Cần chốt' docs/Design/Components/*.md
```

PASS = không in ra file nào. Mỗi spec có đúng một mục `Cần chốt`; không còn gì để ngỏ thì ghi `Không còn`.
