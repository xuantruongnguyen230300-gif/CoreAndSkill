---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Topbar

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Bản tối thiểu đã có ở `src/FE` (F2, ADR-0037) — khu tài khoản đã là một nút mở `Menu` nổi (FE-1, 2026-10-08, hàng "Khu tài khoản"); mục "Ngôn ngữ" và hamburger chưa dựng; bảng "đã có → còn thiếu" ngay dưới đây.

**Nền:** tự dựng. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là một dải bố cục thuần; hành vi duy nhất đáng kể là menu người dùng, và menu đó là một lớp nổi tách riêng chứ không phải một phần của thanh.

## Đã có → còn thiếu (F2, ADR-0037)

Component: `src/FE/src/app/shared/components/topbar/topbar.component.ts`.

| Khoản | Đã có | Còn thiếu |
| --- | --- | --- |
| API | Khớp §API dự kiến (đọc mã 2026-10-08): `user`, `tenantName`, `menuItems`, `sidebarMode`, `drawerOpen`, `state`, `languages`, `currentLanguage`, `pendingLanguage`, `theme`, `scrolled`; output `menuToggled`, `menuItemSelected`, `languageChangeRequested`, `themeChangeRequested`. Không còn `title` | — |
| Tiêu đề tuyến | **Đã gỡ** (đọc mã 2026-10-08, FE-1): `topbar.component.ts` không còn input `title`; `topbar.component.html` không còn `.topbar__tieu-de`; ca ``không có input `title` — Topbar không mang tiêu đề trang`` (`topbar.component.spec.ts`). `shell.component.html` thôi truyền `[title]` | Giữ nguyên — tiêu đề trang là `<h1>` của `PageHeader` |
| Bóng khi `main` đã cuộn — input `scrolled` | Có (đọc mã 2026-10-08, FE-1): `topbar.component.ts` chuỗi `readonly scrolled = input(false);`; `topbar.component.html` gắn `[class.topbar--da-cuon]="scrolled()"`; `topbar.component.scss` § `.topbar--da-cuon` khai `box-shadow: var(--shadow-2)`, chuyển tiếp `--dur-fast`/`--ease-standard`. `platform/shell` truyền `[scrolled]="daCuon()"`, `daCuon` đặt từ `(scroll)` của `main` (`shell.component.ts` chuỗi `scrollTop > 0`). Ca `` `scrolled` bật lớp bóng `` (`topbar.component.spec.ts`) và ``truyền `scrolled` cho Topbar theo vùng cuộn thật của main`` (`shell.component.spec.ts`) | Giữ nguyên. Người soạn chưa chạy test; hình thức thật chưa đo |
| Khu tài khoản | Có (đọc mã 2026-10-08, FE-1): `topbar.component.html` có **một** `<button class="topbar__tai-khoan"` mang `aria-haspopup="menu"`, `[attr.aria-expanded]`, `aria-label` theo khoá `khung.taiKhoan.moMenu` (kèm họ tên + tên đơn vị) và bên trong avatar + tên + tên đơn vị; mở `<app-menu variant="anchored"` với `menuItems`. `platform/shell` truyền hai mục (`shell.component.ts` chuỗi `taiKhoanMenuItems`: `ho-so` rồi `dang-xuat` với `danger: true`) — `Menu` tự chèn vạch ngăn trước nhóm `danger`. Ca `TopbarComponent — khu tài khoản` trong `topbar.component.spec.ts`: `đúng MỘT nút: avatar + tên + tên đơn vị; không còn nút mục inline`, `aria-haspopup="menu", aria-expanded phản ánh menu…`, `Escape đóng menu và trả focus về nút`, `chọn mục → phát menuItemSelected với key của mục` (người soạn chưa chạy test) | Dưới `$bp-xs` mục "Giao diện" vào menu tài khoản (`topbar.component.ts` § `mucTaiKhoanXs`, § `truyVanMedia(TRUY_VAN_DUOI_BP_XS)` — hằng từ `src/FE/src/app/core/theme/diem-ngat.ts`, đọc mã 2026-10-08) — chưa có ca kiểm riêng. Vạch ngăn do `Menu` sinh, chưa có ca ở `Topbar` ép nó. Hình thức thật trên trình duyệt **chưa đo** |
| Mục "Ngôn ngữ" | — | Chưa dựng — `Topbar` chưa tự thêm mục này; v1 `CORE_I18N.languages` một mục nên chưa có màn nào cần thử |
| Nút theme + Menu theme | ✅ Có (đã đọc mã, đối chiếu 2026-10-06; lượt FE-C2, Q61): `topbar.component.html` chuỗi `haspopup="menu"` và `[expanded]="menuThemeMo()"` trên `<app-icon-button`, đứng trước `topbar__tai-khoan`; `<app-menu` ba mục `light`/`dark`/`system` icon `sun`/`moon`/`desktop`, `[selectedKey]="theme()"`; nhãn `khung.theme.nhan`. `shell.component.ts` chuỗi `khiDoiTheme` gọi `themeService.setPreference` rồi đẩy câu `khung.theme.daChuyen*` vào `<div class="sr-only" role="status" aria-live="polite">` (`shell.component.html`). Test `topbar.component.spec.ts` (`describe('TopbarComponent — nút theme'`: aria-haspopup/aria-expanded/không aria-pressed, ba mục + selectedKey, phát khi chọn khác, không phát khi chọn lại) chạy xanh 2026-10-06 | **Chưa đo trên trình duyệt thật:** focus trả về nút khi đóng menu bằng `Escape`/chọn mục; nền giữ khi menu đang mở; không nháy trắng khi tải lại. `shell.component.spec.ts` nay có khối `describe('ShellComponent — đổi giao diện (khiDoiTheme)'` ("… → ThemeService.setPreference(…) và vùng status đọc đúng câu"). Vùng `aria-live` hiện chỉ phục vụ theme, sẽ dùng chung với ngôn ngữ khi dựng ngôn ngữ |
| Hamburger / nút thu-gọn `Sidebar` | — | Chưa render — gắn với `sidebarMode` khác `'expanded'`, cũng hoãn (xem Sidebar.md) |
| `state: 'error'` ở khu tài khoản | Ẩn tên người dùng/đơn vị, giữ avatar mặc định + `menuItems` (đăng xuất còn bấm được) | — |
| `aria-live` khi đổi theme/ngôn ngữ | Theme: có, xem hàng nút theme. | Ngôn ngữ: chưa render, câu `khung.ngonNgu.daDoi` chưa dùng — chờ mục "Ngôn ngữ" |
| Avatar | Vẽ **nội tuyến** trong template: `src/FE/src/app/shared/components/topbar/topbar.component.html` chuỗi `<span class="topbar__avatar" aria-hidden="true">{{ chuCaiDau() }}</span>` — chữ cái đầu của tên, không phải component [`Avatar.md`](./Avatar.md) (nhãn 📐, chưa dựng; `shared/components` chưa có thư mục `avatar`). Đối chiếu 2026-10-06 | Khi dựng `Avatar`, thay đoạn nội tuyến này bằng component đó — một component, một định nghĩa ([`../COMPONENTS.md`](../COMPONENTS.md)) |

