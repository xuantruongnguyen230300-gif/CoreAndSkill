---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Đăng nhập và đổi mật khẩu bắt buộc — màn hình

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Repo chưa có `src/`. Hai màn **sẽ được dựng** ở pha F2.

Người chưa đăng nhập vào **Đăng nhập**; phiên mang cờ buộc đổi mật khẩu thì sang **Đổi mật khẩu bắt buộc**, không đi được tuyến nào khác; đổi xong vào ứng dụng, không đăng nhập lại. Nghiệp vụ: [D1](../../luong/D1-dang-nhap.md), [D2](../../luong/D2-doi-mat-khau-lan-dau.md), [D6](../../luong/D6-dang-xuat.md), [auth.md](../../contracts/auth.md) — không chép lại.

> **Khung:** khung xác thực ([AuthCard.md](../Components/AuthCard.md), không `Sidebar`, không `Topbar`) cho **cả hai** màn — tuyến `noShell` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1, §2.3).
> **Quyền:** không cần permission. Màn đăng nhập không cần phiên; màn đổi mật khẩu bắt buộc cần phiên hợp lệ mang cờ `mustChangePassword` ([auth.md](../../contracts/auth.md) §5, §7).

---

## Luồng giữa hai màn

```text
Đăng nhập ──POST login 200──► đọc DTO phiên từ response login (KHÔNG gọi me) + lấy lại token XSRF
   │                          (không phụ thuộc thứ tự; cả hai xong trước khi điều hướng)
   │                                                   │
   │                         mustChangePassword = true ├──► Đổi mật khẩu bắt buộc
   │                                                   │        │ POST change-password-required 200
   │                                                   │        ▼
   │                                                   │     GET me ──► ghi ngôn ngữ vào hồ sơ nếu khác
   │                                                   │        │           └──► returnUrl hợp lệ | sauDangNhap
   │                                                   │        │
   │                                                   │        └─ Đăng xuất ──► Đăng nhập
   │                         mustChangePassword = false└──► returnUrl hợp lệ | sauDangNhap
   │
   └─ lỗi ──► ở lại màn, hiện lỗi (mục Trạng thái)
```

