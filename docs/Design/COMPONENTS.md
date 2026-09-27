---
kind: luat
scope: core
verified: chua-doi-chieu
---

# COMPONENTS.md — mục lục component Core

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Repo đã có `src/FE`. Một phần component trong mục lục này đã có code thật, phần còn lại chưa — và **mục lục không giữ trạng thái của từng component**. Trạng thái đọc ở nhãn cấp tệp đầu mỗi spec; cách đọc và tiêu chí PASS ở §3.

| Có thật hôm nay | Sẽ thành |
| --- | --- |
| Mỗi spec dưới `Components/` mang một nhãn cấp tệp `📐` hoặc `🚧`. Spec mang `🚧` kèm ngay trong nó một bảng khai phần đã có và phần còn nợ | Spec nào đã mở source ra so trọn vẹn thì mang `✅ ĐÃ ĐỐI CHIẾU` kèm ngày, theo [`CLAUDE.md`](./CLAUDE.md) §2 |
| Cổng [`../../.claude/check-docs.sh`](../../.claude/check-docs.sh) §27 canh **một** chiều: spec mang `📐` mà đã có tệp component cùng tên dạng kebab dưới `src/FE/src/app/` thì cổng đỏ (luật D42, [`../DEBT.md`](../DEBT.md)) | Chiều còn lại — spec mang `🚧` mà chưa có component thật — chưa có cổng nào canh, hôm nay giữ bằng người đọc |

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

> 📖 Vì sao bỏ trống một dòng trạng thái là lỗi nặng hơn viết sai nó: đọc [`CLAUDE.md`](./CLAUDE.md) §6.

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

