---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Sidebar

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4, cây menu là đệ quy đơn giản, không thuộc nhóm "khó". Riêng dạng `drawer` ở màn nhỏ **bọc** [`Drawer.md`](./Drawer.md) `position: left` — bẫy focus, khoá cuộn nền, trả focus là hành vi khó và đã giải ở đó; chiều `shared/components/` dùng `shared/ui/` là chiều được phép ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §2.2). Bọc một component điều hướng của thư viện lại buộc phải đè style nặng, vì hình thức của nó gắn chặt với thương hiệu.

---

## Mục đích

Dải điều hướng chính của khung ứng dụng — cây menu, thu gọn được, và biến thành drawer ở màn nhỏ.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Điều hướng cấp cao nhất giữa các khu vực của ứng dụng | 🛑 Chuyển giữa các phần **trong cùng một trang** → [`Tabs.md`](./Tabs.md) |
| Ứng dụng có từ khoảng năm mục điều hướng trở lên | 🛑 Ứng dụng chỉ có hai, ba mục → để hết trong [`Topbar.md`](./Topbar.md); một dải trống 240px cho ba mục là phí chỗ |
| Điều hướng theo cây, có mục con | 🛑 Danh sách hành động theo bối cảnh → [`Toolbar.md`](./Toolbar.md) |
| | 🛑 Lọc dữ liệu theo nhóm → đó là bộ lọc, thuộc `Toolbar` |

## Biến thể

| Biến thể | Bề rộng | Khi nào |
| --- | --- | --- |
| `expanded` | `--layout-sidebar-w` (240px) | ≥ `$bp-lg`. Icon + nhãn |
| `collapsed` | `--layout-sidebar-w-collapsed` (64px) | `$bp-md` … `$bp-lg`, hoặc khi người dùng tự thu. Chỉ icon; hiện nhãn trong một lớp nổi khi hover hoặc focus |
| `drawer` | `Drawer` `left` cỡ `sm` — `--layout-drawer-w-sm`, trần 92vw ([`Drawer.md`](./Drawer.md) §Kích thước) | < `$bp-md`. Là [`Drawer.md`](./Drawer.md) biến thể `edit` (lớp phủ, bẫy focus), `title` là nhãn của `<nav>`; `Sidebar` chỉ đổ cây menu vào slot thân. Giữ neo trái ở mọi ngưỡng, không đổi sang `bottom` |

**Trạng thái thu/mở do người dùng chọn phải được ghi nhớ** giữa các lần vào. Người dùng thu sidebar là vì họ cần chỗ; bắt họ thu lại mỗi lần tải trang là hỏi lại một câu đã trả lời. Nhớ ở `localStorage` như lựa chọn theme ([`../DESIGN.md`](../DESIGN.md) §8) — hồ sơ người dùng không có trường này ([`../../contracts/profile.md`](../../contracts/profile.md)), và đó là lựa chọn theo máy, không theo người.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Chiều cao mục nav | `--size-control-lg` (42px) |
| Đệm ngang mục nav | `--sp-5` |
| Khe icon → nhãn | `--sp-4` |
| Thụt lề mỗi cấp con | `--sp-6` |
| Đệm dọc quanh nhóm | `--sp-4` |
| Bo góc mục nav | `--radius-md` |
| Cỡ chữ nhãn | `--fs-sm`, `--fw-medium` |
| Cỡ chữ tiêu đề nhóm | `--fs-2xs`, `--fw-semibold`, `--ls-wide`, `--color-text-muted` |
| Nền | `--color-surface` |
| Viền phải | `--border-w` `--color-border` |
| Lớp | `--z-sidebar`; dạng `drawer` dùng lớp của `Drawer` |

**Chiều cao mục nav là `lg` (42px) chứ không `md`.** Vùng bấm điều hướng bị dùng liên tục và ở màn nhỏ dùng bằng ngón tay; 34px là đủ chuẩn nhưng chật.

**Cây menu tối đa hai cấp.** Cấp ba nghĩa là mục ở đó bị chôn quá sâu để tìm thấy, và ở biến thể `collapsed` thì đơn giản là không hiển thị nổi. Cần cấp ba thì đó là dấu hiệu nên có một trang danh mục trung gian. Ở `collapsed`, mục có con mở một **flyout**: chính `<ul>` con với từng `<li><a routerLink>`, định vị cạnh dải icon, cùng lớp nổi hiện nhãn ở §Trạng thái (`hover`); mở khi hover, focus hoặc bấm nút cha mang `aria-expanded`. 🛑 Không dùng [`Menu.md`](./Menu.md) — file đó cấm dùng cho điều hướng chính, và qua `Menu` thì mất `aria-current` lẫn liên kết thật. Mục nav không có số đếm: [`../../contracts/meta-menu.md`](../../contracts/meta-menu.md) không mang trường đó.

## Trạng thái