---

## Mục đích

Thanh đầu khung cố định: mở điều hướng ở màn nhỏ và chứa các thao tác cấp tài khoản. Nó **không** mang tiêu đề trang (chốt 2026-10-08).

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Khung ứng dụng chính, mọi màn sau khi đăng nhập | 🛑 Màn xác thực → [`AuthCard.md`](./AuthCard.md), khung đó cố ý không có `Topbar` |
| Chứa hành động **cấp tài khoản**: hồ sơ, đổi mật khẩu, đăng xuất, đổi theme, đổi ngôn ngữ | 🛑 Hành động **của trang hiện tại** ("Thêm người dùng") → [`Toolbar.md`](./Toolbar.md) ở màn danh sách, [`PageHeader.md`](./PageHeader.md) ở màn khác. Đây là nhầm lẫn hay gặp nhất và nó làm `Topbar` phình theo từng màn |
| Nút mở điều hướng (hamburger, nút thu/mở `Sidebar`) | 🛑 Hành động **trên một danh sách** (lọc, tìm) → [`Toolbar.md`](./Toolbar.md) |
| | 🛑 Điều hướng chính → [`Sidebar.md`](./Sidebar.md), trừ khi ứng dụng chỉ có vài mục; 🛑 tiêu đề trang, đường dẫn phân cấp → [`PageHeader.md`](./PageHeader.md) — `Topbar` không giữ tiêu đề nào |

