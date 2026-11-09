---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Trang lỗi điều hướng — không có quyền, không tìm thấy, không tải được phần ứng dụng — màn hình

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Hai trang đầu đã có ở `src/FE` và đã dựng lại theo sơ đồ bố cục của file này — `PageHeader` cộng `EmptyState` cỡ `page`; mục thứ ba và ca toast kèm theo (chốt 2026-10-08) đã có code (đọc mã 2026-10-08), chưa đo trên trình duyệt — hai dòng cuối của bảng. Bảng "Có thật hôm nay → sẽ thành" ngay dưới khối Khung/Quyền ghi từng mục đã mở source ra so; §Responsive, `<title>` lúc chạy và ảnh màn hình chưa ai đối chiếu, nên `verified: chua-doi-chieu` giữ nguyên.

**Không có quyền** hiện khi `permissionGuard` từ chối một tuyến ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.3); **Không tìm thấy** hiện khi URL không khớp tuyến nào của FE (guard §1, tuyến `/**`). Cả hai không gọi API; gộp một file vì dùng chung phần thân và đường đi tiếp. **Không tải được phần ứng dụng** không phải một tuyến: nó thay chỗ màn khi phần mã của tuyến không tải về được; gộp vào đây vì cũng là một lần điều hướng hỏng và dùng cùng phần thân `EmptyState`.

> **Khung:** khung ứng dụng ([00-khung-ung-dung.md](./00-khung-ung-dung.md)) — hai màn đầu **trong** nhánh shell, sau `authGuard` (guard §1); người dùng còn `Sidebar` để đi tiếp ([AuthCard.md](../Components/AuthCard.md) §Khi nào dùng). Mục thứ ba chạy **không khung nào** — lý do ở chính mục đó.
> **Quyền:** không cần permission, nhưng **cần đăng nhập**: chưa đăng nhập thì `authGuard` đưa về đăng nhập kèm `returnUrl` (guard §3.2), đăng nhập xong mới tới trang lỗi nếu URL vẫn không vào được.

## Có thật hôm nay → sẽ thành

Đối chiếu 2026-09-22, chỉ bằng đọc mã (không chạy ứng dụng, không chạy test). Đường dẫn tính từ `src/FE/src/app/`. Hai trang cùng một khuôn — mỗi trang một `.page.ts` và một `.page.html` cùng tên dưới `platform/loi/`, không còn tệp style riêng — nên mỗi dòng áp cho cả hai.

