---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quản trị vai trò — màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`; màn thuộc pha F3 ([04-f3-man-quan-tri.md](../../wiki-core/fe/trien-khai/04-f3-man-quan-tri.md)).

Quản trị đơn vị xem, tìm vai trò của đơn vị hiện hành; tạo, đổi tên, xoá vai trò — bước 1–2 của [P1](../../luong/P1-tao-vai-tro-va-gan-quyen.md); cấp quyền đi tiếp sang [12-ma-tran-phan-quyen.md](./12-ma-tran-phan-quyen.md). Một màn, hai hộp thoại, một hộp xác nhận. Nghiệp vụ: [roles.md](../../contracts/roles.md) — không chép lại. Bố cục theo [ListScreen.md](../Templates/ListScreen.md) §2.

> **Khung:** khung ứng dụng ([00-khung-ung-dung.md](./00-khung-ung-dung.md))
> **Quyền:** `core.role.read` để vào màn; `core.role.write` cho tạo, đổi tên, xoá — dòng `Quyền:` từng endpoint ở [roles.md](../../contracts/roles.md). Guard theo [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1.

---

## Danh sách vai trò (`/quan-tri/vai-tro`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md
  - main — đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm); không cuộn
    - PageHeader biến thể default
      - tiêu đề + mô tả
      - nhóm hành động: Button primary "Thêm vai trò" (chỉ hiện khi có core.role.write)
                        <a routerLink> "Ma trận phân quyền" → /quan-tri/phan-quyen — mượn hình thức Button secondary
                            (điều hướng là liên kết, không phải nút — Button.md); chỉ hiện khi có core.permission.read
    - Toolbar biến thể search, cỡ md — không có trường lọc; hàng chip có vì readonlyChips khác rỗng
      - ô tìm → searchText (card §1: tìm trên name, không phân biệt hoa thường)
      - readonlyChips — một chip đơn vị hiện hành: nhãn mang tenantName của phiên (auth.md §5),
          lockReason nói vì sao không gỡ được
    - khung DataTable biến thể paged — thân bảng là vùng cuộn duy nhất
      - cột (theo response GET /roles):
        - Tên vai trò — name, sortable, priority high
        - Loại — isSystem → Badge neutral "Vai trò hệ thống"; false → ô ghi dấu gạch (Table.md: không để ô rỗng trơn)
        - Số người dùng — userCount, căn phải, không sortable (không có trong allowlist)
        - Ngày tạo — createdAt, sortable, priority low
        - Hành động — IconButton ghost cỡ sm pi-ellipsis-v mở Menu anchored cỡ sm, header = tên vai trò
            mục "Đổi tên"
            mục "Xoá" (danger)
          (cột chỉ hiện khi có core.role.write)
      - emptyTemplate: EmptyState cỡ compact — biến thể theo ca (mục Trạng thái)
      - loadingTemplate: SkeletonLoader group dạng hàng
      - errorTemplate: EmptyState `error` cỡ compact, nút secondary "Thử lại"
    - dải phân trang — do DataTable vẽ ở biến thể paged, màn không tự đặt Pagination (Templates/ListScreen.md vùng 6); Pagination biến thể full, cỡ md
  - Footer
```

