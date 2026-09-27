---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Trang chủ — trang chào của Core — màn hình

🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Tuyến và một trang tối giản đã có ở `src/FE` (`platform/trang-chu/`), nhưng trang đó chỉ có lời chào và tên đơn vị — chưa có `PageHeader`, chưa có khối lối tắt, chưa có trạng thái nào. Bảng "Có thật hôm nay → sẽ thành" ngay dưới ghi đúng phần đã mở source ra so ngày 2026-09-22, chỉ bằng đọc mã. Phần còn lại là đích đến; nhiều ô còn mang tiền tố *Chờ duyệt:* — người dựng chưa được coi các ô đó là câu cuối ([CLAUDE.md](../CLAUDE.md) §8).

Màn đầu tiên sau khi đăng nhập ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §1: `/` chuyển hướng về `/trang-chu`). Core chỉ cấp **trang chào tối giản** — lời chào, tên đơn vị, lối tắt tới các màn người dùng có quyền; không gọi endpoint riêng, chỉ dùng lại thông tin phiên và cây menu đã tải cho `Sidebar` ([fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.3). **Dự án cấp `CORE_HOME` thì spec này không áp dụng** — trang chủ của dự án thay chỗ trang chào, và spec của nó thuộc dự án.

> **Khung:** khung ứng dụng ([00-khung-ung-dung.md](./00-khung-ung-dung.md))
> **Quyền:** không cần permission — như mọi tuyến trong khung, qua `authGuard` và `mustChangePasswordGuard` ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §2.1, §4). Lối tắt nào hiện do máy chủ lọc theo quyền khi trả menu ([meta-menu.md](../../contracts/meta-menu.md) §1.4); màn không kiểm quyền lần hai.

## Có thật hôm nay → sẽ thành

Đối chiếu 2026-09-22, chỉ bằng đọc mã (không chạy ứng dụng), với `src/FE/src/app/platform/trang-chu/trang-chu.routes.ts`, `src/FE/src/app/platform/trang-chu/pages/trang-chu/trang-chu.page.ts`, `trang-chu.page.html` cùng thư mục, `src/FE/src/app/core/menu/menu.store.ts` và `src/FE/public/i18n/vi.json`. Neo bằng chuỗi tìm được trong tệp, không bằng số dòng.

