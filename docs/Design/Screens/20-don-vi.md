---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quản trị đơn vị (khu hệ thống) — màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`. Khu này dựng sau F3 và đóng khi pha B3 xong ([00-lo-trinh-tong-the.md](../../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) §1).

Tài khoản vận hành hệ thống xem, tìm đơn vị; tạo đơn vị ([V2](../../luong/V2-tao-don-vi-moi.md)); ngưng và bật lại ([V3](../../luong/V3-ngung-va-bat-lai-don-vi.md)); khôi phục tài khoản quản trị của một đơn vị và, khi không còn ai đủ điều kiện, tạo tài khoản quản trị mới ([V4](../../luong/V4-khoi-phuc-quan-tri-don-vi.md)). Người vận hành **không** thấy dữ liệu bên trong đơn vị: không có màn chi tiết, không có danh sách người dùng của đơn vị. Một màn, ba hộp thoại, một hộp xác nhận. Nghiệp vụ: [tenants.md](../../contracts/tenants.md) — không chép lại. Bố cục theo [ListScreen.md](../Templates/ListScreen.md) §2.

> **Khung:** khung ứng dụng ([00-khung-ung-dung.md](./00-khung-ung-dung.md))
> **Quyền:** cờ `isSystemOperator` qua `systemOperatorGuard` — **không** phải permission ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1, §4). Mọi endpoint mang `[RequireSystemOperator]` ([tenants.md](../../contracts/tenants.md)). Không phân quyền theo nút: vào được màn là dùng được mọi thao tác.

---

## Danh sách đơn vị (`/he-thong/don-vi`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md
  - main — đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm); không cuộn
    - PageHeader biến thể default
      - tiêu đề + mô tả
      - Button primary "Tạo đơn vị"
    - Toolbar biến thể search, cỡ md — không có trường lọc
      - ô tìm → searchText (card §1: tìm trên code, name, không phân biệt hoa thường)
    - khung DataTable biến thể paged — thân bảng là vùng cuộn duy nhất; KHÔNG phát rowClick (không có màn chi tiết)
      - cột (theo response GET /system/tenants):
        - Mã đơn vị — code, sortable
        - Tên đơn vị — name, sortable, priority high
        - Trạng thái — isActive → Badge success "Đang hoạt động" / Badge danger "Ngưng hoạt động"
        - Ngày tạo — createdAt, sortable, priority low
        - Hành động — IconButton ghost cỡ sm pi-ellipsis-v mở Menu anchored cỡ sm, header = tên đơn vị
            isActive = true  → mục "Ngưng hoạt động" (danger)
            isActive = false → mục "Bật lại hoạt động"
            mục "Khôi phục quản trị"
            mục "Tạo quản trị mới"
      - emptyTemplate: EmptyState cỡ compact — biến thể theo ca (mục Trạng thái)
      - loadingTemplate: SkeletonLoader group dạng hàng
      - errorTemplate: EmptyState `error` cỡ compact, nút secondary "Thử lại"
    - dải phân trang — do DataTable vẽ ở biến thể paged, màn không tự đặt Pagination (Templates/ListScreen.md vùng 6); Pagination biến thể full, cỡ md
  - Footer
```

Tham số URL và dây: `page`, `pageSize`, `sortBy` (`code` · `name` · `createdAt`), `sortDescending`, `searchText`. Đổi từ khoá thì về trang 1. Đơn vị hệ thống không có trong kết quả (card §1) nên không cần khoá thao tác nào cho nó.

