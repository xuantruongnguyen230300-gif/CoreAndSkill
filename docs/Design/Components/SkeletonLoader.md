---
kind: luat
scope: core
verified: chua-doi-chieu
---

# SkeletonLoader

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có class, chưa có component Angular nào hiện thực hoá spec này.

**Nền:** tự dựng, không bọc PrimeNG — theo [`../COMPONENTS.md`](../COMPONENTS.md) §4, đây là một hình chữ nhật có màu nền. Không có hành vi nào để mượn, và một lớp bọc thư viện chỉ thêm CSS mặc định phải đè.

---

## Mục đích

Giữ chỗ đúng hình dạng của nội dung sắp hiện, trong lúc dữ liệu chưa về.

## Khi nào dùng / khi nào KHÔNG dùng

| Dùng | Không dùng |
| --- | --- |
| Khối nội dung mà ta **biết trước hình dạng**: hàng bảng, thẻ hồ sơ, ô số liệu | 🛑 Chờ ngắn, không biết trước hình dạng kết quả → `pi-spinner` theo [`../Icons.md`](../Icons.md) §5. Xem so sánh dưới |
| Lần tải **đầu tiên** của một trang hoặc một vùng | 🛑 Đang gửi một biểu mẫu → trạng thái `loading` của [`Button.md`](./Button.md) |
| Ảnh đại diện, tiêu đề, đoạn mô tả, nội dung một tab chưa về | 🛑 Dữ liệu đã về nhưng rỗng → [`EmptyState.md`](./EmptyState.md); 🛑 tải hỏng → [`NoticeBanner.md`](./NoticeBanner.md) kèm nút thử lại |

### Skeleton hay spinner

| | `SkeletonLoader` | Spinner |
| --- | --- | --- |
| Dùng khi | Hình dạng kết quả **đã biết** và ổn định | Hình dạng kết quả **chưa biết**, hoặc chờ rất ngắn |
| Chỗ điển hình | Danh sách, thẻ, trang chi tiết | Nút đang gửi, một ô đang tính lại |
| Nó nói gì | "Sắp có một bảng ở đây, gồm chừng này dòng" | "Đang chạy, chưa xong" |

**Vì sao ranh giới nằm đúng ở chỗ đó:** giá trị duy nhất của skeleton so với spinner là nó **mô tả trước bố cục**. Dùng skeleton cho một thứ chưa biết hình dạng thì ta vẽ ra một lời hứa sai — người dùng nhìn thấy ba khối chữ nhật rồi nhận về một thông báo một dòng, và họ đọc đó là "trang bị lỗi". Ngược lại, spinner cho một bảng dữ liệu bỏ phí cơ hội giữ nguyên bố cục.

## Biến thể

| Biến thể | Hình dạng | Dùng khi |
| --- | --- | --- |
| `text` | Một hoặc nhiều vạch cao bằng một dòng chữ; dòng cuối ngắn hơn — một đoạn văn thật không bao giờ kết thúc đúng mép phải | Tiêu đề, đoạn mô tả, ô chữ trong bảng |
| `block` | Khối chữ nhật đặc, `--radius-sm` | Ô nhập, nút, ảnh thu nhỏ, ô số liệu |
| `circle` | Hình tròn, `--radius-full` | [`Avatar.md`](./Avatar.md), chấm trạng thái |
| `group` | Bộ khối ghép sẵn khớp một component cụ thể | Hàng của [`DataTable.md`](./DataTable.md), thân [`Card.md`](./Card.md) |

## Kích thước

| Cỡ | Chiều cao `block` | Chiều cao vạch `text` | Đường kính `circle` | Dùng khi |
| --- | --- | --- | --- | --- |
| `sm` | `--size-control-sm` (28px) | theo `--fs-xs` × `--lh-normal` | `--icon-lg` | Trong hàng bảng, trong chip |
| `md` | `--size-control-md` (34px) | theo `--fs-md` × `--lh-normal` | `--size-control-md` | **Mặc định** |
| `lg` | `--size-control-lg` (42px) | theo `--fs-lg` × `--lh-normal` | `--size-control-lg` | Ảnh đại diện lớn, ô số liệu |

## Trạng thái