Trạng thái ở đây phần lớn là của **một mục nav**, không phải của cả dải.

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Mục nav: chữ `--color-text`, icon kế thừa màu chữ, nền trong suốt | Có |
| `hover` | Nền `--color-surface-2`. Ở `collapsed` thì thêm một lớp nổi hiện nhãn, nền `--color-surface`, `--shadow-3`. Bọc `@media (hover: hover)` | Có |
| `focus-visible` | `outline` `--color-focus`, `outline-offset: -2px` (vòng nằm trong, vì mục nav sát mép dải). Ở `collapsed`, focus cũng bật lớp nổi hiện nhãn — người dùng bàn phím phải đọc được nhãn | Có |
| `active` | **Mục của tuyến hiện tại.** Ba kênh cùng lúc: nền `--color-brand-subtle`, chữ và icon `--color-brand-on-subtle`, dải `--border-w-accent` `--color-brand` ở mép trái | Có |
| `disabled` | Mục người dùng không có quyền vào: chữ `--color-text-disabled`, không hover, không bấm được. **Cân nhắc ẩn hẳn thay vì làm mờ** — xem dưới | Có |
| `loading` | Cây menu chưa về: [`SkeletonLoader.md`](./SkeletonLoader.md) dạng vài dòng chữ nhật. **Không** hiện một sidebar rỗng rồi mọc mục ra — nó làm bố cục nhảy | Có |
| `error` | Không tải được menu: một dòng chữ ngắn `--color-text-muted` + nút "Thử lại" cỡ `sm`. **Không** dùng [`NoticeBanner.md`](./NoticeBanner.md) — dải quá hẹp cho nó | Có |
| `empty` | Người dùng không có quyền vào mục nào: hiện logo, phần menu để trống với một dòng giải thích. Không hiện dải trống không có gì. Khu tài khoản và đường đăng xuất thuộc [`Topbar.md`](./Topbar.md), không thuộc `Sidebar` | Có |

**Ba kênh cho mục đang chọn là bắt buộc, không phải trang trí.** `--color-brand-subtle` chênh với `--color-surface` chỉ **1.25:1** ([`../DESIGN.md`](../DESIGN.md) §2.4) — dưới xa mọi ngưỡng. Dải cạnh trái `--color-brand` đạt 6.85:1 và màu chữ `--color-brand-on-subtle` đạt 7.47:1; chúng mới là thứ thật sự báo "bạn đang ở đây".

**Ẩn hay làm mờ mục không có quyền:** mặc định là **ẩn**. Một danh sách chức năng người dùng không bao giờ dùng được chỉ tạo thất vọng và làm menu dài ra. Ngoại lệ: khi tổ chức muốn người dùng biết chức năng đó tồn tại để đi xin quyền — lúc đó mới làm mờ, và phải có tooltip nói vì sao.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface`, `--color-surface-2`, `--color-text`, `--color-text-muted`, `--color-text-disabled`, `--color-border`, `--color-brand`, `--color-brand-subtle`, `--color-brand-on-subtle`, `--color-focus` |
| Chữ | `--fs-2xs`, `--fs-sm`, `--fw-medium`, `--fw-semibold`, `--ls-wide`, `--lh-snug` |
| Khoảng cách | `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-md`, `--border-w`, `--border-w-accent` |
| Kích thước | `--layout-sidebar-w`, `--layout-sidebar-w-collapsed`, `--size-control-lg`, `--icon-md`; dạng `drawer` theo token của `Drawer` |
| Bóng | `--shadow-3` |
| Lớp | `--z-sidebar` |
| Chuyển động | `--dur-slow`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | `expanded` cố định bên trái; phần nội dung lệch một khoảng bằng `--layout-sidebar-w` |
| `$bp-md` … `$bp-lg` | Mặc định `collapsed`; nhãn hiện qua lớp nổi khi hover/focus; người dùng mở rộng được |
| < `$bp-md` | `drawer` — ẩn hẳn, mở bằng nút hamburger ở [`Topbar.md`](./Topbar.md); lớp phủ và bẫy focus do `Drawer` gánh |
| < `$bp-xs` | Bề rộng chạm trần 92vw của `Drawer`; đóng khi chọn một mục |

Chuyển giữa `expanded` và `collapsed` chạy trong `--dur-slow` với `--ease-standard`; drawer trượt theo chuyển động của `Drawer`. Cả hai tắt khi `prefers-reduced-motion: reduce`.

🛑 **Không animate `width` khi thu/mở.** Nó bắt trình duyệt tính lại bố cục mỗi khung hình và kéo theo cả vùng nội dung. Với thu/mở thì chấp nhận chuyển tức thì còn hơn một animation giật.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<nav>` với `aria-label` nói đây là điều hướng gì ("Điều hướng chính") |
| Danh sách | `<ul>` / `<li>`; mục con là `<ul>` lồng trong `<li>` cha |
| Mục nav | `<a>` với `routerLink`. 🛑 Không `<div (click)>` — mất mở tab mới, mất copy link, mất mọi thứ |
| Mục hiện tại | `aria-current="page"` trên đúng một mục. Đây là kênh mà trình đọc màn hình dùng; ba kênh hình ảnh ở trên không thay được nó |
| Mục có con | `<button aria-expanded>` để mở/đóng nhánh. Nhánh cha vừa điều hướng vừa mở rộng thì tách làm hai phần tử: liên kết và nút mở rộng |
| Bàn phím | Tab đi qua từng mục theo thứ tự đọc. Enter kích hoạt. Mũi tên **không** bắt buộc — đây là danh sách liên kết, không phải widget menu |
| Drawer | Do [`Drawer.md`](./Drawer.md) §Accessibility gánh (biến thể `edit`): bẫy focus, Escape đóng, trả focus về nút hamburger |
| Flyout ở `collapsed` | Nút cha `aria-expanded`; các con là `<a routerLink>` thật trong `<ul>` nên `aria-current` và Tab giữ nguyên như khi `expanded`; Escape đóng flyout, trả focus về nút cha |
| Thu gọn | Nút thu/mở có `aria-label` đổi theo trạng thái và `aria-expanded` |
| Lớp nổi ở `collapsed` | Phải hiện khi **focus**, không chỉ khi hover. Chỉ hover là bỏ rơi hoàn toàn người dùng bàn phím |
| Bỏ qua điều hướng | Trang phải có liên kết "Bỏ qua tới nội dung chính" trước sidebar. Không có nó, người dùng bàn phím phải Tab qua toàn bộ menu ở **mọi** trang |
| Chữ | Nhãn menu qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Liên kết "bỏ qua tới nội dung chính" thuộc khung ứng dụng, không thuộc `Sidebar`** — nhưng nó chỉ có nghĩa vì `Sidebar` tồn tại, nên nó được ghi ở đây. Nó ẩn về mặt hình ảnh cho tới khi nhận focus.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `items` | input | `ReadonlyArray<NavItem>` | `[]` | Cây menu, tối đa hai cấp |
| `mode` | input | `'expanded' \| 'collapsed' \| 'drawer'` | `'expanded'` | `platform/shell` tính theo điểm ngắt và truyền xuống |
| `drawerOpen` | input | `boolean` | `false` | Chỉ có nghĩa ở `mode` là `'drawer'` |
| `activeRoute` | input | `string` | — | Bắt buộc. Truyền vào thay vì tự đọc router — đó là điều giữ component **dumb** |
| `state` | input | `'idle' \| 'loading' \| 'error' \| 'empty'` | `'idle'` | Một biến, không phải nhiều cờ |
| `modeChange` | output | `'expanded' \| 'collapsed'` | — | Người dùng bấm nút thu/mở |
| `drawerClose` | output | `void` | — | Escape, backdrop, hoặc chọn một mục |
| `retry` | output | `void` | — | |