| Khoản | Có thật hôm nay | Sẽ thành |
| --- | --- | --- |
| Tuyến | Có — `trang-chu.routes.ts`: `path: ''`, `title: 'trangChu.tieuDe'`, `loadComponent` nạp `TrangChuPage`. Chuỗi `CORE_HOME` chỉ xuất hiện trong chú thích, tuyến **chưa đọc** token | Tuyến đọc `CORE_HOME` theo [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.5 — việc của `frontend-expert`, ngoài spec này |
| Trang | Có — `trang-chu.page.html`: `<h1>` mang khoá `trangChu.loiChao` với tham số `hoTen`, một `<p>` mang khoá `trangChu.tenDonVi`; đọc phiên qua `auth.nguoiDung()`. Không có `PageHeader`, không có lối tắt, không đọc menu, không có trạng thái đang tải / rỗng / lỗi | Bố cục và năm trạng thái ở dưới, sau khi các ô *Chờ duyệt:* được duyệt |
| Khoá i18n | `vi.json`: `trangChu.tieuDe` = "Trang chủ", `trangChu.loiChao` = "Xin chào, {{hoTen}}", `trangChu.tenDonVi` = "{{tenDonVi}}" — khoá thứ ba có giá trị **chỉ là tham số**, tức một câu không có chữ nào để dịch | Theo bảng Câu chữ; khoá `trangChu.tenDonVi` bỏ nếu duyệt phương án mô tả gộp |
| Nguồn menu để dựng lối tắt | Có sẵn để dùng lại — `menu.store.ts`: `MenuStore` lộ `items` (cây đã dựng) và `state` (`'idle' \| 'loading' \| 'error' \| 'empty'`); cùng thứ `Sidebar` đọc. Trang **chưa** đọc | Trang đọc cùng store, không gọi request riêng |

**Chưa đối chiếu (lỗ mù):** mọi thứ chỉ thấy khi chạy ứng dụng; hành vi của `CoreTitleStrategy` với tuyến này; §Responsive; ảnh màn hình.

---

## Trang chào của Core (`/trang-chu`)

### Sơ đồ bố cục

```text
- Khung ứng dụng — 00-khung-ung-dung.md
  - main — đệm --layout-page-pad (dưới $bp-md: --layout-page-pad-sm)
    - PageHeader — title + description, KHÔNG có nhóm hành động (biến thể default với slot hành động
        để trống; minimal không dùng vì minimal bỏ cả mô tả)
      - title: tiêu đề tuyến (ô "Tiêu đề <h1>" ở bảng Câu chữ)
      - description: lời chào kèm tên đơn vị — hoTen, tenantName lấy từ phiên (auth.md §5)
    - khối lối tắt — <nav> có aria-label riêng (KHÁC nhãn của Sidebar), khe dưới PageHeader --sp-8
      - dữ liệu: cây menu đã tải cho Sidebar (meta-menu.md §1), BỎ mục trang-chu (mục có code
          `trang-chu`, hoặc route trỏ về chính tuyến này) — lối tắt về chỗ đang đứng là vô nghĩa
      - một Card default cỡ md cho MỖI nhóm, xếp lưới (mục Responsive):
        - nhóm đầu (chỉ khi có): mục gốc có route — Card KHÔNG heading
        - mỗi mục cha có ít nhất một con có route: Card heading = bản dịch labelKey của mục cha,
            headingLevel 2; mục cha không còn con nào có route thì KHÔNG vẽ Card
        - thân Card: <ul> — mỗi <li> một <a routerLink="route">:
            icon của mục (--icon-md, aria-hidden; mục không có icon thì không vẽ icon, nhãn vẫn
            thẳng hàng) + khe --sp-4 + bản dịch labelKey; thứ tự: giữ nguyên thứ tự máy chủ trả,
            màn KHÔNG tự sắp — tiêu chí sắp xếp thuộc meta-menu.md §1
      - thay cả khối bằng SkeletonLoader / EmptyState theo mục Trạng thái
  - Footer
```

| Quyết định bố cục | Căn cứ |
| --- | --- |
| *Chờ duyệt:* `<h1>` của `PageHeader` là **tiêu đề tuyến** ("Trang chủ"), lời chào xuống `description`. Phương án thay thế: `<h1>` là lời chào như code hôm nay | [00-khung-ung-dung.md](./00-khung-ung-dung.md) §Responsive "Ẩn đi đâu": dưới `$bp-xs` `Topbar` ẩn tiêu đề tuyến và tiêu đề đó **phải còn** ở `<h1>` của `PageHeader`; `<title>` tài liệu cũng là tiêu đề tuyến ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §2.2) — `<h1>` khác `<title>` là hai tên cho một trang |
| *Chờ duyệt:* lối tắt là **danh sách liên kết trong `Card` `default`, một `Card` mỗi nhóm menu** — không phải lưới thẻ bấm được, không phải nút. Phương án thay thế: mỗi lối tắt một `Card` `interactive` | Điều hướng phải là `<a>` ([Button.md](../Components/Button.md) §Khi nào dùng); `Card` `interactive` hôm nay chỉ là `<button>` và API chưa có input tuyến ([Card.md](../Components/Card.md) §Đã có → còn thiếu) — chọn thẻ bấm được là mở rộng `Card`, quyết ở spec component. `<a>` mượn hình thức nút chỉ được phép trong nhóm hành động của màn ([Button.md](../Components/Button.md)), khối lối tắt không phải nhóm đó |
| Không kiểm quyền ở màn; không hiện mục người dùng không có quyền | Menu về đã lọc ([meta-menu.md](../../contracts/meta-menu.md) §1.4); `Sidebar` cũng ẩn chứ không làm mờ ([Sidebar.md](../Components/Sidebar.md) §Trạng thái) |
| Không có ô số liệu, không có biểu đồ, không gọi endpoint riêng | [fe-architecture.md](../../quy-uoc/fe-architecture.md) §2.3 — bảng tổng hợp là nghiệp vụ của dự án, đi qua `CORE_HOME` |

**Liên kết lối tắt — trạng thái của từng `<a>`** (cùng khuôn liên kết ở [Footer.md](../Components/Footer.md) §Trạng thái, cỡ chữ của thân trang):