| Component | Là gì | Spec | Nền |
| --- | --- | --- | --- |
| `Button` | Nút có nhãn chữ, bốn vai, ba cỡ | [Components/Button.md](./Components/Button.md) | tự dựng |
| `IconButton` | Nút chỉ có icon, luôn kèm nhãn cho trình đọc màn hình | [Components/IconButton.md](./Components/IconButton.md) | tự dựng |
| `Input` | Hợp đồng ô nhập dùng chung cho text, number, select, textarea | [Components/Input.md](./Components/Input.md) | tự dựng |
| `DatePicker` | Ô chọn một ngày hoặc một khoảng ngày, lịch thả xuống, luôn `dd/mm/yyyy` | [Components/DatePicker.md](./Components/DatePicker.md) | bọc PrimeNG |
| `FormRow` | Cụm nhãn + ô nhập + gợi ý + lỗi; nơi duy nhất quyết định lỗi hiện ở đâu | [Components/FormRow.md](./Components/FormRow.md) | tự dựng |
| `Check` | Ô đánh dấu và nút chọn một trong nhiều, gồm trạng thái nửa chọn | [Components/Check.md](./Components/Check.md) | bọc PrimeNG |
| `SegmentedControl` | Chuyển đổi giữa vài chế độ xem loại trừ nhau | [Components/SegmentedControl.md](./Components/SegmentedControl.md) | tự dựng |
| `Card` | Bề mặt gom một khối nội dung, có tiêu đề và chân tuỳ chọn | [Components/Card.md](./Components/Card.md) | tự dựng |
| `Badge` | Nhãn nhỏ mang trạng thái hoặc mang định danh | [Components/Badge.md](./Components/Badge.md) | tự dựng |
| `Avatar` | Ảnh đại diện, rơi về chữ cái đầu khi không có ảnh | [Components/Avatar.md](./Components/Avatar.md) | tự dựng |
| `Table` | Bảng tĩnh — chỉ trình bày, không phân trang, không sắp xếp | [Components/Table.md](./Components/Table.md) | tự dựng |
| `DataTable` | Lưới dữ liệu: phân trang phía máy chủ, sắp xếp, chọn dòng, cột ghim | [Components/DataTable.md](./Components/DataTable.md) | bọc PrimeNG |
| `Dialog` | Hộp thoại chặn, bẫy focus, ba cỡ bề rộng | [Components/Dialog.md](./Components/Dialog.md) | bọc PrimeNG |
| `ConfirmDialog` | Hỏi xác nhận — đúng hai nút, có mức độ nguy hiểm | [Components/ConfirmDialog.md](./Components/ConfirmDialog.md) | tự dựng |
| `Toast` | Thông báo nổi tạm thời, tự biến mất | [Components/Toast.md](./Components/Toast.md) | bọc PrimeNG |
| `NoticeBanner` | Thông báo nằm trong trang, ở lại cho tới khi bối cảnh đổi | [Components/NoticeBanner.md](./Components/NoticeBanner.md) | tự dựng |
| `Sidebar` | Dải điều hướng của khung ứng dụng: cây menu, thu gọn, drawer | [Components/Sidebar.md](./Components/Sidebar.md) | tự dựng |
| `Topbar` | Thanh đầu trang dính: hamburger, tiêu đề tuyến, khu người dùng | [Components/Topbar.md](./Components/Topbar.md) | tự dựng |
| `Toolbar` | Dải điều khiển trên một danh sách: tìm kiếm, bộ lọc, nhóm hành động | [Components/Toolbar.md](./Components/Toolbar.md) | tự dựng |
| `Footer` | Dòng chân trang: phiên bản, bản quyền, liên kết phụ | [Components/Footer.md](./Components/Footer.md) | tự dựng |
| `AuthCard` | Khung thứ hai — màn xác thực chạy **không** có sidebar và topbar | [Components/AuthCard.md](./Components/AuthCard.md) | tự dựng |
| `AuthField` | Ô nhập của màn xác thực: có icon dẫn, có nút hiện/ẩn mật khẩu | [Components/AuthField.md](./Components/AuthField.md) | tự dựng |
| `LanguageSwitcher` | Đổi ngôn ngữ lúc chạy | [Components/LanguageSwitcher.md](./Components/LanguageSwitcher.md) | tự dựng |
| `EmptyState` | Màn trống có nghĩa: icon, câu giải thích, một hành động | [Components/EmptyState.md](./Components/EmptyState.md) | tự dựng |
| `SkeletonLoader` | Khối xám giữ chỗ trong lúc chờ dữ liệu | [Components/SkeletonLoader.md](./Components/SkeletonLoader.md) | tự dựng |
| `FileUpload` | Chọn và tải tệp lên: kéo thả, kiểm loại và dung lượng, tiến trình | [Components/FileUpload.md](./Components/FileUpload.md) | bọc PrimeNG |
| `PageHeader` | Đầu một trang: đường dẫn phân cấp, tiêu đề, mô tả, hành động chính | [Components/PageHeader.md](./Components/PageHeader.md) | tự dựng |
| `Pagination` | Điều hướng trang và chọn số dòng mỗi trang | [Components/Pagination.md](./Components/Pagination.md) | bọc PrimeNG |
| `Tabs` | Chuyển giữa các phần nội dung trong cùng một trang | [Components/Tabs.md](./Components/Tabs.md) | bọc PrimeNG |
| `Menu` | Danh sách hành động ngắn trong một lớp nổi neo vào nút đã mở nó | [Components/Menu.md](./Components/Menu.md) | bọc PrimeNG |
| `Tooltip` | Một dòng chú ngắn, chỉ hiện khi người dùng tỏ ý muốn biết thêm | [Components/Tooltip.md](./Components/Tooltip.md) | bọc PrimeNG |
| `Drawer` | Tấm trượt từ cạnh màn hình — xem hoặc sửa nhanh mà danh sách phía sau vẫn còn | [Components/Drawer.md](./Components/Drawer.md) | bọc PrimeNG |
| `ProgressBar` | Một việc đã đi được bao xa, hoặc một giá trị so với một mốc | [Components/ProgressBar.md](./Components/ProgressBar.md) | tự dựng |
| `FilterChip` | Một điều kiện lọc đang bật, gỡ được bằng một thao tác | [Components/FilterChip.md](./Components/FilterChip.md) | tự dựng |
| `TreeSelect` | Chọn một nút trong cấu trúc phân cấp, khi danh sách phẳng không diễn tả được | [Components/TreeSelect.md](./Components/TreeSelect.md) | bọc PrimeNG |
| `Autocomplete` | Chọn từ danh mục lớn bằng cách gõ vài ký tự, một hoặc nhiều giá trị | [Components/Autocomplete.md](./Components/Autocomplete.md) | bọc PrimeNG |
| `EditableGrid` | Nhập và sửa nhiều dòng ngay trên lưới, không mở hộp thoại từng dòng | [Components/EditableGrid.md](./Components/EditableGrid.md) | bọc PrimeNG |
| `Stepper` | Chia một việc dài thành các bước có thứ tự mà **người dùng** điều khiển | [Components/Stepper.md](./Components/Stepper.md) | tự dựng |
| `Timeline` | Chuỗi sự việc theo thời gian: nhật ký đã xảy ra, hoặc luồng duyệt còn bước phía trước | [Components/Timeline.md](./Components/Timeline.md) | tự dựng |
| `Chart` | Vẽ số liệu thành hình để so sánh nhanh hơn đọc bảng | [Components/Chart.md](./Components/Chart.md) | bọc PrimeNG |
| `FilterPanel` | Nơi nhập nhiều điều kiện lọc cùng lúc rồi áp một lần | [Components/FilterPanel.md](./Components/FilterPanel.md) | tự dựng |