| Quyết định | Căn cứ |
| --- | --- |
| "Ngưng hoạt động" là `Badge` `danger` — cùng vai "bị khoá, bị chặn", cùng màu với "Đã khoá" ở [10-nguoi-dung.md](./10-nguoi-dung.md) | [V3](../../luong/V3-ngung-va-bat-lai-don-vi.md) bước 5 · [Badge.md](../Components/Badge.md) §Biến thể |
| Ô "Mật khẩu tạm" có nút hiện/ẩn (`revealable` bật sẵn), **không** có ô nhập lại — ở mọi hộp có ô này; cùng quyết định với [10-nguoi-dung.md](./10-nguoi-dung.md) | [Input.md](../Components/Input.md) §API |
| Khuôn mã đơn vị kiểm ở FE trước khi gửi, cùng khuôn với card (tập ký tự, ký tự đầu, độ dài tối đa sống ở card — màn không chép biểu thức); ô có `maxlength` bằng độ dài tối đa; FE **chuyển chuỗi về chữ HOA** trước khi kiểm và gửi, ô đổi sang chữ hoa khi rời ô. Sai khuôn → `CORE.CLIENT.VALIDATION_PATTERN`; quá dài → `CORE.CLIENT.VALIDATION_MAXLENGTH`; máy chủ vẫn kiểm lại, trả `CORE.VALIDATION.FAILED` khoá `Code` | [tenants.md](../../contracts/tenants.md) §2 |
| Hộp "Ngưng hoạt động" là `ConfirmDialog` `warning` (hậu quả rộng nhưng khắc phục được bằng bật lại); tiêu đề nêu **tên và mã**; xác nhận → `PUT .../active` với `isActive` = `false` | [V3](../../luong/V3-ngung-va-bat-lai-don-vi.md) bước 5 · [tenants.md](../../contracts/tenants.md) §3 |
| "Bật lại hoạt động" không hỏi xác nhận (thao tác khôi phục); gửi ngay `isActive` = `true`; `Toast` `success`; tải lại danh sách | [ConfirmDialog.md](../Components/ConfirmDialog.md) |
| Hộp khôi phục và hộp tạo quản trị có ô "Gõ lại mã đơn vị": so với `code` của hàng, kiểm ở FE, **không** gửi lên (card §4 nhận đúng hai trường); so không phân biệt hoa thường; chưa khớp → lỗi dưới ô `CORE.CLIENT.VALIDATION_MISMATCH`, focus về ô, không gọi API | [tenants.md](../../contracts/tenants.md) §2, §4 |
| Toast khôi phục **không** nêu gì về tài khoản đích (phản hồi không mang dữ liệu đó); xác minh người yêu cầu và kênh chuyển mật khẩu tạm là quy trình ngoài phần mềm, hộp không có trường cho việc đó | [tenants.md](../../contracts/tenants.md) §4 · [ADR-0029](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) |

**Hộp thoại "Tạo đơn vị"** — `Dialog` cỡ `md`, sáu trường chia hai nhóm (khe nhóm `--sp-7` theo [FormRow.md](../Components/FormRow.md)):

```text
- Dialog md, dirty khi đã gõ bất kỳ ô nào
  - thân
    - NoticeBanner sm vai danger (lỗi không gắn được vào ô, gồm CORE.TENANT.SEED_FAILED)
    - nhóm "Đơn vị"
      - FormRow required "Mã đơn vị" — Input text, maxlength và khuôn theo tenants.md §2, FE kiểm cùng khuôn trước khi gửi; gợi ý: câu tĩnh nêu khuôn bằng lời
      - FormRow required "Tên đơn vị" — Input text
    - nhóm "Tài khoản quản trị đầu tiên"
      - FormRow required "Tên đăng nhập" — Input text, autocomplete off
      - FormRow required "Email" — Input text, inputmode email, autocomplete off
      - FormRow required "Họ tên" — Input text
      - FormRow required "Mật khẩu tạm" — Input password, revealable (mặc định), autocomplete new-password; không có ô nhập lại; gợi ý: câu chính sách tĩnh
  - chân: Button secondary "Huỷ" · Button primary "Tạo đơn vị" (loading khi đang gửi; loadingBlocksClose)
```

Khoá `fieldErrors` của `POST /system/tenants` trùng tên field request, PascalCase (card §2): `Code` → "Mã đơn vị", `Name` → "Tên đơn vị", `AdminUserName` → "Tên đăng nhập", `AdminEmail` → "Email", `AdminFullName` → "Họ tên", `AdminTempPassword` → "Mật khẩu tạm". Tạo xong: đóng hộp, `Toast` `success`, tải lại danh sách; phản hồi **không** mang mật khẩu, màn không hiện lại mật khẩu vừa gõ. `SEED_FAILED` giữ nguyên hộp và nội dung — mã vẫn trống nên gửi lại được.

**Hộp thoại "Khôi phục quản trị"** — `Dialog` cỡ `sm`:

```text
- Dialog sm — tiêu đề nêu tên đơn vị
  - thân
    - NoticeBanner sm vai info — ai là đích hợp lệ
    - NoticeBanner sm vai danger (lỗi không gắn được vào ô)
    - FormRow required "Tên đăng nhập" — Input text, autocomplete off
    - FormRow required "Mật khẩu tạm" — Input password, revealable (mặc định), autocomplete new-password; không có ô nhập lại; gợi ý: câu chính sách tĩnh
    - FormRow required "Gõ lại mã đơn vị" — Input text, autocomplete off; gợi ý nêu mã cần gõ
  - chân: Button secondary "Huỷ" · Button primary "Đặt mật khẩu tạm" (loading; loadingBlocksClose)
```

Xong: đóng hộp, `Toast` `success`.

