---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Topbar

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là một dải bố cục thuần; hành vi duy nhất đáng kể là menu người dùng, và menu đó là một lớp nổi tách riêng chứ không phải một phần của thanh.

---

## Mục đích

Thanh đầu trang cố định: mở drawer ở màn nhỏ, cho biết đang ở đâu, và chứa các thao tác cấp tài khoản.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Khung ứng dụng chính, mọi màn sau khi đăng nhập | 🛑 Màn xác thực → [`AuthCard.md`](./AuthCard.md), khung đó cố ý không có `Topbar` |
| Chứa hành động **cấp tài khoản**: hồ sơ, đổi mật khẩu, đăng xuất, đổi theme, đổi ngôn ngữ | 🛑 Hành động **của trang hiện tại** ("Thêm người dùng") → [`PageHeader.md`](./PageHeader.md). Đây là nhầm lẫn hay gặp nhất và nó làm `Topbar` phình theo từng màn |
| Chứa tiêu đề tuyến để biết đang ở đâu | 🛑 Hành động **trên một danh sách** (lọc, tìm) → [`Toolbar.md`](./Toolbar.md) |
| | 🛑 Điều hướng chính → [`Sidebar.md`](./Sidebar.md), trừ khi ứng dụng chỉ có vài mục; 🛑 đường dẫn phân cấp → [`PageHeader.md`](./PageHeader.md), `Topbar` chỉ giữ tiêu đề tuyến |

**Ranh giới `Topbar` ↔ `PageHeader` là ranh giới quan trọng nhất của component này.** Phép thử: *thao tác này có ý nghĩa ở mọi màn không?* Có → `Topbar`. Chỉ ở màn này → `PageHeader`. Bỏ qua phép thử này thì `Topbar` dần biến thành nơi chứa mọi nút mà không màn nào nhận, và cuối cùng nó phải biết mình đang ở màn nào — tức là hết dumb.

## Biến thể

| Biến thể | Khác gì | Dùng khi |
| --- | --- | --- |
| `default` | Tiêu đề tuyến + khu tài khoản | Mọi màn trong khung ứng dụng |
| `with-nav` | Thêm nhóm liên kết điều hướng ở giữa | Ứng dụng **không có** `Sidebar` vì chỉ vài mục |

Không có biến thể "trong suốt" hay "tràn viền". Chúng làm chữ trên thanh không đoán trước được tương phản, và đó là thứ không đánh đổi được.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Chiều cao | `--layout-topbar-h` (56px), cố định |
| Đệm ngang | `--sp-6` |
| Khe giữa các phần tử | `--sp-4` |
| Nền | `--color-surface` |
| Viền dưới | `--border-w` `--color-border` |
| Bóng khi trang đã cuộn | `--shadow-2` |
| Cỡ chữ tiêu đề tuyến | `--fs-md`, `--fw-semibold` |
| Cỡ chữ tên người dùng | `--fs-sm`, `--fw-medium`, `--color-text` |
| Cỡ chữ tên đơn vị | `--fs-xs`, `--fw-regular`, `--color-text-muted` |
| Lớp | `--z-topbar` |

**Chiều cao cố định, không co giãn.** `Topbar` dính đỉnh nên chiều cao của nó bị trừ khỏi mọi tính toán chiều cao khác trên trang. Một thanh cao thấp thất thường làm mọi vùng cuộn bên dưới tính sai.

**Bóng chỉ xuất hiện khi trang đã cuộn.** Ở đỉnh trang thì viền dưới là đủ; có bóng lúc chưa cuộn trông như một lớp nổi vô cớ. Bóng xuất hiện là tín hiệu "có nội dung đang trôi bên dưới".