Đừng chép số lượng vào tài liệu ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §6). Đếm bằng lệnh:

```bash
grep -L '^kind: lich-su' docs/Design/Components/*.md | wc -l
grep -cE '^\| `[A-Za-z]+` \|.*\]\(\./Components/' docs/Design/COMPONENTS.md
```

PASS = hai số bằng nhau.

> Lệnh thứ hai **cố ý neo vào hình dạng của một dòng bảng có link spec**, không neo vào chuỗi nào khác. Đếm theo một chuỗi nội dung sẽ đếm luôn dòng nhãn fidelity ở đầu file và đếm luôn chính khối lệnh này — một phép kiểm luôn lệch là một phép kiểm không ai chạy lần thứ hai. Cơ chế hỏng ghi ở [`../audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md`](../audit/2026-09-11-lenh-kiem-tu-dem-chinh-no.md).
>
> Lệnh thứ nhất **bỏ spec đã rút** (`kind: lich-su`) vì §8 gỡ dòng của nó khỏi bảng trên. Đếm thẳng bằng `ls` sẽ làm PASS này vỡ ngay lần rút đầu tiên.

### Cột Nền — định nghĩa gốc

**Cột "Nền" nhận đúng một trong hai giá trị: `bọc PrimeNG` · `tự dựng`.** FE đọc cột này bằng máy để xếp component vào thư mục — quy tắc ánh xạ ở [`../wiki-core/fe/05-component-library.md`](../wiki-core/fe/05-component-library.md). Component dựng **trên** một component bọc khác mà không tự import thư viện (như `ConfirmDialog` trên `Dialog`) mang `tự dựng`. Component bọc mà chỉ **một phần** biến thể cần thư viện (như `Chart`: chỉ `line`) vẫn mang `bọc PrimeNG` — các biến thể còn lại vẽ bên trong cùng lớp bọc.

### Trạng thái thi công đọc ở đâu