**Hộp thoại "Tạo quản trị mới"** — `Dialog` cỡ `sm`, mở từ menu hàng khi không còn tài khoản nào đủ điều kiện khôi phục ([V4](../../luong/V4-khoi-phuc-quan-tri-don-vi.md)): bốn ô như nhóm "Tài khoản quản trị đầu tiên" của hộp tạo, thêm ô "Gõ lại mã đơn vị" cùng luật hộp khôi phục; gửi `POST /api/v1/core/system/tenants/{id}/admins`, khoá `fieldErrors`: `UserName`, `Email`, `FullName`, `TempPassword`. Xong: đóng hộp, `Toast` `success`; phản hồi không mang mật khẩu.

### Câu chữ

Câu không ghi nguồn: người dùng duyệt 2026-09-15 ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề màn, tiêu đề tab | Đơn vị | `donVi.tieuDe` |
| Mô tả dưới tiêu đề | Các đơn vị dùng hệ thống. Người vận hành không xem được dữ liệu bên trong đơn vị. | `donVi.moTa` |
| Nút chính | Tạo đơn vị | `donVi.hanhDong.tao` |
| Nhãn ô tìm (ẩn) · `<caption>` bảng (ẩn) | Tìm theo mã hoặc tên đơn vị · Danh sách đơn vị | `donVi.timKiem.nhan` · `donVi.bang.caption` |
| Cột | Mã đơn vị · Tên đơn vị · Trạng thái · Ngày tạo · Hành động | `donVi.cot.ma` · `donVi.cot.ten` · `donVi.cot.trangThai` · `donVi.cot.ngayTao` · `donVi.cot.hanhDong` |
| Badge trạng thái | Đang hoạt động · Ngưng hoạt động | `donVi.trangThai.hoatDong` · `donVi.trangThai.ngung` |
| `aria-label` nút mở menu hàng | Hành động cho đơn vị {{ten}} | `donVi.menu.moMenu` |
| Mục menu | Ngưng hoạt động · Bật lại hoạt động · Khôi phục quản trị · Tạo quản trị mới | `donVi.menu.ngung` · `donVi.menu.batLai` · `donVi.menu.khoiPhuc` · `donVi.menu.taoQuanTri` |
| Tiêu đề hộp tạo | Tạo đơn vị | `donVi.form.tieuDe` |
| Nhãn nhóm | Đơn vị · Tài khoản quản trị đầu tiên | `donVi.form.nhomDonVi` · `donVi.form.nhomQuanTri` |
| Nhãn trường | Mã đơn vị · Tên đơn vị · Tên đăng nhập · Email · Họ tên · Mật khẩu tạm | `donVi.form.ma` · `donVi.form.ten` · `donVi.form.tenDangNhap` · `donVi.form.email` · `donVi.form.hoTen` · `donVi.form.matKhauTam` |
| Gợi ý ô mã (nêu khuôn bằng lời; khuôn máy kiểm sống ở [tenants.md](../../contracts/tenants.md) §2, đổi khuôn thì đổi câu) | Chữ in hoa, chữ số, dấu gạch ngang và gạch dưới; bắt đầu bằng chữ hoặc số. Người dùng của đơn vị gõ mã này khi đăng nhập. | `donVi.form.goiYMa` |
| Lỗi ô mã — sai khuôn · quá dài | Câu của validator khuôn, validator độ dài | `loi.CORE.CLIENT.VALIDATION_PATTERN` · `loi.CORE.CLIENT.VALIDATION_MAXLENGTH` |
| Gợi ý ô mật khẩu tạm (mọi hộp có ô này; dùng chung với [10-nguoi-dung.md](./10-nguoi-dung.md)) | Như màn người dùng | `xacThuc.matKhauTam.goiY` |
| Nút hiện/ẩn mật khẩu tạm (mọi hộp có ô này; dùng chung với [01-dang-nhap.md](./01-dang-nhap.md)) | Hiện mật khẩu ↔ Ẩn mật khẩu | `xacThuc.matKhau.hien` · `xacThuc.matKhau.an` |
| Nút hộp tạo | Huỷ · Tạo đơn vị | `chung.huy` · `donVi.form.xacNhan` |
| Toast tạo thành công (nhắc bước 8 của [V2](../../luong/V2-tao-don-vi-moi.md)) | Đã tạo đơn vị {{ten}}. Chuyển mật khẩu tạm cho quản trị của đơn vị. | `donVi.thongBao.taoThanhCong` |
| Tiêu đề xác nhận ngưng | Ngưng hoạt động đơn vị {{ten}} ({{ma}})? | `donVi.ngung.tieuDe` |
| Mô tả xác nhận ngưng (nói cả hai hiệu lực) · nút | Mọi tài khoản của đơn vị sẽ không đăng nhập được, và phiên đang mở bị chặn ở thao tác kế tiếp. Bật lại được bất cứ lúc nào. · Ngưng hoạt động | `donVi.ngung.moTa` · `donVi.ngung.xacNhan` |
| Toast ngưng / bật lại thành công | Đơn vị {{ten}} đã ngưng hoạt động. · Đơn vị {{ten}} hoạt động trở lại. | `donVi.thongBao.ngungThanhCong` · `donVi.thongBao.batLaiThanhCong` |
| Tiêu đề hộp khôi phục | Khôi phục quản trị — {{ten}} | `donVi.khoiPhuc.tieuDe` |
| Banner đích hợp lệ | Chỉ đặt được mật khẩu tạm cho tài khoản đang mang cờ đặc quyền hoặc đang giữ vai trò hệ thống của đơn vị này. | `donVi.khoiPhuc.ghiChu` |
| Nhãn trường khôi phục · nhãn ô gõ lại mã | Tên đăng nhập · Mật khẩu tạm · Gõ lại mã đơn vị để xác nhận | `donVi.khoiPhuc.tenDangNhap` · `donVi.khoiPhuc.matKhauTam` · `donVi.khoiPhuc.goLaiMa` |
| Gợi ý ô gõ lại mã | Gõ mã {{ma}} của đơn vị này. | `donVi.khoiPhuc.goiYGoLaiMa` |
| Lỗi ô gõ lại mã — không khớp | Câu chung của mã, như ô nhập lại ở [01-dang-nhap.md](./01-dang-nhap.md) | `loi.CORE.CLIENT.VALIDATION_MISMATCH` |
| Nút xác nhận khôi phục · toast khôi phục thành công (không nêu gì về tài khoản đích) | Đặt mật khẩu tạm · Đã đặt mật khẩu tạm. | `donVi.khoiPhuc.xacNhan` · `donVi.thongBao.khoiPhucThanhCong` |
| Tiêu đề hộp tạo quản trị | Tạo quản trị mới — {{ten}} | `donVi.taoQuanTri.tieuDe` |
| Nhãn trường hộp tạo quản trị | Như nhóm "Tài khoản quản trị đầu tiên" của hộp tạo; ô gõ lại mã như hộp khôi phục | `donVi.form.tenDangNhap` · `donVi.form.email` · `donVi.form.hoTen` · `donVi.form.matKhauTam` · `donVi.khoiPhuc.goLaiMa` |
| Nút xác nhận tạo quản trị | Tạo tài khoản | `donVi.taoQuanTri.xacNhan` |
| Toast tạo quản trị thành công | Đã tạo tài khoản quản trị {{tenDangNhap}}. Chuyển mật khẩu tạm cho người nhận. | `donVi.thongBao.taoQuanTriThanhCong` |
| Rỗng — chưa có đơn vị | Chưa có đơn vị nào · Tạo đơn vị đầu tiên cùng tài khoản quản trị của nó. | `donVi.trong.tieuDe` · `donVi.trong.moTa` |
| Rỗng — tìm không ra | Không có đơn vị nào khớp "{{tuKhoa}}" · Thử từ khoá khác hoặc xoá từ khoá đang tìm. | `donVi.khongKetQua.tieuDe` · `donVi.khongKetQua.moTa` |
| Tiêu đề khối lỗi tải danh sách | Không tải được danh sách đơn vị | `donVi.loi.taiThatBai` |
| Thân khối lỗi | Câu dịch theo mã; không có mã thì câu mất kết nối | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` |
| Nút thử lại · nút xoá bộ lọc | Thử lại · Xoá bộ lọc | `chung.thuLai` · `chung.xoaBoLoc` |

Mã lỗi → chỗ hiện (câu sống ở `loi.<mã>`):

| Mã | Endpoint | Hiện ở |
| --- | --- | --- |
| `CORE.VALIDATION.FAILED` — khoá tham số danh sách, gồm `SearchText` | danh sách | `errorTemplate` của `DataTable` |
| `CORE.VALIDATION.FAILED` | tạo | Dòng lỗi dưới ô theo khoá `fieldErrors` ở trên; khoá không khớp → `NoticeBanner` trong hộp |
| `CORE.TENANT.CODE_DUPLICATE` | tạo | Dòng lỗi dưới ô "Mã đơn vị" (FE tự gắn mã vào ô) |
| `CORE.TENANT.ADMIN_CREATE_FAILED` | tạo | Theo `fieldErrors`; khoá không khớp → `NoticeBanner` trong hộp |
| `CORE.TENANT.SEED_FAILED` | tạo | `NoticeBanner` `danger` trong hộp; hộp và nội dung giữ nguyên |
| `CORE.TENANT.NOT_FOUND` | ngưng/bật, khôi phục, tạo quản trị | `NoticeBanner` `danger` trong hộp đang mở; ca bật lại (không có hộp) → `Toast` `danger`; tải lại danh sách |
| `CORE.TENANT.SYSTEM_IMMUTABLE` | ngưng/bật | `NoticeBanner` `danger` trong `ConfirmDialog` — không kỳ vọng xảy ra vì danh sách không chứa đơn vị hệ thống |
| `CORE.VALIDATION.FAILED` — `fieldErrors["UserName"]` / `["TempPassword"]` | khôi phục | Dòng lỗi dưới ô tương ứng |
| `CORE.TENANT.RECOVERY_TARGET_NOT_ELIGIBLE` | khôi phục | `NoticeBanner` `danger` trong hộp — **một câu cho cả hai ca** (card gộp mã có chủ đích); **không** gắn vào ô "Tên đăng nhập" |
| `CORE.TENANT.RECOVERY_RESET_FAILED` | khôi phục | Theo `fieldErrors`, thường là ô "Mật khẩu tạm" |
| `CORE.VALIDATION.FAILED` — `UserName` / `Email` / `FullName` / `TempPassword` | tạo quản trị | Dòng lỗi dưới ô tương ứng |
| Mã còn lại của endpoint ([tenants.md](../../contracts/tenants.md)) | tạo quản trị | Theo `fieldErrors`; khoá không khớp → `NoticeBanner` `danger` trong hộp, hộp và nội dung giữ nguyên |
| `CORE.AUTH.FORBIDDEN` · `CORE.AUTH.CSRF_REJECTED` | mọi endpoint | Đường chung ở [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8 |

### Trạng thái

- **mặc định:** `DataTable` có dữ liệu; `Pagination` hiện "x–y trên tổng z".
- **đang tải:** lần đầu — `loadingTemplate`, `SkeletonLoader` dạng hàng; đổi trang/sắp xếp/từ khoá — giữ dữ liệu cũ, phủ mờ. Đang gửi ngưng/bật: mục menu không bấm lại được cho tới khi xong.
- **rỗng (chưa có đơn vị nào):** `EmptyState` `first-use` cỡ `compact`, nút primary "Tạo đơn vị".
- **rỗng (tìm không ra kết quả):** `EmptyState` `no-results`, nhắc lại từ khoá, nút secondary "Xoá bộ lọc"; **không** mời tạo mới.
- **lỗi:** tải danh sách hỏng → `errorTemplate`, tiêu đề cột giữ nguyên. Vừa mất cờ vận hành giữa phiên → 403 đi đường chung; guard áp ở lần điều hướng kế tiếp.
- **kiểm tra dữ liệu:** trong các hộp — lỗi từng ô dưới ô, lỗi còn lại ở `NoticeBanner` đầu thân hộp; bấm gửi hai lần chỉ gửi một lần. Hộp khôi phục và tạo quản trị: ô gõ lại mã phải khớp mã của hàng trước khi gửi (bảng Quyết định).

### Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Đủ năm cột |
| `$bp-md` … `$bp-lg` | Ẩn "Ngày tạo" (`priority: low`) — chấp nhận mất ở khổ này, cùng quyết định với [11-vai-tro.md](./11-vai-tro.md); hiện lại ở dạng thẻ dưới `$bp-xs` |
| < `$bp-md` | Giữ "Tên đơn vị", "Trạng thái", cột hành động; "Mã đơn vị" đứng dưới tên trong cùng ô, chữ `--color-text-muted` |
| < `$bp-xs` | `DataTable` dạng thẻ, đủ mọi trường. Hộp thoại dính đáy theo `Dialog`; hộp tạo xếp hai nhóm liên tiếp một cột |

### Icon

Theo [Icons.md](../Icons.md) §5.

| Chỗ | Icon |
| --- | --- |
| "Tạo đơn vị" · "Tạo quản trị mới" | `pi-plus` |
| Nút mở menu hàng | `pi-ellipsis-v` |
| "Ngưng hoạt động" · "Bật lại hoạt động" | `pi-power-off` |
| "Khôi phục quản trị" | `pi-key` |
| Nút hiện/ẩn ô "Mật khẩu tạm" | `pi-eye` · `pi-eye-slash` |
| Ô tìm · "Xoá bộ lọc" · "Thử lại" · `th` | `pi-search` · `pi-filter-slash` · `pi-refresh` · `pi-sort-*` |

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn.