**Tên đơn vị của phiên là dòng thứ hai của khu tài khoản**, nằm ngay dưới tên người dùng, trong cùng khối chữ đặt cạnh [`Avatar.md`](./Avatar.md). Hai dòng dùng `--lh-snug` và tổng chiều cao của khối vẫn nằm trong `--layout-topbar-h` — chiều cao thanh không đổi theo việc có hay không có tên đơn vị. Tên dài cắt bằng dấu ba chấm trên **một** dòng; không cho nó xuống dòng thứ ba.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền dưới `--color-border`, không bóng | Có |
| `hover` | **Không áp dụng ở mức thanh.** Hover thuộc các control bên trong | — |
| `focus-visible` | **Không áp dụng ở mức thanh.** Focus thuộc các control bên trong | — |
| `active` | **Không áp dụng.** Thanh không phải control. *(Trạng thái "đã cuộn" — thêm `--shadow-2` — không phải `active`; nó là một biến hiển thị riêng.)* | — |
| `disabled` | **Không áp dụng.** Muốn chặn thao tác thì khoá từng control | — |
| `loading` | Chỉ khu tài khoản có trạng thái này, khi hồ sơ người dùng chưa về: [`SkeletonLoader.md`](./SkeletonLoader.md) hình tròn + một khối chữ nhật cho **mỗi dòng chữ đang hiện ở ngưỡng đó** (tên người dùng, tên đơn vị). Phần còn lại của thanh **hiện bình thường** — hamburger phải bấm được ngay | Có |
| `error` | Không tải được hồ sơ: hiện [`Avatar.md`](./Avatar.md) mặc định + nút đăng xuất; `tenantName` rỗng nên dòng tên đơn vị **không vẽ**, không thay bằng chữ giữ chỗ. **Không** chặn cả thanh — người dùng vẫn phải điều hướng và đăng xuất được | Có |
| `empty` | **Không áp dụng.** `Topbar` luôn có ít nhất hamburger và khu tài khoản | — |

**Lỗi tải hồ sơ không được chặn `Topbar`.** Đây là ca thật hay xảy ra khi phiên hết hạn: nếu thanh hỏng thì người dùng không đăng xuất được và không thoát ra được trạng thái hỏng.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-text`, `--color-text-muted`, `--color-border`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-md`, `--fw-regular`, `--fw-medium`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-md`, `--border-w` |
| Kích thước | `--layout-topbar-h`, `--size-control-md`, `--icon-md`, `--icon-lg` |
| Bóng | `--shadow-2`, `--shadow-3` |
| Lớp | `--z-topbar`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Không có hamburger; tiêu đề tuyến bên trái; khu tài khoản bên phải với tên hiển thị đầy đủ và **tên đơn vị ở dòng dưới** |
| `$bp-md` … `$bp-lg` | Có nút thu/mở `Sidebar`; tên người dùng vẫn hiện; **ẩn dòng tên đơn vị** |
| < `$bp-md` | Có hamburger mở drawer; **ẩn tên người dùng**, chỉ còn [`Avatar.md`](./Avatar.md); tiêu đề tuyến cắt ngắn bằng dấu ba chấm |
| < `$bp-xs` | Ẩn cả tiêu đề tuyến nếu cần chỗ cho hamburger và avatar; nút theme dồn vào menu người dùng thành một mục mở cùng `Menu` theme, nối tiếp như mục "Ngôn ngữ". Đổi ngôn ngữ vốn nằm trong menu đó ở mọi ngưỡng |

**Thứ tự hy sinh khi hết chỗ, từ bỏ trước tới bỏ sau:** thao tác phụ → tên đơn vị → tên người dùng → tiêu đề tuyến. Hamburger và avatar **không bao giờ** bị bỏ: một cái là đường duy nhất tới điều hướng, cái kia là đường duy nhất tới đăng xuất.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<header>` với `role="banner"` |
| Hamburger | `<button>` với `aria-label`, `aria-expanded` phản ánh trạng thái drawer, và `aria-controls` trỏ tới id của drawer |
| Tiêu đề tuyến | Là chữ, **không** phải `<h1>`. `<h1>` thuộc [`PageHeader.md`](./PageHeader.md) — xem dưới |
| Tên đơn vị | Là chữ thuần, không phải heading và không bấm được. Nó bị ẩn về mặt thị giác từ dưới `$bp-lg`, nên nhãn đọc lên của nút mở menu người dùng phải ghép **cả tên người dùng lẫn tên đơn vị** — không ghép thì người dùng trình đọc màn hình mất thông tin này ở đúng những ngưỡng hẹp |
| Menu người dùng | Nút mở có `aria-haspopup="menu"` và `aria-expanded`. Menu có `role="menu"`, mỗi mục `role="menuitem"`; mũi tên di chuyển, Escape đóng, focus trả về nút mở |
| Mục "Ngôn ngữ" | `role="menuitem"` với `aria-haspopup="menu"`. Chọn mục thì danh sách ngôn ngữ mở và focus vào ngôn ngữ đang dùng; `Escape` đóng danh sách và trả focus về nút mở menu người dùng |
| Nút theme | `aria-label` nói rõ đang mở lựa chọn giao diện, `aria-haspopup="menu"`, `aria-expanded`. `Menu` mở ra có ba mục `role="menuitemradio"`, mục đang áp mang `aria-checked="true"` và nhận focus khi mở — [`Menu.md`](./Menu.md) §Accessibility |
| Bàn phím | Mọi control trong thanh nằm trong thứ tự Tab tự nhiên |
| Focus | `outline` + `outline-offset` ≥ 2px |
| Liên kết bỏ qua | Liên kết "Bỏ qua tới nội dung chính" đứng **trước** `Topbar` trong DOM |
| Đổi theme / ngôn ngữ | Đổi xong phải thông báo qua vùng `aria-live` — thay đổi thuần hình ảnh thì người dùng trình đọc màn hình không biết gì |
| Chữ | Qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Vì sao tiêu đề tuyến trong `Topbar` không phải `<h1>`:** mỗi trang chỉ nên có một `<h1>`, và nó phải là tiêu đề của **nội dung**, không phải của khung. Đặt `<h1>` ở `Topbar` thì mọi trang có cùng cấu trúc heading và người dùng trình đọc màn hình mất khả năng nhảy tới nội dung chính bằng phím tắt heading. Tiêu đề tuyến ở `Topbar` chỉ là một chỉ dẫn trực quan lặp lại thông tin đã có ở `PageHeader`.