| Khoản | Có thật hôm nay (neo bằng chuỗi) | Sẽ thành |
| --- | --- | --- |
| Tuyến trong khung, sau `authGuard`; `title` | `app.routes.ts` § `path: 'khong-co-quyen'` và § `path: '**'` nằm trong nhánh `ShellComponent` mang `authGuard`; `title` là `trangLoi.khongCoQuyen.tieuDe` / `trangLoi.khongTimThay.tieuDe` | — |
| `PageHeader` `minimal` mang `<h1>` | Có — `platform/loi/khong-co-quyen.page.html` và `platform/loi/khong-tim-thay.page.html` mở đầu bằng § `<app-page-header [title]=` với khoá `tieuDe` của màn, không truyền `description` hay hành động — đúng biến thể `minimal`; `<h1>` do `PageHeader` vẽ (`shared/components/page-header/page-header.component.html` § `<h1 [id]="headingId()"`) | — (đủ) |
| Thân là `EmptyState` cỡ `page`, biến thể `no-permission` / `not-found`, `headingLevel` 2 | Có — cùng hai tệp `.html`: § `<app-empty-state` với `variant="no-permission"` / `variant="not-found"`, `size="page"`, `[headingLevel]="2"`, `title` cùng khoá với `PageHeader`, `description` khoá `moTa` | — (đủ) |
| Icon mặc định của biến thể, màn không truyền `icon` | Có — hai tệp `.html` không có input `icon`; `EmptyState` tự lấy từ § `ICON_MAC_DINH` (`shared/components/empty-state/empty-state.component.ts`) | — (đủ) |
| Đường đi tiếp là liên kết tới `sauDangNhap`, không phải nút | Có — `[actionLabel]` khoá `diTiep`, `[actionRoute]="routes.sauDangNhap"` với `routes` là `inject(CORE_ROUTES)` trong hai tệp `.page.ts`; `EmptyState` vẽ nhánh route thành `<a … [routerLink]="actionRoute()">` (`empty-state.component.html` § `empty-state__hanh-dong-lien-ket`) | — (đủ) |
| Hình thức thân, hạ bậc đệm dưới `$bp-md` và `$bp-xs` | Đến từ `EmptyState` — `platform/loi/` không còn tệp `.scss`, không còn class riêng. Hình thức thật của cỡ `page` và §Responsive là việc đối chiếu của [EmptyState.md](../Components/EmptyState.md), không lặp ở đây | — |
| Ca kiểm tự động | Có tệp `platform/loi/trang-loi.page.spec.ts` — ba ca cho mỗi màn: `<h1>` duy nhất nằm trong `PageHeader`; thân là `EmptyState` cỡ `page` với `<h2>` và icon mặc định; đường đi tiếp là `<a>` tới `sauDangNhap`, không có `<button>`. **Chưa chạy** trong lượt này | Xanh trong cổng FE khi cổng đó chạy |
| Không tải được phần ứng dụng (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08). Chỗ bắt duy nhất: `core/navigation/loi-tai-phan-ma.ts` § `LoiTaiPhanMaService` nghe `Router.events`; § `laLoiTaiPhanMa` nhận ra lỗi tải phần mã qua ba mẫu câu của Chromium, Firefox, Safari; chưa có `NavigationEnd` nào thì § `this.location.replaceState(urlDich)` rồi bật § `khoiToanTrang`. `app.html` § `@if (loiTaiPhanMa.khoiToanTrang())` thay `<router-outlet />` bằng `<main class="khoi-loi-tai">` chứa `<app-empty-state` `variant="error"`, `size="page"`, `[headingLevel]="1"`, ba khoá của mục *Câu chữ*, `(actionClicked)="loiTaiPhanMa.taiLaiTrang()"` — § `taiLaiTrang` gọi `location.reload()`. `EmptyState` và `Button` import tĩnh ở `app.ts`, tức nằm trong phần tải lúc khởi động. `app.scss` § `.khoi-loi-tai`: `min-height: 100dvh`, nền `--color-bg`, đệm `--layout-page-pad`, dưới `$bp-md` (§ `@include s.duoi-man-hinh(s.$bp-md)`) đệm `--layout-page-pad-sm`, căn giữa hai trục — khớp sơ đồ. `src/FE/public/i18n/vi.json` có `trangLoi.khongTaiDuoc.tieuDe`, `trangLoi.khongTaiDuoc.taiLai`, `loi.CORE.CLIENT.RESOURCE_LOAD_FAILED`, câu khớp mục *Câu chữ*. Ca kiểm: `loi-tai-phan-ma.spec.ts`, `app.spec.ts` (`khoiToanTrang bật → đúng một <h1>, một nút, và KHÔNG còn router-outlet`, `bấm nút → tải lại toàn trang`); e2e `src/FE/e2e/man/02-trang-loi.spec.ts` khối `không tải được phần ứng dụng — chưa có màn nào (khối toàn trang)` (người soạn chưa chạy test) | Chưa đo trên trình duyệt. Ba mẫu câu lỗi của trình duyệt không chuẩn nào ghim — trình duyệt đổi câu thì khối không hiện mà không gì báo (chú thích tại § `MAU_LOI_TAI_PHAN_MA`) |
| Điều hướng hỏng khi đã có màn đang hiện (chốt 2026-10-08) | **Có** (đọc mã 2026-10-08): cùng chỗ bắt như dòng trên; đã có `NavigationEnd` thì `core/navigation/loi-tai-phan-ma.ts` § `this.toast.loi(this.translate.instant('loi.CORE.CLIENT.RESOURCE_LOAD_FAILED'), null, {` kèm hành động nhãn `trangLoi.khongTaiDuoc.taiLai`, chạy § `taiLaiTrang()`. Vai `error` không tự tắt; nút vẽ theo [Toast.md](../Components/Toast.md) bảng đầu tệp, dòng *Nút hành động* — nơi ghép `app.html` truyền mẫu nút, khớp §API của `Toast` (chốt 2026-10-09). Ca kiểm: `loi-tai-phan-ma.spec.ts` (`phần mã hỏng SAU NavigationEnd đầu → toast danger kèm nút "Tải lại trang", không khối`, `nút trong toast gọi tải lại toàn trang đúng một lần`); e2e `src/FE/e2e/man/02-trang-loi.spec.ts` khối `không tải được phần ứng dụng — đã có màn đang hiện (toast)` (người soạn chưa chạy test) | Chưa đo trên trình duyệt |

