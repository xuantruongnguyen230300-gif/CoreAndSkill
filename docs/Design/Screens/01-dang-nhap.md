---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Đăng nhập và đổi mật khẩu bắt buộc — màn hình

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Hai màn đã dựng ở `src/FE` (pha F2). Mới đối chiếu **từng mục** — bảng "Có thật hôm nay → sẽ thành" ngay dưới khối Khung/Quyền ghi mục nào đã so, mục nào lệch; chưa đối chiếu trọn file nên `verified: chua-doi-chieu` giữ nguyên.

Người chưa đăng nhập vào **Đăng nhập**; phiên mang cờ buộc đổi mật khẩu thì sang **Đổi mật khẩu bắt buộc**, không đi được tuyến nào khác; đổi xong vào ứng dụng, không đăng nhập lại. Nghiệp vụ: [D1](../../luong/D1-dang-nhap.md), [D2](../../luong/D2-doi-mat-khau-lan-dau.md), [D6](../../luong/D6-dang-xuat.md), [auth.md](../../contracts/auth.md) — không chép lại.

> **Khung:** khung xác thực ([AuthCard.md](../Components/AuthCard.md), không `Sidebar`, không `Topbar`) cho **cả hai** màn — tuyến `noShell` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1, §2.3).
> **Quyền:** không cần permission. Màn đăng nhập không cần phiên; màn đổi mật khẩu bắt buộc cần phiên hợp lệ mang cờ `mustChangePassword` ([auth.md](../../contracts/auth.md) §5, §7).

## Có thật hôm nay → sẽ thành

Đối chiếu 2026-09-22. Dòng mang ngày 2026-10-05 là đo trên trình duyệt (`ng serve`, Chrome, khung nhìn 1280 × 1000) trong lượt audit giao diện màn Đăng nhập. Dòng mang ngày 2026-10-06 ghi cách đối chiếu ngay trong ô, theo bốn chữ tắt: **đọc mã** — đọc tệp sau hai lượt sửa của frontend cùng ngày; **đo** — lượt đo trên trình duyệt (Chromium do Playwright điều khiển, `ng serve`, bề rộng 1280/768/390, sáng và tối, cả hai chế độ), chạy **trước** hai lượt sửa đó nên không làm bằng chứng cho mục vừa sửa; **lưới** — lưới Playwright `src/FE/e2e/`, phép đo lớp A theo mã (A1–A8) và axe, phiên chính báo xanh, kết quả không lưu trong repo; **ca kiểm** — ca kiểm đơn vị, gọi theo tên. Đường dẫn tính từ `src/FE/src/app/`; hai trang nằm dưới `platform/xac-thuc/pages/`. Mục không có tên trong bảng thì chưa ai so.