**Ranh giới `Topbar` ↔ `PageHeader` là ranh giới quan trọng nhất của component này.** Phép thử: *thao tác này có ý nghĩa ở mọi màn không?* Có → `Topbar`. Chỉ ở màn này → vùng của màn (`PageHeader`, hoặc `Toolbar` ở màn danh sách). Bỏ qua phép thử này thì `Topbar` dần biến thành nơi chứa mọi nút mà không màn nào nhận, và cuối cùng nó phải biết mình đang ở màn nào — tức là hết dumb.

## Biến thể

| Biến thể | Khác gì | Dùng khi |
| --- | --- | --- |
| `default` | Nút mở điều hướng (theo `sidebarMode`) + nút theme + khu tài khoản | Mọi màn trong khung ứng dụng |
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
| Bóng khi `main` đã cuộn | `--shadow-2` |
| Cỡ chữ tên người dùng | `--fs-sm`, `--fw-medium`, `--color-text` |
| Cỡ chữ tên đơn vị | `--fs-xs`, `--fw-regular`, `--color-text-muted` |
| Lớp | `--z-topbar` |

**Chiều cao cố định, không co giãn.** `Topbar` đứng yên ở đỉnh khung còn `main` cuộn bên dưới nó ([`../Screens/00-khung-ung-dung.md`](../Screens/00-khung-ung-dung.md)), nên chiều cao của nó bị trừ khỏi mọi tính toán chiều cao khác trên trang. Một thanh cao thấp thất thường làm mọi vùng cuộn bên dưới tính sai.

**Bóng chỉ xuất hiện khi `main` đã cuộn** — input `scrolled`. Ở đỉnh `main` thì viền dưới là đủ; có bóng lúc chưa cuộn trông như một lớp nổi vô cớ. Bóng xuất hiện là tín hiệu "có nội dung đang trôi bên dưới". Thân bảng cuộn bên trong khung `DataTable` **không** tính — nội dung đó trôi dưới hàng tiêu đề cột, không trôi dưới `Topbar`.

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
| Chữ | `--fs-xs`, `--fs-sm`, `--fw-regular`, `--fw-medium`, `--lh-snug` |
| Khoảng cách | `--sp-4`, `--sp-5`, `--sp-6` |
| Hình dạng | `--radius-md`, `--border-w` |
| Kích thước | `--layout-topbar-h`, `--size-control-md`, `--icon-md`, `--icon-lg` |
| Bóng | `--shadow-2`, `--shadow-3` |
| Lớp | `--z-topbar`, `--z-popover` |
| Chuyển động | `--dur-fast`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Không có hamburger; nút theme và khu tài khoản ghim phải, khu tài khoản với tên hiển thị đầy đủ và **tên đơn vị ở dòng dưới**; phần trái của thanh để trống |
| `$bp-md` … `$bp-lg` | Có nút thu/mở `Sidebar` bên trái; tên người dùng vẫn hiện; **ẩn dòng tên đơn vị** |
| < `$bp-md` | Có hamburger mở drawer bên trái; **ẩn tên người dùng**, chỉ còn [`Avatar.md`](./Avatar.md) |
| < `$bp-xs` | Nút theme dồn vào menu người dùng thành một mục mở cùng `Menu` theme, nối tiếp như mục "Ngôn ngữ". Đổi ngôn ngữ vốn nằm trong menu đó ở mọi ngưỡng. *Hiện trạng (đọc mã 2026-10-08, chưa đo dưới `$bp-xs` trên trình duyệt):* `topbar.component.scss` § `@include s.duoi-man-hinh(s.$bp-xs)` ẩn `.topbar__theme`. Mục "Giao diện" nằm **trong** lớp nổi của menu tài khoản: `topbar.component.html` chuỗi `duoiXs() ? mucTaiKhoanXs(nhanTheme, iconTheme(), menuItems()) : menuItems()`, `duoiXs` đọc § `TRUY_VAN_DUOI_BP_XS` của `src/FE/src/app/core/theme/diem-ngat.ts`; chọn mục đó thì menu tài khoản đóng và `Menu` theme mở neo vào cùng nút (`topbar.component.ts` § `chonMucTaiKhoan`). Chưa có ca kiểm cho ngưỡng này |