Bảng trên khai **danh sách** component Core và **nền** của từng cái. Nó cố ý **không** khai component nào đã dựng: trạng thái đó sống ở nhãn cấp tệp đầu mỗi spec, và một bản sao trong mục lục thì mục ruỗng mà không ai báo — đúng luật *một nội dung, một nguồn đối chiếu duy nhất* ở [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5: trạng thái của một file đọc ở đầu chính file đó, mục lục chỉ giữ nhãn cấp khu.

Đọc trạng thái từng component bằng lệnh:

```bash
grep -m1 -oE '^(📐|🚧|✅) ' docs/Design/Components/*.md
```

Mỗi dòng in ra là một spec kèm nhãn cấp tệp của nó. Nghĩa ba nhãn ở [`CLAUDE.md`](./CLAUDE.md) §2; riêng ở khu này `📐` còn mang thêm một nghĩa **máy kiểm được**: không có tệp component cùng tên dưới `src/FE/src/app/`, và cổng §27 đỏ nếu có.

PASS = hai số dưới đây bằng nhau:

```bash
grep -m1 -oE '^(📐|🚧|✅) ' docs/Design/Components/*.md | wc -l
grep -L '^kind: lich-su' docs/Design/Components/*.md | wc -l
```

Lệch nghĩa là có spec **không** mang nhãn cấp tệp nào, hoặc mang nó ở dạng cổng §27 không đọc được — đó không phải chuyện hình thức: nhãn cấp tệp là thứ duy nhất §27 đọc để quyết đỏ hay xanh.

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
6. **Chuỗi của chính thư viện đi qua tệp dịch, không đặt ở nơi gọi.** Thư viện có câu của riêng nó — "không có kết quả" trong ô chọn, nhãn đọc lên của nút phân trang, nút đóng. Chúng nạp từ nhánh `thuVienUi` của tệp dịch và nạp lại mỗi lần đổi ngôn ngữ (cơ chế: [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md) §8); **tên khoá là tên khoá gốc của thư viện**, kể cả nhánh con `aria`, để tra thẳng không cần bảng chuyển đổi — cùng lý do khoá lỗi giữ nguyên mã BE ([`../quy-uoc/fe-ui-conventions.md`](../quy-uoc/fe-ui-conventions.md) §5.3). Câu của từng khoá ở bảng dưới.

### Câu của thư viện — nhánh `thuVienUi`

Bảng này là **nguồn câu chữ** cho nhánh `thuVienUi` của tệp dịch; `frontend-expert` chép câu từ đây vào `vi.json`, không tự dịch. Ô mang tiền tố *Chờ duyệt:* là câu do `frontend-expert` dịch sát nghĩa bản tiếng Anh của thư viện khi thi công, **chưa ai duyệt** — khuôn nhãn ở [`CLAUDE.md`](./CLAUDE.md) §8; duyệt xong thì xoá tiền tố. Nhóm Paginator lấy câu từ [`Components/Pagination.md`](./Components/Pagination.md) §Accessibility. Cột cuối liệt kê mô-đun của thư viện đọc khoá đó (tìm tên khoá trong gói `primeng` đang cài, 2026-09-22), quy về lớp bọc Core tương ứng.

| Khoá (tên gốc của thư viện) | Câu | Lớp bọc nào hiện |
| --- | --- | --- |
| `emptyMessage` | *Chờ duyệt:* Không có lựa chọn nào | Lớp nổi của `Autocomplete`, `TreeSelect` khi danh mục rỗng; `DataTable`, `Pagination` cũng đọc khoá này nhưng thân bảng rỗng đã đi qua `emptyTemplate` (luật 5) |
| `emptySearchMessage` · `emptyFilterMessage` | *Chờ duyệt:* Không tìm thấy kết quả | Lớp nổi ô chọn khi gõ tìm / lọc không khớp gì |
| `searchMessage` | *Chờ duyệt:* Có {0} kết quả | Đọc lên số kết quả sau khi gõ tìm trong ô chọn |
| `selectionMessage` · `emptySelectionMessage` | *Chờ duyệt:* Đã chọn {0} mục · Chưa chọn mục nào | Đọc lên trạng thái chọn của ô chọn nhiều |
| `aria.close` | *Chờ duyệt:* Đóng | Nút đóng do thư viện vẽ: `Dialog`, `ConfirmDialog` (qua `Dialog`), `Toast` |
| `aria.firstPageLabel` · `aria.prevPageLabel` · `aria.previousPageLabel` · `aria.nextPageLabel` · `aria.lastPageLabel` | Trang đầu · Trang trước · Trang trước · Trang sau · Trang cuối | `Pagination` — [`Components/Pagination.md`](./Components/Pagination.md) §Accessibility, dòng "Nhãn từng nút". Hai khoá `prevPageLabel` / `previousPageLabel` cùng một câu vì thư viện tra cả hai tên |
| `aria.pageLabel` | Trang {page} | `Pagination` — cùng dòng trên ("Trang 3") |
| `aria.rowsPerPageLabel` | Số dòng mỗi trang | `Pagination` — cùng spec, dòng "Nhãn khác" |
| `aria.jumpToPageDropdownLabel` · `aria.jumpToPageInputLabel` | *Chờ duyệt:* Chọn trang · Nhập số trang | `Pagination` — ô nhảy tới trang, nếu biến thể nào bật |
| `aria.listLabel` | *Chờ duyệt:* Danh sách lựa chọn | Nhãn đọc lên của danh sách trong lớp nổi `Autocomplete` (mô-đun autocomplete, select, multiselect của thư viện) |
| `aria.removeLabel` | *Chờ duyệt:* Gỡ | Nút gỡ trên chip do thư viện vẽ trong ô chọn nhiều của `Autocomplete`, `TreeSelect` |
| `aria.selectAll` · `aria.unselectAll` | *Chờ duyệt:* Đã chọn tất cả · Đã bỏ chọn tất cả | `DataTable` công tắc `selectable` — đọc lên sau khi bấm ô đánh dấu ở `th` |
| `aria.selectRow` · `aria.unselectRow` | *Chờ duyệt:* Đã chọn dòng · Đã bỏ chọn dòng | `DataTable` công tắc `selectable` — đọc lên sau khi bấm ô đánh dấu của dòng |

Khoá nào thư viện có mà bảng không có thì thư viện rơi về câu tiếng Anh mặc định của nó — thấy câu tiếng Anh lọt ra giao diện là dấu hiệu bảng này thiếu dòng, thêm dòng vào đây **trước**, rồi mới thêm vào tệp dịch.

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
| Class trạng thái | `.<gốc>--<trạng thái>` — cùng khuôn với biến thể, xem lý do dưới bảng | `.form-row--disabled` |
| Class phần con | `.<gốc>__<phần>` | `.card__header` |
| Token riêng component | `--<gốc>-<thuộc tính>` | `--btn-height` |

**Vì sao "class trạng thái" dùng chung khuôn `--` với "class biến thể", không còn `.is-<trạng thái>` riêng.** Đối chiếu `src/FE/src/app/shared/components/` (2026-09-17), trên các thư mục component có mặt khi đó — `card`, `confirm-dialog`, `footer`, `form-row`, `notice-banner`, `page-header`, `sidebar`, `skeleton-loader`, `topbar`, `auth-card`, `auth-field` — thì:

- Không nơi nào dùng `.is-<trạng thái>`. Phần lớn tám trạng thái ở §2.1 (`hover`, `focus-visible`, `active`, `disabled`) được thể hiện bằng pseudo-class CSS gốc (`:hover`, `:focus-visible`, `:disabled`, `:active`) — không cần một class nào cả.
- Đúng một chỗ cần class cho trạng thái vì phần tử không phải control gốc: `form-row.component.scss` § `.form-row--disabled .form-row__nhan`. Nó tự chọn khuôn `--` giống biến thể, không phải `.is-`.
- Angular binding không phân biệt "biến thể chọn qua input" và "trạng thái đổi lúc chạy" — cả hai đều là `[class.x]="dieu-kien()"`. Giữ hai khuôn cho một cơ chế giống nhau chỉ tạo một lựa chọn phải nhớ mỗi lần thêm class mới, mà không cứu được gì.

Năm điều cấm:

- 🛑 **Không tiền tố công ty/dự án** (`csk-`, `core-`) trong tên class. Core đã là gốc của mọi thứ; tiền tố chỉ thêm nhiễu.
- 🛑 **Không đặt tên theo màu hay vị trí**: không `.btn-blue`, không `.card-left`. Ngày đổi màu hoặc đổi bố cục là ngày tên nói dối.
- 🛑 **Không đặt tên theo màn hình**: không `.user-list-toolbar`. Đó là dấu hiệu component đang bị chặt vào một màn.
- 🛑 **Không viết tắt** trừ khi viết tắt phổ biến hơn từ đầy đủ (`id`, `url`, `api`). `btn` là ngoại lệ đã chốt vì nó phổ biến tới mức không gây mơ hồ.
- 🛑 **Không dùng class trạng thái trần, không tiền tố** (`.active`, `.disabled` không qua `.<gốc>--`). Một class trần không đóng gói theo component, va tên được với bất cứ thứ gì khác trên trang. Đối chiếu `sidebar.component.html` § `sidebar__muc` (2026-09-17): nợ này đã đóng — phần tử `<a>` mang `class="sidebar__muc"` cùng `[class.sidebar__muc--active]` / `[class.sidebar__muc--disabled]`, không còn `[class.active]`/`[class.disabled]` không tiền tố.

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
2. **Gỡ dòng của nó khỏi bảng §3.** Dòng đó mang một link tới spec, và cổng [`../RULES.md`](../RULES.md) §1 D10 cấm trích dẫn file mang `kind: lich-su`. Giữ dòng lại thì hoặc cổng đỏ, hoặc phải bỏ link — và bỏ link làm vỡ phép đếm PASS ngay dưới bảng §3.
3. **Giữ file spec, đổi `kind` thành `lich-su`**, dán banner ở đầu file theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5. Banner đó là nơi ghi **vì sao rút** và **đi đâu**, kèm trỏ về component thay thế.
4. **Gỡ mọi link trỏ tới nó** ở phần còn lại của khu Design — screen spec, prompt pack, spec component khác. Cổng D10 ép bước này, nên nó không phải tuỳ chọn.

**Lý do rút không được xoá — nó đổi chỗ, từ mục lục sang banner của chính file spec.** Lý do đó — *"vì màn hình dùng nó đã bỏ"* chứ không phải *"vì nó là component tồi"* — là bằng chứng người sau cần khi cân nhắc dựng lại. Để nó ở mục lục thì nó là bản sao thứ hai của một câu đã có trong file spec, và bản sao ấy sẽ không được sửa cùng lúc ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5). Để nó ở banner thì người mở file gặp nó ngay, còn người quét mục lục thấy đúng thứ mục lục có nhiệm vụ khai: component nào **đang** dùng được.

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