| Khoản | Có thật hôm nay (neo bằng chuỗi) | Sẽ thành |
| --- | --- | --- |
| Tuyến, guard, `title` | `platform/xac-thuc/xac-thuc.routes.ts` § `path: 'dang-nhap'` không có `canActivate`, `title: 'xacThuc.dangNhap.tieuDe'`; § `path: 'doi-mat-khau-bat-buoc'` mang `authGuard` và `mustChangePasswordGuard` | — |
| Đăng nhập — ô, thứ tự, `autocomplete`, nút chính | `dang-nhap/dang-nhap.page.html` § `@if (cheDoVanHanh()) {` bọc ô `variant="tenantCode"` (`autocomplete="organization"`); luôn có `variant="identifier"` (`username`) → `variant="password"` (`current-password`); § `<app-button variant="primary" size="lg" [block]="true" type="submit"` — chế độ thường hai ô, chế độ vận hành ba ô, đúng bảng hai chế độ | — |
| `Footer` `auth` cỡ `sm`, dưới khung | `dang-nhap/dang-nhap.page.html` § `<app-footer` đứng sau thẻ đóng `app-auth-card`, `variant="auth"`, `size="sm"`, khoá `chung.chanTrang.banQuyen`; **cả khung lẫn `Footer` nằm trong `<app-auth-page>`** (cột `100dvh` — dòng "`Footer` trong khung nhìn đầu" bên dưới) | — |
| Focus mặc định | `dang-nhap/dang-nhap.page.ts` § `focusO` — § `(cacO.find(({ c }) => chon(c)) ?? cacO[0]).o` chọn ô theo chế độ: chế độ thường vào ô tên đăng nhập, chế độ vận hành vào ô mã đơn vị; sau khi chuyển chế độ, vào ô đầu tiên còn trống. Vào màn và đổi chế độ gọi § `this.focusO((c) => c.value === '', true)`, nhánh `lucMo` gọi `focus()` của `AuthField` — ô mang cờ ô tự nhận focus (đọc mã 2026-10-08). Màn đổi mật khẩu: `doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page.ts` § `afterNextRender(() => this.oMatKhauHienTai()?.focus())` | — |
| Ô nhận focus lúc màn mở — rời ô khi chưa sửa gì thì không báo *bắt buộc* (mục Trạng thái, dòng *kiểm tra dữ liệu*, cả hai màn) | Có (đọc mã 2026-10-08): cờ do `focus()` của `AuthField` gắn, `chamVao` không đánh dấu `touched` khi ô mang cờ mà chưa sửa. Neo và ca kiểm — gồm e2e `src/FE/e2e/man/01-dang-nhap.spec.ts` ca `ô tên đăng nhập tự nhận focus: Tab rời ô khi chưa gõ → không dòng lỗi, không aria-invalid` — ở [AuthField.md](../Components/AuthField.md), dòng *Không báo lỗi ở ô màn tự đặt focus…* | Màn đổi mật khẩu chưa có ca e2e riêng cho ô mật khẩu hiện tại |
| Gửi hỏng mà màn ở lại → focus về phần tử đang giữ focus trước lúc gửi (mục Trạng thái, cả hai màn) | Có (đọc mã 2026-10-08), cả hai màn qua `shared/forms/tra-focus-sau-khi-mo-form.ts` § `traFocus`: `dang-nhap/dang-nhap.page.ts` và `doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page.ts` § `traFocusSauLoi`. Neo và ca kiểm — gồm e2e `src/FE/e2e/man/01-dang-nhap.spec.ts` cho ô mật khẩu và ô tên đăng nhập — ở [AuthCard.md](../Components/AuthCard.md), dòng *Focus sau khi gửi hỏng* | Nhánh bấm nút chính chưa có ca kiểm |
| Phiên lấy từ response `login`, không gọi `me`; lấy lại token chống giả mạo; tắt toast | `core/auth/auth.service.ts` § `dangNhap(payload` — `BO_QUA_TOAST_LOI`, đặt người dùng từ DTO rồi § `this.xsrf.lamMoi()` | — |
| Rẽ nhánh sau đăng nhập, `returnUrl` chỉ nhận đường dẫn nội bộ | `dang-nhap/dang-nhap.page.ts` § `dieuHuongSauDangNhap` — `phaiDoiMatKhau()` sang màn đổi mật khẩu kèm `returnUrl`; còn lại `returnUrl` hợp lệ hoặc `sauDangNhap` | — |
| Lỗi đăng nhập — `CORE.VALIDATION.FAILED` vào ô, mã khác vào khu lỗi trừ lớp xuyên suốt, không có envelope thì câu mất kết nối | `dang-nhap/dang-nhap.page.ts` § `xuLyLoi` — `applyFormFailure`, `dichLoiChoMan`, `loi.CORE.CLIENT.NO_CONNECTION` | **Thêm từ 2026-10-06**: khoá `TenantCode` ở chế độ thường vào khu lỗi với câu cấu hình; `INVALID_CREDENTIALS` mang câu theo chế độ — mục Trạng thái |
| Hai chế độ — `tenantCode` từ cấu hình bản cài ở chế độ thường; liên kết chuyển chế độ; chế độ vận hành ghi trên URL | Có. `dang-nhap/dang-nhap.page.ts` § `export const THAM_SO_CHE_DO = 'cheDo';` và § `export const GIA_TRI_CHE_DO_VAN_HANH = 'van-hanh';`, § `inject(TENANT_CODE)` cho mã đơn vị chế độ thường. Liên kết là `a[app-button]` (đọc mã 2026-10-06): `dang-nhap/dang-nhap.page.html` § `app-button` trên thẻ `<a`, `variant="ghost"`, `size="md"`, `queryParamsHandling="merge"`; `dang-nhap.page.scss` chỉ còn § `align-self: center`. Đo: liên kết mang lớp `nut--ghost nut--md`, hình thức ở [Button.md](../Components/Button.md) dòng `a[app-button]` | — |
| Vùng thương hiệu trên tiêu đề | Có (đọc mã 2026-10-06), cả hai màn: trang đọc seam — `dang-nhap/dang-nhap.page.ts` § `inject(CORE_BRANDING)` rồi § `this.branding.name` — và truyền § `[brandName]="tenHeThong"` cho `AuthCard`; khung vẽ § `auth-card__thuong-hieu` trước `<h1>`. Ca kiểm `vùng thương hiệu: tên từ CORE_BRANDING đứng TRƯỚC <h1>` | Chưa đo trên trình duyệt — lượt đo chạy trước khi có vùng này. Input `brandName` và hình thức chữ của vùng: [AuthCard.md](../Components/AuthCard.md) §Kích thước đoạn *Vùng thương hiệu*, §API (chốt 2026-10-08) |
| Khu lỗi là `NoticeBanner` vai `danger`, có tiêu đề tiền tố `chung.thongBao.loi` | Có (đọc mã 2026-10-06): `shared/components/auth-card/auth-card.component.html` § `app-notice-banner`, `severity="danger"`, tiêu đề lấy khoá § `'chung.thongBao.loi'`. Ca kiểm và lưới ở [AuthCard.md](../Components/AuthCard.md), dòng *Khu lỗi* | Chưa đo hình thức trên trình duyệt sau lượt sửa |
| Đang gửi — mọi ô đang hiện `disabled`, giữ giá trị đã gõ; liên kết chuyển chế độ không dùng được; `aria-busy` trên `<form>` | Có. `dang-nhap/dang-nhap.page.ts` § `this.form.disable({ emitEvent: false })` trong `khoaForm()`; liên kết: `dang-nhap/dang-nhap.page.html` § `[routerLink]="dangGui() ? null : []"`. `aria-busy` (đọc mã 2026-10-06): cùng tệp § `[attr.aria-busy]="dangGui() ? 'true' : null"` trên `<form>`; lưới: trạng thái *đang tải (đang gửi)* chờ đúng `main form[aria-busy="true"]`. Đo: ô khoá vẫn giữ giá trị đã gõ; liên kết `aria-disabled="true"`, không còn `href` | — |
| Đổi mật khẩu — đang gửi: mọi ô `disabled` ("như màn Đăng nhập", mục Trạng thái của màn đó) | Có (đọc mã 2026-10-08): `doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page.ts` § `this.form.disable({ emitEvent: false })` trong `khoaForm()`, mở lại trong `moForm()`; ca kiểm `đang gửi → ba ô và nút hiện/ẩn disabled, giữ giá trị; API lỗi → mở lại` (người soạn chưa chạy test). Lưới không bắt được: trạng thái *đang tải* của màn này chỉ chờ `aria-busy` | Chưa đo trên trình duyệt |
| Bấm gửi khi còn ô sai → focus về ô lỗi đầu tiên, không gọi API | Có (đọc mã 2026-10-06). Đăng nhập: `dang-nhap/dang-nhap.page.ts` § `this.focusO((c) => c.invalid)`. Đổi mật khẩu: `doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page.ts` § `focusOSaiKhiGuiSai(this.lanGuiSai`; ca kiểm `gửi khi ô mật khẩu mới trống → focus về ô sai ĐẦU TIÊN (mật khẩu mới), không gọi API` và `gửi rỗng cả ba ô → focus về ô mật khẩu hiện tại` | Chưa đo trên trình duyệt |
| Khu lỗi — câu `CORE.RATE_LIMIT.EXCEEDED`; mã không có câu dịch | Có (đọc mã 2026-10-06): `src/FE/public/i18n/vi.json` khoá `EXCEEDED` mang đúng câu ở mục Câu chữ — chuỗi `Thử lại sau {{RetryAfterSeconds}} giây.`; lượt đo chạy trước lượt sửa nên còn thấy câu cũ. Mã không có câu dịch mà BE gửi `message` rỗng thì ra câu `loi.CORE.SYSTEM.UNEXPECTED`: `core/http/dich-loi.ts` § `KHOA_DU_PHONG`; ca kiểm `thiếu khoá và message rỗng hoặc chỉ khoảng trắng → câu dự phòng chung, không trả chuỗi rỗng` | Chưa đo trên trình duyệt sau lượt sửa. Khoá dự phòng của mã gốc đổi sang `loi.CORE.CLIENT.UNKNOWN_ERROR_CODE` — luật và câu ở [08-i18n.md](../../wiki-core/fe/08-i18n.md) §5.4 |
| Đăng nhập — khe giữa các ô, ô cuối → nút chính | Có. Đo: giữa hai ô `--sp-6`, ô cuối → nút `--sp-7`, nhãn → ô `--sp-4`, ở cả ba bề rộng và cả hai chế độ; lưới A6a. Neo ở [AuthCard.md](../Components/AuthCard.md), dòng *Khe trong form* | — |
| Đăng nhập — ô trống không hiện chữ nào | Có. Đo: không ô nào mang thuộc tính `placeholder`; lưới A2. Neo ở [AuthField.md](../Components/AuthField.md), dòng `placeholder` = `null` | — |
| Đăng nhập — dấu hiệu focus của ô, kể cả ô đang lỗi | Có — một đường biên. Đo: ô hợp lệ có focus dày `--border-w-strong` màu `--color-text`, ô lỗi có focus dày `--border-w-accent` màu `--color-danger-border`, không vòng ngoài, hai theme; lưới A5. Neo ở [AuthField.md](../Components/AuthField.md), dòng `focus-visible` và `error` | — |
| Đăng nhập — ô được trình duyệt tự điền | Có trong CSS của app. Đo bằng ép `:autofill` qua giao thức DevTools: nền ô `--color-surface`, chữ `--color-text`, hai theme; lưới A7. Neo ở [AuthField.md](../Components/AuthField.md), dòng *Trình duyệt tự điền* | Tự điền **thật** của trình duyệt chưa kiểm tay — ảnh nền xanh nhạt người dùng gửi 2026-10-05 chụp trước khi có quy tắc |
| Đăng nhập — nhãn nút chính cùng họ font với màn | Có (đọc mã 2026-10-06): `shared/components/button/button.component.scss` § `font-family: var(--font-sans)`; số đo `Arial` của 2026-10-05 là trước khi khai. Đo: nhãn nút chính vẽ bằng họ đầu của `--font-sans`; lưới A4. Chi tiết ở [Button.md](../Components/Button.md), dòng "Họ font của nhãn" | — |
| Đăng nhập — `Footer` trong khung nhìn đầu, không thanh cuộn thừa | Có (đọc mã 2026-10-06): `dang-nhap/dang-nhap.page.html` § `<app-auth-page>` bọc `app-auth-card` và `app-footer`; `shared/components/auth-page/auth-page.component.ts` § `selector: 'app-auth-page'`, mô tả cột cao tối thiểu `100dvh`. Đo: không thanh cuộn ở 1280 × 800 và 390 × 844, hai chế độ, hai theme; lưới A3 | — |
| `LanguageSwitcher` cuối thứ tự Tab | **Chưa có component** — không có tệp nào dưới `shared/`. v1 chỉ một ngôn ngữ nên người dùng không thấy khác biệt, nhưng có ngôn ngữ thứ hai thì phải sửa màn, trái với dòng "không sửa màn" ở bảng Cố ý không có | Đặt vào `AuthCard` khi component về |
| Đổi mật khẩu bắt buộc — ba ô, gợi ý chính sách, nút chính, nút Đăng xuất `ghost` | `doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page.html` § `autocomplete="new-password"` ở hai ô sau, § `xacThuc.matKhau.goiYChinhSach` qua `[hint]`, § `<app-button variant="ghost" type="button"` | — |
| Đổi mật khẩu — kiểm khớp ở FE; thành công gọi lại `me` rồi đi `returnUrl` hợp lệ hoặc `sauDangNhap`; `PASSWORD_CHANGE_NOT_REQUIRED` coi như thành công | `doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page.ts` § `nhapLaiPhaiKhop('newPassword', 'confirmNewPassword')`, § `sauKhiDoiThanhCong` (gọi `lamMoiPhien`), § `CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED` | — |
| Đổi mật khẩu — đăng xuất hỏng kiểu khác thì hiện ở khu lỗi, không toast | **Lệch.** `doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page.ts` § `dangXuat()` bỏ qua lỗi (`error: () => undefined`) và để interceptor toast | Hiện ở khu lỗi theo mục Trạng thái |
| Đổi mật khẩu — ghi ngôn ngữ vào hồ sơ sau khi đổi | **Chưa có** trong `sauKhiDoiThanhCong`; màn chưa có `LanguageSwitcher` nên chưa có lựa chọn nào để ghi | Làm cùng lúc với `LanguageSwitcher` |