**Thứ tự hy sinh khi hết chỗ, từ bỏ trước tới bỏ sau:** thao tác phụ → tên đơn vị → tên người dùng. Hamburger và avatar **không bao giờ** bị bỏ: một cái là đường duy nhất tới điều hướng, cái kia là đường duy nhất tới đăng xuất.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | `<header>` với `role="banner"` |
| Hamburger | `<button>` với `aria-label`, `aria-expanded` phản ánh trạng thái drawer, và `aria-controls` trỏ tới id của drawer |
| Tiêu đề trang | `Topbar` **không** mang tiêu đề nào. `<h1>` duy nhất của trang thuộc [`PageHeader.md`](./PageHeader.md) — xem dưới |
| Tên đơn vị | Là chữ thuần, không phải heading và không bấm được. Nó bị ẩn về mặt thị giác từ dưới `$bp-lg`, nên nhãn đọc lên của nút mở menu người dùng phải ghép **cả tên người dùng lẫn tên đơn vị** — không ghép thì người dùng trình đọc màn hình mất thông tin này ở đúng những ngưỡng hẹp |
| Menu người dùng | Nút mở có `aria-haspopup="menu"` và `aria-expanded`. Menu có `role="menu"`, mỗi mục `role="menuitem"`; mũi tên di chuyển, Escape đóng, focus trả về nút mở |
| Mục "Ngôn ngữ" | `role="menuitem"` với `aria-haspopup="menu"`. Chọn mục thì danh sách ngôn ngữ mở và focus vào ngôn ngữ đang dùng; `Escape` đóng danh sách và trả focus về nút mở menu người dùng |
| Nút theme | `aria-label` nói rõ đang mở lựa chọn giao diện, `aria-haspopup="menu"`, `aria-expanded`. `Menu` mở ra có ba mục `role="menuitemradio"`, mục đang áp mang `aria-checked="true"` và nhận focus khi mở — [`Menu.md`](./Menu.md) §Accessibility |
| Bàn phím | Mọi control trong thanh nằm trong thứ tự Tab tự nhiên |
| Focus | `outline` + `outline-offset` ≥ 2px |
| Liên kết bỏ qua | Liên kết "Bỏ qua tới nội dung chính" đứng **trước** `Topbar` trong DOM |
| Đổi theme / ngôn ngữ | Đổi xong phải thông báo qua vùng `aria-live` — thay đổi thuần hình ảnh thì người dùng trình đọc màn hình không biết gì |
| Chữ | Qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

**Vì sao `Topbar` không mang tiêu đề trang (chốt 2026-10-08, người dùng).** Một dòng tiêu đề trên `Topbar` chỉ lặp lại `<h1>` của `PageHeader`; lý lẽ duy nhất cho nó — "`Topbar` đứng yên nên tiêu đề vẫn thấy khi trang đã cuộn xa" — không đứng được:

- Ở màn danh sách — loại màn chính của Core — `main` không cuộn và `PageHeader` không trôi khỏi màn, trừ đường lui khi màn quá thấp ([`../Templates/ListScreen.md`](../Templates/ListScreen.md) §4). Hai dòng cùng chữ nằm cách nhau vài chục pixel chỉ là nhiễu.
- Ở màn cuộn được, "đang ở đâu" vẫn có hai kênh luôn nhìn thấy: mục đang chọn của [`Sidebar.md`](./Sidebar.md) (ba kênh hình ảnh và `aria-current="page"`) và tiêu đề tài liệu `<title>` trên tab ([`PageHeader.md`](./PageHeader.md) §Accessibility, dòng "Đổi tiêu đề khi chuyển tuyến").
- Bỏ tiêu đề cũng làm `Topbar` thôi phụ thuộc vào tuyến hiện tại — một input ít hơn để `platform/shell` phải dựng.