| Trạng thái | Xử lý |
| --- | --- |
| `default` | Chữ `--color-text`, `--fs-md`, `--fw-medium`, `--lh-normal`; icon kế thừa màu chữ; khe giữa hai liên kết `--sp-3`; không gạch chân — nằm trong `<nav>`, cả danh sách là liên kết |
| `hover` | Chữ và icon `--color-brand`, gạch chân hiện; bọc `@media (hover: hover)` |
| `focus-visible` | `outline` `--border-w-strong` màu `--color-focus`, `outline-offset: 2px` |
| `active` | Chữ `--color-brand-active`, không dịch chuyển |
| `disabled` | **Không áp dụng** — mục không có quyền không được vẽ |

### Câu chữ

Câu không ghi nguồn và không mang tiền tố *Chờ duyệt:* là câu đã có trong `vi.json` và đã qua một màn khác ([Screen.md](../Templates/Screen.md) §2).

| Phần tử | Câu hiển thị | Khoá i18n | Nguồn |
| --- | --- | --- | --- |
| Tiêu đề tab | Trang chủ | `trangChu.tieuDe` | có trong `vi.json` |
| Tiêu đề `<h1>` của `PageHeader` | *Chờ duyệt:* Trang chủ — cùng khoá với tiêu đề tab | `trangChu.tieuDe` | bảng Quyết định bố cục |
| Mô tả dưới tiêu đề | *Chờ duyệt:* Xin chào, {{hoTen}}. Bạn đang làm việc tại {{tenDonVi}}. — một khoá hai tham số, thay cho hai khoá hôm nay (`trangChu.loiChao` chỉ có `hoTen`; `trangChu.tenDonVi` bỏ) | `trangChu.loiChao` | [08-i18n.md](../../wiki-core/fe/08-i18n.md) §3.1 — không ghép câu từ nhiều khoá |
| `aria-label` của `<nav>` lối tắt | *Chờ duyệt:* Lối tắt | `trangChu.loiTat.nhan` | phải khác `khung.dieuHuong.nhan` để hai vùng điều hướng phân biệt được |
| Tiêu đề nhóm (`heading` của `Card`) · nhãn lối tắt | Bản dịch `labelKey` của mục cha · của mục — khoá **đến từ dữ liệu** | `menu.*` | [meta-menu.md](../../contracts/meta-menu.md) §1.2 |
| Rỗng — tiêu đề · mô tả | *Chờ duyệt:* Chưa có chức năng nào được cấp · Liên hệ quản trị viên của đơn vị để được cấp quyền. — cùng ý với `khung.dieuHuong.trong` của `Sidebar`, tách làm hai câu theo khuôn tiêu đề/mô tả của `EmptyState` | `trangChu.trong.tieuDe` · `trangChu.trong.moTa` | [EmptyState.md](../Components/EmptyState.md) §Viết câu chữ |
| Lỗi — tiêu đề | *Chờ duyệt:* Không tải được danh sách chức năng | `trangChu.loi.taiThatBai` | cùng khuôn "Không tải được danh sách …" ở [11-vai-tro.md](./11-vai-tro.md) |
| Lỗi — thân | Câu dịch theo mã; không có mã thì câu mất kết nối | `loi.<mã>` · `loi.CORE.CLIENT.NO_CONNECTION` | [fe-api-client.md](../../quy-uoc/fe-api-client.md) §2.2 |
| Nút thử lại | Thử lại | `chung.thuLai` | có trong `vi.json` |
| Đọc lên khi đang tải (`aria-live`, ẩn) | Đang tải… | `chung.dangTai` | có trong `vi.json`; cùng cách [03-ho-so-ca-nhan.md](./03-ho-so-ca-nhan.md) |

Mã lỗi → chỗ hiện: màn không gọi endpoint riêng. Request menu là của khung và mang `BO_QUA_TOAST_LOI` ([00-khung-ung-dung.md](./00-khung-ung-dung.md) §Trạng thái — 5xx và 403 `CORE.AUTH.*` vẫn toast); mọi mã lỗi của nó hiện ở khối lỗi dưới đây. `CORE.AUTH.NOT_AUTHENTICATED` (401) và `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` đi đường chung ([fe-routing-guard.md](../../quy-uoc/fe-routing-guard.md) §5.4, §8).

### Trạng thái

Trạng thái của khối lối tắt **đi theo trạng thái menu của khung** — cùng một request, cùng một store; `Sidebar` và khối này đổi trạng thái cùng lúc. `PageHeader` luôn vẽ ngay: phiên đã có trước khi vào khung.