---

## Quyết định chung

- Thân là [EmptyState](../Components/EmptyState.md) cỡ `page`, biến thể **`no-permission`** (403) / **`not-found`** (404); icon mặc định của biến thể (§Biến thể), màn không truyền `icon`. `headingLevel` = 2 vì thân nằm dưới `<h1>` của `PageHeader`. `EmptyState` không tự vẽ nền hay viền (§Token dùng).
- Đường đi tiếp qua `actionRoute` = `sauDangNhap` của seam `CORE_ROUTES` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §6, luật F21). `actionRoute` khác `null` → hành động vẽ thành `<a>`, không phải `Button`, `actionClicked` không phát ([EmptyState.md](../Components/EmptyState.md) §Accessibility · [Button.md](../Components/Button.md) §Khi nào dùng). Không quay lại theo lịch sử trình duyệt: người mở thẳng URL không có trang trước.

---

## Không có quyền (`/khong-co-quyen`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md (Sidebar có Footer `sidebar` ở đáy · Topbar · main)
  - main — đệm --layout-page-pad; không trần bề rộng
    - PageHeader `minimal` — tiêu đề <h1> (mọi trang trong khung mở đầu bằng PageHeader)
    - EmptyState cỡ `page`, biến thể `no-permission`, headingLevel 2
      - icon · tiêu đề <h2> · mô tả — đệm dọc theo cỡ `page` (EmptyState.md §Kích thước)
      - đường đi tiếp — actionRoute = sauDangNhap, nên hành động vẽ thành liên kết
