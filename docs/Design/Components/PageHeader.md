---
kind: luat
scope: core
verified: chua-doi-chieu
---

# PageHeader

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng. Theo tiêu chí ở [`../COMPONENTS.md`](../COMPONENTS.md) §4 đây là bố cục thuần; giá trị của nó nằm ở việc **buộc mọi trang mở đầu giống nhau**, không ở hành vi.

---

## Mục đích

Mở đầu một trang: cho biết đang ở đâu, trang này là gì, và hành động chính của nó nằm đâu.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Mọi trang trong khung ứng dụng | 🛑 Thao tác cấp tài khoản (đăng xuất, đổi theme) → [`Topbar.md`](./Topbar.md) |
| Trang danh sách, trang chi tiết, trang cấu hình | 🛑 Tìm kiếm, lọc, sắp xếp trên một danh sách → [`Toolbar.md`](./Toolbar.md). Nó nằm **trong** vùng dữ liệu, không phải ở đầu trang |
| Trang cần đường dẫn phân cấp | 🛑 Tiêu đề của một khối nội dung trong trang → phần đầu của [`Card.md`](./Card.md) |
| | 🛑 Màn xác thực → [`AuthCard.md`](./AuthCard.md) |

**Ranh giới với `Toolbar`:** `PageHeader` chứa hành động **tạo ra thứ mới** hoặc **áp cho cả trang** (Thêm mới, Xuất dữ liệu, Cấu hình). `Toolbar` chứa thao tác **trên tập dữ liệu đang xem** (tìm, lọc, xoá bộ lọc, thao tác hàng loạt). Nhầm chỗ thì hai vùng cùng có nút và người dùng phải quét cả hai.

## Biến thể

| Biến thể | Thành phần | Dùng khi |
| --- | --- | --- |
| `default` | Tiêu đề + mô tả + nhóm hành động | Trang danh sách, trang cấu hình |
| `with-breadcrumb` | Thêm đường dẫn phân cấp phía trên tiêu đề | Trang nằm sâu từ hai cấp trở lên |
| `detail` | Thêm nút quay lại, thêm [`Badge.md`](./Badge.md) trạng thái cạnh tiêu đề | Trang chi tiết một bản ghi |
| `minimal` | Chỉ tiêu đề | Trang không có hành động nào |

**Nút quay lại chỉ có ở biến thể `detail`, và nó là một liên kết chứ không phải nút "lùi lịch sử".** Dùng `history.back()` là sai: người dùng có thể vào thẳng trang chi tiết từ một liên kết dán vào trình duyệt, và lúc đó "lùi" sẽ đưa họ ra khỏi ứng dụng. Nút quay lại phải trỏ tới **tuyến cha** đã biết.

## Kích thước

| Khoản | Giá trị |
| --- | --- |
| Khe đường dẫn → tiêu đề | `--sp-3` |
| Khe tiêu đề → mô tả | `--sp-3` |
| Khe khối chữ → nhóm hành động | `--sp-6` |
| Khe dưới `PageHeader` → nội dung | `--sp-8` |
| Cỡ chữ tiêu đề | `--fs-xl`, `--fw-bold`, `--lh-tight`, `--ls-tight` |
| Cỡ chữ mô tả | `--fs-sm`, `--color-text-muted`, `--lh-normal` |
| Cỡ chữ đường dẫn | `--fs-xs`, `--color-text-muted` |
| Bề rộng tối đa mô tả | `--layout-form-w` |
| Khe giữa các nút | `--sp-4` |

**`PageHeader` không có nền, không có viền, không có bóng.** Nó nằm trực tiếp trên `--color-bg`. Bọc nó trong một `Card` làm trang có hai lớp hộp lồng nhau ngay từ đầu và tốn chiều dọc vô ích. Nó **cuộn cùng nội dung**, kể cả đường dẫn phân cấp — việc "vẫn biết đang ở đâu khi đã cuộn" do tiêu đề tuyến dính đỉnh ở [`Topbar.md`](./Topbar.md) gánh.