Tham số URL và dây: `page`, `pageSize`, `sortBy` (`name` · `createdAt`), `sortDescending`, `searchText` ([contracts/README.md](../../contracts/README.md) §8 · [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.8). Mặc định sắp theo `name` tăng dần. Đổi từ khoá thì về trang 1.

**Mục menu bị khoá vẫn hiện, nhận focus, kèm `disabledReason`** ([Menu.md](../Components/Menu.md)):

| Ca | Mục khoá | Lý do hiện |
| --- | --- | --- |
| `isSystem = true` (`CORE.ROLE.SYSTEM_IMMUTABLE`) | Đổi tên, Xoá | `vaiTro.menu.lyDoHeThong` |
| `userCount > 0` (`CORE.ROLE.IN_USE`; card cho `userCount` để cảnh báo **trước** khi bấm) | Xoá | `vaiTro.menu.lyDoDangDung` — tham số `soNguoi` từ `userCount` |

**Hộp thoại "Thêm vai trò" / "Đổi tên vai trò"** — `Dialog` cỡ `sm`, một trường:

```text
- Dialog sm — tiêu đề theo ca
  - thân
    - NoticeBanner sm vai danger (chỉ khi có lỗi không gắn được vào ô)
    - FormRow stacked, required — "Tên vai trò"
      - Input text cỡ md, autocomplete off
  - chân: Button secondary "Huỷ" · Button primary "Lưu" (loading khi đang gửi)
```

Tạo xong / đổi tên xong: đóng hộp, `Toast` `success`, tải lại danh sách. Vai trò mới **không có quyền nào** — `Toast` tạo nhắc bước cấp quyền.

**Hộp xác nhận "Xoá vai trò"** — `ConfirmDialog` `severity = 'danger'` (không hoàn tác được); nhãn nút xác nhận nói hành động; chỉ mở được khi mục "Xoá" không bị khoá.

### Câu chữ

Câu không ghi nguồn: người dùng duyệt 2026-09-15 ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề màn, tiêu đề tab | Vai trò | `vaiTro.tieuDe` |
| Mô tả dưới tiêu đề | Vai trò gom các quyền để gán cho người dùng của đơn vị. | `vaiTro.moTa` |
| Nút chính · liên kết sang ma trận | Thêm vai trò · Ma trận phân quyền | `vaiTro.hanhDong.them` · `vaiTro.hanhDong.moMaTran` |
| Nhãn ô tìm (ẩn) | Tìm theo tên vai trò | `vaiTro.timKiem.nhan` |
| Chip đơn vị hiện hành · `Tooltip` lý do khoá chip | Như [10-nguoi-dung.md](./10-nguoi-dung.md) | `chung.loc.donViHienHanh` · `chung.loc.lyDoDonVi` |
| `<caption>` bảng (ẩn) | Danh sách vai trò | `vaiTro.bang.caption` |
| Cột | Tên vai trò · Loại · Số người dùng · Ngày tạo · Hành động | `vaiTro.cot.ten` · `vaiTro.cot.loai` · `vaiTro.cot.soNguoiDung` · `vaiTro.cot.ngayTao` · `vaiTro.cot.hanhDong` |
| Badge loại | Vai trò hệ thống | `vaiTro.loai.heThong` |
| `aria-label` nút mở menu hàng | Hành động cho vai trò {{ten}} | `vaiTro.menu.moMenu` |
| Mục menu | Đổi tên · Xoá | `vaiTro.menu.doiTen` · `vaiTro.menu.xoa` |
| Lý do khoá — vai trò hệ thống | Vai trò hệ thống không đổi tên hay xoá được. | `vaiTro.menu.lyDoHeThong` |
| Lý do khoá — còn người dùng | Còn {{soNguoi}} người dùng mang vai trò này. | `vaiTro.menu.lyDoDangDung` |
| Tiêu đề hộp thoại tạo · đổi tên | Thêm vai trò · Đổi tên vai trò | `vaiTro.form.tieuDeTao` · `vaiTro.form.tieuDeDoiTen` |
| Nhãn trường | Tên vai trò | `vaiTro.form.ten` |
| Nút hộp thoại | Huỷ · Lưu | `chung.huy` · `chung.luu` |
| Toast tạo thành công | Đã tạo vai trò {{ten}}. Vai trò mới chưa có quyền nào — cấp quyền ở Ma trận phân quyền. | `vaiTro.thongBao.taoThanhCong` |
| Toast đổi tên thành công | Đã đổi tên vai trò. | `vaiTro.thongBao.doiTenThanhCong` |
| Tiêu đề xác nhận xoá | Xoá vai trò {{ten}}? | `vaiTro.xoa.tieuDe` |
| Mô tả · nút xác nhận xoá | Không khôi phục được vai trò sau khi xoá. · Xoá vai trò | `vaiTro.xoa.moTa` · `vaiTro.xoa.xacNhan` |
| Toast xoá thành công | Đã xoá vai trò {{ten}}. | `vaiTro.thongBao.xoaThanhCong` |
| Rỗng — chưa có vai trò | Chưa có vai trò nào · Tạo vai trò đầu tiên để bắt đầu cấp quyền cho người dùng. | `vaiTro.trong.tieuDe` · `vaiTro.trong.moTa` |
| Rỗng — tìm không ra | Không có vai trò nào khớp "{{tuKhoa}}" · Thử từ khoá khác hoặc xoá từ khoá đang tìm. | `vaiTro.khongKetQua.tieuDe` · `vaiTro.khongKetQua.moTa` |
| Tiêu đề khối lỗi tải danh sách | Không tải được danh sách vai trò | `vaiTro.loi.taiThatBai` |
| Thân khối lỗi | Theo mã lỗi — bảng dưới; không có mã thì câu mất kết nối | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` |
| Nút thử lại · nút xoá bộ lọc | Thử lại · Xoá bộ lọc | `chung.thuLai` · `chung.xoaBoLoc` |

Mã lỗi → chỗ hiện (câu sống ở `loi.<mã>`, [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §5.3):

| Mã | Endpoint | Hiện ở |
| --- | --- | --- |
| `CORE.VALIDATION.FAILED` — `fieldErrors["Name"]` | tạo, đổi tên | Dòng lỗi dưới ô "Tên vai trò" |
| `CORE.VALIDATION.FAILED` — khoá tham số danh sách, gồm `SearchText` | danh sách | `errorTemplate` của `DataTable` |
| `CORE.ROLE.NAME_DUPLICATE` | tạo, đổi tên | Dòng lỗi dưới ô "Tên vai trò" — card không khai `fieldErrors`, FE tự gắn mã vào ô |
| `CORE.ROLE.SYSTEM_IMMUTABLE` | đổi tên, xoá | `NoticeBanner` `danger` trong hộp đang mở |
| `CORE.ROLE.IN_USE` | xoá | `NoticeBanner` `danger` trong `ConfirmDialog`; hộp không tự đóng. Câu mang **số người** từ `messageParams` của lỗi ([roles.md](../../contracts/roles.md) §4) — số của máy chủ lúc xoá, không phải `userCount` có thể đã cũ |
| `CORE.ROLE.NOT_FOUND` | đổi tên, xoá | `NoticeBanner` `danger` trong hộp; đóng hộp thì tải lại danh sách |
| `CORE.AUTH.FORBIDDEN` | mọi endpoint | Đường chung ở [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8 |

### Trạng thái

- **mặc định:** `DataTable` có dữ liệu; `Pagination` hiện "x–y trên tổng z".
- **đang tải:** lần đầu — `loadingTemplate` với `SkeletonLoader` dạng hàng; đổi trang/sắp xếp/từ khoá — giữ dữ liệu cũ, phủ mờ. `PageHeader` và `Toolbar` hiện ngay; ô tìm không bị khoá.
- **rỗng (chưa có vai trò nào):** `EmptyState` `first-use` cỡ `compact`, nút primary "Thêm vai trò" (chỉ khi có `core.role.write`; không có quyền thì không vẽ nút).
- **rỗng (tìm không ra kết quả):** `EmptyState` `no-results`, nhắc lại từ khoá, nút secondary "Xoá bộ lọc"; **không** mời tạo mới.
- **lỗi:** tải danh sách hỏng → `errorTemplate`, tiêu đề cột giữ nguyên, thử lại được. Hết phiên (401) đi đường chung.
- **kiểm tra dữ liệu:** trong hộp tạo/đổi tên — lỗi từng ô theo bảng mã lỗi; lỗi không gắn được vào ô → `NoticeBanner` đầu thân hộp ([Dialog.md](../Components/Dialog.md) trạng thái `error`). Bấm Lưu hai lần chỉ gửi một lần: nút `loading`, hộp `loadingBlocksClose`.

### Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Đủ năm cột; cột hành động ghim phải |
| `$bp-md` … `$bp-lg` | Ẩn "Ngày tạo" (`priority: low`) — chấp nhận mất ở khổ này; hiện lại ở dạng thẻ dưới `$bp-xs` |
| < `$bp-md` | Giữ "Tên vai trò", "Số người dùng", cột hành động; "Loại" thành `Badge` đứng sau tên trong cùng ô. `PageHeader` đưa nhóm nút xuống hàng dưới |
| < `$bp-xs` | `DataTable` dạng thẻ — mọi trường thành nhãn trường, không mất trường nào. Hộp thoại dính đáy theo `Dialog` |

### Icon

Theo [Icons.md](../Icons.md) §5: `pi-plus` "Thêm vai trò", `pi-pencil` "Đổi tên", `pi-trash` "Xoá", `pi-ellipsis-v` nút mở menu hàng, `pi-search` ô tìm, `pi-filter-slash` "Xoá bộ lọc", `pi-refresh` "Thử lại", `pi-sort-*` ở `th`. Icon khoá trên chip đơn vị do [FilterChip.md](../Components/FilterChip.md) tự mang.

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn.
