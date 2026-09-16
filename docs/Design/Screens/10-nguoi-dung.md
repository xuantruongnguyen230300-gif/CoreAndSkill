---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Quản trị người dùng — màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`; hai màn thuộc pha F3 ([04-f3-man-quan-tri.md](../../wiki-core/fe/trien-khai/04-f3-man-quan-tri.md)).

Quản trị đơn vị tìm, lọc, mở người dùng của đơn vị hiện hành; tạo tài khoản ([N1](../../luong/N1-tao-nguoi-dung-moi.md)). Từ màn chi tiết: sửa hồ sơ, gán vai trò ([P2](../../luong/P2-gan-vai-tro-cho-nguoi-dung.md)), khoá/mở khoá ([D5](../../luong/D5-khoa-va-mo-khoa-tai-khoan.md)), đặt lại mật khẩu tạm ([D4](../../luong/D4-quan-tri-dat-lai-mat-khau.md)). Danh sách → liên kết ở ô tên đăng nhập → chi tiết → nút quay lại về đúng danh sách cũ (trạng thái giữ trên URL). Nghiệp vụ: [users.md](../../contracts/users.md) (trường, mã lỗi, năm luật bảo vệ tài khoản quản trị ở §2) — không chép lại. Bố cục danh sách theo [ListScreen.md](../Templates/ListScreen.md) §2.

> **Khung:** khung ứng dụng ([00-khung-ung-dung.md](./00-khung-ung-dung.md))
> **Quyền:** `core.user.read` để vào cả hai màn ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1, §2.2). Từng thao tác cần khoá riêng, phần chọn vai trò cần thêm `core.role.read` — bảng "Phân quyền theo nút" cuối file.

---

## Danh sách người dùng (`/quan-tri/nguoi-dung`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md
  - main — đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm); không cuộn
    - PageHeader biến thể default
      - tiêu đề + mô tả
      - Button primary "Thêm người dùng" (chỉ khi có core.user.write)
    - Toolbar biến thể full, cỡ md — ô tìm + hai trường lọc nằm thẳng trong dải, không có panel lọc
        (Toolbar.md §"Panel lọc nằm ở đâu": 1–2 trường) + hàng chip
      - ô tìm → searchText (card §3: userName, fullName, email; tối đa 200 ký tự)
      - Autocomplete single cỡ md "Vai trò" → roleId — CHỈ khi có core.role.read; tìm qua GET /api/v1/core/roles với searchText (mục "Ô chọn vai trò")
      - Input select cỡ md "Trạng thái" → status: active | locked
      - readonlyChips — một chip đơn vị hiện hành: nhãn mang tenantName của phiên (auth.md §5),
          lockReason nói vì sao không gỡ được; Toolbar vẽ chip này TRƯỚC các chip gỡ được
      - chips — một chip cho mỗi điều kiện đang bật (tìm, vai trò, trạng thái); từ hai chip có nút xoá tất cả
    - khung DataTable biến thể paged — vùng cuộn duy nhất; không rowClick
      - cột (theo response GET /users):
        - Tên đăng nhập — userName, sortable, priority high; ô là <a routerLink> → /quan-tri/nguoi-dung/:id
            (lối vào chi tiết DUY NHẤT là liên kết ở ô định danh — không rowClick, không nút xem riêng; Button.md §Khi nào dùng)
        - Họ tên — fullName, sortable
        - Email — email, sortable
        - Vai trò — roles[].name dạng chữ, nối bằng dấu phẩy; roles rỗng → "Chưa có vai trò" chữ --color-text-muted
        - Trạng thái — isLocked → Badge danger "Đã khoá" / Badge success "Hoạt động"
        - Ngày tạo — createdAt, sortable, priority low
      - emptyTemplate: EmptyState cỡ compact — biến thể theo ca (mục Trạng thái)
      - loadingTemplate: SkeletonLoader group dạng hàng
      - errorTemplate: EmptyState `error` cỡ compact, nút secondary "Thử lại" — riêng CORE.USER.ROLE_NOT_FOUND nút là "Xoá bộ lọc"
    - dải phân trang — do DataTable vẽ ở biến thể paged, màn không tự đặt Pagination (Templates/ListScreen.md vùng 6); Pagination biến thể full, cỡ md
  - Footer
```

