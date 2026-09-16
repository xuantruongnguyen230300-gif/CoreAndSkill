---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Hồ sơ cá nhân — màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`. Màn **sẽ được dựng** ở pha F2.

Mọi người dùng đã đăng nhập vào được từ menu tài khoản trên `Topbar` ([00-khung-ung-dung.md](./00-khung-ung-dung.md)): xem hồ sơ của mình; sửa họ tên, số điện thoại, ngôn ngữ ưa thích; **tự đổi mật khẩu**. Tài khoản mang cờ đặc quyền thấy thêm lời nhắc và khối **tự từ bỏ** cờ. Nghiệp vụ: [N9](../../luong/N9-sua-ho-so-ca-nhan.md), [V1](../../luong/V1-cai-dat-lan-dau.md) bước 9, [profile.md](../../contracts/profile.md), [auth.md](../../contracts/auth.md) §6.

> **Khung:** khung ứng dụng ([00-khung-ung-dung.md](./00-khung-ung-dung.md)).
> **Quyền:** không cần permission — ba endpoint hồ sơ và endpoint đổi mật khẩu mang `[AuthenticatedOnly]` ([profile.md](../../contracts/profile.md) §1–§3 · [auth.md](../../contracts/auth.md) §6 · [ADR-0024](../../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md)). Tuyến qua `authGuard` và `mustChangePasswordGuard`, **không** qua `permissionGuard` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §4).

---

## Hồ sơ cá nhân (`/ho-so`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md
  - main — rộng tối đa --layout-container-max, đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm)
    - PageHeader `minimal` — tiêu đề <h1>; khe dưới --sp-8
    - cột nội dung — bề rộng tối đa --layout-form-w, căn trái; các khối xếp dọc, khe --sp-8
      - NoticeBanner `md` vai `warning` — nhắc từ bỏ cờ đặc quyền
          CHỈ render khi GET hồ sơ trả hasPermissionBypass = true
      - Card `default`, cỡ `md` (đệm --sp-6) — tiêu đề <h2> "Thông tin cá nhân"
        - thân: form, một cột, khe giữa hai FormRow --sp-6
          - FormRow — Tên đăng nhập     · Input `text`, `readonly`
          - FormRow — Email             · Input `text`, `readonly` · gợi ý: đổi qua quản trị
          - FormRow `required` — Họ tên · Input `text`, autocomplete `name`
          - FormRow — Số điện thoại     · Input `text`, autocomplete `tel`, inputmode `tel`
          - FormRow — Ngôn ngữ ưa thích · Input `select`: một lựa chọn rỗng (= mặc định hệ thống) + danh sách ngôn ngữ
        - chân Card: Button `primary`, type `submit` — Lưu
      - Card `default`, cỡ `md` — tiêu đề <h2> "Đổi mật khẩu"
        - thân: form, một cột
          - FormRow `required` — Mật khẩu hiện tại     · Input `password`, autocomplete `current-password`
          - FormRow `required` — Mật khẩu mới          · Input `password`, autocomplete `new-password` · gợi ý: câu chính sách tĩnh
          - FormRow `required` — Nhập lại mật khẩu mới · Input `password`, autocomplete `new-password`
        - chân Card: Button `primary`, type `submit` — Đổi mật khẩu
      - Card `default`, cỡ `md` — tiêu đề <h2> "Cờ đặc quyền"
          CHỈ render khi GET hồ sơ trả hasPermissionBypass = true
        - thân: mô tả hệ quả
        - Button `secondary` — Từ bỏ cờ đặc quyền → mở ConfirmDialog
  - Footer `full`
- ConfirmDialog `danger` — lớp nổi, xác nhận từ bỏ cờ
```