**Bề rộng mô tả có trần** vì lý do đọc: một dòng chữ trải hết bề rộng `main` thì mắt mất dòng khi xuống hàng. Trần đó dùng `--layout-form-w` — bí danh của bậc "form thông thường" trong thang bề rộng ở [`../DESIGN.md`](../DESIGN.md) §6.1, tức **cùng bề rộng dòng** mà khối form trong trang đã dùng. Mô tả của `PageHeader` và form ngay dưới nó đọc cùng một độ dài dòng, và không bậc thứ tư nào được đẻ ra. Tiêu đề không cần giới hạn — nó ngắn.

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | Đường dẫn (nếu có), tiêu đề, mô tả (nếu có), nhóm hành động ghim phải | Có |
| `hover` | **Không áp dụng ở mức component.** Hover thuộc nút và liên kết bên trong | — |
| `focus-visible` | **Không áp dụng ở mức component.** Focus thuộc các phần tử tương tác bên trong | — |
| `active` | **Không áp dụng.** Không phải control | — |
| `disabled` | **Không áp dụng ở mức component.** Từng nút có trạng thái `disabled` riêng | — |
| `loading` | Trang chi tiết chưa biết tên bản ghi: [`SkeletonLoader.md`](./SkeletonLoader.md) thay chỗ tiêu đề, giữ **đúng chiều cao** của một dòng tiêu đề thật. Đường dẫn và nút hiện bình thường nếu đã biết | Có |
| `error` | **Không áp dụng.** Lỗi tải dữ liệu hiện ở vùng nội dung, không ở đầu trang. `PageHeader` vẫn phải hiện để người dùng biết mình đang ở đâu và quay lại được | — |
| `empty` | **Không áp dụng.** Tiêu đề luôn có. Một trang không có tiêu đề là một trang không đặt tên được — dấu hiệu trang đó chưa rõ mục đích | — |