- **mặc định:** `PageHeader` + các `Card` lối tắt theo sơ đồ.
- **đang tải:** menu chưa về (cùng lúc `Sidebar` ở `loading`) → một `Card` `default` không heading, `loading` = `true` (`aria-busy`), thân là `SkeletonLoader` `text` ba dòng; cạnh đó một vùng ẩn `aria-live="polite"` đọc `chung.dangTai`. **Không** vẽ `EmptyState` trong lúc chờ ([EmptyState.md](../Components/EmptyState.md) §Trạng thái).
- **rỗng:** *Chờ duyệt:* `EmptyState` biến thể **`no-permission`**, cỡ `default`, `headingLevel` 2, **không có nút** — hiện khi menu về nhưng không còn lối tắt nào sau khi bỏ `trang-chu` (menu rỗng, hoặc chỉ có mục trang chủ). Chọn `no-permission` vì người dùng không tự cấp quyền được và tình huống trùng với `Sidebar` `empty`. Phương án thay thế: `not-configured` (nút tới trang cấu hình — Core không có trang đó nên nút không có đích).
- **lỗi:** tải menu hỏng (cùng lúc `Sidebar` ở `error`) → `EmptyState` `error` cỡ `default`, `headingLevel` 2, nút `secondary` "Thử lại" gọi lại **đúng request menu của khung** — bấm ở `Sidebar` hay ở đây đều làm cả hai vùng chuyển sang `loading`. Hết phiên → đường chung, không vẽ gì thêm.
- **kiểm tra dữ liệu:** không áp dụng — màn không có form.

### Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-lg` | `Card` xếp lưới, khe `--sp-8`; mỗi cột rộng tối thiểu `--layout-dialog-w-sm` (mượn bậc `sm` của thang bề rộng ở [DESIGN.md](../DESIGN.md) §6.1, cùng cách `EmptyState` mượn), số cột theo bề rộng `main` |
| `$bp-md` … `$bp-lg` | Lưới giảm số cột theo cùng bề rộng tối thiểu ([Card.md](../Components/Card.md) §Responsive) |
| < `$bp-md` | Một cột; đệm `main` → `--layout-page-pad-sm`; đệm `Card` hạ một bậc theo [Card.md](../Components/Card.md) |
| < `$bp-xs` | Tiêu đề `PageHeader` hạ xuống `--fs-lg` ([PageHeader.md](../Components/PageHeader.md) §Responsive); liên kết giữ nguyên hình thức |

**Ẩn đi đâu:** không ẩn gì. Tiêu đề tuyến bị `Topbar` ẩn dưới `$bp-xs` vẫn là `<h1>` của trang này.

### Icon

Theo [Icons.md](../Icons.md) §5 và §7. Icon từng lối tắt là `icon` của mục menu — dữ liệu, dùng thẳng làm class ([meta-menu.md](../../contracts/meta-menu.md) §1.3), `aria-hidden` vì nhãn đứng cạnh; mục không có `icon` thì không vẽ icon. `EmptyState` mang icon mặc định của biến thể (`pi-lock`, `pi-times-circle`); "Thử lại" là `pi-refresh`.

### Ảnh màn hình

Chưa có — trang đã dựng ở `src/FE` (bảng "Có thật hôm nay → sẽ thành" ở đầu file) nhưng chưa ai chạy ứng dụng chụp màn hình; lượt đối chiếu 2026-09-22 chỉ đọc mã.

### Cần chốt

- **Duyệt các ô mang *Chờ duyệt:*** trong file này — ba lựa chọn (`<h1>`, dạng lối tắt, biến thể `EmptyState` ca rỗng) và các câu chữ mới. Người dùng duyệt; duyệt xong xoá tiền tố ([CLAUDE.md](../CLAUDE.md) §8).
- **Nếu chọn thẻ bấm được thay cho danh sách liên kết:** [Card.md](../Components/Card.md) phải mở rộng biến thể `interactive` sang dạng `<a>` (input tuyến) trước khi dựng — khoảng trống đã ghi ở bảng "Đã có → còn thiếu" của chính spec đó. `design-expert` viết, sau khi người dùng chọn.
- **Mục gốc có `route` gom vào `Card` không heading:** dữ liệu seed của Core hôm nay không có mục nào như vậy ngoài `trang-chu` (đã bỏ), nên ca này chưa có gì để nhìn; giữ trong spec để dữ liệu của dự án không rơi mất lối tắt. Bỏ ca này nếu người dùng thấy `Card` không tiêu đề là thừa.