Cái giá: ở màn cuộn dài, người dùng nhìn vào `Topbar` không thấy tên trang; họ đọc nó ở `Sidebar` hoặc trên tab.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `user` | input | `{ fullName: string; userName: string; email: string \| null } \| null` | `null` | `null` = đang tải hoặc lỗi. `platform/shell` lấy từ phản hồi phiên ([`../../contracts/auth.md`](../../contracts/auth.md) §5), giữ đúng tên trường. Phản hồi không mang ảnh đại diện, nên [`Avatar.md`](./Avatar.md) nhận `fullName` và vẽ chữ cái đầu |
| `tenantName` | input | `string` | `''` | Tên đơn vị của phiên. `platform/shell` lấy từ phản hồi phiên ([`../../contracts/auth.md`](../../contracts/auth.md) §5), giữ đúng tên trường; field này **chỉ để hiển thị**. Chuỗi rỗng — chưa có phiên, hoặc hồ sơ lỗi — thì **không vẽ dòng** và không chừa chỗ trống |
| `menuItems` | input | `ReadonlyArray<UiMenuItem>` | `[]` | Mục trong menu người dùng. Chữ ký đầy đủ ở [`../../quy-uoc/fe-ui-conventions.md`](../../quy-uoc/fe-ui-conventions.md) §9 — cùng kiểu với `items` của [`Menu.md`](./Menu.md), không có kiểu riêng cho `Topbar`. Trường `icon` là **tên trần**, không tiền tố ([`../Icons.md`](../Icons.md) §5): cùng một kiểu đi vào hai component thì phải đọc được theo cùng một nghĩa, nếu không thì chuyển một mục từ `Menu` sang đây sẽ ra `pi-pi-…` và không vẽ gì. `danger: true` cho đăng xuất |
| `sidebarMode` | input | `'expanded' \| 'collapsed' \| 'drawer'` | `'expanded'` | Quyết định hiện hamburger hay nút thu/mở |
| `drawerOpen` | input | `boolean` | `false` | Để đặt `aria-expanded` |
| `scrolled` | input | `boolean` | `false` | `main` đã cuộn khỏi đỉnh — bật `--shadow-2` (§Kích thước). `platform/shell` nghe sự kiện cuộn của `main` và truyền xuống; `Topbar` không tự đọc vị trí cuộn của phần tử nào |
| `state` | input | `'idle' \| 'loading' \| 'error'` | `'idle'` | Chỉ nói về khu tài khoản |
| `languages` | input | `ReadonlyArray<LanguageOption>` | `[]` | Truyền thẳng xuống [`LanguageSwitcher.md`](./LanguageSwitcher.md). Dưới hai mục thì menu người dùng **không có** mục "Ngôn ngữ" |
| `currentLanguage` · `pendingLanguage` | input | `string \| null` | `null` | Truyền thẳng xuống `LanguageSwitcher`, cùng nghĩa như ở đó |
| `theme` | input | `'light' \| 'dark' \| 'system'` | `'light'` | Giá trị đang áp, quyết icon của nút theme và là `selectedKey` của `Menu` theme. `platform/shell` đọc từ lựa chọn đã lưu ([`../DESIGN.md`](../DESIGN.md) §8) |
| `menuToggled` | output | `void` | — | Bấm hamburger hoặc nút thu/mở |
| `menuItemSelected` | output | `string` | — | `key` của mục trong menu người dùng. Không phát cho mục "Ngôn ngữ" |
| `languageChangeRequested` | output | `string` | — | Mã ngôn ngữ người dùng chọn trong danh sách mở từ mục "Ngôn ngữ" |
| `themeChangeRequested` | output | `'light' \| 'dark' \| 'system'` | — | Giá trị người dùng **chọn** trong `Menu` theme; không phát khi chọn lại giá trị đang áp. `platform/shell` áp và lưu, rồi truyền `theme` mới xuống |

**`Topbar` không tự biết người đang đăng nhập là ai, không tự biết `main` đã cuộn chưa, và không tự gọi API lấy hồ sơ.** Nó nhận mọi thứ qua `input()` từ `platform/shell` — tầng smart inject phiên và menu — nên là component **dumb** theo [`../COMPONENTS.md`](../COMPONENTS.md) §5. Nó cũng không cần biết tuyến hiện tại: không có tiêu đề nào để hiện.