**Skeleton của tiêu đề phải đúng chiều cao dòng tiêu đề thật.** Đây là ca hay làm sai: một khối skeleton cao 20px thay cho một dòng `--fs-xl` cao 28px sẽ khiến cả trang nhảy lên 8px khi dữ liệu về.

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-text`, `--color-text-muted`, `--color-brand`, `--color-focus` |
| Chữ | `--fs-xs`, `--fs-sm`, `--fs-xl`, `--fw-bold`, `--fw-regular`, `--lh-tight`, `--lh-normal`, `--ls-tight` |
| Khoảng cách | `--sp-3`, `--sp-4`, `--sp-6`, `--sp-8` |
| Kích thước | `--icon-sm`, `--icon-md`, `--layout-form-w` (trần bề rộng mô tả) |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `$bp-md` | Khối chữ bên trái, nhóm hành động bên phải, cùng một hàng, căn theo mép trên |
| `$bp-sm` … `$bp-md` | Nhóm hành động xuống hàng dưới khối chữ, căn trái |
| < `$bp-sm` | Đường dẫn rút gọn còn cấp cha trực tiếp; mô tả **ẩn** nếu nhóm hành động có từ hai nút; nút chuyển `block` |
| < `$bp-xs` | Cỡ tiêu đề hạ xuống `--fs-lg`; chỉ giữ hành động chính, các hành động phụ dồn vào một [`Menu.md`](./Menu.md). Ngưỡng dồn là điểm ngắt này, không phải số nút |

🛑 **Mô tả ẩn ở màn nhỏ là một sự đánh đổi, không phải một tối ưu.** Nó chỉ chấp nhận được khi mô tả thuần tuý bổ trợ. Nếu mô tả chứa thông tin cần thiết (một cảnh báo, một điều kiện) thì nó không thuộc `PageHeader` — nó thuộc [`NoticeBanner.md`](./NoticeBanner.md), và banner thì không bao giờ ẩn.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Tiêu đề | `<h1>`. **Đây là `<h1>` duy nhất của trang** — [`Topbar.md`](./Topbar.md) cố ý không dùng heading cho tiêu đề tuyến |
| Đường dẫn | `<nav aria-label="Đường dẫn">` chứa `<ol>`; mục cuối (trang hiện tại) **không phải liên kết** và mang `aria-current="page"` |
| Dấu phân cách đường dẫn | Vẽ bằng CSS hoặc mang `aria-hidden="true"`. Trình đọc màn hình đọc "gạch chéo" giữa mỗi cấp là tạp âm |
| Nút quay lại | `<a>` với `routerLink` trỏ tuyến cha, `aria-label` nêu **về đâu**: "Quay lại danh sách người dùng" |
| Nhóm hành động | Đứng **sau** khối chữ trong DOM, kể cả khi hiện bên phải. Thứ tự đọc là: đang ở đâu → trang gì → làm được gì |
| Badge trạng thái | Là chữ, không chỉ là màu ([`../DESIGN.md`](../DESIGN.md) §2.7) |
| Đổi tiêu đề khi chuyển tuyến | Tiêu đề tài liệu (`<title>`) phải đổi theo. Không đổi thì người dùng trình đọc màn hình chuyển trang mà không biết mình đã tới đâu — đây là việc của tầng định tuyến, ghi ở đây vì `PageHeader` là nơi tiêu đề đó xuất hiện |
| Chữ | Qua i18n — [`../../RULES.md`](../../RULES.md) §7 F8 |

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `title` | input | `string` | — | Bắt buộc |
| `description` | input | `string \| null` | `null` | |
| `breadcrumbs` | input | `ReadonlyArray<{ label: string; route: string \| null }>` | `[]` | `route: null` cho mục cuối |
| `backRoute` | input | `string \| null` | `null` | Tuyến cha. Không phải `history.back()` |
| `backLabel` | input | `string \| null` | `null` | Dùng cho `aria-label` của nút quay lại |
| `loading` | input | `boolean` | `false` | Chỉ ảnh hưởng tiêu đề |
| `headingId` | input | `string` | tự sinh | Để vùng nội dung trỏ tới qua `aria-labelledby` |

Nhóm hành động vào qua slot, không qua input. Hành động là các [`Button.md`](./Button.md) thật với nhãn, icon và trạng thái riêng; một mảng input kiểu chuỗi sẽ không diễn tả được điều đó.

Badge trạng thái ở biến thể `detail` cũng vào qua một slot riêng cạnh tiêu đề.

`PageHeader` là component **dumb**: nó nhận đường dẫn phân cấp qua `input()` thay vì tự dựng từ router. Tự dựng từ router nghe tiện và sai — nhãn của một cấp thường là **tên một bản ghi** ("Nguyễn Văn A"), thứ chỉ trang mới biết.

## Do / Don't

- ✅ Mọi trang trong khung ứng dụng mở đầu bằng `PageHeader`.
- ✅ Tiêu đề là `<h1>`, và là `<h1>` duy nhất.
- ✅ Đúng một hành động `primary`.
- ✅ Nút quay lại trỏ tuyến cha, không lùi lịch sử.
- ✅ Skeleton tiêu đề giữ đúng chiều cao.
- ❌ Không đặt tìm kiếm hay bộ lọc vào `PageHeader`.
- ❌ Không bọc `PageHeader` trong `Card`.
- ❌ Không tự dựng đường dẫn phân cấp từ router.
- ❌ Không để mục cuối của đường dẫn là liên kết.
- ❌ Không ẩn mô tả chứa thông tin cần thiết — chuyển nó thành `NoticeBanner`.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Trang chi tiết có hiện thông tin phụ (người tạo, ngày sửa cuối) ở `PageHeader` không? Nó tiện nhưng làm phần đầu trang cao lên và đẩy nội dung xuống | F3 — khi dựng màn chi tiết đầu tiên |