| Quyết định bố cục | Căn cứ |
| --- | --- |
| Cột nội dung rộng tối đa `--layout-form-w` (bí danh bề rộng `Dialog` cỡ `md`) | [DESIGN.md](../DESIGN.md) §6.1 |
| Tên đăng nhập và email là `readonly`, không `disabled`; `PUT` chỉ gửi ba trường sửa được | [profile.md](../../contracts/profile.md) §1, §2 · [Input.md](../Components/Input.md) §Trạng thái |
| Họ tên mang dấu bắt buộc | [profile.md](../../contracts/profile.md) §2, bảng lỗi |
| Đổi mật khẩu là `Card` và form riêng, nút gửi riêng; mỗi `Card` mang đúng một `primary` | [profile.md](../../contracts/profile.md) §2 · [auth.md](../../contracts/auth.md) §6 · [Button.md](../Components/Button.md) §Biến thể |
| Ô mật khẩu là `Input` `password` (không phải `AuthField`), `revealable` bật sẵn, màn không tắt | [AuthField.md](../Components/AuthField.md) §Khi nào dùng · [Input.md](../Components/Input.md) §Biến thể |
| Có ô nhập lại mật khẩu mới, kiểm khớp ở FE; gợi ý chính sách là câu tĩnh | [01-dang-nhap.md](./01-dang-nhap.md), màn đổi mật khẩu bắt buộc |
| Các `Card` xếp dọc, không lồng nhau; khe `--sp-8` | [Card.md](../Components/Card.md) · [DESIGN.md](../DESIGN.md) §4 |
| Lời nhắc từ bỏ cờ là `NoticeBanner` `warning` đầu cột; thao tác từ bỏ ở `Card` riêng | [V1](../../luong/V1-cai-dat-lan-dau.md) §6 · [NoticeBanner.md](../Components/NoticeBanner.md) §Khi nào dùng |
| Nút mở hộp là `secondary`; chỉ nút xác nhận **trong** hộp là `danger` | [Button.md](../Components/Button.md) §Biến thể |
| Hộp xác nhận mức `danger` — từ bỏ cờ là thao tác **một chiều** | [profile.md](../../contracts/profile.md) §3 · [ConfirmDialog.md](../Components/ConfirmDialog.md) §Biến thể |
| Form một cột ở mọi ngưỡng, không dùng `form-grid` | [FormRow.md](../Components/FormRow.md) §Responsive |

### Câu chữ

