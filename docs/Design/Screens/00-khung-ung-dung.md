---
kind: luat
scope: core
verified: 2026-09-17
---

# Khung ứng dụng — màn hình

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Bản tối thiểu đã có ở `src/FE` (F2, [ADR-0037](../../adr/0037-f2-dung-du-component-core-khong-hoan-ngam.md)) — `platform/shell` dựng `Sidebar`/`Topbar`/`Footer` thật qua `input()`/`output()`, không còn markup tay; nhiều mảng của sơ đồ dưới đây (biến thể `collapsed`/`drawer`, `LanguageSwitcher`) **chưa** có trong code; nút theme và phiên bản chân trang đã có từ 2026-10-06; sau FE-1 (2026-10-08) khung đã có menu tài khoản dạng lớp nổi, `Footer` ở đáy `Sidebar`, `Topbar` không tiêu đề, bóng khi cuộn, `main` không trần bề rộng. Hộp hỏi thay đổi chưa lưu thì **đã có** — `<app-unsaved-changes-dialog />` (lớp `UnsavedChangesDialogComponent`) render một lần ở gốc app, xem dòng `unsavedChangesGuard` của bảng. Bảng "Có thật hôm nay → sẽ thành" ngay dưới đây.

Khung bọc mọi màn sau khi đăng nhập: `Sidebar` (chân dải là `Footer`), `Topbar` (nút theme + menu tài khoản, không tiêu đề trang), `main`; giữ hai lớp nổi dùng chung của `Topbar`: **menu tài khoản** và **menu theme**. Màn con ([02](./02-trang-loi.md), [03](./03-ho-so-ca-nhan.md), [04](./04-trang-chu.md), [10](./10-nguoi-dung.md), [11](./11-vai-tro.md), [12](./12-ma-tran-phan-quyen.md), [20](./20-don-vi.md)) chỉ vẽ phần trong `main`. Nghiệp vụ lấy từ các nguồn trích trong file — không chép lại.

> **Khung:** chính là thứ file này mô tả. Tầng smart `platform/shell` inject phiên và menu, truyền xuống `Sidebar`/`Topbar` qua `input()` ([fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.3).
> **Quyền:** không cần permission. Mọi tuyến qua `authGuard` và `mustChangePasswordGuard` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §2.1, §4). Cây menu do máy chủ lọc theo quyền ([meta-menu.md](../../contracts/meta-menu.md) §1.4).

## Có thật hôm nay → sẽ thành (F2, ADR-0037)

Đối chiếu 2026-09-17 với `src/FE/src/app/platform/shell/shell.component.html`, `shell.component.ts`, `shell.component.scss`, `src/FE/src/app/core/guards/unsaved-changes.guard.ts`, `src/FE/src/app/platform/ho-so/ho-so.routes.ts`. Neo chuyển sang chuỗi tìm được (ADR-0046) và kiểm lại chuỗi ngày 2026-09-22, kèm `src/FE/src/app/app.html`.

