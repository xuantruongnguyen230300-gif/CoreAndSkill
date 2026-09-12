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
| | 🛑 Điều hướng chính → [`Sidebar.md`](./Sidebar.md), trừ khi ứng dụng chỉ có vài mục |

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
| Lớp | `--z-topbar` |

**Chiều cao cố định, không co giãn.** `Topbar` dính đỉnh nên chiều cao của nó bị trừ khỏi mọi tính toán chiều cao khác trên trang. Một thanh cao thấp thất thường làm mọi vùng cuộn bên dưới tính sai.

**Bóng chỉ xuất hiện khi trang đã cuộn.** Ở đỉnh trang thì viền dưới là đủ; có bóng lúc chưa cuộn trông như một lớp nổi vô cớ. Bóng xuất hiện là tín hiệu "có nội dung đang trôi bên dưới".

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Nền `--color-surface`, viền dưới `--color-border`, không bóng | Có |
| `hover` | **Không áp dụng ở mức thanh.** Hover thuộc các control bên trong | — |
| `focus-visible` | **Không áp dụng ở mức thanh.** Focus thuộc các control bên trong | — |
| `active` | **Không áp dụng.** Thanh không phải control. *(Trạng thái "đã cuộn" — thêm `--shadow-2` — không phải `active`; nó là một biến hiển thị riêng.)* | — |
| `disabled` | **Không áp dụng.** Muốn chặn thao tác thì khoá từng control | — |
| `loading` | Chỉ khu tài khoản có trạng thái này, khi hồ sơ người dùng chưa về: [`SkeletonLoader.md`](./SkeletonLoader.md) hình tròn + một khối chữ nhật. Phần còn lại của thanh **hiện bình thường** — hamburger phải bấm được ngay | Có |
| `error` | Không tải được hồ sơ: hiện [`Avatar.md`](./Avatar.md) mặc định + nút đăng xuất. **Không** chặn cả thanh — người dùng vẫn phải điều hướng và đăng xuất được | Có |
| `empty` | **Không áp dụng.** `Topbar` luôn có ít nhất hamburger và khu tài khoản | — |

**Lỗi tải hồ sơ không được chặn `Topbar`.** Đây là ca thật hay xảy ra khi phiên hết hạn: nếu thanh hỏng thì người dùng không đăng xuất được và không thoát ra được trạng thái hỏng.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-text`, `--color-text-muted`, `--color-border`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-focus` |
| Chữ | `--fs-sm`, `--fs-md`, `--fw-medium`, `--fw-semibold`, `--lh-snug` |
| Khoảng cách | `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-md`, `--border-w` |
| Kích thước | `--layout-topbar-h`, `--size-control-md`, `--icon-md`, `--icon-lg` |
| Bóng | `--shadow-2`, `--shadow-3` |
| Lớp | `--z-topbar`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-lg` | Không có hamburger; tiêu đề tuyến bên trái; khu tài khoản bên phải với tên hiển thị đầy đủ |
| `--bp-md` … `--bp-lg` | Có nút thu/mở `Sidebar`; tên người dùng vẫn hiện |
| < `--bp-md` | Có hamburger mở drawer; **ẩn tên người dùng**, chỉ còn [`Avatar.md`](./Avatar.md); tiêu đề tuyến cắt ngắn bằng dấu ba chấm |
| < `--bp-xs` | Ẩn cả tiêu đề tuyến nếu cần chỗ cho hamburger và avatar; các thao tác phụ (đổi theme, đổi ngôn ngữ) dồn vào menu người dùng |

**Thứ tự hy sinh khi hết chỗ, từ bỏ trước tới bỏ sau:** thao tác phụ → tên người dùng → tiêu đề tuyến. Hamburger và avatar **không bao giờ** bị bỏ: một cái là đường duy nhất tới điều hướng, cái kia là đường duy nhất tới đăng xuất.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<header>` với `role="banner"` |
| Hamburger | `<button>` với `aria-label`, `aria-expanded` phản ánh trạng thái drawer, và `aria-controls` trỏ tới id của drawer |
| Tiêu đề tuyến | Là chữ, **không** phải `<h1>`. `<h1>` thuộc [`PageHeader.md`](./PageHeader.md) — xem dưới |
| Menu người dùng | Nút mở có `aria-haspopup="menu"` và `aria-expanded`. Menu có `role="menu"`, mỗi mục `role="menuitem"`; mũi tên di chuyển, Escape đóng, focus trả về nút mở |
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
| `title` | input | `string \| null` | `null` | Tiêu đề tuyến. Khung ứng dụng đọc từ dữ liệu tuyến và truyền vào |
| `user` | input | `{ name: string; email: string; avatarUrl: string \| null } \| null` | `null` | `null` = đang tải hoặc lỗi |
| `menuItems` | input | `ReadonlyArray<{ id: string; label: string; icon: string; danger?: boolean }>` | `[]` | Mục trong menu người dùng. `danger: true` cho đăng xuất |
| `sidebarMode` | input | `'expanded' \| 'collapsed' \| 'drawer'` | `'expanded'` | Quyết định hiện hamburger hay nút thu/mở |
| `drawerOpen` | input | `boolean` | `false` | Để đặt `aria-expanded` |
| `state` | input | `'idle' \| 'loading' \| 'error'` | `'idle'` | Chỉ nói về khu tài khoản |
| `menuToggled` | output | `void` | — | Bấm hamburger hoặc nút thu/mở |
| `menuItemSelected` | output | `string` | — | Id mục trong menu người dùng |

**`Topbar` không tự biết mình đang ở tuyến nào và không tự gọi API lấy hồ sơ.** Nó nhận cả hai qua `input()` — component **dumb** theo [`../COMPONENTS.md`](../COMPONENTS.md) §5.

**Menu người dùng nhận danh sách mục qua `input()` chứ không khai cứng bên trong.** Khai cứng nghĩa là `Topbar` phải biết ứng dụng có tính năng đổi mật khẩu hay không — một mẩu nghiệp vụ lọt vào component Core. Nó cũng làm mục menu không thể thay đổi theo quyền.

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
| 1 | Có ô tìm kiếm toàn cục trên `Topbar` không? Nó rất hữu ích nhưng cần một hạ tầng tìm kiếm xuyên module chưa được thiết kế | Sau khi có nhiều module |
| 2 | Đường dẫn phân cấp (breadcrumb) đặt ở `Topbar` hay [`PageHeader.md`](./PageHeader.md)? Hôm nay spec đặt ở `PageHeader`; đặt ở `Topbar` thì luôn thấy khi cuộn nhưng làm thanh cao lên | Khi dựng màn chi tiết đầu tiên |
| 3 | Nút đổi theme là một `IconButton` trên thanh hay một mục trong menu người dùng? Trên thanh thì nhanh; trong menu thì gọn. Ở màn nhỏ nó **buộc** phải vào menu | Người dựng khung ứng dụng |