| Trạng thái | Xử lý | Áp dụng |
| --- | --- | --- |
| `default` | **Không áp dụng theo nghĩa thường.** `SkeletonLoader` chỉ tồn tại trong lúc chờ; trạng thái nghỉ của nó là không được render. Hình dạng khi đang hiện mô tả ở dòng `loading` | — |
| `hover` | **Không áp dụng.** Không phải phần tử tương tác; con trỏ giữ nguyên `default` để không hứa là bấm được | — |
| `focus-visible` | **Không áp dụng.** Không nhận focus, không có `tabindex`. Skeleton nằm trong thứ tự Tab là bẫy: người dùng bàn phím Tab vào một ô trống rồi không hiểu mình đang ở đâu | — |
| `active` | **Không áp dụng.** Cùng lý do với `hover` | — |
| `disabled` | **Không áp dụng.** Không có gì để vô hiệu hoá | — |
| `loading` | **Đây là trạng thái duy nhất.** Nền `--color-surface-3`; dải sáng chạy từ `--color-surface-3` sang `--color-surface-2` rồi về, lặp lại. Vùng chứa mang `aria-busy="true"` | Có |
| `error` | Tải hỏng → skeleton **biến mất ngay**, thay bằng [`NoticeBanner.md`](./NoticeBanner.md) kèm nút thử lại. 🛑 Bẫy: để skeleton chạy tiếp khi request đã hỏng — người dùng ngồi chờ một thứ sẽ không bao giờ tới | Có |
| `empty` | Dữ liệu về nhưng rỗng → skeleton biến mất, thay bằng [`EmptyState.md`](./EmptyState.md). Skeleton **không bao giờ** là trạng thái cuối của một màn | Có |

## Token dùng

| Nhóm | Token |
| --- | --- |
| Màu | `--color-surface-3` (nền, đúng vai đã khai ở [`../DESIGN.md`](../DESIGN.md) §2.1), `--color-surface-2` (đỉnh của dải sáng chạy). Hai màu chỉ chênh 1.11:1 sáng và 1.18:1 tối — **chủ đích**: skeleton không mang thông tin nên không có ngưỡng tương phản phải đạt, và một dải sáng tương phản cao sẽ nhấp nháy kéo mắt về đúng chỗ chưa có gì để đọc |
| Chữ | `--fs-xs`, `--fs-md`, `--fs-lg`, `--lh-normal` — chỉ để **tính chiều cao vạch**, không có chữ nào được vẽ |
| Khoảng cách, hình dạng | `--sp-2`, `--sp-3`, `--sp-6`, `--radius-sm`, `--radius-md`, `--radius-full` |
| Kích thước, chuyển động | `--size-control-sm`, `--size-control-md`, `--size-control-lg`, `--icon-lg`, `--ease-standard` |

## Responsive

| Ngưỡng | Hành vi |
| --- | --- |
| ≥ `--bp-md` | Số khối giữ chỗ khớp số phần tử thật sẽ hiện ở ngưỡng này |
| < `--bp-md` | Biến thể `group` bỏ bớt các khối tương ứng với cột bị ẩn ở màn nhỏ. Vẽ đủ mọi cột rồi để bảng đổi sang một cột là tạo ra đúng cú nhảy bố cục mà skeleton sinh ra để tránh |
| < `--bp-xs` | `text` giảm còn hai vạch; bề rộng chuyển hết sang mức `full` |

**Số dòng skeleton nên khớp số dòng thật sắp hiện.** Vẽ mười dòng rồi nhận về ba là một cú nhảy; vẽ ba rồi nhận về mười cũng vậy. Với danh sách phân trang, con số đúng là **số dòng mỗi trang** đang chọn — nó đã biết trước, không phải đoán.

## Accessibility

| Khoản | Yêu cầu |
| --- | --- |
| Thẻ | Phần tử khối rỗng, không chữ bên trong. Không dùng thẻ ngữ nghĩa (`<p>`, `<h2>`) cho một ô trống |
| Vai trò ARIA | `aria-hidden="true"` trên **mọi** khối skeleton; vùng chứa mang `aria-busy="true"` khi đang chờ và gỡ bỏ khi xong |
| Nhãn, thông báo chờ | Đúng **một** vùng `aria-live="polite"` cho cả vùng đang tải, nội dung là một câu: "Đang tải danh sách người dùng". Vùng này là **thứ duy nhất** trình đọc màn hình đọc lên |
| Khi xong | Nội dung vùng `aria-live` đổi thành kết quả ("Đã tải 24 bản ghi") rồi xoá. Im lặng khi tải xong khiến người dùng không biết mình chờ được chưa |
| Bàn phím, focus, vùng bấm | Không có `tabindex`, không nhận focus, không nằm trong thứ tự Tab; không bấm được và không được nằm chồng lên một vùng bấm thật |
| Chuyển động | Dải sáng chạy **phải tắt** khi `prefers-reduced-motion: reduce` — xem dưới |