**Menu người dùng nhận danh sách mục qua `input()` chứ không khai cứng bên trong.** Khai cứng nghĩa là `Topbar` phải biết ứng dụng có tính năng đổi mật khẩu hay không — một mẩu nghiệp vụ lọt vào component Core. Nó cũng làm mục menu không thể thay đổi theo quyền.

**Mục "Ngôn ngữ" là ngoại lệ: `Topbar` tự thêm nó**, không nhận qua `menuItems` — cùng cách với mục theme khi nút theme dồn vào menu dưới `$bp-xs`. Đổi ngôn ngữ là thao tác của Core chứ không phải mẩu nghiệp vụ, và chọn mục này không phát `menuItemSelected` mà mở một lớp nổi khác — nên `Topbar` phải biết nó. Mục có mặt khi `languages` có từ hai mục, icon `pi-globe` ([`../Icons.md`](../Icons.md) §5), đứng sau các mục thường và trước các mục `danger`. Chọn mục → menu người dùng đóng, rồi [`LanguageSwitcher.md`](./LanguageSwitcher.md) biến thể `anchored` mở, neo vào **cùng** nút mở menu người dùng. Hai lớp nổi nối tiếp nhau, không lồng nhau ([`../COMPONENTS.md`](../COMPONENTS.md) §2.4). Người dùng chọn một ngôn ngữ → `Topbar` phát `languageChangeRequested`; `platform/shell` tải gói dịch rồi truyền `currentLanguage` mới xuống — vòng đi của [`LanguageSwitcher.md`](./LanguageSwitcher.md) §API.

**Nút theme là một [`IconButton.md`](./IconButton.md) `ghost` đặt ngay trên thanh** — [`../DESIGN.md`](../DESIGN.md) §8 đặt nó ở đây, không giấu trong menu (trừ khổ < `$bp-xs`, §Responsive). Icon hiện giá trị đang áp theo `pi-sun` · `pi-moon` · `pi-desktop` ([`../Icons.md`](../Icons.md) §5). Bấm nút mở một [`Menu.md`](./Menu.md) `anchored` ba mục `menuitemradio` sáng · tối · theo hệ điều hành — `Topbar` tự dựng ba mục này như mục "Ngôn ngữ", không nhận qua `menuItems`; `selectedKey` là `theme`. Chọn một mục → menu đóng, `Topbar` phát `themeChangeRequested`; áp và lưu là việc của `platform/shell`.

## Nút theme — đặc tả thi công

Quyết định 2026-10-06 (Q61): dựng ngay — **đã dựng** ở lượt FE-C2 cùng ngày (bảng "Đã có → còn thiếu" ở đầu file). Điều kiện của [`../DESIGN.md`](../DESIGN.md) §8 — mặc định sáng chỉ chấp nhận được khi người dùng đổi được ngay trên `Topbar` — phải thành sự thật. Các khoản dưới đây bổ sung chỗ spec còn thiếu cho người dựng; phần còn lại đã nằm ở §API dự kiến, §Accessibility, §Responsive.