URL và dây mang đúng tên: `page`, `pageSize`, `sortBy` (`userName` · `fullName` · `email` · `createdAt`), `sortDescending`, `searchText`, `roleId`, `status` ([contracts/README.md](../../contracts/README.md) §8 · [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.8). Đổi tìm hoặc lọc thì về trang 1.

| Quyết định | Căn cứ |
| --- | --- |
| Ô "Vai trò" trong bảng chỉ là chữ: không đánh dấu `isSystem`, không phân biệt "chưa có quyền"; vai trò hệ thống đánh dấu ở màn chi tiết | [Badge.md](../Components/Badge.md) cấm nhiều `Badge` một ô · [users.md](../../contracts/users.md) §3 |
| Thiếu `core.role.read` → ẩn ô lọc "Vai trò", ô chọn "Vai trò" trong hộp tạo, nút "Gán vai trò"; URL mang `roleId` thì màn **bỏ tham số** khỏi URL khi vào và không gửi lên. Cột "Vai trò" trong bảng và `Card` "Vai trò" vẫn hiện (tên có sẵn trong response) | [roles.md](../../contracts/roles.md) §1 (cả `GET /api/v1/core/roles/{id}`) |
| Ô chọn vai trò là [Autocomplete](../Components/Autocomplete.md) gọi máy chủ, không tải hết danh sách: ô lọc `single`; hộp tạo và hộp gán `multiple` | Bảng dưới |
| Ô "Mật khẩu tạm" có nút hiện/ẩn (`revealable` bật sẵn, màn không tắt), **không** có ô nhập lại — ở cả hộp tạo và hộp đặt lại | [Input.md](../Components/Input.md) §API |

**Ô chọn vai trò:**

| Khoản | Hành vi | Căn cứ |
| --- | --- | --- |
| Khi `search` phát | `GET /api/v1/core/roles` với `searchText` là chuỗi đang gõ, trang đầu theo phân trang mặc định; `options` = `items` | [roles.md](../../contracts/roles.md) §1 |
| Dòng phụ kết quả | "Vai trò hệ thống" khi `isSystem` | [Autocomplete.md](../Components/Autocomplete.md) §Kích thước |
| Ngưỡng ký tự, chờ ngừng gõ, bỏ kết quả request cũ | Theo component — màn không đặt lại | [Autocomplete.md](../Components/Autocomplete.md) §API |
| Request tìm hỏng | Tắt toast (`BO_QUA_TOAST_LOI`); lỗi hiện trong lớp nổi qua `errorTemplate`, ô giữ chuỗi đang gõ | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 · [Autocomplete.md](../Components/Autocomplete.md) §Trạng thái |
| Nhãn chip lọc "Vai trò" | Tên mục vừa chọn. URL mở sẵn `roleId` mà chưa chọn gì (tải lại, liên kết chia sẻ): `GET /api/v1/core/roles/{id}` lấy `name` cho chip và ô lọc — song song request danh sách, cùng quyền `core.role.read`. Chip chỉ vẽ khi đã có tên; tra hỏng → không vẽ chip, request tra tắt toast | [roles.md](../../contracts/roles.md), endpoint `GET /api/v1/core/roles/{id}` |

**Hộp thoại "Thêm người dùng"** — `Dialog` cỡ `md`:

```text
- Dialog md, dirty khi đã gõ bất kỳ ô nào
  - thân
    - NoticeBanner sm vai danger (lỗi không gắn được vào ô)
    - FormRow required "Tên đăng nhập" — Input text, autocomplete off
    - FormRow required "Email" — Input text, inputmode email, autocomplete off
    - FormRow required "Họ tên" — Input text
    - FormRow required "Mật khẩu tạm" — Input password, revealable (mặc định), autocomplete new-password; không có ô nhập lại; gợi ý: câu chính sách tĩnh
    - FormRow "Vai trò" — Autocomplete multiple, tìm qua searchText (mục "Ô chọn vai trò")
        chỉ hiện khi có CẢ core.user.role.assign (card §5) VÀ core.role.read (nguồn tìm)
  - chân: Button secondary "Huỷ" · Button primary "Tạo người dùng" (loading; loadingBlocksClose)
```

Tạo xong: đóng hộp, `Toast` `success`, tải lại danh sách. Mật khẩu tạm không hiện lại ở đâu (card §5). Tài khoản mới mang `tenant_id` của người tạo — **không** có ô chọn đơn vị ([N1](../../luong/N1-tao-nguoi-dung-moi.md) §5).

### Câu chữ

Câu không ghi nguồn: người dùng duyệt 2026-09-15 ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề màn, tiêu đề tab | Người dùng | `nguoiDung.tieuDe` |
| Mô tả dưới tiêu đề | Tài khoản của đơn vị: tạo mới, gán vai trò, khoá và đặt lại mật khẩu. | `nguoiDung.moTa` |
| Nút chính | Thêm người dùng | `nguoiDung.hanhDong.them` |
| Nhãn ô tìm (ẩn) | Tìm theo tên đăng nhập, họ tên hoặc email | `nguoiDung.timKiem.nhan` |
| Nhãn hai ô lọc | Vai trò · Trạng thái | `nguoiDung.loc.vaiTro` · `nguoiDung.loc.trangThai` |
| Placeholder ô chọn vai trò (ô lọc, hộp tạo, hộp gán) | Gõ tên vai trò để tìm | `nguoiDung.vaiTro.goiYTim` |
| Dòng phụ kết quả tìm — vai trò hệ thống | Vai trò hệ thống | `vaiTro.loai.heThong` |
| Chip đơn vị hiện hành (dùng chung với [11-vai-tro.md](./11-vai-tro.md)) | Đơn vị: {{tenDonVi}} — tham số từ `tenantName` của phiên | `chung.loc.donViHienHanh` |
| `Tooltip` lý do khoá chip đơn vị | Danh sách chỉ gồm dữ liệu của đơn vị bạn đang đăng nhập. | `chung.loc.lyDoDonVi` |
| Giá trị lọc trạng thái | Hoạt động · Đã khoá | `nguoiDung.trangThai.hoatDong` · `nguoiDung.trangThai.daKhoa` |
| `<caption>` bảng (ẩn) | Danh sách người dùng | `nguoiDung.bang.caption` |
| Cột | Tên đăng nhập · Họ tên · Email · Vai trò · Trạng thái · Ngày tạo | `nguoiDung.cot.tenDangNhap` · `nguoiDung.cot.hoTen` · `nguoiDung.cot.email` · `nguoiDung.cot.vaiTro` · `nguoiDung.cot.trangThai` · `nguoiDung.cot.ngayTao` |
| Ô vai trò rỗng | Chưa có vai trò | `nguoiDung.vaiTro.chuaCo` |
| Liên kết ở ô tên đăng nhập | {{tenDangNhap}} — dữ liệu, tự mô tả; không `aria-label` riêng | — |
| Tiêu đề hộp tạo | Thêm người dùng | `nguoiDung.form.tieuDeTao` |
| Nhãn trường | Tên đăng nhập · Email · Họ tên · Mật khẩu tạm · Vai trò | `nguoiDung.form.tenDangNhap` · `nguoiDung.form.email` · `nguoiDung.form.hoTen` · `nguoiDung.form.matKhauTam` · `nguoiDung.form.vaiTro` |
| Gợi ý mật khẩu tạm (dùng chung với [20-don-vi.md](./20-don-vi.md)) | Mật khẩu tạm phải đạt chính sách mật khẩu của hệ thống. Người nhận phải đổi nó ở lần đăng nhập đầu. — câu tĩnh, không chép số của chính sách | `xacThuc.matKhauTam.goiY` |
| Nút hiện/ẩn mật khẩu tạm (hộp tạo, hộp đặt lại; dùng chung với [01-dang-nhap.md](./01-dang-nhap.md)) | Hiện mật khẩu ↔ Ẩn mật khẩu | `xacThuc.matKhau.hien` · `xacThuc.matKhau.an` |
| Nút hộp tạo | Huỷ · Tạo người dùng | `chung.huy` · `nguoiDung.form.xacNhanTao` |
| Toast tạo thành công (nhắc bước 5–6 của [N1](../../luong/N1-tao-nguoi-dung-moi.md)) | Đã tạo tài khoản {{tenDangNhap}}. Chuyển mật khẩu tạm cho người dùng, và gán vai trò nếu chưa gán. | `nguoiDung.thongBao.taoThanhCong` |
| Rỗng — chưa có người dùng | Chưa có người dùng nào · Thêm người dùng đầu tiên của đơn vị. | `nguoiDung.trong.tieuDe` · `nguoiDung.trong.moTa` |
| Rỗng — lọc không ra (điều kiện đang lọc vẫn hiện ở hàng chip) | Không có người dùng nào khớp bộ lọc hiện tại · Đổi từ khoá hoặc xoá bộ lọc để xem lại toàn bộ danh sách. | `nguoiDung.khongKetQua.tieuDe` · `nguoiDung.khongKetQua.moTa` |
| Tiêu đề khối lỗi tải danh sách | Không tải được danh sách người dùng | `nguoiDung.loi.taiThatBai` |
| Thân khối lỗi | Câu dịch theo mã; không có mã thì câu mất kết nối | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` |
| Nút thử lại · nút xoá bộ lọc | Thử lại · Xoá bộ lọc | `chung.thuLai` · `chung.xoaBoLoc` |

Mã lỗi → chỗ hiện (câu sống ở `loi.<mã>`):

| Mã | Endpoint | Hiện ở |
| --- | --- | --- |
| `CORE.VALIDATION.FAILED` — `Page` / `PageSize` / `SortBy` / `SearchText` | danh sách | `errorTemplate` của `DataTable` |
| `CORE.USER.ROLE_NOT_FOUND` | danh sách (`roleId` trên URL) | `errorTemplate`, nút "Xoá bộ lọc" thay cho "Thử lại" |
| `CORE.ROLE.NOT_FOUND` · mọi mã khác, kể cả mất kết nối | tra tên vai trò theo `roleId` | Không vẽ chip; không toast — cùng sự cố, request danh sách nhận `CORE.USER.ROLE_NOT_FOUND` và đi dòng trên |
| `CORE.VALIDATION.FAILED` — `UserName` / `Email` / `FullName` / `TempPassword` / `RoleIds` | tạo | Dòng lỗi dưới ô tương ứng |
| `CORE.USER.USERNAME_DUPLICATED` · `CORE.USER.EMAIL_DUPLICATED` | tạo | Dòng lỗi dưới ô "Tên đăng nhập" / "Email" — card khai `messageParams`, không khai `fieldErrors`; FE tự gắn mã vào ô |
| `CORE.USER.CREATE_FAILED` | tạo | Theo `fieldErrors`; khoá không khớp → `NoticeBanner` trong hộp ([04-f3-man-quan-tri.md](../../wiki-core/fe/trien-khai/04-f3-man-quan-tri.md) §3.4) |
| `CORE.USER.ROLE_NOT_FOUND` | tạo | `NoticeBanner` `danger` trong hộp (không kèm `fieldErrors`) |
| `CORE.USER.ROLE_ESCALATION_FORBIDDEN` | tạo | `NoticeBanner` `danger` trong hộp, hộp giữ nguyên. Mã 403 nghiệp vụ: interceptor không toast, màn tự hiện ([fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2) |
| Mọi mã, kể cả mất kết nối | tìm vai trò (ô chọn) | `errorTemplate` của `Autocomplete` |
| `CORE.AUTH.FORBIDDEN` | mọi endpoint | Đường chung: toast, làm mới tập quyền và menu ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8) |

### Trạng thái

- **mặc định:** `DataTable` có dữ liệu; `Pagination` hiện "x–y trên tổng z".
- **đang tải:** lần đầu — `loadingTemplate`, `SkeletonLoader` dạng hàng; `Toolbar` bấm và gõ được. Đổi trang/sắp xếp/lọc — giữ dữ liệu cũ, phủ mờ, bảng không nhảy. Ô chọn vai trò chờ kết quả → `Autocomplete` `loading`, ô **không** bị khoá ([Autocomplete.md](../Components/Autocomplete.md) §Trạng thái).
- **rỗng (chưa có người dùng nào):** `EmptyState` `first-use`, nút primary "Thêm người dùng" (chỉ khi có `core.user.write`). Trên dữ liệu hợp lệ ca này không xảy ra (người xem luôn là một người dùng của đơn vị) — vẫn khai để màn không trắng khi dữ liệu lệch.
- **rỗng (lọc không ra kết quả):** `EmptyState` `no-results`, nút secondary "Xoá bộ lọc"; **không** mời tạo mới. Hàng chip giữ nguyên.
- **lỗi:** tải hỏng → `errorTemplate`, tiêu đề cột giữ nguyên. `ROLE_NOT_FOUND` theo bảng mã lỗi. Hết phiên đi đường chung ([00-khung-ung-dung.md](./00-khung-ung-dung.md)).
- **kiểm tra dữ liệu:** trong hộp tạo — lỗi từng ô dưới ô, lỗi còn lại ở `NoticeBanner` đầu thân hộp; sửa ô đang lỗi thì lỗi server của ô đó biến mất; bấm "Tạo người dùng" hai lần chỉ tạo một bản ghi.

### Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | Đủ sáu cột; `Toolbar` một hàng: ô tìm, hai ô lọc, hàng chip bên dưới |
| `$bp-md` … `$bp-lg` | Ẩn "Ngày tạo" (`priority: low`) và "Email" — cả hai có ở màn chi tiết |
| < `$bp-md` | Giữ "Tên đăng nhập" (họ tên đứng dưới trong cùng ô, liên kết vẫn là tên đăng nhập), "Trạng thái"; "Vai trò" ẩn — có ở màn chi tiết. `Toolbar` xuống dòng: ô tìm, rồi hai ô lọc, rồi dải chip cuộn ngang |
| < `$bp-xs` | `DataTable` dạng thẻ, đủ mọi trường. Hộp tạo dính đáy theo `Dialog` |

### Icon

Theo [Icons.md](../Icons.md) §5: `pi-plus` (thêm), `pi-search` (ô tìm), `pi-times` (gỡ chip), `pi-filter-slash` (xoá bộ lọc), `pi-refresh` (thử lại), `pi-sort-*` (`th`), `pi-eye` · `pi-eye-slash` (hiện/ẩn "Mật khẩu tạm"). Icon khoá của chip đơn vị và icon đang tải của ô chọn vai trò do [FilterChip.md](../Components/FilterChip.md), [Autocomplete.md](../Components/Autocomplete.md) tự mang.

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn.

---

## Chi tiết người dùng (`/quan-tri/nguoi-dung/:id`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md
  - main — đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm); cuộn dọc bình thường (không phải màn danh sách)
    - PageHeader biến thể detail
      - nút quay lại → /quan-tri/nguoi-dung kèm query param cũ (tuyến cha, không history.back)
      - tiêu đề = fullName; Badge trạng thái cạnh tiêu đề (danger "Đã khoá" / success "Hoạt động")
      - nhóm hành động:
          Button primary "Sửa" (core.user.write)
          Button secondary "Đặt lại mật khẩu" (core.user.reset-password) — không vẽ khi xem chính mình
          Button secondary "Khoá tài khoản" khi isLocked = false (core.user.lock) — không vẽ khi xem chính mình
          Button secondary "Mở khoá" khi isLocked = true (core.user.lock)
    - NoticeBanner md vai warning — chỉ khi mustChangePassword = true
    - Card md "Thông tin tài khoản" — FormRow biến thể inline, chỉ đọc
      - Tên đăng nhập — userName
      - Email — email
      - Họ tên — fullName
      - Trạng thái — Badge như ở tiêu đề
      - Khoá — chỉ hiện khi isLocked = true: lockedByAdmin = true → "Quản trị khoá"; false → "Tự khoá tới {{lockoutEnd}}" (ngày giờ)
      - Buộc đổi mật khẩu — mustChangePassword
      - Ngày tạo — createdAt
    - Card md "Vai trò" — khe --sp-8 với Card trên
      - thân: danh sách roles[] — mỗi vai trò một Badge neutral cỡ md; isSystem kèm Badge outline "Vai trò hệ thống"
              roles rỗng → EmptyState cỡ compact, câu "Chưa có vai trò" (N1 §4)
      - chân: Button secondary "Gán vai trò" (core.user.role.assign VÀ core.role.read)
  - Footer
```

Dữ liệu từ `GET /api/v1/core/users/{id}` — cùng hình dạng một phần tử của danh sách (card §4). Không có thao tác xoá: vô hiệu hoá đi qua khoá (card §10).

**Hộp thoại "Sửa người dùng"** — `Dialog` cỡ `sm`:

```text
- Dialog sm — tiêu đề nêu userName; dirty khi đã đổi ô
  - thân
    - NoticeBanner sm vai danger (lỗi không gắn được vào ô)
    - FormRow "Tên đăng nhập" — Input text readonly (không nằm trong request, card §6)
    - FormRow required "Email" — Input text, inputmode email
    - FormRow required "Họ tên" — Input text
  - chân: Button secondary "Huỷ" · Button primary "Lưu" (loading; loadingBlocksClose)
```

**Hộp thoại "Gán vai trò"** — `Dialog` cỡ `md`. Gửi **toàn bộ** tập mong muốn, không gửi thêm/bớt (card §7):

```text
- Dialog md — tiêu đề nêu userName; dirty khi tập chọn khác tập hiện có
  - thân
    - NoticeBanner sm vai danger (lỗi)
    - FormRow "Vai trò" — Autocomplete multiple, tìm qua searchText (mục "Ô chọn vai trò" ở màn danh sách)
        giá trị ban đầu = roles[] hiện có, trừ phần đứng riêng ở dòng dưới
    - CHỈ khi xem chính mình và roles[] có vai trò isSystem:
        FormRow inline "Vai trò hệ thống của bạn" — Badge neutral cỡ sm mỗi vai trò đó; không gỡ được, không nằm trong ô chọn
  - chân: Button secondary "Huỷ" · Button primary "Lưu vai trò" (loading; loadingBlocksClose)
```

| Quyết định | Căn cứ |
| --- | --- |
| "Lưu vai trò" mà tập mới **bỏ ít nhất một vai trò** đang có → `ConfirmDialog` `warning` trên hộp gán, câu mang **số** vai trò bị gỡ; không bỏ → gửi ngay | [04-f3-man-quan-tri.md](../../wiki-core/fe/trien-khai/04-f3-man-quan-tri.md) §3.5 |
| Xem chính mình: vai trò hệ thống đang giữ đứng **ngoài** ô chọn, chỉ đọc, không có nút gỡ. Tập gửi = ô chọn + vai trò đứng ngoài; kết quả tìm bỏ các vai trò đứng ngoài để không chọn trùng (`CORE.USER.DUPLICATE_ROLE_ENTRY`). "Chính mình" định nghĩa ở mục Phân quyền theo nút | [users.md](../../contracts/users.md) §2 luật 2, §7 |
| "Khoá tài khoản" → `ConfirmDialog` `warning` (khắc phục được bằng mở khoá). Câu nêu đúng nhịp: phiên đang mở của người bị khoá bị chấm dứt **ở request kế tiếp** — không viết như thể đã đăng xuất ngay | [users.md](../../contracts/users.md) §8 · [04-f3-man-quan-tri.md](../../wiki-core/fe/trien-khai/04-f3-man-quan-tri.md) §3.2 |
| "Mở khoá" không hỏi xác nhận (thao tác khôi phục); gửi ngay, `Toast` `success` | — |
| "Đặt lại mật khẩu" là `Dialog` cỡ `sm`, không phải `ConfirmDialog` vì cần nhập liệu; chấm dứt phiên người bị đặt lại cùng nhịp với khoá | [users.md](../../contracts/users.md) §9 |

**Hộp thoại "Đặt lại mật khẩu"**:

```text
- Dialog sm — tiêu đề nêu userName
  - thân
    - NoticeBanner sm vai warning — hệ quả với người bị đặt lại
    - NoticeBanner sm vai danger (lỗi không gắn được vào ô)
    - FormRow required "Mật khẩu tạm" — Input password, revealable (mặc định), autocomplete new-password; không có ô nhập lại; gợi ý: câu chính sách tĩnh
  - chân: Button secondary "Huỷ" · Button primary "Đặt mật khẩu tạm" (loading; loadingBlocksClose)
```

Mọi thao tác xong: đóng hộp, `Toast` `success`, tải lại chi tiết.

### Câu chữ

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề tab | Chi tiết người dùng | `nguoiDung.chiTiet` |
| Tiêu đề trang | {{fullName}} — dữ liệu | — |
| `aria-label` nút quay lại | Quay lại danh sách người dùng | `nguoiDung.chiTietTrang.quayLai` |
| Đường dẫn phân cấp | Người dùng | `nguoiDung.tieuDe` |
| Nút | Sửa · Đặt lại mật khẩu · Khoá tài khoản · Mở khoá · Gán vai trò | `nguoiDung.hanhDong.sua` · `nguoiDung.hanhDong.datLaiMatKhau` · `nguoiDung.hanhDong.khoa` · `nguoiDung.hanhDong.moKhoa` · `nguoiDung.hanhDong.ganVaiTro` |
| Banner buộc đổi mật khẩu | Người dùng này phải đổi mật khẩu ở lần đăng nhập kế tiếp. | `nguoiDung.chiTietTrang.bannerDoiMatKhau` |
| Tiêu đề hai `Card` | Thông tin tài khoản · Vai trò | `nguoiDung.chiTietTrang.theThongTin` · `nguoiDung.chiTietTrang.theVaiTro` |
| Nhãn trường chỉ đọc | Tên đăng nhập · Email · Họ tên · Trạng thái · Khoá · Buộc đổi mật khẩu · Ngày tạo | `nguoiDung.cot.tenDangNhap` · `nguoiDung.cot.email` · `nguoiDung.cot.hoTen` · `nguoiDung.cot.trangThai` · `nguoiDung.chiTietTrang.khoa` · `nguoiDung.chiTietTrang.buocDoiMatKhau` · `nguoiDung.cot.ngayTao` |
| Giá trị ô "Khoá" theo `lockedByAdmin` ([users.md](../../contracts/users.md)) | Quản trị khoá · Tự khoá tới {{lockoutEnd}} | `nguoiDung.khoa.boiQuanTri` · `nguoiDung.khoa.tuDong` |
| Giá trị cờ | Có · Không | `chung.co` · `chung.khong` |
| Badge vai trò hệ thống (dùng chung với [11-vai-tro.md](./11-vai-tro.md)) · không có vai trò | Vai trò hệ thống · Chưa có vai trò | `vaiTro.loai.heThong` · `nguoiDung.vaiTro.chuaCo` |
| Tiêu đề khối lỗi tải chi tiết | Không tải được thông tin người dùng | `nguoiDung.chiTietTrang.loiTai` |
| Tiêu đề hộp sửa | Sửa người dùng {{tenDangNhap}} | `nguoiDung.form.tieuDeSua` |
| Tiêu đề hộp gán | Gán vai trò cho {{tenDangNhap}} | `nguoiDung.ganVaiTro.tieuDe` |
| Nút hộp gán | Lưu vai trò | `nguoiDung.ganVaiTro.xacNhan` |
| Xác nhận gỡ vai trò — tiêu đề | Gỡ {{soLuong}} vai trò của {{tenDangNhap}}? | `nguoiDung.goVaiTro.tieuDe` |
| Xác nhận gỡ vai trò — mô tả · nút | Người dùng mất quyền của các vai trò bị gỡ từ thao tác kế tiếp của họ. · Lưu và gỡ vai trò | `nguoiDung.goVaiTro.moTa` · `nguoiDung.goVaiTro.xacNhan` |
| Xác nhận khoá — tiêu đề | Khoá tài khoản {{tenDangNhap}}? | `nguoiDung.khoa.tieuDe` |
| Xác nhận khoá — mô tả · nút | Tài khoản sẽ không đăng nhập được. Phiên đang mở của tài khoản bị chấm dứt ở thao tác kế tiếp của người đó. Mở khoá được bất cứ lúc nào. · Khoá tài khoản | `nguoiDung.khoa.moTa` · `nguoiDung.khoa.xacNhan` |
| Tiêu đề hộp đặt lại | Đặt lại mật khẩu cho {{tenDangNhap}} | `nguoiDung.datLai.tieuDe` |
| Banner hệ quả đặt lại | Phiên đang mở của {{tenDangNhap}} bị chấm dứt ở thao tác kế tiếp của người đó. Người dùng phải đổi mật khẩu tạm ở lần đăng nhập đầu. | `nguoiDung.datLai.canhBao` |
| Gợi ý mật khẩu tạm · nút hộp đặt lại | Như màn danh sách · Đặt mật khẩu tạm | `xacThuc.matKhauTam.goiY` · `nguoiDung.datLai.xacNhan` |
| Toast sửa · gán · khoá · mở khoá · đặt lại | Đã lưu thông tin người dùng. · Đã cập nhật vai trò của {{tenDangNhap}}. · Đã khoá tài khoản {{tenDangNhap}}. · Đã mở khoá tài khoản {{tenDangNhap}}. · Đã đặt mật khẩu tạm cho {{tenDangNhap}}. | `nguoiDung.thongBao.suaThanhCong` · `nguoiDung.thongBao.ganVaiTroThanhCong` · `nguoiDung.thongBao.khoaThanhCong` · `nguoiDung.thongBao.moKhoaThanhCong` · `nguoiDung.thongBao.datLaiThanhCong` |
| Không tìm thấy người dùng — tiêu đề thân · mô tả · đường đi tiếp | Không tìm thấy người dùng này. · Đường dẫn có thể đã cũ. Quay lại danh sách để tìm người dùng. · Về danh sách người dùng | `nguoiDung.chiTietTrang.khongTimThay` · `nguoiDung.chiTietTrang.khongTimThayMoTa` · `nguoiDung.chiTietTrang.veDanhSach` |

Mã lỗi → chỗ hiện (câu sống ở `loi.<mã>`):

| Mã | Endpoint | Hiện ở |
| --- | --- | --- |
| `CORE.USER.NOT_FOUND` | xem | Thay thân trang — mục Trạng thái, dòng lỗi |
| `CORE.USER.NOT_FOUND` | sửa, gán, khoá, mở khoá, đặt lại | `NoticeBanner` `danger` trong hộp đang mở; ca mở khoá (không có hộp) → `Toast` `danger` |
| `CORE.CONCURRENCY.CONFLICT` (409) | sửa, khoá, mở khoá, đặt lại — request gửi `version` của `GET` chi tiết gần nhất ([users.md](../../contracts/users.md) §6, §8, §9) | `NoticeBanner` `warning` trong hộp đang mở + nút **Tải lại**: `GET` chi tiết lấy `version` mới, hộp nạp giá trị mới, người dùng xác nhận lại; ca mở khoá (không có hộp) → `Toast` `warning` rồi tải lại trang chi tiết. Không tự gộp, không gửi lại `version` cũ — cùng ca với [03-ho-so-ca-nhan.md](./03-ho-so-ca-nhan.md) §Trạng thái |
| `CORE.VALIDATION.FAILED` — `Email` / `FullName` | sửa | Dòng lỗi dưới ô |
| `CORE.USER.EMAIL_DUPLICATED` | sửa | Dòng lỗi dưới ô "Email" (FE tự gắn mã vào ô) |
| `CORE.USER.UPDATE_FAILED` | sửa | Theo `fieldErrors`; khoá không khớp → `NoticeBanner` trong hộp |
| `CORE.VALIDATION.FAILED` · `CORE.USER.DUPLICATE_ROLE_ENTRY` | gán | `NoticeBanner` `danger` — lỗi dựng payload của FE, không gắn vào ô |
| `CORE.USER.ROLE_NOT_FOUND` | gán | `NoticeBanner` `danger`, hộp giữ nguyên — một vai trò trong tập vừa gửi không còn tồn tại |
| `CORE.USER.ROLE_ESCALATION_FORBIDDEN` · `CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN` | gán | `NoticeBanner` `danger` trong hộp gán, hộp giữ nguyên. Mã 403 nghiệp vụ, màn tự hiện ([fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2) |
| `CORE.USER.CANNOT_LOCK_SELF` · `CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN` · `CORE.USER.LOCK_FAILED` | khoá | `NoticeBanner` `danger` trong `ConfirmDialog`, hộp không tự đóng |
| `CORE.USER.LOCK_FAILED` | mở khoá | `Toast` `danger` (không có hộp) |
| `CORE.VALIDATION.FAILED` — `TempPassword` · `CORE.USER.RESET_PASSWORD_FAILED` | đặt lại | Dòng lỗi dưới ô "Mật khẩu tạm" theo `fieldErrors` |
| `CORE.USER.RESET_PASSWORD_TARGET_FORBIDDEN` · `CORE.USER.CANNOT_RESET_OWN_PASSWORD` | đặt lại | `NoticeBanner` `danger` trong hộp. Mã 403 nghiệp vụ, màn tự hiện |
| `CORE.AUTH.FORBIDDEN` | mọi endpoint | Đường chung ở [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8 |

### Trạng thái

- **mặc định:** tiêu đề, badge, hai `Card` có dữ liệu; nút theo quyền.
- **đang tải:** `PageHeader` `loading` — `SkeletonLoader` thay tiêu đề, giữ đúng chiều cao dòng tiêu đề; nút quay lại hiện ngay; nhóm nút ẩn cho tới khi biết `isLocked`. Hai `Card` `loading`. Tải lại sau thao tác: phủ `--color-scrim` lên thân `Card`, không thay bằng skeleton.
- **rỗng:** không áp dụng cho cả màn — một bản ghi luôn có trường bắt buộc. `Card` "Vai trò" rỗng hiện "Chưa có vai trò" như sơ đồ. Không có ca "lọc không ra".
- **lỗi:** `NOT_FOUND` → thân trang là `EmptyState` biến thể **`record-not-found`** ([EmptyState.md](../Components/EmptyState.md) §Biến thể — **không** phải `not-found`, biến thể đó nói về đường dẫn), cỡ `page`, `headingLevel` 2, câu riêng ở bảng Câu chữ; `actionRoute` trỏ về danh sách nên đường đi tiếp là liên kết. `PageHeader` giữ nút quay lại, tiêu đề là tiêu đề tab, không có nhóm nút. Cùng quyết định phần thân với [02-trang-loi.md](./02-trang-loi.md), khác biến thể. Tải hỏng lý do khác → `Card` `error` (`NoticeBanner` `danger` + thử lại) cho cả hai thẻ; `PageHeader` vẫn hiện để quay lại được.
- **kiểm tra dữ liệu:** trong ba hộp thoại — lỗi từng ô dưới ô, lỗi còn lại ở `NoticeBanner` đầu thân hộp; gửi hai lần chỉ gửi một lần.

### Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Hai `Card` xếp dọc, cùng bề rộng `main`; `FormRow` `inline` |
| `$bp-sm` … `$bp-md` | Nhóm nút `PageHeader` xuống hàng dưới tiêu đề |
| < `$bp-md` | `FormRow` tự chuyển `stacked` |
| < `$bp-xs` | Chỉ giữ "Sửa" ngoài `PageHeader`; "Đặt lại mật khẩu", "Khoá tài khoản" / "Mở khoá" dồn vào `Menu` theo [PageHeader.md](../Components/PageHeader.md) — không mất thao tác nào. Hộp thoại dính đáy |

### Icon

Theo [Icons.md](../Icons.md) §5.

| Chỗ | Icon |
| --- | --- |
| Nút quay lại · "Sửa" · "Đặt lại mật khẩu" | `pi-arrow-left` · `pi-pencil` · `pi-key` |
| "Khoá tài khoản" · "Mở khoá" | `pi-lock` · `pi-lock-open` |
| Nút hiện/ẩn ô "Mật khẩu tạm" | `pi-eye` · `pi-eye-slash` |
| Thân trang khi không tìm thấy người dùng | `pi-compass` — mặc định của biến thể `record-not-found`, màn không truyền `icon` |
| "Gán vai trò" | Không có icon — Icons.md §5 không có dòng cho hành động này, không thêm icon trang trí |

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn.

---

## Phân quyền theo nút — cả hai màn

Khoá lấy từ dòng `Quyền:` của card. Thiếu khoá thì **không vẽ** phần tử (directive `appHasPermission`, [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.4) — không vẽ nút mờ.

| Phần tử | Khoá | Card |
| --- | --- | --- |
| Vào hai màn | `core.user.read` | [users.md](../../contracts/users.md) §3, §4 |
| "Thêm người dùng", "Sửa" | `core.user.write` | §5, §6 |
| Ô lọc "Vai trò", tra tên vai trò theo `roleId` trên URL | `core.role.read` | [roles.md](../../contracts/roles.md) §1; `GET /api/v1/core/roles/{id}` cùng card |
| Ô `Autocomplete` `multiple` "Vai trò" trong hộp tạo, nút "Gán vai trò" | `core.user.role.assign` **và** `core.role.read` | §5, §7 · [roles.md](../../contracts/roles.md) §1 |
| "Khoá tài khoản", "Mở khoá" | `core.user.lock` | §8 |
| "Đặt lại mật khẩu" | `core.user.reset-password` | §9 |

**Xem chính mình** — `id` trên tuyến trùng `id` của phiên ([auth.md](../../contracts/auth.md) §5): không vẽ "Khoá tài khoản" và "Đặt lại mật khẩu" kể cả khi có khoá quyền; vai trò hệ thống đang giữ đứng ngoài ô chọn ở hộp gán. BE vẫn chặn cả ba (luật 2, 3, 5 ở [users.md](../../contracts/users.md) §2, HTTP 422) nên bảng mã lỗi của màn chi tiết giữ chỗ hiện cho các mã đó.

## Điểm mở rộng cho dự án hạ nguồn

Theo [ListScreen.md](../Templates/ListScreen.md) §5 và seam `CORE_SCREEN_EXT` khoá `'users'` ([fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.7): màn danh sách nhận thêm cột (nối **sau** "Ngày tạo"), trường lọc, hành động hàng (màn Core không có cột hành động; dự án thêm thì cột đó đứng cuối), nút vào `Toolbar`. Không bớt cột nào. Màn chi tiết chưa có điểm mở rộng — seam hôm nay chỉ nói về màn danh sách.