Nguồn: [D1](../../luong/D1-dang-nhap.md) §3 bước 7–9 · [D2](../../luong/D2-doi-mat-khau-lan-dau.md) §3 · [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.6 (đăng nhập trả 200: đọc DTO phiên từ response, **không** gọi `me`), §5.1, §5.3 điều 4 (sau đổi mật khẩu bắt buộc: cờ làm mới từ BE qua `me`); §3.2 (`returnUrl` chỉ nhận đường dẫn nội bộ); §6 (`sauDangNhap` do app cấp qua seam `CORE_ROUTES`) · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §7. "Không cho thoát ra" (D1 bước 9, D2 bước 2) = **không điều hướng sang tuyến khác**; đăng xuất vẫn làm được ([D6](../../luong/D6-dang-xuat.md) §2).

---

## Đăng nhập (`/dang-nhap`)

### Sơ đồ bố cục

```text
- body
  - main — vùng xác thực, cao 100dvh, nền --color-bg, căn giữa (AuthCard.md §Responsive)
    - AuthCard — biến thể `form`, cỡ `md`: bề rộng --layout-auth-w, đệm --sp-11
      - Thương hiệu — dữ liệu seam CORE_BRANDING; mặc định của Core là tên sản phẩm dạng chữ, không có ảnh logo
      - Tiêu đề <h1> — cỡ --fs-2xl
      - Khu lỗi — NoticeBanner vai `danger`, chỉ có trong DOM khi có lỗi, đứng TRƯỚC các trường
      - form (submit bằng Enter)
        - AuthField `tenantCode` — Mã đơn vị, autocomplete `organization`
        - AuthField `identifier` — Tên đăng nhập, autocomplete `username`
        - AuthField `password`   — Mật khẩu, autocomplete `current-password`
        - Button `primary`, cỡ `lg`, `block`, type `submit` — Đăng nhập
      - LanguageSwitcher `inline`, cỡ `md` — cuối thứ tự Tab; chỉ render khi CORE_I18N.languages có ≥ 2 mục (v1 một ngôn ngữ: không render, chỗ này giữ)
  - Footer biến thể `auth`, cỡ `sm` — DƯỚI khung, không lồng vào AuthCard
```

| Khoản | Giá trị |
| --- | --- |
| Chiều cao ô nhập, nút chính | `--size-control-lg` — cỡ `lg` mặc định của `AuthField` và nút chính của `AuthCard` |
| Khe nhãn → ô | `--sp-5` ([AuthField.md](../Components/AuthField.md) §Responsive) |
| Khe giữa các vùng | Bậc `--sp-*` gán ở [AuthCard.md](../Components/AuthCard.md) |

Thứ tự ba ô **cố định**: mã đơn vị → tên đăng nhập → mật khẩu ([D1](../../luong/D1-dang-nhap.md) §1 · [auth.md](../../contracts/auth.md) §3).

| Cố ý không có | Căn cứ |
| --- | --- |
| Ảnh logo trong mặc định của Core — thương hiệu qua seam `CORE_BRANDING`, hiện trường `name` dạng chữ | [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.5 |
| Ô ghi nhớ đăng nhập | [auth.md](../../contracts/auth.md) §3 · [07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §10 |
| Liên kết quên mật khẩu và dòng hướng dẫn thay nó — không thêm chữ không có nguồn; người bị khoá được chỉ tới quản trị qua câu `LOCKED_OUT` | [ADR-0029](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) |
| `LanguageSwitcher` khi `CORE_I18N.languages` chỉ có một mục — v1 là ca này: component **không render** (trạng thái `empty`); vị trí trong sơ đồ giữ nguyên, seam có ≥ 2 ngôn ngữ thì nó hiện mà không sửa màn | [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Trạng thái · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §7 · duyệt 2026-09-16 |

### Câu chữ

Câu không ghi nguồn: người dùng duyệt 2026-09-15 ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n | Nguồn |
| --- | --- | --- | --- |
| Tiêu đề trang (`title` tuyến) và `<h1>` | Đăng nhập | `xacThuc.dangNhap.tieuDe` | [AuthCard.md](../Components/AuthCard.md) §Accessibility |
| Tên sản phẩm ở vùng thương hiệu · tên từng ngôn ngữ | Dữ liệu, **không** qua khoá: trường `name` của seam `CORE_BRANDING` · tên viết bằng chính ngôn ngữ đó | — | [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.5 · [LanguageSwitcher.md](../Components/LanguageSwitcher.md) |
| Nhãn ô mã đơn vị · tên đăng nhập | Mã đơn vị · Tên đăng nhập | `xacThuc.dangNhap.maDonVi` · `xacThuc.dangNhap.tenDangNhap` | [auth.md](../../contracts/auth.md) §3, bảng field |
| Nhãn ô mật khẩu · nút gửi | Mật khẩu · Đăng nhập | `xacThuc.dangNhap.matKhau` · `xacThuc.dangNhap.nutGui` | [D1](../../luong/D1-dang-nhap.md) §1 |
| Nút hiện/ẩn mật khẩu (dùng chung với màn đổi mật khẩu bắt buộc) | Hiện mật khẩu ↔ Ẩn mật khẩu | `xacThuc.matKhau.hien` · `xacThuc.matKhau.an` | [AuthField.md](../Components/AuthField.md) §Accessibility |
| Nhãn nhóm chọn ngôn ngữ | Ngôn ngữ | `chung.ngonNgu` | [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Accessibility |
| Tiêu đề khu lỗi (tiền tố vai) | Lỗi | `chung.thongBao.loi` | [NoticeBanner.md](../Components/NoticeBanner.md) §Accessibility |
| Khu lỗi — sai thông tin đăng nhập | Mã đơn vị, tên đăng nhập hoặc mật khẩu không đúng. — **một** câu cho cả bốn ca, không chỉ ra ô nào sai | `loi.CORE.AUTH.INVALID_CREDENTIALS` | duyệt · [D1](../../luong/D1-dang-nhap.md) §4 · [auth.md](../../contracts/auth.md) §3 |
| Khu lỗi — tài khoản bị khoá | Tài khoản đang bị khoá. Liên hệ quản trị viên của đơn vị để được mở khoá. | `loi.CORE.AUTH.LOCKED_OUT` | duyệt · [D1](../../luong/D1-dang-nhap.md) §4 · [auth.md](../../contracts/auth.md) §3, Ghi chú |
| Khu lỗi — gọi quá nhanh | Bạn thao tác quá nhanh. Thử lại sau {{RetryAfterSeconds}} giây. — tham số trùng tên tham số BE gửi kèm | `loi.CORE.RATE_LIMIT.EXCEEDED` | duyệt · [auth.md](../../contracts/auth.md) §10 · [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 |
| Khu lỗi — mất kết nối, lỗi khác | Câu dịch theo mã; không có mã thì câu mất kết nối | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 |
| Lỗi dưới ô — bỏ trống · từ máy chủ | Câu của validator bắt buộc · câu dịch từ mã trong `fieldErrors` | `loi.CORE.CLIENT.VALIDATION_REQUIRED` · `loi.<mã>` | [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §5.3 · [auth.md](../../contracts/auth.md) §3 · [09-forms-validation.md](../../wiki-core/fe/09-forms-validation.md) §4 |
| Dòng bản quyền ở `Footer` | © {{nam}} {{tenHeThong}} — năm và tên là tham số, không nhúng vào câu | `chung.chanTrang.banQuyen` | duyệt · [Footer.md](../Components/Footer.md) §API |

### Trạng thái

Căn cứ: [auth.md](../../contracts/auth.md) §3, §5 · [D1](../../luong/D1-dang-nhap.md) §2, bước 7–8 · [AuthCard.md](../Components/AuthCard.md) §Accessibility, §Trạng thái · [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 · [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8 · [03-f2-auth-routing.md](../../wiki-core/fe/trien-khai/03-f2-auth-routing.md) §7 · [FormRow.md](../Components/FormRow.md) §Trạng thái · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §7.

- **mặc định** — ba ô trống, không có khu lỗi; focus vào ô đầu tiên còn trống (ô mã đơn vị).
- **đang tải** — không tải dữ liệu riêng (token chống giả mạo, trạng thái phiên lấy lúc app khởi động). Đang gửi → `loading` của `AuthCard`: nút chính quay, ba ô `disabled` và **giữ giá trị đã gõ**, khung không đổi kích thước.
- **rỗng** — không áp dụng.
- **lỗi** — request đăng nhập **tắt toast** (`BO_QUA_TOAST_LOI`); màn tự hiện ở khu lỗi của `AuthCard`:
  - `CORE.AUTH.INVALID_CREDENTIALS` → khu lỗi. 🛑 **Không** tô đỏ ô nào, kể cả ô mã đơn vị.
  - `CORE.AUTH.LOCKED_OUT` → khu lỗi, câu riêng.
  - `CORE.AUTH.CSRF_REJECTED` → interceptor lấy token mới, gửi lại **một** lần; vẫn hỏng → khu lỗi.
  - `CORE.RATE_LIMIT.EXCEEDED` → khu lỗi, câu mang số giây chờ; nhánh 429 của `errorInterceptor` tôn trọng `BO_QUA_TOAST_LOI`, không toast chồng.
  - Mất mạng, không có envelope → khu lỗi, câu mất kết nối.
  - Hết phiên → không áp dụng. Người bị đẩy về đây vì hết phiên (có `returnUrl`) **không** thấy lời giải thích thêm.
- **kiểm tra dữ liệu** — ba ô bắt buộc. Lỗi thiếu hiện **dưới đúng ô** qua `errorMessage` của `AuthField`, chỉ sau khi rời ô hoặc bấm gửi (luật ba nhánh, thi công ở `fieldErrorText`). Bấm gửi khi còn ô thiếu → **không** gọi API, focus về ô lỗi đầu tiên. `CORE.VALIDATION.FAILED` gắn vào ô theo khoá `TenantCode` / `UserName` / `Password`; khoá không khớp → khu lỗi, không toast. Lỗi xác thực **không bao giờ** vào ô.

Thành công: phiên thiết lập từ **DTO phiên trong chính response `login`** (cùng kiểu với `me` — [auth.md](../../contracts/auth.md) §5); **không** gọi `me` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.6 · [D1](../../luong/D1-dang-nhap.md) §3 bước 8). Lấy lại token chống giả mạo ([D1](../../luong/D1-dang-nhap.md) §3 bước 7) — việc này và việc đọc DTO phiên **không phụ thuộc thứ tự**, nhưng **cả hai** phải xong trước khi điều hướng; rồi rẽ nhánh theo sơ đồ luồng. Ngôn ngữ áp theo `preferredLanguage` của DTO đó — không gọi hồ sơ chỉ để lấy ngôn ngữ.

### Responsive

Theo [AuthCard.md](../Components/AuthCard.md) §Responsive; không thêm hành vi riêng; không phần tử nào bị ẩn.

| Ngưỡng | Cái gì đổi |
| --- | --- |
| < `$bp-sm` | `LanguageSwitcher` **giữ** `inline` trong `AuthCard` ([LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Responsive) |
| < `$bp-md` | `Footer` `auth` xếp dọc ([Footer.md](../Components/Footer.md) §Responsive) |

### Icon

| Chỗ | Icon | Căn cứ |
| --- | --- | --- |
| Icon dẫn ô mã đơn vị · tên đăng nhập · mật khẩu | `pi-building` · `pi-user` · `pi-key` | Biến thể `tenantCode` · `identifier` · `password` của [AuthField.md](../Components/AuthField.md) |
| Nút hiện/ẩn · icon vai khu lỗi · nút đang gửi | `pi-eye`/`pi-eye-slash` · `pi-times-circle` · `pi-spinner` | [Icons.md](../Icons.md) §5 |

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn. `autocomplete` ô mã đơn vị là `organization` — biến thể `tenantCode` của [AuthField.md](../Components/AuthField.md). Khung xác thực **không có nút đổi theme**; lựa chọn đã lưu vẫn áp trước khi trang vẽ, đổi theme làm ở `Topbar` sau khi đăng nhập ([AuthCard.md](../Components/AuthCard.md) §Khi nào dùng).

---

## Đổi mật khẩu bắt buộc (`/doi-mat-khau-bat-buoc`)

### Sơ đồ bố cục

```text
- body
  - main — vùng xác thực, như màn Đăng nhập
    - AuthCard — biến thể `form`, cỡ `md`: bề rộng --layout-auth-w (cùng bề rộng màn trước, khung không nhảy)
      - Thương hiệu — như màn Đăng nhập
      - Tiêu đề <h1>
      - Mô tả — một câu dưới tiêu đề, nêu tên đăng nhập của phiên (userName của DTO phiên — từ response login hoặc từ me lúc khởi động)
      - Khu lỗi — NoticeBanner vai `danger`, chỉ khi có lỗi không thuộc ô nào
      - form
        - AuthField `password` — Mật khẩu hiện tại,     autocomplete `current-password`
        - AuthField `password` — Mật khẩu mới,          autocomplete `new-password`
            gợi ý chính sách — câu tĩnh qua `hint` của AuthField, ô nối `aria-describedby` tới nó
        - AuthField `password` — Nhập lại mật khẩu mới, autocomplete `new-password`
        - Button `primary`, cỡ `lg`, `block`, type `submit` — Đổi mật khẩu
      - Button `ghost`, type `button` — Đăng xuất   (thứ tự Tab: sau nút chính, trước LanguageSwitcher)
      - LanguageSwitcher `inline`, cỡ `md` — như màn Đăng nhập: chỉ render khi có ≥ 2 ngôn ngữ
  - Footer biến thể `auth`, cỡ `sm`
```

Số đo như màn Đăng nhập. 🛑 **Hai ô mật khẩu mới mang `new-password`, không phải `current-password`** ([AuthField.md](../Components/AuthField.md) §Accessibility).

| Quyết định bố cục | Căn cứ |
| --- | --- |
| Có ô nhập lại mật khẩu mới, kiểm khớp **ở FE**; ô thứ ba không gửi lên | [auth.md](../../contracts/auth.md) §7 · [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §6.3 |
| Gợi ý chính sách là **câu tĩnh** qua khoá i18n, không chép số; lỗi cụ thể đến từ `fieldErrors` | Luật S9 · [auth.md](../../contracts/auth.md) §6 |
| Mô tả nêu tên đăng nhập của phiên — người đăng nhập nhầm tài khoản biết mình cần bấm Đăng xuất; Đăng xuất là `Button`, không phải liên kết | [D6](../../luong/D6-dang-xuat.md) §3 |

### Câu chữ

| Phần tử | Câu hiển thị | Khoá i18n | Nguồn |
| --- | --- | --- | --- |
| Tiêu đề trang và `<h1>` · nhãn ô nhập lại | Đặt mật khẩu mới · Nhập lại mật khẩu mới | `xacThuc.doiMatKhauBatBuoc.tieuDe` · `xacThuc.matKhau.nhapLai` | duyệt |
| Mô tả | Tài khoản {{tenDangNhap}} đang dùng mật khẩu tạm. Đặt mật khẩu mới để tiếp tục dùng hệ thống. | `xacThuc.doiMatKhauBatBuoc.moTa` | duyệt · [D2](../../luong/D2-doi-mat-khau-lan-dau.md) §3 bước 2 · [auth.md](../../contracts/auth.md) §7, Request |
| Nhãn ô mật khẩu hiện tại · mật khẩu mới (dùng chung với [03-ho-so-ca-nhan.md](./03-ho-so-ca-nhan.md)) | Mật khẩu hiện tại · Mật khẩu mới | `xacThuc.matKhau.hienTai` · `xacThuc.matKhau.moi` | [auth.md](../../contracts/auth.md) §6 |
| Gợi ý chính sách | Mật khẩu mới phải khác mật khẩu hiện tại và đạt chính sách mật khẩu của hệ thống. Chưa đạt điều gì, hệ thống sẽ nêu cụ thể. | `xacThuc.matKhau.goiYChinhSach` | duyệt · [auth.md](../../contracts/auth.md) §6, §7 Ghi chú |
| Nút gửi · nút đăng xuất | Đổi mật khẩu · Đăng xuất | `xacThuc.matKhau.nutDoi` · `xacThuc.dangXuat` | [Icons.md](../Icons.md) §5, tên hành động |
| Nút hiện/ẩn mật khẩu | Hiện mật khẩu ↔ Ẩn mật khẩu | `xacThuc.matKhau.hien` · `xacThuc.matKhau.an` | Dùng chung với màn Đăng nhập |
| Lỗi dưới ô mật khẩu hiện tại | Câu của mã trong `fieldErrors.CurrentPassword` | `loi.CORE.AUTH.PASSWORD_MISMATCH` | [auth.md](../../contracts/auth.md) §6 |
| Lỗi dưới ô mật khẩu mới | Câu của từng mã trong `fieldErrors.NewPassword`, tham số theo tên — ví dụ `MinLength` | `loi.CORE.AUTH.PASSWORD_TOO_SHORT` · `loi.CORE.AUTH.PASSWORD_REQUIRES_DIGIT` · các mã chính sách khác | [auth.md](../../contracts/auth.md) §6 |
| Lỗi dưới ô mật khẩu mới — trùng mật khẩu hiện tại (lỗi server, trong `fieldErrors.NewPassword`) | Mật khẩu mới phải khác mật khẩu hiện tại. | `loi.CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT` | duyệt · [auth.md](../../contracts/auth.md) §6 |
| Lỗi dưới ô nhập lại — không khớp (một câu chung cho mọi ô nhập lại; dùng chung với [03-ho-so-ca-nhan.md](./03-ho-so-ca-nhan.md), [20-don-vi.md](./20-don-vi.md)) | Nội dung nhập lại không khớp. | `loi.CORE.CLIENT.VALIDATION_MISMATCH` | duyệt · [be-cqrs-handler.md](../../quy-uoc/be-cqrs-handler.md) §7.4 |
| Lỗi dưới ô bị bỏ trống | Câu của validator bắt buộc | `loi.CORE.CLIENT.VALIDATION_REQUIRED` | [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §5.3 |
| Khu lỗi — gọi quá nhanh | Như màn Đăng nhập | `loi.CORE.RATE_LIMIT.EXCEEDED` | [auth.md](../../contracts/auth.md) §10 |
| Tiêu đề khu lỗi, lỗi mất kết nối, chọn ngôn ngữ, `Footer` | Như màn Đăng nhập | Như màn Đăng nhập | — |

### Trạng thái

Căn cứ: [auth.md](../../contracts/auth.md) §1.2, §4, §6, §7, §10 · [D6](../../luong/D6-dang-xuat.md) §2–§4 · [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1, §5.3, §8 · [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 · [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §6.3, §6.5 · [FormRow.md](../Components/FormRow.md) · [09-forms-validation.md](../../wiki-core/fe/09-forms-validation.md) §4 · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §7.

- **mặc định** — ba ô trống, focus vào ô mật khẩu hiện tại; mô tả nêu tên đăng nhập.
- **đang tải** — không tải dữ liệu riêng (cờ, tên đăng nhập đã có trong DTO phiên). Đang gửi → `loading` của `AuthCard`, như màn Đăng nhập. Nút Đăng xuất **vẫn bấm được** lúc form đang gửi.
- **rỗng** — không áp dụng.
- **lỗi** — request đổi mật khẩu và đăng xuất tắt toast, màn tự hiện:
  - `CORE.AUTH.CHANGE_PASSWORD_FAILED` → lỗi vào **ô** theo `fieldErrors` — ánh xạ theo mã, không theo endpoint.
  - Khoá `fieldErrors` không khớp ô nào (ví dụ `$record`) → khu lỗi, không toast.
  - `CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED` → cờ đã hạ ở chỗ khác: gọi lại `me` rồi rời màn như khi thành công; không hiện lỗi.
  - `CORE.RATE_LIMIT.EXCEEDED` (429) → khu lỗi, câu mang số giây chờ; hạn mức áp cho mọi endpoint; không toast chồng.
  - `CORE.AUTH.CSRF_REJECTED`, mất mạng → như màn Đăng nhập.
  - Hết phiên (401) → `SessionExpiryHandler` về Đăng nhập kèm `returnUrl`; mật khẩu vừa gõ mất.
  - Đăng xuất nhận 200 hoặc 401 → đã đăng xuất: dọn trạng thái, lấy lại token chống giả mạo, về Đăng nhập. Hỏng kiểu khác → khu lỗi; **không** dọn trạng thái, ở lại màn.
- **kiểm tra dữ liệu** — ba ô bắt buộc; ô nhập lại khớp ô mật khẩu mới (kiểm ở FE, `CORE.CLIENT.VALIDATION_MISMATCH`); mật khẩu mới khác mật khẩu hiện tại do BE kiểm (`CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT` trong `fieldErrors.NewPassword`). Lỗi dưới đúng ô theo luật ba nhánh; lỗi máy chủ gắn qua `applyFieldErrors`, gỡ khi người dùng sửa ô. Ô mật khẩu mới nhận **nhiều** mã cùng lúc: hiện từng mã, mã đầu theo thứ tự BE trả; sửa ô hoặc gửi lại thì mã kế hiện ra (`fieldErrorText`).

Thành công: BE cấp lại cookie cho phiên đang dùng. FE gọi lại `me` **trước** khi điều hướng. Ngôn ngữ chọn ở màn này chỉ ghi được `localStorage` (`PUT` hồ sơ bị chặn khi cờ còn bật); nếu khác `preferredLanguage` mà `me` vừa trả, **FE ghi lựa chọn đó vào hồ sơ** sau khi đổi mật khẩu — thứ tự và nhánh hỏng ở 08-i18n.md §7. Không bắt đăng nhập lại.

### Responsive

Như màn Đăng nhập. Nút Đăng xuất giữ nguyên ở mọi ngưỡng.

### Icon

| Chỗ | Icon | Căn cứ |
| --- | --- | --- |
| Icon dẫn cả ba ô mật khẩu | `pi-key` | Biến thể `password` của [AuthField.md](../Components/AuthField.md) |
| Nút hiện/ẩn · icon vai khu lỗi · nút đang gửi | `pi-eye`/`pi-eye-slash` · `pi-times-circle` · `pi-spinner` | [Icons.md](../Icons.md) §5 |
| Nút Đăng xuất | Không có icon | Nút có chữ; không thêm icon trang trí |

### Ảnh màn hình

Chưa có — repo chưa có `src/`, không có gì để chụp.

### Cần chốt

Không còn.
