---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Khung ứng dụng — màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`. Khung **sẽ được dựng** ở pha F2 ([03-f2-auth-routing.md](../../wiki-core/fe/trien-khai/03-f2-auth-routing.md) §2).

Khung bọc mọi màn sau khi đăng nhập: `Sidebar`, `Topbar` (tiêu đề tuyến + menu tài khoản), `main`, `Footer`; giữ hai lớp nổi dùng chung của `Topbar`: **menu tài khoản** và **menu theme**. Màn con ([02](./02-trang-loi.md), [03](./03-ho-so-ca-nhan.md), [10](./10-nguoi-dung.md), [11](./11-vai-tro.md), [12](./12-ma-tran-phan-quyen.md), [20](./20-don-vi.md)) chỉ vẽ phần trong `main`. Nghiệp vụ lấy từ các nguồn trích trong file — không chép lại.

> **Khung:** chính là thứ file này mô tả. Tầng smart `platform/shell` inject phiên và menu, truyền xuống `Sidebar`/`Topbar` qua `input()` ([fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.3).
> **Quyền:** không cần permission. Mọi tuyến qua `authGuard` và `mustChangePasswordGuard` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §2.1, §4). Cây menu do máy chủ lọc theo quyền ([meta-menu.md](../../contracts/meta-menu.md) §1.4).

---

## Khung ứng dụng (mọi tuyến trong nhánh shell)

### Sơ đồ bố cục

```text
- body
  - Liên kết "Bỏ qua tới nội dung chính" — ẩn cho tới khi nhận focus, đứng TRƯỚC Sidebar và Topbar trong DOM
      (thuộc khung, không thuộc component nào — Sidebar.md §Accessibility)
  - Sidebar
      expanded  --layout-sidebar-w            ≥ $bp-lg
      collapsed --layout-sidebar-w-collapsed  $bp-md … $bp-lg
      drawer                                  < $bp-md, mở bằng hamburger của Topbar
    - cây menu dựng từ danh sách phẳng của GET /api/v1/core/meta/menu; nhãn = bản dịch của labelKey
    - activeRoute = tuyến hiện tại → mục đó mang aria-current="page"
  - Topbar `default` — cao --layout-topbar-h, dính đỉnh
    - hamburger (drawer) hoặc nút thu/mở Sidebar (collapsed) — theo sidebarMode
    - tiêu đề tuyến — bản dịch khoá `title` của tuyến
    - IconButton ghost — nút theme, icon hiện giá trị đang áp; bấm thì mở Menu theme (lớp nổi, xem dưới)
    - khu tài khoản: Avatar (chữ cái đầu của fullName) + fullName, dòng dưới là tên đơn vị của phiên
        (input tenantName của Topbar — auth.md §5, chỉ để hiển thị; bố cục hai dòng và cách cắt chữ
         khai ở Topbar.md, màn không quyết lại) → mở menu tài khoản
  - main — đích của liên kết bỏ qua; rộng tối đa --layout-container-max;
           đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm)
    - vùng màn con — màn con vẽ từ PageHeader trở xuống
  - Footer `full`, cỡ `md`
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
| Tiêu đề tuyến trên `Topbar` | Bản dịch khoá `title` của tuyến — do spec màn con khai | khoá của tuyến | [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §2.2 |
| Tên đơn vị trên `Topbar` · tên từng ngôn ngữ | Dữ liệu, **không** qua khoá: `tenantName` của phiên · tên viết bằng chính ngôn ngữ đó | — | [auth.md](../../contracts/auth.md) §5 · [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Accessibility |
| `aria-label` nút theme (`aria-haspopup="menu"`); cũng là nhãn mục thay nó trong menu tài khoản dưới `$bp-xs` | Giao diện | `khung.theme.nhan` | duyệt |
| Ba mục `menuitemradio` Menu theme | Sáng · Tối · Theo hệ điều hành | `khung.theme.sang` · `khung.theme.toi` · `khung.theme.theoHeThong` | duyệt · [DESIGN.md](../DESIGN.md) §8 |
| Đọc lên qua `aria-live` sau khi đổi theme · đổi ngôn ngữ | Đã chuyển sang giao diện tối · Đã chuyển sang giao diện theo hệ điều hành · Đã chuyển sang giao diện sáng · Đã đổi ngôn ngữ giao diện | `khung.theme.daChuyenToi` · `khung.theme.daChuyenTheoHeThong` · `khung.theme.daChuyenSang` · `khung.ngonNgu.daDoi` | duyệt · [Topbar.md](../Components/Topbar.md) §Accessibility |
| `aria-label` nút mở menu tài khoản | Tài khoản {{hoTen}}, đơn vị {{tenDonVi}} — ghép **cả hai** vì dòng tên đơn vị ẩn từ dưới `$bp-lg` | `khung.taiKhoan.moMenu` | duyệt · [Topbar.md](../Components/Topbar.md) §Accessibility |
| Mục menu tài khoản | Hồ sơ cá nhân · Ngôn ngữ (chỉ khi `CORE_I18N.languages` ≥ 2; v1 không có, chỗ giữ) · Đăng xuất | `hoSo.tieuDe` · `chung.ngonNgu` · `xacThuc.dangXuat` | [profile.md](../../contracts/profile.md) tên card · [LanguageSwitcher.md](../Components/LanguageSwitcher.md) · [Icons.md](../Icons.md) §5 |
| `Sidebar` — tải menu hỏng · không có mục nào | Không tải được menu. · Tài khoản chưa được cấp chức năng nào. Liên hệ quản trị viên của đơn vị. | `khung.dieuHuong.loiTai` · `khung.dieuHuong.trong` | duyệt |
| Nút thử lại | Thử lại | `chung.thuLai` | [Sidebar.md](../Components/Sidebar.md) §Trạng thái |
| Bản quyền ở `Footer` | Như màn Đăng nhập | `chung.chanTrang.banQuyen` | [01-dang-nhap.md](./01-dang-nhap.md) |
| Hộp cảnh báo sắp hết phiên | **Bỏ** — v1 không có hộp cảnh báo sớm; nhóm khoá `khung.hetPhien.*` không khai | — | [07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §10 · duyệt 2026-09-16 |
| Câu hỏi khi đăng xuất còn thay đổi chưa lưu | Câu chung của `unsavedChangesGuard` | khoá chung của guard — không đặt ở khung | [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §4.1 |
| Toast lỗi đăng xuất, lỗi ghi ngôn ngữ | Câu dịch theo mã; không có mã thì câu mất kết nối | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 |

### Trạng thái

Đường lỗi chung: [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2, §2.5 · [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8.

- **mặc định** — `Sidebar` có cây menu, mục của tuyến hiện tại `active`; `Topbar` có tiêu đề tuyến và họ tên; `main` vẽ màn con.
- **đang tải** — `Sidebar` `loading` ([Sidebar.md](../Components/Sidebar.md) §Trạng thái); `Topbar`, `main` vẽ ngay, hamburger bấm được. Khu tài khoản không có ca tải; tải nội dung thuộc màn con.
- **rỗng** — menu rỗng → `Sidebar` `empty`; hồ sơ và đăng xuất vẫn ở `Topbar`.
- **lỗi**:
  - Tải menu hỏng → `Sidebar` `error` kèm Thử lại; request menu tắt toast (`BO_QUA_TOAST_LOI`) vì `Sidebar` tự hiện lỗi.
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
| ≥ `$bp-lg` | `Sidebar` `expanded`; `Topbar` không hamburger, họ tên đầy đủ |
| `$bp-md` … `$bp-lg` | `Sidebar` `collapsed`, nhãn mục hiện qua lớp nổi khi hover **và** focus; `Topbar` có nút thu/mở, ẩn dòng tên đơn vị |
| < `$bp-md` | `Sidebar` drawer; `Topbar` ẩn họ tên, chỉ còn `Avatar`; đệm `main` → `--layout-page-pad-sm` |
| < `$bp-xs` | `Topbar` ẩn tiêu đề tuyến; nút theme dồn vào menu tài khoản thành một mục mở Menu theme, cùng khuôn mục Ngôn ngữ; drawer đóng khi chọn một mục |

**Ẩn đi đâu:** họ tên và tên đơn vị vẫn nằm trong nhãn đọc lên của nút mở menu tài khoản; tiêu đề tuyến vẫn ở `<h1>` của `PageHeader` màn con; nút theme vào menu tài khoản. Hamburger và `Avatar` không bao giờ ẩn.

### Icon

Theo [Icons.md](../Icons.md) §5, trừ dòng có căn cứ riêng.

| Chỗ | Icon |
| --- | --- |
| Hamburger, nút thu/mở `Sidebar` · mở/thu nhánh menu | `pi-bars` · `pi-chevron-right`/`pi-chevron-down` |
| Icon từng mục menu | Class icon trong dữ liệu (`icon` của mục — [meta-menu.md](../../contracts/meta-menu.md) §1.3) |
| Nút theme và ba mục Menu theme | Dòng Đổi theme — một icon mỗi giá trị; nút mang icon của giá trị đang áp |
| "Hồ sơ cá nhân" · "Ngôn ngữ" · "Đăng xuất" | `pi-user` · `pi-globe` · `pi-sign-out` |

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn.