Chưa đối chiếu: mục Responsive, thứ tự Tab, nhánh `CORE.AUTH.CSRF_REJECTED` gửi lại một lần, câu thật của từng mã lỗi trong tệp dịch, luật ba nhánh bên trong `fieldErrorText`.

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

Màn có **hai chế độ** trên cùng một tuyến ([ADR-0116](../../adr/0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md) §Quyết định, QC2): **chế độ thường** cho mọi người dùng của bản cài, **chế độ vận hành** cho tài khoản vận hành hệ thống. Hai chế độ dùng chung một sơ đồ; dòng ghi "CHỈ chế độ vận hành" không có trong DOM ở chế độ thường.

```text
- body
  - Vùng xác thực — một cột cao TỐI THIỂU 100dvh, nền --color-bg (AuthCard.md §Responsive)
    - main — chiếm phần chiều cao còn lại của cột, đệm --sp-6, căn khung ở giữa
      - AuthCard — biến thể `form`, cỡ `md`: bề rộng --layout-auth-w, đệm --sp-11
        - Thương hiệu — dữ liệu seam CORE_BRANDING; tên sản phẩm dạng chữ, không ảnh logo, không tên hay logo đơn vị
        - Tiêu đề <h1> — cỡ --fs-2xl; như nhau ở hai chế độ
        - Mô tả — CHỈ chế độ vận hành: một câu nói đây là đăng nhập của tài khoản vận hành
        - Khu lỗi — NoticeBanner vai `danger`, chỉ có trong DOM khi có lỗi, đứng TRƯỚC các trường
        - form (submit bằng Enter)
          - AuthField `tenantCode` — Mã đơn vị, autocomplete `organization` — CHỈ chế độ vận hành
          - AuthField `identifier` — Tên đăng nhập, autocomplete `username`
          - AuthField `password`   — Mật khẩu, autocomplete `current-password`
          - Button `primary`, cỡ `lg`, `block`, type `submit` — Đăng nhập
        - Liên kết chuyển chế độ — <a> trên cùng tuyến, chỉ đổi tham số truy vấn; hình thức Button `ghost`, cỡ `md`, căn giữa
            chế độ thường: "Quản trị hệ thống" · chế độ vận hành: "Quay lại đăng nhập thường"
        - LanguageSwitcher `inline`, cỡ `md` — cuối thứ tự Tab; chỉ render khi CORE_I18N.languages có ≥ 2 mục (v1 một ngôn ngữ: không render, chỗ này giữ)
    - Footer biến thể `auth`, cỡ `sm` — đáy cột, DƯỚI khung, không lồng vào AuthCard: đủ chỗ thì sát đáy khung nhìn, thiếu chỗ thì ngay sau khung và trang cuộn
```