**`NavItem`:** chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9.

🛑 **`label` là chuỗi ĐÃ dịch, không phải khoá i18n.** Nhận khoá thì `Sidebar` phải inject service dịch để hiển thị được, và lúc đó nó không còn là component dumb — vi phạm [`../COMPONENTS.md`](../COMPONENTS.md) §5 và luật F11. Trang cha phân giải khoá rồi truyền cây menu xuống. Cái giá: đổi ngôn ngữ lúc chạy buộc trang cha dựng lại cây.

**`Sidebar` nhận cây menu qua `input()`, không tự gọi API.** Đây là ca đầu trong bảng "dễ nhầm" ở [`../COMPONENTS.md`](../COMPONENTS.md) §5: menu đến từ máy chủ nên nó *trông* như phải là smart, nhưng `platform/shell` — tầng smart inject phiên và menu — mới là chỗ lấy dữ liệu rồi truyền xuống. `Sidebar` không tự biết người đang đăng nhập là ai, cũng không tự biết menu của họ.

**`activeRoute` truyền vào chứ không tự đọc router** vì cùng lý do — và thêm một lý do thực dụng: nó làm component dựng lên được trong test chỉ với một chuỗi.

## Do / Don't

- ✅ Ba kênh cho mục đang chọn: nền, màu chữ, dải cạnh.
- ✅ `aria-current="page"` trên đúng một mục.
- ✅ Ghi nhớ lựa chọn thu/mở của người dùng.
- ✅ Ẩn mục không có quyền, đừng làm mờ — trừ khi có lý do rõ ràng.
- ✅ Lớp nổi ở `collapsed` phải hiện cả khi focus.
- ❌ Không quá hai cấp menu.
- ❌ Không dùng `<div>` làm mục điều hướng.
- ❌ Không animate `width`.
- ❌ Không để `Sidebar` tự gọi API.
- ❌ Không dùng `--color-brand-subtle` làm tín hiệu duy nhất.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có ô tìm kiếm trong menu khi cây lớn không? Nó cứu được cây dài nhưng cũng là dấu hiệu cây đang quá dài | Sau F3 — dự án hạ nguồn đầu tiên có trên vài chục mục |
| 2 | Bậc `sm` của `Drawer` (`--layout-drawer-w-sm`) có quá rộng cho một cây menu ở khoảng `$bp-sm` … `$bp-md` không? Bậc hẹp hơn là một bậc mới của thang `--layout-drawer-w-*` — không tự đẻ | Người dùng, khi có màn thật để nhìn |