```

### Câu chữ

Mọi câu do người dùng duyệt 2026-09-15; ý lấy từ [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.3 và khuôn câu `no-permission` ở [EmptyState.md](../Components/EmptyState.md) §Viết câu chữ.

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề trang (`title` tuyến), tiêu đề `PageHeader` và tiêu đề thân | Không có quyền truy cập — nói rõ là **không có quyền** | `trangLoi.khongCoQuyen.tieuDe` |
| Mô tả | Tài khoản của bạn không có quyền mở trang này. Liên hệ quản trị viên của đơn vị để được cấp quyền. | `trangLoi.khongCoQuyen.moTa` |
| Nhãn đường đi tiếp | Về trang chủ | `trangLoi.khongCoQuyen.diTiep` |

### Trạng thái

- **mặc định** — nội dung tĩnh: icon, tiêu đề, mô tả, đường đi tiếp.
- **đang tải · rỗng · lỗi · kiểm tra dữ liệu** — không áp dụng: màn không gọi API, không có form; bản thân màn **là** một trạng thái rỗng có nghĩa. Trạng thái tải của `Sidebar`/`Topbar` thuộc [00-khung-ung-dung.md](./00-khung-ung-dung.md).

Màn **không** bắn toast "không có quyền" — toast đó thuộc nhánh 403 của một request API, còn đây là guard chặn tuyến ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §8).

### Responsive

Khung theo [00-khung-ung-dung.md](./00-khung-ung-dung.md). Thân hạ bậc đệm theo [EmptyState.md](../Components/EmptyState.md) §Responsive: dưới `$bp-md` `page` → `default`; dưới `$bp-xs` hạ thêm một bậc. Tiêu đề tuyến trên `Topbar` ẩn dưới `$bp-xs` vẫn ở `<h1>` của `PageHeader`. Không có gì mất đi.

### Icon

| Chỗ | Icon | Căn cứ |
| --- | --- | --- |
| Thân trang lỗi | `pi-lock` | [Icons.md](../Icons.md) §5, dòng khoá / không có quyền (403); mặc định của biến thể `no-permission` |
| Đường đi tiếp | Không có icon | Liên kết có chữ; không thêm icon trang trí |

### Ảnh màn hình

Chưa có — hai trang lỗi đã dựng ở `src/FE` theo sơ đồ (bảng "Có thật hôm nay → sẽ thành" ở đầu file) nhưng chưa ai chạy ứng dụng để chụp; lượt đối chiếu 2026-09-22 chỉ đọc mã.

### Cần chốt

Không còn.

---

## Không tìm thấy (`/**`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md (Sidebar có Footer `sidebar` ở đáy · Topbar · main)
  - main — đệm --layout-page-pad; không trần bề rộng
    - PageHeader `minimal` — tiêu đề <h1>
    - EmptyState cỡ `page`, biến thể `not-found`, headingLevel 2
      - icon · tiêu đề <h2> · mô tả — đệm dọc theo cỡ `page`
      - đường đi tiếp — actionRoute = sauDangNhap, nên hành động vẽ thành liên kết
```

### Câu chữ

Mọi câu do người dùng duyệt 2026-09-15; ý lấy từ [fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1 và luật "không đổ lỗi cho người dùng" ở [EmptyState.md](../Components/EmptyState.md) §Viết câu chữ.

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề trang, tiêu đề `PageHeader` và tiêu đề thân | Không tìm thấy trang — nói về **đường dẫn**, không về bản ghi | `trangLoi.khongTimThay.tieuDe` |
| Mô tả | Đường dẫn bạn mở không khớp trang nào của hệ thống. | `trangLoi.khongTimThay.moTa` |
| Nhãn đường đi tiếp | Về trang chủ | `trangLoi.khongTimThay.diTiep` |

Chỉ dành cho URL không khớp tuyến nào; ca "bản ghi không tồn tại" thuộc spec màn chi tiết đó (ví dụ [10-nguoi-dung.md](./10-nguoi-dung.md)).

### Trạng thái

- **mặc định** — nội dung tĩnh.
- **đang tải · rỗng · lỗi · kiểm tra dữ liệu** — không áp dụng, như màn Không có quyền.

### Responsive

Như màn Không có quyền. Không phần tử nào bị ẩn mà mất thông tin.

### Icon

| Chỗ | Icon | Căn cứ |
| --- | --- | --- |
| Thân trang lỗi | `pi-compass` | [Icons.md](../Icons.md) §5, dòng không tìm thấy đích; mặc định của biến thể `not-found` |
| Đường đi tiếp | Không có icon | Liên kết có chữ; không thêm icon trang trí |

### Ảnh màn hình

Chưa có — hai trang lỗi đã dựng ở `src/FE` nhưng chưa ai chạy ứng dụng để chụp; lượt đối chiếu 2026-09-22 chỉ đọc mã.

### Cần chốt

Không còn.

---

## Không tải được phần ứng dụng (mọi URL — không có tuyến riêng)

Chốt 2026-10-08: **hiện câu báo lỗi kèm nút tải lại, thay cho trang trắng.** Quyết định này thay chốt 2026-09-11 "không làm ở v1" ở [17-phuc-vu-va-trien-khai.md](../../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §3.3.

**Khi nào hiện.** Một lần điều hướng hỏng vì không tải được phần mã của tuyến (mã phía client `CORE.CLIENT.RESOURCE_LOAD_FAILED`, [be-cqrs-handler.md](../../quy-uoc/be-cqrs-handler.md) §7.4) **trước khi màn nào kịp hiện** trong lần tải trang đó. Ca thường gặp: mở ứng dụng ngay sau khi bản mới được triển khai, hoặc mạng rớt đúng lúc tải. Hôm nay ca này ra trang trắng. Điều hướng hỏng khi **đã có** màn đang hiện là ca khác — mục kế tiếp.

| Quyết định | Vì sao |
| --- | --- |
| Không `Sidebar`, không `Topbar`, không `AuthCard` | Phần mã của khung có thể chính là phần vừa hỏng; và lúc này chưa biết người dùng đang hướng tới màn xác thực hay màn trong khung |
| Khối báo lỗi và mọi thứ nó dùng — `EmptyState`, `Button`, ba câu dịch — nằm trong phần tải lúc khởi động, không trong phần tải theo tuyến | Đặt nó trong phần tải theo tuyến thì nó hỏng cùng lúc với thứ nó báo |
| Nút tải lại **toàn trang** tại đúng địa chỉ đang ở trên thanh địa chỉ, không điều hướng trong ứng dụng | Điều hướng trong ứng dụng lại xin đúng phần mã cũ vừa hỏng; tải lại toàn trang mới lấy bản mới. Ca này chỉ xảy ra khi chưa màn nào hiện, nên địa chỉ trên thanh chính là đích người dùng muốn tới |

### Sơ đồ bố cục

```text
- body
  - main — cao tối thiểu 100dvh, nền --color-bg, đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm); căn khối giữa cả dọc lẫn ngang
    - EmptyState cỡ `page`, biến thể `error`, headingLevel 1 — icon mặc định của biến thể; trang này không có `PageHeader` nên tiêu đề của khối là `<h1>` duy nhất
      - icon · tiêu đề · mô tả — đệm dọc theo cỡ `page`
      - nút "Tải lại trang" — actionLabel, actionRoute = null nên vẽ Button và phát actionClicked; trang tải lại toàn bộ tại địa chỉ hiện tại
```

### Câu chữ

Ba câu do người dùng duyệt 2026-10-08. Mô tả dùng khoá của chính mã lỗi, theo khuôn `loi.<mã>` ([fe-ui-conventions.md](../../quy-uoc/fe-ui-conventions.md) §5.3); ý của câu theo khuôn biến thể `error` ở [EmptyState.md](../Components/EmptyState.md) §Viết câu chữ — nói rõ nên làm gì tiếp. Cả ba khoá cũng dùng cho ca toast ở mục *Điều hướng hỏng khi đã có màn đang hiện*.

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Tiêu đề khối | Không tải được trang | `trangLoi.khongTaiDuoc.tieuDe` |
| Mô tả | Hệ thống vừa được cập nhật hoặc kết nối bị gián đoạn nên trang chưa tải xong. Tải lại trang để tiếp tục. | `loi.CORE.CLIENT.RESOURCE_LOAD_FAILED` |
| Nút | Tải lại trang | `trangLoi.khongTaiDuoc.taiLai` |

### Trạng thái

- **mặc định** — nội dung tĩnh: icon, tiêu đề, mô tả, nút.
- **đang tải** — không áp dụng ở mức khối: bấm nút là trình duyệt tải lại cả trang, khối biến mất cùng trang cũ.
- **lỗi** — khối này chính là trạng thái lỗi. Tải lại mà vẫn hỏng thì khối hiện lại, cùng câu.
- **rỗng · kiểm tra dữ liệu** — không áp dụng: không có dữ liệu, không có form.

### Responsive

Thân hạ bậc đệm theo [EmptyState.md](../Components/EmptyState.md) §Responsive; đệm `main` đổi theo dòng trong sơ đồ. Không phần tử nào bị ẩn.

### Icon

| Chỗ | Icon | Căn cứ |
| --- | --- | --- |
| Thân | `pi-times-circle` | Mặc định của biến thể `error` — [EmptyState.md](../Components/EmptyState.md) §Biến thể |
| Nút tải lại | Không có icon | Nút có chữ; không thêm icon trang trí — cùng luật với đường đi tiếp của hai màn trên |

### Ảnh màn hình

Chưa có — khối đã dựng ở `src/FE` (bảng "Có thật hôm nay → sẽ thành" ở đầu file) nhưng chưa ai chụp; lượt đối chiếu 2026-10-08 chỉ đọc mã.

### Cần chốt

Không còn. Ba câu hỏi cũ đã trả lời 2026-10-08 (người dùng): (1) `EmptyState` nhận `headingLevel` 1 — [EmptyState.md](../Components/EmptyState.md) §API; (2) ca đã có màn đang hiện đi qua toast — mục kế tiếp; (3) ba câu đã duyệt — mục Câu chữ.

---

## Điều hướng hỏng khi đã có màn đang hiện (toast, không có tuyến)

Chốt 2026-10-08: **giữ nguyên màn đang hiện** và bật một [Toast](../Components/Toast.md) vai `danger` **không tự tắt**, kèm nút "Tải lại trang". Không thay màn bằng khối *Không tải được phần ứng dụng*: người dùng có thể đang có dữ liệu dở trên màn đó. Ca thường gặp: bấm sang khu khác, hoặc đăng nhập xong mà phần mã của khung ứng dụng không tải được.

| Phần tử | Câu hiển thị | Khoá i18n |
| --- | --- | --- |
| Nội dung toast | Hệ thống vừa được cập nhật hoặc kết nối bị gián đoạn nên trang chưa tải xong. Tải lại trang để tiếp tục. | `loi.CORE.CLIENT.RESOURCE_LOAD_FAILED` (dùng lại khoá của mô tả khối ở trên) |
| Nhãn nút trong toast | Tải lại trang | `trangLoi.khongTaiDuoc.taiLai` (dùng lại khoá của nút khối ở trên) |

| Quyết định | Quy định |
| --- | --- |
| Vai, tự tắt | `danger`, **không tự tắt** — Toast vai `danger` vốn không tự tắt ([Toast.md](../Components/Toast.md) §Biến thể); đồng hồ phải tắt hẳn bằng cờ `sticky` |
| Nút "Tải lại trang" | Tải lại **toàn trang** tại địa chỉ hiện tại, như nút của khối toàn trang: điều hướng trong ứng dụng lại xin đúng phần mã vừa hỏng. Dữ liệu dở trên màn bị mất — vì vậy đây là hành động người dùng chủ động bấm, không bao giờ tự chạy |
| Không có tiêu đề riêng | Toast chỉ mang câu mô tả; `trangLoi.khongTaiDuoc.tieuDe` không dùng ở ca này |
| Focus, a11y của nút | [Toast.md](../Components/Toast.md) §Accessibility, hàng *Toast có nút hành động* |

Ca này **không** có sơ đồ bố cục riêng và không có tuyến: màn đang hiện giữ nguyên, toast nằm ở ngăn xếp của `Toast` theo [Toast.md](../Components/Toast.md) §Responsive.

Hiện trạng code (đọc mã 2026-10-08): bảng đầu file, dòng *Điều hướng hỏng khi đã có màn đang hiện*. Nút hành động của toast đã có, khớp §API của `Toast` — [Toast.md](../Components/Toast.md) bảng đầu file, dòng *Nút hành động*.