| Khoản | Chế độ thường — mặc định | Chế độ vận hành |
| --- | --- | --- |
| Vào bằng | URL **không** mang tham số chế độ | URL mang tham số chế độ vận hành. Tên và giá trị tham số: [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1 — không chép lại ở đây |
| Ô, theo thứ tự cố định | Tên đăng nhập → mật khẩu | Mã đơn vị → tên đăng nhập → mật khẩu. Mã đơn vị đứng đầu vì nó là điều kiện tra cứu trước tài khoản ([auth.md](../../contracts/auth.md) §3) |
| `tenantCode` gửi lên | Mã đơn vị nghiệp vụ trong cấu hình bản cài — người dùng không thấy và không sửa được | Giá trị người vận hành gõ vào ô mã đơn vị. Mã đơn vị hệ thống **không** nằm trong cấu hình FE |
| Mô tả dưới tiêu đề | Không có | Có — câu ở mục Câu chữ |
| Liên kết chuyển chế độ | "Quản trị hệ thống" → chế độ vận hành | "Quay lại đăng nhập thường" → chế độ thường |
| Focus khi màn mở | Ô tên đăng nhập | Ô mã đơn vị |
| Câu của `CORE.AUTH.INVALID_CREDENTIALS` | Không nhắc mã đơn vị | Nhắc mã đơn vị |

**Liên kết chuyển chế độ dùng component nào.** Đây là điều hướng thật — ADR-0116 phương án 2A: tải lại trang giữ chế độ, đánh dấu được URL, nút Back của trình duyệt quay về chế độ trước. Vì vậy nó là thẻ `<a>`, không phải `Button` ([Button.md](../Components/Button.md) §Khi nào dùng). Khu Design không có component *liên kết* riêng. Thẻ `<a>` này dùng `a[app-button]` biến thể `ghost`, cỡ `md` ([Button.md](../Components/Button.md) §Liên kết mang hình thức nút), áp cho vùng liên kết phụ của `AuthCard` — không tự chép CSS của nút trong `dang-nhap.page.scss`. Cùng vị trí, cùng sức nặng thị giác với nút Đăng xuất `ghost` của màn đổi mật khẩu bắt buộc. Không đẻ component mới: một liên kết chữ đứng riêng chỉ có ở đúng chỗ này.

| Khoản | Giá trị |
| --- | --- |
| Chiều cao ô nhập, nút chính | `--size-control-lg` — cỡ `lg` mặc định của `AuthField` và nút chính của `AuthCard` |
| Khe nhãn → ô | `--sp-4` ([AuthField.md](../Components/AuthField.md) §Responsive) |
| Khe giữa các vùng — gồm form → liên kết chuyển chế độ | Bậc `--sp-*` gán ở [AuthCard.md](../Components/AuthCard.md) |
| Khe giữa hai ô · ô cuối → nút chính | `--sp-6` · `--sp-7`, đo từ dòng lỗi nếu đang hiện — [AuthCard.md](../Components/AuthCard.md) §Kích thước |
| Dấu hiệu focus của ô · nền ô khi trình duyệt tự điền | [DESIGN.md](../DESIGN.md) §2.6 *Ô nhập chữ* |

Thứ tự Tab theo thứ tự đọc: [ô mã đơn vị →] tên đăng nhập → mật khẩu → nút hiện/ẩn mật khẩu → Đăng nhập → liên kết chuyển chế độ → `LanguageSwitcher` ([AuthCard.md](../Components/AuthCard.md) §Accessibility).

| Cố ý không có | Căn cứ |
| --- | --- |
| Ô mã đơn vị ở chế độ thường, ở **bất kỳ** trạng thái nào — kể cả khi đăng nhập trượt | [ADR-0116](../../adr/0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md) §Quyết định 2 |
| Trạng thái *thiếu mã cấu hình* — bản cài thiếu mã thì build sản xuất hỏng, nên màn không bao giờ chạy khi thiếu mã | ADR-0116 QC4 · [RULES-FE.md](../../RULES-FE.md) F54 |
| Tên hoặc logo của đơn vị — FE chỉ có **mã** đơn vị trong cấu hình; tên đơn vị chỉ đến trong DTO phiên sau khi đăng nhập | [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.5 · [auth.md](../../contracts/auth.md) §3, Response 200 |
| Ảnh logo trong mặc định của Core — thương hiệu qua seam `CORE_BRANDING`, hiện trường `name` dạng chữ | [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.5 |
| Ô ghi nhớ đăng nhập | [auth.md](../../contracts/auth.md) §3 · [07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §10 |
| Liên kết quên mật khẩu và dòng hướng dẫn thay nó — không thêm chữ không có nguồn; người bị khoá được chỉ tới quản trị qua câu `LOCKED_OUT` | [ADR-0029](../../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) |
| `LanguageSwitcher` khi `CORE_I18N.languages` chỉ có một mục — v1 là ca này: component **không render** (trạng thái `empty`); vị trí trong sơ đồ giữ nguyên, seam có ≥ 2 ngôn ngữ thì nó hiện mà không sửa màn | [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Trạng thái · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §7 · duyệt 2026-09-16 |

### Câu chữ

Câu không ghi nguồn: người dùng duyệt 2026-09-15 ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n | Nguồn |
| --- | --- | --- | --- |
| Tiêu đề trang (`title` tuyến) và `<h1>` — cả hai chế độ | Đăng nhập | `xacThuc.dangNhap.tieuDe` | [AuthCard.md](../Components/AuthCard.md) §Accessibility |
| Tên sản phẩm ở vùng thương hiệu · tên từng ngôn ngữ | Dữ liệu, **không** qua khoá: trường `name` của seam `CORE_BRANDING` · tên viết bằng chính ngôn ngữ đó | — | [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.5 · [LanguageSwitcher.md](../Components/LanguageSwitcher.md) |
| Mô tả — chỉ chế độ vận hành | Dành cho tài khoản vận hành hệ thống. Nhập mã đơn vị hệ thống được cấp khi cài đặt. | `xacThuc.dangNhap.moTaCheDoVanHanh` | duyệt 2026-10-06 · [D1](../../luong/D1-dang-nhap.md) §1 · ADR-0116 QC2 |
| Nhãn ô mã đơn vị (chỉ chế độ vận hành) · tên đăng nhập | Mã đơn vị · Tên đăng nhập | `xacThuc.dangNhap.maDonVi` · `xacThuc.dangNhap.tenDangNhap` | [auth.md](../../contracts/auth.md) §3, bảng field |
| Nhãn ô mật khẩu · nút gửi | Mật khẩu · Đăng nhập | `xacThuc.dangNhap.matKhau` · `xacThuc.dangNhap.nutGui` | [D1](../../luong/D1-dang-nhap.md) §1 |
| Liên kết sang chế độ vận hành | Quản trị hệ thống | `xacThuc.dangNhap.lienKetCheDoVanHanh` | ADR-0116 §Quyết định 3 · [D1](../../luong/D1-dang-nhap.md) §1 |
| Liên kết về chế độ thường | Quay lại đăng nhập thường | `xacThuc.dangNhap.lienKetCheDoThuong` | duyệt 2026-10-06 · ADR-0116 QC2 |
| Nút hiện/ẩn mật khẩu (dùng chung với màn đổi mật khẩu bắt buộc) | Hiện mật khẩu ↔ Ẩn mật khẩu | `xacThuc.matKhau.hien` · `xacThuc.matKhau.an` | [AuthField.md](../Components/AuthField.md) §Accessibility |
| Nhãn nhóm chọn ngôn ngữ | Ngôn ngữ | `chung.ngonNgu` | [LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Accessibility |
| Tiêu đề khu lỗi (tiền tố vai) | Lỗi | `chung.thongBao.loi` | [NoticeBanner.md](../Components/NoticeBanner.md) §Accessibility |
| Khu lỗi — sai thông tin đăng nhập, chế độ thường | Tên đăng nhập hoặc mật khẩu không đúng. — **một** câu cho cả bốn ca, không chỉ ra phần nào sai | `loi.CORE.AUTH.INVALID_CREDENTIALS` | duyệt 2026-10-06 · [D1](../../luong/D1-dang-nhap.md) §4 · [auth.md](../../contracts/auth.md) §3 · lý do đổi câu ở dưới bảng |
| Khu lỗi — sai thông tin đăng nhập, chế độ vận hành | Mã đơn vị, tên đăng nhập hoặc mật khẩu không đúng. — câu đã duyệt cho form ba ô, giữ nguyên cho chế độ còn ba ô | `xacThuc.dangNhap.loiSaiThongTinVanHanh` | duyệt · [D1](../../luong/D1-dang-nhap.md) §4 · [auth.md](../../contracts/auth.md) §3 |
| Khu lỗi — máy chủ từ chối mã đơn vị cấu hình (`CORE.VALIDATION.FAILED` khoá `TenantCode`, chế độ thường) | Trang đăng nhập đang được cấu hình sai nên chưa đăng nhập được. Hãy báo cho quản trị hệ thống. | `xacThuc.dangNhap.loiCauHinhMaDonVi` | duyệt 2026-10-06 · ADR-0116 §Hệ quả, hàng *Lỗi kiểm dữ liệu của `TenantCode`* |
| Khu lỗi — tài khoản bị khoá | Tài khoản đang bị khoá. Liên hệ quản trị viên của đơn vị để được mở khoá. | `loi.CORE.AUTH.LOCKED_OUT` | duyệt · [D1](../../luong/D1-dang-nhap.md) §4 · [auth.md](../../contracts/auth.md) §3, Ghi chú |
| Khu lỗi — gọi quá nhanh | Bạn thao tác quá nhanh. Thử lại sau {{RetryAfterSeconds}} giây. — tham số trùng tên tham số BE gửi kèm | `loi.CORE.RATE_LIMIT.EXCEEDED` | duyệt · [auth.md](../../contracts/auth.md) §10 · [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 |
| Khu lỗi — mất kết nối, phản hồi không hợp lệ, lỗi khác | Câu dịch theo mã; không có envelope thì chọn theo status: `0` hoặc lỗi không đến từ HTTP (timeout) → mất kết nối; ngoài 5xx (5xx đi lớp xuyên suốt, xem mục Trạng thái) → phản hồi không hợp lệ, kèm mã HTTP | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` · `loi.CORE.CLIENT.INVALID_RESPONSE` | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 · [be-cqrs-handler.md](../../quy-uoc/be-cqrs-handler.md) §7.4 |
| Lỗi dưới ô — bỏ trống · từ máy chủ | Câu của validator bắt buộc · câu dịch từ mã trong `fieldErrors` | `loi.CORE.CLIENT.VALIDATION_REQUIRED` · `loi.<mã>` | [fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §5.3 · [auth.md](../../contracts/auth.md) §3 · [09-forms-validation.md](../../wiki-core/fe/09-forms-validation.md) §4 |
| Dòng bản quyền ở `Footer` | © {{nam}} {{tenHeThong}} — năm và tên là tham số, không nhúng vào câu | `chung.chanTrang.banQuyen` | duyệt · [Footer.md](../Components/Footer.md) §API |
| Dòng phiên bản ở `Footer` (cả hai màn xác thực) | Phiên bản {{ma}} — `ma` là mã commit 7 ký tự của bản dựng, tham số không nhúng vào câu; đứng sau bản quyền, ngăn bằng dấu `·` trang trí. **Không có mã commit → ẩn cả phần phiên bản lẫn dấu `·`**, không chữ giữ chỗ | `chung.chanTrang.phienBan` | duyệt · [Footer.md](../Components/Footer.md) §Phiên bản hiển thị (Q59, 2026-10-06) |

**Vì sao câu `INVALID_CREDENTIALS` đổi theo chế độ.**

- Ở chế độ thường người dùng không thấy ô mã đơn vị. Một câu lỗi nhắc tới thứ họ không thấy và không sửa được chỉ đẩy họ đi tìm một ô không tồn tại. Câu lỗi phải nói bằng những gì người đọc nhìn thấy và làm được.
- Đổi câu **không** mở đường dò. Bốn ca vẫn gộp một mã, một câu **trong mỗi chế độ**. Câu đổi theo chế độ do chính người dùng chọn, không theo nguyên nhân trượt, nên không lộ ca nào đã xảy ra.
- Ca mã cấu hình lệch mã trong database ([D1](../../luong/D1-dang-nhap.md) §4) không cần câu nhắc mã đơn vị: người dùng thường không sửa được mã, cách sửa là build lại FE.

Khoá `loi.CORE.AUTH.INVALID_CREDENTIALS` giữ cho chế độ thường vì đó là chế độ của gần như mọi lần đăng nhập. Chế độ vận hành đè bằng một khoá riêng của màn.

### Trạng thái

Căn cứ: [auth.md](../../contracts/auth.md) §3, §5 · [D1](../../luong/D1-dang-nhap.md) §1, §2, bước 7–8 · ADR-0116 · [AuthCard.md](../Components/AuthCard.md) §Accessibility, §Trạng thái · [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 · [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8 · [07-auth-identity.md](../../wiki-core/fe/07-auth-identity.md) §9 · [FormRow.md](../Components/FormRow.md) §Trạng thái · [08-i18n.md](../../wiki-core/fe/08-i18n.md) §7.

- **mặc định** — chế độ thường: hai ô trống, không có khu lỗi, focus vào ô tên đăng nhập. Chế độ vận hành: ba ô trống, có mô tả, focus vào ô mã đơn vị. Luật chung của [AuthCard.md](../Components/AuthCard.md): focus vào ô đầu tiên còn trống.
- **đang tải** — không tải dữ liệu riêng: token chống giả mạo và trạng thái phiên lấy lúc app khởi động, mã cấu hình nhúng lúc build. Đang gửi → trạng thái `loading` của `AuthCard`, trang tự đặt cả ba dấu hiệu (khung không có input `loading`): nút chính quay; mọi ô đang hiện `disabled` và **giữ giá trị đã gõ**; liên kết chuyển chế độ **không dùng được** (`aria-disabled="true"`, không điều hướng, hình thức `disabled` của `Button`); khung không đổi kích thước.
- **rỗng** — không áp dụng. *Thiếu mã cấu hình* không phải trạng thái của màn — mục Cố ý không có.
- **chuyển chế độ** — bấm liên kết là điều hướng trên cùng tuyến, chỉ đổi tham số chế độ:
  - Giữ nguyên `returnUrl` đang có trên URL.
  - Giữ giá trị đã gõ ở ô tên đăng nhập và mật khẩu. Ô mã đơn vị **luôn trống** khi vào chế độ vận hành — không điền sẵn mã cấu hình, vì đó là mã của đơn vị nghiệp vụ, không phải mã người vận hành cần.
  - Xoá khu lỗi và mọi lỗi dưới ô của chế độ trước: chúng thuộc một lần gửi với mã đơn vị khác.
  - Focus vào ô đầu tiên còn trống của chế độ mới. Không còn ô trống thì vào ô đầu tiên.
- **lỗi** — request đăng nhập **tắt toast** (`BO_QUA_TOAST_LOI`); màn tự hiện ở khu lỗi của `AuthCard`, trừ lớp lỗi xuyên suốt (5xx, 403 `CORE.AUTH.*`): luôn toast kèm `traceId`, khu lỗi không hiện ([ADR-0094](../../adr/0094-lop-loi-xuyen-suot-luon-toast-ke-ca-khi-man-tat-toast.md)):
  - `CORE.AUTH.INVALID_CREDENTIALS` → khu lỗi, câu theo chế độ. 🛑 **Không** tô đỏ ô nào, kể cả ô mã đơn vị ở chế độ vận hành.
  - `CORE.AUTH.LOCKED_OUT` → khu lỗi, câu riêng.
  - `CORE.AUTH.CSRF_REJECTED` → interceptor lấy token mới, gửi lại **một** lần; vẫn hỏng → toast (lớp xuyên suốt), không vào khu lỗi.
  - `CORE.RATE_LIMIT.EXCEEDED` → khu lỗi, câu mang số giây chờ; nhánh 429 của `errorInterceptor` tôn trọng `BO_QUA_TOAST_LOI`, không toast chồng.
  - Không có envelope, ngoài 5xx → khu lỗi: status `0` hoặc lỗi không đến từ HTTP (timeout, mất mạng) → câu mất kết nối; status khác, không envelope → câu phản hồi không hợp lệ, kèm mã HTTP.
  - Hết phiên → không áp dụng. Người bị đẩy về đây vì hết phiên (có `returnUrl`) **không** thấy lời giải thích thêm.
  - Mọi nhánh trên — màn ở lại: form mở khoá rồi focus trở về phần tử đang giữ focus ngay trước lúc gửi — [AuthCard.md](../Components/AuthCard.md) §Accessibility, dòng *Focus sau khi gửi hỏng*.
- **kiểm tra dữ liệu** — mọi ô đang hiện đều bắt buộc. Lỗi thiếu hiện **dưới đúng ô** qua `errorMessage` của `AuthField`, chỉ sau khi rời ô hoặc bấm gửi (luật ba nhánh, thi công ở `fieldErrorText`). Riêng ô nhận focus lúc màn mở: rời ô hay chuyển cửa sổ khi chưa sửa gì thì **không** báo — [FormRow.md](../Components/FormRow.md) §Trạng thái, bảng *Lỗi hiện khi nào* (chốt 2026-10-08). Bấm gửi khi còn ô thiếu → **không** gọi API, focus về ô lỗi đầu tiên; **không** có khối tóm tắt lỗi đầu form, kể cả khi nhiều ô cùng lỗi ([AuthCard.md](../Components/AuthCard.md) §Accessibility, dòng *Tóm tắt lỗi*). `CORE.VALIDATION.FAILED`:
  - khoá `UserName` / `Password` → dưới ô tương ứng;
  - khoá `TenantCode` → dưới ô mã đơn vị ở chế độ vận hành; **khu lỗi** với câu cấu hình ở chế độ thường — không dùng câu của mã trong `fieldErrors`, vì câu đó nói về một ô không có trên màn;
  - khoá không khớp ô nào → khu lỗi, không toast.

  Lỗi xác thực **không bao giờ** vào ô.

Thành công: phiên thiết lập từ **DTO phiên trong chính response `login`** (cùng kiểu với `me` — [auth.md](../../contracts/auth.md) §5); **không** gọi `me` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.6 · [D1](../../luong/D1-dang-nhap.md) §3 bước 8). Lấy lại token chống giả mạo ([D1](../../luong/D1-dang-nhap.md) §3 bước 7) — việc này và việc đọc DTO phiên **không phụ thuộc thứ tự**, nhưng **cả hai** phải xong trước khi điều hướng; rồi rẽ nhánh theo sơ đồ luồng. Ngôn ngữ áp theo `preferredLanguage` của DTO đó — không gọi hồ sơ chỉ để lấy ngôn ngữ. Hai chế độ rẽ nhánh giống nhau; tài khoản vận hành nhận cờ `isSystemOperator` từ cùng DTO.

### Responsive

Theo [AuthCard.md](../Components/AuthCard.md) §Responsive; không thêm hành vi riêng; không phần tử nào bị ẩn. Liên kết chuyển chế độ giữ căn giữa, dưới form, ở mọi ngưỡng.

| Ngưỡng | Cái gì đổi |
| --- | --- |
| < `$bp-sm` | `LanguageSwitcher` **giữ** `inline` trong `AuthCard` ([LanguageSwitcher.md](../Components/LanguageSwitcher.md) §Responsive) |
| < `$bp-md` | `Footer` `auth` **không đổi** — vẫn một hàng căn giữa ([Footer.md](../Components/Footer.md) §Responsive, chốt 2026-10-08) |
| Khung nhìn thấp hơn nội dung (ví dụ điện thoại nằm ngang) — không phải một ngưỡng px | Trang cuộn; `Footer` ngay sau khung. Đủ chỗ thì không cuộn, `Footer` sát đáy. Công thức ngưỡng ở [AuthCard.md](../Components/AuthCard.md) §Responsive. Chế độ vận hành cao hơn một ô nên chạm ngưỡng sớm hơn |

### Icon

| Chỗ | Icon | Căn cứ |
| --- | --- | --- |
| Icon dẫn ô mã đơn vị (chỉ chế độ vận hành) · tên đăng nhập · mật khẩu | `pi-building` · `pi-user` · `pi-key` | Biến thể `tenantCode` · `identifier` · `password` của [AuthField.md](../Components/AuthField.md) |
| Nút hiện/ẩn · icon vai khu lỗi · nút đang gửi | `pi-eye`/`pi-eye-slash` · `pi-times-circle` · `pi-spinner` | [Icons.md](../Icons.md) §5 |
| Liên kết chuyển chế độ | Không có icon | Liên kết có chữ; không thêm icon trang trí — cùng luật với nút Đăng xuất ở màn đổi mật khẩu bắt buộc |

### Ảnh màn hình

Chưa có ảnh nào lưu trong repo. Lượt audit 2026-10-05 có chạy ứng dụng và chụp màn này bằng Chrome không giao diện, nhưng ảnh không lưu lại — khu Design chưa có `Assets/` ([CLAUDE.md](../CLAUDE.md) §9).

### Cần chốt

Không còn.

`autocomplete` ô mã đơn vị là `organization` — biến thể `tenantCode` của [AuthField.md](../Components/AuthField.md). Khung xác thực **không có nút đổi theme**; lựa chọn đã lưu vẫn áp trước khi trang vẽ, đổi theme làm ở `Topbar` sau khi đăng nhập ([AuthCard.md](../Components/AuthCard.md) §Khi nào dùng).

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
  - Footer biến thể `auth`, cỡ `sm` — đáy cột, như màn Đăng nhập
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
- **đang tải** — không tải dữ liệu riêng (cờ, tên đăng nhập đã có trong DTO phiên). Đang gửi → trạng thái `loading` của `AuthCard` do trang đặt, như màn Đăng nhập. Nút Đăng xuất **vẫn bấm được** lúc form đang gửi.
- **rỗng** — không áp dụng.
- **lỗi** — request đổi mật khẩu và đăng xuất tắt toast, màn tự hiện, trừ lớp xuyên suốt như màn Đăng nhập:
  - `CORE.AUTH.CHANGE_PASSWORD_FAILED` → lỗi vào **ô** theo `fieldErrors` — ánh xạ theo mã, không theo endpoint.
  - Khoá `fieldErrors` không khớp ô nào (ví dụ `$record`) → khu lỗi, không toast.
  - `CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED` → cờ đã hạ ở chỗ khác: gọi lại `me` rồi rời màn như khi thành công; không hiện lỗi.
  - `CORE.RATE_LIMIT.EXCEEDED` (429) → khu lỗi, câu mang số giây chờ; hạn mức áp cho mọi endpoint; không toast chồng.
  - `CORE.AUTH.CSRF_REJECTED`, mất mạng → như màn Đăng nhập.
  - Hết phiên (401) → `SessionExpiryHandler` về Đăng nhập kèm `returnUrl`; mật khẩu vừa gõ mất.
  - Đăng xuất nhận 200 hoặc 401 → đã đăng xuất: dọn trạng thái, lấy lại token chống giả mạo, về Đăng nhập. Hỏng kiểu khác → khu lỗi (lớp xuyên suốt: toast, như màn Đăng nhập); **không** dọn trạng thái, ở lại màn.
  - Gửi đổi mật khẩu hỏng mà màn ở lại → form mở khoá rồi focus trở về chỗ cũ, như màn Đăng nhập ([AuthCard.md](../Components/AuthCard.md) §Accessibility, dòng *Focus sau khi gửi hỏng*).
- **kiểm tra dữ liệu** — ô mật khẩu hiện tại nhận focus lúc màn mở nên theo cùng ngoại lệ của màn Đăng nhập; không có khối tóm tắt lỗi đầu form. Ba ô bắt buộc; ô nhập lại khớp ô mật khẩu mới (kiểm ở FE, `CORE.CLIENT.VALIDATION_MISMATCH`); mật khẩu mới khác mật khẩu hiện tại do BE kiểm (`CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT` trong `fieldErrors.NewPassword`). Lỗi dưới đúng ô theo luật ba nhánh; lỗi máy chủ gắn qua `applyFieldErrors`, gỡ khi người dùng sửa ô. Ô mật khẩu mới nhận **nhiều** mã cùng lúc: hiện từng mã, mã đầu theo thứ tự BE trả; sửa ô hoặc gửi lại thì mã kế hiện ra (`fieldErrorText`).

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

Chưa có ảnh nào lưu trong repo. Lượt đo 2026-10-06 có chạy ứng dụng và chụp màn này, nhưng ảnh không lưu lại — khu Design chưa có `Assets/` ([CLAUDE.md](../CLAUDE.md) §9).

### Cần chốt

Không còn.