**Đây có phải là lặp thông tin không?** Có, và có chủ đích: `Topbar` dính đỉnh nên tiêu đề ở đó vẫn thấy khi trang đã cuộn xa. `PageHeader` cuộn đi cùng nội dung. Nhưng lặp về mặt hình ảnh không có nghĩa là lặp về mặt ngữ nghĩa — chỉ một trong hai là heading thật.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `title` | input | `string \| null` | `null` | Tiêu đề tuyến. `platform/shell` đọc từ dữ liệu tuyến và truyền vào |
| `user` | input | `{ fullName: string; userName: string; email: string \| null } \| null` | `null` | `null` = đang tải hoặc lỗi. `platform/shell` lấy từ phản hồi phiên ([`../../contracts/auth.md`](../../contracts/auth.md) §5), giữ đúng tên trường. Phản hồi không mang ảnh đại diện, nên [`Avatar.md`](./Avatar.md) nhận `fullName` và vẽ chữ cái đầu |
| `tenantName` | input | `string` | `''` | Tên đơn vị của phiên. `platform/shell` lấy từ phản hồi phiên ([`../../contracts/auth.md`](../../contracts/auth.md) §5), giữ đúng tên trường; field này **chỉ để hiển thị**. Chuỗi rỗng — chưa có phiên, hoặc hồ sơ lỗi — thì **không vẽ dòng** và không chừa chỗ trống |
| `menuItems` | input | `ReadonlyArray<UiMenuItem>` | `[]` | Mục trong menu người dùng. Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 — cùng kiểu với `items` của [`Menu.md`](./Menu.md), không có kiểu riêng cho `Topbar`. `danger: true` cho đăng xuất |
| `sidebarMode` | input | `'expanded' \| 'collapsed' \| 'drawer'` | `'expanded'` | Quyết định hiện hamburger hay nút thu/mở |
| `drawerOpen` | input | `boolean` | `false` | Để đặt `aria-expanded` |
| `state` | input | `'idle' \| 'loading' \| 'error'` | `'idle'` | Chỉ nói về khu tài khoản |
| `languages` | input | `ReadonlyArray<LanguageOption>` | `[]` | Truyền thẳng xuống [`LanguageSwitcher.md`](./LanguageSwitcher.md). Dưới hai mục thì menu người dùng **không có** mục "Ngôn ngữ" |
| `currentLanguage` · `pendingLanguage` | input | `string \| null` | `null` | Truyền thẳng xuống `LanguageSwitcher`, cùng nghĩa như ở đó |
| `theme` | input | `'light' \| 'dark' \| 'system'` | `'light'` | Giá trị đang áp, quyết icon của nút theme và là `selectedKey` của `Menu` theme. `platform/shell` đọc từ lựa chọn đã lưu ([`../DESIGN.md`](../DESIGN.md) §8) |
| `menuToggled` | output | `void` | — | Bấm hamburger hoặc nút thu/mở |
| `menuItemSelected` | output | `string` | — | `key` của mục trong menu người dùng. Không phát cho mục "Ngôn ngữ" |
| `languageChangeRequested` | output | `string` | — | Mã ngôn ngữ người dùng chọn trong danh sách mở từ mục "Ngôn ngữ" |
| `themeChangeRequested` | output | `'light' \| 'dark' \| 'system'` | — | Giá trị người dùng **chọn** trong `Menu` theme; không phát khi chọn lại giá trị đang áp. `platform/shell` áp và lưu, rồi truyền `theme` mới xuống |

