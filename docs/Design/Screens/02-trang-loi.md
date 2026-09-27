---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Trang lỗi điều hướng — không có quyền và không tìm thấy — màn hình

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Hai trang đã có ở `src/FE` và đã dựng lại theo sơ đồ bố cục của file này — `PageHeader` cộng `EmptyState` cỡ `page`. Bảng "Có thật hôm nay → sẽ thành" ngay dưới khối Khung/Quyền ghi từng mục đã mở source ra so; §Responsive, `<title>` lúc chạy và ảnh màn hình chưa ai đối chiếu, nên `verified: chua-doi-chieu` giữ nguyên.

**Không có quyền** hiện khi `permissionGuard` từ chối một tuyến ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §3.3); **Không tìm thấy** hiện khi URL không khớp tuyến nào của FE (guard §1, tuyến `/**`). Cả hai không gọi API; gộp một file vì dùng chung phần thân và đường đi tiếp.

> **Khung:** khung ứng dụng ([00-khung-ung-dung.md](./00-khung-ung-dung.md)) — cả hai màn **trong** nhánh shell, sau `authGuard` (guard §1); người dùng còn `Sidebar` để đi tiếp ([AuthCard.md](../Components/AuthCard.md) §Khi nào dùng).
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

---

## Quyết định chung

- Thân là [EmptyState](../Components/EmptyState.md) cỡ `page`, biến thể **`no-permission`** (403) / **`not-found`** (404); icon mặc định của biến thể (§Biến thể), màn không truyền `icon`. `headingLevel` = 2 vì thân nằm dưới `<h1>` của `PageHeader`. `EmptyState` không tự vẽ nền hay viền (§Token dùng).
- Đường đi tiếp qua `actionRoute` = `sauDangNhap` của seam `CORE_ROUTES` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §6, luật F21). `actionRoute` khác `null` → hành động vẽ thành `<a>`, không phải `Button`, `actionClicked` không phát ([EmptyState.md](../Components/EmptyState.md) §Accessibility · [Button.md](../Components/Button.md) §Khi nào dùng). Không quay lại theo lịch sử trình duyệt: người mở thẳng URL không có trang trước.

---

## Không có quyền (`/khong-co-quyen`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md (Sidebar · Topbar · main · Footer `full`)
  - main — rộng tối đa --layout-container-max, đệm --layout-page-pad
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
- Khung ứng dụng — 00-khung-ung-dung.md (Sidebar · Topbar · main · Footer `full`)
  - main — rộng tối đa --layout-container-max, đệm --layout-page-pad
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