Câu không ghi nguồn: người dùng duyệt 2026-09-15 ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n | Nguồn |
| --- | --- | --- | --- |
| Tiêu đề trang (`title` tuyến, tiêu đề trên `Topbar`, `<h1>`) · tiêu đề `Card` thông tin | Hồ sơ cá nhân · Thông tin cá nhân | `hoSo.tieuDe` · `hoSo.thongTin.tieuDe` | [profile.md](../../contracts/profile.md) tên card, đoạn mở đầu |
| Nhãn tên đăng nhập · email | Tên đăng nhập · Email | `hoSo.truong.tenDangNhap` · `hoSo.truong.email` | [auth.md](../../contracts/auth.md) §3 · [profile.md](../../contracts/profile.md) §1 |
| Gợi ý dưới email | Muốn đổi email, liên hệ quản trị viên của đơn vị. | `hoSo.goiY.doiQuaQuanTri` | duyệt · [profile.md](../../contracts/profile.md) §1, §2 Ghi chú |
| Nhãn họ tên · số điện thoại · ngôn ngữ | Họ tên · Số điện thoại · Ngôn ngữ ưa thích | `hoSo.truong.hoTen` · `hoSo.truong.soDienThoai` · `hoSo.truong.ngonNguUaThich` | [N9](../../luong/N9-sua-ho-so-ca-nhan.md) §3 |
| Lựa chọn rỗng của ô ngôn ngữ | Theo mặc định của hệ thống | `hoSo.ngonNgu.macDinhHeThong` | duyệt · [profile.md](../../contracts/profile.md) §1 |
| Tên từng ngôn ngữ | Dữ liệu seam `CORE_I18N`, không qua khoá; viết bằng **chính ngôn ngữ đó**, mỗi lựa chọn mang `lang` (cùng luật [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Accessibility) | — | [08-i18n.md](../../wiki-core/fe/08-i18n.md) §2.3 |
| Nút lưu · nút thử lại | Lưu · Thử lại | `chung.luu` · `chung.thuLai` | [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §5.3 · [Card.md](../Components/Card.md) §Trạng thái |
| Toast lưu thành công · tiêu đề banner lỗi tải | Đã lưu hồ sơ. · Không tải được hồ sơ | `hoSo.thongBao.luuThanhCong` · `hoSo.loi.taiThatBai` | duyệt |
| Thân banner lỗi tải, lỗi lưu, lỗi đổi mật khẩu | Câu dịch theo mã; không có mã thì câu mất kết nối | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 |
| Thân banner — gọi quá nhanh | Như màn Đăng nhập; câu mang số giây chờ | `loi.CORE.RATE_LIMIT.EXCEEDED` | [01-dang-nhap.md](./01-dang-nhap.md) · [auth.md](../../contracts/auth.md) §10 |
| Lỗi dưới ô — bỏ trống · quá dài · sai khuôn | Câu của validator bắt buộc, độ dài, khuôn | `loi.CORE.CLIENT.VALIDATION_REQUIRED` · `loi.CORE.CLIENT.VALIDATION_MAXLENGTH` · `loi.CORE.CLIENT.VALIDATION_PATTERN` | [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §5.3 |
| Lỗi dưới ô từ máy chủ | Câu dịch từ mã trong `fieldErrors` | `loi.<mã>` | [profile.md](../../contracts/profile.md) §2 · [auth.md](../../contracts/auth.md) §6 |
| Tiêu đề banner nhắc cờ | Tài khoản đang mang cờ đặc quyền | `hoSo.coDacQuyen.nhac.tieuDe` | duyệt · [V1](../../luong/V1-cai-dat-lan-dau.md) §3 bước 9 |
| Thân banner nhắc cờ | Tài khoản này có mọi quyền mà không cần vai trò. Khi đơn vị đã có người khác quản trị phân quyền qua vai trò, hãy từ bỏ cờ ở khối Cờ đặc quyền. | `hoSo.coDacQuyen.nhac.noiDung` | duyệt · [profile.md](../../contracts/profile.md) §3 · [V1](../../luong/V1-cai-dat-lan-dau.md) §6 |
| Tiêu đề `Card` đổi mật khẩu · nút đổi mật khẩu | Đổi mật khẩu · Đổi mật khẩu | `hoSo.doiMatKhau.tieuDe` · `xacThuc.matKhau.nutDoi` | [Icons.md](../Icons.md) §5, tên hành động |
| Nhãn ba ô mật khẩu · nút hiện/ẩn · gợi ý chính sách · lỗi trùng mật khẩu hiện tại · lỗi nhập lại không khớp | Như [01-dang-nhap.md](./01-dang-nhap.md) | `xacThuc.matKhau.hienTai` · `xacThuc.matKhau.moi` · `xacThuc.matKhau.nhapLai` · `xacThuc.matKhau.hien` · `xacThuc.matKhau.an` · `xacThuc.matKhau.goiYChinhSach` · `loi.CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT` · `loi.CORE.CLIENT.VALIDATION_MISMATCH` | Dùng chung với [01-dang-nhap.md](./01-dang-nhap.md) |
| Toast đổi mật khẩu thành công | Đã đổi mật khẩu. Các phiên đăng nhập khác của tài khoản sẽ bị đăng xuất. | `hoSo.doiMatKhau.thongBao.thanhCong` | duyệt · [auth.md](../../contracts/auth.md) §6, Ghi chú |
| Tiêu đề `Card` cờ đặc quyền · nút mở hộp xác nhận | Cờ đặc quyền · Từ bỏ cờ đặc quyền | `hoSo.coDacQuyen.tieuDe` · `hoSo.coDacQuyen.nutTuBo` | [V1](../../luong/V1-cai-dat-lan-dau.md) §3 bước 9 |
| Mô tả `Card` cờ đặc quyền | Từ bỏ cờ thì tài khoản chỉ còn quyền đến từ vai trò của nó. Chỉ từ bỏ được khi đơn vị còn ít nhất một tài khoản khác, không bị khoá, có quyền sửa phân quyền qua vai trò. | `hoSo.coDacQuyen.moTa` | duyệt · [profile.md](../../contracts/profile.md) §3 |
| Tiêu đề hộp xác nhận | Từ bỏ cờ đặc quyền? | `hoSo.coDacQuyen.xacNhan.tieuDe` | duyệt |
| Nội dung hộp xác nhận | Không bật lại được cờ này. Mọi phiên đăng nhập khác của tài khoản sẽ bị đăng xuất; phiên đang dùng được giữ. | `hoSo.coDacQuyen.xacNhan.noiDung` | duyệt · [profile.md](../../contracts/profile.md) §3, Ghi chú |
| Nút xác nhận trong hộp (nhãn nói đúng hành động) · nút huỷ (`cancelLabel = null`) | Từ bỏ cờ đặc quyền · nhãn huỷ chung | `hoSo.coDacQuyen.xacNhan.nut` · `chung.huy` | [ConfirmDialog.md](../Components/ConfirmDialog.md) §API |
| Lỗi từ bỏ — đơn vị chưa có người quản trị phân quyền khác | Chưa từ bỏ được: đơn vị cần ít nhất một tài khoản khác, không bị khoá, có quyền sửa phân quyền qua vai trò. | `loi.CORE.PROFILE.NO_OTHER_PERMISSION_ADMIN` | duyệt · [profile.md](../../contracts/profile.md) §3 |
| Lỗi từ bỏ — tài khoản không còn mang cờ | Tài khoản không còn mang cờ đặc quyền. | `loi.CORE.PROFILE.PERMISSION_BYPASS_NOT_HELD` | duyệt · [profile.md](../../contracts/profile.md) §3 |
| Toast từ bỏ thành công | Đã từ bỏ cờ đặc quyền. | `hoSo.coDacQuyen.thongBao.thanhCong` | duyệt |
| Tiêu đề banner lỗi (tiền tố vai) | Lỗi | `chung.thongBao.loi` | [NoticeBanner.md](../Components/NoticeBanner.md) §Accessibility |

### Trạng thái

Căn cứ: [profile.md](../../contracts/profile.md) §1–§3 · [auth.md](../../contracts/auth.md) §6, §10, §11 · [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 · [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8 · [Card.md](../Components/Card.md), [SkeletonLoader.md](../Components/SkeletonLoader.md) §Khi nào dùng, [Input.md](../Components/Input.md), [NoticeBanner.md](../Components/NoticeBanner.md) §Khi nào dùng, [ConfirmDialog.md](../Components/ConfirmDialog.md), [FormRow.md](../Components/FormRow.md) §Trạng thái · [09-forms-validation.md](../../wiki-core/fe/09-forms-validation.md) §4 · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §2.3.

- **mặc định** — form thông tin điền sẵn từ `GET`; form đổi mật khẩu ba ô trống. Banner nhắc và `Card` cờ đặc quyền có hay không tuỳ `hasPermissionBypass`.
- **đang tải** — `PageHeader` và `Card` đổi mật khẩu hiện ngay. Thân `Card` thông tin là `SkeletonLoader` giữ đúng năm hàng `FormRow`; tiêu đề và chân `Card` giữ nguyên, `Card` mang `aria-busy`. Banner nhắc và `Card` cờ đặc quyền **không** vẽ skeleton (chưa biết chúng có tồn tại không). Đang gửi một form: chỉ nút gửi của form đó `loading`, các ô không bị khoá.
- **rỗng** — không áp dụng: hồ sơ luôn tồn tại cho người đang gọi. Số điện thoại trống là bình thường; ngôn ngữ rỗng → ô chọn đứng ở "mặc định hệ thống".
- **lỗi** — `GET`/`PUT` hồ sơ, `POST` đổi mật khẩu, `POST /api/v1/core/profile/renounce-permission-bypass` đều **tắt toast** (màn tự hiện; lỗi từ bỏ cờ hiện trong hộp xác nhận):
  - `GET` hỏng → thân `Card` thông tin là `NoticeBanner` `danger` + Thử lại; banner nhắc và `Card` cờ không render; `Card` đổi mật khẩu vẫn dùng được.
  - `PUT` 409 `CORE.CONCURRENCY.CONFLICT` → `NoticeBanner` `warning` đầu thân `Card` thông tin + nút **Tải lại**: `GET` lấy `version` mới, form nạp giá trị mới, người dùng nhập lại rồi Lưu. Không tự gộp, không gửi lại `version` cũ.
  - `PUT` hỏng không có `fieldErrors` (CSRF vẫn hỏng sau gửi lại, mất mạng) → `NoticeBanner` `danger` đầu thân `Card` thông tin; form **giữ nguyên** giá trị.
  - Đổi mật khẩu `CORE.AUTH.CHANGE_PASSWORD_FAILED` → lỗi vào **ô** theo `fieldErrors`; `CORE.AUTH.PASSWORD_MISMATCH` vào ô mật khẩu hiện tại — ánh xạ theo mã. Trùng mật khẩu hiện tại → `CORE.VALIDATION.FAILED` mang `CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT` trong `fieldErrors.NewPassword`, dưới ô mật khẩu mới. Nhiều mã cùng lúc → như [01-dang-nhap.md](./01-dang-nhap.md). Hỏng không có `fieldErrors` → `NoticeBanner` `danger` đầu thân `Card` đổi mật khẩu; ba ô giữ nguyên.
  - `CORE.RATE_LIMIT.EXCEEDED` (429) ở bất kỳ request nào → `NoticeBanner` `danger` đầu thân `Card` của form vừa gửi; thao tác từ bỏ cờ → banner trong hộp, hộp giữ mở. Câu mang số giây chờ; không toast chồng.
  - Hết phiên (401) → `SessionExpiryHandler` về đăng nhập; dữ liệu chưa lưu **mất** — v1 không có cảnh báo sớm ([07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §10).
  - Từ bỏ cờ hỏng → `ConfirmDialog` trạng thái `error`: `NoticeBanner` `danger` trong hộp, hộp **giữ mở**. Riêng `PERMISSION_BYPASS_NOT_HELD`: đóng hộp xong gọi lại `GET` hồ sơ để banner nhắc và khối cờ phản ánh đúng dữ liệu.
- **kiểm tra dữ liệu** — lỗi dưới đúng ô theo luật ba nhánh; bấm gửi khi còn lỗi → focus về ô lỗi đầu tiên của form đó; lỗi máy chủ gắn qua `applyFieldErrors`. Form thông tin kiểm client theo giá trị ở [profile.md](../../contracts/profile.md) §2 (không chép số): họ tên bắt buộc, có trần độ dài; số điện thoại theo khuôn của card; ngôn ngữ chọn từ danh sách một nguồn ở FE. Máy chủ vẫn kiểm cả ba, lỗi gắn vào ô theo khoá `FullName` / `PhoneNumber` / `PreferredLanguage`. Form đổi mật khẩu: như [01-dang-nhap.md](./01-dang-nhap.md).

| Thao tác xong | Màn làm gì | Căn cứ |
| --- | --- | --- |
| Lưu | Form nhận `data` trả về; xoá cờ "có thay đổi chưa lưu"; toast `success`; **gọi lại `GET /api/v1/core/auth/me`** để `Topbar` và phiên mang họ tên mới (`Topbar` đọc tên từ phiên). Ngôn ngữ ưa thích đổi → áp ngay và ghi đè bản sao ở `localStorage` | [profile.md](../../contracts/profile.md) §2 · [auth.md](../../contracts/auth.md) §5 · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §7 · [Topbar.md](../Components/Topbar.md) §API |
| Đổi mật khẩu | Xoá ba ô và cờ thay đổi của form này; toast `success`. Phiên đang dùng **giữ nguyên** (BE cấp lại cookie); mọi phiên khác bị chấm dứt. Không đăng xuất, không bắt đăng nhập lại | [auth.md](../../contracts/auth.md) §6, Ghi chú |
| Rời trang khi còn thay đổi chưa lưu | Hỏi qua `unsavedChangesGuard` dùng chung — màn không tự dựng hộp | [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §4.1 |
| Từ bỏ cờ | Đóng hộp; gỡ banner nhắc và `Card` cờ khỏi DOM; toast `success`; gọi lại `GET /api/v1/core/auth/me` để dựng lại giao diện theo tập quyền mới | [profile.md](../../contracts/profile.md) §3 |

### Responsive

| Ngưỡng | Cái gì đổi |
| --- | --- |
| ≥ `$bp-lg` | `Sidebar` mở đầy đủ; cột nội dung dừng ở `--layout-form-w` |
| `$bp-md` … `$bp-lg` | `Sidebar` thu gọn ([Sidebar.md](../Components/Sidebar.md)) |
| < `$bp-md` | `Sidebar` drawer; đệm `main` → `--layout-page-pad-sm`; cột nội dung theo bề rộng `main`; đệm `Card` hạ một bậc ([Card.md](../Components/Card.md) §Responsive) |
| < `$bp-xs` | Nút chân cả ba `Card` chuyển `block`; `ConfirmDialog` xếp hai nút dọc, nút xác nhận lên trên ([ConfirmDialog.md](../Components/ConfirmDialog.md) §Responsive) |

**Ẩn đi đâu:** không trường nào bị ẩn. Tiêu đề tuyến trên `Topbar` ẩn dưới `$bp-xs` vẫn ở `<h1>` của `PageHeader`.

### Icon

Theo [Icons.md](../Icons.md) §5.

| Chỗ | Icon |
| --- | --- |
| Banner nhắc cờ · hộp xác nhận (biến thể `danger` của [ConfirmDialog.md](../Components/ConfirmDialog.md)) | `pi-exclamation-triangle` |
| Banner lỗi · Thử lại · toast thành công · nút đang gửi | `pi-times-circle` · `pi-refresh` · `pi-check-circle` · `pi-spinner` |
| Nút hiện/ẩn ba ô mật khẩu | `pi-eye` · `pi-eye-slash` |

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn.