**`Topbar` không tự biết người đang đăng nhập là ai, không tự biết mình đang ở tuyến nào, và không tự gọi API lấy hồ sơ.** Nó nhận cả ba qua `input()` từ `platform/shell` — tầng smart inject phiên và menu — nên là component **dumb** theo [`../COMPONENTS.md`](../COMPONENTS.md) §5.

**Menu người dùng nhận danh sách mục qua `input()` chứ không khai cứng bên trong.** Khai cứng nghĩa là `Topbar` phải biết ứng dụng có tính năng đổi mật khẩu hay không — một mẩu nghiệp vụ lọt vào component Core. Nó cũng làm mục menu không thể thay đổi theo quyền.

**Mục "Ngôn ngữ" là ngoại lệ: `Topbar` tự thêm nó**, không nhận qua `menuItems` — cùng cách với mục theme khi nút theme dồn vào menu dưới `$bp-xs`. Đổi ngôn ngữ là thao tác của Core chứ không phải mẩu nghiệp vụ, và chọn mục này không phát `menuItemSelected` mà mở một lớp nổi khác — nên `Topbar` phải biết nó. Mục có mặt khi `languages` có từ hai mục, icon `pi-globe` ([`../Icons.md`](../Icons.md) §5), đứng sau các mục thường và trước các mục `danger`. Chọn mục → menu người dùng đóng, rồi [`LanguageSwitcher.md`](./LanguageSwitcher.md) biến thể `anchored` mở, neo vào **cùng** nút mở menu người dùng. Hai lớp nổi nối tiếp nhau, không lồng nhau ([`../COMPONENTS.md`](../COMPONENTS.md) §2.4). Người dùng chọn một ngôn ngữ → `Topbar` phát `languageChangeRequested`; `platform/shell` tải gói dịch rồi truyền `currentLanguage` mới xuống — vòng đi của [`LanguageSwitcher.md`](./LanguageSwitcher.md) §API.

**Nút theme là một [`IconButton.md`](./IconButton.md) `ghost` đặt ngay trên thanh** — [`../DESIGN.md`](../DESIGN.md) §8 đặt nó ở đây, không giấu trong menu (trừ khổ < `$bp-xs`, §Responsive). Icon hiện giá trị đang áp theo `pi-sun` · `pi-moon` · `pi-desktop` ([`../Icons.md`](../Icons.md) §5). Bấm nút mở một [`Menu.md`](./Menu.md) `anchored` ba mục `menuitemradio` sáng · tối · theo hệ điều hành — `Topbar` tự dựng ba mục này như mục "Ngôn ngữ", không nhận qua `menuItems`; `selectedKey` là `theme`. Chọn một mục → menu đóng, `Topbar` phát `themeChangeRequested`; áp và lưu là việc của `platform/shell`.

## Do / Don't

- ✅ Chỉ đặt hành động **cấp tài khoản** vào `Topbar`.
- ✅ Chiều cao cố định.
- ✅ Bóng chỉ khi đã cuộn.
- ✅ Giữ hamburger và avatar ở mọi khổ màn.
- ✅ Thông báo qua `aria-live` khi đổi theme hoặc ngôn ngữ.
- ❌ Không đặt `<h1>` trong `Topbar`.
- ❌ Không đặt hành động của trang vào `Topbar`.
- ❌ Không để `Topbar` tự đọc router hay tự gọi API.
- ❌ Không để lỗi tải hồ sơ chặn cả thanh.
- ❌ Không khai cứng danh sách mục menu người dùng.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có ô tìm kiếm toàn cục trên `Topbar` không? Nó rất hữu ích nhưng cần một hạ tầng tìm kiếm xuyên module chưa được thiết kế | Sau F3 — dự án hạ nguồn đầu tiên có nhiều module |