| Khoản | Có thật hôm nay | Sẽ thành |
| --- | --- | --- |
| `Sidebar`/`Topbar` là component dumb riêng | Có — `<app-sidebar>`/`<app-topbar>` nhận dữ liệu qua `input()`, phát `output()`; không còn markup tay trong `shell.component.html` (`shell.component.html`, chuỗi `<app-sidebar` và `<app-topbar`) | — (đủ, đúng `fe-architecture.md` §2.3) |
| Liên kết "Bỏ qua tới nội dung chính" | Có, đúng vị trí — trước `Sidebar`/`Topbar` trong DOM (`shell.component.html`, chuỗi `class="bo-qua"`) | — (đủ) |
| Biến thể `Sidebar`/`sidebarMode` của `Topbar` | Cố định `'expanded'`/`'default'` — `shell.component.html` không truyền `mode`/`sidebarMode`, không có logic đọc breakpoint (`shell.component.ts` không có `modeChange`/`drawerOpen`) | `collapsed`, `drawer`, hamburger, ghi nhớ `localStorage` — xem [Sidebar.md](../Components/Sidebar.md) §"Đã có → còn thiếu", [Topbar.md](../Components/Topbar.md) §"Đã có → còn thiếu" |
| Khu tài khoản trên `Topbar` | **Có** (đọc mã 2026-10-08): MỘT nút (avatar + tên + tên đơn vị) mở menu nổi — `topbar.component.html` chuỗi `class="topbar__tai-khoan"` và `aria-haspopup="menu"`; `shell.component.ts` chuỗi `taiKhoanMenuItems` gồm Hồ sơ rồi Đăng xuất (`danger: true`). Chi tiết và ca kiểm: [Topbar.md](../Components/Topbar.md) §"Đã có → còn thiếu" | Giữ nguyên |
| Nút theme + Menu theme, `aria-live` đổi theme (Q61) | ✅ Có (đã đọc mã, đối chiếu 2026-10-06; FE-C2) — `shell.component.html` chuỗi `[theme]="theme()"`, `(themeChangeRequested)="khiDoiTheme($event)"` và `<div class="sr-only" role="status" aria-live="polite">`; `shell.component.ts` chuỗi `khiDoiTheme` → `themeService.setPreference` rồi đặt câu `khung.theme.daChuyen*`. Bảy khoá `khung.theme.*` có ở `public/i18n/vi.json`. Test của `Topbar` xanh; `shell.component.spec.ts` có khối `describe('ShellComponent — đổi giao diện (khiDoiTheme)'` (đọc 2026-10-08, người soạn chưa chạy) | Chưa đo trên trình duyệt thật: không nháy trắng khi tải lại, nút ẩn và mục thay thế hiện dưới 480px, focus trả về nút. Dưới `$bp-xs` mục "Giao diện" vào menu tài khoản đã có trong mã (`topbar.component.ts` chuỗi `mucTaiKhoanXs`), chưa có ca kiểm riêng |
| Mục "Ngôn ngữ", `aria-live` đổi ngôn ngữ | Chưa dựng — `shell.component.html` không truyền `languages`/`currentLanguage` xuống `Topbar` | Cần `LanguageSwitcher.md` cho mục "Ngôn ngữ" (v1 `CORE_I18N.languages` một mục nên chưa có màn nào ép phải thử) — [Topbar.md](../Components/Topbar.md) |
| Phiên bản ở `Footer` (Q59) | ✅ Có (đã đọc mã, đối chiếu 2026-10-06) — `shell.component.html` chuỗi `chung.chanTrang.phienBan`, `shell.component.ts` chuỗi `maCommitNgan()`; hai trang xác thực cũng truyền. Chi tiết ở [Footer.md](../Components/Footer.md) §"Đã có → còn thiếu" | Mã commit thật chỉ có sau CI build (job FE chưa từng chạy); máy dev ẩn phần này |
| Đăng xuất | Có — `POST` qua `AuthService.dangXuat()` rồi điều hướng về đăng nhập (`shell.component.ts`, chuỗi `private async dangXuat()`) | — (đủ ở mức luồng; chi tiết "lấy lại token chống giả mạo" nằm trong `AuthService`, ngoài hai file đã đối chiếu) |
| "Còn thay đổi chưa lưu ở màn con — hỏi TRƯỚC khi gửi request" (`unsavedChangesGuard`) | Có — guard đọc `UnsavedChangesService.xinRoiTrang()` (`src/FE/src/app/core/guards/unsaved-changes.guard.ts`, chuỗi `export const unsavedChangesGuard` rồi `thayDoi.xinRoiTrang()`; phiên đã dọn thì cho rời, không hỏi lại — chuỗi `if (!auth.daDangNhap())`, so 2026-09-26); gắn `canDeactivate: [unsavedChangesGuard]` trên tuyến `/ho-so` (`src/FE/src/app/platform/ho-so/ho-so.routes.ts`, chuỗi `canDeactivate: [unsavedChangesGuard]`); đăng xuất gọi thẳng cùng `xinRoiTrang()` vì không đi qua Router (`shell.component.ts`, chuỗi `private async dangXuat()`, ADR-0040); hộp hỏi dùng chung render một lần ở gốc app (`src/FE/src/app/app.html`, chuỗi `<app-unsaved-changes-dialog />`) | — (đủ) |
| `Footer` — biến thể, cỡ, vị trí (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08): `shell.component.html` đặt `<app-footer sidebarFooter variant="sidebar" size="sm"` trong `<app-sidebar>`; không còn `grid-area: chan-trang`. Chi tiết: [Footer.md](../Components/Footer.md), [Sidebar.md](../Components/Sidebar.md) §"Chân dải" | — |
| Khung cao đúng `100dvh`, `main` là vùng cuộn dọc (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08): `shell.component.scss` § `:host` khai `height: 100dvh` và `overflow: hidden` (không còn `min-height: 100dvh`); lưới hai hàng `var(--layout-topbar-h)` / `minmax(0, 1fr)`; `.noi-dung` khai `overflow-y: auto`, `min-height: 0`, cột flex. Màn danh sách gọi mixin `man-danh-sach` (`_spacing.scss`) để lấp chiều cao | Hình thức thật chưa đo trên trình duyệt |
| `main` không trần bề rộng (chốt 2026-10-08) | **Đã gỡ** (đọc mã 2026-10-08): `shell.component.scss` không còn chuỗi `max-width` hay `--layout-container-max`; `.noi-dung` chỉ còn đệm `--layout-page-pad` (`--layout-page-pad-sm` dưới `$bp-md`) | — |
| `Topbar` không tiêu đề trang (chốt 2026-10-08) | **Đã gỡ** (đọc mã 2026-10-08): `shell.component.html` không còn `[title]`; `Topbar` không còn input `title` — [Topbar.md](../Components/Topbar.md) | — |
| Bóng `Topbar` khi `main` đã cuộn — `scrolled` (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08): `shell.component.html` chuỗi `[scrolled]="daCuon()"` và `(scroll)="khiCuon($event)"` trên `<main>`; `shell.component.ts` chuỗi `scrollTop > 0`. Ca ``truyền `scrolled` cho Topbar theo vùng cuộn thật của main`` (`shell.component.spec.ts`; người soạn chưa chạy test) | Hình thức thật chưa đo |
| Mục "Đơn vị" ở `Sidebar` của đơn vị hệ thống (chốt 2026-10-08) | **Mã nguồn đã có** (đọc mã 2026-10-08): `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/CoreTenantSeedSource.cs` chuỗi `SystemTenantMenuItems` với mục `new("he-thong-don-vi", "menu.he-thong-don-vi", "pi-building", "/he-thong/don-vi", …, 100, …)`; `TenantProvisioningService.cs` ghép nó khi đơn vị vừa tạo mang `IsSystem` (chuỗi `CoreTenantSeedSource.SystemTenantMenuItems`); khoá dịch `menu.he-thong-don-vi` = `Đơn vị` ở `src/FE/public/i18n/vi.json`; tuyến `he-thong` ở `src/FE/src/app/app.routes.ts`. Script bù cho bản cài đã bootstrap: `database/bu-du-lieu/0001__core__he-thong-don-vi-menu.sql` | **Bản cài đã có cần người dùng chạy script bù** (điểm 5 của [ADR-0122](../../adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md)) — chưa biết đã chạy ở môi trường nào, nên mục có hiện ở `Sidebar` thật hay không **chưa xác nhận**. Người soạn chưa chạy test BE (`CoreTenantSeedMenuTests`) |
| Toast 5xx không envelope | Có (so 2026-09-30) — `core/interceptors/error.interceptor.ts` chuỗi `toast.loi(dichLoi(translate, body, err.status), body?.traceId ?? null);`, nhánh `body === null` của lớp xuyên suốt chọn câu `CORE.CLIENT.SERVER_UNAVAILABLE` qua `maLoiKhongEnvelope` ([`fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.2, luật F45), `traceId` là `null`; câu ở `vi.json` khớp bảng Câu chữ (vẫn *Chờ duyệt*) | — |

---

## Khung ứng dụng (mọi tuyến trong nhánh shell)

### Sơ đồ bố cục

```text
- body
  - Liên kết "Bỏ qua tới nội dung chính" — ẩn cho tới khi nhận focus, đứng TRƯỚC Sidebar và Topbar trong DOM
      (thuộc khung, không thuộc component nào — Sidebar.md §Accessibility)
  - khung — cao ĐÚNG 100dvh, trang không cuộn ở mức body; hai cột: Sidebar | cột phải (Topbar trên, main dưới)
  - Sidebar — cao trọn khung
      expanded  --layout-sidebar-w            ≥ $bp-lg
      collapsed --layout-sidebar-w-collapsed  $bp-md … $bp-lg
      drawer                                  < $bp-md, mở bằng hamburger của Topbar
    - <nav> cây menu dựng từ danh sách phẳng của GET /api/v1/core/meta/menu; nhãn = bản dịch của labelKey;
        cuộn riêng khi dài
      - activeRoute = tuyến hiện tại → mục đó mang aria-current="page"
      - CHỈ ở đơn vị hệ thống: mục CẤP MỘT "Đơn vị" (pi-building) → /he-thong/don-vi,
          đứng CUỐI cây — sau nhóm "Quản trị" (seed theo ADR-0122; bảng Quyết định bố cục)
    - chân dải (slot sidebarFooter, ngoài <nav>) — Footer `sidebar`, cỡ `sm`:
        bản quyền, dòng dưới là phiên bản; không vẽ ở collapsed; ở drawer nằm trong chân Drawer
  - Topbar `default` — cao --layout-topbar-h, đứng yên ở đỉnh cột phải; KHÔNG tiêu đề trang
    - hamburger (drawer) hoặc nút thu/mở Sidebar (collapsed) — theo sidebarMode; expanded thì bên trái để trống
    - khoảng co giãn
    - IconButton ghost — nút theme, icon hiện giá trị đang áp; bấm thì mở Menu theme (lớp nổi, xem dưới)
    - khu tài khoản: MỘT nút — Avatar (chữ cái đầu của fullName) + fullName, dòng dưới là tên đơn vị của phiên
        (input tenantName của Topbar — auth.md §5, chỉ để hiển thị; bố cục hai dòng và cách cắt chữ
         khai ở Topbar.md, màn không quyết lại) → mở menu tài khoản
    - bóng --shadow-2 khi main đã cuộn khỏi đỉnh (input scrolled)
  - main — đích của liên kết bỏ qua; vùng cuộn dọc của khung; KHÔNG trần bề rộng;
           đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm)
    - vùng màn con — màn con vẽ từ PageHeader trở xuống
        màn danh sách lấp đúng chiều cao main → main không cuộn, chỉ thân bảng cuộn (ListScreen.md §4)
        màn khác cuộn theo nội dung
- Menu tài khoản — lớp nổi của Topbar, neo vào khu tài khoản
  - Hồ sơ cá nhân → /ho-so
  - Ngôn ngữ (chỉ khi CORE_I18N.languages ≥ 2; v1 không có, chỗ giữ) — mục do Topbar tự thêm;
      chọn thì menu tài khoản đóng và LanguageSwitcher biến thể anchored mở,
      neo vào chính nút mở menu tài khoản (Topbar.md)
  - vạch ngăn
  - Đăng xuất (danger)
- Menu theme — lớp nổi của Topbar, neo vào nút theme, cùng khuôn mục Ngôn ngữ (Topbar.md)
  - ba mục `menuitemradio`: Sáng · Tối · Theo hệ điều hành (ba giá trị của DESIGN.md §8);
      mục đang áp mang aria-checked="true"; chọn thì menu đóng
```

Số đo: token [DESIGN.md](../DESIGN.md) §6.1 qua [Sidebar.md](../Components/Sidebar.md), [Topbar.md](../Components/Topbar.md), [Footer.md](../Components/Footer.md); màn không thêm số đo.

| Quyết định bố cục | Căn cứ |
| --- | --- |
| `Topbar` không có `loading`/`error` ở khu tài khoản — `user` không bao giờ `null` trong khung | [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.2 · [07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §2.1 |
| Mục "Ngôn ngữ" mở `LanguageSwitcher` `anchored`, không trải từng ngôn ngữ thành mục menu | [Topbar.md](../Components/Topbar.md) §API · [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §9 · [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Accessibility |
| Không có mục "Đổi mật khẩu" — đổi tự nguyện nằm trong màn hồ sơ | [03-ho-so-ca-nhan.md](./03-ho-so-ca-nhan.md) |
| Đăng xuất là mục menu gửi request, không phải liên kết | [D6](../../luong/D6-dang-xuat.md) §3 |
| Menu không khai cứng trong FE; khung **không** rẽ nhánh theo cờ `isSystemOperator` | [07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §6 · [auth.md](../../contracts/auth.md) §3 |
| **Đường vào khu hệ thống: đúng một mục `Sidebar`, chỉ ở đơn vị hệ thống.** Mục **cấp một** "Đơn vị", icon `pi-building`, trỏ `/he-thong/don-vi`, đứng **cuối** cây — sau nhóm "Quản trị". Mã mục `he-thong-don-vi`, khoá nhãn `menu.he-thong-don-vi` theo khuôn `menu.<mã>` của mọi mục seed. Mục đến từ dữ liệu menu như mọi mục khác — khung vẫn không rẽ nhánh theo cờ; `systemOperatorGuard` vẫn là chỗ chặn thật. Vì sao cấp một chứ không là con của "Quản trị": "Quản trị" là phạm vi **trong đơn vị mình**, còn màn đơn vị nhìn **xuyên đơn vị** — đặt chung nhóm là nói sai phạm vi; và tài khoản vận hành không có quyền nào nên mọi con khác của "Quản trị" đều ẩn, nhóm đó sẽ chỉ còn tồn tại để chứa mục này. Không đặt dưới một nhóm mới: [ADR-0122](../../adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md) điểm 1 chốt đúng **một** mục, nhóm mới là mục thứ hai. Vì sao đứng cuối: việc của người vận hành trong khu này hiếm, không nên chen trước các mục hằng ngày; `displayOrder` của mục lớn hơn mọi mục gốc trong nguồn seed của Core | người dùng 2026-10-08 · [ADR-0122](../../adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md) · [meta-menu.md](../../contracts/meta-menu.md) §1.2, §1.4 |
| Khung cao **đúng** `100dvh`; `Topbar` đứng yên; `main` là vùng cuộn dọc duy nhất ở mức khung; màn danh sách lấp đúng chiều cao `main` nên chỉ thân bảng cuộn | người dùng 2026-10-08 · [ListScreen.md](../Templates/ListScreen.md) §4 |
| `main` không có trần bề rộng | người dùng 2026-10-08 · [DESIGN.md](../DESIGN.md) §6.1 |
| `Footer` ở đáy `Sidebar`, không ở cuối `main` | người dùng 2026-10-08 · [Footer.md](../Components/Footer.md) §Khi nào dùng |
| `Topbar` không mang tiêu đề trang — tiêu đề chỉ ở `<h1>` của `PageHeader` màn con | người dùng 2026-10-08 · [Topbar.md](../Components/Topbar.md) §Accessibility |
| Không có hộp cảnh báo sớm hết phiên ở v1 — hết phiên xử lý khi gặp 401 bởi `SessionExpiryHandler`; khung không dựng lớp nổi nào cho việc này | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.5 · [07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §7.5, §10 · duyệt 2026-09-16 |

**Đổi ngôn ngữ khi đã đăng nhập.** `Topbar` phát `languageChangeRequested` mang mã ngôn ngữ ([Topbar.md](../Components/Topbar.md) §API); khung đặt `pendingLanguage`, rồi: tải gói dịch → áp → ghi `preferredLanguage` qua `PUT /api/v1/core/profile` ([profile.md](../../contracts/profile.md) §2 — request mang cả ba trường sửa được nên khung đọc hồ sơ hiện tại, §1, để gửi lại đúng họ tên và số điện thoại) → `PUT` xong gọi `lamMoiPhien()` để phiên mang `preferredLanguage` mới ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.3). Phải ghi hồ sơ vì ngôn ngữ mỗi lần đăng nhập áp theo `preferredLanguage` của phiên ([auth.md](../../contracts/auth.md) §5 · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §7 · [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §API).

**Đăng xuất.** Bốn bước của [D6](../../luong/D6-dang-xuat.md) §3: `POST /api/v1/core/auth/logout` → dọn trạng thái cục bộ → lấy lại token chống giả mạo → về màn đăng nhập. Không kèm `returnUrl` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8).

**Còn thay đổi chưa lưu ở màn con — hỏi TRƯỚC khi gửi request**, bằng đúng hộp của `unsavedChangesGuard` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §4.1); khung không dựng hộp thứ hai.

| Người dùng chọn | Khung làm gì |
| --- | --- |
| Ở lại | Đóng hộp. **Không** gửi request; phiên và thay đổi giữ nguyên |
| Rời đi | Gửi request, đi bốn bước trên. Câu trả lời dùng luôn cho lần điều hướng cuối — **không** hỏi lần hai |

### Câu chữ

Câu không ghi nguồn: người dùng duyệt 2026-09-15 ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n | Nguồn |
| --- | --- | --- | --- |
| Liên kết bỏ qua · `aria-label` `Sidebar` | Bỏ qua tới nội dung chính · Điều hướng chính | `khung.boQuaNoiDung` · `khung.dieuHuong.nhan` | [Sidebar.md](../Components/Sidebar.md) §Accessibility |
| Nhãn mục menu | Bản dịch của `labelKey` — khoá **đến từ dữ liệu** | `menu.*` | [meta-menu.md](../../contracts/meta-menu.md) §1.2 |
| `aria-label` hamburger · nút thu/mở `Sidebar` | Mở menu điều hướng ↔ Đóng menu điều hướng · Thu gọn điều hướng ↔ Mở rộng điều hướng | `khung.dieuHuong.moDrawer` · `khung.dieuHuong.dongDrawer` · `khung.dieuHuong.thuGon` · `khung.dieuHuong.moRong` | duyệt |
| Nhãn mục "Đơn vị" ở `Sidebar` — chỉ đơn vị hệ thống | Đơn vị — trùng tiêu đề màn `donVi.tieuDe` đã duyệt; nhãn do `design-expert` chốt theo uỷ quyền của người dùng 2026-10-08 | `menu.he-thong-don-vi` | [ADR-0122](../../adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md) điểm 6 |
| Tên đơn vị trên `Topbar` · tên từng ngôn ngữ | Dữ liệu, **không** qua khoá: `tenantName` của phiên · tên viết bằng chính ngôn ngữ đó | — | [auth.md](../../contracts/auth.md) §5 · [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Accessibility |
| `aria-label` nút theme (`aria-haspopup="menu"`); cũng là nhãn mục thay nó trong menu tài khoản dưới `$bp-xs` | Giao diện | `khung.theme.nhan` | duyệt |
| Ba mục `menuitemradio` Menu theme | Sáng · Tối · Theo hệ điều hành | `khung.theme.sang` · `khung.theme.toi` · `khung.theme.theoHeThong` | duyệt · [DESIGN.md](../DESIGN.md) §8 |
| Đọc lên qua `aria-live` sau khi đổi theme · đổi ngôn ngữ | Đã chuyển sang giao diện tối · Đã chuyển sang giao diện theo hệ điều hành · Đã chuyển sang giao diện sáng · Đã đổi ngôn ngữ giao diện | `khung.theme.daChuyenToi` · `khung.theme.daChuyenTheoHeThong` · `khung.theme.daChuyenSang` · `khung.ngonNgu.daDoi` | duyệt · [Topbar.md](../Components/Topbar.md) §Accessibility |
| `aria-label` nút mở menu tài khoản | Tài khoản {{hoTen}}, đơn vị {{tenDonVi}} — ghép **cả hai** vì dòng tên đơn vị ẩn từ dưới `$bp-lg` | `khung.taiKhoan.moMenu` | duyệt · [Topbar.md](../Components/Topbar.md) §Accessibility |
| Mục menu tài khoản | Hồ sơ cá nhân · Ngôn ngữ (chỉ khi `CORE_I18N.languages` ≥ 2; v1 không có, chỗ giữ) · Đăng xuất | `hoSo.tieuDe` · `chung.ngonNgu` · `xacThuc.dangXuat` | [profile.md](../../contracts/profile.md) tên card · [LanguageSwitcher.md](../Components/LanguageSwitcher.md) · [Icons.md](../Icons.md) §5 |
| `Sidebar` — tải menu hỏng · không có mục nào | Không tải được menu. · Tài khoản chưa được cấp chức năng nào. Liên hệ quản trị viên của đơn vị. | `khung.dieuHuong.loiTai` · `khung.dieuHuong.trong` | duyệt |
| Nút thử lại | Thử lại | `chung.thuLai` | [Sidebar.md](../Components/Sidebar.md) §Trạng thái |
| Bản quyền ở `Footer` | Như màn Đăng nhập | `chung.chanTrang.banQuyen` | [01-dang-nhap.md](./01-dang-nhap.md) |
| Phiên bản ở `Footer` (mã commit lúc build, 7 ký tự; vắng mã thì không vẽ) | Phiên bản {{ma}} | `chung.chanTrang.phienBan` | duyệt · [Footer.md](../Components/Footer.md) §Phiên bản hiển thị |
| Hộp cảnh báo sắp hết phiên | **Bỏ** — v1 không có hộp cảnh báo sớm; nhóm khoá `khung.hetPhien.*` không khai | — | [07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §10 · duyệt 2026-09-16 |
| Câu hỏi khi đăng xuất còn thay đổi chưa lưu | Câu chung của `unsavedChangesGuard` | khoá chung của guard — không đặt ở khung | [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §4.1 |
| Toast lỗi đăng xuất, lỗi ghi ngôn ngữ | Câu dịch theo mã; không có phản hồi (status 0) thì câu mất kết nối; 5xx không envelope → dòng dưới | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 |
| Toast 5xx không envelope (proxy 502/504), mọi request — lớp xuyên suốt, không `traceId` | *Chờ duyệt:* Máy chủ tạm thời không phản hồi, vui lòng thử lại sau. | `loi.CORE.CLIENT.SERVER_UNAVAILABLE` | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 |
| Toast phản hồi không hợp lệ — không mang envelope, status khác `0` và ngoài 5xx, mọi request | Máy chủ trả về phản hồi không hợp lệ (mã HTTP {{maHttp}}). Vui lòng thử lại; nếu vẫn lỗi, báo quản trị viên kèm mã này. | `loi.CORE.CLIENT.INVALID_RESPONSE` | duyệt 2026-09-30 · [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 · [be-cqrs-handler.md](../../quy-uoc/be-cqrs-handler.md) §7.4 |

### Trạng thái

Đường lỗi chung: [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2, §2.5 · [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8.

- **mặc định** — `Sidebar` có cây menu, mục của tuyến hiện tại `active`, chân dải có bản quyền và phiên bản; `Topbar` có nút theme và họ tên; `main` vẽ màn con.
- **đang tải** — `Sidebar` `loading` ([Sidebar.md](../Components/Sidebar.md) §Trạng thái); `Topbar`, `main` vẽ ngay, hamburger bấm được. Khu tài khoản không có ca tải; tải nội dung thuộc màn con.
- **rỗng** — menu rỗng → `Sidebar` `empty`; hồ sơ và đăng xuất vẫn ở `Topbar`.
- **lỗi**:
  - Tải menu hỏng → `Sidebar` `error` kèm Thử lại. Request menu mang `BO_QUA_TOAST_LOI`: lỗi khác không toast, riêng 5xx và 403 `CORE.AUTH.*` vẫn toast kèm `traceId` ([ADR-0094](../../adr/0094-lop-loi-xuyen-suot-luon-toast-ke-ca-khi-man-tat-toast.md)).
  - 401 → `SessionExpiryHandler`: dọn phiên, về đăng nhập kèm `returnUrl`, không toast.
  - 403 `CORE.AUTH.FORBIDDEN` → toast, làm mới tập quyền **và nạp lại menu cùng bước**; ở lại màn.
  - 403 `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` → interceptor **không toast, không điều hướng**: gọi `AuthService.lamMoiPhien()`, router chạy lại guard của URL hiện tại, `mustChangePasswordGuard` trả `UrlTree` sang màn đổi mật khẩu bắt buộc ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §5.4 · [auth.md](../../contracts/auth.md) §11).
  - 429 → toast kèm số giây chờ, trừ request đã tắt toast.
  - Đăng xuất nhận 200 hoặc 401 → đã đăng xuất ([auth.md](../../contracts/auth.md) §4). Hỏng kiểu khác → toast; **không** dọn trạng thái, ở lại màn, thử lại được ([D6](../../luong/D6-dang-xuat.md) §4).
  - Đổi ngôn ngữ: gói dịch hỏng → giữ ngôn ngữ cũ, toast ([LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Trạng thái). Ghi hồ sơ hỏng → toast; giữ ngôn ngữ vừa chọn cho phiên này.
- **kiểm tra dữ liệu** — không áp dụng: khung không có form.

### Responsive

Theo [Topbar.md](../Components/Topbar.md) §Responsive và [Sidebar.md](../Components/Sidebar.md).

| Ngưỡng | Cái gì đổi |
| --- | --- |
| ≥ `$bp-lg` | `Sidebar` `expanded`, chân dải hiện; `Topbar` không hamburger, họ tên đầy đủ |
| `$bp-md` … `$bp-lg` | `Sidebar` `collapsed`, nhãn mục hiện qua lớp nổi khi hover **và** focus, chân dải không vẽ; `Topbar` có nút thu/mở, ẩn dòng tên đơn vị |
| < `$bp-md` | `Sidebar` drawer, chân dải ở chân `Drawer`; `Topbar` ẩn họ tên, chỉ còn `Avatar`; đệm `main` → `--layout-page-pad-sm` |
| < `$bp-xs` | Nút theme dồn vào menu tài khoản thành một mục mở Menu theme, cùng khuôn mục Ngôn ngữ; drawer đóng khi chọn một mục |

**Ẩn đi đâu:** họ tên và tên đơn vị vẫn nằm trong nhãn đọc lên của nút mở menu tài khoản; nút theme vào menu tài khoản; bản quyền và phiên bản ở `Sidebar` thu gọn đọc được khi mở rộng dải hoặc trong drawer. Hamburger và `Avatar` không bao giờ ẩn.

### Icon

Theo [Icons.md](../Icons.md) §5, trừ dòng có căn cứ riêng.

| Chỗ | Icon |
| --- | --- |
| Hamburger, nút thu/mở `Sidebar` · mở/thu nhánh menu | `pi-bars` · `pi-chevron-right`/`pi-chevron-down` |
| Icon từng mục menu | Class icon trong dữ liệu (`icon` của mục — [meta-menu.md](../../contracts/meta-menu.md) §1.3) |
| Mục "Đơn vị" — đơn vị hệ thống | `pi-building` — dòng Đơn vị của Icons.md §5; giá trị đi trong dữ liệu seed theo ADR-0122 |
| Nút theme và ba mục Menu theme | Dòng Đổi theme — một icon mỗi giá trị; nút mang icon của giá trị đang áp |
| "Hồ sơ cá nhân" · "Ngôn ngữ" · "Đăng xuất" | `pi-user` · `pi-globe` · `pi-sign-out` |

### Ảnh màn hình

Chưa có — khung đã dựng ở `src/FE` (bảng "Có thật hôm nay → sẽ thành" ở đầu file) nhưng chưa ai chạy ứng dụng chụp màn hình.

### Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | ~~Đường vào nhìn thấy được tới khu `/he-thong`~~ **Đã đóng 2026-10-08:** cơ chế ở [ADR-0122](../../adr/0122-don-vi-he-thong-duoc-seed-mot-muc-menu-toi-he-thong-don-vi-moi-tai-khoan-cua-no-mang-co-van-hanh.md) (sửa một phần ADR-0079); nhãn, khoá, icon, vị trí ở bảng Quyết định bố cục. Màn thứ hai của khu hệ thống mà cần nhóm dưới một mục cha thì là dấu hiệu sai 1 của ADR-0122 — quay lại `architect`, không tự thêm mục cha | Đã trả lời |
| 2 | Duyệt các câu mang `*Chờ duyệt:*` ở bảng Câu chữ | Người dùng |