🛑 **Bẫy: để trình đọc màn hình đọc từng khối skeleton là tra tấn.** Một bảng mười dòng, sáu cột sinh ra sáu mươi khối. Không có `aria-hidden`, người dùng trình đọc màn hình phải nghe sáu mươi lần một thứ vô nghĩa trước khi tới nội dung thật — và thứ được đọc lên thường là tạp âm chứ không phải một từ. Luật ở đây gọn: **skeleton câm hoàn toàn, một vùng `aria-live` nói thay cho tất cả.**

🛑 **Dải sáng chạy là chuyển động lặp vô hạn.** [`../DESIGN.md`](../DESIGN.md) §7 cấm chuyển động lặp vô hạn trừ chỉ báo loading — skeleton nằm đúng trong ngoại lệ đó, nhưng ngoại lệ ấy **không** miễn trừ `prefers-reduced-motion: reduce`. Khi người dùng khai giảm chuyển động, dải sáng dừng hẳn và khối giữ nguyên nền `--color-surface-3` tĩnh. Khối tĩnh vẫn làm đủ việc của nó — giữ chỗ và mô tả bố cục; chuyển động chỉ nói thêm "đang chạy", và câu đó đã có vùng `aria-live` nói rồi.

## API dự kiến

| Tên | Chiều | Kiểu | Mặc định | Ghi chú |
| --- | --- | --- | --- | --- |
| `variant` | input | `'text' \| 'block' \| 'circle' \| 'group'` | `'text'` | |
| `size` | input | `'sm' \| 'md' \| 'lg'` | `'md'` | |
| `width` | input | `'full' \| 'wide' \| 'half' \| 'short'` | `'full'` | Bốn mức đặt sẵn, không nhận số tự do — số tự do là cách một hệ có hai mươi bề rộng skeleton mà không cái nào khớp nội dung thật |
| `lines` · `repeat` | input | `number` | `1` | `lines` chỉ có nghĩa với `variant = 'text'`, dòng cuối tự rút ngắn; `repeat` là số lần lặp cả cụm, dùng cho danh sách và hàng bảng |
| `preset` | input | `string \| null` | `null` | Chỉ có nghĩa với `variant = 'group'`; tên khuôn ghép sẵn |
| `rounded` | input | `boolean` | `false` | Ép `--radius-full` cho `block` khi nó giữ chỗ cho một chip |

Không có `output()` nào: skeleton không tương tác, không có gì để báo ra ngoài. Đây là component **dumb** ([`../COMPONENTS.md`](../COMPONENTS.md) §5) ở mức triệt để nhất trong bảng — nó không biết mình đang chờ cái gì, không biết request nào đang bay. Ràng buộc đi kèm: nơi gọi **phải** tắt skeleton ở cả nhánh thành công lẫn nhánh lỗi; chỉ tắt ở nhánh thành công là cách một màn treo vĩnh viễn ở trạng thái đang tải.

## Do / Don't

- ✅ Hình dạng skeleton khớp hình dạng nội dung thật, kể cả số dòng.
- ✅ `aria-hidden="true"` cho mọi khối, một vùng `aria-live` nói thay.
- ✅ Tắt dải sáng chạy khi `prefers-reduced-motion: reduce`.
- ✅ Tắt skeleton ở **cả** nhánh lỗi, không chỉ nhánh thành công.
- ❌ Không để skeleton nhận focus, và không dùng nó làm trạng thái rỗng.
- ❌ Không vẽ ba vạch dài bằng nhau cho một đoạn văn, và không dùng bề rộng tự do.
- ❌ Không dùng màu tương phản cao cho dải sáng chạy.

## Cần chốt

| # | Câu hỏi | Ai trả lời được |
| --- | --- | --- |
| 1 | Thời lượng một vòng sáng chạy chưa có token. [`../DESIGN.md`](../DESIGN.md) §7 dừng ở `--dur-slow`, vốn dành cho chuyển tiếp một lần; một vòng lặp cần thời lượng dài hơn hẳn và thuộc một nhóm khác | Người dựng hệ token |
| 2 | Có ngưỡng thời gian tối thiểu trước khi hiện skeleton không? Dữ liệu về sau một khoảng rất ngắn sẽ tạo ra cú nháy skeleton còn khó chịu hơn là không có gì; nhưng ngưỡng chờ lại làm màn có vẻ đơ ở đầu | Sau khi đo thời gian phản hồi thật |
| 3 | Danh sách khuôn `group` gồm những gì, và ai giữ nó? Để mỗi màn tự ghép khối là quay lại đúng vấn đề bề rộng tự do | Sau khi có `DataTable` và `Card` thật |