| Khoản | Quy định |
| --- | --- |
| Phạm vi | Chỉ khung ứng dụng. Khung xác thực **không có** nút — [`../Screens/01-dang-nhap.md`](../Screens/01-dang-nhap.md) |
| Vị trí | Trong `Topbar`, **bên phải, đứng liền trước khu tài khoản** (thứ tự trái → phải: nút mở điều hướng nếu có · khoảng co giãn · nút theme · khu tài khoản). Khe `--sp-4` |
| Component | [`IconButton.md`](./IconButton.md) `variant="ghost"`, `size="md"`; `type="button"` |
| Icon | Hiện giá trị **đang áp**: `light` → `pi-sun`, `dark` → `pi-moon`, `system` → `pi-desktop` ([`../Icons.md`](../Icons.md) §5). Truyền **tên trần** `sun` · `moon` · `desktop` cho `icon` của `IconButton`. Ba mục của `Menu` mang cùng ba icon đó |
| Menu | [`Menu.md`](./Menu.md) `variant="anchored"`, `size="md"`, neo vào nút, `selectedKey` = `theme`. Ba mục theo thứ tự **Sáng · Tối · Theo hệ điều hành**, `key` lần lượt `light` · `dark` · `system`. `ariaLabel` của `Menu` dùng chung khoá với nhãn nút |
| Nhãn / khoá i18n | Nhãn nút và `ariaLabel` của `Menu`: `khung.theme.nhan` ("Giao diện"). Ba mục: `khung.theme.sang` · `khung.theme.toi` · `khung.theme.theoHeThong`. Câu thông báo: `khung.theme.daChuyenSang` · `khung.theme.daChuyenToi` · `khung.theme.daChuyenTheoHeThong` — bảng đầy đủ ở [`../Screens/00-khung-ung-dung.md`](../Screens/00-khung-ung-dung.md) §Câu chữ |
| Lưu ở đâu | `localStorage`, khoá `theme`, giá trị `light` · `dark` · `system` — do `ThemeService.setPreference` ghi (`core/theme/theme.service.ts`). `Topbar` không đụng `localStorage`; `platform/shell` gọi service khi nhận `themeChangeRequested` |
| Giá trị ban đầu | Đọc từ `ThemeService.preference()` rồi truyền xuống `theme`. **Mặc định khi chưa từng chọn là `light`** — [`../DESIGN.md`](../DESIGN.md) §8, không theo hệ điều hành; `system` chỉ là một lựa chọn người dùng tự bấm |
| Áp trước khi trang vẽ | Việc của script nội tuyến ở `index.html` đọc cùng khoá `theme` trước khung hình đầu — **đã có**. Nút không áp lại lúc khởi tạo (tránh nháy trắng) |
| Chọn lại giá trị đang áp | Menu đóng, **không** phát `themeChangeRequested`, **không** thông báo `aria-live` |
| `hover` | Theo `ghost` của `IconButton`: nền `--color-surface-2`, icon `--color-text`; bọc `@media (hover: hover)` |
| `focus-visible` | Vòng focus theo `IconButton` (`--color-focus`, `outline-offset` ≥ 2px). Đóng menu bằng `Escape` hoặc chọn mục → focus trả về nút |
| `active` / menu đang mở | Giữ nền như `hover` cho tới khi menu đóng — người dùng thấy nút nào đang giữ menu |
| `disabled` / `loading` | Không áp dụng — việc đổi theme không chờ dữ liệu nào, không có điều kiện chặn |
| Trợ năng | Nút là **nút mở menu**: `aria-haspopup="menu"` + `aria-expanded`, **không** `aria-pressed` (`pressed` của `IconButton` để `null`). Giá trị đang áp được đọc qua mục `aria-checked="true"` trong menu, không qua nhãn nút. Sau khi áp, `platform/shell` đẩy câu thông báo vào **một** vùng `role="status"` / `aria-live="polite"` ẩn thị giác, đặt trong khung; vùng này dùng chung với thông báo đổi ngôn ngữ |
| Dưới `$bp-xs` | Nút ẩn; menu tài khoản có thêm mục "Giao diện" (`khung.theme.nhan`, icon theo giá trị đang áp) mở `Menu` theme nối tiếp — §Responsive |

**Chỗ spec `IconButton` từng thiếu cho ca này:** hai input `haspopup`/`expanded` đã bổ sung vào [`IconButton.md`](./IconButton.md) §API dự kiến và đã có trong code. Không bọc thêm phần tử ngoài để đặt thuộc tính hộ.

## Do / Don't

- ✅ Chỉ đặt hành động **cấp tài khoản** vào `Topbar`.
- ✅ Chiều cao cố định.
- ✅ Bóng chỉ khi `main` đã cuộn.
- ✅ Giữ hamburger và avatar ở mọi khổ màn.
- ✅ Thông báo qua `aria-live` khi đổi theme hoặc ngôn ngữ.
- ❌ Không đặt tiêu đề trang — `<h1>` hay một dòng chữ thường — trong `Topbar`.
- ❌ Không đặt hành động của trang vào `Topbar`.
- ❌ Không để `Topbar` tự đọc router hay tự gọi API.
- ❌ Không để lỗi tải hồ sơ chặn cả thanh.
- ❌ Không khai cứng danh sách mục menu người dùng.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Có ô tìm kiếm toàn cục trên `Topbar` không? Nó rất hữu ích nhưng cần một hạ tầng tìm kiếm xuyên module chưa được thiết kế | Sau F3 — dự án hạ nguồn đầu tiên có nhiều module |
